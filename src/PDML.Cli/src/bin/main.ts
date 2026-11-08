#!/bin/env node
import * as child_process from "node:child_process";
import * as process from "node:process";
import * as path from "node:path";

import {native, discover} from "../lib/lib";

const executable: string = native.resolveNative();
let extensions: string = discover
    .discoverExtensions(import.meta.dirname)
    .map(extension => extension.extensionDir)
    .join(path.delimiter);

if (process.env["PDML_EXTENSIONS_PATH"] && extensions) {
    process.env["PDML_EXTENSIONS_PATH"] += path.delimiter + extensions;
} else if (extensions) {
    process.env["PDML_EXTENSIONS_PATH"] = extensions;
}

const child: child_process.ChildProcess = child_process.spawn(executable, process.argv.slice(2), {
    stdio: "inherit",
    shell: false,
    env: process.env
});

child.on("exit", (code) => {
    if (code != null) {
        process.exit(code);
    } else {
        process.exit(1);
    }
});

