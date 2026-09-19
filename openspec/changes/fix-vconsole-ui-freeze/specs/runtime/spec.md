# Runtime Specification

## ADDED Requirements

### Requirement: VConsole failure does not block the UI

KCOM SHALL keep the desktop UI responsive when the Alyx VConsole endpoint is unavailable during startup.

#### Scenario: VConsole port is unavailable

- **WHEN** the client cannot connect to the configured VConsole port
- **THEN** KCOM SHALL write a non-blocking diagnostic to the output log
- **AND** KCOM SHALL NOT open a modal reconnect dialog
- **AND** KCOM SHALL NOT recursively retry on the UI thread
- **AND** the multiplayer WebSocket client SHALL remain running
