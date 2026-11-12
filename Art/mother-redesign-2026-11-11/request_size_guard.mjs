// Measure the actual serialized POST before the existing image client sends it.
// No credentials, request headers or image contents are written to the receipt.
import { appendFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';

// The first call reached the image client's 120-second response timeout.
// Keep the original client and grant retried calls enough time to finish.
const originalTimeout = AbortSignal.timeout.bind(AbortSignal);
AbortSignal.timeout = (milliseconds) => originalTimeout(milliseconds === 120000 ? 600000 : milliseconds);
const watchdog = setTimeout(() => {
  process.stderr.write('Image request exceeded the task runtime limit; no additional POST was sent.\n');
  process.exit(1);
}, 660000);
watchdog.unref();

const originalFetch = globalThis.fetch;
globalThis.fetch = async (input, init) => {
  const request = new Request(input, init);
  if (request.method === 'POST') {
    const bytes = (await request.clone().arrayBuffer()).byteLength;
    const receipt = {
      time_utc: new Date().toISOString(),
      endpoint: new URL(request.url).pathname,
      serialized_body_bytes: bytes,
      working_limit_bytes: 16000000,
      hard_limit_bytes: 20000000,
      allowed: bytes < 16000000,
    };
    appendFileSync(fileURLToPath(new URL('./request_receipts.jsonl', import.meta.url)), JSON.stringify(receipt) + '\n');
    if (!receipt.allowed) throw new Error(`POST body ${bytes} bytes exceeds the task working limit; split or compress inputs first.`);
    process.stdout.write(`POST size verified: ${bytes} bytes (limit < 20000000).\n`);
  }
  return originalFetch(request);
};
