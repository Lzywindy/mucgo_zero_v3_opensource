using System;
using System.Collections.Generic;
using System.Text;
namespace MUCGO_zero_CS
{
    using static Math;
    using static Stone;
    using static Board;
    using static Utils;
    using System.IO;
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
        private readonly Dictionary<string, int> sgfpos2fpos;
        private readonly Dictionary<int, string> fpos2sgfpos;
        private readonly Dictionary<string, string> HeadInfo;
        private readonly List<(string color, short pos)> Moves;
        private int filecounter = 0;
        public Board currentboard { get; private set; }
        private string GetString()
        {
            string stringbuf = "";
            stringbuf += "(" + Environment.NewLine;
            stringbuf += ";";
            foreach (var node in HeadInfo)
            {
                stringbuf += node.Key + "[" + node.Value + "]";
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
            string format_float = "{0:N" + usablelen + "}";
            stringbuf = string.Format(format_float, f);
            return stringbuf;
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
        public SGF()
        {
            sgfpos2fpos = new Dictionary<string, int>();
            fpos2sgfpos = new Dictionary<int, string>();
            HeadInfo = new Dictionary<string, string>();
            Moves = new List<(string color, short pos)>();
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
            InitializeConst();
        }

        /// <summary>
        /// 清空SGF记录器
        /// </summary>
        public void Clear()
        {
            HeadInfo.Clear();
            Moves.Clear();
        }
        /// <summary>
        /// 一步一步记录
        /// </summary>
        /// <param name="node">需要记录的游戏棋盘</param>
        public void Record(Board board)
        {
            currentboard = board as Board;
        }
        /// <summary>
        /// 读取SGF文件
        /// </summary>
        /// <param name="File">文件名及其全路径</param>
        /// <returns>文件是否载入成功</returns>
        public bool Load(string File)
        {
            Action<string> GetHead = (string input) =>
            {
                var tempVector = input.Split(new char[] { '[', ']', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                for (var i = 0; i < tempVector.Length; i += 2)
                    HeadInfo.Add(tempVector[i], tempVector[i + 1]);
            };
            Action<string> GetMove = (string input) =>
            {
                var tempVector = input.Split(new char[] { '[', ']', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tempVector.Length < 2)
                    Moves.Add((tempVector[0], 0));
                else
                    Moves.Add((tempVector[0], (short)sgfpos2fpos[tempVector[1]]));
            };
            Action SetParameterss = () =>
            {
                bool update = false;
                if (HeadInfo.ContainsKey("KM"))
                {
                    float komi = 6.5f;
                    float.TryParse(HeadInfo["KM"], out komi);
                    if (Board.komi[0] != komi)
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
                        //SetBoardSize(_size);
                        update = update || true;
                    }
                }
                if (update) InitializeConst();
            };
            Func<string, bool> ReadSGF = (string filepath) =>
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
            Func<bool> GetGameRoat = () =>
            {
                SetParameterss();
                Board game = EmptyBoard.Clone() as Board;
                for (short i = 0; i < Moves.Count; i++)
                {
                    Stone color = (Moves[i].Item1 == "B") ? S_BLACK : (Moves[i].Item1 == "W") ? S_WHITE : S_EMPTY;
                    if (color == S_EMPTY) return false;
                    //string pos_str;
                    //IntegerToString(Moves[i].Item2, out pos_str);
                    //Console.WriteLine(i + ":" + pos_str);
                    PutStone(game, Moves[i].Item2, color);
                   
                }
                Record(game);
                //PrintBoard(game);
                return true;
            };
            if (!ReadSGF(File)) return false;
            return GetGameRoat();
        }
        /*保存SGF文件*/
        public void Save(string File, bool RemoveOld = false)
        {
            if (currentboard == null) return;
            try
            {
                void GenHead()
                {
                    HeadInfo.Clear();
                    HeadInfo.Add(NM_GM, "1");
                    HeadInfo.Add(NM_FF, "4");
                    HeadInfo.Add(NM_CA, "UTF-8");
                    HeadInfo.Add(NM_AP, appName);
                    HeadInfo.Add(NM_KM, ToString(komi[0], 1));
                    HeadInfo.Add(NM_SZ, ToString(pure_board_size));
                    HeadInfo.Add(NM_DT, CurDate());
                    HeadInfo.Add(NM_PB, "Default");
                    HeadInfo.Add(NM_PW, "Default");
                    string RE_V = "";
                    var score = currentboard.Score_Final;
                    if (score < 0)
                        RE_V = "B+Resign";
                    else if (score > 0)
                        RE_V = "W+Resign";
                    else
                        RE_V = "Draw";
                    HeadInfo.Add(NM_RE, RE_V);
                }
                bool GetMoves()
                {
                    Moves.Clear();
                    var info = currentboard.record;
                    for (var i = 0; i < info.Count; i++)
                    {
                        string color = info[i].color == S_BLACK ? "B" : "W";
                        short pos = Max(info[i].pos, (short)0);
                        Moves.Add((color, pos));
                    }
                    return true;
                }
                GenHead();
                GetMoves();
                var content = GetString();
                string FileName = File;
                var tempName = FileName.Split(new string[] { SGF_TN }, StringSplitOptions.RemoveEmptyEntries);
                if (tempName == null || tempName[0] == null || tempName[0] == "") return;
                string BasicName = tempName[0];
                string Num = "";
                if (!RemoveOld)
                {
                    while (CheckFile(FileName))
                    {
                        if (Num != "")
                            tempName = tempName[0].Split(new string[] { Num }, StringSplitOptions.RemoveEmptyEntries);
                        Num = ToString(filecounter++);
                        FileName = BasicName + Num + SGF_TN;
                    }
                }
                StreamWriter sgffile = new StreamWriter(FileName, false, Encoding.Default);
                sgffile.Write(content);
                sgffile.Close();
            }
            catch (Exception)
            {
            }
        }
    }
}
