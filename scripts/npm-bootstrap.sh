#!/usr/bin/env bash
# One-time setup for publishing the PDML npm packages from GitHub Actions via OIDC (trusted publishing).
#
# npm can only bind a trusted publisher to a package that already exists, so this script
#   1. publishes a 0.0.0 placeholder for every package that does not exist yet, and
#   2. binds every package to the CD.yml workflow of this repository as trusted publisher.
#
# Run it locally, once. Requirements: `npm login` as an owner of the @pdml scope, 2FA enabled, npm >= 11.15.
#
#   scripts/npm-bootstrap.sh [owner/repo] [workflow-file]
#
# Both arguments are case-sensitive and must match the GitHub repository and workflow file exactly.
set -euo pipefail

REPO="${1:-CBx0-dev/PDML}"
WORKFLOW="${2:-CD.yml}"

cd "$(dirname "$0")/.."

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT

node scripts/npm-stage.mjs placeholder --out "$STAGE" > /dev/null
mapfile -t PACKAGES < <(node scripts/npm-stage.mjs list)

for pkg in "${PACKAGES[@]}"; do
  if npm view "$pkg" name > /dev/null 2>&1; then
    echo "exists:    $pkg"
  else
    echo "publish:   $pkg@0.0.0 (placeholder)"
    npm publish "$STAGE/${pkg#@pdml/}" --access public --tag bootstrap
  fi
done

for pkg in "${PACKAGES[@]}"; do
  echo "trust:     $pkg -> $REPO ($WORKFLOW)"
  npm trust github "$pkg" --file "$WORKFLOW" --repo "$REPO" --allow-publish --yes \
    || echo "WARN: could not configure trust for $pkg (already configured?)"
done

echo
echo "Done. Verify with: npm trust list <package>"
