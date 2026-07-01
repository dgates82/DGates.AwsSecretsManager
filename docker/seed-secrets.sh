#!/usr/bin/env bash
# Seeds LocalStack Secrets Manager with test secrets for local dev and integration tests.
# Requires the AWS CLI. Run after `docker compose up -d` and a healthy LocalStack.
#
# Usage: ./docker/seed-secrets.sh

set -euo pipefail

ENDPOINT="http://localhost:4566"
REGION="us-west-2"

export AWS_ACCESS_KEY_ID=test
export AWS_SECRET_ACCESS_KEY=test
export AWS_DEFAULT_REGION="$REGION"

create_or_update_secret() {
  local name="$1"
  local value="$2"

  if aws --endpoint-url="$ENDPOINT" secretsmanager describe-secret --secret-id "$name" >/dev/null 2>&1; then
    echo "Updating existing secret: $name"
    aws --endpoint-url="$ENDPOINT" secretsmanager put-secret-value \
      --secret-id "$name" --secret-string "$value" >/dev/null
  else
    echo "Creating secret: $name"
    aws --endpoint-url="$ENDPOINT" secretsmanager create-secret \
      --name "$name" --secret-string "$value" >/dev/null
  fi
}

create_or_update_secret "test/SampleSecret" '{"Value":"local-dev-value"}'
create_or_update_secret "myapp/ApiKey" '{"ApiKey":"local-dev-api-key-12345"}'

echo "Done. Seeded secrets are available at $ENDPOINT (region $REGION)."
