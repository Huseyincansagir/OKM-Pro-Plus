-- L4-B6: DispatchRun and route execution events
-- Canonical PostgreSQL design script for the EF Core migration AddDispatchRunsAndRouteExecution.
-- Production execution is performed by the application migration runner.

BEGIN;

CREATE TABLE dispatch_runs (
    id uuid PRIMARY KEY,
    shipment_id uuid NOT NULL REFERENCES shipments(id) ON DELETE RESTRICT,
    load_plan_id uuid NOT NULL REFERENCES load_plans(id) ON DELETE RESTRICT,
    route_plan_id uuid NOT NULL REFERENCES route_plans(id) ON DELETE RESTRICT,
    vehicle_id uuid NOT NULL REFERENCES vehicles(id) ON DELETE RESTRICT,
    driver_id uuid NOT NULL REFERENCES drivers(id) ON DELETE RESTRICT,
    status varchar(30) NOT NULL DEFAULT 'Prepared',
    planned_departure_at timestamptz,
    actual_departed_at timestamptz,
    completed_at timestamptz,
    cancelled_at timestamptz,
    created_by uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    dispatched_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    completed_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    cancelled_by uuid REFERENCES users(id) ON DELETE RESTRICT,
    exception_reason text,
    row_version bigint NOT NULL DEFAULT 1,
    created_at timestamptz NOT NULL,
    updated_at timestamptz NOT NULL,
    CONSTRAINT ck_dispatch_runs_status CHECK (
        status IN ('Prepared', 'Dispatched', 'InTransit', 'Completed', 'Cancelled')
    ),
    CONSTRAINT ck_dispatch_runs_completed_pair CHECK (
        (status <> 'Completed' AND completed_at IS NULL AND completed_by IS NULL)
        OR
        (status = 'Completed' AND completed_at IS NOT NULL AND completed_by IS NOT NULL)
    ),
    CONSTRAINT ck_dispatch_runs_cancelled_pair CHECK (
        (status <> 'Cancelled' AND cancelled_at IS NULL AND cancelled_by IS NULL)
        OR
        (status = 'Cancelled'
            AND cancelled_at IS NOT NULL
            AND cancelled_by IS NOT NULL
            AND nullif(btrim(exception_reason), '') IS NOT NULL)
    ),
    CONSTRAINT ck_dispatch_runs_departed_pair CHECK (
        status IN ('Prepared', 'Dispatched', 'Cancelled') OR actual_departed_at IS NOT NULL
    ),
    CONSTRAINT ck_dispatch_runs_time_order CHECK (
        completed_at IS NULL
        OR actual_departed_at IS NULL
        OR completed_at >= actual_departed_at
    )
);

CREATE UNIQUE INDEX ux_dispatch_runs_active_route_plan
    ON dispatch_runs(route_plan_id)
    WHERE status IN ('Prepared', 'Dispatched', 'InTransit');
CREATE UNIQUE INDEX ux_dispatch_runs_active_shipment
    ON dispatch_runs(shipment_id)
    WHERE status IN ('Prepared', 'Dispatched', 'InTransit');
CREATE UNIQUE INDEX ux_dispatch_runs_active_vehicle
    ON dispatch_runs(vehicle_id)
    WHERE status IN ('Prepared', 'Dispatched', 'InTransit');
CREATE UNIQUE INDEX ux_dispatch_runs_active_driver
    ON dispatch_runs(driver_id)
    WHERE status IN ('Prepared', 'Dispatched', 'InTransit');
CREATE INDEX ix_dispatch_runs_board
    ON dispatch_runs(status, planned_departure_at, id);
CREATE INDEX ix_dispatch_runs_shipment_history
    ON dispatch_runs(shipment_id, created_at DESC, id DESC);
CREATE INDEX ix_dispatch_runs_vehicle_history
    ON dispatch_runs(vehicle_id, created_at DESC, id DESC);

CREATE TABLE route_execution_events (
    id uuid PRIMARY KEY,
    dispatch_run_id uuid NOT NULL REFERENCES dispatch_runs(id) ON DELETE RESTRICT,
    route_plan_id uuid NOT NULL REFERENCES route_plans(id) ON DELETE RESTRICT,
    route_stop_id uuid REFERENCES route_stops(id) ON DELETE RESTRICT,
    event_type varchar(40) NOT NULL,
    sequence_no bigint NOT NULL,
    occurred_at timestamptz NOT NULL,
    location_text varchar(240),
    latitude numeric(10,7),
    longitude numeric(10,7),
    reason text,
    actor_id uuid NOT NULL REFERENCES users(id) ON DELETE RESTRICT,
    idempotency_key varchar(160) NOT NULL,
    correlation_id varchar(120) NOT NULL,
    payload_snapshot jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL,
    CONSTRAINT ck_route_execution_events_type CHECK (
        event_type IN ('Departed', 'ArrivedAtStop', 'DepartedStop', 'SkippedStop', 'RouteCompleted', 'Cancelled')
    ),
    CONSTRAINT ck_route_execution_events_sequence CHECK (sequence_no > 0),
    CONSTRAINT ck_route_execution_events_location CHECK (
        (latitude IS NULL AND longitude IS NULL)
        OR (latitude BETWEEN -90 AND 90 AND longitude BETWEEN -180 AND 180)
    ),
    CONSTRAINT ck_route_execution_events_reason CHECK (
        event_type NOT IN ('SkippedStop', 'Cancelled')
        OR nullif(btrim(reason), '') IS NOT NULL
    ),
    CONSTRAINT ck_route_execution_events_stop_pair CHECK (
        event_type IN ('Departed', 'RouteCompleted', 'Cancelled')
        OR route_stop_id IS NOT NULL
    )
);

CREATE UNIQUE INDEX ux_route_execution_events_idempotency
    ON route_execution_events(dispatch_run_id, idempotency_key);
CREATE UNIQUE INDEX ux_route_execution_events_sequence
    ON route_execution_events(dispatch_run_id, sequence_no);
CREATE INDEX ix_route_execution_events_timeline
    ON route_execution_events(dispatch_run_id, sequence_no, occurred_at, id);
CREATE INDEX ix_route_execution_events_stop
    ON route_execution_events(route_stop_id, occurred_at, id)
    WHERE route_stop_id IS NOT NULL;
CREATE INDEX ix_route_execution_events_type_time
    ON route_execution_events(event_type, occurred_at DESC);

ALTER TABLE route_stops
    ADD COLUMN actual_departure_at timestamptz,
    ADD COLUMN skipped_at timestamptz;

ALTER TABLE route_stops
    ADD CONSTRAINT ck_route_stops_execution_time_order CHECK (
        actual_departure_at IS NULL
        OR actual_arrival_at IS NULL
        OR actual_departure_at >= actual_arrival_at
    );

-- Existing values remain valid; new B6 operational values are Arrived and Departed.
ALTER TABLE route_stops
    DROP CONSTRAINT IF EXISTS ck_route_stops_status;
ALTER TABLE route_stops
    ADD CONSTRAINT ck_route_stops_status CHECK (
        status IN ('Pending', 'Arrived', 'Departed', 'InProgress', 'Delivered', 'Partial', 'Failed', 'Skipped')
    );
ALTER TABLE route_stops
    ADD CONSTRAINT ck_route_stops_skipped_reason CHECK (
        status <> 'Skipped' OR nullif(btrim(exception_reason), '') IS NOT NULL
    );

COMMIT;

-- Development-only rollback (reverse order):
-- BEGIN;
-- ALTER TABLE route_stops DROP CONSTRAINT IF EXISTS ck_route_stops_skipped_reason;
-- ALTER TABLE route_stops DROP CONSTRAINT IF EXISTS ck_route_stops_status;
-- ALTER TABLE route_stops DROP CONSTRAINT IF EXISTS ck_route_stops_execution_time_order;
-- ALTER TABLE route_stops DROP COLUMN IF EXISTS actual_departure_at;
-- ALTER TABLE route_stops DROP COLUMN IF EXISTS skipped_at;
-- DROP TABLE IF EXISTS route_execution_events;
-- DROP TABLE IF EXISTS dispatch_runs;
-- COMMIT;
