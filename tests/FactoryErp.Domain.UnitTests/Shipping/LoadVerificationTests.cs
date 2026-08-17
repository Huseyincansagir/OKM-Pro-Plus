using FactoryErp.Domain.Common;
using FactoryErp.Domain.Shipping;
using FluentAssertions;

namespace FactoryErp.Domain.UnitTests.Shipping;

public sealed class LoadVerificationTests
{
    private static readonly Guid LoadPlanId = Guid.Parse("80000000-0000-0000-0000-000000000001");
    private static readonly Guid ShipmentId = Guid.Parse("80000000-0000-0000-0000-000000000002");
    private static readonly Guid PackageId = Guid.Parse("80000000-0000-0000-0000-000000000003");
    private static readonly Guid LoadUnitId = Guid.Parse("80000000-0000-0000-0000-000000000004");
    private static readonly Guid ActorId = Guid.Parse("80000000-0000-0000-0000-000000000005");
    private static readonly DateTimeOffset Now = new(2026, 8, 17, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Session_starts_only_for_locked_plan_and_has_one_active_transition()
    {
        var session = CreateSession();

        LoadVerificationSession.EnsureStartAllowed(LoadPlanStatus.Locked, null);
        session.Start(Now.AddMinutes(1));

        session.Status.Should().Be(LoadVerificationSessionStatus.InProgress);
        var action = () => session.Start(Now.AddMinutes(2));
        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_VERIFICATION_INVALID_TRANSITION");
    }

    [Fact]
    public void Session_start_rejects_non_locked_plan_and_active_session()
    {
        var notLocked = () => LoadVerificationSession.EnsureStartAllowed(LoadPlanStatus.Valid, null);
        notLocked.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_PLAN_NOT_LOCKED");

        var active = () => LoadVerificationSession.EnsureStartAllowed(
            LoadPlanStatus.Locked,
            LoadVerificationSessionStatus.InProgress);
        active.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_VERIFICATION_ACTIVE_SESSION");
    }

    [Fact]
    public void Accepted_package_is_recorded_once_and_duplicate_is_rejected()
    {
        var session = StartSession();

        session.AcceptPackage(PackageId, Now.AddMinutes(2));
        session.AcceptedPackageIds.Should().ContainSingle().Which.Should().Be(PackageId);

        var action = () => session.AcceptPackage(PackageId, Now.AddMinutes(3));
        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PACKAGE_ALREADY_LOADED");
    }

    [Fact]
    public void Complete_requires_all_expected_packages()
    {
        var session = StartSession();

        var action = () => session.Complete(ActorId, false, Now.AddMinutes(2));

        action.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_VERIFICATION_INCOMPLETE");
        session.Status.Should().Be(LoadVerificationSessionStatus.InProgress);
    }

    [Fact]
    public void Complete_moves_session_to_completed_after_all_packages_are_accepted()
    {
        var session = StartSession();
        session.AcceptPackage(PackageId, Now.AddMinutes(2));

        session.Complete(ActorId, true, Now.AddMinutes(3));

        session.Status.Should().Be(LoadVerificationSessionStatus.Completed);
        session.CompletedBy.Should().Be(ActorId);
        session.CompletedAt.Should().Be(Now.AddMinutes(3));
    }

    [Fact]
    public void Discrepancy_close_requires_reason_and_cannot_reopen_completed_session()
    {
        var session = StartSession();

        var missingReason = () => session.CloseAsDiscrepancy(ActorId, " ", Now.AddMinutes(2));
        missingReason.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_VERIFICATION_DISCREPANCY_REASON_REQUIRED");

        session.CloseAsDiscrepancy(ActorId, "Eksik paket açıklaması", Now.AddMinutes(2));
        session.Status.Should().Be(LoadVerificationSessionStatus.Discrepancy);

        var reopen = () => session.Start(Now.AddMinutes(3));
        reopen.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_VERIFICATION_INVALID_TRANSITION");
    }

    [Fact]
    public void Scan_rejects_empty_barcode_and_non_positive_quantity()
    {
        var emptyBarcode = () => CreateScan(" ", 1);
        emptyBarcode.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PACKAGE_BARCODE_REQUIRED");

        var zeroQuantity = () => CreateScan("PKG-001", 0);
        zeroQuantity.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_VERIFICATION_QUANTITY_INVALID");
    }

    [Fact]
    public void Accepted_scan_requires_package_and_wrong_unit_requires_expected_unit()
    {
        var acceptedWithoutPackage = () => LoadVerificationScan.Create(
            Guid.NewGuid(), Now, Guid.NewGuid(), LoadPlanId, ShipmentId, null, LoadUnitId, LoadUnitId,
            "PKG-001", LoadVerificationScanStatus.Accepted, LoadVerificationScanMode.Package, 1, null, null,
            ActorId, "idem-1", "corr-1");
        acceptedWithoutPackage.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PACKAGE_REQUIRED_FOR_ACCEPTED_SCAN");

        var wrongUnitWithoutExpected = () => LoadVerificationScan.Create(
            Guid.NewGuid(), Now, Guid.NewGuid(), LoadPlanId, ShipmentId, PackageId, null, null,
            "PKG-001", LoadVerificationScanStatus.WrongUnit, LoadVerificationScanMode.Package, 1, "LOAD_UNIT_MISMATCH", "wrong",
            ActorId, "idem-2", "corr-2");
        wrongUnitWithoutExpected.Should().Throw<DomainException>().Which.Error.Code.Should().Be("EXPECTED_LOAD_UNIT_REQUIRED");
    }

    [Fact]
    public void Scan_normalizes_barcode_and_audit_keys()
    {
        var scan = LoadVerificationScan.Create(
            Guid.NewGuid(), Now, Guid.NewGuid(), LoadPlanId, ShipmentId, PackageId, LoadUnitId, LoadUnitId,
            "  PKG-001  ", LoadVerificationScanStatus.Accepted, LoadVerificationScanMode.Package, 4000,
            null, null, ActorId, " idem-1 ", " corr-1 ");

        scan.Barcode.Should().Be("PKG-001");
        scan.IdempotencyKey.Should().Be("idem-1");
        scan.CorrelationId.Should().Be("corr-1");
        scan.QuantityBase.Should().Be(4000);
    }

    [Fact]
    public void Package_policy_rejects_wrong_plan_cancelled_package_and_wrong_unit()
    {
        var wrongPlan = () => LoadVerificationPolicy.EnsurePackageCanBeAccepted(
            LoadPlanStatus.Locked, LoadVerificationSessionStatus.InProgress, ShipmentPackageStatus.Available,
            false, LoadUnitId, LoadUnitId);
        wrongPlan.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PACKAGE_NOT_IN_LOAD_PLAN");

        var cancelled = () => LoadVerificationPolicy.EnsurePackageCanBeAccepted(
            LoadPlanStatus.Locked, LoadVerificationSessionStatus.InProgress, ShipmentPackageStatus.Cancelled,
            true, LoadUnitId, LoadUnitId);
        cancelled.Should().Throw<DomainException>().Which.Error.Code.Should().Be("PACKAGE_CANCELLED");

        var wrongUnit = () => LoadVerificationPolicy.EnsurePackageCanBeAccepted(
            LoadPlanStatus.Locked, LoadVerificationSessionStatus.InProgress, ShipmentPackageStatus.Available,
            true, LoadUnitId, Guid.NewGuid());
        wrongUnit.Should().Throw<DomainException>().Which.Error.Code.Should().Be("LOAD_UNIT_MISMATCH");
    }

    [Fact]
    public void Package_policy_accepts_locked_in_progress_available_package()
    {
        var action = () => LoadVerificationPolicy.EnsurePackageCanBeAccepted(
            LoadPlanStatus.Locked, LoadVerificationSessionStatus.InProgress, ShipmentPackageStatus.Available,
            true, LoadUnitId, LoadUnitId);

        action.Should().NotThrow();
    }

    private static LoadVerificationSession CreateSession()
        => LoadVerificationSession.Create(Guid.NewGuid(), Now, LoadPlanId, ShipmentId, ActorId);

    private static LoadVerificationSession StartSession()
    {
        var session = CreateSession();
        session.Start(Now.AddMinutes(1));
        return session;
    }

    private static LoadVerificationScan CreateScan(string barcode, decimal quantity)
        => LoadVerificationScan.Create(
            Guid.NewGuid(), Now, Guid.NewGuid(), LoadPlanId, ShipmentId, PackageId, LoadUnitId, LoadUnitId,
            barcode, LoadVerificationScanStatus.Accepted, LoadVerificationScanMode.Package, quantity, null, null,
            ActorId, "idem-1", "corr-1");
}
