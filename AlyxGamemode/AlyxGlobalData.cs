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
using KiwisCoOpModCore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlyxGamemode
{
    public sealed class AlyxGlobalData
    {
        public static readonly AlyxGlobalData instance = new();
        private readonly List<Player> players = new();
        private readonly List<Moveable> manipulatedEntities = new();
        public bool AddManipulatedEntity(Entity MovedObject, Guid ClientID, List<IndexedClient> Connections)
        {
            Moveable? oldMoveable = manipulatedEntities.Find(m => m.GetMovedObject().Name == MovedObject.Name);
            if (oldMoveable == null)
            {
                manipulatedEntities.Add(new Moveable(MovedObject, ClientID, Connections));
                return true;
            }
            else if(oldMoveable.GetClientID() == ClientID || oldMoveable.GetTimedOut())
            {
                oldMoveable.Update(MovedObject, ClientID, Connections);
                return true;
            }
            return false;
        }
        public List<Player> GetPlayers()
        {
            return players;
        }
        public Player? GetPlayer(Guid id)
        {
            Player? oldPlayer = players.Find(p => {
                if (p != null)
                    return p.Client.Session.ConnectionInfo.Id == id;
                else
                    return false;
            });
            if (oldPlayer != null)
                return oldPlayer;
            return null;
        }
        public Player? AddPlayer(IndexedClient client)
        {
            lock (players)
            {
                Player? existing = players.Find(player => player.Client.Session.ConnectionInfo.Id == client.Session.ConnectionInfo.Id);
                if (existing != null) return existing;
                for (int index = 0; index < 16; index++)
                {
                    if (players.All(player => player.Index != index))
                    {
                        Player player = new(index, client);
                        players.Add(player);
                        return player;
                    }
                }
                return null;
            }
        }
        public bool RemovePlayer(Guid id)
        {
            lock (players)
            {
                Player? oldPlayer = players.Find(p => p.Client.Session.ConnectionInfo.Id == id);
                return oldPlayer != null && players.Remove(oldPlayer);
            }
        }
        public bool RemovePlayer(int index)
        {
            lock (players)
            {
                Player? oldPlayer = players.Find(p => p.Index == index);
                return oldPlayer != null && players.Remove(oldPlayer);
            }
        }
    }
}
