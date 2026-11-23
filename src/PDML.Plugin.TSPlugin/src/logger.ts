export interface Logger {
    info(message: string): void;
}

export const silentLogger: Logger = {
    info(): void {
        return;
    }
};
