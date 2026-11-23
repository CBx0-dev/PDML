import * as fs from "node:fs";
import * as path from "node:path";

export type FileChangeKind = "created" | "deleted" | "changed";

export interface FileChange {
    readonly path: string;
    readonly kind: FileChangeKind;
}

function signatureOf(file: string): string | null {
    try {
        const stat: fs.Stats = fs.statSync(file);
        return `${stat.mtimeMs}:${stat.size}`;
    } catch {
        return null;
    }
}

function nearestExistingDirectory(directory: string): string {
    let current: string = directory;
    while (!fs.existsSync(current)) {
        const parent: string = path.dirname(current);
        if (parent == current) {
            break;
        }

        current = parent;
    }

    return current;
}

export class FileSetWatcher {
    private readonly signatures: Map<string, string | null> = new Map();
    private readonly directoryWatchers: Map<string, fs.FSWatcher> = new Map();
    private readonly onChange: (changes: FileChange[]) => void;
    private readonly debounceMs: number;
    private timer: NodeJS.Timeout | null = null;
    private disposed: boolean = false;

    public constructor(
        onChange: (changes: FileChange[]) => void,
        debounceMs: number = 50
    ) {
        this.onChange = onChange;
        this.debounceMs = debounceMs;
    }

    public setPaths(paths: Iterable<string>): void {
        if (this.disposed) {
            return;
        }

        const wanted: Set<string> = new Set(paths);

        for (const file of [...this.signatures.keys()]) {
            if (!wanted.has(file)) {
                this.signatures.delete(file);
            }
        }

        for (const file of wanted) {
            if (!this.signatures.has(file)) {
                this.signatures.set(file, signatureOf(file));
            }
        }

        this.syncDirectoryWatchers();
    }

    public check(): void {
        if (this.disposed) {
            return;
        }

        const changes: FileChange[] = [];

        for (const [file, previous] of this.signatures) {
            const current: string | null = signatureOf(file);
            if (current == previous) {
                continue;
            }

            this.signatures.set(file, current);

            if (previous == null) {
                changes.push({path: file, kind: "created"});
            } else if (current == null) {
                changes.push({path: file, kind: "deleted"});
            } else {
                changes.push({path: file, kind: "changed"});
            }
        }

        this.syncDirectoryWatchers();

        if (changes.length > 0) {
            this.onChange(changes);
        }
    }

    public dispose(): void {
        this.disposed = true;
        if (this.timer) {
            clearTimeout(this.timer);
            this.timer = null;
        }

        for (const watcher of this.directoryWatchers.values()) {
            watcher.close();
        }

        this.directoryWatchers.clear();
        this.signatures.clear();
    }

    private schedule(): void {
        if (this.disposed) {
            return;
        }

        if (this.timer) {
            clearTimeout(this.timer);
        }

        this.timer = setTimeout(() => {
            this.timer = null;
            this.check();
        }, this.debounceMs);
        this.timer.unref();
    }

    private syncDirectoryWatchers(): void {
        const wanted: Set<string> = new Set();
        for (const file of this.signatures.keys()) {
            wanted.add(nearestExistingDirectory(path.dirname(file)));
        }

        for (const [directory, watcher] of [...this.directoryWatchers]) {
            if (!wanted.has(directory)) {
                watcher.close();
                this.directoryWatchers.delete(directory);
            }
        }

        for (const directory of wanted) {
            if (this.directoryWatchers.has(directory)) {
                continue;
            }

            try {
                const watcher: fs.FSWatcher = fs.watch(directory, {persistent: false}, () => this.schedule());
                watcher.on("error", () => {
                    watcher.close();
                    this.directoryWatchers.delete(directory);
                });

                this.directoryWatchers.set(directory, watcher);
            } catch {
            }
        }
    }
}
