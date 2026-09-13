#!/usr/bin/env bash
set -euo pipefail
DIR="$(cd "$(dirname "$0")/.." && pwd)"

docker run --rm \
  -v "$DIR":/src \
  -w /src \
  mcr.microsoft.com/dotnet/sdk:10.0 \
  dotnet test --nologo "$@"
