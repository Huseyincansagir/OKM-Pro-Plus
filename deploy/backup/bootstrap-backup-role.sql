SELECT CASE
    WHEN EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'backup_user')
        THEN format('ALTER ROLE %I LOGIN PASSWORD %L', :'backup_user', :'backup_password')
    ELSE format('CREATE ROLE %I LOGIN PASSWORD %L', :'backup_user', :'backup_password')
END;
\gexec

SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'target_database', :'backup_user');
\gexec

\connect :target_database

SELECT format('GRANT USAGE ON SCHEMA public TO %I', :'backup_user');
\gexec
SELECT format('GRANT SELECT ON ALL TABLES IN SCHEMA public TO %I', :'backup_user');
\gexec
SELECT format('GRANT SELECT ON ALL SEQUENCES IN SCHEMA public TO %I', :'backup_user');
\gexec
SELECT format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON TABLES TO %I', :'backup_user');
\gexec
SELECT format('ALTER DEFAULT PRIVILEGES IN SCHEMA public GRANT SELECT ON SEQUENCES TO %I', :'backup_user');
\gexec
