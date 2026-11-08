export interface Coordinate {
    readonly position: number;
    readonly line: number;
    readonly column: number;
}

export interface Location {
    readonly sourceFile: string;
    readonly start: Coordinate;
    readonly end: Coordinate;
}

export enum DiagnosticSeverity {
    Info,
    Warning,
    Error,
}

export interface Diagnostic {
    readonly code: number;
    readonly severity: DiagnosticSeverity;
    readonly location: Location;
    readonly additionalLocations: Location[];
    readonly message: string;

}
