#!/usr/bin/env bash
# Quick runner: k6 run <scenario> [extra k6 flags]
#
# Available scenarios:
#   capture-upload    — 200 concurrent invigilators: register + upload + verify  (default)
#   concurrent-review — 50 reviewers + 10 supervisors draining the review queue
#   ocr-pipeline      — OCR trigger + result polling under load
#   auth-login        — Auth endpoint stress + brute-force rate-limit probe
#
# Usage:
#   ./run.sh                                           # capture-upload on localhost:5000
#   ./run.sh auth-login --env BASE_URL=http://api:5000
#   ./run.sh ocr-pipeline --scenario normal
set -euo pipefail

SCENARIO=${1:-capture-upload}
shift || true

k6 run \
  --env BASE_URL="${BASE_URL:-http://localhost:5000}" \
  --env ADMIN_EMAIL="${ADMIN_EMAIL:-admin@examshield.local}" \
  --env ADMIN_PASS="${ADMIN_PASS:-Admin@123!}" \
  "scenarios/${SCENARIO}.js" \
  "$@"
