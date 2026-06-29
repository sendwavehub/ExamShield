#!/usr/bin/env bash
# ExamShield first-time setup script (macOS / Linux)
# Usage: ./setup.sh [--demo]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEMO_MODE=false
for arg in "$@"; do [[ "$arg" == "--demo" ]] && DEMO_MODE=true; done

# ── Colours ──────────────────────────────────────────────────────────────────
R='\033[0;31m' G='\033[0;32m' Y='\033[1;33m' C='\033[0;36m' B='\033[0;34m' NC='\033[0m'
ok()   { echo -e "  ${G}✓${NC} $*"; }
warn() { echo -e "  ${Y}⚠${NC} $*"; }
err()  { echo -e "  ${R}✗${NC} $*"; exit 1; }
info() { echo -e "  ${B}→${NC} $*"; }
sep()  { echo -e "\n${C}── $* ──${NC}"; }

echo -e "${C}"
cat << 'EOF'
  ╔══════════════════════════════════════╗
  ║         ExamShield  Setup            ║
  ║    Secure Exam Scanning System       ║
  ╚══════════════════════════════════════╝
EOF
echo -e "${NC}"

# ── 1. Prerequisites ─────────────────────────────────────────────────────────
sep "Checking prerequisites"

require() {
  command -v "$1" &>/dev/null || err "$1 is required. Install it and re-run."
  ok "$1 → $(command -v "$1")"
}

require docker
require dotnet

DOTNET_MAJOR=$(dotnet --version 2>/dev/null | cut -d. -f1)
[[ "$DOTNET_MAJOR" -ge 9 ]] || err ".NET 9+ SDK required (found $(dotnet --version))"
ok ".NET SDK $(dotnet --version)"

if command -v node &>/dev/null; then
  ok "Node.js $(node --version)"
else
  warn "Node.js not found — dashboard dev server unavailable (npm run dev)"
fi

if command -v python3 &>/dev/null; then
  ok "Python 3 (used for JSON editing)"
else
  warn "Python 3 not found — JWT/AES secrets will not be auto-written to appsettings"
fi

# ── 2. Environment file ───────────────────────────────────────────────────────
sep "Configuring environment"

ENV_FILE="$SCRIPT_DIR/infra/.env"
ENV_EXAMPLE="$SCRIPT_DIR/infra/.env.example"

if [[ -f "$ENV_FILE" ]]; then
  ok ".env already exists — skipping generation"
else
  info "Generating .env from .env.example with secure random values…"
  cp "$ENV_EXAMPLE" "$ENV_FILE"

  gen_pass() { LC_ALL=C tr -dc 'A-Za-z0-9@#%^&*' < /dev/urandom 2>/dev/null | head -c "${1:-32}" || true; }

  PG_PASS=$(gen_pass 24)
  RMQ_PASS=$(gen_pass 24)
  MINIO_SECRET=$(gen_pass 32)

  # Portable sed (macOS needs -i '' ; Linux uses -i)
  SED_INPLACE=(-i)
  [[ "$(uname)" == "Darwin" ]] && SED_INPLACE=(-i '')

  sed "${SED_INPLACE[@]}" \
    -e "s|POSTGRES_PASSWORD=.*|POSTGRES_PASSWORD=$PG_PASS|" \
    -e "s|RABBITMQ_PASSWORD=.*|RABBITMQ_PASSWORD=$RMQ_PASS|" \
    -e "s|MINIO_SECRET_KEY=.*|MINIO_SECRET_KEY=$MINIO_SECRET|" \
    "$ENV_FILE"

  # Update JWT secret and AES master key in appsettings.Development.json
  SETTINGS="$SCRIPT_DIR/src/ExamShield.Api/appsettings.Development.json"
  if command -v python3 &>/dev/null && [[ -f "$SETTINGS" ]]; then
    JWT_SECRET=$(gen_pass 64)
    AES_KEY=$(openssl rand -base64 32 2>/dev/null || gen_pass 44)
    python3 - "$SETTINGS" "$JWT_SECRET" "$AES_KEY" << 'PYEOF'
import json, sys
path, jwt, aes = sys.argv[1], sys.argv[2], sys.argv[3]
with open(path) as f: cfg = json.load(f)
cfg['Jwt']['Secret'] = jwt
cfg['Encryption']['MasterKeyBase64'] = aes
with open(path, 'w') as f: json.dump(cfg, f, indent=2)
PYEOF
    info "JWT secret and AES encryption key regenerated in appsettings.Development.json"
  fi

  ok ".env generated"
fi

# ── 3. Docker infrastructure ──────────────────────────────────────────────────
sep "Starting infrastructure (Docker Compose)"

cd "$SCRIPT_DIR/infra"
docker compose up -d
info "Waiting for services to become healthy…"

wait_healthy() {
  local svc="$1" elapsed=0
  while true; do
    local health
    health=$(docker inspect --format '{{.State.Health.Status}}' \
      "$(docker compose ps -q "$svc" 2>/dev/null)" 2>/dev/null || echo "unknown")
    [[ "$health" == "healthy" ]] && { ok "$svc"; return; }
    elapsed=$((elapsed + 3))
    [[ $elapsed -gt 90 ]] && { err "$svc did not become healthy after 90s"; }
    printf "    %s… %ds\r" "$svc" "$elapsed"
    sleep 3
  done
}

wait_healthy postgres
wait_healthy redis
wait_healthy rabbitmq
wait_healthy minio
cd "$SCRIPT_DIR"

# ── 4. Database migrations ────────────────────────────────────────────────────
sep "Applying database migrations"

dotnet ef database update \
  --project src/ExamShield.Infrastructure \
  --startup-project src/ExamShield.Api \
  --no-build 2>/dev/null ||
dotnet ef database update \
  --project src/ExamShield.Infrastructure \
  --startup-project src/ExamShield.Api
ok "Migrations applied"

# ── 5. Build ──────────────────────────────────────────────────────────────────
sep "Building backend"
dotnet build --configuration Release -q
ok "Build succeeded"

# ── 6. Demo mode shortcut ─────────────────────────────────────────────────────
if [[ "$DEMO_MODE" == "true" ]]; then
  sep "Demo mode: enabling AutoSeedDemo"
  # Set the feature flag so DataSeeder seeds on next API startup
  SETTINGS="$SCRIPT_DIR/src/ExamShield.Api/appsettings.Development.json"
  if command -v python3 &>/dev/null && [[ -f "$SETTINGS" ]]; then
    python3 - "$SETTINGS" << 'PYEOF'
import json, sys
with open(sys.argv[1]) as f: cfg = json.load(f)
cfg.setdefault('Features', {})['AutoSeedDemo'] = True
with open(sys.argv[1], 'w') as f: json.dump(cfg, f, indent=2)
PYEOF
    info "AutoSeedDemo = true — demo data will seed on first API startup"
  fi
fi

# ── Done ──────────────────────────────────────────────────────────────────────
sep "Setup complete"
echo ""
echo -e "  ${G}Run these commands to start ExamShield:${NC}"
echo ""
echo -e "    ${C}# Terminal 1 — API${NC}"
echo -e "    dotnet run --project src/ExamShield.Api"
echo ""
echo -e "    ${C}# Terminal 2 — Dashboard${NC}"
echo -e "    cd src/ExamShield.Dashboard && npm install && npm run dev"
echo ""
echo -e "  Then open ${C}http://localhost:5173/setup${NC} to complete installation."
echo ""
if [[ "$DEMO_MODE" == "true" ]]; then
  echo -e "  ${Y}Demo credentials (after API starts):${NC}"
  echo -e "    Email:    admin@examshield.local"
  echo -e "    Password: Demo@1234"
  echo ""
fi
