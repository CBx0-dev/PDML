import * as path from "node:path";
import * as os from "node:os";

declare const DEVELOPMENT: boolean;

export enum Rid {
    WinX64 = "win-x64",
    WinArm64 = "win-arm64",
    LinuxX64 = "linux-x64",
    LinuxArm64 = "linux-arm64",
    OsxX64 = "osx-x64",
    OsxArm64 = "osx-arm64",
}

export interface PlatformInformation {
    rid: Rid;
    extension: string;
    packageName: string;
}

export function resolveNative(): string {
    const platform: PlatformInformation = resolvePlatformInformation();

    if (DEVELOPMENT) {
        return path.join(import.meta.dirname, "..", "..", "PDML", "bin", "Debug", "net9.0", "PDML") + platform.extension;
    }

    try {
        return import.meta.resolve(platform.packageName);
    } catch {
        throw new Error(`Could not find '${platform.packageName}'. Please reinstall '@pdml/cli' with optional dependencies enabled.`);
    }
}

export function resolvePlatformInformation(): PlatformInformation {
    const platform = os.platform();
    const arch = os.arch();

    const targets: Record<string, Rid> = {
        "win32-x64": Rid.WinX64,
        "win32-arm64": Rid.WinArm64,
        "linux-x64": Rid.LinuxX64,
        "linux-arm64": Rid.LinuxArm64,
        "darwin-x64": Rid.OsxX64,
        "darwin-arm64": Rid.OsxArm64
    };

    const key: string = `${platform}-${arch}`;
    const rid: Rid | undefined = targets[key];

    if (!rid) {
        throw new Error(`Unsupported platform: ${key}`);
    }

    return {
        rid,
        packageName: `@pdml/compiler-${rid}/executable`,
        extension: platform === "win32" ? ".exe" : ""
    };

}