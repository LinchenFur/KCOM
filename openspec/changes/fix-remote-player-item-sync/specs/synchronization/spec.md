# Synchronization

## ADDED Requirements

### Requirement: Remote dynamic entities use a stable synchronization name

The addon MUST send a stable synchronization name for a picked-up or dynamically spawned entity, and the receiving client MUST reuse an existing entity with that name or create one before applying later position updates.

#### Scenario: A player picks up an item

- **WHEN** the sender reports an item pickup
- **THEN** the server forwards the sender's synchronization name and the receiver creates or reuses the item at the reported origin

#### Scenario: A later physics update arrives

- **WHEN** the receiver has not placed the entity in its UUID cache
- **THEN** the receiver resolves the entity by synchronization name and applies the position and angle update

### Requirement: Remote player proxies remain addressable

The addon MUST resolve named player proxy entities when processing remote head and hand updates, including when the bulk name lookup returns no entries.

#### Scenario: A remote head update is received

- **WHEN** the client receives a `kcom_setlocation_nonuuid` command for a player proxy
- **THEN** it locates the named proxy and applies the update instead of silently dropping it


### Requirement: The game script exposes synchronization health and disconnect cleanup

The addon MUST periodically report a lightweight heartbeat after initialization, and the server MUST clear a disconnected player's remote proxy on other ready clients.

#### Scenario: The game timer is running

- **WHEN** the initialized addon executes its interval callback
- **THEN** it periodically emits `ALIV KCOM` so the server can distinguish a live sync loop from a connected but inactive WebSocket

#### Scenario: A ready client disconnects

- **WHEN** the server receives the client's close event
- **THEN** other ready clients receive a cleanup command and move that player's head, hands, and label out of the map
