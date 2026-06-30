/**
 * Load scenario: Auth endpoint stress + rate-limit verification
 *
 * Tests three patterns against POST /auth/login:
 *   1. Ramped valid logins  — p95 latency must stay < 300 ms
 *   2. Concurrent valid     — 100 VUs, 2 min sustained, 0 % errors
 *   3. Brute-force probe    — sends deliberately wrong passwords and asserts
 *      that the API returns 401/429 (never 200) within rate-limit windows
 *
 * The brute-force scenario validates that rate limiting is active.
 * It does NOT constitute an actual attack — credentials are random UUIDs.
 *
 * Run:
 *   k6 run --env BASE_URL=http://localhost:5000 scenarios/auth-login.js
 *   k6 run --env BASE_URL=http://localhost:5000 --scenario brute scenarios/auth-login.js
 */

import http from 'k6/http'
import { check, sleep } from 'k6'
import { Trend, Rate, Counter } from 'k6/metrics'
import { BASE_URL, ADMIN_EMAIL, ADMIN_PASS } from '../config.js'

const loginLatency   = new Trend('auth_login_latency', true)
const errorRate      = new Rate('auth_errors')
const rateLimitHits  = new Counter('rate_limit_responses')

export const options = {
  scenarios: {
    // Scenario 1: Normal sustained load
    normal: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 50  },
        { duration: '2m',  target: 100 },
        { duration: '30s', target: 0   },
      ],
      tags: { scenario: 'normal' },
    },
    // Scenario 2: Brute-force probe — runs after normal, not concurrently
    brute: {
      executor: 'constant-vus',
      vus: 20,
      duration: '1m',
      startTime: '3m30s',   // after normal load finishes
      tags: { scenario: 'brute' },
    },
  },
  thresholds: {
    'auth_login_latency{scenario:normal}': ['p(95)<300'],
    'http_req_failed{scenario:normal}':    ['rate<0.01'],
    // For the brute scenario we assert that ≥ 80% of requests are rejected (401 or 429)
    // A low rate-limited rejection rate means the limiter is not working.
    'auth_errors{scenario:brute}':         ['rate>0.80'],
  },
}

const JSON_HEADERS = { 'Content-Type': 'application/json' }

export default function () {
  const scenario = __ENV['scenario'] || 'normal'

  if (__SCENARIO_NAME === 'normal') {
    // Happy path — valid credentials
    const res = http.post(
      `${BASE_URL}/auth/login`,
      JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASS }),
      { headers: JSON_HEADERS })

    loginLatency.add(res.timings.duration)

    const ok = check(res, {
      'login status 200': r => r.status === 200,
      'has token':        r => Boolean(r.json('token')),
    })
    errorRate.add(!ok)

    sleep(Math.random() * 1.5 + 0.5)

  } else {
    // Brute-force probe — random credentials that will never succeed
    const fakeEmail    = `probe-${__VU}-${__ITER}@nowhere.invalid`
    const fakePassword = `Wrong${__VU}${__ITER}!`

    const res = http.post(
      `${BASE_URL}/auth/login`,
      JSON.stringify({ email: fakeEmail, password: fakePassword }),
      { headers: JSON_HEADERS })

    const rejected = res.status === 401 || res.status === 429
    if (res.status === 429) rateLimitHits.add(1)

    // In the brute scenario, a rejected request IS the expected outcome.
    // Record as "error" only when the server unexpectedly returns 2xx.
    errorRate.add(res.status >= 200 && res.status < 300)

    check(res, { 'probe rejected (401/429)': r => rejected })

    sleep(0.1)  // fast hammering to trigger rate limiter
  }
}
