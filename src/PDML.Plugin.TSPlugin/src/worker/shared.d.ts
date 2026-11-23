import type {CompileResult, IPCException} from "@pdml/cli";

export type WorkerCommand =
    | CompileCommand
    ;

export interface CompileCommand {
    readonly command: "compile";
    readonly id: number;

    readonly files: string[];
    readonly generator: string;
    readonly dllPaths: string[];
    readonly executable?: string;
}

export interface WorkerResult<T = unknown> {
    readonly id: number;
    readonly data: T;
}

export interface CompileWorker {
    send?<T = unknown>(command: Omit<WorkerCommand, "id">): Promise<T>;
    compile(request: Omit<CompileCommand, "id">): Promise<IPCException | CompileResult>;
    dispose(): void;
}
