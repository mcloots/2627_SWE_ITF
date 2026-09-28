// Node 22+, no npm dependencies. Run against the isolated compose.week5.yml stack.
import { mkdir, writeFile } from 'node:fs/promises';
import { cpus, totalmem, platform } from 'node:os';
import { resolve } from 'node:path';
import { request, compose, rabbit, waitFor, parallel, waitForIdle, root } from './common.mjs';

const options = Object.fromEntries(process.argv.slice(2).map(arg => arg.replace(/^--/, '').split('=')));
const followers = Number(options.followers ?? 10000);
const posts = Number(options.posts ?? 100);
const concurrency = Number(options.concurrency ?? 16);
const workers = Number(options.workers ?? 1);
for (const [key, value, max] of [['followers', followers, 100000], ['posts', posts, 1000], ['concurrency', concurrency, 128], ['workers', workers, 16]])
  if (!Number.isInteger(value) || value < 1 || value > max) throw new Error(`${key} must be 1–${max}`);

await request('/health');
await waitForIdle();
const authorId = crypto.randomUUID();
let firstFollower;
console.log(`Seeding ${followers} followers for fresh author ${authorId}.`);
await parallel(followers, concurrency, async i => {
  const follower = crypto.randomUUID();
  if (i === 0) firstFollower = follower;
  await request(`/authors/${authorId}/followers/${follower}`, { method: 'PUT', body: {}, expected: 204 });
});

let restartNeeded = true;
try {
  compose('stop', 'worker');
  const latencies = [];
  const postIds = [];
  const acceptStart = performance.now();
  await parallel(posts, concurrency, async i => {
    const start = performance.now();
    const post = await request('/posts', { method: 'POST', body: { authorId, content: `Week 5 load run ${authorId}: post ${i}` }, expected: 201 });
    latencies.push(performance.now() - start);
    postIds.push(post.postId);
  });
  const acceptanceSeconds = (performance.now() - acceptStart) / 1000;
  const expectedBatches = posts * Math.ceil(followers / 500);
  console.log(`Accepted ${posts} posts with delivery workers stopped. Waiting for ${expectedBatches} queued batches.`);
  await waitFor(async () => (await rabbit('/queues/%2F/itfpulse-deliver-feed-v1')).messages_ready >= expectedBatches,
    'planned backlog', 300000);
  const before = await request(`/demo/fanout/${authorId}`);
  if (before.posts !== posts || before.followers !== followers || before.feedEntries !== 0)
    throw new Error(`Unexpected backlog state: ${JSON.stringify(before)}`);

  const start = performance.now();
  compose('up', '-d', '--no-deps', '--scale', `worker=${workers}`, 'worker');
  restartNeeded = false;
  const expectedEntries = followers * posts;
  let lastLog = 0;
  await waitFor(async () => {
    const progress = await request(`/demo/fanout/${authorId}`);
    if (performance.now() - lastLog > 10000) {
      console.log(`Delivered ${progress.feedEntries}/${expectedEntries} feed references.`);
      lastLog = performance.now();
    }
    if (progress.feedEntries > expectedEntries) throw new Error('Too many feed entries');
    return progress.feedEntries === expectedEntries;
  }, 'complete fan-out', 600000);
  const drainSecondsIncludingWorkerStart = (performance.now() - start) / 1000;
  await waitForIdle();
  const sample = await request(`/feeds/${firstFollower}?limit=100`);
  if (sample.items.length !== Math.min(posts, 100) || sample.items.some(item => !postIds.includes(item.postId)))
    throw new Error('Sample follower feed does not match this run');
  latencies.sort((a, b) => a - b);
  const percentile = p => latencies[Math.ceil(latencies.length * p) - 1];
  const report = {
    recordedAt: new Date().toISOString(), authorId, firstFollower, followers, posts, workers,
    httpConcurrency: concurrency, consumerConcurrencyPerWorker: process.env.FEED_CONCURRENCY ?? '8 (compose default)',
    prefetchPerWorker: process.env.FEED_PREFETCH ?? '16 (compose default)',
    expectedFeedEntries: expectedEntries, verifiedFeedEntries: expectedEntries, expectedBatches,
    acceptanceSeconds, acceptedPostsPerSecond: posts / acceptanceSeconds,
    httpP50Ms: percentile(0.5), httpP95Ms: percentile(0.95), httpMaxMs: latencies.at(-1),
    drainSecondsIncludingWorkerStart, feedEntriesPerSecond: expectedEntries / drainSecondsIncludingWorkerStart,
    host: { platform: platform(), cpu: cpus()[0]?.model, logicalCpus: cpus().length, memoryGiB: totalmem() / 2 ** 30 },
    caveats: 'Local single-node broker and PostgreSQL; not a production capacity guarantee. Drain includes worker startup and <=500ms polling. Followers are seeded before posts. API acceptance is measured with delivery stopped; no synchronous baseline. Compare repeated runs under equal resource limits.',
  };
  const directory = resolve(root, 'docs/week5/results');
  await mkdir(directory, { recursive: true });
  const path = resolve(directory, `load-${workers}workers-${Date.now()}.json`);
  await writeFile(path, JSON.stringify(report, null, 2) + '\n');
  console.log(JSON.stringify(report, null, 2));
  console.log(`Saved ${path}`);
} finally {
  if (restartNeeded) compose('up', '-d', '--no-deps', '--scale', `worker=${workers}`, 'worker');
}
