#!/usr/bin/env bash
# ExamShield — deployment package builder
#
# Usage:
#   ./package.sh                           # build tarball from git tag
#   ./package.sh --push ghcr.io/yourorg   # build tarball AND push image to registry
#   ./package.sh --skip-tests             # skip test suite (faster CI re-runs)
#   ./package.sh --version 1.2.3          # override version (default: git tag)
#
# Output:
#   dist/examshield-VERSION.tar.gz
#
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# ── Defaults ─────────────────────────────────────────────────────────────────
PUSH_REGISTRY=""
SKIP_TESTS=false
VERSION_OVERRIDE=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --push)       PUSH_REGISTRY="$2"; shift 2 ;;
    --skip-tests) SKIP_TESTS=true;    shift ;;
    --version)    VERSION_OVERRIDE="$2"; shift 2 ;;
    *) echo "Unknown argument: $1"; exit 1 ;;
  esac
done

# ── Colours ───────────────────────────────────────────────────────────────────
R='\033[0;31m' G='\033[0;32m' Y='\033[1;33m' C='\033[0;36m' B='\033[0;34m' NC='\033[0m'
ok()    { echo -e "  ${G}✓${NC} $*"; }
warn()  { echo -e "  ${Y}⚠${NC} $*"; }
err()   { echo -e "  ${R}✗${NC} $*" >&2; exit 1; }
info()  { echo -e "  ${B}→${NC} $*"; }
sep()   { echo -e "\n${C}── $* ──${NC}"; }

echo -e "${C}"
cat << 'EOF'
  ╔══════════════════════════════════════╗
  ║       ExamShield  Packager           ║
  ║   Build · Test · Bundle · Release    ║
  ╚══════════════════════════════════════╝
EOF
echo -e "${NC}"

# ── Version ───────────────────────────────────────────────────────────────────
sep "Version"

if [[ -n "$VERSION_OVERRIDE" ]]; then
  VERSION="$VERSION_OVERRIDE"
  warn "Version overridden: $VERSION"
elif git -C "$SCRIPT_DIR" describe --tags --exact-match HEAD 2>/dev/null | grep -q .; then
  VERSION="$(git -C "$SCRIPT_DIR" describe --tags --exact-match HEAD)"
  ok "Git tag: $VERSION"
elif git -C "$SCRIPT_DIR" describe --tags 2>/dev/null | grep -q .; then
  VERSION="$(git -C "$SCRIPT_DIR" describe --tags)"
  warn "Not on exact tag — using describe: $VERSION"
else
  VERSION="0.0.0-dev-$(git -C "$SCRIPT_DIR" rev-parse --short HEAD 2>/dev/null || echo 'unknown')"
  warn "No git tags found — using: $VERSION"
fi

# Strip leading 'v' for use in image tags and filenames
VERSION_CLEAN="${VERSION#v}"
PACKAGE_NAME="examshield-${VERSION_CLEAN}"
DIST_DIR="$SCRIPT_DIR/dist"
STAGE_DIR="$DIST_DIR/$PACKAGE_NAME"

echo -e "  Version : ${C}$VERSION_CLEAN${NC}"
echo -e "  Package : ${C}$PACKAGE_NAME.tar.gz${NC}"
[[ -n "$PUSH_REGISTRY" ]] && echo -e "  Registry: ${C}$PUSH_REGISTRY${NC}"

# ── Prerequisites ─────────────────────────────────────────────────────────────
sep "Prerequisites"

require() { command -v "$1" &>/dev/null || err "$1 is required but not found."; ok "$1"; }
require docker
require dotnet
require node
require npm

# ── Tests ─────────────────────────────────────────────────────────────────────
if [[ "$SKIP_TESTS" == "true" ]]; then
  warn "Skipping tests (--skip-tests)"
else
  sep "Running tests"
  cd "$SCRIPT_DIR"
  dotnet test --configuration Release -q --no-build 2>/dev/null || \
    dotnet test --configuration Release -q
  ok "All tests passed"
fi

# ── Build API Docker image ────────────────────────────────────────────────────
sep "Building API Docker image"

API_IMAGE="examshield-api"
API_TAG="${API_IMAGE}:${VERSION_CLEAN}"
API_TAG_LATEST="${API_IMAGE}:latest"

cd "$SCRIPT_DIR"
docker build \
  --file src/ExamShield.Api/Dockerfile \
  --tag "$API_TAG" \
  --tag "$API_TAG_LATEST" \
  --build-arg BUILD_CONFIGURATION=Release \
  --label "org.opencontainers.image.version=$VERSION_CLEAN" \
  --label "org.opencontainers.image.created=$(date -u +%Y-%m-%dT%H:%M:%SZ)" \
  --label "org.opencontainers.image.revision=$(git rev-parse HEAD 2>/dev/null || echo '')" \
  .

ok "Image built: $API_TAG"

# ── Build Dashboard ───────────────────────────────────────────────────────────
sep "Building Dashboard"

cd "$SCRIPT_DIR/src/ExamShield.Dashboard"

if [[ ! -d node_modules ]]; then
  info "Installing npm dependencies…"
  npm ci --prefer-offline --quiet
fi

# Production build — API served at /api relative to dashboard origin (nginx proxy)
VITE_API_URL=/api npm run build -- --mode production
ok "Dashboard built: $(du -sh dist | cut -f1) in src/ExamShield.Dashboard/dist"
cd "$SCRIPT_DIR"

# ── Assemble staging directory ────────────────────────────────────────────────
sep "Assembling package"

rm -rf "$STAGE_DIR"
mkdir -p "$STAGE_DIR"/{dashboard,k8s,nginx,scripts}

# Dashboard static files
cp -r src/ExamShield.Dashboard/dist/. "$STAGE_DIR/dashboard/"
ok "Dashboard → package/dashboard/"

# Nginx config
cp infra/nginx/nginx.conf "$STAGE_DIR/nginx/"
ok "nginx.conf → package/nginx/"

# K8s manifests
cp infra/k8s/*.yaml "$STAGE_DIR/k8s/"
# Stamp the correct image tag into api.yaml
sed -i.bak "s|image: examshield-api:latest|image: ${API_TAG}|g" "$STAGE_DIR/k8s/api.yaml"
rm -f "$STAGE_DIR/k8s/api.yaml.bak"
ok "k8s manifests → package/k8s/ (image tagged: $API_TAG)"

# Production compose
cp infra/docker-compose.prod.yml "$STAGE_DIR/"
# Update compose file to reference correct image version
sed -i.bak "s|examshield-api}:latest|examshield-api}:${VERSION_CLEAN}|g" "$STAGE_DIR/docker-compose.prod.yml"
rm -f "$STAGE_DIR/docker-compose.prod.yml.bak"
ok "docker-compose.prod.yml → package/"

# Secrets template
cat > "$STAGE_DIR/.env.template" << ENVEOF
# ExamShield production environment — copy to .env and fill in all values.
# NEVER commit .env to version control.
# Generated for version: ${VERSION_CLEAN}

APP_VERSION=${VERSION_CLEAN}

# ── Database ──────────────────────────────────────────────────────────────────
POSTGRES_PASSWORD=CHANGE_ME_min24chars

# ── Cache ─────────────────────────────────────────────────────────────────────
REDIS_PASSWORD=CHANGE_ME_min24chars

# ── Message broker ────────────────────────────────────────────────────────────
RABBITMQ_USER=examshield
RABBITMQ_PASSWORD=CHANGE_ME_min24chars

# ── Object storage ────────────────────────────────────────────────────────────
MINIO_ACCESS_KEY=CHANGE_ME
MINIO_SECRET_KEY=CHANGE_ME_min32chars
MINIO_BUCKET=examshield
# Set true to enable S3 Object Lock (WORM, tamper-evident). Cannot be undone.
MINIO_ENABLE_LOCK=true

# ── Authentication ────────────────────────────────────────────────────────────
# Generate: openssl rand -base64 64
JWT_SECRET=CHANGE_ME_at_least_64_chars
JWT_ISSUER=ExamShield
JWT_AUDIENCE=ExamShield

# ── Encryption ────────────────────────────────────────────────────────────────
# AES-256 master key — 32 random bytes, base64 encoded.
# Generate: openssl rand -base64 32
# In production, use HashiCorp Vault / AWS KMS / Azure Key Vault instead.
ENCRYPTION_MASTER_KEY=CHANGE_ME_base64_32bytes

# Watermark HMAC key — 32 random bytes, base64 encoded.
# Generate: openssl rand -base64 32
WATERMARK_HMAC_KEY=CHANGE_ME_base64_32bytes

# Server signing key — ECDSA P-256 PEM private key.
# Generate: openssl ecparam -name prime256v1 -genkey -noout | openssl pkcs8 -topk8 -nocrypt
SERVER_SIGNING_KEY=-----BEGIN PRIVATE KEY-----\\nCHANGE_ME\\n-----END PRIVATE KEY-----

# ── Dashboard ─────────────────────────────────────────────────────────────────
# URL where the dashboard is accessible (for CORS). No trailing slash.
DASHBOARD_URL=https://examshield.yourorganization.com
DASHBOARD_PORT=80

# ── Networking ────────────────────────────────────────────────────────────────
API_PORT=8080

# ── OCR (optional) ────────────────────────────────────────────────────────────
# Stub = no OCR processing (answers stay as-is)
# Http = real OCR service
OCR_TYPE=Stub
# OCR_ENDPOINT=http://your-ocr-service:8081/ocr/extract
ENVEOF
ok ".env.template → package/"

# Write a simple health-check file for the nginx container
echo "ok" > "$STAGE_DIR/dashboard/health.txt"

# Deployment instructions
cat > "$STAGE_DIR/DEPLOY.md" << MDEOF
# ExamShield ${VERSION_CLEAN} — Deployment Guide

## Quick start (Docker Compose, single server)

\`\`\`bash
# 1. Copy secrets template and fill in all values
cp .env.template .env
nano .env

# 2. Generate required secrets (if not using a secrets manager)
echo "POSTGRES_PASSWORD=\$(openssl rand -base64 24)"   >> .env
echo "REDIS_PASSWORD=\$(openssl rand -base64 24)"       >> .env
echo "RABBITMQ_PASSWORD=\$(openssl rand -base64 24)"   >> .env
echo "MINIO_SECRET_KEY=\$(openssl rand -base64 32)"    >> .env
echo "JWT_SECRET=\$(openssl rand -base64 64)"           >> .env
echo "ENCRYPTION_MASTER_KEY=\$(openssl rand -base64 32)" >> .env
echo "WATERMARK_HMAC_KEY=\$(openssl rand -base64 32)"  >> .env

# 3. Load the API image (skip if using a registry)
docker load < api-image.tar

# 4. Start all services
docker compose -f docker-compose.prod.yml up -d

# 5. Open the browser and complete first-time setup
# → http://YOUR_SERVER/setup
\`\`\`

## First-time setup wizard

Navigate to \`http://YOUR_SERVER/setup\` after all containers are healthy.
The wizard will:
1. Verify service connectivity
2. Create the first Super Administrator account
3. Optionally load demo data for evaluation

The wizard locks permanently once the first Super Administrator is created.

## Upgrading

\`\`\`bash
docker load < api-image.tar           # load new API image
docker compose -f docker-compose.prod.yml pull dashboard
docker compose -f docker-compose.prod.yml up -d --no-deps api dashboard
\`\`\`

## Kubernetes

Apply manifests from the \`k8s/\` directory:

\`\`\`bash
kubectl apply -f k8s/namespace.yaml
kubectl apply -f k8s/
\`\`\`

Before applying, fill in the secrets in \`k8s/api.yaml\` — look for all \`CHANGE_ME\` values.

## Security checklist

- [ ] All \`.env\` secrets are random and at least 32 characters
- [ ] \`MINIO_ENABLE_LOCK=true\` for production (tamper-evident storage)
- [ ] \`Features__AutoSeedDemo=false\` (default in this build)
- [ ] RabbitMQ management UI (port 15672) is NOT exposed to the internet
- [ ] MinIO console (port 9001) is NOT exposed to the internet
- [ ] TLS termination is configured upstream (nginx, Caddy, Traefik, load balancer)
- [ ] Backups scheduled for postgres_data, minio_data volumes
MDEOF
ok "DEPLOY.md → package/"

# ── Save Docker image ─────────────────────────────────────────────────────────
sep "Saving Docker image"

IMAGE_TAR="$STAGE_DIR/api-image.tar"
info "docker save $API_TAG → api-image.tar (this may take a moment)…"
docker save "$API_TAG" | gzip > "$IMAGE_TAR"
IMAGE_SIZE=$(du -sh "$IMAGE_TAR" | cut -f1)
ok "api-image.tar ($IMAGE_SIZE)"

# ── Create tarball ────────────────────────────────────────────────────────────
sep "Creating tarball"

TAR_FILE="$DIST_DIR/${PACKAGE_NAME}.tar.gz"
cd "$DIST_DIR"
tar -czf "$TAR_FILE" "$PACKAGE_NAME/"
TARBALL_SIZE=$(du -sh "$TAR_FILE" | cut -f1)
ok "${PACKAGE_NAME}.tar.gz ($TARBALL_SIZE)"

# Checksum
sha256sum "$TAR_FILE" > "${TAR_FILE}.sha256" 2>/dev/null || \
  shasum -a 256 "$TAR_FILE" > "${TAR_FILE}.sha256"
ok "SHA-256: $(cat "${TAR_FILE}.sha256" | awk '{print $1}')"

# ── Registry push (optional) ──────────────────────────────────────────────────
if [[ -n "$PUSH_REGISTRY" ]]; then
  sep "Pushing to registry: $PUSH_REGISTRY"

  REMOTE_TAG="${PUSH_REGISTRY}/examshield-api:${VERSION_CLEAN}"
  REMOTE_LATEST="${PUSH_REGISTRY}/examshield-api:latest"

  docker tag "$API_TAG"     "$REMOTE_TAG"
  docker tag "$API_TAG"     "$REMOTE_LATEST"
  docker push "$REMOTE_TAG"
  docker push "$REMOTE_LATEST"
  ok "Pushed: $REMOTE_TAG"
  ok "Pushed: $REMOTE_LATEST"

  # Write the registry image reference into the staged k8s manifest
  sed -i.bak "s|image: ${API_TAG}|image: ${REMOTE_TAG}|g" "$STAGE_DIR/k8s/api.yaml"
  rm -f "$STAGE_DIR/k8s/api.yaml.bak"

  # Re-create tarball with updated k8s manifest
  cd "$DIST_DIR"
  tar -czf "$TAR_FILE" "$PACKAGE_NAME/"
  sha256sum "$TAR_FILE" > "${TAR_FILE}.sha256" 2>/dev/null || \
    shasum -a 256 "$TAR_FILE" > "${TAR_FILE}.sha256"
  info "Tarball re-created with registry image reference"
fi

# ── Summary ───────────────────────────────────────────────────────────────────
sep "Done"
echo ""
echo -e "  ${G}Package:${NC} dist/${PACKAGE_NAME}.tar.gz ($TARBALL_SIZE)"
echo -e "  ${G}Checksum:${NC} dist/${PACKAGE_NAME}.tar.gz.sha256"
echo ""
echo -e "  ${C}Contents:${NC}"
echo -e "    api-image.tar          — Docker image (load with: docker load < api-image.tar)"
echo -e "    docker-compose.prod.yml — production Compose"
echo -e "    dashboard/             — compiled React SPA"
echo -e "    nginx/nginx.conf       — nginx serving config"
echo -e "    k8s/                   — Kubernetes manifests"
echo -e "    .env.template          — secrets template"
echo -e "    DEPLOY.md              — operator deployment guide"
echo ""
[[ -n "$PUSH_REGISTRY" ]] && echo -e "  ${G}Registry image:${NC} ${PUSH_REGISTRY}/examshield-api:${VERSION_CLEAN}"
echo ""
