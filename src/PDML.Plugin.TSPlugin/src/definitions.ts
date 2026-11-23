import * as fs from "node:fs";

import type * as ts from "typescript/lib/tsserverlibrary";

import * as convention from "./convention";
import type {PdmlWorkspace} from "./workspace";

function escapeRegExp(value: string): string {
    return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

export function redirectDefinition<T extends ts.DefinitionInfo>(definition: T, workspace: PdmlWorkspace): T {
    let key: string = definition.fileName;
    if (convention.isPdmlPath(key)) {
        key = convention.toVirtualPath(key);
    }

    if (!convention.isVirtualPath(key) || !workspace.hasVirtualFile(key)) {
        return definition;
    }

    const source: string | null = workspace.sourceOf(key);
    if (!source) {
        return definition;
    }

    let start: number = 0;
    let length: number = 0;

    const name: string = definition.name;
    if (name) {
        try {
            const match: RegExpExecArray | null = new RegExp(`\\b${escapeRegExp(name)}\\b`, "i")
                .exec(fs.readFileSync(source, "utf-8"));

            if (match) {
                start = match.index;
                length = match[0].length;
            }
        } catch {
        }
    }

    return {
        ...definition,
        fileName: source,
        textSpan: {start, length},
        contextSpan: undefined
    };
}

export function redirectDefinitions<T extends ts.DefinitionInfo>(
    definitions: readonly T[] | undefined,
    workspace: PdmlWorkspace
): T[] | undefined {
    if (!definitions) {
        return undefined;
    }

    return definitions.map((definition) => redirectDefinition(definition, workspace));
}
