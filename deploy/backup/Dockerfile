FROM postgres:16.4

COPY backup.sh /usr/local/bin/factory-erp-backup
COPY restore-smoke.sh /usr/local/bin/factory-erp-restore-smoke
COPY bootstrap-backup-role.sh /usr/local/bin/factory-erp-bootstrap-backup-role
COPY bootstrap-backup-role.sql /opt/backup/bootstrap-backup-role.sql
COPY bootstrap-restore-role.sh /usr/local/bin/factory-erp-bootstrap-restore-role
COPY bootstrap-restore-role.sql /opt/backup/bootstrap-restore-role.sql

RUN chmod 0755 \
      /usr/local/bin/factory-erp-backup \
      /usr/local/bin/factory-erp-restore-smoke \
      /usr/local/bin/factory-erp-bootstrap-backup-role \
      /usr/local/bin/factory-erp-bootstrap-restore-role

USER postgres
