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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxGamemode
{
    public enum PacketType
    {
        None = 0,
        PlayerPosAng,
        HeadPosAng,
        HandPosAng,
        Initialization,
        InitializedEntities,
        RightHandIndexes,
        LeftHandIndexes,
        HeadsetIndexes,
        TextIndexes,
        ColliderIndexes,
        Prefix,
        Alive,
        ColliderDamage,
        PhysicsObjectIndexStartPos,
        PhysicsObjectPosAng,
        MapName,
        ResourceSnapshot,
        ButtonIndexStartPos,
        ButtonPressIndex,
        DoorIndexStartPos,
        BrokenProp,
        TriggerIndexStartPos,
        TriggerActivateIndex,
        EntityRemoved,
        EntityFired,
        PlayerDamage,
        ParentChanged,
        NPCHealth,
        KCOMCommand,
        Teleport,
        TemplateEntity,
        Spawn,
    }
    public class Packet
    {
        public PacketType type = PacketType.None;
        public string[] args = Array.Empty<string>();
        public Packet()
        {
        }
        public Packet(string type)
        {
            this.type = ParseType(type);
        }
        public Packet(string type, string args)
        {
            this.type = ParseType(type);
            this.args = Tokenize(args);
        }
        public Packet(string type, string[] args)
        {
            this.type = ParseType(type);
            this.args = args;
        }
        public Packet(PacketType type)
        {
            this.type = type;
        }
        public Packet(PacketType type, string args)
        {
            this.type = type;
            this.args = Tokenize(args);
        }
        public Packet(PacketType type, string[] args)
        {
            this.type = type;
            this.args = args;
        }
        public bool IsValid()
        {
            if (type == PacketType.None || args.Length == 0 || args[^1] != "KCOM") return false;
            return type switch
            {
                PacketType.PlayerPosAng => args.Length == 8 && AreFiniteNumbers(0, 7),
                PacketType.HeadPosAng => args.Length == 7 && AreFiniteNumbers(0, 6),
                PacketType.HandPosAng => args.Length == 13 && AreFiniteNumbers(0, 12),
                PacketType.Initialization or PacketType.InitializedEntities => args.Length == 1,
                PacketType.PhysicsObjectPosAng => args.Length == 8 && IsSafeToken(args[0]) && AreFiniteNumbers(1, 6),
                PacketType.MapName => args.Length == 3 && IsSafeToken(args[0]) && IsNonNegativeInteger(args[1]),
                PacketType.ResourceSnapshot => args.Length == 5 && AreNonNegativeIntegers(0, 4),
                PacketType.Teleport => args.Length == 7 && AreFiniteNumbers(0, 6),
                PacketType.Spawn => (args.Length == 6 || args.Length == 7) && IsSafeToken(args[0]) && IsSafeToken(args[1]) && AreFiniteNumbers(2, 3) && (args.Length == 6 || IsSafeToken(args[5])),
                PacketType.BrokenProp or PacketType.EntityRemoved or PacketType.TemplateEntity => args.Length == 2 && IsSafeToken(args[0]),
                PacketType.EntityFired => args.Length == 3 && IsSafeToken(args[0]) && IsSafeToken(args[1]),
                PacketType.PlayerDamage => args.Length == 6 && AreFiniteNumbers(0, 5),
                PacketType.ParentChanged => args.Length == 3 && IsSafeToken(args[0]) && IsSafeToken(args[1]),
                PacketType.NPCHealth => args.Length == 3 && IsSafeToken(args[0]) && IsFiniteNumber(args[1]),
                PacketType.KCOMCommand => args.Length >= 2,
                _ => args.Length >= 1,
            };
        }

        private static string[] Tokenize(string value) =>
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        private bool AreFiniteNumbers(int start, int count)
        {
            if (start < 0 || count < 0 || start + count > args.Length) return false;
            for (int i = start; i < start + count; i++)
                if (!IsFiniteNumber(args[i])) return false;
            return true;
        }

        private bool AreNonNegativeIntegers(int start, int count)
        {
            if (start < 0 || count < 0 || start + count > args.Length) return false;
            for (int i = start; i < start + count; i++)
                if (!IsNonNegativeInteger(args[i])) return false;
            return true;
        }

        private static bool IsFiniteNumber(string value) =>
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && float.IsFinite(parsed);

        private static bool IsNonNegativeInteger(string value) =>
            int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out int parsed) && parsed >= 0;

        private static bool IsSafeToken(string value) =>
            !string.IsNullOrWhiteSpace(value) && value.IndexOfAny(new[] { '\0', '\r', '\n', ';', '"', '\\' }) < 0;

        private static PacketType ParseType(string type)
        {
            return type.ToUpperInvariant() switch
            {
                "PLYR" => PacketType.PlayerPosAng,
                "HEAD" => PacketType.HeadPosAng,
                "HAND" => PacketType.HandPosAng,
                "INIT" => PacketType.Initialization,
                "IENT" => PacketType.InitializedEntities,
                "RHND" => PacketType.RightHandIndexes,
                "LHND" => PacketType.LeftHandIndexes,
                "HSET" => PacketType.HeadsetIndexes,
                "TAGS" => PacketType.TextIndexes,
                "NPCS" => PacketType.ColliderIndexes,
                "PRFX" => PacketType.Prefix,
                "ALIV" => PacketType.Alive,
                "DMGE" => PacketType.ColliderDamage,
                "PROP" => PacketType.PhysicsObjectIndexStartPos,
                "PHYS" => PacketType.PhysicsObjectPosAng,
                "MAPN" => PacketType.MapName,
                "RESC" => PacketType.ResourceSnapshot,
                "BUTN" => PacketType.ButtonIndexStartPos,
                "BPRS" => PacketType.ButtonPressIndex,
                "DOOR" => PacketType.DoorIndexStartPos,
                "BRAK" => PacketType.BrokenProp,
                "TRIG" => PacketType.TriggerIndexStartPos,
                "TRGD" => PacketType.TriggerActivateIndex,
                "EREM" => PacketType.EntityRemoved,
                "FIRE" => PacketType.EntityFired,
                "HURT" => PacketType.PlayerDamage,
                "PARN" => PacketType.ParentChanged,
                "NPHP" => PacketType.NPCHealth,
                "CMND" => PacketType.KCOMCommand,
                "TELE" => PacketType.Teleport,
                "TENT" => PacketType.TemplateEntity,
                "SPWN" => PacketType.Spawn,
                _ => PacketType.None,
            };
        }
        public override string ToString()
        {
            return type switch
            {
                PacketType.PlayerPosAng => "PLYR",
                PacketType.HeadPosAng => "HEAD",
                PacketType.HandPosAng => "HAND",
                PacketType.Initialization => "INIT",
                PacketType.InitializedEntities => "IENT",
                PacketType.RightHandIndexes => "RHND",
                PacketType.LeftHandIndexes => "LHND",
                PacketType.HeadsetIndexes => "HSET",
                PacketType.TextIndexes => "TAGS",
                PacketType.ColliderIndexes => "NPCS",
                PacketType.Prefix => "PRFX",
                PacketType.Alive => "ALIV",
                PacketType.ColliderDamage => "DMGE",
                PacketType.PhysicsObjectIndexStartPos => "PROP",
                PacketType.PhysicsObjectPosAng => "PHYS",
                PacketType.MapName => "MAPN",
                PacketType.ResourceSnapshot => "RESC",
                PacketType.ButtonIndexStartPos => "BUTN",
                PacketType.ButtonPressIndex => "BUTN",
                PacketType.DoorIndexStartPos => "DOOR",
                PacketType.BrokenProp => "BRAK",
                PacketType.TriggerIndexStartPos => "TRIG",
                PacketType.TriggerActivateIndex => "TRGD",
                PacketType.EntityRemoved => "EREM",
                PacketType.EntityFired => "FIRE",
                PacketType.PlayerDamage => "HURT",
                PacketType.ParentChanged => "PARN",
                PacketType.NPCHealth => "NPHP",
                PacketType.KCOMCommand => "CMND",
                PacketType.Teleport => "TELE",
                PacketType.TemplateEntity => "TENT",
                PacketType.Spawn => "SPWN",
                _ => "NONE",
            };
        }
    }
}
