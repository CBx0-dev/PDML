import * as vscode from "vscode";

import {
    LanguageClient,
    LanguageClientOptions,
    ServerOptions
} from "vscode-languageclient/node";

import * as resolver from "./resolver.js";

let client: LanguageClient | undefined;

export async function activate(context: vscode.ExtensionContext): Promise<void> {
    const server: resolver.ResolvedServer = await resolver.resolveServer(context);
    console.log(server);

    const serverOptions: ServerOptions = {
        command: server.command,
        args: server.args,
        options: {
            cwd: context.extensionPath
        }
    };

    const clientOptions: LanguageClientOptions = {
        documentSelector: [
            {
                scheme: "file",
                language: "pdml"
            }
        ]
    };

    client = new LanguageClient(
        "PDML",
        serverOptions,
        clientOptions
    );

    context.subscriptions.push(client);
    await client.start();
}

export async function deactivate(): Promise<void> {
    if (client) {
        await client.stop();
    }
}