import * as fs from "node:fs";
import * as path from "node:path";
import * as config from "./config";
import * as utils from "./utils";

export interface StageNativeOptions {
    rid: string;
    version: string;
    publishDir: string;
    out: string;
}

export interface StageGeneratorOptions {
    name: string;
    version: string;
    dllDir: string;
    out: string;
}

export interface StageCliOptions {
    version: string;
    out: string;
}

export interface StagePackageOptions {
    name: string;
    version: string;
    out: string;
}

export interface StagePlaceholdersOptions {
    out: string;
}

export function stageNative(options: StageNativeOptions): string {
    const rid: string = options.rid;
    const target: config.NativeTarget | undefined = config.NATIVE_TARGETS[rid];
    if (!target) {
        throw new Error(`unknown rid '${rid}', expected one of ${Object.keys(config.NATIVE_TARGETS).join(", ")}`);
    }

    const version: string = utils.validateVersion(options.version);
    const publishDir: string = path.resolve(options.publishDir);
    const exePath: string = path.join(publishDir, target.exe);
    if (!fs.existsSync(exePath)) {
        throw new Error(`'${target.exe}' not found in ${publishDir}`);
    }

    const name: string = utils.getNativePackageName(rid);
    const dir: string = utils.freshDir(options.out, name);
    fs.cpSync(publishDir, path.join(dir, "bin"), {recursive: true});

    if (target.os !== "win32") {
        fs.chmodSync(path.join(dir, "bin", target.exe), 0o755);
    }

    const manifest: utils.PackageManifest = {
        ...utils.baseManifest(name, version, `Native PDML compiler for ${rid}.`),
        os: [target.os],
        cpu: [target.cpu],
        ...(target.libc ? {libc: target.libc} : {}),
        // Resolved by @pdml/cli through import.meta.resolve("<name>/executable")
        exports: {
            "./executable": `./bin/${target.exe}`,
            "./package.json": "./package.json"
        },
        files: ["bin/**", "README.md", "LICENSE"]
    };

    utils.writeManifest(dir, manifest);
    utils.copyLegal(dir);

    return dir;
}

export function stageGenerator(options: StageGeneratorOptions): string {
    const key: string = options.name;
    const generator: config.Generator | undefined = config.GENERATORS[key];
    if (!generator) {
        throw new Error(`unknown generator '${key}', expected one of ${Object.keys(config.GENERATORS).join(", ")}`);
    }

    const version: string = utils.validateVersion(options.version);
    const dllSource: string = path.join(path.resolve(options.dllDir), generator.dll);
    if (!fs.existsSync(dllSource)) {
        throw new Error(`'${generator.dll}' not found at ${dllSource}`);
    }

    const name: string = utils.getGeneratorPackageName(key);
    const dir: string = utils.freshDir(options.out, name);
    fs.mkdirSync(path.join(dir, "lib"));
    // Only the generator itself: PDML.Compiler and PDML.Generator.Abstractions are provided by the native CLI.
    fs.copyFileSync(dllSource, path.join(dir, "lib", generator.dll));

    const manifest: utils.PackageManifest = {
        ...utils.baseManifest(name, version, generator.description),
        keywords: ["pdml", "generator", key],
        exports: {
            "./extension": `./lib/${generator.dll}`,
            "./package.json": "./package.json"
        },
        peerDependencies: {[config.CLI_PACKAGE]: version},
        files: ["lib/**", "README.md", "LICENSE"]
    };

    utils.writeManifest(dir, manifest);
    utils.copyLegal(dir);

    return dir;
}

export function stageCli(options: StageCliOptions): string {
    const version: string = utils.validateVersion(options.version);
    const cliDir: string = utils.getCliDir();
    const source: utils.PackageManifest = JSON.parse(fs.readFileSync(path.join(cliDir, "package.json"), "utf-8"));

    const distDir: string = path.join(cliDir, "dist");
    for (const file of ["bin.js", "lib.js", "lib.d.ts"]) {
        if (!fs.existsSync(path.join(distDir, file))) {
            throw new Error(`dist/${file} is missing, build the package first (pnpm --filter ${config.CLI_PACKAGE} run build)`);
        }
    }

    const dir: string = utils.freshDir(options.out, config.CLI_PACKAGE);
    fs.cpSync(distDir, path.join(dir, "dist"), {recursive: true});

    const optionalDependencies: Record<string, string> = Object.fromEntries(
        Object.keys(config.NATIVE_TARGETS).map((rid) => [utils.getNativePackageName(rid), version])
    );

    const manifest: utils.PackageManifest = {
        ...utils.baseManifest(source.name, version, source.description),
        keywords: source.keywords,
        type: source.type,
        main: source.main,
        types: source.types,
        bin: source.bin,
        engines: source.engines,
        files: source.files,
        optionalDependencies
    };

    utils.writeManifest(dir, manifest);
    utils.copyLegal(dir);

    return dir;
}

export function stagePackage(options: StagePackageOptions): string {
    const key: string = options.name;
    const pkg: config.Package | undefined =
        config.PACKAGES[key] ||
        Object.values(config.PACKAGES).find(
            (p) => p.name === key || path.basename(p.dir).toLowerCase() === key.toLowerCase()
        );

    if (!pkg) {
        throw new Error(`unknown package '${key}', expected one of ${Object.keys(config.PACKAGES).join(", ")}`);
    }

    const version: string = utils.validateVersion(options.version);
    const rootDir: string = utils.findProjectRoot();
    const pkgDir: string = path.resolve(rootDir, pkg.dir);
    const manifestPath: string = path.join(pkgDir, "package.json");

    if (!fs.existsSync(manifestPath)) {
        throw new Error(`package.json not found at ${manifestPath}`);
    }

    const source: utils.PackageManifest = JSON.parse(fs.readFileSync(manifestPath, "utf-8"));
    const distDir: string = path.join(pkgDir, "dist");

    if (!fs.existsSync(distDir)) {
        throw new Error(`dist directory is missing in ${pkg.dir}, build the package first (pnpm --filter ${pkg.name} run build)`);
    }

    const dir: string = utils.freshDir(options.out, pkg.name);
    fs.cpSync(distDir, path.join(dir, "dist"), {recursive: true});

    const resolveWorkspaceDeps = (deps?: Record<string, string>): Record<string, string> | undefined => {
        if (!deps) {
            return undefined;
        }

        const resolved: Record<string, string> = {};
        for (const [dep, val] of Object.entries(deps)) {
            if (typeof val === "string" && val.startsWith("workspace:")) {
                resolved[dep] = version;
            } else {
                resolved[dep] = val;
            }
        }
        return resolved;
    };

    const manifest: utils.PackageManifest = {
        ...utils.baseManifest(source.name || pkg.name, version, source.description || pkg.description || ""),
        keywords: source.keywords,
        type: source.type,
        main: source.main,
        types: source.types,
        bin: source.bin,
        engines: source.engines,
        files: source.files,
        ...(source.dependencies ? {dependencies: resolveWorkspaceDeps(source.dependencies as Record<string, string>)} : {}),
        ...(source.peerDependencies ? {peerDependencies: resolveWorkspaceDeps(source.peerDependencies as Record<string, string>)} : {}),
        ...(source.optionalDependencies ? {optionalDependencies: resolveWorkspaceDeps(source.optionalDependencies as Record<string, string>)} : {})
    };

    utils.writeManifest(dir, manifest);
    utils.copyLegal(dir);

    return dir;
}

export function stagePlaceholders(options: StagePlaceholdersOptions): string[] {
    const out: string = options.out;
    const dirs: string[] = [];

    for (const name of utils.getAllPackageNames()) {
        const dir: string = utils.freshDir(out, name);
        utils.writeManifest(dir, {
            name,
            version: "0.0.0",
            description: "Placeholder, the real release is published from CI.",
            license: "Apache-2.0",
            repository: {type: "git", url: `git+https://github.com/${config.REPO_SLUG}.git`},
            publishConfig: {access: "public"}
        });
        fs.writeFileSync(path.join(dir, "README.md"), `# ${name}\n\nPlaceholder.\n`);
        dirs.push(dir);
    }

    return dirs;
}
