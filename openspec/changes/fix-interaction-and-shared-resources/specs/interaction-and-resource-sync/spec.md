# Interaction and Resource Synchronization

## ADDED Requirements

### Requirement: Interactive entity outputs are synchronized

KCOM MUST track common doors, moving doors, buttons, triggers, and gameplay items, and MUST forward supported interaction outputs as `FIRE` events to Ready clients on the same map.

#### Scenario: A player activates an interactive entity

- **WHEN** a tracked entity emits `OnPressed`, `OnPlayerUse`, `OnTrigger`, `OnStartTouch`, `OnEndTouch`, or a supported door output
- **THEN** the addon emits a stable entity identifier and output name
- **AND** the server sends `kcom_fireoutput` to other Ready clients on the same map

### Requirement: Shared resource snapshots use an authoritative baseline

When shared resource inventory is enabled, the server MUST treat the current shared snapshot as authoritative and MUST update each recipient baseline after sending it.

#### Scenario: A late client reports its local snapshot

- **GIVEN** an existing shared snapshot and a client without a previous server baseline
- **WHEN** the client sends `RESC`
- **THEN** the existing shared snapshot is not replaced by the client's unrelated initial inventory
- **AND** the client receives the authoritative shared snapshot

#### Scenario: A client changes resources after write-back

- **GIVEN** the server has written the authoritative snapshot to a client
- **WHEN** that client sends its next `RESC`
- **THEN** the server applies only the difference from the authoritative baseline
- **AND** all eligible clients receive the new shared snapshot

### Requirement: Runtime shared-resource toggles refresh clients

When the shared-resource setting changes while the server is running, KCOM MUST update the runtime setting and request fresh resource snapshots from connected clients.

#### Scenario: Shared resources are enabled at runtime

- **WHEN** the operator enables shared resource inventory
- **THEN** the server sends `kcom_sync_resources`
- **AND** each client immediately emits a fresh `RESC` snapshot
