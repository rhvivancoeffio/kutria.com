#!/usr/bin/env bash
# Stop Commerce Postgres containers and remove related volumes (compose + Aspire).
set -euo pipefail

echo "Stopping Commerce / Aspire Postgres containers..."
ids=$(docker ps -aq --filter 'name=commerce' --filter 'name=commerce-postgres' --filter 'name=Commerce-Postgres' 2>/dev/null || true)
# Broader match for Aspire-generated names
ids2=$(docker ps -aq --filter 'name=commerce-postgres' 2>/dev/null || true)
all_ids=$(echo "$ids $ids2" | tr ' ' '\n' | sort -u | tr '\n' ' ' | xargs echo || true)
if [ -n "${all_ids// }" ]; then
  # Prefer compose down when available
  if [ -f docker-compose.yml ] || [ -f compose.yml ]; then
    docker compose down --remove-orphans 2>/dev/null || true
  fi
  docker stop $all_ids 2>/dev/null || true
  docker rm $all_ids 2>/dev/null || true
else
  echo "No matching Commerce postgres containers running."
  if [ -f docker-compose.yml ] || [ -f compose.yml ]; then
    docker compose down --remove-orphans 2>/dev/null || true
  fi
fi

echo "Removing postgres volumes (commerce / aspire)..."
vols=$(docker volume ls -q | grep -Ei 'commerce|postgres' || true)
if [ -n "$vols" ]; then
  for v in $vols; do
    echo "Removing volume $v"
    docker volume rm "$v" || true
  done
else
  echo "No matching volumes found."
  docker volume ls | grep -Ei 'postgres|commerce' || true
fi

echo "Postgres clean complete."
