#!/usr/bin/env bash
set -euo pipefail
task_root="$(cd "$(dirname "$0")/.." && pwd)"
cd "$task_root"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
task_dotnet="${DISCOVERY_DOTNET:-$task_root/work/dotnet/dotnet}"
if [[ ! -x "$task_dotnet" ]]; then
  echo 'Install the pinned .NET SDK, or run python scripts/bootstrap-sdk.py.' >&2
  exit 1
fi
"$task_dotnet" restore DiscoveryRows.sln --locked-mode
"$task_dotnet" build DiscoveryRows.sln --no-restore -c Release
"$task_dotnet" test tests/DiscoveryRows.Tests --no-build --no-restore -c Release
python scripts/package.py
