import * as child_process from "node:child_process";

import * as native from "./native";

import type {Diagnostic} from "./diagnostic";
import type {IPCException} from "./exception";


export interface CompileResult {
    readonly diagnostics: Diagnostic[];
    readonly outputFiles: Readonly<Record<string, string>>;
}

interface BaseCompileOptions {
    generator: string;
    properties?: Record<string, string>;
    dllPaths?: string[];
    output?: string;
}

interface CompileDiskOptions extends BaseCompileOptions {
    files: string[];
}

export interface CompileVirtualOptions extends BaseCompileOptions {
    stdin: string;
}

export type CompileOptions = CompileDiskOptions | CompileVirtualOptions;

export class Compiler {
    private readonly _executable: string;

    public constructor(executable?: string) {
        this._executable = executable ?? native.resolveNative();
    }

    public compile(options: CompileOptions): CompileResult | IPCException {
        const args: string[] = ["compile", "-f", "json"];
        args.push("-g", options.generator);
        if (options.dllPaths) {
            for (const dllPath of options.dllPaths) {
                args.push("--dll-path", dllPath);
            }
        }
        if (options.properties) {
            for (const [key, value] of Object.entries(options.properties)) {
                args.push(`-p:${key}`, value);
            }
        }

        if ("files" in options) {
            args.push(...options.files);
            return this.exec(args);
        }

        args.push("--stdin");
        return this.exec(args, options.stdin);
    }

    private exec<T>(args: string[], stdin?: string): T {
        const process = child_process.spawnSync(this._executable, args, {
            stdio: "pipe",
            input: stdin
        });

        const stdout: string = process.stdout.toString("utf-8");

        try {
            return JSON.parse(stdout);
        } catch {
            throw new Error("Unable to parse output");
        }
    }
}