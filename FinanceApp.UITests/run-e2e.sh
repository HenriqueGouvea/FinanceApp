#!/usr/bin/env bash
# Runs the E2E suite (or one shard of it) and reruns the failed tests once, so a flaky failure is told apart from a
# real one: a test that passes on the rerun is reported as a warning, one that fails again fails the run.
#
# Usage: FinanceApp.UITests/run-e2e.sh [--shard INDEX COUNT] [--filter "Category=Smoke"]
#   --shard   runs only the test classes of shard INDEX (1-based) out of COUNT, balanced by test count.
#   --filter  extra dotnet test filter, e.g. the PR smoke set.
# Expects the tests to be built already (dotnet build FinanceApp.UITests/FinanceApp.UITests.csproj).
set -uo pipefail

cd "$(dirname "$0")/.."
project=FinanceApp.UITests/FinanceApp.UITests.csproj
results=TestResults

shard_index=1
shard_count=1
extra_filter=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --shard) shard_index=$2; shard_count=$3; shift 3 ;;
    --filter) extra_filter=$2; shift 2 ;;
    *) echo "Unknown argument: $1" >&2; exit 2 ;;
  esac
done

# Classes are dealt to the shard with the fewest tests so far, biggest classes first, so shards take about as long.
shard_classes() {
  for file in FinanceApp.UITests/Tests/*Tests.cs; do
    echo "$(grep -c '\[Fact\]\|\[Theory\]' "$file") $(basename "$file" .cs)"
  done | sort -k1,1nr -k2,2 | awk -v index_="$shard_index" -v count="$shard_count" '
    {
      best = 1
      for (s = 2; s <= count; s++) if (total[s] < total[best]) best = s
      total[best] += $1
      if (best == index_) print $2
    }'
}

filter=""
if [[ $shard_count -gt 1 ]]; then
  for class in $(shard_classes); do
    filter+="${filter:+|}FullyQualifiedName~FinanceApp.UITests.Tests.$class."
  done
  echo "Shard $shard_index/$shard_count: ${filter//FullyQualifiedName~FinanceApp.UITests.Tests./}"
fi
if [[ -n $extra_filter ]]; then
  filter="${filter:+($filter)&}$extra_filter"
fi

run_tests() {
  local trx=$1 test_filter=$2
  dotnet test "$project" --no-build --filter "$test_filter" \
    --logger "trx;LogFileName=$trx" --logger "console;verbosity=normal" --results-directory "$results"
}

run_tests e2e.trx "${filter:-Category=E2E}" && exit 0

# The TRX lists each failed test by its fully qualified name.
mapfile -t failed < <(grep -o '<UnitTestResult [^>]*outcome="Failed"' "$results/e2e.trx" 2>/dev/null \
  | sed -n 's/.*testName="\([^"]*\)".*/\1/p' | sed 's/(.*//' | sort -u)
if [[ ${#failed[@]} -eq 0 ]]; then
  echo "The run failed before any test failed (see the log above), so there is nothing to rerun." >&2
  exit 1
fi

echo "Rerunning ${#failed[@]} failed test(s) once: ${failed[*]}"
rerun_filter=$(printf 'FullyQualifiedName=%s|' "${failed[@]}")
if run_tests e2e-rerun.trx "${rerun_filter%|}"; then
  for test in "${failed[@]}"; do
    echo "::warning title=Flaky E2E test::$test failed, then passed on the rerun."
  done
  exit 0
fi
exit 1
