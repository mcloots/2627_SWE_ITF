import assert from 'node:assert/strict';
import { setTimeout as sleep } from 'node:timers/promises';
import { request, compose, waitFor, parallel, publish, rabbit, waitForIdle } from './common.mjs';

await request('/health');
await waitForIdle();
const authorId = crypto.randomUUID();
const followers = Array.from({ length: 1001 }, () => crypto.randomUUID());
await parallel(followers.length, 16, i => request(`/authors/${authorId}/followers/${followers[i]}`, { method: 'PUT', body: {}, expected: 204 }));
await request(`/authors/${authorId}/followers/${followers[0]}`, { method: 'PUT', body: {}, expected: 204 });
await request(`/authors/${authorId}/followers/${authorId}`, { method: 'PUT', body: {}, expected: 400 });
await request('/posts', { method: 'POST', body: { authorId, content: '' }, expected: 400 });
const posted = await request('/posts', { method: 'POST', body: { authorId, content: 'Recovery verification' }, expected: 201 });
await waitFor(async () => (await request(`/demo/fanout/${authorId}`)).feedEntries === 1001, '1001 deliveries across three batches');
const sample = await request(`/feeds/${followers[0]}?limit=1`);
assert.equal(sample.items[0].postId, posted.postId);
assert.equal(sample.items[0].content, 'Recovery verification');
assert.equal((await request(`/feeds/${followers[0]}?before=${sample.nextCursor}&limit=1`)).items.length, 0);
assert.equal((await request(`/demo/fanout/${authorId}`)).followers, 1001);

// Different message IDs bypass transport duplicate detection: the DB key must still protect the effect.
for (let i = 0; i < 3; i++) await publish('itfpulse-deliver-feed-v1', 'DeliverPostToFollowersV1', {
  postId: posted.postId, createdAt: sample.items[0].createdAt, followerIds: followers.slice(0, 500),
});
await waitForIdle();
assert.equal((await request(`/demo/fanout/${authorId}`)).feedEntries, 1001);
console.log('PASS: bounded multi-page fan-out, duplicate follow, validation, feed read/pagination and duplicate delivery.');

const lateFollower = crypto.randomUUID();
await request(`/authors/${authorId}/followers/${lateFollower}`, { method: 'PUT', body: {}, expected: 204 });
assert.equal((await request(`/feeds/${lateFollower}`)).items.length, 0);

try {
  compose('stop', 'rabbitmq');
  await request('/posts', { method: 'POST', body: { authorId, content: 'Committed while broker was offline' }, expected: 201 });
  assert.equal((await request(`/demo/fanout/${authorId}`)).posts, 2);
  // Restart the API as well: recovery must rely on durable outbox rows, not process memory.
  compose('restart', 'api');
  await waitFor(() => request('/health'), 'API restart without broker');
} finally {
  compose('start', 'rabbitmq');
}
await waitFor(async () => (await request(`/demo/fanout/${authorId}`)).feedEntries === 2003, 'outbox recovery after broker/API restart');
await waitForIdle();
console.log('PASS: broker outage + API restart preserved committed work; late follower receives only the new post.');

// Kill delivery workers while a larger batch of posts is being fanned out.
const crashPosts = 20;
await parallel(crashPosts, 8, i => request('/posts', { method: 'POST', body: { authorId, content: `Crash recovery ${i}` }, expected: 201 }));
try { compose('kill', '-s', 'SIGKILL', 'worker'); }
finally { compose('start', 'worker'); }
await waitFor(async () => (await request(`/demo/fanout/${authorId}`)).feedEntries === 2003 + crashPosts * 1002, 'worker crash recovery');
await waitForIdle();
console.log('PASS: all expected feed entries after killing and restarting delivery workers.');

// Poison-message behavior: invalid commands go to an inspectable error queue, not an endless requeue loop.
await publish('itfpulse-deliver-feed-v1', 'DeliverPostToFollowersV1', { postId: posted.postId, createdAt: sample.items[0].createdAt, followerIds: [] });
await waitFor(async () => (await rabbit('/queues/%2F/itfpulse-deliver-feed-v1_error')).messages_ready === 1, 'poison message in error queue');
console.log('PASS: invalid message isolated in itfpulse-deliver-feed-v1_error.');
// Remove only the deliberately injected test message. Leave unrelated messages untouched.
const poison = await rabbit('/queues/%2F/itfpulse-deliver-feed-v1_error/get', { count: 1, ackmode: 'ack_requeue_false', encoding: 'auto' });
assert.equal(JSON.parse(poison[0].payload).message.postId, posted.postId);
await sleep(6000);
console.log('All Week 5 integration checks passed.');
