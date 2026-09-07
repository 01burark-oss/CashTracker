#!/usr/bin/env bash
set -euo pipefail

deploy_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
backup_dir="${BACKUP_DIR:-/opt/systemcel/backups}"
timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
db_dump_name="systemcel-db-${timestamp}.dump"
appdata_archive_name="systemcel-appdata-${timestamp}.tar.gz"
checksum_name="systemcel-${timestamp}.sha256"
db_dump_partial="${backup_dir}/${db_dump_name}.partial"
appdata_archive_partial="${backup_dir}/${appdata_archive_name}.partial"
checksum_partial="${backup_dir}/${checksum_name}.partial"
services_to_restart=()

cd "${deploy_dir}"

if [[ ! -f .env ]]; then
  echo "Eksik: ${deploy_dir}/.env" >&2
  exit 1
fi

command -v jq >/dev/null || {
  echo "Eksik bağımlılık: jq" >&2
  exit 1
}

compose_config="$(docker compose config --format json)"
postgres_user="$(jq -er '.services.db.environment.POSTGRES_USER' <<<"${compose_config}")"
postgres_db="$(jq -er '.services.db.environment.POSTGRES_DB' <<<"${compose_config}")"

umask 077
mkdir -p "${backup_dir}"

cleanup() {
  rm -f "${db_dump_partial}" "${appdata_archive_partial}" "${checksum_partial}"
  if [[ "${#services_to_restart[@]}" -gt 0 ]]; then
    docker compose up -d "${services_to_restart[@]}" >/dev/null
  fi
}
trap cleanup EXIT

if [[ "${1:-}" == "--quiesce" ]]; then
  for service in app caddy; do
    if docker compose ps --status running --services | grep -qx "${service}"; then
      services_to_restart+=("${service}")
    fi
  done
  docker compose stop app caddy
elif [[ "$#" -ne 0 ]]; then
  echo "Kullanım: $0 [--quiesce]" >&2
  exit 1
fi

docker compose up -d db >/dev/null
docker compose exec -T db pg_dump \
  --username "${postgres_user}" \
  --dbname "${postgres_db}" \
  --format custom \
  > "${db_dump_partial}"

docker run --rm --interactive postgres:18-alpine \
  pg_restore --list < "${db_dump_partial}" >/dev/null

docker run --rm \
  --volume systemcel_app_data:/data:ro \
  --volume "${backup_dir}:/backup" \
  alpine:3.22 \
  tar -C /data -czf "/backup/${appdata_archive_name}.partial" .

chmod 600 "${db_dump_partial}" "${appdata_archive_partial}"
tar -tzf "${appdata_archive_partial}" >/dev/null

mv "${db_dump_partial}" "${backup_dir}/${db_dump_name}"
mv "${appdata_archive_partial}" "${backup_dir}/${appdata_archive_name}"

(
  cd "${backup_dir}"
  sha256sum "${db_dump_name}" "${appdata_archive_name}" > "${checksum_name}.partial"
  sha256sum --check --strict "${checksum_name}.partial"
  mv "${checksum_name}.partial" "${checksum_name}"
  chmod 600 "${checksum_name}"
  if [[ "${EUID}" -eq 0 ]]; then
    chown --reference="${backup_dir}" "${db_dump_name}" "${appdata_archive_name}" "${checksum_name}"
  fi
)

find "${backup_dir}" -type f -name 'systemcel-*' -mtime +14 -delete
echo "Yedek tamamlandı ve doğrulandı: ${timestamp}"
