import * as fs from "node:fs";
import * as path from "node:path";

interface PackageJson {
    name: string;
    version: string;
    exports?: Record<string, unknown>;
    peerDependencies?: Record<string, string>;
    dependencies?: Record<string, string>;
}

export interface Extension {
    name: string;
    version: string;
    packageDir: string;
    extensionDir: string;
}

function readPackageJson(dir: string): PackageJson | null {
    try {
        const content: string = fs.readFileSync(path.join(dir, "package.json"), "utf-8");
        return JSON.parse(content);
    } catch {
        return null;
    }
}

function* traversePackages(nodeModuleDir: string): Generator<string> {
    let entries: fs.Dirent[];

    try {
        entries = fs.readdirSync(nodeModuleDir, {
            withFileTypes: true
        });
    } catch {
        return;
    }

    for (const entry of entries) {
        const entryPath: string = path.join(nodeModuleDir, entry.name);

        if (entry.name[0] == "@" && entry.isDirectory()) {
            let scopedEntries: fs.Dirent[] = [];

            try {
                scopedEntries = fs.readdirSync(entryPath, {
                    withFileTypes: true
                });
            } catch {
                continue;
            }

            for (const scopedEntry of scopedEntries) {
                if (
                    scopedEntry.isDirectory() ||
                    scopedEntry.isSymbolicLink()
                ) {
                    yield path.join(entryPath, scopedEntry.name);
                }
            }
        } else if (
            entry.isDirectory() ||
            entry.isSymbolicLink()
        ) {
            yield entryPath;
        }
    }
}

function resolveExtensionDirectory(
    packageDir: string,
    packageJson: PackageJson
): string | null {
    const exportEntry: unknown = packageJson.exports?.["./extension"];

    let relativePath: string | undefined;

    if (typeof exportEntry == "string") {
        relativePath = exportEntry;
    } else if (typeof exportEntry == "object") {
        const conditions: Record<string, unknown> = exportEntry as Record<string, unknown>;
        const candidate: unknown = conditions["node"] || conditions["default"];

        if (typeof candidate == "string") {
            relativePath = candidate;
        }
    }

    if (!relativePath) {
        return null;
    }

    if (
        path.isAbsolute(relativePath) ||
        relativePath.includes("\\") ||
        relativePath.split("/").includes("..")
    ) {
        return null;
    }

    const dllPath: string = path.resolve(packageDir, relativePath);


    try {
        const realPackageDir: string = fs.realpathSync(packageDir);
        const realDllPath: string = fs.realpathSync(dllPath);
        const relative: string = path.relative(realPackageDir, realDllPath);

        if (
            !relative ||
            relative == ".." ||
            relative.startsWith(`..${path.sep}`) ||
            path.isAbsolute(relative) ||
            !fs.statSync(relative).isFile()
        ) {
            return null;
        }

        return path.dirname(realDllPath);
    } catch {
        return null;
    }
}

function discoverNodeModulesFolder(sourceDir: string): string | null {
    let currentDir: string = path.resolve(sourceDir);
    let nodeModulesDir: string | null = null;
    while (true) {
        if (path.basename(currentDir) == "node_modules") {
            nodeModulesDir = currentDir;
            break;
        }

        const parentDir: string = path.dirname(currentDir);
        if (currentDir == parentDir) {
            break;
        }

        currentDir = parentDir;
    }

    return nodeModulesDir;
}

export function discoverExtensions(sourceDir: string): Extension[] {
    const nodeModulesDir: string | null = discoverNodeModulesFolder(sourceDir);
    if (!nodeModulesDir) {
        return [];
    }

    const results: Extension[] = [];
    const seen: Set<string> = new Set();

    for (const packageDir of traversePackages(nodeModulesDir)) {
        let realPackageDir: string;

        try {
            realPackageDir = fs.realpathSync(packageDir);
        } catch {
            continue;
        }

        if (seen.has(realPackageDir)) {
            continue;
        }

        seen.add(realPackageDir);

        const packageJson: PackageJson | null = readPackageJson(realPackageDir);
        if (!packageJson) {
            continue;
        }

        if (
            (!packageJson.dependencies || !("@pdml/compiler" in packageJson.dependencies)) &&
            (!packageJson.peerDependencies || !("@pdml/compiler" in packageJson.peerDependencies))
        ) {
            continue;
        }

        const extensionDir: string | null = resolveExtensionDirectory(realPackageDir, packageJson);
        if (!extensionDir) {
            continue;
        }

        results.push({
            name: packageJson.name,
            version: packageJson.version,
            packageDir: realPackageDir,
            extensionDir
        });
    }

    return results;
}