#!/usr/bin/env node
// Local OpenAI-compatible shim for OpenRouter's Image API.
//
// dsh-image-gen's `openai-compat` provider sends reference-image edits as
// multipart POST /v1/images/edits. OpenRouter has no such endpoint: its image
// API is POST /api/v1/images (JSON) with reference images in `input_references`.
// This server accepts the OpenAI-style edit request, converts it, and forwards
// to OpenRouter. Text-to-image is passed through unchanged.
//
//   node tools/openrouter_relay/server.mjs
//
// Env:
//   PORT                 listen port (default 18787, loopback only)
//   OPENROUTER_API_KEY   fallback key when the client sends no Authorization
//   RELAY_MODEL          fallback model when the request has none
//   RELAY_TIMEOUT_MS     upstream timeout (default 300000)
//   OPENROUTER_IMAGES_URL upstream endpoint (default https://openrouter.ai/api/v1/images)

import http from "node:http";

const PORT = Number(process.env.PORT || 18787);
const UPSTREAM =
  process.env.OPENROUTER_IMAGES_URL || "https://openrouter.ai/api/v1/images";
const DEFAULT_MODEL =
  process.env.RELAY_MODEL || "bytedance-seed/seedream-5-5";
const TIMEOUT_MS = Number(process.env.RELAY_TIMEOUT_MS || 300000);
const MAX_BODY = 40 * 1024 * 1024;

const FORWARD_FIELDS = new Set([
  "model",
  "prompt",
  "n",
  "resolution",
  "aspect_ratio",
  "size",
  "quality",
  "output_format",
  "frame_count",
  "frame_duration_ms",
  "markup",
  "background",
  "output_compression",
  "seed",
  "response_format",
  "provider",
  "user",
]);

const QUALITY_VALUES = new Set(["auto", "low", "medium", "high", "xhigh", "max"]);

function log(line) {
  const stamp = new Date().toISOString().slice(11, 19);
  console.log(`[${stamp}] ${line}`);
}

function sendJson(res, status, value) {
  const text = JSON.stringify(value);
  res.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "content-length": Buffer.byteLength(text),
  });
  res.end(text);
}

function readBody(req) {
  return new Promise((resolve, reject) => {
    const chunks = [];
    let size = 0;
    req.on("data", (chunk) => {
      size += chunk.length;
      if (size > MAX_BODY) {
        reject(new Error(`request body exceeds ${MAX_BODY} bytes`));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => resolve(Buffer.concat(chunks)));
    req.on("error", reject);
  });
}

function keyFrom(req) {
  const header = req.headers.authorization;
  if (typeof header === "string" && header.startsWith("Bearer ")) {
    const value = header.slice(7).trim();
    if (value.length > 0) return value;
  }
  return process.env.OPENROUTER_API_KEY?.trim() || "";
}

function sniffMime(buf) {
  if (buf.length >= 8 && buf.subarray(0, 8).equals(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]))) {
    return "image/png";
  }
  if (buf.length >= 3 && buf[0] === 0xff && buf[1] === 0xd8 && buf[2] === 0xff) {
    return "image/jpeg";
  }
  if (buf.length >= 12 && buf.toString("ascii", 0, 4) === "RIFF" && buf.toString("ascii", 8, 12) === "WEBP") {
    return "image/webp";
  }
  if (buf.length >= 6 && buf.toString("ascii", 0, 3) === "GIF") {
    return "image/gif";
  }
  return "image/png";
}

function toDataUrl(buf, mime) {
  return `data:${mime || sniffMime(buf)};base64,${buf.toString("base64")}`;
}

function gcd(a, b) {
  while (b) [a, b] = [b, a % b];
  return a;
}

const KNOWN_RATIOS = new Set([
  "1:1",
  "1:2",
  "2:1",
  "2:3",
  "3:2",
  "3:4",
  "4:3",
  "4:5",
  "5:4",
  "9:16",
  "16:9",
  "9:20",
  "20:9",
  "9:21",
  "21:9",
]);

// The plugin sends OpenAI pixel sizes such as 1024x1536. OpenRouter prefers
// aspect_ratio/resolution tiers and only some providers accept explicit pixels,
// so translate a reducible size into aspect_ratio and keep tier strings as-is.
// Exception: OpenAI gpt-image-2.5/2.6 accepts explicit custom WIDTHxHEIGHT and
// rejects a simultaneous resolution/aspect_ratio, so pass it through unchanged.
function sizeFields(size, model = "") {
  if (typeof size !== "string" || size.trim().length === 0) return {};
  const value = size.trim();
  if (/^(512|768|1K|1.5K|2K|4K)$/i.test(value)) {
    return { resolution: value.toUpperCase() };
  }
  // TEMP DIAGNOSTIC: disable explicit OpenAI size passthrough; test old aspect_ratio-only behavior.
  // if (/^openai\/gpt-image-2(?:[.-]|$)/i.test(model) && /^\d+x\d+$/i.test(value)) {
  //   return { size: value };
  // }
  const match = value.match(/^(\d+)x(\d+)$/i);
  if (match) {
    const width = Number(match[1]);
    const height = Number(match[2]);
    if (width > 0 && height > 0) {
      const divisor = gcd(width, height);
      const ratio = `${width / divisor}:${height / divisor}`;
      if (KNOWN_RATIOS.has(ratio)) {
        const resolution = width * height >= 1500000 ? "2K" : "1K";
        return { aspect_ratio: ratio, resolution };
      }
    }
    return { size: value };
  }
  return {};
}

function normalizeReference(value) {
  // value may be a File/Blob, a data URL, a bare base64 string, or an object.
  if (value == null) return null;
  if (typeof value === "string") {
    if (value.startsWith("data:")) {
      return { type: "image_url", image_url: { url: value } };
    }
    if (/^https?:\/\//i.test(value)) {
      return { type: "image_url", image_url: { url: value } };
    }
    return {
      type: "image_url",
      image_url: { url: `data:image/png;base64,${value}` },
    };
  }
  if (typeof value === "object" && typeof value.image_url === "string") {
    return { type: "image_url", image_url: { url: value.image_url } };
  }
  if (
    typeof value === "object" &&
    value.image_url &&
    typeof value.image_url.url === "string"
  ) {
    return { type: "image_url", image_url: { url: value.image_url.url } };
  }
  return null;
}

async function blobToReference(blob) {
  const buf = Buffer.from(await blob.arrayBuffer());
  if (buf.length === 0) return null;
  return toDataUrl(buf, blob.type || sniffMime(buf));
}

function pickPayload(source, refs) {
  const payload = {};
  for (const [key, value] of Object.entries(source || {})) {
    if (FORWARD_FIELDS.has(key) && value !== undefined && value !== null && value !== "") {
      payload[key] = value;
    }
  }
  if (payload.quality !== undefined && !QUALITY_VALUES.has(payload.quality)) {
    // The plugin's OpenAI-compatible vocabulary ("standard", "hd") is not
    // OpenRouter's enum ("auto/low/medium/high/xhigh/max").
    delete payload.quality;
  }
  if (refs.length > 0) payload.input_references = refs;
  if (!payload.model) payload.model = DEFAULT_MODEL;
  if (!payload.prompt) payload.prompt = "";
  if (payload.n === undefined) payload.n = 1;
  // Inline bytes keep the plugin from depending on a 24-hour signed URL.
  payload.response_format = "b64_json";
  return payload;
}

async function forward(payload, key, res, label) {
  if (!key) {
    sendJson(res, 401, { error: { message: "relay: no OpenRouter API key" } });
    return;
  }
  const started = Date.now();
  let upstream;
  try {
    upstream = await fetch(UPSTREAM, {
      method: "POST",
      headers: {
        authorization: `Bearer ${key}`,
        "content-type": "application/json",
      },
      body: JSON.stringify(payload),
      signal: AbortSignal.timeout(TIMEOUT_MS),
    });
  } catch (error) {
    log(`${label} upstream error: ${error?.message || error}`);
    sendJson(res, 502, { error: { message: `relay upstream error: ${String(error?.message || error)}` } });
    return;
  }
  const text = await upstream.text();
  const elapsed = ((Date.now() - started) / 1000).toFixed(1);
  log(
    `${label} model=${payload.model} refs=${(payload.input_references || []).length} status=${upstream.status} ${elapsed}s`,
  );
  res.writeHead(upstream.status, {
    "content-type": upstream.headers.get("content-type") || "application/json",
    "content-length": Buffer.byteLength(text),
  });
  res.end(text);
}

async function handleGenerations(req, res) {
  const raw = await readBody(req);
  let body;
  try {
    body = JSON.parse(raw.toString("utf8") || "{}");
  } catch {
    sendJson(res, 400, { error: { message: "relay: invalid JSON body" } });
    return;
  }
  const refs = Array.isArray(body.input_references)
    ? body.input_references
    : [];
  const payload = pickPayload(body, refs);
  if (typeof payload.size === "string") {
    const mapped = sizeFields(payload.size, payload.model);
    delete payload.size;
    Object.assign(payload, mapped);
  }
  await forward(payload, keyFrom(req), res, "generations");
}

async function refsFromMultipart(req) {
  const raw = await readBody(req);
  const form = await new Response(raw, {
    headers: { "content-type": req.headers["content-type"] || "" },
  }).formData();

  const fields = {};
  for (const [key, value] of form.entries()) {
    if (typeof value === "string") {
      fields[key] = value;
    }
  }

  const refs = [];
  for (const key of ["image", "image[]", "images", "image_url", "reference_image"]) {
    for (const value of form.getAll(key)) {
      if (typeof value === "string") {
        const ref = normalizeReference(value);
        if (ref) refs.push(ref);
      } else {
        const url = await blobToReference(value);
        if (url) refs.push({ type: "image_url", image_url: { url } });
      }
    }
  }

  // formReferenceImages sends one JSON array of bare base64 strings.
  const packed = form.get("reference_images");
  if (typeof packed === "string" && packed.trim().startsWith("[")) {
    try {
      for (const entry of JSON.parse(packed)) {
        const ref = normalizeReference(entry);
        if (ref) refs.push(ref);
      }
    } catch {
      /* ignore malformed packed references */
    }
  }

  return { fields, refs };
}

function refsFromJson(body) {
  const refs = [];
  const candidates = [];
  if (Array.isArray(body.input_references)) candidates.push(...body.input_references);
  if (Array.isArray(body.images)) candidates.push(...body.images);
  if (Array.isArray(body.image)) candidates.push(...body.image);
  else if (typeof body.image === "string") candidates.push(body.image);
  for (const candidate of candidates) {
    const ref = normalizeReference(candidate);
    if (ref) refs.push(ref);
  }
  return refs;
}

async function handleEdits(req, res) {
  const contentType = String(req.headers["content-type"] || "");
  let source;
  let refs;

  if (contentType.includes("application/json")) {
    const raw = await readBody(req);
    let body;
    try {
      body = JSON.parse(raw.toString("utf8") || "{}");
    } catch {
      sendJson(res, 400, { error: { message: "relay: invalid JSON body" } });
      return;
    }
    source = body;
    refs = refsFromJson(body);
  } else {
    const parsed = await refsFromMultipart(req);
    source = parsed.fields;
    refs = parsed.refs;
    const size = sizeFields(source.size, source.model);
    Object.assign(source, size);
    delete source.size;
  }

  if (refs.length === 0) {
    sendJson(res, 400, {
      error: {
        message:
          "relay: no reference image found (expected multipart image/image[] or JSON input_references)",
      },
    });
    return;
  }

  const payload = pickPayload(source, refs);
  await forward(payload, keyFrom(req), res, "edits");
}

const server = http.createServer(async (req, res) => {
  const url = new URL(req.url || "/", `http://127.0.0.1:${PORT}`);
  try {
    if (req.method === "GET" && url.pathname === "/health") {
      sendJson(res, 200, {
        ok: true,
        upstream: UPSTREAM,
        model: DEFAULT_MODEL,
        hasEnvKey: Boolean(process.env.OPENROUTER_API_KEY),
      });
      return;
    }
    if (req.method === "POST" && url.pathname === "/v1/images/generations") {
      await handleGenerations(req, res);
      return;
    }
    if (req.method === "POST" && url.pathname === "/v1/images/edits") {
      await handleEdits(req, res);
      return;
    }
    sendJson(res, 404, { error: { message: `relay: no route for ${req.method} ${url.pathname}` } });
  } catch (error) {
    log(`handler error: ${error?.stack || error}`);
    if (!res.headersSent) {
      sendJson(res, 500, { error: { message: `relay error: ${String(error?.message || error)}` } });
    } else {
      res.end();
    }
  }
});

server.listen(PORT, "127.0.0.1", () => {
  log(`listening on http://127.0.0.1:${PORT} -> ${UPSTREAM}`);
});
