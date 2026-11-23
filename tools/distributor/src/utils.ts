import * as fs from "node:fs";
import * as path from "node:path";
import * as config from "./config";

export interface PackageManifest {
    name: string;
    version: string;
    description: string;
    author?: string;
    license?: string;
    repository?: {
        type: string;
        url: string;
    };
    bugs?: {
        url: string;
    };
    homepage?: string;
    publishConfig?: {
        access: string;
    };
    os?: string[];
    cpu?: string[];
    libc?: string[];
    exports?: Record<string, string>;
    files?: string[];
    keywords?: string[];
    type?: string;
    main?: string;
    types?: string;
    bin?: Record<string, string> | string;
    engines?: Record<string, string>;
    peerDependencies?: Record<string, string>;
    optionalDependencies?: Record<string, string>;
    [key: string]: unknown;
}

export function getAllPackageNames(): string[] {
    return [
        ...Object.keys(config.NATIVE_TARGETS).map(getNativePackageName),
        ...Object.keys(config.GENERATORS).map(getGeneratorPackageName),
        config.CLI_PACKAGE,
        ...Object.values(config.PACKAGES).map((pkg) => pkg.name)
    ];
}

export function getNativePackageName(rid: string): string {
    return `${config.ORG}/compiler-${rid}`;
}

export function getGeneratorPackageName(lang: string): string {
    return `${config.ORG}/generator-${lang}`;
}

export function getDirName(name: string): string {
    return name.slice(config.ORG.length + 1);
}

export function findProjectRoot(startDir: string = import.meta.dirname): string {
    let current: string = path.resolve(startDir);
    while (true) {
        if (fs.existsSync(path.join(current, "pnpm-workspace.yaml")) && fs.existsSync(path.join(current, "LICENSE"))) {
            return current;
        }
        const parent: string = path.dirname(current);
        if (parent == current) {
            return path.resolve(startDir, "..", "..", "..");
        }
        current = parent;
    }
}

export function getCliDir(rootDir: string = findProjectRoot()): string {
    return path.join(rootDir, "src", "PDML.Cli");
}

export function validateVersion(version: string): string {
    if (!/^\d+\.\d+\.\d+$/.test(version)) {
        throw new Error(`version must look like 1.2.3, got '${version}'`);
    }
    return version;
}

export function baseManifest(name: string, version: string, description: string): PackageManifest {
    return {
        name,
        version,
        description,
        author: "CBx0",
        license: "Apache-2.0",
        repository: {
            type: "git",
            url: `git+https://github.com/${config.REPO_SLUG}.git`
        },
        bugs: {
            url: `https://github.com/${config.REPO_SLUG}/issues`
        },
        homepage: `https://github.com/${config.REPO_SLUG}#readme`,
        publishConfig: {
            access: "public"
        }
    };
}

export function freshDir(out: string, name: string): string {
    const dir: string = path.join(path.resolve(out), getDirName(name));
    fs.rmSync(dir, {recursive: true, force: true});
    fs.mkdirSync(dir, {recursive: true});
    return dir;
}

export function writeManifest(dir: string, manifest: PackageManifest): void {
    fs.writeFileSync(path.join(dir, "package.json"), JSON.stringify(manifest, null, 2) + "\n");
}

export function copyLegal(dir: string, rootDir: string = findProjectRoot()): void {
    fs.copyFileSync(path.join(rootDir, "LICENSE"), path.join(dir, "LICENSE"));
    fs.copyFileSync(path.join(rootDir, "README.md"), path.join(dir, "README.md"));
}