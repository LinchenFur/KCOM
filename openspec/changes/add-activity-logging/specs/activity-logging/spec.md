# Activity Logging

## ADDED Requirements

### Requirement: Persist every operation relevant to synchronization

KCOM MUST persist UI actions, VConsole input/output, WebSocket messages, and game synchronization packets in a per-session activity log. Password values MUST NOT be written.

#### Scenario: A player performs a game action

- **WHEN** the addon emits a KCOM packet such as `HEAD`, `PHYS`, `FIRE`, `NPHP`, `SPWN`, `RESC`, or `ALIV`
- **THEN** the server activity log contains the actor, map, packet type, and sanitized packet detail

#### Scenario: The client executes a server command

- **WHEN** the server sends a VConsole command to a client
- **THEN** the client activity log contains the command execution event

#### Scenario: Logging fails

- **WHEN** the log file cannot be written
- **THEN** synchronization continues without throwing the logging error to the game or network path
