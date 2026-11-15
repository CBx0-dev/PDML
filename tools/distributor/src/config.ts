export interface NativeTarget {
    os: string;
    cpu: string;
    exe: string;
    libc?: string[];
}

export interface Generator {
    dll: string;
    description: string;
}

export const ORG: string = "@pdml";

export const CLI_PACKAGE: string = `${ORG}/cli`;

export const REPO_SLUG: string = process.env["GITHUB_REPOSITORY"] || "CBx0-dev/PDML";

export const NATIVE_TARGETS: Record<string, NativeTarget> = {
    "win-x64": {
        os: "win32",
        cpu: "x64",
        exe: "PDML.exe"
    },
    "win-arm64": {
        os: "win32",
        cpu: "arm64",
        exe: "PDML.exe"
    },
    "linux-x64": {
        os: "linux",
        cpu: "x64",
        exe: "PDML",
        libc: ["glibc"]
    },
    "linux-arm64": {
        os: "linux",
        cpu: "arm64",
        exe: "PDML",
        libc: ["glibc"]
    },
    "osx-x64": {
        os: "darwin",
        cpu: "x64",
        exe: "PDML"
    },
    "osx-arm64": {
        os: "darwin",
        cpu: "arm64",
        exe: "PDML"
    },
};

export const GENERATORS: Record<string, Generator> = {
    csharp: {
        dll: "PDML.Generator.CSharp.dll",
        description: "C# code generator for the PDML language."
    },
    typescript: {
        dll: "PDML.Generator.TypeScript.dll",
        description: "TypeScript code generator for the PDML language."
    },
};
