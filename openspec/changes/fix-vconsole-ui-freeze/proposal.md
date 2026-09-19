# Fix VConsole Connection UI Freeze

## Why

When Alyx's VConsole endpoint is unavailable, KCOM opens a modal reconnect dialog from the startup path and recursively retries when the user selects Yes. This can make the KCOM window appear frozen and can prevent the rest of the client from remaining responsive.

## Scope

Treat VConsole as an optional connection during startup: log the failure and continue without a modal dialog or recursive retry. WebSocket multiplayer connection behavior and game protocol messages remain unchanged.
