export const PDML_EXTENSION: string = ".pdml";
export const GENERATED_EXTENSION: string = ".ts";
export const VIRTUAL_SUFFIX: string = ".pdml.d.ts";
export const PLACEHOLDER_TEXT: string = "export {};\n";

export function lowerFirst(value: string): string {
    if (value.length == 0) {
        return value;
    }

    return value[0]!.toLowerCase() + value.slice(1);
}

export function segmentsOf(identity: string): string[] {
    return identity.split(/[:;\\/]+/).filter((segment) => segment.length > 0);
}

export function outputNameToSegments(localName: string): string[] {
    if (localName.endsWith(GENERATED_EXTENSION)) {
        return segmentsOf(localName.slice(0, -GENERATED_EXTENSION.length));
    }

    return segmentsOf(localName);
}

export function specifierToSegments(specifier: string): string[] | null {
    if (!specifier.startsWith("./") && !specifier.startsWith(".\\")) {
        return null;
    }

    return segmentsOf(specifier.slice(2));
}

export function segmentsKey(segments: string[] | readonly string[]): string {
    return segments.join("/");
}

export function toTsPath(nativePath: string): string {
    return nativePath.replace(/\\/g, "/");
}

export function isPdmlPath(file: string): boolean {
    return file.endsWith(PDML_EXTENSION);
}

export function isVirtualPath(file: string): boolean {
    return file.endsWith(VIRTUAL_SUFFIX);
}

export function toVirtualPath(pdmlTsPath: string): string {
    return pdmlTsPath + ".d.ts";
}

export function fromVirtualPath(virtualPath: string): string {
    return virtualPath.slice(0, -".d.ts".length);
}
