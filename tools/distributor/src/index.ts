#!/usr/bin/env node
import * as process from "node:process";
import {Command} from "commander";

import * as utils from "./utils";
import * as stage from "./stage";

export * as config from "./config";
export * as utils from "./utils";
export * as stage from "./stage";

const program = new Command();

program
    .name("distributor")
    .description("PDML workspace and package staging tool");

program
    .command("list")
    .description("List all NPM packages")
    .action(() => {
        console.log(utils.getAllPackageNames().join("\n"));
    });

program
    .command("native")
    .description("Stage native compiler package")
    .requiredOption("--rid <value>", "The targeting native runtime identifier")
    .requiredOption("--version <value>", "Package version (e.g. 1.2.3)")
    .requiredOption("--publish-dir <dir>", "Directory containing published native compiler")
    .requiredOption("--out <dir>", "Output directory for staged package")
    .action((options) => {
        try {
            const dir: string = stage.stageNative({
                rid: options.rid,
                version: options.version,
                publishDir: options.publishDir,
                out: options.out
            });
            console.log(dir);
        } catch (err: unknown) {
            console.error(`distributor: ${(err as Error).message}`);
            process.exit(1);
        }
    });

program
    .command("generator")
    .description("Stage code generator extension package")
    .requiredOption("--name <value>", "Generator name (csharp, typescript)")
    .requiredOption("--version <value>", "Package version (e.g. 1.2.3)")
    .requiredOption("--dll-dir <dir>", "Directory containing generator dll")
    .requiredOption("--out <dir>", "Output directory for staged package")
    .action((options) => {
        try {
            const dir: string = stage.stageGenerator({
                name: options.name,
                version: options.version,
                dllDir: options.dllDir,
                out: options.out
            });
            console.log(dir);
        } catch (err: unknown) {
            console.error(`distributor: ${(err as Error).message}`);
            process.exit(1);
        }
    });

program
    .command("cli")
    .description("Stage CLI package")
    .requiredOption("--version <value>", "Package version (e.g. 1.2.3)")
    .requiredOption("--out <dir>", "Output directory for staged package")
    .action((options) => {
        try {
            const dir: string = stage.stageCli({
                version: options.version,
                out: options.out
            });
            console.log(dir);
        } catch (err: unknown) {
            console.error(`distributor: ${(err as Error).message}`);
            process.exit(1);
        }
    });

program
    .command("placeholder")
    .description("Stage placeholder packages for initial bootstrap")
    .requiredOption("--out <dir>", "Output directory for staged packages")
    .action((options) => {
        try {
            const dirs: string[] = stage.stagePlaceholders({
                out: options.out
            });
            for (const dir of dirs) {
                console.log(dir);
            }
        } catch (err: unknown) {
            console.error(`distributor: ${(err as Error).message}`);
            process.exit(1);
        }
    });

program.parse();