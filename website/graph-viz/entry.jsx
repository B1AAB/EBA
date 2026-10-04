// The landing page's live graph: a static EBA sample drawn with panorama.
// Built by build.mjs into static/graph-viz/; the page calls mount() at runtime.
import { StrictMode, useEffect } from "react";
import { createRoot } from "react-dom/client";
import { GraphViz, PanoramaProvider, buildGraphDoc, useScene } from "panorama";

// The landing page's look, passed to panorama as its starting defaults.
// Only what differs from panorama's own defaults is listed here.
const HOP_SHELLS_OPTIONS = {
  nodes: { labelBudget: 0 },
  edges: { scheduler: "height-ordered" },
  // Draws the graph right of centre, leaving the left for the page's overlay text.
  camera: { offsetX: 0.18 },
  effects: { background: "#000000" },
  // "auto" visibility draws only the outermost shell, so these are switched on.
  hops: { 0: { visible: "on", opacity: 0.25 }, 1: { visible: "on", opacity: 0.1 } },
};

const CONFIG = {
  storageKey: "eba-landing-graph",
  // Every visit starts from these defaults; a visitor's tweaks last until reload.
  persist: false,
  defaultRenderer: "hop-shells-3d",
  defaultRendererOptions: { "hop-shells-3d": HOP_SHELLS_OPTIONS },
  nodeStyles: {
    script: { color: "#3b82f6", shape: "sphere", icon: "wallet" },
    tx: { color: "#f59e0b", shape: "octahedron", icon: "arrow-left-right" },
    block: { color: "#8b5cf6", shape: "cube", icon: "box" },
  },
  formatValue: (sats) => `${(sats / 1e8).toLocaleString(undefined, { maximumFractionDigits: 8 })} BTC`,
};

const TYPES = {
  block: { label: "Block", key: "Height", key_type: "integer", caption: "Height" },
  tx: { label: "Tx", key: "Txid", key_type: "string", caption: "Txid" },
  script: { label: "Script", key: "SHA256Hash", key_type: "string", caption: "Address" },
};

// The sample is the whole graph: nothing to fetch, nothing expands.
const SCENE_OPTIONS = {
  fetcher: { expand: async () => ({ doc: null, hasMore: false }) },
  canExpand: () => false,
};

function propertyNames(items) {
  const byType = new Map();
  for (const item of items) {
    const names = byType.get(item.type) ?? new Set();
    for (const k of Object.keys(item.props ?? {})) names.add(k);
    byType.set(item.type, names);
  }
  return byType;
}

function schemaOf(doc) {
  return {
    node_types: [...propertyNames(doc.nodes)].map(([type, names]) => {
      const t = TYPES[type] ?? { label: type, key: "id", key_type: "string", caption: "id" };
      return { type, ...t, lookup: [t.key], properties: [...names].filter((n) => n !== t.key) };
    }),
    relationship_types: [...propertyNames(doc.edges)].map(([type, names]) => ({ type, properties: [...names] })),
    semantics: { edge_value: "Value", edge_height: "Height", node_height: "Height" },
    identities: {},
    limits: {
      expand_per_direction: 0,
      expand_max: 0,
      query_default: 0,
      query_max: 0,
      lookup_max: 0,
      pattern_max_hops: 0,
      max_response_elements: doc.nodes.length + doc.edges.length,
    },
    loaded_at: "",
  };
}

function Graph({ doc, roots, schema }) {
  const scene = useScene(SCENE_OPTIONS, schema);
  useEffect(() => {
    if (scene.getState().revision === 0) scene.getState().seed(doc, roots);
  }, [scene, doc, roots]);
  return <GraphViz scene={scene} schema={schema} />;
}

/** Draws the graph at dataUrl into el; resolves to a function that removes it. */
export async function mount(el, { dataUrl }) {
  const res = await fetch(dataUrl);
  if (!res.ok) throw new Error(`${dataUrl}: HTTP ${res.status}`);
  const payload = await res.json();
  const doc = buildGraphDoc(payload);
  if (!doc) throw new Error(`${dataUrl}: no graph data`);
  const roots = Array.isArray(payload.roots) ? payload.roots : [];
  // The graph sits in the page, so the plain wheel scrolls the page and only
  // ctrl+wheel (or a trackpad pinch, which sets ctrlKey) reaches the graph to
  // zoom. Stopped while capturing, before the graph's own listeners see it;
  // without preventDefault the browser still scrolls. Panels keep scrolling.
  const onWheel = (e) => {
    if (!e.ctrlKey && e.target instanceof HTMLCanvasElement) e.stopPropagation();
  };
  el.addEventListener("wheel", onWheel, { capture: true });
  const root = createRoot(el);
  root.render(
    <StrictMode>
      <PanoramaProvider config={CONFIG}>
        <Graph doc={doc} roots={roots} schema={schemaOf(doc)} />
      </PanoramaProvider>
    </StrictMode>,
  );
  return () => {
    root.unmount();
    el.removeEventListener("wheel", onWheel, { capture: true });
  };
}
