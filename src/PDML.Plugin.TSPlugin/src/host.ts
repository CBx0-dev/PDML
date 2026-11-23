import type * as ts from "typescript/lib/tsserverlibrary";

import * as convention from "./convention";
import {redirectDefinitions} from "./definitions";
import type {Logger} from "./logger";
import type {PdmlWorkspace} from "./workspace";

type Host = ts.LanguageServiceHost & {
    resolveModuleNameLiterals?: (...args: any[]) => any;
    resolveModuleNames?: (...args: any[]) => any;
    hasInvalidatedResolutions?: (filePath: ts.Path) => boolean;
};

function makeResolution(tsModule: typeof ts, virtualPath: string): ts.ResolvedModuleWithFailedLookupLocations {
    return {
        resolvedModule: {
            resolvedFileName: convention.fromVirtualPath(virtualPath),
            extension: tsModule.Extension.Ts,
            isExternalLibraryImport: false,
            resolvedUsingTsExtension: false
        },
        failedLookupLocations: [],
        affectingLocations: [],
        resolutionDiagnostics: []
    } as unknown as ts.ResolvedModuleWithFailedLookupLocations;
}

function served(fileName: string, workspace: PdmlWorkspace): string | null {
    let key: string = fileName;
    if (convention.isPdmlPath(fileName)) {
        key = convention.toVirtualPath(fileName);
    }

    if (convention.isVirtualPath(key) && workspace.hasVirtualFile(key)) {
        return key;
    }

    return null;
}

export function patchHost(
    tsModule: typeof ts,
    host: ts.LanguageServiceHost,
    workspace: PdmlWorkspace,
    logger: Logger
): void {
    const target: Host = host as Host;

    if (target.resolveModuleNameLiterals) {
        target.resolveModuleNameLiterals = overrideResolveModuleNameLiterals(
            target.resolveModuleNameLiterals.bind(target),
            tsModule,
            workspace
        );
    } else if (target.resolveModuleNames) {
        target.resolveModuleNames = overrideResolveModuleNames(
            target.resolveModuleNames.bind(target),
            tsModule,
            workspace
        );
    }

    target.hasInvalidatedResolutions = overrideHasInvalidatedResolutions(
        target.hasInvalidatedResolutions ? target.hasInvalidatedResolutions.bind(target) : null,
        workspace
    );

    target.fileExists = overrideFileExists(
        target.fileExists ? target.fileExists.bind(target) : null,
        tsModule,
        workspace
    );

    target.readFile = overrideReadFile(
        target.readFile ? target.readFile.bind(target) : null,
        tsModule,
        workspace
    );

    target.getScriptSnapshot = overrideGetScriptSnapshot(
        target.getScriptSnapshot.bind(target),
        tsModule,
        workspace
    );

    target.getScriptVersion = overrideGetScriptVersion(
        target.getScriptVersion.bind(target),
        workspace
    );

    target.getScriptKind = overrideGetScriptKind(
        target.getScriptKind ? target.getScriptKind.bind(target) : null,
        tsModule,
        workspace
    );

    target.getProjectVersion = overrideGetProjectVersion(
        target.getProjectVersion ? target.getProjectVersion.bind(target) : null,
        workspace
    );

    logger.info("[pdml] language service host patched");
}

function overrideResolveModuleNameLiterals(
    original: ((literals: readonly ts.StringLiteralLike[], containingFile: string, ...rest: unknown[]) => readonly ts.ResolvedModuleWithFailedLookupLocations[]) | null,
    tsModule: typeof ts,
    workspace: PdmlWorkspace
) {
    return function (literals: readonly ts.StringLiteralLike[], containingFile: string, ...rest: unknown[]): ts.ResolvedModuleWithFailedLookupLocations[] {
        const results: Array<ts.ResolvedModuleWithFailedLookupLocations | undefined> = new Array(literals.length);
        const others: ts.StringLiteralLike[] = [];
        const otherIndexes: number[] = [];

        literals.forEach((literal, index) => {
            const virtualPath: string | null = workspace.resolve(literal.text, containingFile);
            if (virtualPath) {
                results[index] = makeResolution(tsModule, virtualPath);
            } else {
                others.push(literal);
                otherIndexes.push(index);
            }
        });

        if (others.length > 0 && original) {
            const resolved: readonly ts.ResolvedModuleWithFailedLookupLocations[] = original(others, containingFile, ...rest);
            resolved.forEach((resolution, index) => {
                results[otherIndexes[index]!] = resolution;
            });
        }

        workspace.markResolved(containingFile);

        return results as ts.ResolvedModuleWithFailedLookupLocations[];
    };
}

function overrideResolveModuleNames(
    original: ((moduleNames: string[], containingFile: string, ...rest: unknown[]) => Array<ts.ResolvedModuleFull | undefined>) | null,
    tsModule: typeof ts,
    workspace: PdmlWorkspace
) {
    return function (moduleNames: string[], containingFile: string, ...rest: unknown[]): Array<ts.ResolvedModuleFull | undefined> {
        const results: Array<ts.ResolvedModuleFull | undefined> = new Array(moduleNames.length);
        const others: string[] = [];
        const otherIndexes: number[] = [];

        moduleNames.forEach((name, index) => {
            const virtualPath: string | null = workspace.resolve(name, containingFile);
            if (virtualPath) {
                results[index] = makeResolution(tsModule, virtualPath).resolvedModule;
            } else {
                others.push(name);
                otherIndexes.push(index);
            }
        });

        if (others.length > 0 && original) {
            const resolved: Array<ts.ResolvedModuleFull | undefined> = original(others, containingFile, ...rest);
            resolved.forEach((resolution, index) => {
                results[otherIndexes[index]!] = resolution;
            });
        }

        workspace.markResolved(containingFile);

        return results;
    };
}

function overrideHasInvalidatedResolutions(
    original: ((filePath: ts.Path) => boolean) | null,
    workspace: PdmlWorkspace
) {
    return function (filePath: ts.Path): boolean {
        if (workspace.isInvalidated(filePath)) {
            return true;
        }

        if (original) {
            return original(filePath);
        }

        return false;
    };
}

function overrideFileExists(
    original: ((fileName: string) => boolean) | null,
    tsModule: typeof ts,
    workspace: PdmlWorkspace
) {
    return function (fileName: string): boolean {
        if (convention.isVirtualPath(fileName) && workspace.hasVirtualFile(fileName)) {
            return true;
        }

        if (original) {
            return original(fileName);
        }

        return tsModule.sys.fileExists(fileName);
    };
}

function overrideReadFile(
    original: ((fileName: string, encoding?: string) => string | undefined) | null,
    tsModule: typeof ts,
    workspace: PdmlWorkspace
) {
    return function (fileName: string, encoding?: string): string | undefined {
        const key: string | null = served(fileName, workspace);
        if (key) {
            return workspace.getText(key);
        }

        if (original) {
            return original(fileName, encoding);
        }

        return tsModule.sys.readFile(fileName, encoding);
    };
}

function overrideGetScriptSnapshot(
    original: (fileName: string) => ts.IScriptSnapshot | undefined,
    tsModule: typeof ts,
    workspace: PdmlWorkspace
) {
    return function (fileName: string): ts.IScriptSnapshot | undefined {
        const originalSnapshot: ts.IScriptSnapshot | undefined = original(fileName);

        const key: string | null = served(fileName, workspace);
        if (key) {
            return tsModule.ScriptSnapshot.fromString(workspace.getText(key));
        }

        return originalSnapshot;
    };
}

function overrideGetScriptVersion(
    original: (fileName: string) => string,
    workspace: PdmlWorkspace
) {
    return function (fileName: string): string {
        const key: string | null = served(fileName, workspace);
        if (key) {
            return workspace.getVersion(key);
        }

        return original(fileName);
    };
}

function overrideGetScriptKind(
    original: ((fileName: string) => ts.ScriptKind) | null,
    tsModule: typeof ts,
    workspace: PdmlWorkspace
) {
    return function (fileName: string): ts.ScriptKind {
        if (served(fileName, workspace)) {
            return tsModule.ScriptKind.TS;
        }

        if (original) {
            return original(fileName);
        }

        return tsModule.ScriptKind.Unknown;
    };
}

function overrideGetProjectVersion(
    original: (() => string) | null,
    workspace: PdmlWorkspace
) {
    return function (): string {
        const base: string = original ? original() : "";
        return `${base}|pdml${workspace.version}`;
    };
}

const PENDING_DIAGNOSTIC_CODES: Set<number> = new Set([
    2305,
    2307,
    2614,
    2694,
    2724
]);

export function decorateLanguageService(
    languageService: ts.LanguageService,
    workspace: PdmlWorkspace,
    onDispose: () => void
): ts.LanguageService {
    const proxy: ts.LanguageService = Object.create(null);

    for (const key of Object.keys(languageService) as Array<keyof ts.LanguageService>) {
        const member = languageService[key] as (...args: unknown[]) => unknown;
        (proxy as any)[key] = (...args: unknown[]) => member.apply(languageService, args);
    }

    proxy.getSemanticDiagnostics = (fileName: string) => {
        const diagnostics: ts.Diagnostic[] = languageService.getSemanticDiagnostics(fileName);

        if (!workspace.busy) {
            return diagnostics;
        }

        return diagnostics.filter((diagnostic) => !PENDING_DIAGNOSTIC_CODES.has(diagnostic.code));
    };

    proxy.getDefinitionAtPosition = (fileName: string, position: number) =>
        redirectDefinitions(languageService.getDefinitionAtPosition(fileName, position), workspace);

    proxy.getTypeDefinitionAtPosition = (fileName: string, position: number) =>
        redirectDefinitions(languageService.getTypeDefinitionAtPosition(fileName, position), workspace);

    proxy.getDefinitionAndBoundSpan = (fileName: string, position: number) => {
        const result: ts.DefinitionInfoAndBoundSpan | undefined = languageService.getDefinitionAndBoundSpan(fileName, position);
        if (!result) {
            return undefined;
        }

        return {
            ...result,
            definitions: redirectDefinitions(result.definitions, workspace)
        };
    };

    proxy.dispose = () => {
        onDispose();
        languageService.dispose();
    };

    return proxy;
}
