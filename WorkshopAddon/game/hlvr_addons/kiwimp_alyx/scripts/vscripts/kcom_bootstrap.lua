-- Invoked by the authenticated desktop client, not by localized console output.
-- Do not cache this file with require: readiness must be checked after map changes.
local player = Entities:GetLocalPlayer()
if not IsValidEntity(player) or not KCOM_BOOTSTRAP_SESSION then return end

local sameSession = KCOM_BOOTSTRAP_STARTED_SESSION == KCOM_BOOTSTRAP_SESSION
local script = Entities:FindByName(nil, "kcom_script")
local scope = IsValidEntity(script) and script:GetPrivateScriptScope()
if sameSession and scope and scope.KCOM_INITIALIZED
    and IsValidEntity(Entities:FindByName(nil, "kcom_timer")) then return end

local now = Time()
if sameSession and KCOM_BOOTSTRAP_RETRY_AT and now < KCOM_BOOTSTRAP_RETRY_AT then return end
if sameSession then print("KERR KCOM") end
KCOM_BOOTSTRAP_STARTED_SESSION = KCOM_BOOTSTRAP_SESSION
KCOM_BOOTSTRAP_RETRY_AT = now + 10
print("KRDY KCOM")
