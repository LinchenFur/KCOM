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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KiwisCoOpModCore
{
    public static class Map
    {
        public static string map = "";
        public const string InvalidNameMessage = "地图名无效：请输入地图内部名称（如 mp_kiwitest），不要带扩展名、反斜杠、空格或控制台命令。";

        public static bool TryNormalize(string? input, out string name)
        {
            name = input?.Trim() ?? "";
            if (name.Length == 0 || name.Length > 240) return false;
            foreach (string segment in name.Split('/'))
            {
                if (segment.Length == 0) return false;
                foreach (char c in segment)
                    if (!(c >= 'a' && c <= 'z') && !(c >= 'A' && c <= 'Z') &&
                        !(c >= '0' && c <= '9') && c != '_' && c != '-') return false;
            }
            return true;
        }

        public static string LoadCommand(string name)
        {
            if (!TryNormalize(name, out string normalized))
                throw new ArgumentException(InvalidNameMessage, nameof(name));
            return "addon_enable 2739356543;addon_enable kiwimp_alyx;addon_play " + normalized + ";addon_tools_map " + normalized;
        }
    }
}
