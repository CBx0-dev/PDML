import * as fs from "node:fs";
import * as path from "node:path";

import type {CompileWorker} from "./worker/shared";
import * as convention from "./convention";
import type {Logger} from "./logger";
import {findModuleOnDisk, ModuleMapping, solveModuleMapping} from "./root";
import {type FileChange, FileSetWatcher} from "./watch";
import {CompileResult, Diagnostic, DiagnosticSeverity, IPCException, isIPCException} from "@pdml/cli";

export interface WorkspaceSettings {
    executable?: string;
    generator: string;
    dllPaths: string[];
}

export interface WorkspaceChange {
    invalidated: Set<string>;
}

export interface WorkspaceOptions {
    backend: CompileWorker;
    logger: Logger;
    caseSensitive: boolean;
    settings: () => WorkspaceSettings;
    onChange: (change: WorkspaceChange) => void;
    debounceMs?: number;
}

interface ModuleRecord {
    sourcePath: string;
    sourceNative: string;
    text: string;
    version: number;
    signature: string;
    specifiers: Map<string, string>;
}

function isFile(file: string): boolean {
    try {
        return fs.statSync(file).isFile();
    } catch {
        return false;
    }
}

export class PdmlWorkspace {
    private readonly entries: Map<string, string>;
    private records: Map<string, ModuleRecord>;
    private readonly importers: Map<string, Set<string>>;
    private readonly missing: Map<string, string>;
    private readonly invalidated: Set<string>;
    private readonly idleWaiters: Array<() => void>;
    private readonly watcher: FileSetWatcher;

    private readonly options: WorkspaceOptions;
    private versionCounter: number;
    private stateVersion: number;
    private timer: NodeJS.Timeout | null;
    private running: boolean;
    private rerun: boolean;
    private disposed: boolean;

    public constructor(options: WorkspaceOptions) {
        this.entries = new Map<string, string>();
        this.records = new Map<string, ModuleRecord>();
        this.importers = new Map<string, Set<string>>();
        this.missing = new Map<string, string>();
        this.invalidated = new Set<string>();
        this.idleWaiters = new Array<() => void>();
        this.watcher = new FileSetWatcher((changes: FileChange[]): void => this.onFilesChanged(changes));

        this.options = options;
        this.versionCounter = 0;
        this.stateVersion = 0;
        this.timer = null;
        this.running = false;
        this.rerun = false;
        this.disposed = false;
    }

    public get version(): number {
        return this.stateVersion;
    }

    public get busy(): boolean {
        return this.running || this.timer != null;
    }

    public canonical(file: string): string {
        return this.options.caseSensitive ? file : file.toLowerCase();
    }

    public resolve(specifier: string, containingFile: string): string | null {
        if (convention.isPdmlPath(specifier)) {
            return this.resolvePdmlImport(specifier, containingFile);
        }

        if (this.isGeneratedFile(containingFile)) {
            return this.resolveGeneratedImport(specifier, containingFile);
        }

        return null;
    }

    public hasVirtualFile(virtualPath: string): boolean {
        return this.records.has(this.canonical(virtualPath))
            || this.entries.has(this.canonical(convention.fromVirtualPath(virtualPath)));
    }

    public getText(virtualPath: string): string {
        const record: ModuleRecord | undefined = this.records.get(this.canonical(virtualPath));
        if (record) {
            return record.text;
        }

        return convention.PLACEHOLDER_TEXT;
    }

    public getVersion(virtualPath: string): string {
        const record: ModuleRecord | undefined = this.records.get(this.canonical(virtualPath));
        if (record) {
            return String(record.version);
        }

        return "0";
    }

    public sourceOf(virtualPath: string): string | null {
        const record: ModuleRecord | undefined = this.records.get(this.canonical(virtualPath));
        if (record) {
            return record.sourcePath;
        }

        const source: string = convention.fromVirtualPath(virtualPath);
        if (this.entries.has(this.canonical(source))) {
            return source;
        }

        return null;
    }

    public isInvalidated(importer: string): boolean {
        return this.invalidated.has(this.canonical(importer));
    }

    public markResolved(importer: string): void {
        this.invalidated.delete(this.canonical(importer));
    }

    public whenIdle(): Promise<void> {
        if (!this.busy) {
            return Promise.resolve();
        }

        return new Promise<void>((resolve) => this.idleWaiters.push(resolve));
    }

    public dispose(): void {
        this.disposed = true;
        if (this.timer) {
            clearTimeout(this.timer);
            this.timer = null;
        }

        this.watcher.dispose();
        this.options.backend.dispose();
        this.flushIdleWaiters();
    }

    private isGeneratedFile(file: string): boolean {
        let key: string = file;
        if (convention.isPdmlPath(file)) {
            key = convention.toVirtualPath(file);
        }

        return convention.isVirtualPath(key) && this.hasVirtualFile(key);
    }

    private resolvePdmlImport(specifier: string, containingFile: string): string | null {
        if (!specifier.startsWith(".") && !path.isAbsolute(specifier)) {
            return null;
        }

        const native: string = path.resolve(path.dirname(containingFile), specifier);
        const tsPath: string = convention.toTsPath(native);
        const key: string = this.canonical(tsPath);

        let importers: Set<string> | undefined = this.importers.get(key);
        if (!importers) {
            importers = new Set<string>();
            this.importers.set(key, importers);
        }

        importers.add(this.canonical(containingFile));

        if (!isFile(native)) {
            if (!this.missing.has(key)) {
                this.missing.set(key, native);
                this.syncWatcher();
            }

            return null;
        }

        this.missing.delete(key);
        if (!this.entries.has(key)) {
            this.entries.set(key, native);
            this.syncWatcher();
            this.schedule();
        }

        return convention.toVirtualPath(tsPath);
    }

    private resolveGeneratedImport(specifier: string, containingFile: string): string | null {
        const segments: string[] | null = convention.specifierToSegments(specifier);
        if (!segments) {
            return null;
        }

        let containingKey: string = containingFile;
        if (convention.isPdmlPath(containingFile)) {
            containingKey = convention.toVirtualPath(containingFile);
        }

        const record: ModuleRecord | undefined = this.records.get(this.canonical(containingKey));
        if (!record) {
            return null;
        }

        const source: string | undefined = record.specifiers.get(convention.segmentsKey(segments));
        if (!source) {
            return null;
        }

        return convention.toVirtualPath(source);
    }

    private schedule(): void {
        if (this.disposed) {
            return;
        }

        if (this.timer) {
            clearTimeout(this.timer);
        }

        const debounce: number = this.options.debounceMs ?? 40;
        this.timer = setTimeout(() => {
            this.timer = null;
            void this.compile();
        }, debounce);
        this.timer.unref();
    }

    private async compile(): Promise<void> {
        if (this.running) {
            this.rerun = true;
            return;
        }

        this.running = true;
        try {
            do {
                this.rerun = false;
                await this.compileOnce();
            } while (this.rerun && !this.disposed);
        } finally {
            this.running = false;
        }

        if (!this.disposed) {
            this.notify(new Set());
        }

        this.flushIdleWaiters();
    }

    private async compileOnce(): Promise<void> {
        const invalidated: Set<string> = this.pruneMissingEntries();

        const files: string[] = [...this.entries.values()];
        if (files.length == 0) {
            this.records = new Map();
            this.syncWatcher();
            if (invalidated.size > 0) {
                this.notify(invalidated);
            }

            return;
        }

        const settings: WorkspaceSettings = this.options.settings();
        const outcome: IPCException | CompileResult = await this.options.backend.compile({
            command: "compile",
            files,
            generator: settings.generator,
            dllPaths: [...settings.dllPaths],
            executable: settings.executable
        });

        if (this.disposed) {
            return;
        }

        this.apply(files, outcome);
        if (invalidated.size > 0) {
            this.notify(invalidated);
        }
    }

    private pruneMissingEntries(): Set<string> {
        const invalidated: Set<string> = new Set();

        for (const [key, native] of [...this.entries]) {
            if (isFile(native)) {
                continue;
            }

            this.entries.delete(key);
            this.records.delete(this.canonical(convention.toVirtualPath(convention.toTsPath(native))));
            this.collectImporters(key, invalidated);
        }

        for (const importer of invalidated) {
            this.invalidated.add(importer);
        }

        return invalidated;
    }

    private apply(files: string[], result: IPCException | CompileResult): void {
        const log: Logger = this.options.logger;

        if (isIPCException(result)) {
            log.info(`[pdml] compilation failed, keeping the previous declarations: ${result.message}`);
            return;
        }

        const names: string[] = Object.keys(result.outputFiles);
        if (names.length == 0) {
            log.info(`[pdml] compilation reported errors, keeping the previous declarations:\n${describe(result.diagnostics)}`);
            return;
        }

        const mapping: ModuleMapping | null = solveModuleMapping({
            entries: files,
            outputNames: names,
            findOnDisk: findModuleOnDisk
        });

        if (!mapping) {
            log.info(`[pdml] could not map the generated files back to their sources (entries: ${files.join(", ")}; outputs: ${names.join(", ")})`);
            return;
        }

        for (const name of mapping.unmapped) {
            log.info(`[pdml] generated file '${name}' has no known source file, ignoring it`);
        }

        const specifiers: Map<string, string> = new Map();
        for (const [key, native] of mapping.sources) {
            specifiers.set(key, convention.toTsPath(native));
        }
        const signature: string = JSON.stringify([...specifiers]);

        const next: Map<string, ModuleRecord> = new Map();
        let changed: boolean = false;

        for (const name of names) {
            const key: string = convention.segmentsKey(convention.outputNameToSegments(name));
            const native: string | undefined = mapping.sources.get(key);
            if (!native) {
                continue;
            }

            const sourcePath: string = convention.toTsPath(native);
            const virtualKey: string = this.canonical(convention.toVirtualPath(sourcePath));
            const text: string = result.outputFiles[name]!;
            const previous: ModuleRecord | undefined = this.records.get(virtualKey);

            const unchanged: boolean = previous != null
                && previous.text == text
                && previous.signature == signature;

            if (!unchanged) {
                changed = true;
            }

            next.set(virtualKey, {
                sourcePath,
                sourceNative: native,
                text,
                version: unchanged ? previous!.version : ++this.versionCounter,
                signature,
                specifiers
            });
        }

        for (const [virtualKey, record] of this.records) {
            if (!next.has(virtualKey) && this.entries.has(this.canonical(record.sourcePath))) {
                next.set(virtualKey, record);
            }
        }

        if (next.size != this.records.size) {
            changed = true;
        }

        this.records = next;
        this.syncWatcher();

        if (changed) {
            this.stateVersion++;
        }

        log.info(`[pdml] compiled ${names.length} module(s), common root '${mapping.root}'${changed ? "" : " (no changes)"}`);
    }

    private syncWatcher(): void {
        const files: Set<string> = new Set(this.entries.values());
        for (const record of this.records.values()) {
            files.add(record.sourceNative);
        }
        for (const native of this.missing.values()) {
            files.add(native);
        }

        this.watcher.setPaths(files);
    }

    private onFilesChanged(changes: FileChange[]): void {
        if (this.disposed) {
            return;
        }

        const invalidated: Set<string> = new Set();
        let recompile: boolean = false;

        for (const change of changes) {
            const tsPath: string = convention.toTsPath(change.path);
            const key: string = this.canonical(tsPath);

            switch (change.kind) {
                case "deleted":
                    if (this.entries.delete(key)) {
                        this.records.delete(this.canonical(convention.toVirtualPath(tsPath)));
                    }

                    this.collectImporters(key, invalidated);
                    recompile = true;
                    break;

                case "created":
                    this.missing.delete(key);
                    this.collectImporters(key, invalidated);
                    recompile = true;
                    break;

                case "changed":
                    recompile = true;
                    break;
            }
        }

        for (const importer of invalidated) {
            this.invalidated.add(importer);
        }

        this.stateVersion++;
        this.syncWatcher();
        this.notify(invalidated);

        if (recompile) {
            this.schedule();
        }
    }

    private collectImporters(key: string, into: Set<string>): void {
        const importers: Set<string> | undefined = this.importers.get(key);
        if (!importers) {
            return;
        }

        for (const importer of importers) {
            into.add(importer);
        }
    }

    private notify(invalidated: Set<string>): void {
        this.options.onChange({invalidated});
    }

    private flushIdleWaiters(): void {
        if (this.busy) {
            return;
        }

        for (const waiter of this.idleWaiters.splice(0)) {
            waiter();
        }
    }
}

function describe(diagnostics?: readonly Diagnostic[] | null): string {
    if (!diagnostics || diagnostics.length == 0) {
        return "  (no diagnostics)";
    }

    const filtered: string[] = [];
    for (const diagnostic of diagnostics) {
        if (diagnostic.severity >= DiagnosticSeverity.Error) {
            filtered.push(`  ${diagnostic.location.sourceFile}(${diagnostic.location.start.line},${diagnostic.location.start.column}): ${diagnostic.message}`);
            if (filtered.length >= 5) {
                break;
            }
        }
    }

    if (filtered.length == 0) {
        return "  (no diagnostics)";
    }

    return filtered.join("\n");
}
