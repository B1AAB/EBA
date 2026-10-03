#!/usr/bin/env node
// Builds the landing page's graph widget (entry.jsx) into static/graph-viz/.
//
// panorama, the graph library, is private: the docs CI cannot install it, so
// this runs locally against a checkout and its output is committed. The
// build tools come from that checkout's node_modules (run `npm install`
// there first); only the obfuscator, an EBA concern, is the website's own.
//
//   cd website
//   PANORAMA_DIR=../../panorama node graph-viz/build.mjs
//
// Nothing readable of panorama ships:
// - no source maps;
// - its GLSL shaders lose their comments and layout before bundling;
// - third-party libraries are split into vendor-* chunks, and every other
//   chunk (panorama and entry.jsx only) is obfuscated, which also encodes its
//   strings, shaders included;
// - chunk names are bare hashes, so file names don't mirror panorama's modules.
// Vendor chunks stay as their libraries publish them, with their licences in
// THIRD_PARTY_LICENSES.txt. Code that runs in a browser can always be
// reverse-engineered with effort; this makes that effort substantial.
import { execFileSync } from "node:child_process";
import { createHash } from "node:crypto";
import { existsSync, readdirSync, readFileSync, writeFileSync } from "node:fs";
import { createRequire } from "node:module";
import { dirname, join, resolve, sep } from "node:path";
import { fileURLToPath } from "node:url";
import JavaScriptObfuscator from "javascript-obfuscator";

const here = dirname(fileURLToPath(import.meta.url));
const outDir = resolve(here, "../static/graph-viz");
// File names are content-hashed so browsers never run a stale build; the
// page learns the current names from this generated module.
const manifestPath = resolve(here, "../src/data/graphViz.js");
const SCOPE = ".eba-graph";

const fail = (msg) => {
  console.error(`graph-viz: ${msg}`);
  process.exit(1);
};

if (!process.env.PANORAMA_DIR) fail("set PANORAMA_DIR to a local panorama checkout.");
const panoramaDir = resolve(process.env.PANORAMA_DIR);
if (!existsSync(join(panoramaDir, "node_modules"))) fail(`${panoramaDir} has no node_modules; run \`npm install\` there.`);

const req = createRequire(join(panoramaDir, "package.json"));
const { build } = req("vite");
const react = req("@vitejs/plugin-react").default;
const { compile, optimize } = req("@tailwindcss/node");
const { Scanner } = req("@tailwindcss/oxide");
const postcss = req("postcss");

// The panorama commit this build came from, so it can be reproduced.
const git = (...args) => execFileSync("git", ["-C", panoramaDir, ...args], { encoding: "utf8" }).trim();
const panorama = { commit: git("rev-parse", "HEAD"), dirty: git("status", "--porcelain").length > 0 };
if (panorama.dirty) console.warn("graph-viz: warning: panorama has uncommitted changes; this build cannot be reproduced from its commit.");

// ------------------------------------------------------------------ GLSL

/** Strips comments and layout from GLSL; preprocessor lines keep their own line. */
function minifyGlsl(text) {
  const lines = text
    .replace(/\/\*[\s\S]*?\*\//g, "")
    .replace(/\/\/[^\n]*/g, "")
    .split("\n")
    .map((l) => l.trim().replace(/\s+/g, " "))
    .filter(Boolean);
  let out = "";
  for (const line of lines) {
    if (line.startsWith("#")) out += (out && !out.endsWith("\n") ? "\n" : "") + line + "\n";
    else out += (out && !out.endsWith("\n") ? " " : "") + line;
  }
  // An interpolation next to this part may hold a preprocessor line.
  return (/^\s*\n/.test(text) ? "\n" : "") + out + (/\n\s*$/.test(text) ? "\n" : "");
}

/** Minifies every `/* glsl *\/` template literal; ${...} interpolations are kept as written. */
function minifyGlslTemplates(code) {
  const tag = /\/\*\s*glsl\s*\*\/\s*`/g;
  let out = "";
  let last = 0;
  for (let m; (m = tag.exec(code)); ) {
    let i = m.index + m[0].length;
    out += code.slice(last, i);
    let part = "";
    for (;;) {
      const c = code[i];
      if (c === undefined) throw new Error("unterminated glsl template");
      if (c === "\\") {
        part += code.slice(i, i + 2);
        i += 2;
      } else if (c === "`") {
        out += minifyGlsl(part) + "`";
        i++;
        break;
      } else if (c === "$" && code[i + 1] === "{") {
        let depth = 0;
        let j = i + 1;
        do {
          if (code[j] === "{") depth++;
          else if (code[j] === "}") depth--;
          j++;
        } while (depth > 0);
        out += minifyGlsl(part) + code.slice(i, j);
        part = "";
        i = j;
      } else {
        part += c;
        i++;
      }
    }
    last = tag.lastIndex = i;
  }
  return out + code.slice(last);
}

const panoramaSrc = join(panoramaDir, "src") + sep;
const glslPlugin = {
  name: "eba-glsl-minify",
  enforce: "pre",
  transform(code, id) {
    if (!resolve(id.split("?")[0]).startsWith(panoramaSrc) || !code.includes("glsl")) return null;
    return { code: minifyGlslTemplates(code), map: null };
  },
};

// ------------------------------------------------------- vendor & licences

const packageOf = (id) => id.replaceAll("\\", "/").match(/\/node_modules\/((?:@[^/]+\/)?[^/]+)\//)?.[1] ?? null;

// Libraries are grouped by when they load: the UI's with the page, the 3D
// renderer's and the icon set's on demand. The rest are the 2D renderer's,
// which loads only if a visitor switches to it. A new eager dependency of
// panorama belongs in "ui", or the 2D libraries start loading with the page.
const VENDOR_GROUPS = [
  { name: "vendor-react", test: /^(react|react-dom|scheduler)$/ },
  { name: "vendor-ui", test: /^(zustand|lucide-react|@radix-ui\/.*)$/ },
  { name: "vendor-three", test: /^(three|camera-controls|postprocessing)$/ },
  { name: "vendor-icons", test: /^lucide$/ },
  { name: "vendor-2d", test: /./ },
].map(({ name, test }) => ({ name, test: (id) => test.test(packageOf(id) ?? "") }));

const author = (meta) => (typeof meta.author === "string" ? meta.author : meta.author?.name);

/** Each bundled package's licence, as its own LICENSE file states it. */
function thirdPartyLicenses(moduleIds) {
  const packages = new Map();
  for (const id of moduleIds) {
    const pkg = packageOf(id);
    if (!pkg || packages.has(pkg)) continue;
    const norm = id.replaceAll("\\", "/");
    const dir = norm.slice(0, norm.lastIndexOf(`/node_modules/${pkg}/`) + `/node_modules/${pkg}`.length);
    const meta = JSON.parse(readFileSync(join(dir, "package.json"), "utf8"));
    const file = readdirSync(dir).find((f) => /^(licen[cs]e|copying)(\.|$)/i.test(f));
    packages.set(pkg, { meta, text: file ? readFileSync(join(dir, file), "utf8").trim() : null });
  }
  const sections = [...packages.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([name, { meta, text }]) => {
      const head = `${name}@${meta.version} (${meta.license ?? "see below"})`;
      return `${head}\n${"-".repeat(head.length)}\n${text ?? `No licence file shipped. package.json declares ${meta.license}${author(meta) ? `, author ${author(meta)}` : ""}.`}\n`;
    });
  return `Third-party software bundled in the EBA graph widget.\n\n${sections.join("\n")}`;
}

// ------------------------------------------------------------ obfuscation

const OBFUSCATION = {
  compact: true,
  // Fixed so an unchanged input rebuilds to the same files.
  seed: 0x5eed,
  identifierNamesGenerator: "mangled-shuffled",
  stringArray: true,
  stringArrayEncoding: ["base64"],
  stringArrayThreshold: 1,
  stringArrayRotate: true,
  stringArrayShuffle: true,
  stringArrayIndexShift: true,
  stringArrayWrappersCount: 2,
  stringArrayWrappersType: "function",
  // Off: they slow the render loop or break module exports.
  controlFlowFlattening: false,
  deadCodeInjection: false,
  renameGlobals: false,
  selfDefending: false,
  debugProtection: false,
  transformObjectKeys: true,
  sourceMap: false,
  target: "browser",
  reservedStrings: ["!~\{", "\.js$"],
};

const obfuscate = (code) => JavaScriptObfuscator.obfuscate(code, OBFUSCATION).getObfuscatedCode();

const licenseIds = new Set();
// Runs in renderChunk, before file names are hashed, so a name changes
// whenever the shipped code does. Import paths stay literal: at this point
// they hold the bundler's hash placeholders (!~{001}~), which it rewrites later.
const obfuscatePlugin = {
  name: "eba-obfuscate",
  renderChunk(code, chunk) {
    for (const id of chunk.moduleIds) licenseIds.add(id);
    const vendor = chunk.moduleIds.filter(packageOf);
    if (vendor.length === chunk.moduleIds.length && vendor.length > 0) return null;
    if (vendor.length > 0)
      fail(`chunk ${chunk.fileName} mixes panorama and third-party code: ${[...new Set(vendor.map(packageOf))].join(", ")}`);
    return { code: obfuscate(code), map: null };
  },
};

// ----------------------------------------------------------------- build

const result = await build({
  configFile: false,
  root: here,
  base: "./",
  logLevel: "warn",
  plugins: [glslPlugin, react(), obfuscatePlugin],
  resolve: {
    alias: { panorama: join(panoramaDir, "src/index.ts") },
    dedupe: ["react", "react-dom"],
  },
  worker: {
    format: "iife",
    plugins: () => [glslPlugin, obfuscatePlugin],
    rolldownOptions: { output: { entryFileNames: "assets/[hash].js" } },
  },
  build: {
    outDir,
    emptyOutDir: true,
    sourcemap: false,
    modulePreload: false,
    target: "es2022",
    chunkSizeWarningLimit: 1024,
    rolldownOptions: {
      input: { "graph-viz": join(here, "entry.jsx") },
      preserveEntrySignatures: "exports-only",
      output: {
        format: "es",
        entryFileNames: "[name]-[hash].js",
        // Vendor chunks keep their library's name; panorama's are bare hashes.
        chunkFileNames: (chunk) => (chunk.name.startsWith("vendor-") ? "chunks/[name]-[hash].js" : "chunks/[hash].js"),
        assetFileNames: "assets/[hash][extname]",
        comments: { legal: false },
        codeSplitting: { groups: VENDOR_GROUPS },
      },
    },
  },
});

// panorama styles itself with Tailwind classes. The CSS is generated here,
// then scoped under SCOPE and taken out of cascade layers: the site's own
// unlayered CSS would otherwise beat every layered rule.
const source = `@import "tailwindcss" source(none);\n@source "${join(panoramaDir, "src").replaceAll("\\", "/")}";\n`;
const compiler = await compile(source, { base: panoramaDir, onDependency: () => {} });
const scanner = new Scanner({ sources: compiler.sources });
const generated = optimize(compiler.build(scanner.scan()), { minify: false }).code;

const scopeSelector = (sel) => (/^(:root|:host|html|body)$/.test(sel.trim()) ? SCOPE : `${SCOPE} ${sel.trim()}`);
const scoped = postcss([
  {
    postcssPlugin: "eba-graph-scope",
    Once(root) {
      root.walkAtRules("layer", (at) => (at.nodes ? at.replaceWith(at.nodes) : at.remove()));
      root.walkRules((rule) => {
        if (rule.parent?.type === "atrule" && /keyframes$/.test(rule.parent.name)) return;
        rule.selectors = [...new Set(rule.selectors.map(scopeSelector))];
      });
    },
  },
]).process(generated, { from: undefined }).css;

const css = optimize(scoped, { minify: true }).code;
const style = `graph-viz-${createHash("sha256").update(css).digest("base64url").slice(0, 8)}.css`;
writeFileSync(join(outDir, style), css);

writeFileSync(join(outDir, "THIRD_PARTY_LICENSES.txt"), thirdPartyLicenses(licenseIds));

const script = [result].flat().flatMap((r) => r.output).find((o) => o.type === "chunk" && o.isEntry).fileName;
writeFileSync(manifestPath, `// Generated by graph-viz/build.mjs.\nexport default ${JSON.stringify({ script, style, panorama }, null, 2)};\n`);
console.log(`graph-viz: built ${script} and ${style} into ${outDir} from panorama ${panorama.commit.slice(0, 7)}${panorama.dirty ? " (dirty)" : ""}`);
