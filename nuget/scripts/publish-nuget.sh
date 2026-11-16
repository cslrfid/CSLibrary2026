#!/usr/bin/env bash
#
# Builds and publishes CSLibrary2026 to NuGet.org (Linux / macOS).
#
# Usage:
#   ./nuget/scripts/publish-nuget.sh <version> [--skip-push] [--dry-run]
#
# The NuGet API key is read from the NUGET_API_KEY environment variable or from
# nuget/api-key.txt (gitignored - copy nuget/api-key.txt.template to create it).
#
# net10.0-ios can only be built on macOS. On Linux the package is built without
# the iOS flavour; use the GitHub workflow (.github/workflows/publish-nuget.yml)
# to publish a package that contains every target framework.

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
PROJECT="${REPO_ROOT}/CSLibrary2026.csproj"
OUTPUT_DIR="${REPO_ROOT}/nupkg"

VERSION="${1:-}"
SKIP_PUSH="false"

shift || true
for arg in "$@"; do
    case "${arg}" in
        --skip-push) SKIP_PUSH="true" ;;
        --dry-run)   SKIP_PUSH="true" ;;
        *) echo "Unknown argument: ${arg}" >&2; exit 1 ;;
    esac
done

if [ -z "${VERSION}" ]; then
    echo "Usage: $0 <version> [--skip-push]" >&2
    exit 1
fi

# Resolve the API key.
API_KEY="${NUGET_API_KEY:-}"
if [ -z "${API_KEY}" ] && [ -f "${REPO_ROOT}/nuget/api-key.txt" ]; then
    API_KEY="$(tr -d '\r\n' < "${REPO_ROOT}/nuget/api-key.txt")"
fi

# Target frameworks for this host. net10.0-ios requires macOS.
if [ "$(uname -s)" = "Darwin" ]; then
    TARGET_FRAMEWORKS="netstandard2.0;netstandard2.1;net10.0;net10.0-windows10.0.19041.0;net10.0-android;net10.0-ios"
else
    TARGET_FRAMEWORKS="netstandard2.0;netstandard2.1;net10.0;net10.0-windows10.0.19041.0;net10.0-android"
fi

echo "Packing CSLibrary2026 ${VERSION} for: ${TARGET_FRAMEWORKS}"

mkdir -p "${OUTPUT_DIR}"

# PACK_TARGET_FRAMEWORKS is used instead of -p:TargetFrameworks because MSBuild
# treats ';' as a property separator on the command line.
PACK_TARGET_FRAMEWORKS="${TARGET_FRAMEWORKS}" \
    dotnet pack "${PROJECT}" \
        --configuration Release \
        --output "${OUTPUT_DIR}" \
        -p:Version="${VERSION}" \
        -p:ContinuousIntegrationBuild=true \
        --nologo

PACKAGE="${OUTPUT_DIR}/CSLibrary2026.${VERSION}.nupkg"
if [ ! -f "${PACKAGE}" ]; then
    echo "Expected package not found: ${PACKAGE}" >&2
    exit 1
fi

echo "Built ${PACKAGE}"

if [ "${SKIP_PUSH}" = "true" ]; then
    echo "Skipping push."
    exit 0
fi

if [ -z "${API_KEY}" ]; then
    echo "No NuGet API key. Set NUGET_API_KEY or create nuget/api-key.txt." >&2
    exit 1
fi

echo "Pushing ${PACKAGE} to https://api.nuget.org/v3/index.json"

dotnet nuget push "${PACKAGE}" \
    --api-key "${API_KEY}" \
    --source https://api.nuget.org/v3/index.json \
    --skip-duplicate \
    --no-symbols

echo "Published CSLibrary2026 ${VERSION}"
