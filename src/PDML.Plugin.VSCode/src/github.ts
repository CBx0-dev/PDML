export const OWNER: string = "CBx0-dev";
export const REPO: string = "PDML";
export const ASSET_PREFIX: string = "pdml-lsp-";

export interface GitHubReleaseAsset {
    name: string;
    browser_download_url: string;
    digest?: string | null;
}

export interface GitHubRelease {
    tag_name: string;
    assets: GitHubReleaseAsset[];
}
