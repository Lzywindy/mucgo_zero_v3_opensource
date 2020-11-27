using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace TestGo
{
    using static Math;
    using static MD;
    using static LARGE_MD;
    using static Stone;
    using static Eye_condition;
    using static KOMI_MODE;
    using static LIBERTY_STATE;
    using static SEARCH_MODE;
    using static HashInfo;
    using static ConstValues;
    using static Board;
    using static Pattern;
    using static PatternHash;
    using static Seki;
    using static Semeai;
    using static Utils;
    using System.IO;
    using System.Threading;

    enum TimeType
    {
        second,
        minute,
        hour
    };
    /// <summary>
    /// 综合类
    /// </summary>
    static class Utils
    {
        private static bool debug_message = true;
        private const string pass = "PASS";
        private const string resign = "resign";
        private static readonly char[] gogui_x = {
                'I', 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'J',
                'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T',
                'U', 'V', 'W', 'X', 'Y', 'Z'
        };
        public const string appName = "MUC-GO";
        public static void PrintBoard(game_info_t game)
        {
            char[] stone = { '+', 'B', 'W', '#' };
            int i, x, y, pos;
            if (!debug_message) return;
            Console.Error.WriteLine("Prisoner(B) : " + game.prisoner[(int)S_BLACK]);
            Console.Error.WriteLine("Prisoner(W) : " + game.prisoner[(int)S_WHITE]);
            Console.Error.WriteLine("Moves : " + game.moves);
            Console.Error.Write("  ");
            for (i = 1, y = board_start; y <= board_end; y++, i++)
                Console.Error.Write(" " + gogui_x[i]);
            Console.Error.WriteLine();
            Console.Error.Write(" +");
            for (i = 0; i < pure_board_size * 2 + 1; i++)
                Console.Error.Write("-");
            Console.Error.WriteLine("+");
            for (i = 1, y = board_start; y <= board_end; y++, i++)
            {
                Console.Error.Write(":|");
                for (x = board_start; x <= board_end; x++)
                {
                    pos = POS(x, y);
                    Console.Error.Write(" " + stone[(int)game.board[pos]]);
                }
                Console.Error.Write(" |");
                Console.Error.WriteLine();
            }
            Console.Error.Write(" +");
            for (i = 1; i <= pure_board_size * 2 + 1; i++)
            {
                Console.Error.Write("-");
            }
            Console.Error.WriteLine("+");
        }
        public static void PrintString(game_info_t game)
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
        public static void PrintStringID(game_info_t game)
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
        public static void PrintStringNext(game_info_t game)
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
                temp = (pure_board_size + 1 - i) + ":|";
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
                return ("" + GOGUI_X(pos) + GOGUI_Y(pos));
            }
        }
        public static int GOGUI_X(int pos) { return (gogui_x[CORRECT_X(pos)]); }
        public static int GOGUI_Y(int pos) { return (pure_board_size + 1 - CORRECT_Y(pos)); }
        public static int StringToInteger(string cpos)
        {
            char alphabet;
            int x, y, pos;
            if (string.Compare(cpos, "pass") == 0 || string.Compare(cpos, "PASS") == 0)
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
                y = pure_board_size - cpos[1] + 1;
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
                x = X(pos) - (OB_SIZE - 1);
                y = pure_board_size - (Y(pos) - OB_SIZE);
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
    }
    /// <summary>
    /// 游戏定时器
    /// </summary>
    class GameTimer
    {
        private DateTime m_begin;
        // 重置定时器并开始计时
        public void Reset()
        {
            m_begin = DateTime.Now;
        }
        //默认输出毫秒
        public int elapsed()
        {
            var m_Now = DateTime.Now;
            TimeSpan midTime = m_Now - m_begin;
            return midTime.Milliseconds;
        }
        //秒
        public int elapsed_seconds()
        {
            var m_Now = DateTime.Now;
            TimeSpan midTime = m_Now - m_begin;
            return midTime.Seconds;
        }
        //分
        public int elapsed_minutes()
        {
            var m_Now = DateTime.Now;
            TimeSpan midTime = m_Now - m_begin;
            return midTime.Minutes;
        }
        //时
        public int elapsed_hours()
        {
            var m_Now = DateTime.Now;
            TimeSpan midTime = m_Now - m_begin;
            return midTime.Hours;
        }
    };
    /// <summary>
    /// SGF棋谱记录
    /// </summary>
    public class SGF
    {
        private const string NM_GM = "GM";
        private const string NM_FF = "FF";
        private const string NM_CA = "CA";
        private const string NM_AP = "AP";
        private const string NM_KM = "KM";
        private const string NM_SZ = "SZ";
        private const string NM_DT = "DT";
        private const string NM_PB = "PB";
        private const string NM_PW = "PW";
        private const string NM_RE = "RE";
        private const string SGF_TN = ".sgf";
        private readonly char[] posh = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's' };
        private Dictionary<string, int> sgfpos2fpos;
        private Dictionary<int, string> fpos2sgfpos;
        private Dictionary<string, string> HeadInfo;
        private List<Tuple<string, int>> Moves;
        private List<game_info_t> gameroat;
        private int filecounter = 0;
        private string GetString()
        {
            string stringbuf = "";
            stringbuf += ("(" + Environment.NewLine);
            stringbuf += ";";
            foreach (var node in HeadInfo)
            {
                stringbuf += (node.Key + "[" + node.Value + "]");
            }
            stringbuf += Environment.NewLine;
            for (var i = 0; i < Moves.Count; i++)
            {
                string ppos_s = Moves[i].Item2 == 0 ? "" : fpos2sgfpos[Moves[i].Item2];
                stringbuf += ";" + Moves[i].Item1 + "[" + ppos_s + "]";
                if ((i + 1) % 5 == 0)
                    stringbuf += Environment.NewLine;
            }
            stringbuf += Environment.NewLine;
            stringbuf += ")" + Environment.NewLine;
            return stringbuf;
        }
        private string ToString(float f, int usablelen = 2)
        {
            string stringbuf = "";
            string format_float = "{0:0.";
            for (int i = 0; i < usablelen; i++)
                format_float += "0";
            format_float += "}";
            stringbuf += string.Format(format_float, f);
            return format_float;
        }
        private string ToString(int i)
        {
            string stringbuf = "";
            stringbuf += i;
            return stringbuf;
        }
        private string CurDate()
        {
            return DateTime.Now.ToShortDateString();
        }
        delegate void Func1();
        delegate bool Func2();
        delegate void Func3(string str);
        delegate bool Func4(string str);
        public SGF()
        {
            sgfpos2fpos = new Dictionary<string, int>();
            fpos2sgfpos = new Dictionary<int, string>();
            HeadInfo = new Dictionary<string, string>();
            Moves = new List<Tuple<string, int>>();
            gameroat = new List<game_info_t>();
            for (int y = BOARD_START; y <= BOARD_END; y++)
            {
                for (int x = BOARD_START; x <= BOARD_END; x++)
                {
                    string tmp = "";
                    tmp += posh[x - BOARD_START];
                    tmp += posh[y - BOARD_START];
                    var fpos = x + y * BOARD_SIZE;
                    sgfpos2fpos.Add(tmp, fpos);
                    fpos2sgfpos.Add(fpos, tmp);
                }
            }
            InitializeHash();
            InitializeConst();
        }
        /// <summary>
        /// 反悔多步
        /// </summary>
        /// <param name="step">步数</param>
        public void BackUp(ushort step = 0)
        {
            if (step == 0) return;
            if (gameroat.Count == 0) return;
            if (gameroat.Count < step)
                gameroat.Clear();
            else
                gameroat.RemoveRange(gameroat.Count - step + 1, step);
        }
        /// <summary>
        /// 清空SGF记录器
        /// </summary>
        public void Clear()
        {
            HeadInfo.Clear();
            Moves.Clear();
            gameroat.Clear();
        }
        /// <summary>
        /// 一步一步记录
        /// </summary>
        /// <param name="node">需要记录的游戏棋盘</param>
        public void Record(game_info_t node)
        {
            game_info_t Current = new game_info_t();
            CopyGame(ref Current, node);
            gameroat.Add(Current);
        }
        /// <summary>
        /// 读取SGF文件
        /// </summary>
        /// <param name="File">文件名及其全路径</param>
        /// <returns>文件是否载入成功</returns>
        public bool Load(string File)
        {
            Func3 GetHead = (string input) =>
            {
                var tempVector = input.Split(new char[] { '[', ']', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < tempVector.Length; i += 2)
                    HeadInfo.Add(tempVector[i], tempVector[i + 1]);
            };
            Func3 GetMove = (string input) =>
            {
                var tempVector = input.Split(new char[] { '[', ']', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tempVector.Length < 2)
                    Moves.Add(new Tuple<string, int>(tempVector[0], 0));
                else
                    Moves.Add(new Tuple<string, int>(tempVector[0], sgfpos2fpos[tempVector[1]]));
            };
            Func1 SetParameterss = () =>
            {
                bool update = false;
                if (HeadInfo.ContainsKey("KM"))
                {
                    float komi = 6.5f;
                    float.TryParse(HeadInfo["KM"], out komi);
                    if (KomiSetup != komi)
                    {
                        SetKomi(komi);
                        update = update || true;
                    }
                }
                if (HeadInfo.ContainsKey("SZ"))
                {
                    int _size = 19;
                    int.TryParse(HeadInfo["SZ"], out _size);
                    _size = Min(Max(_size, 9), 19);
                    if (_size == board_size)
                    {
                        SetBoardSize(_size);
                        update = update || true;
                    }
                }
                if (update) InitializeConst();
            };
            Func4 ReadSGF = (string filepath) =>
            {
                bool exist = CheckFile(filepath);
                if (!exist) return false;
                StreamReader sgffile = new StreamReader(filepath, Encoding.Default);
                int count = 0;
                string content;
                while ((content = sgffile.ReadLine()) != null)
                {
                    var temp = content.Split(new char[] { '(', ')', ' ', '\t', '\n', '\r', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (temp != null && temp.Length > 0)
                    {
                        if (count == 0)
                            GetHead(temp[0]);
                        else
                            for (int i = 0; i < temp.Length; i++)
                                GetMove(temp[i]);
                        count++;
                    }
                }
                sgffile.Close();
                return true;
            };
            Func2 GetGameRoat = () =>
            {
                SetParameterss();
                game_info_t game = new game_info_t();
                InitializeBoard(ref game);
                for (var i = 0; i < Moves.Count; i++)
                {
                    Stone color = (Moves[i].Item1 == "B") ? S_BLACK : (Moves[i].Item1 == "W") ? S_WHITE : S_EMPTY;
                    if (color == S_EMPTY) return false;
                    //string pos_str;
                    //IntegerToString(Moves[i].Item2, out pos_str);
                    //Console.WriteLine(i + ":" + pos_str);
                    PutStone(game, Moves[i].Item2, color);
                    Record(game);
                }
                //PrintBoard(game);
                return true;
            };
            if (!ReadSGF(File)) return false;
            return GetGameRoat();
        }
        /*保存SGF文件*/
        public void Save(string File, bool RemoveOld = false)
        {
            Func1 GenHead = () =>
            {
                HeadInfo.Clear();
                HeadInfo.Add(NM_GM, "1");
                HeadInfo.Add(NM_FF, "4");
                HeadInfo.Add(NM_CA, "UTF-8");
                HeadInfo.Add(NM_AP, appName);
                HeadInfo.Add(NM_KM, ToString(KomiSetup, 1));
                HeadInfo.Add(NM_SZ, ToString(pure_board_size));
                HeadInfo.Add(NM_DT, CurDate());
                HeadInfo.Add(NM_PB, "Default");
                HeadInfo.Add(NM_PW, "Default");
                string RE_V = "";
                var score = GetorComputeScore(gameroat[gameroat.Count - 1], true);
                if (score < 0)
                    RE_V = "B+Resign";
                else if (score > 0)
                    RE_V = "W+Resign";
                else
                    RE_V = "Draw";
                HeadInfo.Add(NM_RE, RE_V);
            };
            Func2 GetMoves = () =>
            {
                Moves.Clear();
                var info = gameroat[gameroat.Count - 1].record;
                var length = gameroat[gameroat.Count - 1].moves;
                for (var i = 1; i < length; i++)
                {
                    string color = info[i].color == S_BLACK ? "B" : info[i].color == S_WHITE ? "W" : "E";
                    if (color == "E") return false;
                    int pos = Max(info[i].pos, 0);
                    Moves.Add(new Tuple<string, int>(color, pos));
                }
                return true;
            };
            GenHead();
            GetMoves();
            var content = GetString();
            string FileName = File;
            string Num = "";
            if (!RemoveOld)
            {
                while (CheckFile(FileName))
                {
                    var tempName = FileName.Split(new string[] { SGF_TN }, StringSplitOptions.RemoveEmptyEntries);
                    string temp = tempName[0];
                    if (Num != "")
                        tempName = FileName.Split(new string[] { Num }, StringSplitOptions.RemoveEmptyEntries);
                    Num = ToString(filecounter++);
                    FileName = tempName[0] + Num + SGF_TN;
                }
            }
            StreamWriter sgffile = new StreamWriter(FileName, false, Encoding.Default);
            sgffile.Write(content);
            sgffile.Close();
        }
    }
}
