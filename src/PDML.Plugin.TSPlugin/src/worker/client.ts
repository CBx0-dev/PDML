import * as path from "node:path";
import {Worker} from "node:worker_threads";

import type {CompileCommand, CompileWorker, WorkerCommand, WorkerResult} from "./shared";
import {CompileResult, IPCException} from "@pdml/cli";

interface PendingJob {
    resolve(outcome: unknown): void;
    timer: NodeJS.Timeout;
}

export class CompileClient implements CompileWorker {
    private readonly workerFile: string;
    private readonly timeoutMs: number;
    private readonly pending: Map<number, PendingJob>;

    private worker: Worker | null = null;
    private nextId: number = 1;

    public constructor(
        workerFile: string = path.join(__dirname, "worker.js"),
        timeoutMs: number = 120_000
    ) {
        this.workerFile = workerFile;
        this.timeoutMs = timeoutMs;
        this.pending = new Map<number, PendingJob>();
    }

    public send<T = unknown>(command: Omit<WorkerCommand, "id">): Promise<T> {
        return new Promise<T>((resolve) => {
            let worker: Worker;
            try {
                worker = this.ensureWorker();
            } catch (error) {
                resolve({
                    name: "Error",
                    message: String(error),
                    innerException: null
                } as T);
                return;
            }

            const id: number = this.nextId++;
            const timer: NodeJS.Timeout = setTimeout(() => {
                this.pending.delete(id);
                resolve({
                    name: "Error",
                    message: `The PDML compiler did not answer within ${this.timeoutMs} ms`,
                    innerException: null
                } as T);

                this.resetWorker(worker);
            }, this.timeoutMs);
            timer.unref();

            this.pending.set(id, {
                resolve: resolve as (outcome: unknown) => void,
                timer
            });
            worker.postMessage({...command, id});
        });
    }

    public compile(request: Omit<CompileCommand, "id">): Promise<IPCException | CompileResult> {
        return this.send<IPCException | CompileResult>(request);
    }

    public dispose(): void {
        this.failAll("The compile worker was disposed");
        if (!this.worker) {
            return;
        }

        const worker: Worker = this.worker;
        this.worker = null;
        void worker.terminate();
    }

    private ensureWorker(): Worker {
        if (this.worker != null) {
            return this.worker;
        }

        const worker: Worker = new Worker(this.workerFile);
        worker.unref();

        worker.on("message", (result: WorkerResult) => {
            const job: PendingJob | undefined = this.pending.get(result.id);
            if (!job) {
                return;
            }

            clearTimeout(job.timer);
            this.pending.delete(result.id);
            job.resolve(result);
        });

        worker.on("error", async (error: unknown): Promise<void> => {
            this.failAll(`The compile worker crashed: ${error instanceof Error ? error.message : String(error)}`);
            await this.resetWorker(worker);
        });

        worker.on("exit", async (): Promise<void> => {
            this.failAll("The compile worker exited unexpectedly");
            await this.resetWorker(worker);
        });

        this.worker = worker;
        return worker;
    }

    private async resetWorker(worker: Worker): Promise<void> {
        if (this.worker == worker) {
            this.worker = null;
        }

        await worker.terminate();
    }

    private failAll(error: string): void {
        for (const [id, job] of this.pending.entries()) {
            clearTimeout(job.timer);
            this.pending.delete(id);
            job.resolve({
                name: "Error",
                message: error,
                innerException: null
            });
        }
    }
}
