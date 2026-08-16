using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FactoryErp.Infrastructure.Persistence.Migrations;

[DbContext(typeof(FactoryErp.Infrastructure.Persistence.FactoryErpDbContext))]
[Migration("20260816113000_AddFoundationTriggerAndSeed")]
public partial class AddFoundationTriggerAndSeed : Migration
{
    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
        // The schema model is owned by FactoryErpDbContextModelSnapshot. This migration only adds
        // deterministic database trigger and reference-data operations.
    }

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE OR REPLACE FUNCTION set_row_version_bigint()
            RETURNS trigger
            LANGUAGE plpgsql
            AS $$
            BEGIN
                NEW.row_version = OLD.row_version + 1;
                RETURN NEW;
            END;
            $$;
            """);

        migrationBuilder.Sql("""
            CREATE TRIGGER users_row_version_trigger
            BEFORE UPDATE ON users
            FOR EACH ROW
            EXECUTE FUNCTION set_row_version_bigint();
            """);

        migrationBuilder.Sql("""
            INSERT INTO roles (id, code, name, is_system_role, is_active)
            VALUES
              ('00000000-0000-0000-0000-000000000001', 'system_admin', 'Sistem yöneticisi', true, true),
              ('00000000-0000-0000-0000-000000000002', 'viewer', 'Görüntüleyici', true, true),
              ('00000000-0000-0000-0000-000000000003', 'sales', 'Satış', true, true),
              ('00000000-0000-0000-0000-000000000004', 'warehouse', 'Depo', true, true),
              ('00000000-0000-0000-0000-000000000005', 'production', 'Üretim', true, true),
              ('00000000-0000-0000-0000-000000000006', 'accounting', 'Muhasebe', true, true),
              ('00000000-0000-0000-0000-000000000007', 'hr', 'İnsan kaynakları', true, true)
            ON CONFLICT (code) DO UPDATE
              SET name = EXCLUDED.name,
                  is_system_role = EXCLUDED.is_system_role,
                  is_active = EXCLUDED.is_active;
            """);

        migrationBuilder.Sql("""
            INSERT INTO permissions (id, code, module, action, is_active)
            VALUES
              ('10000000-0000-0000-0000-000000000001', 'system.read', 'system', 'read', true),
              ('10000000-0000-0000-0000-000000000002', 'system.manage', 'system', 'manage', true),
              ('10000000-0000-0000-0000-000000000003', 'audit.read', 'audit', 'read', true),
              ('10000000-0000-0000-0000-000000000004', 'products.read', 'products', 'read', true),
              ('10000000-0000-0000-0000-000000000005', 'warehouse.read', 'warehouse', 'read', true),
              ('10000000-0000-0000-0000-000000000006', 'sales.read', 'sales', 'read', true)
            ON CONFLICT (code) DO UPDATE
              SET module = EXCLUDED.module,
                  action = EXCLUDED.action,
                  is_active = EXCLUDED.is_active;
            """);

        migrationBuilder.Sql("""
            INSERT INTO role_permissions (role_id, permission_id, assigned_at)
            SELECT r.id, p.id, now()
            FROM roles r
            CROSS JOIN permissions p
            WHERE r.code = 'system_admin'
            ON CONFLICT (role_id, permission_id) DO NOTHING;
            """);

        migrationBuilder.Sql("""
            INSERT INTO system_settings (id, key, value, value_type, updated_at)
            VALUES
              ('20000000-0000-0000-0000-000000000001', 'system.timezone', 'Europe/Istanbul', 'string', now()),
              ('20000000-0000-0000-0000-000000000002', 'schema.foundation_version', 'g1', 'string', now())
            ON CONFLICT (key) DO UPDATE
              SET value = EXCLUDED.value,
                  value_type = EXCLUDED.value_type,
                  updated_at = EXCLUDED.updated_at;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DELETE FROM role_permissions WHERE role_id = '00000000-0000-0000-0000-000000000001';");
        migrationBuilder.Sql("DELETE FROM permissions WHERE id IN ('10000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000005', '10000000-0000-0000-0000-000000000006');");
        migrationBuilder.Sql("DELETE FROM roles WHERE id IN ('00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002', '00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000004', '00000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000006', '00000000-0000-0000-0000-000000000007');");
        migrationBuilder.Sql("DELETE FROM system_settings WHERE key IN ('system.timezone', 'schema.foundation_version');");
        migrationBuilder.Sql("DROP TRIGGER IF EXISTS users_row_version_trigger ON users;");
        migrationBuilder.Sql("DROP FUNCTION IF EXISTS set_row_version_bigint();");
    }
}
