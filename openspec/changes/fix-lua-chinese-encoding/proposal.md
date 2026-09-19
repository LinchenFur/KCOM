# Fix Lua Chinese Encoding

## Why

The desktop UI and WebSocket messages support Chinese, but embedded Lua uses KeraLua's default ASCII string conversion. Chinese text emitted by the localized Lua scripts is therefore shown as `?` in the server status log.

## Scope

Configure the embedded Lua runtime to use UTF-8 for all Lua/CLR string conversions. Do not change protocol field names, commands, map names, or gameplay behavior.
