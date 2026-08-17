using FactoryErp.Domain.Common;

namespace FactoryErp.Domain.Shipping;

public enum VehicleStatus
{
    Available,
    Assigned,
    Loading,
    InTransit,
    Maintenance,
    OutOfService,
}

public enum DriverStatus
{
    Active,
    Suspended,
    Inactive,
}

public enum RoutePlanStatus
{
    Draft,
    Planned,
    Locked,
    InProgress,
    Completed,
    Exception,
    Superseded,
}

public enum RouteStopStatus
{
    Pending,
    InProgress,
    Delivered,
    Partial,
    Failed,
    Skipped,
}

public sealed class VehicleType : Entity
{
    private VehicleType(Guid id, string code, string name, bool isActive)
        : base(id, DateTimeOffset.UtcNow)
    {
        Code = code;
        Name = name;
        IsActive = isActive;
    }

    public string Code { get; private set; }
    public string Name { get; private set; }
    public bool IsActive { get; private set; }

    public static VehicleType Create(Guid id, string code, string name)
    {
        DomainGuard.AgainstEmpty(id, "VEHICLE_TYPE_ID_REQUIRED", "Araç tipi kimliği zorunludur.");
        DomainGuard.AgainstBlank(code, "VEHICLE_TYPE_CODE_REQUIRED", "Araç tipi kodu zorunludur.");
        DomainGuard.AgainstBlank(name, "VEHICLE_TYPE_NAME_REQUIRED", "Araç tipi adı zorunludur.");
        return new VehicleType(id, code.Trim(), name.Trim(), true);
    }

    public void Rename(string code, string name)
    {
        DomainGuard.AgainstBlank(code, "VEHICLE_TYPE_CODE_REQUIRED", "Araç tipi kodu zorunludur.");
        DomainGuard.AgainstBlank(name, "VEHICLE_TYPE_NAME_REQUIRED", "Araç tipi adı zorunludur.");
        Code = code.Trim();
        Name = name.Trim();
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public static VehicleType Rehydrate(Guid id, string code, string name, bool isActive)
    {
        var type = Create(id, code, name);
        type.IsActive = isActive;
        return type;
    }

    public void Activate()
    {
        IsActive = true;
    }
}

public sealed class VehicleCapacity : Entity
{
    private VehicleCapacity(
        Guid id,
        Guid vehicleTypeId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        decimal maxGrossWeight,
        decimal tareWeight,
        decimal maxUsableVolume,
        int maxPalletCount,
        decimal maxLoadHeight,
        string policySnapshot)
        : base(id, DateTimeOffset.UtcNow)
    {
        VehicleTypeId = vehicleTypeId;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        MaxGrossWeight = maxGrossWeight;
        TareWeight = tareWeight;
        MaxUsableVolume = maxUsableVolume;
        MaxPalletCount = maxPalletCount;
        MaxLoadHeight = maxLoadHeight;
        PolicySnapshot = policySnapshot;
    }

    public Guid VehicleTypeId { get; }
    public DateTimeOffset EffectiveFrom { get; }
    public DateTimeOffset? EffectiveTo { get; }
    public decimal MaxGrossWeight { get; }
    public decimal TareWeight { get; }
    public decimal MaxUsableVolume { get; }
    public int MaxPalletCount { get; }
    public decimal MaxLoadHeight { get; }
    public string PolicySnapshot { get; }

    public decimal MaxPayloadWeight => MaxGrossWeight - TareWeight;

    public static VehicleCapacity Create(
        Guid id,
        Guid vehicleTypeId,
        DateTimeOffset effectiveFrom,
        DateTimeOffset? effectiveTo,
        decimal maxGrossWeight,
        decimal tareWeight,
        decimal maxUsableVolume,
        int maxPalletCount,
        decimal maxLoadHeight,
        string policySnapshot)
    {
        DomainGuard.AgainstEmpty(id, "VEHICLE_CAPACITY_ID_REQUIRED", "Kapasite profili kimliği zorunludur.");
        DomainGuard.AgainstEmpty(vehicleTypeId, "VEHICLE_TYPE_REQUIRED", "Kapasite profili araç tipine bağlı olmalıdır.");
        DomainGuard.AgainstBlank(policySnapshot, "CAPACITY_POLICY_SNAPSHOT_REQUIRED", "Kapasite policy snapshot zorunludur.");
        if (effectiveTo is not null && effectiveTo <= effectiveFrom)
        {
            throw new DomainException(new("CAPACITY_EFFECTIVE_RANGE_INVALID", "Kapasite geçerlilik aralığı geçersizdir."));
        }

        if (maxGrossWeight <= 0 || tareWeight < 0 || tareWeight >= maxGrossWeight)
        {
            throw new DomainException(new("CAPACITY_WEIGHT_INVALID", "Araç brüt ve dara ağırlığı geçerli olmalıdır."));
        }

        if (maxUsableVolume <= 0 || maxPalletCount <= 0 || maxLoadHeight <= 0)
        {
            throw new DomainException(new("CAPACITY_LIMIT_INVALID", "Araç kapasite sınırları sıfırdan büyük olmalıdır."));
        }

        return new VehicleCapacity(
            id,
            vehicleTypeId,
            effectiveFrom,
            effectiveTo,
            maxGrossWeight,
            tareWeight,
            maxUsableVolume,
            maxPalletCount,
            maxLoadHeight,
            policySnapshot.Trim());
    }
}

public sealed class Vehicle : Entity
{
    private Vehicle(Guid id, Guid vehicleTypeId, string plateNumber, DateTimeOffset? maintenanceUntil)
        : base(id, DateTimeOffset.UtcNow)
    {
        VehicleTypeId = vehicleTypeId;
        PlateNumber = plateNumber;
        MaintenanceUntil = maintenanceUntil;
        Status = VehicleStatus.Available;
        LastStatusAt = DateTimeOffset.UtcNow;
    }

    public Guid VehicleTypeId { get; }
    public string PlateNumber { get; }
    public VehicleStatus Status { get; private set; }
    public DateTimeOffset? MaintenanceUntil { get; private set; }
    public Guid? CurrentRoutePlanId { get; private set; }
    public string? LastKnownLocationText { get; private set; }
    public DateTimeOffset LastStatusAt { get; private set; }

    public static Vehicle Create(
        Guid id,
        Guid vehicleTypeId,
        string plateNumber,
        DateTimeOffset? maintenanceUntil = null,
        string? lastKnownLocationText = null)
    {
        DomainGuard.AgainstEmpty(id, "VEHICLE_ID_REQUIRED", "Araç kimliği zorunludur.");
        DomainGuard.AgainstEmpty(vehicleTypeId, "VEHICLE_TYPE_REQUIRED", "Araç tipi zorunludur.");
        DomainGuard.AgainstBlank(plateNumber, "VEHICLE_PLATE_REQUIRED", "Araç plakası zorunludur.");
        return new Vehicle(
            id,
            vehicleTypeId,
            NormalizePlate(plateNumber),
            maintenanceUntil)
        {
            LastKnownLocationText = string.IsNullOrWhiteSpace(lastKnownLocationText) ? null : lastKnownLocationText.Trim(),
        };
    }

    public void SetMaintenance(DateTimeOffset? maintenanceUntil, DateTimeOffset now)
    {
        if (Status == VehicleStatus.InTransit)
        {
            throw new DomainException(new("VEHICLE_IN_TRANSIT", "Seyir halindeki araca bakım penceresi atanamaz."));
        }

        MaintenanceUntil = maintenanceUntil;
        if (maintenanceUntil is not null)
        {
            Status = VehicleStatus.Maintenance;
        }
        else if (Status == VehicleStatus.Maintenance)
        {
            Status = VehicleStatus.Available;
        }

        LastStatusAt = now;
    }

    public void ChangeStatus(VehicleStatus status, DateTimeOffset now)
    {
        if (Status == VehicleStatus.InTransit && status is VehicleStatus.Available or VehicleStatus.OutOfService)
        {
            throw new DomainException(new("VEHICLE_INVALID_TRANSITION", "Seyir halindeki araç doğrudan müsait veya hizmet dışı yapılamaz."));
        }

        if (status == VehicleStatus.Available && MaintenanceUntil is not null && MaintenanceUntil > now)
        {
            throw new DomainException(new("VEHICLE_MAINTENANCE", "Bakım penceresi devam eden araç müsait yapılamaz."));
        }

        Status = status;
        LastStatusAt = now;
    }

    public void AssignToRoute(Guid routePlanId, DateTimeOffset now)
    {
        DomainGuard.AgainstEmpty(routePlanId, "ROUTE_PLAN_REQUIRED", "Araç ataması için rota planı zorunludur.");
        if (Status is VehicleStatus.Maintenance or VehicleStatus.OutOfService or VehicleStatus.InTransit)
        {
            throw new DomainException(new("VEHICLE_UNAVAILABLE", "Araç mevcut durumu nedeniyle atanamaz."));
        }

        if (MaintenanceUntil is not null && MaintenanceUntil > now)
        {
            throw new DomainException(new("VEHICLE_MAINTENANCE", "Araç rota başlangıcında bakımda olacaktır."));
        }

        CurrentRoutePlanId = routePlanId;
        Status = VehicleStatus.Assigned;
        LastStatusAt = now;
    }

    public static Vehicle Rehydrate(
        Guid id,
        Guid vehicleTypeId,
        string plateNumber,
        VehicleStatus status,
        DateTimeOffset? maintenanceUntil,
        Guid? currentRoutePlanId,
        string? lastKnownLocationText,
        DateTimeOffset lastStatusAt)
    {
        var vehicle = Create(id, vehicleTypeId, plateNumber, maintenanceUntil, lastKnownLocationText);
        vehicle.Status = status;
        vehicle.CurrentRoutePlanId = currentRoutePlanId;
        vehicle.LastStatusAt = lastStatusAt;
        return vehicle;
    }

    public static string NormalizePlate(string plateNumber)
        => string.Join(' ', plateNumber.Trim().ToUpperInvariant().Split(' ', StringSplitOptions.RemoveEmptyEntries));
}

public sealed class Driver : Entity
{
    private Driver(Guid id, Guid? employeeId, string fullName, string? phone, string licenseNumber, DateOnly licenseExpiry)
        : base(id, DateTimeOffset.UtcNow)
    {
        EmployeeId = employeeId;
        FullName = fullName;
        Phone = phone;
        LicenseNumber = licenseNumber;
        LicenseExpiry = licenseExpiry;
        Status = DriverStatus.Active;
    }

    public Guid? EmployeeId { get; }
    public string FullName { get; }
    public string? Phone { get; }
    public string LicenseNumber { get; }
    public DateOnly LicenseExpiry { get; }
    public DriverStatus Status { get; private set; }

    public static Driver Create(
        Guid id,
        Guid? employeeId,
        string fullName,
        string? phone,
        string licenseNumber,
        DateOnly licenseExpiry)
    {
        DomainGuard.AgainstEmpty(id, "DRIVER_ID_REQUIRED", "Şoför kimliği zorunludur.");
        DomainGuard.AgainstBlank(fullName, "DRIVER_NAME_REQUIRED", "Şoför adı zorunludur.");
        DomainGuard.AgainstBlank(licenseNumber, "DRIVER_LICENSE_REQUIRED", "Ehliyet numarası zorunludur.");
        if (licenseExpiry < DateOnly.FromDateTime(DateTime.UtcNow.Date))
        {
            throw new DomainException(new("DRIVER_LICENSE_EXPIRED", "Ehliyet geçerlilik tarihi geçmiş olamaz."));
        }

        return new Driver(
            id,
            employeeId,
            fullName.Trim(),
            string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            licenseNumber.Trim().ToUpperInvariant(),
            licenseExpiry);
    }

    public void ChangeStatus(DriverStatus status)
    {
        Status = status;
    }

    public static Driver Rehydrate(
        Guid id,
        Guid? employeeId,
        string fullName,
        string? phone,
        string licenseNumber,
        DateOnly licenseExpiry,
        DriverStatus status)
    {
        var driver = Create(id, employeeId, fullName, phone, licenseNumber, licenseExpiry);
        driver.Status = status;
        return driver;
    }

    public void EnsureAssignable(DateOnly routeEndDate)
    {
        if (Status != DriverStatus.Active)
        {
            throw new DomainException(new("DRIVER_INACTIVE", "Şoför aktif olmadığı için atanamaz."));
        }

        if (LicenseExpiry < routeEndDate)
        {
            throw new DomainException(new("DRIVER_LICENSE_EXPIRED", "Ehliyet rota bitiş tarihine kadar geçerli değildir."));
        }
    }
}

public sealed class RouteStop : Entity
{
    private RouteStop(
        Guid id,
        int sequenceNo,
        Guid customerId,
        Guid addressId,
        DateTimeOffset? plannedArrivalAt)
        : base(id, DateTimeOffset.UtcNow)
    {
        SequenceNo = sequenceNo;
        CustomerId = customerId;
        AddressId = addressId;
        PlannedArrivalAt = plannedArrivalAt;
        Status = RouteStopStatus.Pending;
    }

    public int SequenceNo { get; private set; }
    public Guid CustomerId { get; }
    public Guid AddressId { get; }
    public RouteStopStatus Status { get; private set; }
    public DateTimeOffset? PlannedArrivalAt { get; }

    public static RouteStop Create(
        Guid id,
        int sequenceNo,
        Guid customerId,
        Guid addressId,
        DateTimeOffset? plannedArrivalAt)
    {
        DomainGuard.AgainstEmpty(id, "ROUTE_STOP_ID_REQUIRED", "Rota durağı kimliği zorunludur.");
        DomainGuard.AgainstEmpty(customerId, "ROUTE_STOP_CUSTOMER_REQUIRED", "Rota durağı müşterisi zorunludur.");
        DomainGuard.AgainstEmpty(addressId, "ROUTE_STOP_ADDRESS_REQUIRED", "Rota durağı adresi zorunludur.");
        if (sequenceNo <= 0)
        {
            throw new DomainException(new("ROUTE_STOP_SEQUENCE_INVALID", "Rota durak sırası pozitif olmalıdır."));
        }

        return new RouteStop(id, sequenceNo, customerId, addressId, plannedArrivalAt);
    }

    public void Reorder(int sequenceNo)
    {
        if (sequenceNo <= 0)
        {
            throw new DomainException(new("ROUTE_STOP_SEQUENCE_INVALID", "Rota durak sırası pozitif olmalıdır."));
        }

        SequenceNo = sequenceNo;
    }

    public static RouteStop Rehydrate(
        Guid id,
        int sequenceNo,
        Guid customerId,
        Guid addressId,
        RouteStopStatus status,
        DateTimeOffset? plannedArrivalAt)
    {
        var stop = Create(id, sequenceNo, customerId, addressId, plannedArrivalAt);
        stop.Status = status;
        return stop;
    }
}

public sealed class RoutePlan : AggregateRoot
{
    private readonly List<RouteStop> _stops = [];

    private RoutePlan(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        int version,
        DateTimeOffset? plannedStartAt,
        DateTimeOffset? plannedEndAt)
        : base(id, createdAt)
    {
        ShipmentId = shipmentId;
        Version = version;
        PlannedStartAt = plannedStartAt;
        PlannedEndAt = plannedEndAt;
        Status = RoutePlanStatus.Draft;
    }

    public Guid ShipmentId { get; }
    public int Version { get; }
    public Guid? ReplannedFromId { get; private set; }
    public Guid? VehicleId { get; private set; }
    public Guid? DriverId { get; private set; }
    public RoutePlanStatus Status { get; private set; }
    public DateTimeOffset? PlannedStartAt { get; private set; }
    public DateTimeOffset? PlannedEndAt { get; private set; }
    public IReadOnlyCollection<RouteStop> Stops => _stops;

    public static RoutePlan Create(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        int version,
        DateTimeOffset? plannedStartAt,
        DateTimeOffset? plannedEndAt)
    {
        DomainGuard.AgainstEmpty(id, "ROUTE_PLAN_ID_REQUIRED", "Rota planı kimliği zorunludur.");
        DomainGuard.AgainstEmpty(shipmentId, "SHIPMENT_REQUIRED", "Rota planı shipment kaydına bağlı olmalıdır.");
        if (version <= 0)
        {
            throw new DomainException(new("ROUTE_VERSION_INVALID", "Rota versiyonu pozitif olmalıdır."));
        }

        ValidateTimeWindow(plannedStartAt, plannedEndAt);
        return new RoutePlan(id, createdAt, shipmentId, version, plannedStartAt, plannedEndAt);
    }

    public void ReplaceStops(IEnumerable<RouteStop> stops)
    {
        EnsureDraft();
        var materialized = stops.OrderBy(x => x.SequenceNo).ToArray();
        if (materialized.Select(x => x.SequenceNo).Distinct().Count() != materialized.Length
            || materialized.Select(x => x.SequenceNo).SequenceEqual(Enumerable.Range(1, materialized.Length)) == false)
        {
            throw new DomainException(new("ROUTE_STOP_SEQUENCE_INVALID", "Rota durak sırası 1’den başlayıp boşluksuz ilerlemelidir."));
        }

        _stops.Clear();
        _stops.AddRange(materialized);
    }

    public void AssignResources(Guid vehicleId, Guid driverId)
    {
        EnsureDraftOrPlanned();
        DomainGuard.AgainstEmpty(vehicleId, "VEHICLE_REQUIRED", "Rota için araç zorunludur.");
        DomainGuard.AgainstEmpty(driverId, "DRIVER_REQUIRED", "Rota için şoför zorunludur.");
        VehicleId = vehicleId;
        DriverId = driverId;
    }

    public void SetSchedule(DateTimeOffset? plannedStartAt, DateTimeOffset? plannedEndAt)
    {
        EnsureDraft();
        ValidateTimeWindow(plannedStartAt, plannedEndAt);
        PlannedStartAt = plannedStartAt;
        PlannedEndAt = plannedEndAt;
    }

    public void Plan()
    {
        EnsureDraft();
        EnsureReadyForPlan();
        Status = RoutePlanStatus.Planned;
    }

    public void Lock()
    {
        if (Status != RoutePlanStatus.Planned)
        {
            throw new DomainException(new("ROUTE_STATE_CONFLICT", $"{Status} durumundaki rota kilitlenemez."));
        }

        EnsureReadyForPlan();
        Status = RoutePlanStatus.Locked;
    }

    public void Supersede()
    {
        if (Status != RoutePlanStatus.Locked)
        {
            throw new DomainException(new("ROUTE_STATE_CONFLICT", "Yalnızca kilitli rota superseded yapılabilir."));
        }

        Status = RoutePlanStatus.Superseded;
    }

    public static RoutePlan Rehydrate(
        Guid id,
        DateTimeOffset createdAt,
        Guid shipmentId,
        int version,
        Guid? replannedFromId,
        Guid? vehicleId,
        Guid? driverId,
        RoutePlanStatus status,
        DateTimeOffset? plannedStartAt,
        DateTimeOffset? plannedEndAt,
        IEnumerable<RouteStop> stops)
    {
        var plan = Create(id, createdAt, shipmentId, version, plannedStartAt, plannedEndAt);
        plan.ReplannedFromId = replannedFromId;
        plan.VehicleId = vehicleId;
        plan.DriverId = driverId;
        plan.Status = status;
        plan._stops.AddRange(stops.OrderBy(x => x.SequenceNo));
        return plan;
    }

    private void EnsureDraft()
    {
        if (Status != RoutePlanStatus.Draft)
        {
            throw new DomainException(new("ROUTE_STATE_CONFLICT", $"{Status} durumundaki rota düzenlenemez."));
        }
    }

    private void EnsureDraftOrPlanned()
    {
        if (Status is not (RoutePlanStatus.Draft or RoutePlanStatus.Planned))
        {
            throw new DomainException(new("ROUTE_STATE_CONFLICT", $"{Status} durumundaki rotaya kaynak atanamaz."));
        }
    }

    private void EnsureReadyForPlan()
    {
        if (VehicleId is null || DriverId is null)
        {
            throw new DomainException(new("ROUTE_RESOURCES_REQUIRED", "Planlama için araç ve şoför atanmalıdır."));
        }

        if (PlannedStartAt is null || PlannedEndAt is null)
        {
            throw new DomainException(new("ROUTE_TIME_WINDOW_REQUIRED", "Planlama için rota zaman aralığı zorunludur."));
        }

        if (_stops.Count == 0)
        {
            throw new DomainException(new("ROUTE_STOPS_REQUIRED", "Planlama için en az bir rota durağı gereklidir."));
        }
    }

    private static void ValidateTimeWindow(DateTimeOffset? start, DateTimeOffset? end)
    {
        if (start is not null && end is not null && end <= start)
        {
            throw new DomainException(new("ROUTE_TIME_WINDOW_INVALID", "Rota bitiş zamanı başlangıç zamanından sonra olmalıdır."));
        }
    }
}
