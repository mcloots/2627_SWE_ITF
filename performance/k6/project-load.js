import http from 'k6/http';
import { check } from 'k6';
import exec from 'k6/execution';
import { Rate } from 'k6/metrics';

function positiveNumber(name, fallback, integer = false) {
  const value = Number(__ENV[name] || fallback);
  if (!Number.isFinite(value) || value <= 0 || (integer && !Number.isInteger(value))) {
    throw new Error(`${name} must be a positive ${integer ? 'integer' : 'number'}`);
  }
  return value;
}

const target = __ENV.TARGET || 'health';
if (!['health', 'posts', 'read'].includes(target)) throw new Error('TARGET must be health, posts, or read');
if (target === 'posts' && __ENV.ALLOW_WRITES !== 'true') {
  throw new Error('Posts are saved in the database. Set ALLOW_WRITES=true for a disposable test database.');
}
const baseUrl = (__ENV.BASE_URL || 'http://localhost:5212').replace(/\/+$/, '');
const rate = positiveNumber('RATE', 1, true);
const preAllocatedVUs = positiveNumber('PRE_ALLOCATED_VUS', 20, true);
const maxVUs = positiveNumber('MAX_VUS', 100, true);
if (maxVUs < preAllocatedVUs) throw new Error('MAX_VUS must be >= PRE_ALLOCATED_VUS');
const p95 = positiveNumber('P95_MS', 500);
const authorId = __ENV.AUTHOR_ID || '11111111-1111-4111-8111-111111111111';
const guid = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const emptyGuid = '00000000-0000-0000-0000-000000000000';
if (!guid.test(authorId) || authorId === emptyGuid) throw new Error('AUTHOR_ID must be a nonempty GUID');

const postId = (__ENV.POST_ID || '').toLowerCase();
if (target === 'read' && (!guid.test(postId) || postId === emptyGuid)) {
  throw new Error('TARGET=read requires POST_ID: create a test post first and pass its ID.');
}
const expectedContent = __ENV.EXPECTED_CONTENT;

// One sample per response: a wrong body counts once, even if several checks fail.
const failedResponses = new Rate('failed_responses');
http.setResponseCallback(http.expectedStatuses(target === 'posts' ? 201 : 200));

export const options = {
  scenarios: {
    api: {
      executor: 'constant-arrival-rate',
      rate,
      timeUnit: '1s',
      duration: __ENV.DURATION || '15s',
      preAllocatedVUs,
      maxVUs,
      gracefulStop: '15s',
    },
  },
  // Do not follow redirects: one iteration must send exactly one request.
  maxRedirects: 0,
  summaryTrendStats: ['avg', 'med', 'p(95)', 'p(99)', 'max'],
  thresholds: {
    http_req_duration: [`p(95)<${p95}`],
    http_req_failed: ['rate<0.01'],
    failed_responses: ['rate<0.01'],
    dropped_iterations: ['count==0'],
  },
};

export default function () {
  const name = target === 'posts' ? 'POST /api/posts'
    : target === 'read' ? 'GET /api/posts/:id' : 'GET /api/health';
  const params = { timeout: '10s', tags: { name } };
  let response;
  if (target === 'posts') {
    params.headers = { 'Content-Type': 'application/json' };
    response = http.post(`${baseUrl}/api/posts`, JSON.stringify({
      authorId,
      content: `k6-workshop vu=${exec.vu.idInTest} iteration=${exec.scenario.iterationInTest}`,
    }), params);
  } else if (target === 'read') {
    response = http.get(`${baseUrl}/api/posts/${postId}`, params);
  } else {
    response = http.get(`${baseUrl}/api/health`, params);
  }

  let body = null;
  try { body = response.json(); } catch (_) { /* HTML/errors must fail checks, not crash the iteration. */ }
  const correct = target === 'posts'
    ? check(response, {
      '201 Created': (r) => r.status === 201,
      'nonempty postId': () => guid.test(body?.postId || '') && body.postId !== emptyGuid,
      'Location identifies the created post': (r) => Boolean(body?.postId) && r.headers.Location === `/api/posts/${body.postId}`,
    })
    : target === 'read'
      ? check(response, {
        '200 OK': (r) => r.status === 200,
        'requested post returned': () => typeof body?.postId === 'string' && body.postId.toLowerCase() === postId,
        'post author matches': () => typeof body?.authorId === 'string' && body.authorId.toLowerCase() === authorId.toLowerCase(),
        'post content matches': () => typeof body?.content === 'string' && body.content.length > 0
          && (expectedContent === undefined || body.content === expectedContent),
        'valid creation timestamp': () => typeof body?.createdAt === 'string'
          && !Number.isNaN(Date.parse(body.createdAt)),
      })
    : check(response, {
      '200 OK': (r) => r.status === 200,
      'health response contract': () => typeof body?.status === 'string'
        && body.status.startsWith('Healthy')
        && typeof body?.service === 'string' && body.service.length > 0
        && typeof body?.timestamp === 'string' && !Number.isNaN(Date.parse(body.timestamp)),
    });
  failedResponses.add(!correct);
  // No sleep: the arrival-rate executor schedules iterations for us.
}
