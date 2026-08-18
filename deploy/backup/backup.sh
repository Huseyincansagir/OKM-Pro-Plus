#!/bin/sh
set -eu

: "${PGHOST:?PGHOST is required}"
: "${PGPORT:?PGPORT is required}"
: "${PGDATABASE:?PGDATABASE is required}"
: "${PGUSER:?PGUSER is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${BACKUP_ROOT:=/var/backups/factory-erp}"
: "${BACKUP_RETENTION_DAYS:=14}"
: "${BACKUP_PREFIX:=factory_erp}"

case "$BACKUP_RETENTION_DAYS" in
  ''|*[!0-9]*) echo "BACKUP_RETENTION_DAYS must be a non-negative integer" >&2; exit 2 ;;
esac

mkdir -p "$BACKUP_ROOT"

stamp="$(date -u +%Y%m%dT%H%M%SZ)"
backup_file="$BACKUP_ROOT/${BACKUP_PREFIX}_${stamp}.dump"
checksum_file="$backup_file.sha256"

pg_dump \
  --format=custom \
  --no-owner \
  --no-privileges \
  --file="$backup_file" \
  "$PGDATABASE"

sha256sum "$backup_file" > "$checksum_file"
sha256sum --check --status "$checksum_file"
ln -sfn "$(basename "$backup_file")" "$BACKUP_ROOT/latest.dump"
ln -sfn "$(basename "$checksum_file")" "$BACKUP_ROOT/latest.dump.sha256"

find "$BACKUP_ROOT" -maxdepth 1 -type f -name "${BACKUP_PREFIX}_*.dump" -mtime +"$BACKUP_RETENTION_DAYS" -delete
find "$BACKUP_ROOT" -maxdepth 1 -type f -name "${BACKUP_PREFIX}_*.dump.sha256" -mtime +"$BACKUP_RETENTION_DAYS" -delete

printf 'backup_file=%s\n' "$backup_file"
printf 'checksum_file=%s\n' "$checksum_file"
printf 'retention_days=%s\n' "$BACKUP_RETENTION_DAYS"
