using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
namespace MUCGO_zero_CS
{
    using static Math;
    using static Stone;
    using static Board;
    using System.IO;
    using System.Threading;
    using System.Security.Cryptography;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;

    public enum TimeType
    {
        second,
        minute,
        hour
    };
    /// <summary>
    /// 综合类
    /// </summary>
    public static class Utils
    {
        public static bool InRange((byte x, byte y) pos, (byte limit_x, byte limit_y) size)
        {
            return (pos.x >= 0 && pos.x < size.limit_x && pos.y >= 0 && pos.y < size.limit_y);
        }
        /// <summary>
        /// 日志文件记录器
        /// </summary>
        public static void WriteLog(string data)
        {
            StreamWriter Logger = new StreamWriter(GetExpFilePath() + "Log.txt", true, Encoding.UTF8);
            Logger.WriteLine(data);
            Logger.Close();
        }
        public static string GetExpFilePath()
        {
            var ExpFilePath = "";
            var path = AppDomain.CurrentDomain.BaseDirectory;
            var model = path.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            if (model == null) return "";
            else if (model.Length == 1 || model.Length == 2)
            {
                for (int i = 0; i < model.Length; i++)
                {
                    ExpFilePath += model[i] + "\\";
                }
                ExpFilePath += "\\" + "ExpFiles" + "\\";
            }
            else
            {
                for (int i = 0; i < model.Length - 1; i++)
                {
                    ExpFilePath += model[i] + "\\";
                }
                ExpFilePath += "\\" + "ExpFiles" + "\\";
            }
            ExpFilePath = ExpFilePath.Replace("\\\\", "\\");
            return ExpFilePath;
        }
        private static bool debug_message = true;
        private const string pass = "PASS";
        private const string resign = "resign";
        private static readonly char[] gogui_x = {
                'I', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J',
                'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
                'U', 'V', 'W', 'X', 'Y', 'Z'
        };
        public const string appName = "MUC-GO";
        public static void PrintBoard(Board game)
        {
            char[] stone = { '+', 'B', 'W', '#' };
            int i, x, y, pos;
            if (!debug_message) return;
            string str = "";
            str += "Prisoner(B) : " + ((Board)game).prisoner[(int)S_BLACK - 1] + Environment.NewLine;
            str += "Prisoner(W) : " + ((Board)game).prisoner[(int)S_WHITE - 1] + Environment.NewLine;
            str += "Moves : " + ((Board)game).MovesFinished + Environment.NewLine;
            str += "  ";
            for (i = 1, y = board_start; y <= board_end; y++, i++)
                str += " " + gogui_x[i];
            str += Environment.NewLine;
            str += " +";
            for (i = 0; i < pure_board_size * 2 + 1; i++)
                str += "-";
            str += "+";
            str += Environment.NewLine;
            for (i = 1, y = board_start; y <= board_end; y++, i++)
            {
                str += ":|";
                for (x = board_start; x <= board_end; x++)
                {
                    pos = POS(x, y);
                    str += " " + stone[(int)((Board)game).board[pos]];
                }
                str += " |";
                str += Environment.NewLine;
            }
            str += " +";
            for (i = 1; i <= pure_board_size * 2 + 1; i++)
            {
                str += "-";
            }
            str += "+" + Environment.NewLine;
            Console.Error.WriteLine(str);
        }
        public static void PrintString(Board game)
        {
            var @string = game.@string;
            int i, pos, neighbor;
            if (!debug_message) return;
            Console.Error.WriteLine(" :: :: String :: ::");
            for (i = 0; i < MAX_STRING; i++)
            {
                if (@string[i].flag)
                {
                    if (game.board[@string[i].origin] == S_BLACK)
                        Console.Error.Write("Black String ");
                    else
                        Console.Error.Write("White String ");
                    Console.Error.WriteLine("ID : " + i + " (libs : " + @string[i].libs + ", size : " + @string[i].size + ")");
                    pos = @string[i].lib[0];
                    Console.Error.WriteLine(" Liberty : ");
                    Console.Error.Write(" ");
                    while (pos != STRING_END)
                    {
                        Console.Error.Write(GOGUI_X(pos) + GOGUI_Y(pos) + " ");
                        pos = @string[i].lib[pos];
                    }
                    Console.Error.WriteLine();
                    pos = @string[i].origin;
                    Console.Error.WriteLine(" Stone : ");
                    Console.Error.Write(" ");
                    while (pos != STRING_END)
                    {
                        Console.Error.Write(GOGUI_X(pos) + GOGUI_Y(pos) + " ");
                        pos = game.string_next[pos];
                        if (pos == game.string_next[pos]) Console.ReadKey();
                    }
                    Console.Error.WriteLine();
                    neighbor = @string[i].neighbor[0];
                    if (neighbor == 0) Console.ReadKey();
                    Console.Error.WriteLine(" Neighbor : ");
                    Console.Error.Write(" ");
                    while (neighbor < NEIGHBOR_END)
                    {
                        Console.Error.Write(neighbor + " ");
                        neighbor = @string[i].neighbor[neighbor];
                    }
                    Console.Error.WriteLine();
                }
            }
            Console.Error.WriteLine();
        }
        public static void PrintStringID(Board game)
        {
            int i, x, y, pos;
            if (!debug_message) return;
            Console.Error.Write(" ");
            for (i = 1, y = board_start; y <= board_end; y++, i++)
                Console.Error.Write(" " + gogui_x[i]);
            Console.Error.WriteLine();
            for (i = 1, y = board_start; y <= board_end; y++, i++)
            {
                string temp = "" + (pure_board_size + 1 - i) + ":";
                temp.PadLeft(3);
                Console.Error.Write(temp);
                for (x = board_start; x <= board_end; x++)
                {
                    pos = x + y * board_size;
                    if (game.@string[game.string_id[pos]].flag)
                    {
                        temp = "" + game.string_id[pos];
                        temp.PadLeft(3);
                        Console.Error.Write(" " + temp);
                    }
                    else
                    {
                        Console.Error.Write(" -");
                    }
                }
                Console.Error.WriteLine();
            }
            Console.Error.WriteLine();
        }
        public static void PrintStringNext(Board game)
        {
            int i, x, y, pos;
            if (!debug_message) return;
            Console.Error.Write(" ");
            for (i = 1, y = board_start; y <= board_end; y++, i++)
            {
                Console.Error.Write(" " + gogui_x[i]);
            }
            Console.Error.WriteLine();
            for (i = 1, y = board_start; y <= board_end; y++, i++)
            {
                string temp = "" + (pure_board_size + 1 - i) + ":";
                temp.PadLeft(3);
                Console.Error.Write(temp);
                for (x = board_start; x <= board_end; x++)
                {
                    pos = x + y * board_size;
                    if (game.@string[game.string_id[pos]].flag)
                    {
                        if (game.string_next[pos] != STRING_END)
                        {
                            temp = "" + game.string_id[pos];
                            temp.PadLeft(3);
                            Console.Error.Write(temp);
                        }
                        else
                        {
                            Console.Error.Write(" END");
                        }
                    }
                    else
                    {
                        Console.Error.Write(" -");
                    }
                }
                Console.Error.WriteLine();
            }
            Console.Error.WriteLine();
        }
        public static void PrintOwnerNN(Stone color, ref float[] own)
        {
            int player = 0, opponent = 0;
            float score;
            if (!debug_message) return;
            Console.Error.Write(" ");
            for (int i = 1, y = board_start; y <= board_end; y++, i++)
                Console.Error.Write(" " + gogui_x[i]);
            Console.Error.WriteLine();
            Console.Error.Write(" +");
            for (int i = 0; i < pure_board_size * 4; i++)
                Console.Error.Write("-");
            Console.Error.WriteLine("+");
            string temp = "";
            for (int i = 1, y = board_start; y <= board_end; y++, i++)
            {
                temp = pure_board_size + 1 - i + ":|";
                temp.PadLeft(2);
                Console.Error.Write(temp);
                for (int x = board_start; x <= board_end; x++)
                {
                    int pos = POS(x, y);
                    float owner = own[pos];
                    if (owner > 0.5)
                    {
                        player++;
                    }
                    else
                    {
                        opponent++;
                    }
                    temp = (int)(owner * 100) + " ";
                    temp.PadLeft(3);
                    Console.Error.Write(temp);
                }
                Console.Error.WriteLine("|");
            }
            Console.Error.Write(" +");
            for (int i = 0; i < pure_board_size * 4; i++)
                Console.Error.Write("-");
            Console.Error.WriteLine("+");
            if (color == S_BLACK)
            {
                if (player - opponent > komi[0])
                {
                    score = player - opponent - komi[0];
                    Console.Error.WriteLine("WHITE+" + score);
                }
                else
                {
                    score = -(player - opponent - komi[0]);
                    Console.Error.WriteLine("BLACK+" + score);
                }
            }
            else
            {
                if (player - opponent > -komi[0])
                {
                    score = player - opponent + komi[0];
                    Console.Error.WriteLine("WHITE+" + score);
                }
                else
                {
                    score = -(player - opponent + komi[0]);
                    Console.Error.WriteLine("BLACK+" + score);
                }
            }
        }
        public static void PrintPoint(int pos)
        {
            if (!debug_message) return;
            if (pos == PASS)
            {
                Console.Error.WriteLine("PASS");
            }
            else if (pos == RESIGN)
            {
                Console.Error.WriteLine("RESIGN");
            }
            else
            {
                Console.Error.WriteLine("" + GOGUI_X(pos) + GOGUI_Y(pos));
            }
        }
        public static string FormatMove(int pos)
        {
            if (pos == PASS)
            {
                return "PASS";
            }
            else if (pos == RESIGN)
            {
                return "RESIGN";
            }
            else
            {
                return "" + GOGUI_X(pos) + GOGUI_Y(pos);
            }
        }
        public static int GOGUI_X(int pos) { return gogui_x[CORRECT_X((short)pos)]; }
        public static int GOGUI_Y(int pos) { return pure_board_size + 1 - CORRECT_Y((short)pos); }
        public static int StringToInteger(string cpos)
        {
            char alphabet;
            int x, y, pos;
            cpos = cpos.ToLower();
            if (string.Compare(cpos, "pass") == 0)
            {
                pos = PASS;
            }
            else
            {
                alphabet = char.ToUpper(cpos[0]);
                x = 0;
                for (int i = 1; i <= pure_board_size; i++)
                {
                    if (gogui_x[i] == alphabet)
                    {
                        x = i;
                    }
                }
                var tmpy = 0;
                int.TryParse(cpos.Substring(1), out tmpy);
                y = pure_board_size - tmpy + 1;
                pos = POS(x + (OB_SIZE - 1), y + (OB_SIZE - 1));
            }
            return pos;
        }
        public static void IntegerToString(int pos, out string _cpos)
        {
            int x, y;
            if (pos == PASS)
                _cpos = pass;
            else if (pos == RESIGN)
                _cpos = resign;
            else
            {
                _cpos = "";
                x = X((short)pos) - (OB_SIZE - 1);
                y = pure_board_size - (Y((short)pos) - OB_SIZE);
                _cpos += gogui_x[x];
                if (y / 10 == 0)
                {
                    _cpos += (char)('0' + y % 10);
                }
                else
                {
                    _cpos += (char)('0' + y / 10);
                    _cpos += (char)('0' + y % 10);
                }
            }
        }
        public static string GetProgramPath()
        {
            return Environment.CurrentDirectory;
        }
        public static bool CheckFile(string FilePath)
        {
            return File.Exists(FilePath);
        }
        public static void ReLU(float[] datas)
        {
            if (datas == null) return;
            for (int i = 0; i < datas.Length; i++)
                Volatile.Write(ref datas[i], Max(datas[i], 0));
        }
        public static void MutexAddf(ref float data, float addon)
        {
            Volatile.Write(ref data, data + addon);
        }
        public static void MutexAddi(ref int data, int addon)
        {
            Volatile.Write(ref data, data + addon);
        }
        public static void MutexMinusf(ref float data, float addon)
        {
            Volatile.Write(ref data, data - addon);
        }
        public static void MutexMinusi(ref int data, int addon)
        {
            Volatile.Write(ref data, data - addon);
        }
        /// <summary>
        /// 得到序列化后的字符数组
        /// </summary>
        /// <typeparam name="T">类型：必须为可序列化的</typeparam>
        /// <param name="data">需要序列化的数据</param>
        /// <returns>序列化的字节数组</returns>
        public static byte[] GetBytesSerializabled<T>(T data)
        {
            MemoryStream stream = new MemoryStream();
            IFormatter formatter = new BinaryFormatter();
            formatter.Serialize(stream, data);
            byte[] newArray = new byte[stream.Length];
            stream.Seek(0, SeekOrigin.Begin);
            stream.Read(newArray, 0, (int)stream.Length);
            stream.Close();
            return newArray;
        }
        //创建SHA1对象
        //static readonly MemoryStream stream = new MemoryStream();
        //static readonly IFormatter formatter = new BinaryFormatter();
        //static readonly MD5 md5 = new MD5CryptoServiceProvider();
        /// <summary>
        /// 计算内容的哈希值
        /// </summary>
        /// <param name="byte_datas">字节数组</param>
        /// <returns>字节数组的Hash内容</returns>
        public static byte[] GetContexHash(IList<byte> byte_datas)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            //没有任何东西
            if (byte_datas == null || byte_datas.Count == 0) return null;
            //Hash运算
            return md5.ComputeHash(byte_datas.ToArray());
        }
        /// <summary>
        /// 得到64位hash值
        /// </summary>
        /// <param name="byte_datas"></param>
        /// <returns></returns>
        public static ulong GetContexHashUint64(IList<byte> byte_datas)
        {
            var array_data = GetContexHash(byte_datas);
            ulong data = 0;
            for (int index = 0; index < array_data.Length / sizeof(ulong); index++)
            {
                data ^= BitConverter.ToUInt64(byte_datas.ToArray(), index * sizeof(ulong));
            }
            return data;
        }
        /// <summary>
        /// 字节数组对比
        /// </summary>
        /// <param name="byteA1">字节数组1</param>
        /// <param name="byteA2">字节数组2</param>
        /// <returns>是否相等</returns>
        public static bool CompareByteArray(byte[] byteA1, byte[] byteA2)
        {
            if (byteA1 == null || byteA2 == null) return false;
            if (byteA1.Length != byteA2.Length) return false;
            return string.Compare(Convert.ToBase64String(byteA1), Convert.ToBase64String(byteA2), false) == 0 ? true : false;
        }
    }
}
