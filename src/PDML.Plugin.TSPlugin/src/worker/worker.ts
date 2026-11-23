import {parentPort} from "node:worker_threads";

import {Compiler, type CompileResult, type IPCException} from "@pdml/cli";

import type {CompileCommand, WorkerCommand} from "./shared";

const port = parentPort;
if (!port) {
    throw new Error("This file must be started as a worker thread");
}

async function compile(request: CompileCommand): Promise<CompileResult | IPCException> {
    try {
        const compiler: Compiler = new Compiler(request.executable);
        return compiler.compile({
            generator: request.generator,
            dllPaths: request.dllPaths,
            files: request.files,
            properties: {
                "FileExtension": ".pdml.d.ts"
            }
        });
    } catch (error) {
        return {
            name: "Error",
            message: String(error),
            innerException: null
        };
    }
}

port.on("message", async (request: WorkerCommand) => {
    switch (request.command) {
        case "compile":
            const res: IPCException | CompileResult = await compile(request);
            port.postMessage({
                ...res,
                id: request.id
            });
            return;
    }
});
