import * as fs from "fs/promises";
import * as crypto from "crypto";
import * as os from "os";
import * as path from "path";

import * as vscode from "vscode";

import * as github from "./github.js";
import { table } from "console";

interface TargetInfo {
    rid: string;
    extension: string;
}

interface LastReleaseFile {
    tag: string;
    path: string;
}

export async function ensureLatestVersion(context: vscode.ExtensionContext): Promise<string> {
    const {rid, extension} = getTarget();
    const assetName: string = `${github.ASSET_PREFIX}-${rid}${extension}`;

    const storage: string = context.globalStorageUri.fsPath;
    const markerPath: string = path.join(storage, `${github.ASSET_PREFIX}-last-release.json`);
    
    let previous: LastReleaseFile | undefined;

    try {
        const content: string = await fs.readFile(markerPath, "utf8");
        previous = JSON.parse(content) as LastReleaseFile;
        await fs.access(previous.path);
    } catch {
        previous = undefined;
    }

    try {
        const response: Response = await fetch(
            `https://api.github.com/repos/${github.OWNER}/${github.REPO}/releases/latest`,
            {
                headers: {
                    "Accept": "application/vnd.github+json",
                    "User-Agent": `${github.REPO}-vscode-extension`
                },
                signal: AbortSignal.timeout(15000)
            }
        );

        if (!response.ok) {
            throw new Error(`GitHub API returned HTTP ${response.status}`);
        }

        const release: github.GitHubRelease = await response.json() as github.GitHubRelease;
        const asset: github.GitHubReleaseAsset | undefined = release.assets.find(a => a.name == assetName);
        if (!asset) {
            throw new Error(`Release ${release.tag_name} has no asset named ${assetName}`);
        }

        const versionDir: string = path.join(storage, release.tag_name);
        await fs.mkdir(versionDir, {recursive: true});
        
        const executable: string = path.join(versionDir, assetName);

        try {
            await fs.access(executable);
        } catch {
            const download: Response = await fetch(asset.browser_download_url, {
                    headers: {
                    "Accept": "application/vnd.github+json",
                    "User-Agent": `${github.REPO}-vscode-extension`
                },
                signal: AbortSignal.timeout(120000)
            });

            if (!download.ok) {
                throw new Error(`Asset download failed: HTTP ${download.status}`);
            }

            const bytes: Buffer = Buffer.from(await download.arrayBuffer());
            if (asset.digest) {
                const [algorithm, expected] = asset.digest.split(":");
                if (algorithm != "sha256") {
                    throw new Error(`Unsupported asset digest: ${algorithm}`);
                }

                const actual: string = crypto.createHash("sha256").update(bytes).digest("hex");
                if (actual != expected) {
                    throw new Error("Downloaded LSP failed SHA-256 verification");
                }
            }

            const temporary = `${executable}.download`;
            await fs.writeFile(temporary, bytes);

            if (process.platform != "win32") {
                await fs.chmod(temporary, 0o755);
            }

            await fs.rename(temporary, executable);
        }

        await fs.writeFile(markerPath, JSON.stringify({
            tag: release.tag_name,
            path: executable
        } satisfies LastReleaseFile));

        return executable;
    } catch (error) {
        if (previous) {
            vscode.window.showWarningMessage(
                `Couldn't check for the latest language server; using cached ${previous.tag}.`
            );
            
            return previous.path;
        }

        throw new Error(`Unable to download the language server from GitHub: ${String(error)}`);
    }
}


function getTarget(): TargetInfo {
    const platform = os.platform();
    const arch = os.arch();

    const targets: Record<string, string> = {
        "win32-x64": "win-x64",
        "win32-a>rm64": "win-arm64",
        "linux-x64": "linux-x64",
        "linux-arm64": "linux-arm64",
        "darwin-x64": "osx-x64",
        "darwin-arm64": "osx-arm64"
    };

    const key = `${platform}-${arch}`;
    const rid = targets[key];

    if (!rid) {
        throw new Error(`Unsupported LSP platform: ${key}`);
    }

    return {
        rid,
        extension: platform === "win32" ? ".exe" : ""
    };
}