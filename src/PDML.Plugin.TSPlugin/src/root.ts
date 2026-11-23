import * as fs from "node:fs";
import * as path from "node:path";

import * as convention from "./convention";

export interface PathApi {
    dirname(value: string): string;
}

export interface ModuleMapping {
    readonly root: string;
    readonly sources: Map<string, string>;
    readonly unmapped: string[];
}

export interface SolveOptions {
    readonly entries: string[];
    readonly outputNames: string[];
    readonly findOnDisk?: (root: string, segments: string[]) => string | null;
    readonly pathApi?: PathApi;
}

function splitPath(value: string): string[] {
    return value.split(/[\\/]+/).filter((segment) => segment.length > 0);
}

export function expectedSegments(root: string, file: string): string[] | null {
    const withoutExtension: string = file.endsWith(convention.PDML_EXTENSION)
        ? file.slice(0, -convention.PDML_EXTENSION.length)
        : file;

    if (!file.startsWith(root) || root.length > withoutExtension.length) {
        return null;
    }

    return splitPath(withoutExtension.slice(root.length)).map(convention.lowerFirst);
}

export function solveModuleMapping(options: SolveOptions): ModuleMapping | null {
    const {entries, outputNames} = options;

    if (entries.length == 0 || outputNames.length == 0) {
        return null;
    }

    const outputs: Map<string, string> = new Map();
    for (const name of outputNames) {
        outputs.set(convention.segmentsKey(convention.outputNameToSegments(name)), name);
    }

    const pathApi: PathApi = options.pathApi ?? path;
    let candidate: string = pathApi.dirname(entries[0]!);
    while (true) {
        const sources: Map<string, string> | null = tryRoot(candidate, entries, outputs);
        if (sources) {
            return complete(candidate, sources, outputs, options.findOnDisk);
        }

        const parent: string = pathApi.dirname(candidate);
        if (parent == candidate) {
            return null;
        }

        candidate = parent;
    }
}

function tryRoot(
    root: string,
    entries: string[],
    outputs: Map<string, string>
): Map<string, string> | null {
    const sources: Map<string, string> = new Map();

    for (const entry of entries) {
        const segments: string[] | null = expectedSegments(root, entry);
        if (!segments || segments.length == 0) {
            return null;
        }

        const key: string = convention.segmentsKey(segments);
        if (!outputs.has(key)) {
            return null;
        }

        sources.set(key, entry);
    }

    return sources;
}

function complete(
    root: string,
    sources: Map<string, string>,
    outputs: Map<string, string>,
    findOnDisk?: SolveOptions["findOnDisk"]
): ModuleMapping {
    const unmapped: string[] = [];

    for (const [key, name] of outputs) {
        if (sources.has(key)) {
            continue;
        }

        let found: string | null = null;
        if (findOnDisk) {
            found = findOnDisk(root, key.split("/"));
        }

        if (found) {
            sources.set(key, found);
        } else {
            unmapped.push(name);
        }
    }

    return {root, sources, unmapped};
}

export function findModuleOnDisk(root: string, segments: string[] | readonly string[]): string | null {
    let directory: string = root;

    for (let index: number = 0; index < segments.length; index++) {
        const isLast: boolean = index == segments.length - 1;

        let children: fs.Dirent[];
        try {
            children = fs.readdirSync(directory, {withFileTypes: true});
        } catch {
            return null;
        }

        const match: string | undefined = children
            .filter((child) => isLast
                ? child.name.endsWith(convention.PDML_EXTENSION) && !child.isDirectory()
                && convention.lowerFirst(child.name.slice(0, -convention.PDML_EXTENSION.length)) == segments[index]
                : (child.isDirectory() || child.isSymbolicLink()) && convention.lowerFirst(child.name) == segments[index]
            )
            .map((child) => child.name)
            .sort()[0];

        if (!match) {
            return null;
        }

        directory = path.join(directory, match);
    }

    return directory;
}
