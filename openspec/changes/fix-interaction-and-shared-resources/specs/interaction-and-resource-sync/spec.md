# Interaction and Resource Synchronization

## ADDED Requirements

### Requirement: Interactive entity outputs are synchronized

KCOM MUST track common doors, moving doors, buttons, triggers, gameplay items, NPCs, and known short-lived projectile entities, and MUST forward supported interaction outputs as `FIRE` events to Ready clients on the same map.

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

### Requirement: NPC health and projectile movement are synchronized

KCOM MUST synchronize tracked NPC movement and health changes, and MUST discover known short-lived projectile entities during the periodic sync pass so their spawn, movement, and removal can be forwarded to other Ready clients.

#### Scenario: An NPC is damaged

- **WHEN** a tracked NPC loses health or dies
- **THEN** the addon emits `NPHP`
- **AND** other clients apply the same health change and death state

#### Scenario: A monster projectile is created

- **WHEN** a known projectile entity appears and moves on one client
- **THEN** the addon emits `SPWN` and `PHYS` for its stable identifier
- **AND** other clients create, move, and remove the corresponding entity when it expires

### Requirement: Hitscan damage feedback is synchronized

KCOM MUST forward local player damage events as sanitized `HURT` packets so other clients can render feedback on the corresponding remote player proxy.

#### Scenario: A player is hit by an instant attack

- **WHEN** the local game emits `player_hurt`
- **THEN** the addon emits the player's damage, current health, and origin in a `HURT` packet
- **AND** the server forwards a `kcom_player_hurt` command to other Ready clients on the same map
- **AND** the receiving client plays damage feedback at the matching remote player proxy

### Requirement: Physics props use stable spawn and movement synchronization

KCOM MUST discover common physics props such as bottles, cans, loose containers, and interactive props after map initialization, emit one stable `SPWN` (including the model when available), and continue forwarding their `PHYS` movement until removal.

#### Scenario: A player throws a bottle or can

- **WHEN** a physics prop is picked up or newly discovered and then moves
- **THEN** the addon emits one stable `SPWN` followed by `PHYS` updates
- **AND** the receiving client reuses the same entity for subsequent movement and removal events
