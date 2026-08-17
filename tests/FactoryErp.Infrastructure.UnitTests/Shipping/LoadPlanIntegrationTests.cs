using FactoryErp.Application.Shipping;
using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FactoryErp.Infrastructure.Persistence;
using FactoryErp.Infrastructure.Persistence.Entities;
using FactoryErp.Infrastructure.Shipping;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FactoryErp.Infrastructure.UnitTests.Shipping;

public sealed class LoadPlanIntegrationTests
{
    private static readonly Guid ProductId = Guid.Parse("30000000-0000-0000-0000-000000000201");
    private static readonly Guid PackagingId = Guid.Parse("30000000-0000-0000-0000-000000000213");
    private static readonly Guid CustomerId = Guid.Parse("40000000-0000-0000-0000-000000000101");
    private static readonly Guid AddressId = Guid.Parse("40000000-0000-0000-0000-000000000102");

    [Fact]
    public async Task Create_draft_persists_nested_units_items_and_stop_allocations_and_replays()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b2-draft");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b2-draft");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;

            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var request = BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000);
            var key = "l4b2-load-plan-" + Guid.NewGuid();

            var created = await service.CreateLoadPlanAsync(shipmentId, request, actorId, key, "l4b2-draft", CancellationToken.None);
            var replay = await service.CreateLoadPlanAsync(shipmentId, request, actorId, key, "l4b2-draft", CancellationToken.None);
            loadPlanId = created.Id;

            created.Status.Should().Be("Draft");
            created.FeasibilityStatus.Should().Be("Infeasible");
            created.LoadUnits.Should().ContainSingle();
            created.LoadUnits.Single().Items.Should().ContainSingle();
            created.LoadUnits.Single().Items.Single().QuantityBase.Should().Be(4000);
            created.LoadUnits.Single().Items.Single().StopAllocations.Should().ContainSingle(x => x.QuantityBase == 4000);
            replay.Id.Should().Be(created.Id);

            var loaded = await service.GetLoadPlanAsync(loadPlanId, CancellationToken.None);
            loaded.Should().NotBeNull();
            loaded!.LoadUnits.Single().Items.Single().StopAllocations.Should().ContainSingle();
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
        }
    }

    [Fact]
    public async Task Create_draft_rejects_quantity_above_shipment_item_ceiling()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b2-ceiling");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b2-ceiling");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;

            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var request = BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4001);

            var action = () => service.CreateLoadPlanAsync(
                shipmentId,
                request,
                actorId,
                "l4b2-ceiling-" + Guid.NewGuid(),
                "l4b2-ceiling",
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "QUANTITY_EXCEEDED");
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, Guid.Empty, profileId);
        }
    }

    [Fact]
    public async Task Create_draft_rejects_stop_from_another_route_plan()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var foreignRoutePlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b2-stop");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b2-stop");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            var foreign = await CreateRouteAsync(actorId, setup.Shipment, "l4b2-foreign-stop");
            foreignRoutePlanId = foreign.RoutePlan.Id;

            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var request = BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, foreign.StopId, 4000);

            var action = () => service.CreateLoadPlanAsync(
                shipmentId,
                request,
                actorId,
                "l4b2-stop-" + Guid.NewGuid(),
                "l4b2-stop",
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "PACKAGE_STOP_MISMATCH");
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, Guid.Empty, profileId, foreignRoutePlanId);
        }
    }

    [Fact]
    public async Task Validate_creates_deterministic_results_and_replays_idempotently()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b4-validate");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b4-validate");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var created = await service.CreateLoadPlanAsync(
                shipmentId,
                BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000),
                actorId,
                "l4b4-validate-create-" + Guid.NewGuid(),
                "l4b4-validate",
                CancellationToken.None);
            loadPlanId = created.Id;

            var request = new ValidateLoadPlanRequest();
            var key = "l4b4-validate-command-" + Guid.NewGuid();
            var result = await service.ValidateLoadPlanAsync(loadPlanId, request, created.RowVersion, actorId, key, "l4b4-validate", CancellationToken.None);
            var replay = await service.ValidateLoadPlanAsync(loadPlanId, request, result.LoadPlan.RowVersion, actorId, key, "l4b4-validate", CancellationToken.None);

            result.Results.Should().NotBeEmpty();
            result.Results.Should().Contain(x => x.Code == "LOAD_PLAN_INFEASIBLE");
            replay.Results.Select(x => x.Id).Should().BeEquivalentTo(result.Results.Select(x => x.Id));
            result.LoadPlan.Status.Should().Be("NeedsReview");
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
        }
    }

    [Fact]
    public async Task Lock_rejects_open_hard_validation_errors_even_with_approval()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b4-lock-hard");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b4-lock-hard");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var created = await service.CreateLoadPlanAsync(
                shipmentId,
                BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000),
                actorId,
                "l4b4-lock-create-" + Guid.NewGuid(),
                "l4b4-lock-hard",
                CancellationToken.None);
            loadPlanId = created.Id;
            var validated = await service.ValidateLoadPlanAsync(loadPlanId, new ValidateLoadPlanRequest(), created.RowVersion, actorId, "l4b4-lock-validate-" + Guid.NewGuid(), "l4b4-lock-hard", CancellationToken.None);

            var action = () => service.LockLoadPlanAsync(
                loadPlanId,
                new LockLoadPlanRequest(true, []),
                validated.LoadPlan.RowVersion,
                actorId,
                "l4b4-lock-command-" + Guid.NewGuid(),
                "l4b4-lock-hard",
                CancellationToken.None);

            await action.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "LOAD_PLAN_INFEASIBLE");
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
        }
    }

    [Fact]
    public async Task Lock_succeeds_with_approval_and_vehicle_capacity_snapshot()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var capacityId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            await InsertLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b4-lock-success");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b4-lock-success");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var created = await service.CreateLoadPlanAsync(
                shipmentId,
                BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000),
                actorId,
                "l4b4-lock-success-create-" + Guid.NewGuid(),
                "l4b4-lock-success",
                CancellationToken.None);
            loadPlanId = created.Id;
            var plan = await context.LoadPlans.SingleAsync(x => x.Id == loadPlanId);
            plan.VehicleId = vehicleId;
            plan.VehicleCapacityId = capacityId;
            plan.InputSnapshotHash = "sha256:l4b4-lock-success";
            plan.FeasibilityStatus = nameof(LoadPlanFeasibilityStatus.Feasible);
            plan.Status = nameof(LoadPlanStatus.Valid);
            plan.ValidationSummary = "{\"hardErrors\":0,\"warnings\":0}";
            await context.SaveChangesAsync();

            var locked = await service.LockLoadPlanAsync(
                loadPlanId,
                new LockLoadPlanRequest(true, []),
                plan.RowVersion,
                actorId,
                "l4b4-lock-success-command-" + Guid.NewGuid(),
                "l4b4-lock-success",
                CancellationToken.None);

            locked.Status.Should().Be("Locked");
            locked.VehicleId.Should().Be(vehicleId);
            locked.VehicleCapacityId.Should().Be(capacityId);
            locked.LoadUnits.Single().Status.Should().Be("Locked");
            locked.ApprovedBy.Should().Be(actorId);
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
            await CleanupLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
        }
    }

    [Fact]
    public async Task Warning_resolution_and_manual_change_are_audited()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b4-audit");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b4-audit");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            await using var context = CreateContext();
            var service = CreateLoadPlanService(context);
            var created = await service.CreateLoadPlanAsync(
                shipmentId,
                BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000),
                actorId,
                "l4b4-audit-create-" + Guid.NewGuid(),
                "l4b4-audit",
                CancellationToken.None);
            loadPlanId = created.Id;
            var validated = await service.ValidateLoadPlanAsync(loadPlanId, new ValidateLoadPlanRequest(), created.RowVersion, actorId, "l4b4-audit-validate-" + Guid.NewGuid(), "l4b4-audit", CancellationToken.None);
            var warning = validated.Results.First(x => x.Severity == "Warning");
            var resolved = await service.ResolveValidationResultAsync(
                loadPlanId,
                warning.Id,
                new ResolveLoadPlanValidationRequest("Overridden", "Depo sorumlusu kontrol etti."),
                validated.LoadPlan.RowVersion,
                actorId,
                "l4b4-audit-warning-" + Guid.NewGuid(),
                "l4b4-audit",
                CancellationToken.None);

            var itemId = created.LoadUnits.Single().Items.Single().Id;
            var manualKey = "l4b4-audit-manual-" + Guid.NewGuid();
            var manualRequest = new CreateLoadPlanManualChangeRequest("ChangeQuantity", "LoadUnitItem", itemId, "{\"quantityBase\":4000}", "{\"quantityBase\":3500}", "Manuel depo düzeltmesi");
            var stale = () => service.CreateManualChangeAsync(
                loadPlanId,
                manualRequest,
                validated.LoadPlan.RowVersion - 1,
                actorId,
                "l4b4-audit-stale-" + Guid.NewGuid(),
                "l4b4-audit",
                CancellationToken.None);
            await stale.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "RESOURCE_VERSION_CONFLICT");

            var changed = await service.CreateManualChangeAsync(
                loadPlanId,
                manualRequest,
                validated.LoadPlan.RowVersion,
                actorId,
                manualKey,
                "l4b4-audit",
                CancellationToken.None);
            var mismatch = () => service.CreateManualChangeAsync(
                loadPlanId,
                manualRequest with { Reason = "Farklı gerekçe" },
                validated.LoadPlan.RowVersion,
                actorId,
                manualKey,
                "l4b4-audit",
                CancellationToken.None);

            await mismatch.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "IDEMPOTENCY_PAYLOAD_MISMATCH");
            resolved.ResolutionStatus.Should().Be("Overridden");
            changed.Status.Should().Be("NeedsReview");
            (await context.LoadPlanManualChanges.CountAsync(x => x.LoadPlanId == loadPlanId)).Should().Be(1);
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
        }
    }

    [Fact]
    public async Task Load_verification_accepts_package_replays_scan_and_completes_state_chain()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var capacityId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            await InsertLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b5-accepted");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b5-accepted");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            await using var planContext = CreateContext();
            var planService = CreateLoadPlanService(planContext);
            var created = await planService.CreateLoadPlanAsync(
                shipmentId,
                BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000),
                actorId,
                "l4b5-accepted-create-" + Guid.NewGuid(),
                "l4b5-accepted",
                CancellationToken.None);
            loadPlanId = created.Id;
            var plan = await planContext.LoadPlans.SingleAsync(x => x.Id == loadPlanId);
            plan.VehicleId = vehicleId;
            plan.VehicleCapacityId = capacityId;
            plan.InputSnapshotHash = "sha256:l4b5-accepted";
            plan.FeasibilityStatus = nameof(LoadPlanFeasibilityStatus.Feasible);
            plan.Status = nameof(LoadPlanStatus.Valid);
            plan.ValidationSummary = "{\"hardErrors\":0,\"warnings\":0}";
            await planContext.SaveChangesAsync();
            var locked = await planService.LockLoadPlanAsync(
                loadPlanId,
                new LockLoadPlanRequest(true, []),
                plan.RowVersion,
                actorId,
                "l4b5-accepted-lock-" + Guid.NewGuid(),
                "l4b5-accepted",
                CancellationToken.None);

            await using var context = CreateContext();
            var verification = CreateLoadVerificationService(context);
            var packageCode = await context.ShipmentPackages
                .Where(x => x.Id == setup.PackageId)
                .Select(x => x.PackageCode!)
                .SingleAsync();
            var session = await verification.StartSessionAsync(
                loadPlanId,
                new StartLoadVerificationRequest(),
                locked.RowVersion,
                actorId,
                "l4b5-accepted-session-" + Guid.NewGuid(),
                "l4b5-accepted",
                CancellationToken.None);
            var scanRequest = new ScanLoadVerificationRequest(packageCode, locked.LoadUnits.Single().Id, "Package");
            var scanKey = "l4b5-accepted-scan-" + Guid.NewGuid();
            var scan = await verification.ScanAsync(
                session.Id,
                scanRequest,
                session.RowVersion,
                actorId,
                scanKey,
                "l4b5-accepted",
                CancellationToken.None);
            var replay = await verification.ScanAsync(
                session.Id,
                scanRequest,
                session.RowVersion,
                actorId,
                scanKey,
                "l4b5-accepted",
                CancellationToken.None);

            scan.Status.Should().Be(nameof(LoadVerificationScanStatus.Accepted));
            replay.Id.Should().Be(scan.Id);
            (await context.ShipmentPackages.SingleAsync(x => x.Id == setup.PackageId)).Status.Should().Be(nameof(ShipmentPackageStatus.Loaded));

            var refreshedSession = await verification.GetSessionAsync(session.Id, CancellationToken.None);
            var completed = await verification.CompleteAsync(
                session.Id,
                new CompleteLoadVerificationRequest(),
                refreshedSession!.RowVersion,
                actorId,
                "l4b5-accepted-complete-" + Guid.NewGuid(),
                "l4b5-accepted",
                CancellationToken.None);

            completed.Status.Should().Be(nameof(LoadVerificationSessionStatus.Completed));
            (await context.LoadUnits.SingleAsync(x => x.LoadPlanId == loadPlanId)).Status.Should().Be(nameof(LoadUnitStatus.Loaded));
            (await context.Shipments.SingleAsync(x => x.Id == shipmentId)).Status.Should().Be("Loaded");
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
            await CleanupLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
        }
    }

    [Fact]
    public async Task Load_verification_records_unexpected_barcode_and_rejects_stale_session_version()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var capacityId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            await InsertLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b5-discrepancy");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b5-discrepancy");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            await using var planContext = CreateContext();
            var planService = CreateLoadPlanService(planContext);
            var created = await planService.CreateLoadPlanAsync(
                shipmentId,
                BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000),
                actorId,
                "l4b5-discrepancy-create-" + Guid.NewGuid(),
                "l4b5-discrepancy",
                CancellationToken.None);
            loadPlanId = created.Id;
            var plan = await planContext.LoadPlans.SingleAsync(x => x.Id == loadPlanId);
            plan.VehicleId = vehicleId;
            plan.VehicleCapacityId = capacityId;
            plan.InputSnapshotHash = "sha256:l4b5-discrepancy";
            plan.FeasibilityStatus = nameof(LoadPlanFeasibilityStatus.Feasible);
            plan.Status = nameof(LoadPlanStatus.Valid);
            plan.ValidationSummary = "{\"hardErrors\":0,\"warnings\":0}";
            await planContext.SaveChangesAsync();
            var locked = await planService.LockLoadPlanAsync(
                loadPlanId,
                new LockLoadPlanRequest(true, []),
                plan.RowVersion,
                actorId,
                "l4b5-discrepancy-lock-" + Guid.NewGuid(),
                "l4b5-discrepancy",
                CancellationToken.None);

            await using var context = CreateContext();
            var verification = CreateLoadVerificationService(context);
            var session = await verification.StartSessionAsync(
                loadPlanId,
                new StartLoadVerificationRequest(),
                locked.RowVersion,
                actorId,
                "l4b5-discrepancy-session-" + Guid.NewGuid(),
                "l4b5-discrepancy",
                CancellationToken.None);
            var unexpected = await verification.ScanAsync(
                session.Id,
                new ScanLoadVerificationRequest("UNKNOWN-BARCODE", null, "Package"),
                session.RowVersion,
                actorId,
                "l4b5-discrepancy-scan-" + Guid.NewGuid(),
                "l4b5-discrepancy",
                CancellationToken.None);
            unexpected.Status.Should().Be(nameof(LoadVerificationScanStatus.Unexpected));
            unexpected.ReasonCode.Should().Be("PACKAGE_BARCODE_NOT_FOUND");

            var stale = () => verification.ScanAsync(
                session.Id,
                new ScanLoadVerificationRequest("UNKNOWN-BARCODE-2", null, "Package"),
                session.RowVersion,
                actorId,
                "l4b5-discrepancy-stale-" + Guid.NewGuid(),
                "l4b5-discrepancy",
                CancellationToken.None);
            await stale.Should().ThrowAsync<DomainException>()
                .Where(x => x.Error.Code == "RESOURCE_VERSION_CONFLICT");

            var refreshedSession = await verification.GetSessionAsync(session.Id, CancellationToken.None);
            var closed = await verification.CloseDiscrepancyAsync(
                session.Id,
                new CloseLoadVerificationDiscrepancyRequest("Barkod fiziksel kontrolde bulunamadı."),
                refreshedSession!.RowVersion,
                actorId,
                "l4b5-discrepancy-close-" + Guid.NewGuid(),
                "l4b5-discrepancy",
                CancellationToken.None);
            closed.Status.Should().Be(nameof(LoadVerificationSessionStatus.Discrepancy));
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
            await CleanupLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
        }
    }

    [Fact]
    public async Task Concurrent_load_verification_scan_accepts_package_only_once()
    {
        var actorId = await GetAdminIdAsync();
        var profileId = Guid.NewGuid();
        var vehicleTypeId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var capacityId = Guid.NewGuid();
        var fixture = default(DeliveryFixture);
        var shipmentId = Guid.Empty;
        var routePlanId = Guid.Empty;
        var loadPlanId = Guid.Empty;

        try
        {
            await InsertPhysicalProfileAsync(profileId);
            await InsertLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
            fixture = await CreateDeliveryFixtureAsync(actorId, "l4b5-race");
            var setup = await CreateShipmentAndRouteAsync(actorId, fixture!, "l4b5-race");
            shipmentId = setup.Shipment.Id;
            routePlanId = setup.RoutePlan.Id;
            Guid sessionId;
            long sessionRowVersion;
            string packageCode;
            Guid loadUnitId;
            await using (var planContext = CreateContext())
            {
                var planService = CreateLoadPlanService(planContext);
                var created = await planService.CreateLoadPlanAsync(
                    shipmentId,
                    BuildRequest(setup.Shipment, setup.RoutePlan, setup.ShipmentItemId, setup.PackageId, setup.StopId, 4000),
                    actorId,
                    "l4b5-race-create-" + Guid.NewGuid(),
                    "l4b5-race",
                    CancellationToken.None);
                loadPlanId = created.Id;
                var plan = await planContext.LoadPlans.SingleAsync(x => x.Id == loadPlanId);
                plan.VehicleId = vehicleId;
                plan.VehicleCapacityId = capacityId;
                plan.InputSnapshotHash = "sha256:l4b5-race";
                plan.FeasibilityStatus = nameof(LoadPlanFeasibilityStatus.Feasible);
                plan.Status = nameof(LoadPlanStatus.Valid);
                plan.ValidationSummary = "{\"hardErrors\":0,\"warnings\":0}";
                await planContext.SaveChangesAsync();
                var locked = await planService.LockLoadPlanAsync(
                    loadPlanId,
                    new LockLoadPlanRequest(true, []),
                    plan.RowVersion,
                    actorId,
                    "l4b5-race-lock-" + Guid.NewGuid(),
                    "l4b5-race",
                    CancellationToken.None);
                await using var sessionContext = CreateContext();
                var sessionService = CreateLoadVerificationService(sessionContext);
                var session = await sessionService.StartSessionAsync(
                    loadPlanId,
                    new StartLoadVerificationRequest(),
                    locked.RowVersion,
                    actorId,
                    "l4b5-race-session-" + Guid.NewGuid(),
                    "l4b5-race",
                    CancellationToken.None);
                sessionId = session.Id;
                sessionRowVersion = session.RowVersion;
                packageCode = await sessionContext.ShipmentPackages.Where(x => x.Id == setup.PackageId).Select(x => x.PackageCode!).SingleAsync();
                loadUnitId = locked.LoadUnits.Single().Id;
            }

            await using var contextA = CreateContext();
            await using var contextB = CreateContext();
            var serviceA = CreateLoadVerificationService(contextA);
            var serviceB = CreateLoadVerificationService(contextB);
            var scanRequest = new ScanLoadVerificationRequest(packageCode, loadUnitId, "Package");
            var results = await Task.WhenAll(
                TryScanAsync(serviceA, sessionId, scanRequest, sessionRowVersion, actorId, "l4b5-race-scan-a-" + Guid.NewGuid()),
                TryScanAsync(serviceB, sessionId, scanRequest, sessionRowVersion, actorId, "l4b5-race-scan-b-" + Guid.NewGuid()));

            results.Count(x => x.Result?.Status == nameof(LoadVerificationScanStatus.Accepted)).Should().Be(1);
            results.Count(x => x.ErrorCode == "RESOURCE_VERSION_CONFLICT").Should().Be(1);
            await using var verifyContext = CreateContext();
            (await verifyContext.LoadVerificationScans.CountAsync(x => x.SessionId == sessionId && x.Status == nameof(LoadVerificationScanStatus.Accepted))).Should().Be(1);
            (await verifyContext.ShipmentPackages.SingleAsync(x => x.Id == setup.PackageId)).Status.Should().Be(nameof(ShipmentPackageStatus.Loaded));
        }
        finally
        {
            await CleanupAsync(fixture, shipmentId, routePlanId, loadPlanId, profileId);
            await CleanupLockResourcesAsync(vehicleTypeId, vehicleId, capacityId);
        }
    }

    private static CreateLoadPlanRequest BuildRequest(
        ShipmentDto shipment,
        RoutePlanDto routePlan,
        Guid shipmentItemId,
        Guid packageId,
        Guid stopId,
        decimal quantity)
        => new(
            routePlan.Id,
            routePlan.Version,
            shipment.RowVersion,
            [new CreateLoadUnitRequest(
                "PAL-001",
                "Pallet",
                null,
                false,
                1200,
                800,
                150,
                30,
                230,
                1.44m,
                1,
                "ZONE-A",
                1,
                [new CreateLoadUnitItemRequest(
                    packageId,
                    shipmentItemId,
                    quantity,
                    [new CreateLoadUnitStopAllocationRequest(stopId, quantity, 1)])])]);

    private static async Task<ShipmentRouteSetup> CreateShipmentAndRouteAsync(Guid actorId, DeliveryFixture fixture, string scope)
    {
        await using var context = CreateContext();
        var audit = new EfAuditWriter(context);
        var idempotency = new EfIdempotencyStore(context);
        var logistics = new LogisticsCommandService(context, audit, idempotency);
        var shipment = await logistics.CreateShipmentAsync(
            new CreateShipmentRequest(fixture.DeliveryNoteId, 1),
            actorId,
            scope + "-shipment-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        var route = await CreateRouteAsync(logistics, actorId, shipment, scope);
        var shipmentItemId = shipment.Items.Single().Id;
        var packageService = new ShipmentPackageCommandService(context, audit, idempotency);
        var package = await packageService.CreateShipmentPackageAsync(
            shipment.Id,
            new CreateShipmentPackageRequest(shipmentItemId, PackagingId, route.StopId, "Case", 2, 2000, 2, scope + "-package-" + Guid.NewGuid(), true),
            actorId,
            scope + "-package-key-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        return new ShipmentRouteSetup(shipment, route.RoutePlan, route.StopId, shipmentItemId, package.Id);
    }

    private static async Task<DeliveryFixture> CreateDeliveryFixtureAsync(Guid actorId, string scope)
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        var salesOrderId = Guid.NewGuid();
        var salesOrderItemId = Guid.NewGuid();
        var deliveryNoteId = Guid.NewGuid();
        var deliveryNoteItemId = Guid.NewGuid();
        const decimal quantityBase = 4000m;

        context.SalesOrders.Add(new SalesOrderRecord
        {
            Id = salesOrderId,
            OrderNumber = "SO-L4B2-" + Guid.NewGuid().ToString("N"),
            CustomerId = CustomerId,
            Status = "Approved",
            CurrencyCode = "TRY",
            TotalNet = 100,
            TotalTax = 0,
            TotalGross = 100,
            CreatedBy = actorId,
            CreatedAt = now,
            UpdatedAt = now,
            RowVersion = 1,
        });
        context.SalesOrderItems.Add(new SalesOrderItemRecord
        {
            Id = salesOrderItemId,
            SalesOrderId = salesOrderId,
            ProductId = ProductId,
            OrderedQty = quantityBase,
            ReservedQty = quantityBase,
            ShippedQty = 0,
            CancelledQty = 0,
            RemainingQty = quantityBase,
            EnteredQuantity = 2,
            EnteredPackagingId = PackagingId,
            PackagingSnapshot = "{}",
            PartialDeliveryAllowed = true,
            UnitPrice = 100,
            PriceSnapshot = "{}",
            RowVersion = 1,
        });
        context.DeliveryNotes.Add(new DeliveryNoteRecord
        {
            Id = deliveryNoteId,
            DocumentNumber = "DN-L4B2-" + Guid.NewGuid().ToString("N"),
            SalesOrderId = salesOrderId,
            CustomerId = CustomerId,
            Status = "Issued",
            IssuedAt = now,
            IssuedBy = actorId,
            CreatedAt = now,
            RowVersion = 1,
        });
        context.DeliveryNoteItems.Add(new DeliveryNoteItemRecord
        {
            Id = deliveryNoteItemId,
            DeliveryNoteId = deliveryNoteId,
            SalesOrderItemId = salesOrderItemId,
            ProductId = ProductId,
            QuantityBase = quantityBase,
            EnteredQuantity = 2,
            EnteredPackagingId = PackagingId,
            PackagingSnapshot = "{\"quantityInBaseUom\":2000}",
            ShippedQty = quantityBase,
            InvoicedQty = 0,
            WaivedQty = 0,
            RemainingToInvoice = quantityBase,
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
        return new DeliveryFixture(deliveryNoteId, salesOrderId, salesOrderItemId, deliveryNoteItemId, scope);
    }

    private static async Task<RouteSetup> CreateRouteAsync(Guid actorId, ShipmentDto shipment, string scope)
    {
        await using var context = CreateContext();
        var logistics = new LogisticsCommandService(context, new EfAuditWriter(context), new EfIdempotencyStore(context));
        return await CreateRouteAsync(logistics, actorId, shipment, scope);
    }

    private static async Task<RouteSetup> CreateRouteAsync(ILogisticsCommandService logistics, Guid actorId, ShipmentDto shipment, string scope)
    {
        var start = DateTimeOffset.UtcNow.AddDays(2);
        var route = await logistics.CreateRoutePlanAsync(
            shipment.Id,
            new CreateRoutePlanRequest(start, start.AddHours(2), shipment.RowVersion),
            actorId,
            scope + "-route-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        var updated = await logistics.ReplaceStopsAsync(
            route.Id,
            new ReplaceRouteStopsRequest([new RouteStopInput(1, CustomerId, AddressId, start.AddHours(1))]),
            route.RowVersion,
            actorId,
            scope + "-stops-" + Guid.NewGuid(),
            scope,
            CancellationToken.None);
        return new RouteSetup(updated!, updated!.Stops.Single().Id);
    }

    private static LoadPlanCommandService CreateLoadPlanService(FactoryErpDbContext context)
        => new(context, new EfAuditWriter(context), new EfIdempotencyStore(context));

    private static LoadVerificationCommandService CreateLoadVerificationService(FactoryErpDbContext context)
        => new(context, new EfAuditWriter(context), new EfIdempotencyStore(context));

    private static async Task<(LoadVerificationScanDto? Result, string? ErrorCode)> TryScanAsync(
        LoadVerificationCommandService service,
        Guid sessionId,
        ScanLoadVerificationRequest request,
        long expectedRowVersion,
        Guid actorId,
        string idempotencyKey)
    {
        try
        {
            return (await service.ScanAsync(sessionId, request, expectedRowVersion, actorId, idempotencyKey, "l4b5-race", CancellationToken.None), null);
        }
        catch (DomainException exception)
        {
            return (null, exception.Error.Code);
        }
    }

    private static FactoryErpDbContext CreateContext()
    {
        var connectionString = Environment.GetEnvironmentVariable("FactoryErpTestConnectionString")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__FactoryErp")
            ?? "Host=127.0.0.1;Port=5432;Database=factory_erp_g1;Username=factory_erp;Password=dev_only_change_me";
        return new FactoryErpDbContext(new DbContextOptionsBuilder<FactoryErpDbContext>().UseNpgsql(connectionString).Options);
    }

    private static async Task<Guid> GetAdminIdAsync()
    {
        await using var context = CreateContext();
        return await context.Users.Where(x => x.UserName == "admin").Select(x => x.Id).SingleAsync();
    }

    private static async Task InsertPhysicalProfileAsync(Guid profileId)
    {
        await using var context = CreateContext();
        context.ProductPhysicalProfiles.Add(new ProductPhysicalProfileRecord
        {
            Id = profileId,
            ProductId = ProductId,
            EffectiveFrom = DateTimeOffset.UtcNow.AddMinutes(-5),
            LengthMm = 100,
            WidthMm = 100,
            HeightMm = 100,
            NetWeightKg = 1,
            VolumeM3 = 0.001m,
            IsStackable = true,
            MaxStackCount = 5,
            PhysicalPolicySnapshot = "{}",
            IncompatibleGroups = "[]",
            AllowedOrientations = "[\"LWH\"]",
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
    }

    private static async Task InsertLockResourcesAsync(Guid vehicleTypeId, Guid vehicleId, Guid capacityId)
    {
        await using var context = CreateContext();
        var now = DateTimeOffset.UtcNow;
        context.VehicleTypes.Add(new VehicleTypeRecord
        {
            Id = vehicleTypeId,
            Code = "L4B4-" + vehicleTypeId.ToString("N")[..8],
            Name = "L4-B4 Lock Vehicle Type",
            IsActive = true,
        });
        context.Vehicles.Add(new VehicleRecord
        {
            Id = vehicleId,
            VehicleTypeId = vehicleTypeId,
            PlateNumber = "L4B4-" + vehicleId.ToString("N")[..8],
            Status = "Available",
            LastStatusAt = now,
            RowVersion = 1,
        });
        context.VehicleCapacities.Add(new VehicleCapacityRecord
        {
            Id = capacityId,
            VehicleTypeId = vehicleTypeId,
            EffectiveFrom = now.AddHours(-1),
            MaxGrossWeight = 1000,
            TareWeight = 100,
            MaxUsableVolume = 30,
            MaxPalletCount = 10,
            MaxLoadHeight = 1800,
            CapacityPolicySnapshot = "{\"source\":\"l4b4-test\"}",
            RowVersion = 1,
        });
        await context.SaveChangesAsync();
    }

    private static async Task CleanupLockResourcesAsync(Guid vehicleTypeId, Guid vehicleId, Guid capacityId)
    {
        await using var context = CreateContext();
        context.VehicleCapacities.RemoveRange(context.VehicleCapacities.Where(x => x.Id == capacityId));
        context.Vehicles.RemoveRange(context.Vehicles.Where(x => x.Id == vehicleId));
        context.VehicleTypes.RemoveRange(context.VehicleTypes.Where(x => x.Id == vehicleTypeId));
        await context.SaveChangesAsync();
    }

    private static async Task CleanupAsync(
        DeliveryFixture? fixture,
        Guid shipmentId,
        Guid routePlanId,
        Guid loadPlanId,
        Guid profileId,
        Guid foreignRoutePlanId = default)
    {
        await using var context = CreateContext();
        var planIds = loadPlanId == Guid.Empty ? Array.Empty<Guid>() : new[] { loadPlanId };
        context.LoadPlanManualChanges.RemoveRange(context.LoadPlanManualChanges.Where(x => planIds.Contains(x.LoadPlanId)));
        context.LoadPlanValidationResults.RemoveRange(context.LoadPlanValidationResults.Where(x => planIds.Contains(x.LoadPlanId)));
        context.LoadVerificationScans.RemoveRange(context.LoadVerificationScans.Where(x => planIds.Contains(x.LoadPlanId)));
        context.LoadVerificationSessions.RemoveRange(context.LoadVerificationSessions.Where(x => planIds.Contains(x.LoadPlanId)));
        var unitIds = await context.LoadUnits.Where(x => planIds.Contains(x.LoadPlanId)).Select(x => x.Id).ToArrayAsync();
        var itemIds = await context.LoadUnitItems.Where(x => unitIds.Contains(x.LoadUnitId)).Select(x => x.Id).ToArrayAsync();
        context.LoadUnitStopAllocations.RemoveRange(context.LoadUnitStopAllocations.Where(x => itemIds.Contains(x.LoadUnitItemId)));
        context.LoadUnitItems.RemoveRange(context.LoadUnitItems.Where(x => unitIds.Contains(x.LoadUnitId)));
        context.LoadUnits.RemoveRange(context.LoadUnits.Where(x => unitIds.Contains(x.Id)));
        context.LoadPlans.RemoveRange(context.LoadPlans.Where(x => planIds.Contains(x.Id)));

        if (shipmentId != Guid.Empty)
        {
            context.ShipmentPackages.RemoveRange(context.ShipmentPackages.Where(x => x.ShipmentId == shipmentId));
            context.ShipmentItems.RemoveRange(context.ShipmentItems.Where(x => x.ShipmentId == shipmentId));
            context.Shipments.RemoveRange(context.Shipments.Where(x => x.Id == shipmentId));
        }

        var routeIds = new[] { routePlanId, foreignRoutePlanId }.Where(x => x != Guid.Empty).ToArray();
        context.RouteStops.RemoveRange(context.RouteStops.Where(x => routeIds.Contains(x.RoutePlanId)));
        context.RoutePlans.RemoveRange(context.RoutePlans.Where(x => routeIds.Contains(x.Id)));

        if (fixture is not null)
        {
            context.DeliveryNoteItems.RemoveRange(context.DeliveryNoteItems.Where(x => x.Id == fixture!.DeliveryNoteItemId));
            context.DeliveryNotes.RemoveRange(context.DeliveryNotes.Where(x => x.Id == fixture!.DeliveryNoteId));
            context.SalesOrderItems.RemoveRange(context.SalesOrderItems.Where(x => x.Id == fixture!.SalesOrderItemId));
            context.SalesOrders.RemoveRange(context.SalesOrders.Where(x => x.Id == fixture!.SalesOrderId));
        }

        var profile = await context.ProductPhysicalProfiles.FindAsync(profileId);
        if (profile is not null)
        {
            context.ProductPhysicalProfiles.Remove(profile);
        }

        await context.SaveChangesAsync();
    }

    private sealed record RouteSetup(RoutePlanDto RoutePlan, Guid StopId);
    private sealed record DeliveryFixture(Guid DeliveryNoteId, Guid SalesOrderId, Guid SalesOrderItemId, Guid DeliveryNoteItemId, string Scope);
    private sealed record ShipmentRouteSetup(ShipmentDto Shipment, RoutePlanDto RoutePlan, Guid StopId, Guid ShipmentItemId, Guid PackageId);
}
