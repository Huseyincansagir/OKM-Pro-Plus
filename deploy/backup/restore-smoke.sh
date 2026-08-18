#!/bin/sh
set -eu

: "${PGHOST:?PGHOST is required}"
: "${PGPORT:?PGPORT is required}"
: "${PGUSER:?PGUSER is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${SOURCE_DATABASE:?SOURCE_DATABASE is required}"
: "${BACKUP_FILE:?BACKUP_FILE is required}"
: "${RESTORE_DATABASE:=factory_erp_restore_smoke}"

checksum_file="${BACKUP_FILE}.sha256"
[ -f "$BACKUP_FILE" ] || { echo "Backup file does not exist: $BACKUP_FILE" >&2; exit 1; }
[ -f "$checksum_file" ] || { echo "Checksum file does not exist: $checksum_file" >&2; exit 1; }
sha256sum --check --status "$checksum_file"

admin_db="${PGDATABASE:-postgres}"

psql_admin() {
  PGDATABASE="$admin_db" psql -v ON_ERROR_STOP=1 "$@"
}

psql_admin -v restore_database="$RESTORE_DATABASE" <<'SQL'
SELECT pg_terminate_backend(pid)
FROM pg_stat_activity
WHERE datname = :'restore_database'
  AND pid <> pg_backend_pid();
SQL

psql_admin -v restore_database="$RESTORE_DATABASE" -v source_database="$SOURCE_DATABASE" <<'SQL'
DROP DATABASE IF EXISTS :"restore_database";
CREATE DATABASE :"restore_database" TEMPLATE template0;
SQL

PGDATABASE="$RESTORE_DATABASE" pg_restore \
  --exit-on-error \
  --no-owner \
  --no-privileges \
  --dbname="$RESTORE_DATABASE" \
  "$BACKUP_FILE"

PGDATABASE="$RESTORE_DATABASE" psql -v ON_ERROR_STOP=1 -Atc "SELECT current_database(); SELECT to_regclass('public.\"__EFMigrationsHistory\"'); SELECT count(*) FROM public.\"__EFMigrationsHistory\";"

printf 'restore_database=%s\n' "$RESTORE_DATABASE"
printf 'backup_file=%s\n' "$BACKUP_FILE"
