import * as fs from "fs/promises";
import * as path from "path";
import * as os from "os";

import * as vscode from "vscode";

import * as release from "./release.js";

export interface ResolvedServer {
    command: string;
    args: string[];
}

export async function resolveServer(context: vscode.ExtensionContext): Promise<ResolvedServer> {
    if (context.extensionMode != vscode.ExtensionMode.Production) {
        const projectPath = path.resolve(context.extensionPath, "../PDML.Plugin.LSP/PDML.Plugin.LSP.csproj");
        const dotnet: string = await resolveCommand("dotnet");


        return {
            command: dotnet,
            args: ["run", "--project", projectPath]
        }
    }

    const executePath: string = await release.ensureLatestVersion(context);
    
    return {
        command: executePath,
        args: []
    }
}

export async function resolveCommand(hint: string): Promise<string> {
    const pathEnv: string = process.env.PATH ?? "";
    const pathSeperator: string = path.delimiter;

    const executableNames = os.platform() == "win32"
        ? [`${hint}.exe`, `${hint}.cmd`, `${hint}.bat`, hint]
        : [hint, `${hint}.sh`]

    for (const directory of pathEnv.split(pathSeperator)) {
        if (!directory) {
            continue;
        }

        for (const executableName of executableNames) {
            const candidate = path.join(directory, executableName);

            try {
                await fs.access(candidate);
                return candidate;
            } catch {
                // Continue
            }
        }
    }

    throw new Error(`Could not find '${hint}' on PATH`);
}