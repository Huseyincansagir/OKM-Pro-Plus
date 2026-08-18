#!/bin/sh
set -eu

: "${PGHOST:?PGHOST is required}"
: "${PGPORT:?PGPORT is required}"
: "${PGDATABASE:?PGDATABASE is required}"
: "${PGUSER:?PGUSER is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${BACKUP_ROLE:?BACKUP_ROLE is required}"
: "${BACKUP_ROLE_PASSWORD:?BACKUP_ROLE_PASSWORD is required}"
: "${TARGET_DATABASE:?TARGET_DATABASE is required}"

psql \
  -v ON_ERROR_STOP=1 \
  -v backup_user="$BACKUP_ROLE" \
  -v backup_password="$BACKUP_ROLE_PASSWORD" \
  -v target_database="$TARGET_DATABASE" \
  --file=/opt/backup/bootstrap-backup-role.sql
