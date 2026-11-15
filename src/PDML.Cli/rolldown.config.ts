import * as process from "node:process";

import {defineConfig} from "rolldown";
import {dts} from "rolldown-plugin-dts";

const isDev: boolean = process.argv.includes("--dev");

export default defineConfig([
    {
        input: "src/bin/main.ts",
        output: {
            file: "dist/bin.js",
            format: "esm",
            exports: "named",

        },
        transform: {
            define: {
                DEVELOPMENT: JSON.stringify(isDev)
            }
        },

        platform: "node"
    },
    {
        input: "src/lib/lib.ts",
        plugins: [dts()],
        output: {
            dir: "dist",
            format: "esm",
            exports: "named",
        },

        transform: {
            define: {
                DEVELOPMENT: JSON.stringify(isDev)
            }
        },

        platform: "node"
    }
]);