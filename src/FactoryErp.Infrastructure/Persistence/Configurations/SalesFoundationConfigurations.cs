using FactoryErp.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FactoryErp.Infrastructure.Persistence.Configurations;

public sealed class CustomerRecordConfiguration : IEntityTypeConfiguration<CustomerRecord>
{
    public void Configure(EntityTypeBuilder<CustomerRecord> builder)
    {
        builder.ToTable("customers", table =>
        {
            table.HasCheckConstraint("ck_customers_status", "status in ('Candidate', 'Active', 'Inactive', 'Blocked')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerCode).HasColumnName("customer_code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.LegalName).HasColumnName("legal_name").HasMaxLength(240).IsRequired();
        builder.Property(x => x.TaxNumber).HasColumnName("tax_number").HasMaxLength(40);
        builder.Property(x => x.TaxOffice).HasColumnName("tax_office").HasMaxLength(160);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(240);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(40);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.IsDeleted).HasColumnName("is_deleted").IsRequired();
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.CustomerCode).IsUnique();
        builder.HasIndex(x => new { x.Status, x.IsDeleted });
    }
}

public sealed class CustomerAddressRecordConfiguration : IEntityTypeConfiguration<CustomerAddressRecord>
{
    public void Configure(EntityTypeBuilder<CustomerAddressRecord> builder)
    {
        builder.ToTable("customer_addresses", table =>
        {
            table.HasCheckConstraint("ck_customer_addresses_type", "address_type in ('Billing', 'Delivery', 'Other')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.AddressType).HasColumnName("address_type").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(120);
        builder.Property(x => x.Line1).HasColumnName("line1").HasMaxLength(240).IsRequired();
        builder.Property(x => x.Line2).HasColumnName("line2").HasMaxLength(240);
        builder.Property(x => x.District).HasColumnName("district").HasMaxLength(120);
        builder.Property(x => x.City).HasColumnName("city").HasMaxLength(120).IsRequired();
        builder.Property(x => x.PostalCode).HasColumnName("postal_code").HasMaxLength(20);
        builder.Property(x => x.CountryCode).HasColumnName("country_code").HasMaxLength(2).IsRequired();
        builder.Property(x => x.IsDefault).HasColumnName("is_default").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.AddressType, x.IsDefault });
        builder.HasOne(x => x.Customer).WithMany(x => x.Addresses).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CustomerContactRecordConfiguration : IEntityTypeConfiguration<CustomerContactRecord>
{
    public void Configure(EntityTypeBuilder<CustomerContactRecord> builder)
    {
        builder.ToTable("customer_contacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.FullName).HasColumnName("full_name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(240);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(40);
        builder.Property(x => x.RoleTitle).HasColumnName("role_title").HasMaxLength(120);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary").IsRequired();
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.IsPrimary });
        builder.HasOne(x => x.Customer).WithMany(x => x.Contacts).HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PriceListRecordConfiguration : IEntityTypeConfiguration<PriceListRecord>
{
    public void Configure(EntityTypeBuilder<PriceListRecord> builder)
    {
        builder.ToTable("price_lists", table =>
        {
            table.HasCheckConstraint("ck_price_lists_valid_window", "valid_to is null or valid_to > valid_from");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamptz");
        builder.Property(x => x.ValidTo).HasColumnName("valid_to").HasColumnType("timestamptz");
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class CustomerPriceGroupRecordConfiguration : IEntityTypeConfiguration<CustomerPriceGroupRecord>
{
    public void Configure(EntityTypeBuilder<CustomerPriceGroupRecord> builder)
    {
        builder.ToTable("customer_price_groups");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(160).IsRequired();
        builder.Property(x => x.PriceListId).HasColumnName("price_list_id");
        builder.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
        builder.HasOne(x => x.PriceList).WithMany().HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CustomerPriceGroupMemberRecordConfiguration : IEntityTypeConfiguration<CustomerPriceGroupMemberRecord>
{
    public void Configure(EntityTypeBuilder<CustomerPriceGroupMemberRecord> builder)
    {
        builder.ToTable("customer_price_group_members", table =>
        {
            table.HasCheckConstraint("ck_customer_price_group_members_valid_window", "effective_to is null or effective_to > effective_from");
        });
        builder.HasKey(x => new { x.CustomerId, x.CustomerPriceGroupId, x.EffectiveFrom });
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CustomerPriceGroupId).HasColumnName("customer_price_group_id");
        builder.Property(x => x.EffectiveFrom).HasColumnName("effective_from").HasColumnType("timestamptz");
        builder.Property(x => x.EffectiveTo).HasColumnName("effective_to").HasColumnType("timestamptz");
    }
}

public sealed class ProductPriceRecordConfiguration : IEntityTypeConfiguration<ProductPriceRecord>
{
    public void Configure(EntityTypeBuilder<ProductPriceRecord> builder)
    {
        builder.ToTable("product_prices", table =>
        {
            table.HasCheckConstraint("ck_product_prices_non_negative", "unit_price >= 0");
            table.HasCheckConstraint("ck_product_prices_valid_window", "valid_to is null or valid_to > valid_from");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.PriceListId).HasColumnName("price_list_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.PackagingId).HasColumnName("packaging_id");
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.TaxCode).HasColumnName("tax_code").HasMaxLength(40);
        builder.Property(x => x.ValidFrom).HasColumnName("valid_from").HasColumnType("timestamptz");
        builder.Property(x => x.ValidTo).HasColumnName("valid_to").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.PriceListId, x.ProductId, x.PackagingId, x.ValidFrom }).IsUnique();
        builder.HasOne<PriceListRecord>().WithMany().HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductPackagingRecord>().WithMany().HasForeignKey(x => x.PackagingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class QuoteRequestRecordConfiguration : IEntityTypeConfiguration<QuoteRequestRecord>
{
    public void Configure(EntityTypeBuilder<QuoteRequestRecord> builder)
    {
        builder.ToTable("quote_requests", table =>
        {
            table.HasCheckConstraint("ck_quote_requests_source", "source in ('Public', 'Internal')");
            table.HasCheckConstraint("ck_quote_requests_status", "status in ('Received', 'InReview', 'Converted', 'Rejected', 'Closed')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.RequestNumber).HasColumnName("request_number").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.CustomerCandidateName).HasColumnName("customer_candidate_name").HasMaxLength(240);
        builder.Property(x => x.CustomerCandidateEmail).HasColumnName("customer_candidate_email").HasMaxLength(240);
        builder.Property(x => x.CustomerCandidatePhone).HasColumnName("customer_candidate_phone").HasMaxLength(40);
        builder.Property(x => x.ConsentAt).HasColumnName("consent_at").HasColumnType("timestamptz");
        builder.Property(x => x.ReviewedBy).HasColumnName("reviewed_by");
        builder.Property(x => x.ReviewedAt).HasColumnName("reviewed_at").HasColumnType("timestamptz");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.RequestNumber).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.ReviewedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class QuoteRequestItemRecordConfiguration : IEntityTypeConfiguration<QuoteRequestItemRecord>
{
    public void Configure(EntityTypeBuilder<QuoteRequestItemRecord> builder)
    {
        builder.ToTable("quote_request_items", table =>
        {
            table.HasCheckConstraint("ck_quote_request_items_entered_positive", "entered_quantity > 0");
            table.HasCheckConstraint("ck_quote_request_items_base_positive", "quantity_base > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.QuoteRequestId).HasColumnName("quote_request_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 6);
        builder.Property(x => x.EnteredPackagingId).HasColumnName("entered_packaging_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.HasOne(x => x.QuoteRequest).WithMany(x => x.Items).HasForeignKey(x => x.QuoteRequestId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductPackagingRecord>().WithMany().HasForeignKey(x => x.EnteredPackagingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StockReservationRecordConfiguration : IEntityTypeConfiguration<StockReservationRecord>
{
    public void Configure(EntityTypeBuilder<StockReservationRecord> builder)
    {
        builder.ToTable("stock_reservations", table =>
        {
            table.HasCheckConstraint("ck_stock_reservations_quantity_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_stock_reservations_components_non_negative", "consumed_qty_base >= 0 and released_qty_base >= 0");
            table.HasCheckConstraint("ck_stock_reservations_components_within_quantity", "consumed_qty_base + released_qty_base <= quantity_base");
            table.HasCheckConstraint("ck_stock_reservations_status", "status in ('Open', 'PartiallyConsumed', 'Consumed', 'Released')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalesOrderItemId).HasColumnName("sales_order_item_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.WarehouseId).HasColumnName("warehouse_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.ConsumedQtyBase).HasColumnName("consumed_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.ReleasedQtyBase).HasColumnName("released_qty_base").HasPrecision(18, 6);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.SalesOrderItemId, x.Status });
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WarehouseRecord>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<SalesOrderItemRecord>().WithMany().HasForeignKey(x => x.SalesOrderItemId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesOrderRecordConfiguration : IEntityTypeConfiguration<SalesOrderRecord>
{
    public void Configure(EntityTypeBuilder<SalesOrderRecord> builder)
    {
        builder.ToTable("sales_orders", table =>
        {
            table.HasCheckConstraint("ck_sales_orders_status", "status in ('Draft', 'PendingApproval', 'Approved', 'Preparing', 'PartiallyShipped', 'Fulfilled', 'Completed', 'Cancelled')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.OrderNumber).HasColumnName("order_number").HasMaxLength(80).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.SourceQuoteId).HasColumnName("source_quote_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40).IsRequired();
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.PriceSnapshotVersion).HasColumnName("price_snapshot_version").HasMaxLength(120);
        builder.Property(x => x.TotalNet).HasColumnName("total_net").HasPrecision(18, 2);
        builder.Property(x => x.TotalTax).HasColumnName("total_tax").HasPrecision(18, 2);
        builder.Property(x => x.TotalGross).HasColumnName("total_gross").HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.SourceQuoteId)
            .IsUnique()
            .HasFilter("source_quote_id IS NOT NULL");
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceQuote).WithMany().HasForeignKey(x => x.SourceQuoteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesOrderItemRecordConfiguration : IEntityTypeConfiguration<SalesOrderItemRecord>
{
    public void Configure(EntityTypeBuilder<SalesOrderItemRecord> builder)
    {
        builder.ToTable("sales_order_items", table =>
        {
            table.HasCheckConstraint("ck_sales_order_items_ordered_positive", "ordered_qty > 0");
            table.HasCheckConstraint("ck_sales_order_items_components_non_negative", "reserved_qty >= 0 and shipped_qty >= 0 and cancelled_qty >= 0");
            table.HasCheckConstraint("ck_sales_order_items_shipped_within_ordered", "shipped_qty + cancelled_qty <= ordered_qty");
            table.HasCheckConstraint("ck_sales_order_items_remaining_projection", "remaining_qty = ordered_qty - shipped_qty - cancelled_qty");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.OrderedQty).HasColumnName("ordered_qty").HasPrecision(18, 6);
        builder.Property(x => x.ReservedQty).HasColumnName("reserved_qty").HasPrecision(18, 6);
        builder.Property(x => x.ShippedQty).HasColumnName("shipped_qty").HasPrecision(18, 6);
        builder.Property(x => x.CancelledQty).HasColumnName("cancelled_qty").HasPrecision(18, 6);
        builder.Property(x => x.RemainingQty).HasColumnName("remaining_qty").HasPrecision(18, 6);
        builder.Property(x => x.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 6);
        builder.Property(x => x.EnteredPackagingId).HasColumnName("entered_packaging_id");
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.PartialDeliveryAllowed).HasColumnName("partial_delivery_allowed").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.TaxCode).HasColumnName("tax_code").HasMaxLength(40);
        builder.Property(x => x.PriceSnapshot).HasColumnName("price_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.SalesOrderId, x.ProductId });
        builder.HasOne(x => x.SalesOrder).WithMany(x => x.Items).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductPackagingRecord>().WithMany().HasForeignKey(x => x.EnteredPackagingId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class SalesOrderApprovalRecordConfiguration : IEntityTypeConfiguration<SalesOrderApprovalRecord>
{
    public void Configure(EntityTypeBuilder<SalesOrderApprovalRecord> builder)
    {
        builder.ToTable("sales_order_approvals", table =>
        {
            table.HasCheckConstraint("ck_sales_order_approvals_decision", "decision in ('Approved', 'Rejected')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.SalesOrderId).HasColumnName("sales_order_id");
        builder.Property(x => x.Decision).HasColumnName("decision").HasMaxLength(30).IsRequired();
        builder.Property(x => x.Comment).HasColumnName("comment").HasColumnType("text");
        builder.Property(x => x.DecidedBy).HasColumnName("decided_by");
        builder.Property(x => x.DecidedAt).HasColumnName("decided_at").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.SalesOrderId, x.DecidedAt });
        builder.HasOne(x => x.SalesOrder).WithMany(x => x.Approvals).HasForeignKey(x => x.SalesOrderId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.DecidedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class QuoteRecordConfiguration : IEntityTypeConfiguration<QuoteRecord>
{
    public void Configure(EntityTypeBuilder<QuoteRecord> builder)
    {
        builder.ToTable("quotes", table =>
        {
            table.HasCheckConstraint("ck_quotes_status", "status in ('Draft', 'Issued')");
            table.HasCheckConstraint("ck_quotes_totals_non_negative", "total_net >= 0 and total_tax >= 0 and total_gross >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.QuoteNumber).HasColumnName("quote_number").HasMaxLength(80).IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.QuoteRequestId).HasColumnName("quote_request_id");
        builder.Property(x => x.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();
        builder.Property(x => x.TotalNet).HasColumnName("total_net").HasPrecision(18, 2);
        builder.Property(x => x.TotalTax).HasColumnName("total_tax").HasPrecision(18, 2);
        builder.Property(x => x.TotalGross).HasColumnName("total_gross").HasPrecision(18, 2);
        builder.Property(x => x.ValidUntil).HasColumnName("valid_until").HasColumnType("timestamptz");
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at").HasColumnType("timestamptz");
        builder.Property(x => x.IssuedBy).HasColumnName("issued_by");
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamptz");
        builder.HasIndex(x => x.QuoteNumber).IsUnique();
        builder.HasIndex(x => x.QuoteRequestId).IsUnique();
        builder.HasIndex(x => new { x.Status, x.CreatedAt });
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuoteRequestRecord>().WithMany().HasForeignKey(x => x.QuoteRequestId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.IssuedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class QuoteItemRecordConfiguration : IEntityTypeConfiguration<QuoteItemRecord>
{
    public void Configure(EntityTypeBuilder<QuoteItemRecord> builder)
    {
        builder.ToTable("quote_items", table =>
        {
            table.HasCheckConstraint("ck_quote_items_entered_positive", "entered_quantity > 0");
            table.HasCheckConstraint("ck_quote_items_base_positive", "quantity_base > 0");
            table.HasCheckConstraint("ck_quote_items_unit_price_non_negative", "unit_price >= 0");
            table.HasCheckConstraint("ck_quote_items_line_net_non_negative", "line_net >= 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.QuoteId).HasColumnName("quote_id");
        builder.Property(x => x.ProductId).HasColumnName("product_id");
        builder.Property(x => x.QuoteRequestItemId).HasColumnName("quote_request_item_id");
        builder.Property(x => x.EnteredQuantity).HasColumnName("entered_quantity").HasPrecision(18, 6);
        builder.Property(x => x.EnteredPackagingId).HasColumnName("entered_packaging_id");
        builder.Property(x => x.QuantityBase).HasColumnName("quantity_base").HasPrecision(18, 6);
        builder.Property(x => x.PackagingSnapshot).HasColumnName("packaging_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(18, 2);
        builder.Property(x => x.ListUnitPrice).HasColumnName("list_unit_price").HasPrecision(18, 2);
        builder.Property(x => x.PriceListId).HasColumnName("price_list_id");
        builder.Property(x => x.TaxCode).HasColumnName("tax_code").HasMaxLength(40);
        builder.Property(x => x.PriceSnapshot).HasColumnName("price_snapshot").HasColumnType("jsonb").IsRequired();
        builder.Property(x => x.LineNet).HasColumnName("line_net").HasPrecision(18, 2);
        builder.Property(x => x.RowVersion).HasColumnName("row_version").HasColumnType("bigint").IsConcurrencyToken().ValueGeneratedOnAddOrUpdate().HasDefaultValue(1L);
        builder.HasIndex(x => new { x.QuoteId, x.QuoteRequestItemId }).IsUnique();
        builder.HasIndex(x => x.PriceListId);
        builder.HasOne(x => x.Quote).WithMany(x => x.Items).HasForeignKey(x => x.QuoteId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductRecord>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ProductPackagingRecord>().WithMany().HasForeignKey(x => x.EnteredPackagingId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<QuoteRequestItemRecord>().WithMany().HasForeignKey(x => x.QuoteRequestItemId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PriceListRecord>().WithMany().HasForeignKey(x => x.PriceListId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CustomerOutboundEmailRecordConfiguration : IEntityTypeConfiguration<CustomerOutboundEmailRecord>
{
    public void Configure(EntityTypeBuilder<CustomerOutboundEmailRecord> builder)
    {
        builder.ToTable("customer_outbound_emails", table =>
        {
            table.HasCheckConstraint(
                "ck_customer_outbound_emails_status",
                "status in ('Queued', 'Sent', 'Failed')");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.CustomerId).HasColumnName("customer_id");
        builder.Property(x => x.ContactId).HasColumnName("contact_id");
        builder.Property(x => x.ToEmail).HasColumnName("to_email").HasMaxLength(240).IsRequired();
        builder.Property(x => x.Subject).HasColumnName("subject").HasMaxLength(240).IsRequired();
        builder.Property(x => x.Body).HasColumnName("body").HasColumnType("text").IsRequired();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).IsRequired();
        builder.Property(x => x.LastError).HasColumnName("last_error").HasColumnType("text");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasColumnType("timestamptz");
        builder.Property(x => x.SentAt).HasColumnName("sent_at").HasColumnType("timestamptz");
        builder.HasIndex(x => new { x.CustomerId, x.CreatedAt });
        builder.HasOne<CustomerRecord>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CustomerContactRecord>().WithMany().HasForeignKey(x => x.ContactId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UserRecord>().WithMany().HasForeignKey(x => x.CreatedBy).OnDelete(DeleteBehavior.Restrict);
    }
}
