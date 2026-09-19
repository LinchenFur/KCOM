# Design

Set `Lua.Encoding` to `Encoding.UTF8` immediately after creating the KeraLua/NLua runtime and before loading any scripts. This is the smallest supported fix: KeraLua documents ASCII as its default and exposes an encoding property specifically for string conversion.

The change affects Lua strings crossing the CLR boundary, including localized status messages and script responses. WebSocket serialization remains unchanged because it already uses .NET strings and Newtonsoft.Json.
