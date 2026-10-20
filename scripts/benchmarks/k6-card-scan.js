import http from 'k6/http';
import { check, sleep } from 'k6';
import { Trend, Rate } from 'k6/metrics';

// Custom metrics for Door Check-in Acceptance Criteria SC-01 (<50ms p95)
const doorScanLatency = new Trend('door_scan_latency_ms');
const errorRate = new Rate('door_scan_errors');

export const options = {
  scenarios: {
    door_burst_scan: {
      executor: 'per-vu-iterations',
      vus: 50,
      iterations: 1,
      maxDuration: '30s',
    },
  },
  thresholds: {
    // SC-01 & GATE-01: Door card check-in response time MUST be strictly < 50ms p95
    'door_scan_latency_ms': ['p(95)<50'],
    // 0 server errors (5xx)
    'door_scan_errors': ['rate==0'],
    'http_req_failed': ['rate<0.01'],
  },
};

const BASE_URL = __ENV.API_BASE_URL || 'http://localhost:5080';
const WORKSPACE_ID = __ENV.WORKSPACE_ID || '018f2f2c-5678-7123-8abc-000000000000';
const SESSION_ID = __ENV.SESSION_ID || '018f3a10-1111-7000-8000-000000000001';
const JWT_TOKEN = __ENV.AUTH_TOKEN || '';

export default function () {
  const vuId = __VU;
  const rawToken = `NFC_TEST_TOKEN_${String(vuId).padStart(4, '0')}`;

  const url = `${BASE_URL}/api/v1/workspaces/${WORKSPACE_ID}/sessions/${SESSION_ID}/attendance/scan`;
  const payload = JSON.stringify({
    raw_token: rawToken,
  });

  const params = {
    headers: {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
      'Authorization': `Bearer ${JWT_TOKEN}`,
    },
  };

  const startTime = Date.now();
  const res = http.post(url, payload, params);
  const latency = Date.now() - startTime;

  doorScanLatency.add(latency);

  const isSuccess = check(res, {
    'status is 200 or 201': (r) => r.status === 200 || r.status === 201,
    'latency is under 50ms': () => latency < 50,
    'not a 5xx server error': (r) => r.status < 500,
  });

  if (!isSuccess) {
    errorRate.add(1);
  } else {
    errorRate.add(0);
  }
}
