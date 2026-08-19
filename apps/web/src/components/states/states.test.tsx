import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { EmptyState } from "@/components/states/empty-state";
import { ErrorState } from "@/components/states/error-state";
import { PermissionDenied } from "@/components/states/permission-denied";

describe("state components", () => {
  it("renders empty, error and permission denied copy", () => {
    render(
      <>
        <EmptyState title="Kayıt yok" description="Henüz sipariş yok." />
        <ErrorState title="Yüklenemedi" description="Bağlantı kesildi." />
        <PermissionDenied />
      </>,
    );

    expect(screen.getByText("Kayıt yok")).toBeInTheDocument();
    expect(screen.getByText("Henüz sipariş yok.")).toBeInTheDocument();
    expect(screen.getByText("Yüklenemedi")).toBeInTheDocument();
    expect(screen.getByText("Bu işlem için yetkiniz yok")).toBeInTheDocument();
  });
});
