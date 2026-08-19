"use client";

import { useEffect, useState } from "react";
import { Users } from "lucide-react";
import { AppShell } from "@/components/shell/app-shell";
import { KpiMetric } from "@/components/dashboard/kpi-metric";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardBody, CardHeader, CardTitle } from "@/components/ui/card";
import { DataTable } from "@/components/ui/data-table";
import { Input } from "@/components/ui/input";
import { StatusBadge } from "@/components/ui/status-badge";
import { ApiError } from "@/lib/api/types";
import { userFacingMessage } from "@/lib/api/auth-client";
import { useSessionStore } from "@/lib/auth/session-store";
import { createEmployee, listEmployees, type EmployeeRow } from "@/lib/hr/employees";

export function EmployeeBoard() {
  const user = useSessionStore((state) => state.user);
  const permissions = user?.permissions ?? [];
  const canRead = permissions.includes("employee.read");
  const canCreate = permissions.includes("employee.create");
  const [rows, setRows] = useState<EmployeeRow[] | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [denied, setDenied] = useState(false);
  const [loading, setLoading] = useState(canRead);
  const [reload, setReload] = useState(0);
  const [fullName, setFullName] = useState("");
  const [title, setTitle] = useState("");
  const [department, setDepartment] = useState("");
  const [acting, setActing] = useState(false);
  const [actionError, setActionError] = useState<string | null>(null);

  useEffect(() => {
    if (!canRead) {
      setLoading(false);
      return;
    }
    let cancelled = false;
    setLoading(true);
    listEmployees()
      .then((result) => {
        if (!cancelled) setRows(result);
      })
      .catch((caught) => {
        if (cancelled) return;
        if (caught instanceof ApiError && caught.kind === "permission_denied") {
          setDenied(true);
          return;
        }
        setError(userFacingMessage(caught));
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [canRead, reload]);

  async function submit() {
    if (!fullName.trim()) {
      setActionError("Ad soyad zorunludur.");
      return;
    }
    setActing(true);
    setActionError(null);
    try {
      await createEmployee({
        fullName: fullName.trim(),
        title: title.trim() || undefined,
        department: department.trim() || undefined,
      });
      setFullName("");
      setTitle("");
      setDepartment("");
      setReload((value) => value + 1);
    } catch (caught) {
      setActionError(userFacingMessage(caught));
    } finally {
      setActing(false);
    }
  }

  const ready = Boolean(rows) && !loading && !error && !denied;
  const active = rows?.filter((row) => row.status === "Active").length ?? 0;

  return (
    <AppShell
      currentHref="/personel"
      breadcrumbs={[
        { label: "Çalışma alanı", href: "/dashboard" },
        { label: "Personel" },
      ]}
      pageTitle="Personel"
      pageDescription="GET /employees. Maaş/puantaj bu dilimde yoktur; uydurulmaz."
      pageActions={
        canRead ? (
          <Button variant="secondary" loading={loading} onClick={() => setReload((value) => value + 1)}>
            Yenile
          </Button>
        ) : null
      }
    >
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <KpiMetric
          label="Kayıt"
          value={ready ? String(rows?.length ?? 0) : "—"}
          icon={Users}
          tone="navy"
          unavailable={!ready}
          caption="GET /employees · pencere"
        />
        <KpiMetric
          label="Aktif"
          value={ready ? String(active) : "—"}
          icon={Users}
          tone="teal"
          unavailable={!ready}
          caption="status = Active"
        />
      </div>
      {canCreate ? (
        <Card className="mt-4">
          <CardHeader>
            <CardTitle>Yeni personel</CardTitle>
          </CardHeader>
          <CardBody className="grid gap-3 md:grid-cols-3">
            <Input label="Ad soyad" required value={fullName} onChange={(event) => setFullName(event.target.value)} />
            <Input label="Unvan" value={title} onChange={(event) => setTitle(event.target.value)} />
            <Input label="Birim" value={department} onChange={(event) => setDepartment(event.target.value)} />
            <div className="md:col-span-3">
              {actionError ? <Alert tone="danger" title="Kayıt oluşmadı">{actionError}</Alert> : null}
              <Button className="mt-2" loading={acting} onClick={() => void submit()}>
                Kaydet
              </Button>
            </div>
          </CardBody>
        </Card>
      ) : null}
      <Card className="mt-4">
        <CardBody>
          {!canRead ? (
            <PermissionDenied
              title="Personel bu oturumda görünmez"
              description="employee.read yok. Bu kontrol yalnızca görünürlük içindir; gerçek yetki backend’dedir."
            />
          ) : denied ? (
            <PermissionDenied />
          ) : error ? (
            <ErrorState title="Personel yüklenemedi" description={error} onRetry={() => setReload((value) => value + 1)} />
          ) : loading || !rows ? (
            <p className="text-sm text-slate-600">Yükleniyor…</p>
          ) : rows.length === 0 ? (
            <EmptyState title="Personel yok" description="Bu pencerede employee kaydı yok." />
          ) : (
            <DataTable
              columns={[
                { id: "code", header: "Kod", accessor: (row) => row.code },
                { id: "name", header: "Ad", accessor: (row) => row.fullName },
                { id: "title", header: "Unvan", accessor: (row) => row.title || "—" },
                { id: "dept", header: "Birim", accessor: (row) => row.department || "—" },
                {
                  id: "status",
                  header: "Durum",
                  accessor: (row) => (
                    <StatusBadge status={row.status === "Active" ? "success" : "inactive"} label={row.status} />
                  ),
                },
              ]}
              rows={rows}
              getRowId={(row) => row.id}
            />
          )}
        </CardBody>
      </Card>
    </AppShell>
  );
}
