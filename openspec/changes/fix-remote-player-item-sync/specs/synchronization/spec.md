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

