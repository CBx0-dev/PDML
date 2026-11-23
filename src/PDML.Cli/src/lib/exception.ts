export interface IPCException {
    readonly name: string;
    readonly message: string;
    readonly innerException: IPCException | null;
}

export function isIPCException(value: any): value is IPCException {
    return (
        value != null &&
        typeof value == "object" &&
        typeof value.name == "string" &&
        typeof value.message == "string" &&
        (
            value.innerException == null ||
            isIPCException(value.innerException)
        )
    );
}