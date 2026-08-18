#!/bin/sh
set -eu

: "${PGHOST:?PGHOST is required}"
: "${PGPORT:?PGPORT is required}"
: "${PGDATABASE:?PGDATABASE is required}"
: "${PGUSER:?PGUSER is required}"
: "${PGPASSWORD:?PGPASSWORD is required}"
: "${RESTORE_ROLE:?RESTORE_ROLE is required}"
: "${RESTORE_ROLE_PASSWORD:?RESTORE_ROLE_PASSWORD is required}"
: "${TARGET_DATABASE:?TARGET_DATABASE is required}"

psql \
  -v ON_ERROR_STOP=1 \
  -v restore_user="$RESTORE_ROLE" \
  -v restore_password="$RESTORE_ROLE_PASSWORD" \
  -v target_database="$TARGET_DATABASE" \
  --file=/opt/backup/bootstrap-restore-role.sql
