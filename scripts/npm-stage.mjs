#!/usr/bin/env node
// Stages the npm packages of PDML into <out>/<dir> so they can be `npm pack`ed and published.
//
//   node scripts/npm-stage.mjs list
//   node scripts/npm-stage.mjs native      --rid <rid> --version <x.y.z> --publish-dir <dir> --out <dir>
//   node scripts/npm-stage.mjs generator   --name <csharp|typescript> --version <x.y.z> --dll-dir <dir> --out <dir>
//   node scripts/npm-stage.mjs cli         --version <x.y.z> --out <dir>
//   node scripts/npm-stage.mjs placeholder --out <dir>      (one-time bootstrap, see npm-bootstrap.sh)
//
// All packages are released in lockstep, so every package gets the same version.

import * as fs from "node:fs";
import * as path from "node:path";
import {parseArgs} from "node:util";
import {fileURLToPath} from "node:url";

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), "..");
const cliDir = path.join(root, "src", "PDML.Cli");

const SCOPE = "@pdml";
const CLI_PACKAGE = `${SCOPE}/cli`;

// Keep in sync with Rid / resolvePlatformInformation in src/PDML.Cli/src/lib/native.ts
const NATIVE_TARGETS = {
    "win-x64": {os: "win32", cpu: "x64", exe: "PDML.exe"},
    "win-arm64": {os: "win32", cpu: "arm64", exe: "PDML.exe"},
    "linux-x64": {os: "linux", cpu: "x64", libc: ["glibc"], exe: "PDML"},
    "linux-arm64": {os: "linux", cpu: "arm64", libc: ["glibc"], exe: "PDML"},
    "osx-x64": {os: "darwin", cpu: "x64", exe: "PDML"},
    "osx-arm64": {os: "darwin", cpu: "arm64", exe: "PDML"},
};

const GENERATORS = {
    csharp: {dll: "PDML.Generator.CSharp.dll", description: "C# code generator for the PDML language."},
    typescript: {dll: "PDML.Generator.TypeScript.dll", description: "TypeScript code generator for the PDML language."},
};

const nativeName = (rid) => `${SCOPE}/compiler-${rid}`;
const generatorName = (key) => `${SCOPE}/generator-${key}`;
const dirName = (name) => name.slice(SCOPE.length + 1);

// Publish order: natives and generators first, the CLI (which depends on the natives) last.
function allPackageNames() {
    return [
        ...Object.keys(NATIVE_TARGETS).map(nativeName),
        ...Object.keys(GENERATORS).map(generatorName),
        CLI_PACKAGE,
    ];
}

function fail(message) {
    console.error(`npm-stage: ${message}`);
    process.exit(1);
}

function requireOption(values, key) {
    if (!values[key]) {
        fail(`missing --${key}`);
    }
    return values[key];
}

function requireVersion(values) {
    const version = requireOption(values, "version");
    if (!/^\d+\.\d+\.\d+$/.test(version)) {
        fail(`version must look like 1.2.3, got '${version}'`);
    }
    return version;
}

// The repository url must match the GitHub repository exactly (case-sensitive) for trusted publishing/provenance.
const repoSlug = process.env.GITHUB_REPOSITORY || "CBx0-dev/PDML";

function baseManifest(name, version, description) {
    return {
        name,
        version,
        description,
        author: "CBx0",
        license: "Apache-2.0",
        repository: {type: "git", url: `git+https://github.com/${repoSlug}.git`},
        bugs: {url: `https://github.com/${repoSlug}/issues`},
        homepage: `https://github.com/${repoSlug}#readme`,
        publishConfig: {access: "public"},
    };
}

function freshDir(out, name) {
    const dir = path.join(path.resolve(out), dirName(name));
    fs.rmSync(dir, {recursive: true, force: true});
    fs.mkdirSync(dir, {recursive: true});
    return dir;
}

function writeManifest(dir, manifest) {
    fs.writeFileSync(path.join(dir, "package.json"), JSON.stringify(manifest, null, 2) + "\n");
}

function copyLegal(dir, readmeText) {
    fs.copyFileSync(path.join(root, "LICENSE"), path.join(dir, "LICENSE"));
    if (readmeText) {
        fs.writeFileSync(path.join(dir, "README.md"), readmeText);
    } else {
        fs.copyFileSync(path.join(root, "README.md"), path.join(dir, "README.md"));
    }
}

function stageNative(values) {
    const rid = requireOption(values, "rid");
    const target = NATIVE_TARGETS[rid];
    if (!target) {
        fail(`unknown rid '${rid}', expected one of ${Object.keys(NATIVE_TARGETS).join(", ")}`);
    }

    const version = requireVersion(values);
    const publishDir = path.resolve(requireOption(values, "publish-dir"));
    const exePath = path.join(publishDir, target.exe);
    if (!fs.existsSync(exePath)) {
        fail(`'${target.exe}' not found in ${publishDir}`);
    }

    const name = nativeName(rid);
    const dir = freshDir(requireOption(values, "out"), name);
    fs.cpSync(publishDir, path.join(dir, "bin"), {recursive: true});

    if (target.os !== "win32") {
        fs.chmodSync(path.join(dir, "bin", target.exe), 0o755);
    }

    const manifest = {
        ...baseManifest(name, version, `Native PDML compiler for ${rid}.`),
        os: [target.os],
        cpu: [target.cpu],
        ...(target.libc ? {libc: target.libc} : {}),
        // Resolved by @pdml/cli through import.meta.resolve("<name>/executable")
        exports: {
            "./executable": `./bin/${target.exe}`,
            "./package.json": "./package.json",
        },
        files: ["bin/**", "README.md", "LICENSE"],
    };

    writeManifest(dir, manifest);
    copyLegal(dir, `# ${name}\n\nNative PDML compiler for \`${rid}\`.\n\nDo not install this package directly. ` +
        `It is installed automatically as an optional dependency of [\`${CLI_PACKAGE}\`](https://www.npmjs.com/package/${CLI_PACKAGE}).\n`);
    console.log(dir);
}

function stageGenerator(values) {
    const key = requireOption(values, "name");
    const generator = GENERATORS[key];
    if (!generator) {
        fail(`unknown generator '${key}', expected one of ${Object.keys(GENERATORS).join(", ")}`);
    }

    const version = requireVersion(values);
    const dllSource = path.join(path.resolve(requireOption(values, "dll-dir")), generator.dll);
    if (!fs.existsSync(dllSource)) {
        fail(`'${generator.dll}' not found at ${dllSource}`);
    }

    const name = generatorName(key);
    const dir = freshDir(requireOption(values, "out"), name);
    fs.mkdirSync(path.join(dir, "lib"));
    // Only the generator itself: PDML.Compiler and PDML.Generator.Abstractions are provided by the native CLI.
    fs.copyFileSync(dllSource, path.join(dir, "lib", generator.dll));

    const manifest = {
        ...baseManifest(name, version, generator.description),
        keywords: ["pdml", "generator", key],
        exports: {
            "./extension": `./lib/${generator.dll}`,
            "./package.json": "./package.json",
        },
        peerDependencies: {[CLI_PACKAGE]: version},
        files: ["lib/**", "README.md", "LICENSE"],
    };

    writeManifest(dir, manifest);
    copyLegal(dir, `# ${name}\n\n${generator.description}\n\nExtension for [\`${CLI_PACKAGE}\`](https://www.npmjs.com/package/${CLI_PACKAGE}). ` +
        `Install it next to the CLI and it is discovered automatically.\n`);
    console.log(dir);
}

function stageCli(values) {
    const version = requireVersion(values);
    const source = JSON.parse(fs.readFileSync(path.join(cliDir, "package.json"), "utf-8"));

    const distDir = path.join(cliDir, "dist");
    for (const file of ["bin.js", "lib.js", "lib.d.ts"]) {
        if (!fs.existsSync(path.join(distDir, file))) {
            fail(`dist/${file} is missing, build the package first (pnpm --filter ${CLI_PACKAGE} run build)`);
        }
    }

    const dir = freshDir(requireOption(values, "out"), CLI_PACKAGE);
    fs.cpSync(distDir, path.join(dir, "dist"), {recursive: true});

    const optionalDependencies = Object.fromEntries(
        Object.keys(NATIVE_TARGETS).map((rid) => [nativeName(rid), version])
    );

    const manifest = {
        ...baseManifest(source.name, version, source.description),
        keywords: source.keywords,
        type: source.type,
        main: source.main,
        types: source.types,
        bin: source.bin,
        engines: source.engines,
        files: source.files,
        optionalDependencies,
    };

    writeManifest(dir, manifest);
    copyLegal(dir);
    console.log(dir);
}

// Minimal 0.0.0 packages. npm only lets you bind a trusted publisher to a package that already exists.
function stagePlaceholders(values) {
    const out = requireOption(values, "out");
    for (const name of allPackageNames()) {
        const dir = freshDir(out, name);
        writeManifest(dir, {
            name,
            version: "0.0.0",
            description: "Placeholder, the real release is published from CI.",
            license: "Apache-2.0",
            repository: {type: "git", url: `git+https://github.com/${repoSlug}.git`},
            publishConfig: {access: "public"},
        });
        fs.writeFileSync(path.join(dir, "README.md"), `# ${name}\n\nPlaceholder.\n`);
        console.log(dir);
    }
}

const {positionals, values} = parseArgs({
    allowPositionals: true,
    options: {
        rid: {type: "string"},
        name: {type: "string"},
        version: {type: "string"},
        "publish-dir": {type: "string"},
        "dll-dir": {type: "string"},
        out: {type: "string"},
    },
});

switch (positionals[0]) {
    case "list":
        console.log(allPackageNames().join("\n"));
        break;
    case "native":
        stageNative(values);
        break;
    case "generator":
        stageGenerator(values);
        break;
    case "cli":
        stageCli(values);
        break;
    case "placeholder":
        stagePlaceholders(values);
        break;
    default:
        fail("expected one of: list, native, generator, cli, placeholder");
}
