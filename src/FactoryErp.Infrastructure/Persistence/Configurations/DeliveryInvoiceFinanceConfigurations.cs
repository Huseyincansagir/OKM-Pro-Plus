using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class DeliveryNoteRecordConfiguration : IEntityTypeConfiguration<DeliveryNoteRecord>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteRecord> builder)
    {
        builder.ToTable("delivery_notes", table =>
        {
            table.HasCheckConstraint("ck_delivery_notes_status", "status in ('Draft', 'Prepared', 'ReadyToIssue', 'Issued', 'Reversed', 'Closed')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DocumentNumber).HasColumnName("document_number").HasMaxLength(80).IsRequired();
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id");
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamptz");
        builder.Property(x => x.IssuedBy).HasColumnName("issued_by");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.DocumentNumber).IsUnique();
        builder.HasIndex(x => new { x.SalesOrderId, x.Status });
        builder.HasOne<SalesOrderRecord>().WithMany().HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.IssuedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeliveryNoteItemRecordConfiguration : IEntityTypeConfiguration<DeliveryNoteItemRecord>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteItemRecord> builder)
    {
        builder.ToTable("delivery_note_items", table =>
        {
            table.HasCheckConstraint("ck_delivery_note_items_quantity_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_delivery_note_items_components_non_negative", "shipped_qty >= 0 and invoiced_qty >= 0 and waived_qty >= 0");
            table.HasCheckConstraint("ck_delivery_note_items_invoiced_within_shipped", "invoiced_qty + waived_qty <= shipped_qty");
            table.HasCheckConstraint("ck_delivery_note_items_remaining_projection", "remaining_to_invoice = shipped_qty - invoiced_qty - waived_qty");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DeliveryNoteId).HasColumnName("delivery_note_id");
        builder.Property(x => x.SalesOrderItemId).HasColumnName("sales_order_item_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 6);
        builder.Property(x => x.EnteredPackagingId).HasColumnName("entered_packaging_id");
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.ShippedQty).HasColumnName("shipped_qty").HasPrecision(18, 6);
        builder.Property(x => x.InvoicedQty).HasColumnName("invoiced_qty").HasPrecision(18, 6);
        builder.Property(x => x.WaivedQty).HasColumnName("waived_qty").HasPrecision(18, 6);
        builder.Property(x => x.RemainingToInvoice).HasColumnName("remaining_to_invoice").HasPrecision(18, 6);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.DeliveryNoteId, x.SalesOrderItemId }).IsUnique();
        builder.HasOne(x => x.DeliveryNote).WithMany(x => x.Items).HasForeignKey(x => x.DeliveryNoteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesOrderItemRecord>().WithMany().HasForeignKey(x => x.SalesOrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductPackagingRecord>().WithMany().HasForeignKey(x => x.EnteredPackagingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class DeliveryNoteItemAllocationRecordConfiguration : IEntityTypeConfiguration<DeliveryNoteItemAllocationRecord>
{
    public void Configure(EntityTypeBuilder<DeliveryNoteItemAllocationRecord> builder)
    {
        builder.ToTable("delivery_note_item_allocations", table =>
        {
            table.HasCheckConstraint("ck_delivery_note_allocations_quantity_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_delivery_note_allocations_kind", "allocation_kind in ('Original', 'Reversal')");
            table.HasCheckConstraint("ck_delivery_note_allocations_status", "status in ('Active', 'Reversed', 'Voided')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalesOrderItemId).HasColumnName("sales_order_item_id");
        builder.Property(x => x.DeliveryNoteItemId).HasColumnName("delivery_note_item_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.BaseUomId).HasColumnName("base_uom_id");
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AllocationKind).HasColumnName("allocation_kind").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReversedFromId).HasColumnName("reversed_from_id");
        builder.Property(x => x.ReversalReason).HasColumnName("reversal_reason").HasColumnType("text");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => x.IdempotencyKey)
            .HasDatabaseName("ix_delivery_allocation_idempotency_key");
        builder.HasIndex(x => new { x.SalesOrderItemId, x.DeliveryNoteItemId })
            .IsUnique()
            .HasDatabaseName("ux_delivery_allocation_active_target")
            .HasFilter("status = 'Active' AND allocation_kind = 'Original'");
        builder.HasIndex(x => new { x.SalesOrderItemId, x.Status })
            .HasDatabaseName("ix_delivery_allocation_source_status");
        builder.HasIndex(x => new { x.DeliveryNoteItemId, x.Status })
            .HasDatabaseName("ix_delivery_allocation_target_status");
        builder.HasOne<SalesOrderItemRecord>().WithMany().HasForeignKey(x => x.SalesOrderItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryNoteItemRecord>().WithMany().HasForeignKey(x => x.DeliveryNoteItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasureRecord>().WithMany().HasForeignKey(x => x.BaseUomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryNoteItemAllocationRecord>().WithMany().HasForeignKey(x => x.ReversedFromId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TaxCodeRecordConfiguration : IEntityTypeConfiguration<TaxCodeRecord>
{
    public void Configure(EntityTypeBuilder<TaxCodeRecord> builder)
    {
        builder.ToTable("tax_codes", table =>
        {
            table.HasCheckConstraint("ck_tax_codes_rate", "rate >= 0 and rate <= 1");
            table.HasCheckConstraint("ck_tax_codes_valid_window", "valid_to is null or valid_to > valid_from");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(120).IsRequired();
        builder.Property(x => x.Rate).HasColumnName("rate").HasPrecision(9, 6);
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamptz");
        builder.Property(x => x.ValidTo).HasColumnName("valid_to").HasColumnType("timestamptz");
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class InvoiceRecordConfiguration : IEntityTypeConfiguration<InvoiceRecord>
{
    public void Configure(EntityTypeBuilder<InvoiceRecord> builder)
    {
        builder.ToTable("invoices", table =>
        {
            table.HasCheckConstraint("ck_invoices_status", "status in ('Draft', 'ReadyToIssue', 'Issued', 'PartiallyPaid', 'Paid', 'Reversed', 'Credited')");
            table.HasCheckConstraint("ck_invoices_totals_non_negative", "subtotal >= 0 and tax_total >= 0 and grand_total >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.InvoiceNumber).HasColumnName("invoice_number").HasMaxLength(80).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(18, 2);
        builder.Property(x => x.TaxTotal).HasColumnName("tax_total").HasPrecision(18, 2);
        builder.Property(x => x.GrandTotal).HasColumnName("grand_total").HasPrecision(18, 2);
        builder.Property(x => x.TaxSnapshot).HasColumnName("tax_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamptz");
        builder.Property(x => x.IssuedBy).HasColumnName("issued_by");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.InvoiceNumber).IsUnique();
        builder.HasIndex(x => new { x.CustomerId, x.Status, x.CreatedAt });
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.IssuedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoiceItemRecordConfiguration : IEntityTypeConfiguration<InvoiceItemRecord>
{
    public void Configure(EntityTypeBuilder<InvoiceItemRecord> builder)
    {
        builder.ToTable("invoice_items", table =>
        {
            table.HasCheckConstraint("ck_invoice_items_quantity_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_invoice_items_amounts_non_negative", "unit_price >= 0 and line_total >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        builder.Property(x => x.DeliveryNoteItemId).HasColumnName("delivery_note_item_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 6);
        builder.Property(x => x.EnteredPackagingId).HasColumnName("entered_packaging_id");
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.TaxCodeId).HasColumnName("tax_code_id");
        builder.Property(x => x.TaxSnapshot).HasColumnName("tax_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.LineTotal).HasColumnName("line_total").HasPrecision(18, 2);
        builder.HasIndex(x => new { x.InvoiceId, x.DeliveryNoteItemId }).IsUnique();
        builder.HasOne(x => x.Invoice).WithMany(x => x.Items).HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<DeliveryNoteItemRecord>().WithMany().HasForeignKey(x => x.DeliveryNoteItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductPackagingRecord>().WithMany().HasForeignKey(x => x.EnteredPackagingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TaxCodeRecord>().WithMany().HasForeignKey(x => x.TaxCodeId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class InvoiceItemAllocationRecordConfiguration : IEntityTypeConfiguration<InvoiceItemAllocationRecord>
{
    public void Configure(EntityTypeBuilder<InvoiceItemAllocationRecord> builder)
    {
        builder.ToTable("invoice_item_allocations", table =>
        {
            table.HasCheckConstraint("ck_invoice_allocations_quantity_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_invoice_allocations_kind", "allocation_kind in ('Original', 'Reversal')");
            table.HasCheckConstraint("ck_invoice_allocations_status", "status in ('Active', 'Reversed', 'Voided')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.DeliveryNoteItemId).HasColumnName("delivery_note_item_id");
        builder.Property(x => x.InvoiceItemId).HasColumnName("invoice_item_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.BaseUomId).HasColumnName("base_uom_id");
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PriceSnapshot).HasColumnName("price_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.TaxSnapshot).HasColumnName("tax_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.AllocationKind).HasColumnName("allocation_kind").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(160).IsRequired();
        builder.Property(x => x.PayloadHash).HasColumnName("payload_hash").HasMaxLength(128).IsRequired();
        builder.Property(x => x.CreditedFromId).HasColumnName("credited_from_id");
        builder.Property(x => x.CreditReason).HasColumnName("credit_reason").HasColumnType("text");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => x.IdempotencyKey)
            .HasDatabaseName("ix_invoice_allocation_idempotency_key");
        builder.HasIndex(x => new { x.DeliveryNoteItemId, x.InvoiceItemId })
            .IsUnique()
            .HasDatabaseName("ux_invoice_allocation_active_target")
            .HasFilter("status = 'Active' AND allocation_kind = 'Original'");
        builder.HasIndex(x => new { x.DeliveryNoteItemId, x.Status })
            .HasDatabaseName("ix_invoice_allocation_source_status");
        builder.HasIndex(x => new { x.InvoiceItemId, x.Status })
            .HasDatabaseName("ix_invoice_allocation_target_status");
        builder.HasOne<DeliveryNoteItemRecord>().WithMany().HasForeignKey(x => x.DeliveryNoteItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InvoiceItemRecord>().WithMany().HasForeignKey(x => x.InvoiceItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasureRecord>().WithMany().HasForeignKey(x => x.BaseUomId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InvoiceItemAllocationRecord>().WithMany().HasForeignKey(x => x.CreditedFromId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CurrentAccountRecordConfiguration : IEntityTypeConfiguration<CurrentAccountRecord>
{
    public void Configure(EntityTypeBuilder<CurrentAccountRecord> builder)
    {
        builder.ToTable("current_accounts", table =>
        {
            table.HasCheckConstraint("ck_current_accounts_totals_non_negative", "debit_total >= 0 and credit_total >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.DebitTotal).HasColumnName("debit_total").HasPrecision(18, 2);
        builder.Property(x => x.CreditTotal).HasColumnName("credit_total").HasPrecision(18, 2);
        builder.Property(x => x.Balance).HasColumnName("balance").HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => x.CustomerId).IsUnique();
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CurrentTransactionRecordConfiguration : IEntityTypeConfiguration<CurrentTransactionRecord>
{
    public void Configure(EntityTypeBuilder<CurrentTransactionRecord> builder)
    {
        builder.ToTable("current_transactions", table =>
        {
            table.HasCheckConstraint("ck_current_transactions_amounts_non_negative", "debit_amount >= 0 and credit_amount >= 0");
            table.HasCheckConstraint("ck_current_transactions_one_side", "(debit_amount > 0 and credit_amount = 0) or (credit_amount > 0 and debit_amount = 0)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CurrentAccountId).HasColumnName("current_account_id");
        builder.Property(x => x.TransactionType).HasColumnName("transaction_type").HasMaxLength(40).IsRequired();
        builder.Property(x => x.DebitAmount).HasColumnName("debit_amount").HasPrecision(18, 2);
        builder.Property(x => x.CreditAmount).HasColumnName("credit_amount").HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.SourceEntityType).HasColumnName("source_entity_type").HasMaxLength(120).IsRequired();
        builder.Property(x => x.SourceEntityId).HasColumnName("source_entity_id");
        builder.Property(x => x.IdempotencyKey).HasColumnName("idempotency_key").HasMaxLength(160);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.SourceEntityType, x.SourceEntityId, x.IdempotencyKey }).IsUnique().HasFilter("idempotency_key is not null");
        builder.HasOne<CurrentAccountRecord>().WithMany().HasForeignKey(x => x.CurrentAccountId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentMethodRecordConfiguration : IEntityTypeConfiguration<PaymentMethodRecord>
{
    public void Configure(EntityTypeBuilder<PaymentMethodRecord> builder)
    {
        builder.ToTable("payment_methods");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(40).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class PaymentRecordConfiguration : IEntityTypeConfiguration<PaymentRecord>
{
    public void Configure(EntityTypeBuilder<PaymentRecord> builder)
    {
        builder.ToTable("payments", table =>
        {
            table.HasCheckConstraint("ck_payments_amount_positive", "amount > 0");
            table.HasCheckConstraint("ck_payments_status", "status in ('Draft', 'Applied', 'Reversed')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.PaymentMethodId).HasColumnName("payment_method_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(20).IsRequired();
        builder.Property(x => x.Reference).HasColumnName("reference").HasMaxLength(160);
        builder.Property(x => x.AppliedAt).HasColumnName("applied_at").HasColumnType("timestamptz");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.CustomerId, x.Status, x.AppliedAt });
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PaymentMethodRecord>().WithMany().HasForeignKey(x => x.PaymentMethodId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PaymentAllocationRecordConfiguration : IEntityTypeConfiguration<PaymentAllocationRecord>
{
    public void Configure(EntityTypeBuilder<PaymentAllocationRecord> builder)
    {
        builder.ToTable("payment_allocations", table =>
        {
            table.HasCheckConstraint("ck_payment_allocations_amount_positive", "amount > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PaymentId).HasColumnName("payment_id");
        builder.Property(x => x.InvoiceId).HasColumnName("invoice_id");
        builder.Property(x => x.Amount).HasColumnName("amount").HasPrecision(18, 2);
        builder.HasIndex(x => new { x.PaymentId, x.InvoiceId }).IsUnique();
        builder.HasOne<PaymentRecord>().WithMany().HasForeignKey(x => x.PaymentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InvoiceRecord>().WithMany().HasForeignKey(x => x.InvoiceId).OnDelete(DeleteBehavior.Restrict);
    }
}
