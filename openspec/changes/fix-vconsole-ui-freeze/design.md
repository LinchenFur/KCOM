# Design

Replace the modal `MessageBox` and recursive retry in `ClientProgram.ConnectVConsole` with one non-blocking log message. This is the smallest safe fix: VConsole failure no longer blocks the UI thread, while the existing WebSocket connection and server/client session continue to run. The user can restart KCOM after Alyx exposes the VConsole port.
