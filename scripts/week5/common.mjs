import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { setTimeout as sleep } from 'node:timers/promises';

export const root = fileURLToPath(new URL('../../', import.meta.url));
export const base = process.env.PULSE_API ?? 'http://127.0.0.1:5155/api';
const management = process.env.PULSE_RABBIT_API ?? 'http://127.0.0.1:15675/api';
const auth = Buffer.from(`${process.env.PULSE_RABBIT_USER ?? 'pulse'}:${process.env.PULSE_RABBIT_PASSWORD ?? 'local-pulse-demo'}`).toString('base64');

export async function request(path, { method = 'GET', body, expected = 200 } = {}) {
  const response = await fetch(`${base}${path}`, { method,
    headers: { 'content-type': 'application/json' },
    body: body === undefined ? undefined : JSON.stringify(body), signal: AbortSignal.timeout(30000) });
  if (response.status !== expected) throw new Error(`${method} ${path}: expected ${expected}, got ${response.status}: ${await response.text()}`);
  return response.status === 204 ? undefined : response.json();
}

export function compose(...args) {
  const result = spawnSync('docker', ['compose', '-f', 'compose.week5.yml', ...args], {
    cwd: root, encoding: 'utf8', timeout: 180000,
  });
  if (result.status !== 0) throw new Error(`docker compose ${args.join(' ')}: ${result.error ?? result.stderr}`);
  return result.stdout;
}

export async function rabbit(path, body) {
  const result = await fetch(`${management}${path}`, { method: body ? 'POST' : 'GET',
    headers: { authorization: `Basic ${auth}`, 'content-type': 'application/json' },
    body: body ? JSON.stringify(body) : undefined, signal: AbortSignal.timeout(10000) });
  if (!result.ok) throw new Error(`RabbitMQ ${path}: ${result.status}`);
  return result.json();
}

export async function waitFor(check, label, timeoutMs = 120000) {
  const start = performance.now();
  let last;
  while (performance.now() - start < timeoutMs) {
    try { last = await check(); if (last) return last; } catch (error) { last = error.message; }
    await sleep(500);
  }
  throw new Error(`Timed out waiting for ${label}; last result: ${JSON.stringify(last)}`);
}

export async function parallel(count, concurrency, task) {
  let next = 0;
  await Promise.all(Array.from({ length: Math.min(count, concurrency) }, async () => {
    while (next < count) { const index = next++; await task(index); }
  }));
}

export async function publish(queue, type, message, messageId = crypto.randomUUID()) {
  const result = await rabbit('/exchanges/%2F/amq.default/publish', {
    properties: { content_type: 'application/vnd.masstransit+json', delivery_mode: 2 },
    routing_key: queue,
    payload_encoding: 'string',
    payload: JSON.stringify({ messageId, messageType: [`urn:message:ITFPulse.Contracts.Messaging:${type}`], message }),
  });
  if (!result.routed) throw new Error(`Message was not routed to ${queue}`);
}

export async function waitForIdle() {
  // Management statistics are sampled; require repeated empty observations.
  let emptySamples = 0;
  await waitFor(async () => {
    const queues = (await rabbit('/queues/%2F')).filter(q => q.name.startsWith('itfpulse-'));
    const failed = queues.filter(q => /_(error|skipped)$/.test(q.name) && q.messages > 0);
    if (failed.length) throw new Error(`Failed messages: ${failed.map(q => q.name).join(', ')}`);
    const active = queues.filter(q => !/_(error|skipped)$/.test(q.name));
    emptySamples = active.length >= 3 && active.every(q => q.messages === 0) ? emptySamples + 1 : 0;
    return emptySamples >= 12;
  }, 'all queues to drain without faults');
}
