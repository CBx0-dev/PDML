import * as path from "node:path";

import type * as ts from "typescript/lib/tsserverlibrary";

import {CompileClient} from "./worker/client";
import * as host from "./host";
import type {Logger} from "./logger";
import {PdmlWorkspace, type WorkspaceSettings} from "./workspace";

interface PluginConfig {
    cli?: string;
    generator?: string;
    dllPaths?: string[];
    debounceMs?: number;
}

const GENERATOR_PACKAGES: string[] = [
    "@pdml/typescript-generator"
];

function registerGeneratorPackages(logger?: Logger): void {
    for (const name of GENERATOR_PACKAGES) {
        try {
            require(name);
        } catch (error) {
            if (logger) {
                logger.info(`[pdml] generator package '${name}' is not available: ${String(error)}`);
            }
        }
    }
}

function dllPathsFromEnvironment(): string[] {
    const envPath: string | undefined = process.env["PDML_EXTENSIONS_PATH"];
    if (!envPath) {
        return [];
    }

    return envPath
        .split(path.delimiter)
        .filter((entry) => entry.length > 0);
}

function init(modules: { typescript: typeof ts }): ts.server.PluginModule {
    const tsModule: typeof ts = modules.typescript;

    let editorConfig: PluginConfig = {};

    return {
        create(info: ts.server.PluginCreateInfo): ts.LanguageService {
            const logger: Logger = {
                info: (message: string): void => info.project.projectService.logger.info(message)
            };
            logger.info("[pdml] Starting plugin");

            registerGeneratorPackages(logger);

            const configOf = (): PluginConfig => ({...(info.config as PluginConfig | undefined), ...editorConfig});

            const settings = (): WorkspaceSettings => {
                const config: PluginConfig = configOf();

                return {
                    executable: config.cli,
                    generator: config.generator ?? "typescript",
                    dllPaths: [...new Set([...(config.dllPaths ?? []), ...dllPathsFromEnvironment()])]
                };
            };

            const workspace: PdmlWorkspace = new PdmlWorkspace({
                backend: new CompileClient(),
                logger,
                caseSensitive: info.serverHost.useCaseSensitiveFileNames,
                settings,
                debounceMs: configOf().debounceMs,
                onChange: (): void => {
                    try {
                        const project = info.project as unknown as {
                            markAsDirty?: () => void;
                            refreshDiagnostics?: () => void;
                        };
                        if (project.markAsDirty) {
                            project.markAsDirty();
                        }
                        if (project.refreshDiagnostics) {
                            project.refreshDiagnostics();
                        }
                    } catch (error) {
                        logger.info(`[pdml] unable to refresh the project: ${String(error)}`);
                    }
                }
            });

            host.patchHost(tsModule, info.languageServiceHost, workspace, logger);

            return host.decorateLanguageService(info.languageService, workspace, () => workspace.dispose());
        },

        onConfigurationChanged(config: PluginConfig): void {
            editorConfig = config ?? {};
        }
    };
}

export = init;
