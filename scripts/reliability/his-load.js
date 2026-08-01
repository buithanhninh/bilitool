import http from 'k6/http';
import { check } from 'k6';
import exec from 'k6/execution';

const profile = __ENV.LOAD_PROFILE || 'smoke';
const profiles = {
  smoke: { rate: 10, duration: '10s', preAllocatedVUs: 10, maxVUs: 20 },
  load: { rate: 100, duration: '2m', preAllocatedVUs: 50, maxVUs: 200 },
  soak: { rate: 50, duration: '30m', preAllocatedVUs: 20, maxVUs: 100 },
};

if (!profiles[profile]) {
  throw new Error(`Unknown LOAD_PROFILE '${profile}'`);
}

export const options = {
  scenarios: {
    clinical_calculation: {
      executor: 'constant-arrival-rate',
      rate: profiles[profile].rate,
      timeUnit: '1s',
      duration: profiles[profile].duration,
      preAllocatedVUs: profiles[profile].preAllocatedVUs,
      maxVUs: profiles[profile].maxVUs,
      gracefulStop: '5s',
    },
  },
  thresholds: {
    checks: ['rate==1'],
    http_req_failed: ['rate<0.02'],
    http_req_duration: ['p(95)<2000', 'p(99)<5000'],
    dropped_iterations: ['count==0'],
  },
};

const baseUrl = __ENV.BASE_URL || 'http://127.0.0.1:18080';
const apiKey = __ENV.SANDBOX_API_KEY;
if (!apiKey) throw new Error('SANDBOX_API_KEY is required');

export default function () {
  const unique = `${profile}-${exec.vu.idInTest}-${exec.scenario.iterationInTest}-${Date.now()}`;
  const payload = JSON.stringify({
    source: { system: 'K6', facility: 'SANDBOX', messageId: `msg-${unique}` },
    patient: { identifier: `patient-${unique}`, assigningAuthority: 'SANDBOX', ageHours: 48, gestationalAgeWeeks: 38, phototherapyStatus: 'none' },
    encounter: { identifier: `enc-${unique}` },
    order: { identifier: `order-${unique}` },
    specimen: { identifier: `spec-${unique}`, collectedAt: '2026-08-01T08:00:00Z' },
    observation: { identifier: `obs-${unique}`, effectiveAt: '2026-08-01T08:00:00Z', value: 12, unit: 'mg/dL' },
    riskFactors: {},
  });
  const response = http.post(`${baseUrl}/api/v3/clinical/bilirubin/calculate`, payload, {
    headers: {
      'Content-Type': 'application/json',
      'X-API-Key': apiKey,
      'Idempotency-Key': unique,
      'X-Correlation-ID': `k6-${unique}`.slice(0, 64),
    },
    timeout: '6s',
  });

  check(response, {
    'status is 200': (value) => value.status === 200,
    'response has resultId': (value) => value.status === 200 && value.json('resultId') !== undefined,
    'response has provenance': (value) => value.status === 200 && value.json('provenance.engineVersion') !== undefined,
  });
}
