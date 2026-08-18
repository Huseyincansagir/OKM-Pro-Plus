SELECT CASE
    WHEN EXISTS (SELECT 1 FROM pg_roles WHERE rolname = :'restore_user')
        THEN format('ALTER ROLE %I LOGIN CREATEDB PASSWORD %L', :'restore_user', :'restore_password')
    ELSE format('CREATE ROLE %I LOGIN CREATEDB PASSWORD %L', :'restore_user', :'restore_password')
END;
\gexec

SELECT format('GRANT CONNECT ON DATABASE %I TO %I', :'target_database', :'restore_user');
\gexec
