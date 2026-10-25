import * as vscode from "vscode";

import * as lsp from "./lsp.js";

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    await Promise.allSettled([
        lsp.activate(context)
    ]);
}

export async function deactivate(): Promise<void> {
    await Promise.allSettled([
        lsp.deactivate()
    ]);
}