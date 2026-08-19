import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { DataTable, type DataTableColumn } from "@/components/ui/data-table";

type SampleRow = {
  id: string;
  name: string;
};

const columns: DataTableColumn<SampleRow>[] = [
  {
    id: "name",
    header: "Kayıt",
    accessor: (row) => row.name,
    sortable: true,
  },
];

const rows: SampleRow[] = [
  { id: "ornek-1", name: "Örnek kalem A" },
  { id: "ornek-2", name: "Örnek kalem B" },
];

describe("DataTable", () => {
  it("renders loading skeletons from supplied rows and does not fetch", () => {
    const { container } = render(
      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        loading
      />,
    );

    expect(screen.getByRole("table")).toBeInTheDocument();
    expect(screen.queryByText("Örnek kalem A")).not.toBeInTheDocument();
    expect(container.querySelectorAll(".animate-pulse").length).toBeGreaterThan(0);
  });

  it("renders the empty state when there are no rows", () => {
    render(
      <DataTable
        columns={columns}
        rows={[]}
        getRowId={(row) => row.id}
        emptyTitle="Kayıt yok"
        emptyDescription="Bu listede gösterilecek kayıt bulunmuyor."
      />,
    );

    expect(screen.getByText("Kayıt yok")).toBeInTheDocument();
    expect(
      screen.getByText("Bu listede gösterilecek kayıt bulunmuyor."),
    ).toBeInTheDocument();
  });

  it("renders the error state from the supplied message", () => {
    render(
      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        error="Bağlantı kesildi."
      />,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText("Tablo yüklenemedi")).toBeInTheDocument();
    expect(screen.getByText("Bağlantı kesildi.")).toBeInTheDocument();
    expect(screen.queryByText("Örnek kalem A")).not.toBeInTheDocument();
  });

  it("notifies the parent when a sortable header is clicked", async () => {
    const user = userEvent.setup();
    const onSortChange = vi.fn();

    render(
      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        sort={{ id: "name", direction: "asc" }}
        onSortChange={onSortChange}
      />,
    );

    await user.click(screen.getByRole("button", { name: "Kayıt" }));

    expect(onSortChange).toHaveBeenCalledTimes(1);
    expect(onSortChange).toHaveBeenCalledWith({ id: "name", direction: "desc" });
  });

  it("notifies the parent when a row is selected", async () => {
    const user = userEvent.setup();
    const onSelectionChange = vi.fn();

    render(
      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        selectedIds={[]}
        onSelectionChange={onSelectionChange}
      />,
    );

    await user.click(screen.getByRole("checkbox", { name: "ornek-1 satırını seç" }));

    expect(onSelectionChange).toHaveBeenCalledTimes(1);
    expect(onSelectionChange).toHaveBeenCalledWith(["ornek-1"]);
  });

  it("notifies the parent when pagination changes", async () => {
    const user = userEvent.setup();
    const onPageChange = vi.fn();

    render(
      <DataTable
        columns={columns}
        rows={rows}
        getRowId={(row) => row.id}
        page={1}
        pageSize={1}
        totalCount={2}
        onPageChange={onPageChange}
      />,
    );

    expect(screen.getByText("Sayfa 1 / 2")).toBeInTheDocument();
    await user.click(screen.getByRole("button", { name: "Sonraki" }));
    expect(onPageChange).toHaveBeenCalledWith(2);
  });
});
