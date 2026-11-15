export interface IPCException {
    readonly name: string;
    readonly message: string;
    readonly innerException: IPCException | null;
}

export function isIPCException(value: any): value is IPCException {
    return (
        typeof value == "object" &&
        typeof value.name == "string" &&
        typeof value.message == "string" &&
        (
            typeof value.innerException == null ||
            isIPCException(value.innerException)
        )
    );
}