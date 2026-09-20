/*
    Kiwi's Co-Op Mod for Half-Life: Alyx
    Copyright (c) 2022 KiwifruitDev
    All rights reserved.
    This software is licensed under the MIT License.
    -----------------------------------------------------------------------------
    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
    -----------------------------------------------------------------------------
*/
using Fleck;
using KiwisCoOpModCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace AlyxGamemode
{
    public class AlyxGamemode : CoreGamemode
    {
        public AlyxGamemode() : base()
        {
            Author = "KiwifruitDev";
            Name = "Half-Life: Alyx";
            Description = "Play Half-Life: Alyx with up to 16 players!";
        }
        private static readonly Dictionary<Guid, HashSet<string>> compatibilityEvents = new();
        private static bool IsSafeCompatibilityName(string value) => value.Length is > 0 and <= 64 && value.All(c =>
            (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c is '_' or '-');
        private static bool TryParseCompatibilityRegistration(string data, out string key)
        {
            key = "";
            string[] parts = data.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 4 || parts[0] != "XREG" || parts[3] != "KCOM" ||
                !IsSafeCompatibilityName(parts[1]) || !IsSafeCompatibilityName(parts[2])) return false;
            key = parts[1] + ":" + parts[2];
            return true;
        }
        private static bool TryParseCompatibilityEvent(string data, out string key, out string payload)
        {
            key = ""; payload = "";
            string value = data.Trim();
            if (!value.StartsWith("XEVT ", StringComparison.Ordinal) || !value.EndsWith(" KCOM", StringComparison.Ordinal)) return false;
            string body = value[5..^5].Trim();
            int first = body.IndexOf(' '), second = first < 0 ? -1 : body.IndexOf(' ', first + 1);
            if (first <= 0 || second <= first + 1) return false;
            string ns = body[..first], eventName = body[(first + 1)..second];
            payload = body[(second + 1)..];
            if (!IsSafeCompatibilityName(ns) || !IsSafeCompatibilityName(eventName) || payload.Length > 1024 ||
                payload.Any(c => c is '\0' or '\r' or '\n' or ';' or '"' or '\\')) return false;
            key = ns + ":" + eventName;
            return payload.Length > 0;
        }

        private readonly record struct ResourceSnapshot(int Energygun, int Rapidfire, int Shotgun, int Resin)
        {
            public static ResourceSnapshot operator +(ResourceSnapshot left, ResourceSnapshot right) =>
                new(left.Energygun + right.Energygun, left.Rapidfire + right.Rapidfire, left.Shotgun + right.Shotgun, left.Resin + right.Resin);
            public static ResourceSnapshot operator -(ResourceSnapshot left, ResourceSnapshot right) =>
                new(left.Energygun - right.Energygun, left.Rapidfire - right.Rapidfire, left.Shotgun - right.Shotgun, left.Resin - right.Resin);
            public ResourceSnapshot ClampNonNegative() => new(Math.Max(0, Energygun), Math.Max(0, Rapidfire), Math.Max(0, Shotgun), Math.Max(0, Resin));
        }

        public static bool SharedResourceInventory
        {
            get => ResourceInventorySettings.Shared;
            set => ResourceInventorySettings.Shared = value;
        }
        private static ResourceSnapshot? sharedResources;
        private static readonly Dictionary<Guid, ResourceSnapshot> resourceSnapshots = new();
        public static void ResetResourceInventory()
        {
            sharedResources = null;
            resourceSnapshots.Clear();
        }

        private static bool TryParseResources(Packet packet, out ResourceSnapshot resources)
        {
            resources = default;
            if (packet.args.Length != 5 || packet.args[4] != "KCOM") return false;
            int[] values = new int[4];
            for (int i = 0; i < values.Length; i++)
                if (!int.TryParse(packet.args[i], NumberStyles.None, CultureInfo.InvariantCulture, out values[i]) || values[i] < 0) return false;
            resources = new(values[0], values[1], values[2], values[3]);
            return true;
        }

        private static string ResourceCommand(ResourceSnapshot resources) =>
            "kcom_setresources " + resources.Energygun + " " + resources.Rapidfire + " " + resources.Shotgun + " " + resources.Resin;

        private static void BroadcastResources(ResourceSnapshot resources, Player sender, List<IndexedClient> connections)
        {
            Response update = new("command", ResourceCommand(resources));
            foreach (IndexedClient broadcast in connections)
            {
                Player? recipient = AlyxGlobalData.instance.GetPlayer(broadcast.Session.ConnectionInfo.Id);
                if (CanReceiveSync(recipient, sender, Guid.Empty))
                    broadcast.Session.Send(update.ToString());
            }
        }

        private static bool CanReceiveSync(Player? recipient, Player sender, Guid senderId)
        {
            return sender.InitializationStage == InitializationStage.Ready &&
                   recipient != null && recipient.InitializationStage == InitializationStage.Ready &&
                   recipient.Client.Map == sender.Client.Map &&
                   recipient.Client.Session.ConnectionInfo.Id != senderId;
        }

        public AlyxGamemode(GamemodeHandleType type, params object[]? vs)
        {
            int APIVersion = 4;
            State = HandleState.Continue;
            bool overrideState = false;
            Random rnd = new Random();
            try
            {
                switch (type)
                {
                    case GamemodeHandleType.ClientOpen:
                        if (vs != null)
                        {
                            List<IndexedClient> openConnections = (List<IndexedClient>)vs[0];
                            IWebSocketConnection openSocket = (IWebSocketConnection)vs[1];
                            Player? player2 = AlyxGlobalData.instance.GetPlayer(openSocket.ConnectionInfo.Id);
                            Response enableAddon = new("command", "addon_enable 2739356543;addon_enable kiwimp_alyx");
                            openSocket.Send(JsonConvert.SerializeObject(enableAddon));
                        }
                        break;
                    case GamemodeHandleType.ClientClose:
                        if (vs != null)
                        {
                            List<IndexedClient> closeConnections = (List<IndexedClient>)vs[0];
                            IWebSocketConnection closeSocket = (IWebSocketConnection)vs[1];
                            Player? player = AlyxGlobalData.instance.GetPlayer(closeSocket.ConnectionInfo.Id);
                            if (player != null)
                            {
                                Response removeProxy = new("command", "kcom_remove_player " + player.Index);
                                foreach (IndexedClient recipientClient in closeConnections)
                                {
                                    Player? recipient = AlyxGlobalData.instance.GetPlayer(recipientClient.Session.ConnectionInfo.Id);
                                    if (CanReceiveSync(recipient, player, closeSocket.ConnectionInfo.Id))
                                        recipientClient.Session.Send(removeProxy.ToString());
                                }
                                lock (player)
                                {
                                    player.InitializationGeneration++;
                                    player.InitializationStage = InitializationStage.None;
                                    player.SyncHeartbeatReported = false;
                                    resourceSnapshots.Remove(closeSocket.ConnectionInfo.Id);
                                    compatibilityEvents.Remove(closeSocket.ConnectionInfo.Id);
                                    AlyxGlobalData.instance.RemovePlayer(closeSocket.ConnectionInfo.Id);
                                }
                            }
                        }
                        break;
                    case GamemodeHandleType.PreResponse:
                        if (vs != null)
                        {
                            Response response = (Response)vs[0];
                            List<IndexedClient> connections = (List<IndexedClient>)vs[1];
                            IWebSocketConnection socket = (IWebSocketConnection)vs[2];
                            string map = (string)vs[3];
                            switch (response.type)
                            {
                                case "print":
                                    if (response.data != null)
                                    {
                                        if (response.data.Trim().Equals("KRDY KCOM", StringComparison.Ordinal))
                                        {
                                            if (!connections.Any(c => c.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id)) break;
                                            if (AlyxGlobalData.instance.GetPlayer(socket.ConnectionInfo.Id) == null)
                                            {
                                                if (AlyxGlobalData.instance.GetPlayers().Count == 0) ResetResourceInventory();
                                                foreach (IndexedClient client in connections)
                                                {
                                                    if (client.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id)
                                                    {
                                                        Player? player = AlyxGlobalData.instance.AddPlayer(client);
                                                        if (player != null)
                                                        {
                                                            Response output2 = new("status", "♩ " + client.Username + " joined as index " + player.Index);
                                                            Response blip = new("command", "play kcom/blip" + rnd.Next(1, 4));
                                                            connections.ForEach(c =>
                                                            {
                                                                if (c.Session.ConnectionInfo.Id != client.Session.ConnectionInfo.Id)
                                                                {
                                                                    c.Session.Send(JsonConvert.SerializeObject(blip));
                                                                    c.Session.Send(JsonConvert.SerializeObject(output2));
                                                                }
                                                            });
                                                        }
                                                        else
                                                        {
                                                            Response output2 = new("status", "The server is currently at maximum capacity, please try again later.");
                                                            socket.Send(JsonConvert.SerializeObject(output2));
                                                            socket.Close();
                                                        }
                                                        break;
                                                    }
                                                }
                                            }
                                            Player? readyPlayer = AlyxGlobalData.instance.GetPlayer(socket.ConnectionInfo.Id);
                                            if (readyPlayer != null)
                                            {
                                                lock (readyPlayer)
                                                {
                                                    readyPlayer.InitializationGeneration++;
                                                    readyPlayer.InitializationStage = InitializationStage.AwaitInit;
                                                    readyPlayer.SyncHeartbeatReported = false;
                                                    Response initialize = new("command", "sv_cheats 1;ent_remove_all kcom_script;ent_remove_all kcom_timer;echo INIT KCOM");
                                                    socket.Send(JsonConvert.SerializeObject(initialize));
                                                }
                                            }
                                        }
                                        else if (TryParseCompatibilityRegistration(response.data, out string registration))
                                        {
                                            if (!compatibilityEvents.TryGetValue(socket.ConnectionInfo.Id, out HashSet<string>? registered))
                                                compatibilityEvents[socket.ConnectionInfo.Id] = registered = new(StringComparer.Ordinal);
                                            registered.Add(registration);
                                        }
                                        else if (TryParseCompatibilityEvent(response.data, out string eventKey, out string payload))
                                        {
                                            Player? sender = AlyxGlobalData.instance.GetPlayer(socket.ConnectionInfo.Id);
                                            if (sender != null && compatibilityEvents.TryGetValue(socket.ConnectionInfo.Id, out HashSet<string>? registered) && registered.Contains(eventKey))
                                            {
                                                string[] names = eventKey.Split(':', 2);
                                                Response extension = new("command", "kcom_compat_event " + names[0] + " " + names[1] + " \"" + payload + "\"");
                                                foreach (IndexedClient broadcast in connections)
                                                {
                                                    Player? recipient = AlyxGlobalData.instance.GetPlayer(broadcast.Session.ConnectionInfo.Id);
                                                    if (CanReceiveSync(recipient, sender, socket.ConnectionInfo.Id))
                                                        broadcast.Session.Send(extension.ToString());
                                                }
                                            }
                                        }
                                        else if (response.data.Trim().Equals("KERR KCOM", StringComparison.Ordinal))
                                        {
                                            if (connections.Any(c => c.Session.ConnectionInfo.Id == socket.ConnectionInfo.Id))
                                                socket.Send(new Response("status", "游戏脚本初始化尚未完成，正在重试。请检查本地 kiwimp_alyx addon 和 kcom_interval.lua；可打开“客户端：显示 VConsole”查看脚本错误。").ToString());
                                        }
                                        else if (response.data.Contains("KCOM"))
                                        {
                                            List<string> packetList = response.data.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).ToList();
                                            string packetType = packetList[0];
                                            packetList.Remove(packetType);
                                            Packet packet = new(packetType, packetList.ToArray());
                                            Player? logPlayer = AlyxGlobalData.instance.GetPlayer(socket.ConnectionInfo.Id);
                                            bool validPacket = packet.IsValid();
                                            ActivityLog.Write("SYNC", logPlayer?.Client.Username, logPlayer?.Client.Map ?? map, validPacket ? packet.ToString() : "invalid_packet", validPacket ? string.Join(" ", packet.args.Take(Math.Max(0, packet.args.Length - 1))) : response.data);
                                            if (validPacket)
                                            {
                                                Player? pair = logPlayer;
                                                if (pair != null)
                                                {
                                                    Player player = pair;
                                                    switch (packet.type)
                                                    {
                                                        case PacketType.Spawn:
                                                            Response spawn = new("command", "kcom_spawn " + packet.args[0] + " " + packet.args[1] + " " + packet.args[2] + " " + packet.args[3] + " " + packet.args[4]);
                                                            foreach (IndexedClient broadcastClient2 in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcastClient2.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    broadcastClient2.Session.Send(JsonConvert.SerializeObject(spawn));
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.Teleport:
                                                            Vector TeleOrigin = new(
                                                                float.Parse(packet.args[0], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[1], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[2], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Angle TeleAngles = new(
                                                                float.Parse(packet.args[3], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[4], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[5], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Response teleport = new("command", "kcom_teleportangles " + TeleOrigin + " " + TeleAngles);
                                                            foreach (IndexedClient broadcastClient2 in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcastClient2.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    broadcastClient2.Session.Send(JsonConvert.SerializeObject(teleport));
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.HeadPosAng:
                                                            Vector HeadOrigin = new(
                                                                float.Parse(packet.args[0], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[1], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[2], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Angle HeadAngles = new(
                                                                float.Parse(packet.args[3], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[4], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[5], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Vector HatOrigin = new(
                                                                HeadOrigin.X,
                                                                HeadOrigin.Y,
                                                                HeadOrigin.Z + 10
                                                            );
                                                            Vector HatAngles = new(
                                                                0,
                                                                HeadAngles.Yaw + 90,
                                                                90
                                                            );
                                                            Response movement = new("command", "kcom_setlocation_nonuuid kcom_head_" + player.Index + " " + HeadOrigin + " " + HeadAngles);
                                                            Response hatMovement = new("command", "kcom_setlocation_nonuuid kcom_text_" + player.Index + " " + HatOrigin + " " + HatAngles);
                                                            Response headText = new("command", "ent_fire kcom_text_" + player.Index + " setmessage \"" + player.Client.Username + "\"");
                                                            foreach (IndexedClient broadcastClient2 in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcastClient2.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    broadcastClient2.Session.Send(JsonConvert.SerializeObject(movement));
                                                                    broadcastClient2.Session.Send(JsonConvert.SerializeObject(hatMovement));
                                                                    broadcastClient2.Session.Send(JsonConvert.SerializeObject(headText));
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.HandPosAng:
                                                            Vector LeftHandOrigin = new(
                                                                float.Parse(packet.args[0], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[1], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[2], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Angle LeftHandAngles = new(
                                                                float.Parse(packet.args[3], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[4], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[5], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Vector RightHandOrigin = new(
                                                                float.Parse(packet.args[6], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[7], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[8], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Angle RightHandAngles = new(
                                                                float.Parse(packet.args[9], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[10], CultureInfo.InvariantCulture.NumberFormat),
                                                                float.Parse(packet.args[11], CultureInfo.InvariantCulture.NumberFormat)
                                                            );
                                                            Response movementLeftHand = new("command", "kcom_setlocation_nonuuid kcom_lefthand_" + player.Index + " " + LeftHandOrigin + " " + LeftHandAngles);
                                                            Response movementRightHand = new("command", "kcom_setlocation_nonuuid kcom_righthand_" + player.Index + " " + RightHandOrigin + " " + RightHandAngles);
                                                            foreach (IndexedClient broadcastClient2 in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcastClient2.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    broadcastClient2.Session.Send(JsonConvert.SerializeObject(movementLeftHand));
                                                                    broadcastClient2.Session.Send(JsonConvert.SerializeObject(movementRightHand));
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.Initialization:
                                                            lock (player)
                                                            {
                                                                if (player.InitializationStage != InitializationStage.AwaitInit) break;
                                                                player.InitializationStage = InitializationStage.AwaitEntities;
                                                                socket.Send(new Response("status", "正在初始化联机，等待游戏脚本回报地图……").ToString());
                                                                socket.Send(new Response("command", "ent_create logic_script {targetname kcom_script vscripts kcom_interval};ent_create logic_timer {targetname kcom_timer refiretime 0.01};echo IENT KCOM").ToString());
                                                            }
                                                            break;
                                                        case PacketType.InitializedEntities:
                                                            lock (player)
                                                            {
                                                                if (player.InitializationStage != InitializationStage.AwaitEntities) break;
                                                                player.InitializationStage = InitializationStage.AwaitTimer;
                                                                int generation = player.InitializationGeneration;
                                                                _ = Task.Run(async () =>
                                                                {
                                                                    await Task.Delay(2500);
                                                                    try
                                                                    {
                                                                        Task send;
                                                                        lock (player)
                                                                        {
                                                                            if (player.InitializationGeneration != generation ||
                                                                                player.InitializationStage != InitializationStage.AwaitTimer ||
                                                                                AlyxGlobalData.instance.GetPlayer(socket.ConnectionInfo.Id) != player || !socket.IsAvailable) return;
                                                                            player.InitializationStage = InitializationStage.AwaitMap;
                                                                            send = socket.Send(new Response("command", "buddha 1;unpause;ent_fire kcom_timer addoutput OnTimer>kcom_script>CallScriptFunction>KiwisCoOpMod>0>-1").ToString());
                                                                        }
                                                                        await send.ConfigureAwait(false);
                                                                    }
                                                                    catch (Exception e) { System.Diagnostics.Debug.WriteLine(e); }
                                                                });
                                                            }
                                                            break;
                                                        case PacketType.RightHandIndexes:
                                                        case PacketType.LeftHandIndexes:
                                                        case PacketType.HeadsetIndexes:
                                                        case PacketType.TextIndexes:
                                                        case PacketType.ColliderIndexes:
                                                        case PacketType.Prefix:
                                                        case PacketType.Alive:
                                                            if (player.InitializationStage == InitializationStage.Ready && !player.SyncHeartbeatReported)
                                                            {
                                                                player.SyncHeartbeatReported = true;
                                                                socket.Send(new Response("status", "已收到游戏同步心跳；玩家、门、怪物和物品同步通道正常。").ToString());
                                                            }
                                                            break;
                                                        case PacketType.ColliderDamage:
                                                        case PacketType.PhysicsObjectIndexStartPos:
                                                            break;
                                                        case PacketType.PhysicsObjectPosAng:
                                                            if (packet.args.Length >= 7)
                                                            {
                                                                Entity entity = new(packet.args[0], float.Parse(packet.args[1], CultureInfo.InvariantCulture), float.Parse(packet.args[2], CultureInfo.InvariantCulture), float.Parse(packet.args[3], CultureInfo.InvariantCulture), float.Parse(packet.args[4], CultureInfo.InvariantCulture), float.Parse(packet.args[5], CultureInfo.InvariantCulture), float.Parse(packet.args[6], CultureInfo.InvariantCulture));
                                                                if (AlyxGlobalData.instance.AddManipulatedEntity(entity, socket.ConnectionInfo.Id, connections))
                                                                {
                                                                    Response output4 = new("command", "kcom_setlocation " + packet.args[0] + " " + packet.args[1] + " " + packet.args[2] + " " + packet.args[3] + " " + packet.args[4] + " " + packet.args[5] + " " + packet.args[6]);
                                                                    foreach (IndexedClient broadcastClient2 in connections)
                                                                    {
                                                                        Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcastClient2.Session.ConnectionInfo.Id);
                                                                        if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                        {
                                                                            broadcastClient2.Session.Send(JsonConvert.SerializeObject(output4));
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.ResourceSnapshot:
                                                            if (!TryParseResources(packet, out ResourceSnapshot resources) || player.InitializationStage != InitializationStage.Ready)
                                                                break;
                                                            if (!SharedResourceInventory)
                                                                break;
                                                            Guid resourceId = socket.ConnectionInfo.Id;
                                                            if (!sharedResources.HasValue)
                                                                sharedResources = resources;
                                                            else if (resourceSnapshots.TryGetValue(resourceId, out ResourceSnapshot previous))
                                                                sharedResources = (sharedResources.Value + (resources - previous)).ClampNonNegative();
                                                            resourceSnapshots[resourceId] = resources;
                                                            BroadcastResources(sharedResources.Value, player, connections);
                                                            break;
                                                        case PacketType.MapName:
                                                            if (packet.args.Length != 3 || packet.args[2] != "KCOM" ||
                                                                !Map.TryNormalize(packet.args[0], out string detectedMap) ||
                                                                !int.TryParse(packet.args[1], NumberStyles.None, CultureInfo.InvariantCulture, out int gamemodeAPIVersion))
                                                            {
                                                                socket.Send(new Response("status", "游戏返回的 MAPN 地图回报格式无效，尚未确认初始化成功。").ToString());
                                                                break;
                                                            }
                                                            if (gamemodeAPIVersion != APIVersion)
                                                            {
                                                                socket.Send(new Response("status", "游戏脚本 API 版本不匹配：需要 " + APIVersion + "，收到 " + gamemodeAPIVersion + "。请更新 addon 脚本；尚未确认初始化成功。").ToString());
                                                                break;
                                                            }
                                                            lock (player)
                                                            {
                                                                if (player.InitializationStage != InitializationStage.AwaitMap) break;
                                                                player.InitializationStage = InitializationStage.Ready;
                                                                player.Client.Map = detectedMap;
                                                                Map.map = detectedMap;
                                                                socket.Send(new Response("command", "play kcom/jingle_up2").ToString());
                                                                socket.Send(new Response("status", "联机初始化完成；已识别地图（Detected map）：" + detectedMap).ToString());
                                                            }
                                                            break;
                                                        case PacketType.ButtonIndexStartPos:
                                                        case PacketType.ButtonPressIndex:
                                                        case PacketType.DoorIndexStartPos:
                                                        case PacketType.TriggerIndexStartPos:
                                                        case PacketType.TriggerActivateIndex:
                                                            break;
                                                        case PacketType.BrokenProp:
                                                            Response removeBreak = new("command", "kcom_break " + packet.args[0]);
                                                            foreach (IndexedClient broadcast in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcast.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    Response breakprop = new("status", "Broken prop: " + packet.args[0]);
                                                                    broadcast.Session.Send(JsonConvert.SerializeObject(removeBreak));
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.EntityRemoved:
                                                            Response remove = new("command", "ent_remove " + packet.args[0]);
                                                            foreach (IndexedClient broadcast in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcast.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    broadcast.Session.Send(JsonConvert.SerializeObject(remove));
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.EntityFired:
                                                            Entity entityfired = new(packet.args[0]);
                                                            Response fire = new("command", "kcom_fireoutput " + packet.args[0] + " " + packet.args[1]);
                                                            foreach (IndexedClient broadcast in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcast.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    broadcast.Session.Send(JsonConvert.SerializeObject(fire));
                                                                }
                                                            }
                                                            break;
                                                        case PacketType.NPCHealth:
                                                            Response p = new("command", "kcom_npc_sethealth " + packet.args[0] + " " + packet.args[1]);
                                                            foreach (IndexedClient broadcast in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcast.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                    broadcast.Session.Send(JsonConvert.SerializeObject(p));
                                                            }
                                                            break;
                                                        case PacketType.KCOMCommand:
                                                            response.type = "chat";
                                                            response.data = "/" + string.Join(" ", packet.args.Take(packet.args.Count() - 1));
                                                            State = HandleState.Continue;
                                                            overrideState = true;
                                                            break;
                                                        case PacketType.TemplateEntity:
                                                            Response templateCache = new("command", "kcom_cache_entity " + packet.args[0]);
                                                            foreach (IndexedClient broadcast in connections)
                                                            {
                                                                Player? keyValuePair = AlyxGlobalData.instance.GetPlayer(broadcast.Session.ConnectionInfo.Id);
                                                                if (CanReceiveSync(keyValuePair, player, socket.ConnectionInfo.Id))
                                                                {
                                                                    broadcast.Session.Send(JsonConvert.SerializeObject(templateCache));
                                                                }
                                                            }
                                                            break;
                                                    }
                                                }
                                                if(!overrideState)
                                                    State = HandleState.Handled;
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                    case GamemodeHandleType.PostResponse:
                        if (vs != null)
                        {
                            Response response = (Response)vs[0];
                            List<IndexedClient> connections = (List<IndexedClient>)vs[1];
                            IWebSocketConnection socket = (IWebSocketConnection)vs[2];
                            switch (response.type)
                            {
                                case "chat":
                                    if (AlyxGlobalData.instance.GetPlayer(socket.ConnectionInfo.Id) != null)
                                    {
                                        if (response.data != null)
                                        {
                                            if (!response.data.StartsWith("/"))
                                            {
                                                Response output = new("command", "play sounds/ui/hint.vsnd");
                                                connections.ForEach(c => c.Session.Send(JsonConvert.SerializeObject(output)));
                                            }
                                        }
                                    }
                                    break;
                            }
                        }
                        break;
                }
            }
            catch (Exception e)
            {
                ActivityLog.Write("SYNC", "", "", "error", e.ToString());
            }
        }
    }
}