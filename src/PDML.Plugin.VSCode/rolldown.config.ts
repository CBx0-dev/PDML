import { defineConfig } from "rolldown";

export default defineConfig({
    input: "src/main.ts",

    output: {
        file: "dist/main.js",
        format: "esm",
        exports: "named"
    },

    external: ["vscode"],
    platform: "node",
    treeshake: true
});
