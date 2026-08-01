#!/usr/bin/env bash
set -euo pipefail

: "${BASE_URL:=http://127.0.0.1:18080}"
: "${SANDBOX_API_KEY:?SANDBOX_API_KEY is required}"

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work_dir="$(mktemp -d)"
trap 'rm -rf "$work_dir"' EXIT

curl --fail --silent --show-error "$BASE_URL/openapi/v3.json" > "$work_dir/openapi.json"
npm install --prefix "$work_dir" --no-save --silent \
  @types/node@24.1.0 openapi-fetch@0.14.0 openapi-typescript@7.8.0 typescript@5.9.2 tsx@4.20.3
cat > "$work_dir/package.json" <<'EOF'
{"private":true,"type":"module"}
EOF
"$work_dir/node_modules/.bin/openapi-typescript" "$work_dir/openapi.json" --output "$work_dir/schema.d.ts"

cp "$repo_root/sandbox/fixtures/rest-v3.json" "$work_dir/request.json"
cat > "$work_dir/client.ts" <<'EOF'
import createClient from "openapi-fetch";
import type { paths } from "./schema.js";
import request from "./request.json" with { type: "json" };

const baseUrl = process.env.BASE_URL!;
const apiKey = process.env.SANDBOX_API_KEY!;
const client = createClient<paths>({
  baseUrl,
  headers: { "X-API-Key": apiKey, "Idempotency-Key": "generated-client-001" }
});
const { data, error, response } = await client.POST("/api/v3/clinical/bilirubin/calculate", {
  body: request
});
if (response.status !== 200 || error || !data || !("resultId" in data)) {
  throw new Error(`Generated client failed: status=${response.status} error=${JSON.stringify(error)}`);
}
console.log(`Generated OpenAPI client PASS: status=${response.status}`);
EOF
cat > "$work_dir/tsconfig.json" <<'EOF'
{"compilerOptions":{"target":"ES2022","module":"NodeNext","moduleResolution":"NodeNext","strict":true,"resolveJsonModule":true,"esModuleInterop":true,"skipLibCheck":false},"include":["client.ts","schema.d.ts"]}
EOF

(cd "$work_dir" && ./node_modules/.bin/tsc --noEmit)
(cd "$work_dir" && BASE_URL="$BASE_URL" SANDBOX_API_KEY="$SANDBOX_API_KEY" ./node_modules/.bin/tsx client.ts)
