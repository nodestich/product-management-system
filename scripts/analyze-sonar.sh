#!/usr/bin/env bash
#
# Builds and tests the backend, then publishes the analysis to SonarQube.
# Runs inside the sonar-scan image; the host only needs Docker.
#
#   docker compose --profile sonar run --rm sonar-scan
#
set -euo pipefail

PROJECT_KEY="${PROJECT_KEY:-product-management-system}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://sonar:9000}"
# Where a human can reach the UI; SONAR_HOST_URL is the container-internal address.
SONAR_PUBLIC_URL="${SONAR_PUBLIC_URL:-http://localhost:9000}"
RESULTS_DIR=/src/backend/TestResults

# Generated code and the composition root carry no analysable logic.
EXCLUSIONS='**/Migrations/**,**/*Designer.cs,**/AppDbContextModelSnapshot.cs,**/Program.cs'

cd /src/backend

if [ -z "${SONAR_TOKEN:-}" ]; then
    echo "SONAR_TOKEN is not set. Add it to .env (copy .env.example) - generate one at ${SONAR_PUBLIC_URL}/account/security." >&2
    exit 1
fi

# Reports must reflect this run only, otherwise the wildcard below picks up stale files.
# The directory itself is a bind mount, so clear its contents rather than removing it.
mkdir -p "$RESULTS_DIR"
find "$RESULTS_DIR" -mindepth 1 -delete

echo "==> Waiting for SonarQube at ${SONAR_HOST_URL}"
status_url="${SONAR_HOST_URL%/}/api/system/status"
for attempt in $(seq 1 60); do
    status=$(curl -fsS "$status_url" 2>/dev/null | sed -n 's/.*"status":"\([A-Z]*\)".*/\1/p') || status=""
    if [ "$status" = "UP" ]; then
        echo "    SonarQube is UP"
        break
    fi

    if [ "$attempt" -eq 60 ]; then
        echo "SonarQube never reported UP at $status_url." >&2
        exit 1
    fi

    echo "    status is ${status:-unreachable} ($attempt/60)"
    sleep 5
done

echo "==> Starting the SonarQube analysis"
dotnet sonarscanner begin \
    "/k:${PROJECT_KEY}" \
    "/n:Product Management System" \
    "/d:sonar.host.url=${SONAR_HOST_URL}" \
    "/d:sonar.token=${SONAR_TOKEN}" \
    "/d:sonar.cs.cobertura.reportsPaths=${RESULTS_DIR}/**/coverage.cobertura.xml" \
    "/d:sonar.exclusions=${EXCLUSIONS}" \
    "/d:sonar.coverage.exclusions=${EXCLUSIONS}" \
    "/d:sonar.qualitygate.wait=true"

echo "==> Building the solution"
dotnet build ProductManagement.sln --configuration Release --no-incremental --disable-build-servers

run_tests() {
    local name=$1
    local project=$2

    echo "==> Running $name"
    dotnet test "$project" \
        --configuration Release \
        --no-build \
        --collect:"XPlat Code Coverage" \
        --results-directory "$RESULTS_DIR/$name"
}

run_tests unit tests/ProductManagement.UnitTests/ProductManagement.UnitTests.csproj
run_tests integration tests/ProductManagement.IntegrationTests/ProductManagement.IntegrationTests.csproj
run_tests specs tests/ProductManagement.Specs/ProductManagement.Specs.csproj

echo "==> Completing the analysis and waiting for the quality gate"
dotnet sonarscanner end "/d:sonar.token=${SONAR_TOKEN}"

echo
echo "Dashboard: ${SONAR_PUBLIC_URL}/dashboard?id=${PROJECT_KEY}"
