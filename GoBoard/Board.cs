using MUCGO_zero_CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MUCGO_zero_CS
{
    using static Math;
    using static MD;
    using static LARGE_MD;
    using static Stone;
    using static Eye_condition;
    using static LIBERTY_STATE;
    using static HashInfo;
    using static ConstValues;
    using static PatternHash;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    using System.Threading.Tasks;
    using System.Linq;
    using System.Threading;
    using System.Runtime.InteropServices;


    [Serializable]
    public partial class Board : IBoard
    {
        public readonly int[,] capture_pos = new int[2, PURE_BOARD_MAX];
        public readonly List<Move_t> record = new List<Move_t>(MAX_RECORDS);
        public readonly String_t[] @string = new String_t[MAX_STRING];
        public readonly Stone[] board = new Stone[BOARD_MAX];
        public readonly bool[] candidates = new bool[PURE_BOARD_MAX];
        public readonly Pattern_t[] pat = new Pattern_t[BOARD_MAX];
        public readonly HashSet<ulong> preHashCodes = new HashSet<ulong>();
        public readonly short[] prisoner = new short[((int)S_MAX >> 1)];
        public readonly bool[] seki = new bool[PURE_BOARD_MAX];
        public readonly short[] string_id = new short[STRING_POS_MAX];
        public readonly short[] string_next = new short[STRING_POS_MAX];
        public ulong current_hash;
        public int ko_move;
        public int ko_pos;
        public int pass_count;
        public ulong positional_hash;
        public ulong previous1_hash;
        public ulong previous2_hash;
        public Board()
        {
            for (int i = 0; i < pat.Length; i++)
                pat[i] = new Pattern_t();
            for (int i = 0; i < @string.Length; i++)
                @string[i] = new String_t();
        }
        public sbyte[] Area { get; private set; }
        /// <summary>
        /// 当前将要落子的玩家
        /// </summary>
        public Stone Board_CurrentPlayer { get { return Moves % 2 == 1 ? S_BLACK : S_WHITE; } }
        /// <summary>
        /// 棋盘大小
        /// </summary>
        public int BoardSize { get { return pure_board_max; } }
        public int[] capture_num
        {
            get
            {
                int[] c = new int[2];
                for (int index = 0; index < PURE_BOARD_MAX; index++)
                {
                    c[0] += capture_pos[0, index];
                    c[1] += capture_pos[1, index];
                }
                return c;
            }
        }
        /// <summary>
        /// 当前轮到的玩家
        /// </summary>
        public sbyte CurrentPlayer { get { return (sbyte)(Moves % 2 == 1 ? 1 : -1); } }
        /// <summary>
        /// 当前允许的走步
        /// </summary>
        public byte[] EnabledPos
        {
            get
            {
                byte[] EnabledPos = new byte[PURE_BOARD_MAX];
                bool enabledeverywhere = false;
                for (int index = 0; index < pure_board_max; index++)
                    if (IsLegal(this, onboard_pos_2full[index], Board_CurrentPlayer) &&
                        IsLegalNotEye(this, onboard_pos_2full[index], Board_CurrentPlayer) &&
                        (capture_pos[((int)FLIP_COLOR(Board_CurrentPlayer) - 1), index] == 0))
                        EnabledPos[index] = 1;
                enabledeverywhere = EnabledPos.Any((byte enabled) => enabled > 0);
                if (!enabledeverywhere)
                {
                    for (int index = 0; index < pure_board_max; index++)
                        if (IsLegal(this, onboard_pos_2full[index], Board_CurrentPlayer) &&
                            IsLegalNotEye(this, onboard_pos_2full[index], Board_CurrentPlayer))
                            EnabledPos[index] = 1;
                }
                return EnabledPos;
            }
        }
        public byte[] EnabledPos4Analysis
        {
            get
            {
                byte[] Enabled_IsLegal = new byte[PURE_BOARD_MAX];
                byte[] Enabled_IsLegalNotEye = new byte[PURE_BOARD_MAX];
                byte[] Enabled_EndGameEval = new byte[PURE_BOARD_MAX];
                byte[] EnabledPos = new byte[PURE_BOARD_MAX];
                for (int index = 0; index < pure_board_max; index++)
                {
                    var pos = onboard_pos_2full[index];
                    if (IsLegal(this, pos, Board_CurrentPlayer))
                        Enabled_IsLegal[index] = 1;
                    else
                        Enabled_IsLegal[index] = 0;
                    if (IsLegalNotEye(this, pos, Board_CurrentPlayer))
                        Enabled_IsLegalNotEye[index] = 1;
                    else
                        Enabled_IsLegalNotEye[index] = 0;
                    if (board[pos] != S_EMPTY)
                    {
                        GetNeighbor4(out short[] neigbours, pos);
                        int black = 0;
                        int white = 0;
                        int empty = 0;
                        int opp_onelib_str = 0;
                        int my_onelib_str = 0;
                        foreach (var pos_color in neigbours)
                        {
                            if (board[pos_color] == S_EMPTY)
                                empty++;
                            else if (board[pos_color] == S_BLACK)
                                black++;
                            else if (board[pos_color] == S_WHITE)
                                white++;
                            var my_string = @string[string_id[pos_color]];
                            if (my_string.color == FLIP_COLOR(Board_CurrentPlayer) && my_string.libs == 1 && my_string.flag) opp_onelib_str++;
                            if (my_string.color == Board_CurrentPlayer && my_string.libs == 1 && my_string.flag) my_onelib_str++;
                        }
                        if ((white != 0 && black != 0) && Enabled_IsLegal[index] == 1)
                        {
                            if (black > white && CurrentPlayer == 1)
                                Enabled_EndGameEval[index] = 1;
                            else if (black < white && CurrentPlayer == -1)
                                Enabled_EndGameEval[index] = 1;
                            else if (opp_onelib_str > 0)
                                Enabled_EndGameEval[index] = 1;
                            else if (!IsCurrentStaticEmptyPos(pos) && my_onelib_str < 1)
                                Enabled_EndGameEval[index] = 1;
                            else
                                Enabled_EndGameEval[index] = 0;
                        }
                        else if (empty == neigbours.Length)
                            Enabled_EndGameEval[index] = 1;
                        else
                            Enabled_EndGameEval[index] = 0;
                    }
                    else
                    {
                        Enabled_EndGameEval[index] = 0;
                    }
                }
                for (int index = 0; index < pure_board_max; index++)
                {
                    EnabledPos[index] = (byte)(Enabled_IsLegal[index] & (Enabled_IsLegalNotEye[index] | Enabled_EndGameEval[index]));
                }
                return EnabledPos;
            }
        }
        /// <summary>
        /// 分级助手
        /// </summary>
        public sbyte[] Features
        {
            get
            {
                var features = new sbyte[PURE_BOARD_MAX * 3];
                var Enabledpos = EnabledPos;
                for (short pos = 0; pos < PURE_BOARD_MAX; pos++)
                {
                    features[pos + DIM_BOARD] = Convert2Num(board[onboard_pos_2full[pos]]);
                    features[pos + DIM_CURRENTPLAYER] = CurrentPlayer;
                    features[pos + DIM_CANDIDATE] = (sbyte)Enabledpos[pos];
                }
                return features;
            }
        }
        /// <summary>
        /// 表示游戏可以结束的标志
        /// </summary>
        public bool GameOver => (record.Count((Move_t move) => move.pos == PASS) >= 2);
        /// <summary>
        /// 得到当前盘面的Hash值
        /// </summary>
        public ulong HashCode => current_hash;
        //{
        //    get
        //    {
        //        //List<byte> bytes = new List<byte>((PURE_BOARD_MAX + 32) * 2);
        //        //bytes.AddRange(Array.ConvertAll(CurrentBoard, (sbyte item) => { return (byte)(item + 2); }));
        //        //bytes.AddRange(EnabledPos);
        //        //return Utils.GetContexHashUint64(bytes);
        //        return current_hash;
        //    }
        //}
        /// <summary>
        /// 得到最近下的棋子坐标
        /// </summary>
        public int LastestPos => record.Count < 1 ? -1 : record[record.Count - 1].pos;
        /// <summary>
        /// 最近的记录
        /// </summary>
        public Move_t LatestRecord { get { return record.Count > 0 ? record[record.Count - 1] : null; } }
        /// <summary>
        /// 当前将要走的步数
        /// </summary>
        public int Moves { get { return record.Count + 1; } }
        /// <summary>
        /// 当前已经走的步数
        /// </summary>
        public int MovesFinished { get { return record.Count; } }
        public float Score { get; private set; }
        /// <summary>
        /// 最后的分数
        /// </summary>
        public float Score_Final
        {
            get
            {
                CheckBentFourInTheCorner(this);
                CheckSeki(this);
                return GetScore(this);
            }
        }
        /// <summary>
        /// UCT搜索树使用的最后分数
        /// </summary>
        public float Score_UCT
        {
            get
            {
                return CalculateScore(this) + prisoner[0] - prisoner[1] - komi[(byte)Board_CurrentPlayer];
            }
        }
        /// <summary>
        /// 返回占地情况
        /// </summary>
        public sbyte[] Terrains
        {
            get
            {
                if (Moves > 100 && Area != null)
                {
                    return Area;
                }
                else
                {
                    var Terrains = new sbyte[pure_board_max];
                    for (int i = 0; i < pure_board_max; i++)
                    {
                        var pos = onboard_pos_2full[i];
                        Terrains[i] = Convert2Num(board[pos]);
                        if (Terrains[i] == 0) Terrains[i] = Convert2Num(territory[Pat3(pat, pos)]);
                        if (Terrains[i] == 0) Terrains[i] = (sbyte)Sign(capture_pos[0, i] - capture_pos[1, i]);
                    }
                    return Terrains;
                }
            }
        }
        /// <summary>
        /// 得到胜利者
        /// </summary>
        public sbyte Winner { get { var ScoreFinal = Score_Final; return (sbyte)(ScoreFinal > 0 ? 1 : ScoreFinal < 0 ? -1 : 0); } }
        /// <summary>
        /// 浮点类型的胜利值，表示胜利的程度
        /// </summary>
        public float WinnerF
        {
            get
            {
                var ScoreFinal = Score_Final;
                return ScoreFinal > 0 ? ScoreFinal / (pure_board_max - komi[0]) : ScoreFinal < 0 ? (ScoreFinal / (pure_board_max + komi[0])) : 0;
            }
        }
    }
    public partial class Board
    {

        #region 公共变量
        public static short board_end => BOARD_END;
        public static short board_max => BOARD_MAX;
        public static short board_size => BOARD_SIZE;
        public static short board_start => BOARD_START;
        public static short pure_board_max => PURE_BOARD_MAX;
        public static short pure_board_size => PURE_BOARD_SIZE;
        public static readonly float[] komi = new float[(int)S_OB];
        public static readonly short[] onboard_pos_2full = new short[PURE_BOARD_MAX];
        public static readonly short[] onboard_pos_2pure = new short[BOARD_MAX];
        protected static char[] stone = { '+', '@', 'O', '#' };
        protected static readonly int[] board_pos_id = new int[BOARD_MAX];
        protected static readonly int[] board_x = new int[BOARD_MAX];
        protected static readonly int[] board_y = new int[BOARD_MAX];
        protected static readonly short[] border_dis_x = new short[BOARD_MAX];
        protected static readonly short[] border_dis_y = new short[BOARD_MAX];
        protected static readonly int[] corner = new int[4];
        protected static readonly int[,] corner_neighbor = new int[4, 2];
        protected static readonly int[] cross = new int[4];
        protected static readonly float[] dynamic_komi = new float[(int)S_OB];
        protected static readonly Stone[] eye = new Stone[PAT3_MAX];
        protected static readonly Eye_condition[] eye_condition = new Eye_condition[PAT3_MAX];
        protected static readonly Stone[] false_eye = new Stone[PAT3_MAX];
        protected static readonly Stone[] falsy_eye = new Stone[PAT3_MAX];
        protected static readonly short[,] move_dis = new short[PURE_BOARD_SIZE, PURE_BOARD_SIZE];
        protected static readonly Stone[] nb4_empty = new Stone[PAT3_MAX];
        protected static readonly Stone[] territory = new Stone[PAT3_MAX];
        protected static readonly uint[,] update_mask = new uint[40, 3] {
            { 0, 0x00004000, 0x00008000 },
            { 0, 0x00001000, 0x00002000 },
            { 0, 0x00000400, 0x00000800 },
            { 0, 0x00000100, 0x00000200 },
            { 0, 0x00000040, 0x00000080 },
            { 0, 0x00000010, 0x00000020 },
            { 0, 0x00000004, 0x00000008 },
            { 0, 0x00000001, 0x00000002 },
            { 0, 0x00100000, 0x00200000 },
            { 0, 0x00400000, 0x00800000 },
            { 0, 0x00010000, 0x00020000 },
            { 0, 0x00040000, 0x00080000 },
            { 0, 0x00001000, 0x00002000 },
            { 0, 0x00004000, 0x00008000 },
            { 0, 0x00010000, 0x00020000 },
            { 0, 0x00040000, 0x00080000 },
            { 0, 0x00100000, 0x00200000 },
            { 0, 0x00400000, 0x00800000 },
            { 0, 0x00000001, 0x00000002 },
            { 0, 0x00000004, 0x00000008 },
            { 0, 0x00000010, 0x00000020 },
            { 0, 0x00000040, 0x00000080 },
            { 0, 0x00000100, 0x00000200 },
            { 0, 0x00000400, 0x00000800 },
            { 0, 0x00010000, 0x00020000 },
            { 0, 0x00040000, 0x00080000 },
            { 0, 0x00100000, 0x00200000 },
            { 0, 0x00400000, 0x00800000 },
            { 0, 0x01000000, 0x02000000 },
            { 0, 0x04000000, 0x08000000 },
            { 0, 0x10000000, 0x20000000 },
            { 0, 0x40000000, 0x80000000 },
            { 0, 0x00000001, 0x00000002 },
            { 0, 0x00000004, 0x00000008 },
            { 0, 0x00000010, 0x00000020 },
            { 0, 0x00000040, 0x00000080 },
            { 0, 0x00000100, 0x00000200 },
            { 0, 0x00000400, 0x00000800 },
            { 0, 0x00001000, 0x00002000 },
            { 0, 0x00004000, 0x00008000 }
        };
        protected static readonly ulong[,] large_mask = new ulong[20, 3] {
            { 0, 0x0000000000100000, 0x0000000000200000 },
            { 0, 0x0000000000400000, 0x0000000000800000 },
            { 0, 0x0000000001000000, 0x0000000002000000 },
            { 0, 0x0000000004000000, 0x0000000008000000 },
            { 0, 0x0000000010000000, 0x0000000020000000 },
            { 0, 0x0000000040000000, 0x0000000080000000 },
            { 0, 0x0000000100000000, 0x0000000200000000 },
            { 0, 0x0000000400000000, 0x0000000800000000 },
            { 0, 0x0000001000000000, 0x0000002000000000 },
            { 0, 0x0000004000000000, 0x0000008000000000 },
            { 0, 0x0000000000000001, 0x0000000000000002 },
            { 0, 0x0000000000000004, 0x0000000000000008 },
            { 0, 0x0000000000000010, 0x0000000000000020 },
            { 0, 0x0000000000000040, 0x0000000000000080 },
            { 0, 0x0000000000000100, 0x0000000000000200 },
            { 0, 0x0000000000000400, 0x0000000000000800 },
            { 0, 0x0000000000001000, 0x0000000000002000 },
            { 0, 0x0000000000004000, 0x0000000000008000 },
            { 0, 0x0000000000010000, 0x0000000000020000 },
            { 0, 0x0000000000040000, 0x0000000000080000 },
        };
        #endregion
        #region 定值
        public const int PURE_BOARD_SIZE = 19;
        public const int OB_SIZE = 5;
        public const int BOARD_SIZE = PURE_BOARD_SIZE + OB_SIZE + OB_SIZE;
        public const int PURE_BOARD_MAX = PURE_BOARD_SIZE * PURE_BOARD_SIZE;
        public const int BOARD_MAX = BOARD_SIZE * BOARD_SIZE;
        public const int MAX_STRING = PURE_BOARD_MAX * 4 / 5;
        public const int MAX_NEIGHBOR = MAX_STRING;
        public const int BOARD_START = OB_SIZE;
        public const int BOARD_END = PURE_BOARD_SIZE + OB_SIZE - 1;
        public const int STRING_LIB_MAX = BOARD_SIZE * (PURE_BOARD_SIZE + OB_SIZE);
        public const int STRING_POS_MAX = BOARD_SIZE * (PURE_BOARD_SIZE + OB_SIZE);
        public const int STRING_END = STRING_POS_MAX - 1;
        public const int NEIGHBOR_END = MAX_NEIGHBOR - 1;
        public const int LIBERTY_END = STRING_LIB_MAX - 1;
        public const int MAX_RECORDS = PURE_BOARD_MAX * 3;
        public const int MAX_MOVES = PURE_BOARD_MAX * 3;
        public const short PASS = PURE_BOARD_MAX;
        public const short RESIGN = -2;
        public const float lambdaSarsa = 0.8f;
        public const float Cpuct = 0.1f;
        public const float LearningRateQTable = 0.2f;
        public const float Gramma = 0.8f;
        public const float KOMI = 7.5f;
        public const int MD2_MAX = 16777216;
        public const int PAT3_MAX = 65536;
        public const int MD2_LIMIT = 1060624;
        public const int PAT3_LIMIT = 4468;
        public const int GTP_COMMAND_NUM = 33;
        public const int GTP_COMMAND_SIZE = 64;
        public const int BUF_SIZE = 256;
        public const float RED = 0.35f;
        public const float GREEN = 0.75f;
        public const int LINEAR_THRESHOLD = 200;
        public const int HANDICAP_WEIGHT = 8;
        public const int LFR_DIMENSION = 5;
        public const int UCT_MASK_MAX = 64;
        public const int POS_ID_MAX = 64;
        public const int MOVE_DISTANCE_MAX = 16;
        public const int CFG_DISTANCE_MAX = 8;
        public const int LARGE_PAT_MAX = 150000;
        public const int OWNER_MAX = 11;
        public const int CRITICALITY_MAX = 7;
        public const int UCT_PHYSICALS_MAX = 1 << 14;
        public const float CRITICALITY_INIT = 0.765745f;
        public const float CRITICALITY_BIAS = 0.036f;
        public const float OWNER_K = 0.05f;
        public const float OWNER_BIAS = 34.0f;
        public const int MAX_NODES = 1000000;
        public const float ALL_THINKING_TIME = 90.0f;
        public const int CONST_PLAYOUT = 10000;
        public const float CONST_TIME = 10.0f;
        public const int PLAYOUT_SPEED = 1000;
        public const int TIME_RATE_9 = 20;
        public const int TIME_C_13 = 30;
        public const int TIME_MAXPLY_13 = 30;
        public const int TIME_C_19 = 60;
        public const int TIME_MAXPLY_19 = 80;
        public const int CRITICALITY_INTERVAL = 100;
        public const float FPU = 5.0f;
        public const float PROGRESSIVE_WIDENING = 1.8f;
        public const int EXPAND_THRESHOLD_9 = 20;
        public const int EXPAND_THRESHOLD_13 = 25;
        public const int EXPAND_THRESHOLD_19 = 40;
        public const int UCT_CHILD_MAX = PURE_BOARD_MAX + 1;
        public const int PASS_INDEX = 0;
        public const float BONUS_EQUIVALENCE = 1000;
        public const float BONUS_WEIGHT = 0.35f;
        public const float PASS_THRESHOLD = 0.90f;
        public const float RESIGN_THRESHOLD = 0.10f;
        public const int VIRTUAL_LOSS = 1;
        public const float c_puct = 1;
        public const float value_scale = 0.5f;
        public const uint UCT_HASH_SIZE = 262144;
        public const int HASH_MAX = 1048576;
        public const int BIT_MAX = 60;
        public const int SELFPLAY_MINSTEP = PURE_BOARD_MAX << 1;
        public const int PLAY_MINSTEP = PURE_BOARD_MAX / 2 - 10;
        public const int EngineBoardMax = PURE_BOARD_MAX;
        public const int EngineBoardSize = PURE_BOARD_SIZE;
        public const float MaxiumNodeKeepPersentage = 0.2f;
        public const int criticality_max = CRITICALITY_MAX;
        public const int DIM_BOARD = 0;
        public const int DIM_CURRENTPLAYER = PURE_BOARD_MAX;
        public const int DIM_CANDIDATE = PURE_BOARD_MAX * 2;
        public const int DIM_SEKI = PURE_BOARD_MAX * 3;
        public const int DIM_CAPTURE_POS_BLACK = PURE_BOARD_MAX * 4;
        public const int DIM_CAPTURE_POS_WHITE = PURE_BOARD_MAX * 5;
        public const int DIM_LIB_BLACK = PURE_BOARD_MAX * 6;
        public const int DIM_LIB_WHITE = PURE_BOARD_MAX * 7;
        public const int DIM_CUR_CAPTURE = PURE_BOARD_MAX * 8;
        protected const int HASH_HMIRROR = 2;
        protected const int HASH_VMIRROR = 1;
        protected const int HASH_XYFLIP = 4;
        #endregion
        #region 公共函数
        protected static ulong REV(ulong p) { return ((p) >> 2) | (((p) & 0x3) << 2); }
        protected static ulong REV10(ulong p) { return ((p) >> 20) | (((p) & 0x3) << 20); }
        protected static ulong REV12(ulong p) { return ((p) >> 24) | (((p) & 0x3) << 24); }
        protected static ulong REV14(ulong p) { return ((p) >> 28) | (((p) & 0x3) << 28); }
        protected static ulong REV16(ulong p) { return ((p) >> 32) | (((p) & 0x3) << 32); }
        protected static ulong REV18(ulong p) { return ((p) >> 36) | (((p) & 0x3) << 36); }
        protected static ulong REV2(ulong p) { return ((p) >> 4) | (((p) & 0x3) << 4); }
        protected static ulong REV3(ulong p) { return ((p) >> 4) | ((p) & 0xC) | (((p) & 0x3) << 4); }
        protected static ulong REV4(ulong p) { return ((p) >> 8) | (((p) & 0x3) << 8); }
        protected static ulong REV6(ulong p) { return ((p) >> 12) | (((p) & 0x3) << 12); }
        protected static ulong REV8(ulong p) { return ((p) >> 16) | (((p) & 0x3) << 16); }
        protected static int East => 1;
        protected static int EE => East + East;
        protected static int NE => North + East;
        protected static int NN => North + North;
        protected static int North => -board_size;
        protected static int NW => North + West;
        protected static int SE => South + East;
        protected static int South => board_size;
        protected static int SS => South + South;
        protected static int SW => South + West;
        protected static int West => -1;
        protected static int WW => West + West;
        public static short CORRECT_X(short pos) { return (short)(pos % board_size - OB_SIZE + 1); }
        public static short CORRECT_Y(short pos) { return (short)(pos / board_size - OB_SIZE + 1); }
        public static short DIS(short pos1, short pos2) { return move_dis[DX(pos1, pos2), DY(pos1, pos2)]; }
        public static void DisplayInputMD2(uint md2)
        {
            Console.WriteLine("\n");
            Console.WriteLine(" %c \n", stone[(md2 >> 16) & 0x3]);
            Console.WriteLine(" %c%c%c \n", stone[md2 & 0x3], stone[(md2 >> 2) & 0x3], stone[(md2 >> 4) & 0x3]);
            Console.WriteLine("%c%c*%c%c\n", stone[(md2 >> 22) & 0x3], stone[(md2 >> 6) & 0x3], stone[(md2 >> 8) & 0x3], stone[(md2 >> 18) & 0x3]);
            Console.WriteLine(" %c%c%c \n", stone[(md2 >> 10) & 0x3], stone[(md2 >> 12) & 0x3], stone[(md2 >> 14) & 0x3]);
            Console.WriteLine(" %c \n", stone[(md2 >> 20) & 0x3]);
        }
        public static void DisplayInputMD3(uint md3)
        {
            Console.WriteLine("\n");
            Console.WriteLine(" %c \n", stone[md3 & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md3 >> 22) & 0x3], stone[(md3 >> 2) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md3 >> 20) & 0x3], stone[(md3 >> 4) & 0x3]);
            Console.WriteLine("%c * %c\n", stone[(md3 >> 18) & 0x3], stone[(md3 >> 6) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md3 >> 16) & 0x3], stone[(md3 >> 8) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md3 >> 14) & 0x3], stone[(md3 >> 10) & 0x3]);
            Console.WriteLine(" %c \n", stone[(md3 >> 12) & 0x3]);
        }
        public static void DisplayInputMD4(uint md4)
        {
            Console.WriteLine("\n");
            Console.WriteLine(" %c \n", stone[md4 & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md4 >> 30) & 0x3], stone[(md4 >> 2) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md4 >> 28) & 0x3], stone[(md4 >> 4) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md4 >> 26) & 0x3], stone[(md4 >> 6) & 0x3]);
            Console.WriteLine("%c * %c\n", stone[(md4 >> 24) & 0x3], stone[(md4 >> 8) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md4 >> 22) & 0x3], stone[(md4 >> 10) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md4 >> 20) & 0x3], stone[(md4 >> 12) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md4 >> 18) & 0x3], stone[(md4 >> 14) & 0x3]);
            Console.WriteLine(" %c \n", stone[(md4 >> 16) & 0x3]);
        }
        public static void DisplayInputMD5(ulong md5)
        {
            Console.WriteLine("\n");
            Console.WriteLine(" %c \n", stone[md5 & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 38) & 0x3], stone[(md5 >> 2) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 36) & 0x3], stone[(md5 >> 4) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 34) & 0x3], stone[(md5 >> 6) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 32) & 0x3], stone[(md5 >> 8) & 0x3]);
            Console.WriteLine("%c * %c\n", stone[(md5 >> 30) & 0x3], stone[(md5 >> 10) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 28) & 0x3], stone[(md5 >> 12) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 26) & 0x3], stone[(md5 >> 14) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 24) & 0x3], stone[(md5 >> 16) & 0x3]);
            Console.WriteLine(" %c %c \n", stone[(md5 >> 22) & 0x3], stone[(md5 >> 18) & 0x3]);
            Console.WriteLine(" %c \n", stone[(md5 >> 20) & 0x3]);
        }
        public static void DisplayInputPat3(uint pat3)
        {
            Console.WriteLine("\n");
            Console.WriteLine("%c%c%c\n", stone[pat3 & 0x3], stone[(pat3 >> 2) & 0x3], stone[(pat3 >> 4) & 0x3]);
            Console.WriteLine("%c*%c\n", stone[(pat3 >> 6) & 0x3], stone[(pat3 >> 8) & 0x3]);
            Console.WriteLine("%c%c%c\n", stone[(pat3 >> 10) & 0x3], stone[(pat3 >> 12) & 0x3], stone[(pat3 >> 14) & 0x3]);
        }
        public static void DisplayInputPattern(Pattern_t pattern, int size)
        {
            uint md2, md3, md4;
            md2 = pattern.list[(int)MD_2];
            md3 = pattern.list[(int)MD_3];
            md4 = pattern.list[(int)MD_4];
            if (size == 4)
            {
                Console.WriteLine("\n");
                Console.WriteLine(" %c \n", stone[md4 & 0x3]);
                Console.WriteLine(" %c%c%c \n", stone[(md4 >> 30) & 0x3], stone[md3 & 0x3], stone[(md4 >> 2) & 0x3]);
                Console.WriteLine(" %c%c%c%c%c \n", stone[(md4 >> 28) & 0x3], stone[(md3 >> 22) & 0x3], stone[(md2 >> 16) & 0x3], stone[(md3 >> 2) & 0x3], stone[(md4 >> 4) & 0x3]);
                Console.WriteLine(" %c%c%c%c%c%c%c \n", stone[(md4 >> 26) & 0x3], stone[(md3 >> 20) & 0x3], stone[md2 & 0x3], stone[(md2 >> 2) & 0x3], stone[(md2 >> 4) & 0x3], stone[(md3 >> 4) & 0x3], stone[(md4 >> 6) & 0x3]);
                Console.WriteLine("%c%c%c%c*%c%c%c%c\n", stone[(md4 >> 24) & 0x3], stone[(md3 >> 18) & 0x3], stone[(md2 >> 22) & 0x3], stone[(md2 >> 6) & 0x3], stone[(md2 >> 8) & 0x3], stone[(md2 >> 18) & 0x3], stone[(md3 >> 6) & 0x3], stone[(md4 >> 8) & 0x3]);
                Console.WriteLine(" %c%c%c%c%c%c%c \n", stone[(md4 >> 22) & 0x3], stone[(md3 >> 16) & 0x3], stone[(md2 >> 10) & 0x3], stone[(md2 >> 12) & 0x3], stone[(md2 >> 14) & 0x3], stone[(md3 >> 8) & 0x3], stone[(md4 >> 10) & 0x3]);
                Console.WriteLine(" %c%c%c%c%c \n", stone[(md4 >> 20) & 0x3], stone[(md3 >> 14) & 0x3], stone[(md2 >> 20) & 0x3], stone[(md3 >> 10) & 0x3], stone[(md4 >> 12) & 0x3]);
                Console.WriteLine(" %c%c%c \n", stone[(md4 >> 18) & 0x3], stone[(md3 >> 12) & 0x3], stone[(md4 >> 14) & 0x3]);
                Console.WriteLine(" %c \n", stone[(md4 >> 16) & 0x3]);
            }
            else if (size == 3)
            {
                Console.WriteLine("\n");
                Console.WriteLine(" %c \n", stone[md3 & 0x3]);
                Console.WriteLine(" %c%c%c \n", stone[(md3 >> 22) & 0x3], stone[(md2 >> 16) & 0x3], stone[(md3 >> 2) & 0x3]);
                Console.WriteLine(" %c%c%c%c%c \n", stone[(md3 >> 20) & 0x3], stone[md2 & 0x3], stone[(md2 >> 2) & 0x3], stone[(md2 >> 4) & 0x3], stone[(md3 >> 4) & 0x3]);
                Console.WriteLine("%c%c%c*%c%c%c\n", stone[(md3 >> 18) & 0x3], stone[(md2 >> 22) & 0x3], stone[(md2 >> 6) & 0x3], stone[(md2 >> 8) & 0x3], stone[(md2 >> 18) & 0x3], stone[(md3 >> 6) & 0x3]);
                Console.WriteLine(" %c%c%c%c%c \n", stone[(md3 >> 16) & 0x3], stone[(md2 >> 10) & 0x3], stone[(md2 >> 12) & 0x3], stone[(md2 >> 14) & 0x3], stone[(md3 >> 8) & 0x3]);
                Console.WriteLine(" %c%c%c \n", stone[(md3 >> 14) & 0x3], stone[(md2 >> 20) & 0x3], stone[(md3 >> 10) & 0x3]);
                Console.WriteLine(" %c \n", stone[(md3 >> 12) & 0x3]);
            }
            else if (size == 2)
            {
                Console.WriteLine("\n");
                Console.WriteLine(" %c \n", stone[(md2 >> 16) & 0x3]);
                Console.WriteLine(" %c%c%c \n", stone[md2 & 0x3], stone[(md2 >> 2) & 0x3], stone[(md2 >> 4) & 0x3]);
                Console.WriteLine("%c%c*%c%c\n", stone[(md2 >> 22) & 0x3], stone[(md2 >> 6) & 0x3], stone[(md2 >> 8) & 0x3], stone[(md2 >> 18) & 0x3]);
                Console.WriteLine(" %c%c%c \n", stone[(md2 >> 10) & 0x3], stone[(md2 >> 12) & 0x3], stone[(md2 >> 14) & 0x3]);
                Console.WriteLine(" %c \n", stone[(md2 >> 20) & 0x3]);
            }
        }
        public static short DX(short pos1, short pos2) { return (short)Abs(board_x[pos1] - board_x[pos2]); }
        public static short DY(short pos1, short pos2) { return (short)Abs(board_y[pos1] - board_y[pos2]); }
        public static short EAST(short pos) { return (short)(pos + 1); }
        public static Stone FLIP_COLOR(Stone col) { return col ^ S_OB; }
        public static uint MD2(Pattern_t[] pat, int pos)
        {
            return pat[pos].list[(int)MD_2];
        }
        public static uint MD2HorizontalMirror(uint md2)
        {
            return (uint)((REV3((md2 & 0x00FC00) >> 10) << 10)
                              | (REV((md2 & 0x0003C0) >> 6) << 6)
                              | REV3(md2 & 0x00003F)
                              | (REV2((md2 & 0xCC0000) >> 18) << 18)
                              | (md2 & 0x330000));
        }
        public static uint MD2Reverse(uint md2)
        {
            return ((md2 >> 1) & 0x555555) | ((md2 & 0x555555) << 1);
        }
        public static uint MD2Rotate90(uint md2)
        {
            return ((md2 & 0x000003) << 10)
                | ((md2 & 0x000C0C) << 4)
                | ((md2 & 0x003030) >> 4)
                | ((md2 & 0x0300C0) << 6)
                | ((md2 & 0x000300) >> 6)
                | ((md2 & 0x00C000) >> 10)
                | ((md2 & 0xFC0000) >> 2);
        }
        public static void MD2Transpose16(uint md2, uint[] transp)
        {
            MD2Transpose8(md2, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD2Reverse(transp[i]);
            }
        }
        public static void MD2Transpose8(uint md2, uint[] transp)
        {
            transp[0] = md2;
            transp[1] = MD2VerticalMirror(md2);
            transp[2] = MD2HorizontalMirror(md2);
            transp[3] = MD2VerticalMirror(transp[2]);
            transp[4] = MD2Rotate90(md2);
            transp[5] = MD2Rotate90(transp[1]);
            transp[6] = MD2Rotate90(transp[2]);
            transp[7] = MD2Rotate90(transp[3]);
        }
        public static uint MD2VerticalMirror(uint md2)
        {
            return (uint)(((md2 & 0x00FC00) >> 10) | (md2 & 0x0003C0) | ((md2 & 0x00003F) << 10)
                              | (REV2((md2 & 0x330000) >> 16) << 16) | (md2 & 0xCC0000));
        }
        public static uint MD3(Pattern_t[] pat, int pos)
        {
            return pat[pos].list[(int)MD_3];
        }
        public static uint MD3HorizontalMirror(uint md3)
        {
            return (uint)((md3 & 0x003003)
                              | (REV10((md3 & 0xC0000C) >> 2) << 2) | (REV8((md3 & 0x300030) >> 4) << 4) | (REV6((md3 & 0x0C00C0) >> 6) << 6) | (REV4((md3 & 0x030300) >> 8) << 8) | (REV2((md3 & 0x00CC00) >> 10) << 10));
        }
        public static uint MD3Reverse(uint md3)
        {
            return ((md3 >> 1) & 0x555555) | ((md3 & 0x555555) << 1);
        }
        public static uint MD3Rotate90(uint md3)
        {
            return ((md3 & 0x00003F) << 18)
                | ((md3 & 0xFFFFC0) >> 6);
        }
        public static void MD3Transpose16(uint md3, uint[] transp)
        {
            MD3Transpose8(md3, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD3Reverse(transp[i]);
            }
        }
        public static void MD3Transpose8(uint md3, uint[] transp)
        {
            transp[0] = md3;
            transp[1] = MD3VerticalMirror(md3);
            transp[2] = MD3HorizontalMirror(md3);
            transp[3] = MD3VerticalMirror(transp[2]);
            transp[4] = MD3Rotate90(md3);
            transp[5] = MD3Rotate90(transp[1]);
            transp[6] = MD3Rotate90(transp[2]);
            transp[7] = MD3Rotate90(transp[3]);
        }
        public static uint MD3VerticalMirror(uint md3)
        {
            return (uint)((REV6(md3 & 0x003003)) | (REV4((md3 & 0x000C0C) >> 2) << 2) | (REV2((md3 & 0x000330) >> 4) << 4) | (REV4((md3 & 0xC0C000) >> 14) << 14) | (REV2((md3 & 0x330000) >> 16) << 16) | (md3 & 0x0C00C0));
        }
        public static uint MD4(Pattern_t[] pat, int pos)
        {
            return pat[pos].list[(int)MD_4];
        }
        public static uint MD4HorizontalMirror(uint md4)
        {
            return (uint)((md4 & 0x00030003)
                              | (REV14((md4 & 0xC000000C) >> 2) << 2) | (REV12((md4 & 0x30000030) >> 4) << 4) | (REV10((md4 & 0x0C0000C0) >> 6) << 6) | (REV8((md4 & 0x03000300) >> 8) << 8) | (REV6((md4 & 0x00C00C00) >> 10) << 10) | (REV4((md4 & 0x00303000) >> 12) << 12) | (REV2((md4 & 0x000CC000) >> 14) << 14));
        }
        public static uint MD4Reverse(uint md4)
        {
            return ((md4 >> 1) & 0x55555555) | ((md4 & 0x55555555) << 1);
        }
        public static uint MD4Rotate90(uint md4)
        {
            return ((md4 & 0x000000FF) << 24)
                | ((md4 & 0xFFFFFF00) >> 8);
        }
        public static void MD4Transpose16(uint md4, uint[] transp)
        {
            MD4Transpose8(md4, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD4Reverse(transp[i]);
            }
        }
        public static void MD4Transpose8(uint md4, uint[] transp)
        {
            transp[0] = md4;
            transp[1] = MD4VerticalMirror(md4);
            transp[2] = MD4HorizontalMirror(md4);
            transp[3] = MD4VerticalMirror(transp[2]);
            transp[4] = MD4Rotate90(md4);
            transp[5] = MD4Rotate90(transp[1]);
            transp[6] = MD4Rotate90(transp[2]);
            transp[7] = MD4Rotate90(transp[3]);
        }
        public static uint MD4VerticalMirror(uint md4)
        {
            return (uint)((REV8(md4 & 0x00030003)) | (REV6((md4 & 0x0000C00C) >> 2) << 2) | (REV4((md4 & 0x00003030) >> 4) << 4) | (REV2((md4 & 0x00000CC0) >> 6) << 6) | (REV6((md4 & 0xC00C0000) >> 18) << 18) | (REV4((md4 & 0x30300000) >> 20) << 20) | (REV2((md4 & 0x0CC00000) >> 22) << 22) | (md4 & 0x03000300));
        }
        public static ulong MD5(Pattern_t[] pat, int pos)
        {
            return pat[pos].large_list[(int)MD_5];
        }
        public static ulong MD5HorizontalMirror(ulong md5)
        {
            return (md5 & 0x0000300003)
                | (REV18((md5 & 0xC00000000C) >> 2) << 2)
                | (REV16((md5 & 0x3000000030) >> 4) << 4)
                | (REV14((md5 & 0x0C000000C0) >> 6) << 6)
                | (REV12((md5 & 0x0300000300) >> 8) << 8)
                | (REV10((md5 & 0x00C0000C00) >> 10) << 10)
                | (REV8((md5 & 0x0030003000) >> 12) << 12)
                | (REV6((md5 & 0x000C00C000) >> 14) << 14)
                | (REV4((md5 & 0x0003030000) >> 16) << 16)
                | (REV2((md5 & 0x0000CC0000) >> 18) << 18);
        }
        public static ulong MD5Reverse(ulong md5)
        {
            return ((md5 >> 1) & 0x5555555555) | ((md5 & 0x5555555555) << 1);
        }
        public static ulong MD5Rotate90(ulong md5)
        {
            return ((md5 & 0x00000003FF) << 30)
                | ((md5 & 0xFFFFFFFC00) >> 10);
        }
        public static void MD5Transpose16(ulong md5, ulong[] transp)
        {
            MD5Transpose8(md5, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD5Reverse(transp[i]);
            }
        }
        public static void MD5Transpose8(ulong md5, ulong[] transp)
        {
            transp[0] = md5;
            transp[1] = MD5VerticalMirror(md5);
            transp[2] = MD5HorizontalMirror(md5);
            transp[3] = MD5VerticalMirror(transp[2]);
            transp[4] = MD5Rotate90(md5);
            transp[5] = MD5Rotate90(transp[1]);
            transp[6] = MD5Rotate90(transp[2]);
            transp[7] = MD5Rotate90(transp[3]);
        }
        public static ulong MD5VerticalMirror(ulong md5)
        {
            return (REV10(md5 & 0x0000300003))
                | (REV8((md5 & 0x00000C000C) >> 2) << 2)
                | (REV6((md5 & 0x0000030030) >> 4) << 4)
                | (REV4((md5 & 0x000000C0C0) >> 6) << 6)
                | (REV2((md5 & 0x0000003300) >> 8) << 8)
                | (REV8((md5 & 0xC000C00000) >> 22) << 22)
                | (REV6((md5 & 0x3003000000) >> 24) << 24)
                | (REV4((md5 & 0x0C0C000000) >> 26) << 26)
                | (REV2((md5 & 0x0330000000) >> 28) << 28)
                | (md5 & 0x00C0000C00);
        }
        public static short NORTH(short pos) { return (short)(pos - board_size); }
        public static short NORTH_EAST(short pos) { return (short)(pos - board_size + 1); }
        public static short NORTH_WEST(short pos) { return (short)(pos - board_size - 1); }
        public static uint Pat3(Pattern_t[] pat, int pos)
        {
            return pat[pos].list[(int)MD_2] & 0xFFFF;
        }
        public static uint Pat3HorizontalMirror(uint pat3)
        {
            return (uint)((REV3((pat3 & 0xFC00) >> 10) << 10)
                              | (REV((pat3 & 0x03C0) >> 6) << 6)
                              | REV3(pat3 & 0x003F));
        }
        public static uint Pat3Reverse(uint pat3)
        {
            return ((pat3 >> 1) & 0x5555) | ((pat3 & 0x5555) << 1);
        }
        public static uint Pat3Rotate90(uint pat3)
        {
            return ((pat3 & 0x0003) << 10)
                | ((pat3 & 0x0C0C) << 4)
                | ((pat3 & 0x3030) >> 4)
                | ((pat3 & 0x00C0) << 6)
                | ((pat3 & 0x0300) >> 6)
                | ((pat3 & 0xC000) >> 10);
        }
        public static void Pat3Transpose16(uint pat3, uint[] transp)
        {
            Pat3Transpose8(pat3, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = Pat3Reverse(transp[i]);
            }
        }
        public static void Pat3Transpose8(uint pat3, uint[] transp)
        {
            transp[0] = pat3;
            transp[1] = Pat3VerticalMirror(pat3);
            transp[2] = Pat3HorizontalMirror(pat3);
            transp[3] = Pat3VerticalMirror(transp[2]);
            transp[4] = Pat3Rotate90(pat3);
            transp[5] = Pat3Rotate90(transp[1]);
            transp[6] = Pat3Rotate90(transp[2]);
            transp[7] = Pat3Rotate90(transp[3]);
        }
        public static uint Pat3VerticalMirror(uint pat3)
        {
            return ((pat3 & 0xFC00) >> 10) | (pat3 & 0x03C0) | ((pat3 & 0x003F) << 10);
        }
        public static short POS(short x, short y) { return (short)(x + y * board_size); }
        public static short POS(int x, int y) { return (short)(x + y * board_size); }
        public static short SOUTH(short pos) { return (short)(pos + board_size); }
        public static short SOUTH_EAST(short pos) { return (short)(pos + board_size + 1); }
        public static short SOUTH_WEST(short pos) { return (short)(pos + board_size - 1); }
        public static int TransformMove(short p, int i)
        {
            if (p == PASS || p == RESIGN)
                return p;
            int p0 = p;
            int x = X(p);
            int y = Y(p);
            if ((i & HASH_VMIRROR) != 0)
            {
                y = board_end - (y - board_start);
            }
            if ((i & HASH_HMIRROR) != 0)
            {
                x = board_end - (x - board_start);
            }
            if ((i & HASH_XYFLIP) != 0)
            {
                var temp = x;
                x = y;
                y = x;
            }
            int row = x;
            int col = y;
            if (row < board_start || row > board_end || col < board_start || col > board_end)
            {
                Console.Error.WriteLine("BAD TRANS " + p0 + "." + p + " " + board_size + " " + i + " " + row + "," + col);
                Environment.Exit(1);
            }
            return POS(x, y);
        }
        public static void UpdateMD2Empty(Pattern_t[] pat, int pos)
        {
            pat[pos + NW].list[(int)MD_2] &= 0xFF3FFF;
            pat[pos + North].list[(int)MD_2] &= 0xFFCFFF;
            pat[pos + NE].list[(int)MD_2] &= 0xFFF3FF;
            pat[pos + West].list[(int)MD_2] &= 0xFFFCFF;
            pat[pos + East].list[(int)MD_2] &= 0xFFFF3F;
            pat[pos + SW].list[(int)MD_2] &= 0xFFFFCF;
            pat[pos + South].list[(int)MD_2] &= 0xFFFFF3;
            pat[pos + SE].list[(int)MD_2] &= 0xFFFFFC;
            pat[pos + NN].list[(int)MD_2] &= 0xCFFFFF;
            pat[pos + EE].list[(int)MD_2] &= 0x3FFFFF;
            pat[pos + SS].list[(int)MD_2] &= 0xFCFFFF;
            pat[pos + WW].list[(int)MD_2] &= 0xF3FFFF;
        }
        public static void UpdateMD2Stone(Pattern_t[] pat, Stone color, int pos)
        {
            pat[pos + NW].list[(int)MD_2] |= update_mask[0, (int)color];
            pat[pos + North].list[(int)MD_2] |= update_mask[1, (int)color];
            pat[pos + NE].list[(int)MD_2] |= update_mask[2, (int)color];
            pat[pos + West].list[(int)MD_2] |= update_mask[3, (int)color];
            pat[pos + East].list[(int)MD_2] |= update_mask[4, (int)color];
            pat[pos + SW].list[(int)MD_2] |= update_mask[5, (int)color];
            pat[pos + South].list[(int)MD_2] |= update_mask[6, (int)color];
            pat[pos + SE].list[(int)MD_2] |= update_mask[7, (int)color];
            pat[pos + NN].list[(int)MD_2] |= update_mask[8, (int)color];
            pat[pos + EE].list[(int)MD_2] |= update_mask[9, (int)color];
            pat[pos + SS].list[(int)MD_2] |= update_mask[10, (int)color];
            pat[pos + WW].list[(int)MD_2] |= update_mask[11, (int)color];
        }
        public static void UpdatePat3Empty(Pattern_t[] pat, int pos)
        {
            pat[pos + NW].list[(int)MD_2] &= 0xFF3FFF;
            pat[pos + North].list[(int)MD_2] &= 0xFFCFFF;
            pat[pos + NE].list[(int)MD_2] &= 0xFFF3FF;
            pat[pos + West].list[(int)MD_2] &= 0xFFFCFF;
            pat[pos + East].list[(int)MD_2] &= 0xFFFF3F;
            pat[pos + SW].list[(int)MD_2] &= 0xFFFFCF;
            pat[pos + South].list[(int)MD_2] &= 0xFFFFF3;
            pat[pos + SE].list[(int)MD_2] &= 0xFFFFFC;
        }
        public static void UpdatePat3Stone(Pattern_t[] pat, Stone color, int pos)
        {
            pat[pos + NW].list[(int)MD_2] |= update_mask[0, (int)color];
            pat[pos + North].list[(int)MD_2] |= update_mask[1, (int)color];
            pat[pos + NE].list[(int)MD_2] |= update_mask[2, (int)color];
            pat[pos + West].list[(int)MD_2] |= update_mask[3, (int)color];
            pat[pos + East].list[(int)MD_2] |= update_mask[4, (int)color];
            pat[pos + SW].list[(int)MD_2] |= update_mask[5, (int)color];
            pat[pos + South].list[(int)MD_2] |= update_mask[6, (int)color];
            pat[pos + SE].list[(int)MD_2] |= update_mask[7, (int)color];
        }
        public static void UpdatePatternEmpty(Pattern_t[] pat, int pos)
        {
            pat[pos + NW].list[(int)MD_2] &= 0xFF3FFF;
            pat[pos + North].list[(int)MD_2] &= 0xFFCFFF;
            pat[pos + NE].list[(int)MD_2] &= 0xFFF3FF;
            pat[pos + West].list[(int)MD_2] &= 0xFFFCFF;
            pat[pos + East].list[(int)MD_2] &= 0xFFFF3F;
            pat[pos + SW].list[(int)MD_2] &= 0xFFFFCF;
            pat[pos + South].list[(int)MD_2] &= 0xFFFFF3;
            pat[pos + SE].list[(int)MD_2] &= 0xFFFFFC;
            pat[pos + NN].list[(int)MD_2] &= 0xCFFFFF;
            pat[pos + EE].list[(int)MD_2] &= 0x3FFFFF;
            pat[pos + SS].list[(int)MD_2] &= 0xFCFFFF;
            pat[pos + WW].list[(int)MD_2] &= 0xF3FFFF;
            pat[pos + NN + North].list[(int)MD_3] &= 0xFFCFFF;
            pat[pos + NN + East].list[(int)MD_3] &= 0xFF3FFF;
            pat[pos + EE + North].list[(int)MD_3] &= 0xFCFFFF;
            pat[pos + EE + East].list[(int)MD_3] &= 0xF3FFFF;
            pat[pos + EE + South].list[(int)MD_3] &= 0xCFFFFF;
            pat[pos + SS + East].list[(int)MD_3] &= 0x3FFFFF;
            pat[pos + SS + South].list[(int)MD_3] &= 0xFFFFFC;
            pat[pos + SS + West].list[(int)MD_3] &= 0xFFFFF3;
            pat[pos + WW + South].list[(int)MD_3] &= 0xFFFFCF;
            pat[pos + WW + West].list[(int)MD_3] &= 0xFFFF3F;
            pat[pos + WW + North].list[(int)MD_3] &= 0xFFFCFF;
            pat[pos + NN + West].list[(int)MD_3] &= 0xFFF3FF;
            pat[pos + NN + NN].list[(int)MD_4] &= 0xFFFCFFFF;
            pat[pos + NN + NE].list[(int)MD_4] &= 0xFFF3FFFF;
            pat[pos + NE + NE].list[(int)MD_4] &= 0xFFCFFFFF;
            pat[pos + EE + NE].list[(int)MD_4] &= 0xFF3FFFFF;
            pat[pos + EE + EE].list[(int)MD_4] &= 0xFCFFFFFF;
            pat[pos + EE + SE].list[(int)MD_4] &= 0xF3FFFFFF;
            pat[pos + SE + SE].list[(int)MD_4] &= 0xCFFFFFFF;
            pat[pos + SS + SE].list[(int)MD_4] &= 0x3FFFFFFF;
            pat[pos + SS + SS].list[(int)MD_4] &= 0xFFFFFFFC;
            pat[pos + SS + SW].list[(int)MD_4] &= 0xFFFFFFF3;
            pat[pos + SW + SW].list[(int)MD_4] &= 0xFFFFFFCF;
            pat[pos + WW + SW].list[(int)MD_4] &= 0xFFFFFF3F;
            pat[pos + WW + WW].list[(int)MD_4] &= 0xFFFFFCFF;
            pat[pos + WW + NW].list[(int)MD_4] &= 0xFFFFF3FF;
            pat[pos + NW + NW].list[(int)MD_4] &= 0xFFFFCFFF;
            pat[pos + NN + NW].list[(int)MD_4] &= 0xFFFF3FFF;
            pat[pos + NN + NN + North].large_list[(int)MD_5] &= 0xFFFFCFFFFF;
            pat[pos + NN + NN + East].large_list[(int)MD_5] &= 0xFFFF3FFFFF;
            pat[pos + NN + NE + East].large_list[(int)MD_5] &= 0xFFFCFFFFFF;
            pat[pos + NN + EE + East].large_list[(int)MD_5] &= 0xFFF3FFFFFF;
            pat[pos + NE + EE + East].large_list[(int)MD_5] &= 0xFFCFFFFFFF;
            pat[pos + EE + EE + East].large_list[(int)MD_5] &= 0xFF3FFFFFFF;
            pat[pos + SE + EE + East].large_list[(int)MD_5] &= 0xFCFFFFFFFF;
            pat[pos + SS + EE + East].large_list[(int)MD_5] &= 0xF3FFFFFFFF;
            pat[pos + SS + SE + East].large_list[(int)MD_5] &= 0xCFFFFFFFFF;
            pat[pos + SS + SS + East].large_list[(int)MD_5] &= 0x3FFFFFFFFF;
            pat[pos + SS + SS + South].large_list[(int)MD_5] &= 0xFFFFFFFFFC;
            pat[pos + SS + SS + West].large_list[(int)MD_5] &= 0xFFFFFFFFF3;
            pat[pos + SS + SW + West].large_list[(int)MD_5] &= 0xFFFFFFFFCF;
            pat[pos + SS + WW + West].large_list[(int)MD_5] &= 0xFFFFFFFF3F;
            pat[pos + SW + WW + West].large_list[(int)MD_5] &= 0xFFFFFFFCFF;
            pat[pos + WW + WW + West].large_list[(int)MD_5] &= 0xFFFFFFF3FF;
            pat[pos + NW + WW + West].large_list[(int)MD_5] &= 0xFFFFFFCFFF;
            pat[pos + NN + WW + West].large_list[(int)MD_5] &= 0xFFFFFF3FFF;
            pat[pos + NN + NW + West].large_list[(int)MD_5] &= 0xFFFFFCFFFF;
            pat[pos + NN + NN + West].large_list[(int)MD_5] &= 0xFFFFF3FFFF;
        }
        public static void UpdatePatternStone(Pattern_t[] pat, Stone color, int pos)
        {
            pat[pos + NW].list[(int)MD_2] |= update_mask[0, (int)color];
            pat[pos + North].list[(int)MD_2] |= update_mask[1, (int)color];
            pat[pos + NE].list[(int)MD_2] |= update_mask[2, (int)color];
            pat[pos + West].list[(int)MD_2] |= update_mask[3, (int)color];
            pat[pos + East].list[(int)MD_2] |= update_mask[4, (int)color];
            pat[pos + SW].list[(int)MD_2] |= update_mask[5, (int)color];
            pat[pos + South].list[(int)MD_2] |= update_mask[6, (int)color];
            pat[pos + SE].list[(int)MD_2] |= update_mask[7, (int)color];
            pat[pos + NN].list[(int)MD_2] |= update_mask[8, (int)color];
            pat[pos + EE].list[(int)MD_2] |= update_mask[9, (int)color];
            pat[pos + SS].list[(int)MD_2] |= update_mask[10, (int)color];
            pat[pos + WW].list[(int)MD_2] |= update_mask[11, (int)color];
            pat[pos + NN + North].list[(int)MD_3] |= update_mask[12, (int)color];
            pat[pos + NN + East].list[(int)MD_3] |= update_mask[13, (int)color];
            pat[pos + EE + North].list[(int)MD_3] |= update_mask[14, (int)color];
            pat[pos + EE + East].list[(int)MD_3] |= update_mask[15, (int)color];
            pat[pos + EE + South].list[(int)MD_3] |= update_mask[16, (int)color];
            pat[pos + SS + East].list[(int)MD_3] |= update_mask[17, (int)color];
            pat[pos + SS + South].list[(int)MD_3] |= update_mask[18, (int)color];
            pat[pos + SS + West].list[(int)MD_3] |= update_mask[19, (int)color];
            pat[pos + WW + South].list[(int)MD_3] |= update_mask[20, (int)color];
            pat[pos + WW + West].list[(int)MD_3] |= update_mask[21, (int)color];
            pat[pos + WW + North].list[(int)MD_3] |= update_mask[22, (int)color];
            pat[pos + NN + West].list[(int)MD_3] |= update_mask[23, (int)color];
            pat[pos + NN + NN].list[(int)MD_4] |= update_mask[24, (int)color];
            pat[pos + NN + NE].list[(int)MD_4] |= update_mask[25, (int)color];
            pat[pos + NE + NE].list[(int)MD_4] |= update_mask[26, (int)color];
            pat[pos + EE + NE].list[(int)MD_4] |= update_mask[27, (int)color];
            pat[pos + EE + EE].list[(int)MD_4] |= update_mask[28, (int)color];
            pat[pos + EE + SE].list[(int)MD_4] |= update_mask[29, (int)color];
            pat[pos + SE + SE].list[(int)MD_4] |= update_mask[30, (int)color];
            pat[pos + SS + SE].list[(int)MD_4] |= update_mask[31, (int)color];
            pat[pos + SS + SS].list[(int)MD_4] |= update_mask[32, (int)color];
            pat[pos + SS + SW].list[(int)MD_4] |= update_mask[33, (int)color];
            pat[pos + SW + SW].list[(int)MD_4] |= update_mask[34, (int)color];
            pat[pos + WW + SW].list[(int)MD_4] |= update_mask[35, (int)color];
            pat[pos + WW + WW].list[(int)MD_4] |= update_mask[36, (int)color];
            pat[pos + WW + NW].list[(int)MD_4] |= update_mask[37, (int)color];
            pat[pos + NW + NW].list[(int)MD_4] |= update_mask[38, (int)color];
            pat[pos + NN + NW].list[(int)MD_4] |= update_mask[39, (int)color];
            pat[pos + NN + NN + North].large_list[(int)MD_5] |= large_mask[0, (int)color];
            pat[pos + NN + NN + East].large_list[(int)MD_5] |= large_mask[1, (int)color];
            pat[pos + NN + NE + East].large_list[(int)MD_5] |= large_mask[2, (int)color];
            pat[pos + NN + EE + East].large_list[(int)MD_5] |= large_mask[3, (int)color];
            pat[pos + NE + EE + East].large_list[(int)MD_5] |= large_mask[4, (int)color];
            pat[pos + EE + EE + East].large_list[(int)MD_5] |= large_mask[5, (int)color];
            pat[pos + SE + EE + East].large_list[(int)MD_5] |= large_mask[6, (int)color];
            pat[pos + SS + EE + East].large_list[(int)MD_5] |= large_mask[7, (int)color];
            pat[pos + SS + SE + East].large_list[(int)MD_5] |= large_mask[8, (int)color];
            pat[pos + SS + SS + East].large_list[(int)MD_5] |= large_mask[9, (int)color];
            pat[pos + SS + SS + South].large_list[(int)MD_5] |= large_mask[10, (int)color];
            pat[pos + SS + SS + West].large_list[(int)MD_5] |= large_mask[11, (int)color];
            pat[pos + SS + SW + West].large_list[(int)MD_5] |= large_mask[12, (int)color];
            pat[pos + SS + WW + West].large_list[(int)MD_5] |= large_mask[13, (int)color];
            pat[pos + SW + WW + West].large_list[(int)MD_5] |= large_mask[14, (int)color];
            pat[pos + WW + WW + West].large_list[(int)MD_5] |= large_mask[15, (int)color];
            pat[pos + NW + WW + West].large_list[(int)MD_5] |= large_mask[16, (int)color];
            pat[pos + NN + WW + West].large_list[(int)MD_5] |= large_mask[17, (int)color];
            pat[pos + NN + NW + West].large_list[(int)MD_5] |= large_mask[18, (int)color];
            pat[pos + NN + NN + West].large_list[(int)MD_5] |= large_mask[19, (int)color];
        }
        public static short WEST(short pos) { return (short)(pos - 1); }
        public static short X(short pos) { return (short)(pos % board_size); }
        public static short Y(short pos) { return (short)(pos / board_size); }
        protected static short AddLiberty(String_t @string, short pos, short head)
        {
            int lib;
            if (@string.lib[pos] != 0) return pos;
            lib = head;
            while (@string.lib[lib] < pos)
            {
                lib = @string.lib[lib];
            }
            @string.lib[pos] = @string.lib[lib];
            @string.lib[lib] = pos;
            @string.libs++;
            return pos;
        }
        protected static void AddNeighbor(String_t @string, int id, int head)
        {
            int neighbor = 0;
            if (@string.neighbor[id] != 0) return;
            neighbor = head;
            while (@string.neighbor[neighbor] < id)
            {
                neighbor = @string.neighbor[neighbor];
            }
            @string.neighbor[id] = @string.neighbor[neighbor];
            @string.neighbor[neighbor] = (short)id;
            @string.neighbors++;
        }
        protected static void GetNeighbor4(out short[] neighbor4, short pos)
        {
            List<short> neighbor4_list = new List<short>();
            if (onboard_pos_2pure[NORTH(pos)] != PURE_BOARD_MAX)
                neighbor4_list.Add(NORTH(pos));
            if (onboard_pos_2pure[WEST(pos)] != PURE_BOARD_MAX)
                neighbor4_list.Add(WEST(pos));
            if (onboard_pos_2pure[EAST(pos)] != PURE_BOARD_MAX)
                neighbor4_list.Add(EAST(pos));
            if (onboard_pos_2pure[SOUTH(pos)] != PURE_BOARD_MAX)
                neighbor4_list.Add(SOUTH(pos));
            neighbor4 = neighbor4_list.ToArray();
        }
        protected static void GetNeighbor4P(out short[] neighbor4, short pos)
        {
            List<short> neighbor4_list = new List<short>();
            var P_pos = (short)(pos - pure_board_max);
            if (P_pos >= 0 && P_pos < pure_board_max)
                neighbor4_list.Add(P_pos);
            P_pos = (short)(pos + pure_board_max);
            if (P_pos >= 0 && P_pos < pure_board_max)
                neighbor4_list.Add(P_pos);
            P_pos = (short)(pos - 1);
            if (P_pos >= 0 && P_pos < pure_board_max)
                neighbor4_list.Add(P_pos);
            P_pos = (short)(pos + 1);
            if (P_pos >= 0 && P_pos < pure_board_max)
                neighbor4_list.Add(P_pos);
            neighbor4 = neighbor4_list.ToArray();
        }
        protected static void InitializeEye()
        {
            uint[] transp = new uint[8], pat3_transp16 = new uint[16];
            uint[] eye_pat3 = { 0x5554, 0x5556, 0x5544, 0x5546, 0x1554, 0x1556, 0x1544, 0x1546, 0x1564, 0x1146, 0xFD54, 0xFD55, 0xFF74, 0xFF75, 0x5566, 0xFD66 };
            uint[] false_eye_pat3 = { 0x5965, 0x9955, 0xFD56, 0xFF76 };
            uint[] falsy_eye_pat3 = { 0x9556, 0x9546, 0x9146, 0x5566, 0x5166, 0x1166 };
            uint[] complete_half_eye = { 0x5566, 0x5965, 0x5166, 0x5966, 0x1166, 0x1964, 0x1966, 0x9966, 0xFD56, 0xFD46, 0xFD66, 0xFF76 };
            uint[] half_3_eye = { 0x1144, 0x1146 };
            uint[] half_2_eye = { 0x5144, 0x5146, 0x5164, 0xFD44 };
            uint[] half_1_eye = { 0x5544, 0x5564, 0x5145, 0x5165, 0xFD54, 0xFF74 };
            uint[] complete_one_eye = { 0x5555, 0x5554, 0x5556, 0xFD55, 0xFF75 };
            Array.Clear(eye_condition, 0, eye_condition.Length);
            for (int i = 0; i < 12; i++)
            {
                Pat3Transpose16(complete_half_eye[i], pat3_transp16);
                for (int j = 0; j < 16; j++)
                {
                    eye_condition[pat3_transp16[j]] = E_COMPLETE_HALF_EYE;
                }
            }
            for (int i = 0; i < 2; i++)
            {
                Pat3Transpose16(half_3_eye[i], pat3_transp16);
                for (int j = 0; j < 16; j++)
                {
                    eye_condition[pat3_transp16[j]] = E_HALF_3_EYE;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                Pat3Transpose16(half_2_eye[i], pat3_transp16);
                for (int j = 0; j < 16; j++)
                {
                    eye_condition[pat3_transp16[j]] = E_HALF_2_EYE;
                }
            }
            for (int i = 0; i < 6; i++)
            {
                Pat3Transpose16(half_1_eye[i], pat3_transp16);
                for (int j = 0; j < 16; j++)
                {
                    eye_condition[pat3_transp16[j]] = E_HALF_1_EYE;
                }
            }
            for (int i = 0; i < 5; i++)
            {
                Pat3Transpose16(complete_one_eye[i], pat3_transp16);
                for (int j = 0; j < 16; j++)
                {
                    eye_condition[pat3_transp16[j]] = E_COMPLETE_ONE_EYE;
                }
            }
            eye[0x5555] = S_BLACK;
            eye[Pat3Reverse(0x5555)] = S_WHITE;
            eye[0x1144] = S_BLACK;
            eye[Pat3Reverse(0x1144)] = S_WHITE;
            for (int i = 0; i < 14; i++)
            {
                Pat3Transpose8(eye_pat3[i], transp);
                for (int j = 0; j < 8; j++)
                {
                    eye[transp[j]] = S_BLACK;
                    eye[Pat3Reverse(transp[j])] = S_WHITE;
                }
            }
            for (int i = 0; i < 4; i++)
            {
                Pat3Transpose8(false_eye_pat3[i], transp);
                for (int j = 0; j < 8; j++)
                {
                    false_eye[transp[j]] = S_BLACK;
                    false_eye[Pat3Reverse(transp[j])] = S_WHITE;
                }
            }
            foreach (uint p in falsy_eye_pat3)
            {
                Pat3Transpose8(p, transp);
                for (int j = 0; j < 8; j++)
                {
                    falsy_eye[transp[j]] = S_BLACK;
                    falsy_eye[Pat3Reverse(transp[j])] = S_WHITE;
                }
            }
        }
        protected static void InitializeNeighbor()
        {
            for (int i = 0; i < PAT3_MAX; i++)
            {
                byte empty = 0;
                if (((i >> 2) & 0x3) == (int)S_EMPTY) empty++;
                if (((i >> 6) & 0x3) == (int)S_EMPTY) empty++;
                if (((i >> 8) & 0x3) == (int)S_EMPTY) empty++;
                if (((i >> 12) & 0x3) == (int)S_EMPTY) empty++;
                nb4_empty[i] = (Stone)empty;
            }
        }
        protected static void InitializeTerritory()
        {
            for (int i = 0; i < PAT3_MAX; i++)
            {
                if ((i & 0x1144) == 0x1144)
                {
                    territory[i] = S_BLACK;
                }
                else if ((i & 0x2288) == 0x2288)
                {
                    territory[i] = S_WHITE;
                }
            }
        }
        protected static bool check_superko = true;
        protected static float default_komi;
        public static void SetKomi(float new_komi)
        {
            default_komi = new_komi;
            komi[0] = dynamic_komi[0] = default_komi;
            komi[(int)S_BLACK] = dynamic_komi[(int)S_BLACK] = default_komi + 1;
            komi[(int)S_WHITE] = dynamic_komi[(int)S_WHITE] = default_komi - 1;
        }
        public static int RevTransformMove(short p, int i)
        {
            if (p == PASS || p == RESIGN)
                return p;
            int p0 = p;
            int x = X(p);
            int y = Y(p);
            if ((i & HASH_XYFLIP) != 0)
            {
                var temp = x;
                x = y;
                y = x;
            }
            if ((i & HASH_HMIRROR) != 0)
            {
                x = board_end - (x - board_start);
            }
            if ((i & HASH_VMIRROR) != 0)
            {
                y = board_end - (y - board_start);
            }
            int row = x;
            int col = y;
            if (row < board_start || row > board_end || col < board_start || col > board_end)
            {
                Console.Error.WriteLine("BAD TRANS " + p0 + "." + p + " " + board_size + " " + i + " " + row + "," + col);
                Environment.Exit(1);
            }
            return POS(x, y);
        }
        public static void SetSuperKo(bool flag)
        {
            check_superko = flag;
        }
        public static int PureBoardPos(short pos)
        {
            int x = X(pos) - OB_SIZE;
            int y = Y(pos) - OB_SIZE;
            return x + y * pure_board_size;
        }
        private static bool IsNeighbor(int pos0, int pos1)
        {
            int index_distance = pos0 - pos1;
            return index_distance == 1
                || index_distance == -1
                || index_distance == board_size
                || index_distance == -board_size;
        }
        public int ToBoardPos(int pos, bool convertFull = false)
        {
            if (convertFull)
            {
                if (pos < 0 || pos >= pure_board_max)
                    return PASS;
                else
                    return onboard_pos_2full[pos];
            }
            else
            {
                if (pos < 0 || pos >= board_max)
                    return pure_board_max;
                else
                    return onboard_pos_2pure[pos];
            }
        }
        protected bool NonEmptyPos(short pos)
        {
            return board[pos] != S_EMPTY && board[pos] != S_OB;
        }
        public static string BoardInfo { get; protected set; }
        #endregion

        /// <summary>
        /// 深度复制
        /// </summary>
        /// <param name="dst">目标位置</param>
        /// <param name="src">源位置</param>
        public static void DeepCopy(ref Board dst, Board src)
        {
            if (src == null) return;
            if (dst == null) dst = new Board();
            IFormatter formatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();
            formatter.Serialize(memoryStream, src);
            memoryStream.Seek(0, SeekOrigin.Begin);
            dst = formatter.Deserialize(memoryStream) as Board;
            memoryStream.Close();
        }
        public static float GetScore(Board game_t)
        {
            var game = game_t.Clone() as Board;
            RemoveDeadlyStones(game);
            (short[] area, short[] territory, int[] captures, float areaScore, float territoryScore) score = default((short[], short[], int[], float, float));
            var areamap = AreaMap(game_t.board);
            score.area = new short[2];
            score.territory = new short[2];
            score.captures = game.capture_num;
            for (short pos = 0; pos < areamap.Length; pos++)
            {
                var sign = areamap[pos];
                if (sign == 0) continue;
                var index = (sign > 0 ? 0 : 1);
                score.area[index]++;
                if (game_t.board[onboard_pos_2full[pos]] != S_EMPTY) score.territory[index]++;
            }
            score.areaScore = score.area[0] - score.area[1] - komi[0];
            score.territoryScore = score.territory[0] - score.territory[1] + score.captures[0] - score.captures[1] - komi[0];
            return score.territoryScore;
        }
        public void Clear()
        {
            short x, y, pos;
            record.Clear();
            for (int index = 0; index < pat.Length; index++)
                pat[index].Reset();
            for (int index = 0; index < @string.Length; index++)
                @string[index].Reset();
            Array.Clear(prisoner, 0, prisoner.Length);
            Array.Clear(board, 0, board.Length);
            Array.Clear(seki, 0, seki.Length);
            Array.Clear(candidates, 0, candidates.Length);
            Array.Clear(string_id, 0, string_id.Length);
            Array.Clear(string_next, 0, string_next.Length);
            for (int index = 0; index < (int)S_OB - 1; index++)
                for (int j = 0; j < PURE_BOARD_MAX; j++)
                    capture_pos[index, j] = 0;
            preHashCodes.Clear();
            ko_pos = 0;
            ko_move = 0;
            current_hash = 0;
            previous1_hash = 0;
            previous2_hash = 0;
            positional_hash = 0;
            current_hash = 0;
            previous1_hash = 0;
            previous2_hash = 0;
            pass_count = 0;
            for (y = 0; y < board_size; y++)
            {
                for (x = 0; x < OB_SIZE; x++)
                {
                    board[POS(x, y)] = S_OB;
                    board[POS(y, x)] = S_OB;
                    board[POS(y, (short)(board_size - 1 - x))] = S_OB;
                    board[POS((short)(board_size - 1 - x), y)] = S_OB;
                }
            }
            for (y = board_start; y <= board_end; y++)
            {
                for (x = board_start; x <= board_end; x++)
                {
                    pos = POS(x, y);
                    candidates[onboard_pos_2pure[pos]] = true;
                }
            }
            ClearPattern(pat);
            Score = 0;
            Area = null;
        }
        /// <summary>
        /// 复制一份的深度复制
        /// </summary>
        /// <returns></returns>
        public Board Clone()
        {
            IFormatter formatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();
            formatter.Serialize(memoryStream, this);
            memoryStream.Seek(0, SeekOrigin.Begin);
            var dst = formatter.Deserialize(memoryStream) as Board;
            return dst;
            //return TransExpV2<Board, Board>.Trans(this);
        }
        public void Copy(ref Board OutBoard)
        {
            if (OutBoard == null)
            {
                OutBoard = new Board();
                OutBoard.Clear();
            }
            else
            {
                OutBoard.record.Clear();
            }
            for (int index = 0; index < record.Count; index++)
                OutBoard.record.Add(record[index].Clone() as Move_t);
            for (int index = 0; index < pat.Length; index++)
                pat[index].Clone(ref OutBoard.pat[index]);
            for (int index = 0; index < @string.Length; index++)
                @string[index].Clone(ref OutBoard.@string[index]);
            prisoner.CopyTo(OutBoard.prisoner, 0);
            board.CopyTo(OutBoard.board, 0);
            seki.CopyTo(OutBoard.seki, 0);
            candidates.CopyTo(OutBoard.candidates, 0);
            string_id.CopyTo(OutBoard.string_id, 0);
            string_next.CopyTo(OutBoard.string_next, 0);
            for (int index = 0; index < (int)S_OB - 1; index++)
                for (int j = 0; j < PURE_BOARD_MAX; j++)
                    OutBoard.capture_pos[index, j] = capture_pos[index, j];
            OutBoard.ko_pos = ko_pos;
            OutBoard.ko_move = ko_move;
            OutBoard.current_hash = current_hash;
            OutBoard.previous1_hash = previous1_hash;
            OutBoard.previous2_hash = previous2_hash;
            OutBoard.positional_hash = positional_hash;
            OutBoard.current_hash = current_hash;
            OutBoard.previous1_hash = previous1_hash;
            OutBoard.previous2_hash = previous2_hash;
            OutBoard.pass_count = pass_count;
        }
        /// <summary>
        /// 一对多深度复制
        /// </summary>
        /// <param name="counts">复制的份数</param>
        /// <returns>复制后的数组</returns>
        public Board[] DeepCopy(int counts)
        {
            Board[] arrays = new Board[counts];
            IFormatter formatter = new BinaryFormatter();
            MemoryStream memoryStream = new MemoryStream();
            formatter.Serialize(memoryStream, this);
            for (int index = 0; index < counts; index++)
            {
                memoryStream.Seek(0, SeekOrigin.Begin);
                arrays[index] = formatter.Deserialize(memoryStream) as Board;
            }
            memoryStream.Close();
            return arrays;
        }
        public bool IsCurrentStaticEmptyPos(short pos)
        {
            List<int> SurroundPos = new List<int>();
            int n_pos = pos + NW;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            n_pos = pos + North;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            n_pos = pos + NE;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            n_pos = pos + East;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            n_pos = pos + SE;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            n_pos = pos + South;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            n_pos = pos + SW;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            n_pos = pos + West;
            if (board[n_pos] != S_OB)
                SurroundPos.Add(n_pos);
            int MySidePos = 0;
            int OppSidePos = 0;
            foreach (var m_pos in SurroundPos)
            {
                if (board[m_pos] == Board_CurrentPlayer)
                    MySidePos++;
                else if (board[m_pos] == FLIP_COLOR(Board_CurrentPlayer))
                    OppSidePos++;
            }
            if (MySidePos == 0 || OppSidePos == 0) return true;
            switch (SurroundPos.Count)
            {
                case 8:
                    if ((OppSidePos > 0 && MySidePos > 6) || (MySidePos > 0 && OppSidePos > 6)) return true;
                    break;
                case 5:
                    if ((OppSidePos > 0 && MySidePos > 3) || (MySidePos > 0 && OppSidePos > 3)) return true;
                    break;
                case 3:
                    if ((OppSidePos > 0 && MySidePos > 1) || (MySidePos > 0 && OppSidePos > 1)) return true;
                    break;
                default:
                    break;
            }
            return false;
        }
        public bool LastestPassMove(int last_index)
        {
            if (record.Count > last_index && last_index >= 0)
                return record[record.Count - last_index - 1].pos == PASS;
            return false;
        }
        /// <summary>
        /// UCT搜索树使用的落子函数
        /// </summary>
        /// <param name="pos">落子位置</param>
        /// <returns>是否落子成功</returns>
        public bool PutStone_UCT(int pos)
        {
            if (pos == PURE_BOARD_MAX || pos == BoardSize)
            {
                PutStone(this, PASS, Board_CurrentPlayer);
                return true;
            }
            else
            {
                var f_pos = onboard_pos_2full[pos];
                if (IsLegal(this, f_pos, Board_CurrentPlayer))
                {
                    PutStone(this, f_pos, Board_CurrentPlayer);
                    return true;
                }
                else return false;
            }
        }
        /// <summary>
        /// 纯棋盘上的点变换
        /// </summary>
        /// <param name="pos">位置</param>
        /// <param name="convertFull">是否转为全棋盘的位置</param>
        /// <returns>点位</returns>
        public void UpdateScoreEsimate((sbyte[] area, float score) data)
        {
            Area = data.area;
            Score = data.score - komi[0];
        }
        /// <summary>
        /// 预制的空棋盘
        /// </summary>
        public static Board EmptyBoard { get; set; }
        public static Board AllocateGame()
        {
            return EmptyBoard.Clone();
        }
        public static int CalculateScore(Board game, bool FinalScore = false)
        {
            var board = game.board;
            int i;
            int pos;
            Stone color;
            var scores = new int[(int)S_MAX];
            if (FinalScore)
                CheckBentFourInTheCorner(game);
            if (game.Moves > (pure_board_max >> 3))
                CheckSeki(game);
            for (i = 0; i < pure_board_max; i++)
            {
                pos = onboard_pos_2full[i];
                color = board[pos];
                if (color == S_EMPTY) color = territory[Pat3(game.pat, pos)];
                scores[(int)color]++;
            }
            return scores[(int)S_BLACK] - scores[(int)S_WHITE];
        }
        public static int CapturableCandidate(Board game, int id)
        {
            String_t[] @string = game.@string;
            int neighbor = @string[id].neighbor[0];
            bool flag = false;
            int capturable_pos = -1;
            while (neighbor != NEIGHBOR_END)
            {
                if (@string[neighbor].libs == 1)
                {
                    if (@string[neighbor].size >= 2)
                    {
                        return -1;
                    }
                    else
                    {
                        if (flag)
                        {
                            return -1;
                        }
                        capturable_pos = @string[neighbor].lib[0];
                        flag = true;
                    }
                }
                neighbor = @string[id].neighbor[neighbor];
            }
            return capturable_pos;
        }
        public static bool CheckIsCapturableAtari(Board game, short pos, Stone color, int opponent_pos)
        {
            Stone other = FLIP_COLOR(color);
            int neighbor;
            int id;
            int libs;
            if (!IsLegal(game, pos, color))
            {
                return false;
            }
            CopyGame(ref capturable_game, game);
            PutStone(capturable_game, pos, color);
            var @string = capturable_game.@string;
            var string_id = capturable_game.string_id;
            id = string_id[opponent_pos];
            neighbor = @string[id].neighbor[0];
            while (neighbor != NEIGHBOR_END)
            {
                if (@string[neighbor].libs == 1)
                {
                    return false;
                }
                neighbor = @string[id].neighbor[neighbor];
            }
            if (!IsLegal(capturable_game, @string[string_id[opponent_pos]].lib[0], other))
            {
                return true;
            }
            PutStone(capturable_game, @string[string_id[opponent_pos]].lib[0], other);
            libs = @string[string_id[opponent_pos]].libs;
            if (libs == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static LIBERTY_STATE CheckLibertyState(Board game, short pos, Stone color, int id)
        {
            int string_pos = game.@string[id].origin;
            int libs = game.@string[id].libs;
            int new_libs;
            if (!IsLegal(game, pos, color))
                return L_DECREASE;
            CopyGame(ref liberty_game, game);
            PutStone(liberty_game, pos, color);
            var @string = liberty_game.@string;
            var string_id = liberty_game.string_id;
            new_libs = @string[string_id[string_pos]].libs;
            if (new_libs > libs + 1)
                return L_INCREASE;
            else if (new_libs > libs)
                return L_EVEN;
            else
                return L_DECREASE;
        }
        public static int CheckOiotoshi(Board game, short pos, Stone color, int opponent_pos)
        {
            Stone other = FLIP_COLOR(color);
            int neighbor;
            int id, num = -1;
            if (!IsLegal(game, pos, color))
            {
                return -1;
            }
            CopyGame(ref oiotoshi_game, game);
            PutStone(oiotoshi_game, pos, color);
            var @string = oiotoshi_game.@string;
            var string_id = oiotoshi_game.string_id;
            id = string_id[opponent_pos];
            neighbor = @string[id].neighbor[0];
            while (neighbor != NEIGHBOR_END)
            {
                if (@string[neighbor].libs == 1)
                {
                    return -1;
                }
                neighbor = @string[id].neighbor[neighbor];
            }
            if (!IsLegal(oiotoshi_game, @string[string_id[opponent_pos]].lib[0], other))
            {
                return -1;
            }
            PutStone(oiotoshi_game, @string[string_id[opponent_pos]].lib[0], other);
            if (@string[string_id[opponent_pos]].libs == 1)
            {
                num = @string[string_id[opponent_pos]].size;
            }
            return num;
        }
        public static void CheckSeki(Board game)
        {
            short i, j, k, pos, id;
            var board = game.board;
            var string_id = game.string_id;
            var @string = game.@string;
            var seki_candidate = new bool[BOARD_MAX];
            var lib1_id = new short[4];
            var lib2_id = new short[4];
            short[] neighbor4;
            short neighbor1_lib, neighbor2_lib, lib1_ids, lib2_ids, lib1, lib2;
            bool already_checked;
            for (i = 0; i < pure_board_max; i++)
            {
                pos = onboard_pos_2full[i];
                if (IsSelfAtari(game, S_BLACK, pos) && IsSelfAtari(game, S_WHITE, pos))
                    seki_candidate[pos] = true;
            }
            for (i = 0; i < MAX_STRING; i++)
            {
                if (!@string[i].flag || @string[i].libs != 2) continue;
                if (@string[i].size >= 6) continue;
                lib1 = @string[i].lib[0];
                lib2 = @string[i].lib[lib1];
                if (seki_candidate[lib1] && seki_candidate[lib2])
                {
                    GetNeighbor4(out neighbor4, lib1);
                    lib1_ids = 0;
                    for (j = 0; j < neighbor4.Length; j++)
                    {
                        if (board[neighbor4[j]] == S_BLACK || board[neighbor4[j]] == S_WHITE)
                        {
                            id = string_id[neighbor4[j]];
                            if (id != i)
                            {
                                already_checked = false;
                                for (k = 0; k < lib1_ids; k++)
                                {
                                    if (lib1_id[k] == id)
                                    {
                                        already_checked = true;
                                        break;
                                    }
                                }
                                if (!already_checked)
                                    lib1_id[lib1_ids++] = id;
                            }
                        }
                    }
                    GetNeighbor4(out neighbor4, lib2);
                    lib2_ids = 0;
                    for (j = 0; j < neighbor4.Length; j++)
                    {
                        if (board[neighbor4[j]] == S_BLACK || board[neighbor4[j]] == S_WHITE)
                        {
                            id = string_id[neighbor4[j]];
                            if (id != i)
                            {
                                already_checked = false;
                                for (k = 0; k < lib2_ids; k++)
                                {
                                    if (lib2_id[k] == id)
                                    {
                                        already_checked = true;
                                        break;
                                    }
                                }
                                if (!already_checked)
                                    lib2_id[lib2_ids++] = id;
                            }
                        }
                    }
                    if (lib1_ids == 1 && lib2_ids == 1)
                    {
                        neighbor1_lib = @string[lib1_id[0]].lib[0];
                        if (neighbor1_lib == lib1 || neighbor1_lib == lib2)
                            neighbor1_lib = @string[lib1_id[0]].lib[neighbor1_lib];
                        neighbor2_lib = @string[lib2_id[0]].lib[0];
                        if (neighbor2_lib == lib1 || neighbor2_lib == lib2)
                            neighbor2_lib = @string[lib2_id[0]].lib[neighbor2_lib];
                        if (neighbor1_lib == neighbor2_lib)
                        {
                            if (eye_condition[Pat3(game.pat, neighbor1_lib)] != E_NOT_EYE)
                                game.seki[onboard_pos_2pure[lib1]] = game.seki[onboard_pos_2pure[lib2]] = game.seki[onboard_pos_2pure[neighbor1_lib]] = true;
                        }
                        else if (eye_condition[Pat3(game.pat, neighbor1_lib)] == E_COMPLETE_HALF_EYE && eye_condition[Pat3(game.pat, neighbor2_lib)] == E_COMPLETE_HALF_EYE)
                        {
                            int tmp_id1 = 0, tmp_id2 = 0;
                            GetNeighbor4(out neighbor4, neighbor1_lib);
                            for (j = 0; j < neighbor4.Length; j++)
                            {
                                if (board[neighbor4[j]] == S_BLACK || board[neighbor4[j]] == S_WHITE)
                                {
                                    id = string_id[neighbor4[j]];
                                    if (id != lib1_id[0] && id != lib2_id[0] && id != tmp_id1)
                                        tmp_id1 = id;
                                }
                            }
                            GetNeighbor4(out neighbor4, neighbor2_lib);
                            for (j = 0; j < neighbor4.Length; j++)
                            {
                                var pos_nb = neighbor4[j];
                                if (board[pos_nb] == S_BLACK || board[pos_nb] == S_WHITE)
                                {
                                    id = string_id[pos_nb];
                                    if (id != lib1_id[0] && id != lib2_id[0] && id != tmp_id2)
                                        tmp_id2 = id;
                                }
                            }
                            if (tmp_id1 == tmp_id2)
                            {
                                game.seki[onboard_pos_2pure[lib1]] = game.seki[onboard_pos_2pure[lib2]] = game.seki[onboard_pos_2pure[neighbor1_lib]] = game.seki[onboard_pos_2pure[neighbor2_lib]] = true;
                            }
                        }
                    }
                }
            }
        }
        public static void ClearBoard(Board game)
        {
            DeepCopy(ref game, EmptyBoard);
            #region OldClear
            //int i, x, y, pos;
            //game.record.Clear();
            //for (int index = 0; index < game.pat.Length; index++)
            //    game.pat[index].Reset();
            //for (int index = 0; index < game.@string.Length; index++)
            //    game.@string[index].Reset();
            //Array.Clear(game.prisoner, 0, game.prisoner.Length);
            //Array.Clear(game.board, 0, game.board.Length);
            //Array.Clear(game.seki, 0, game.seki.Length);
            //Array.Clear(game.candidates, 0, game.candidates.Length);
            //Array.Clear(game.string_id, 0, game.string_id.Length);
            //Array.Clear(game.string_next, 0, game.string_next.Length);
            //Array.Clear(game.capture_num, 0, game.capture_num.Length);
            //for (int index = 0; index < (int)S_OB; index++)
            //    for (int j = 0; j < PURE_BOARD_MAX; j++)
            //        game.capture_pos[index, j] = 0;
            //game.ko_pos = 0;
            //game.ko_move = 0;
            //game.current_hash = 0;
            //game.previous1_hash = 0;
            //game.previous2_hash = 0;
            //game.positional_hash = 0;
            //game.current_hash = 0;
            //game.previous1_hash = 0;
            //game.previous2_hash = 0;
            //game.pass_count = 0;
            //Array.Clear(game.candidates, 0, game.candidates.Length);
            //for (y = 0; y < board_size; y++)
            //{
            //    for (x = 0; x < OB_SIZE; x++)
            //    {
            //        game.board[POS(x, y)] = S_OB;
            //        game.board[POS(y, x)] = S_OB;
            //        game.board[POS(y, board_size - 1 - x)] = S_OB;
            //        game.board[POS(board_size - 1 - x, y)] = S_OB;
            //    }
            //}
            //for (y = board_start; y <= board_end; y++)
            //{
            //    for (x = board_start; x <= board_end; x++)
            //    {
            //        pos = POS(x, y);
            //        game.candidates[pos] = true;
            //    }
            //}
            //for (i = 0; i < game.@string.Length; i++)
            //{
            //    game.@string[i].Reset();
            //}
            //ClearPattern(game.pat);
            #endregion
        }
        public static void ClearPattern(Pattern_t[] pat)
        {
            int y;
            for (int i = 0; i < pat.Length; i++)
                pat[i].Reset();
            for (y = board_start; y <= board_end; y++)
            {
                pat[POS(y, board_start)].list[(int)MD_2] |= 0x0003003F; pat[POS(y, board_start)].list[(int)MD_3] |= 0x00F0003F; pat[POS(y, board_start)].list[(int)MD_4] |= 0xFC0000FF; pat[POS(y, board_start)].large_list[(int)MD_5] |= 0xFF000003FF;
                pat[POS(board_end, y)].list[(int)MD_2] |= 0x000CC330; pat[POS(board_end, y)].list[(int)MD_3] |= 0x00000FFC; pat[POS(board_end, y)].list[(int)MD_4] |= 0x0000FFFC; pat[POS(board_end, y)].large_list[(int)MD_5] |= 0x00000FFFFC;
                pat[POS(y, board_end)].list[(int)MD_2] |= 0x0030FC00; pat[POS(y, board_end)].list[(int)MD_3] |= 0x0003FF00; pat[POS(y, board_end)].list[(int)MD_4] |= 0x00FFFC00; pat[POS(y, board_end)].large_list[(int)MD_5] |= 0x003FFFF000;
                pat[POS(board_start, y)].list[(int)MD_2] |= 0x00C00CC3; pat[POS(board_start, y)].list[(int)MD_3] |= 0x00FFC000; pat[POS(board_start, y)].list[(int)MD_4] |= 0xFFFC0000; pat[POS(board_start, y)].large_list[(int)MD_5] |= 0xFFFFC00000;
                pat[POS(y, board_start + 1)].list[(int)MD_2] |= 0x00030000; pat[POS(y, board_start + 1)].list[(int)MD_3] |= 0x00C0000F; pat[POS(y, board_start + 1)].list[(int)MD_4] |= 0xF000003F; pat[POS(y, board_start + 1)].large_list[(int)MD_5] |= 0xFC000000FF;
                pat[POS(board_end - 1, y)].list[(int)MD_2] |= 0x000C0000; pat[POS(board_end - 1, y)].list[(int)MD_3] |= 0x000003F0; pat[POS(board_end - 1, y)].list[(int)MD_4] |= 0x00003FF0; pat[POS(board_end - 1, y)].large_list[(int)MD_5] |= 0x000003FFF0;
                pat[POS(y, board_end - 1)].list[(int)MD_2] |= 0x00300000; pat[POS(y, board_end - 1)].list[(int)MD_3] |= 0x0000FC00; pat[POS(y, board_end - 1)].list[(int)MD_4] |= 0x003FF000; pat[POS(y, board_end - 1)].large_list[(int)MD_5] |= 0x000FFFC000;
                pat[POS(board_start + 1, y)].list[(int)MD_2] |= 0x00C00000; pat[POS(board_start + 1, y)].list[(int)MD_3] |= 0x003F0000; pat[POS(board_start + 1, y)].list[(int)MD_4] |= 0x3FF00000; pat[POS(board_start + 1, y)].large_list[(int)MD_5] |= 0x3FFF000000;
                pat[POS(y, board_start + 2)].list[(int)MD_3] |= 0x00000003; pat[POS(y, board_start + 2)].list[(int)MD_4] |= 0xC000000F; pat[POS(y, board_start + 2)].large_list[(int)MD_5] |= 0xF00000003F;
                pat[POS(board_end - 2, y)].list[(int)MD_3] |= 0x000000C0; pat[POS(board_end - 2, y)].list[(int)MD_4] |= 0x00000FC0; pat[POS(board_end - 2, y)].large_list[(int)MD_5] |= 0x000000FFC0;
                pat[POS(y, board_end - 2)].list[(int)MD_3] |= 0x00003000; pat[POS(y, board_end - 2)].list[(int)MD_4] |= 0x000FC000; pat[POS(y, board_end - 2)].large_list[(int)MD_5] |= 0x0003FF0000;
                pat[POS(board_start + 2, y)].list[(int)MD_3] |= 0x000C0000; pat[POS(board_start + 2, y)].list[(int)MD_4] |= 0x0FC00000; pat[POS(board_start + 2, y)].large_list[(int)MD_5] |= 0x0FFC000000;
                pat[POS(y, board_start + 3)].list[(int)MD_4] |= 0x00000003; pat[POS(y, board_start + 3)].large_list[(int)MD_5] |= 0xC00000000F;
                pat[POS(board_end - 3, y)].list[(int)MD_4] |= 0x00000300; pat[POS(board_end - 3, y)].large_list[(int)MD_5] |= 0x0000003F00;
                pat[POS(y, board_end - 3)].list[(int)MD_4] |= 0x00030000; pat[POS(y, board_end - 3)].large_list[(int)MD_5] |= 0x0000FC0000;
                pat[POS(board_start + 3, y)].list[(int)MD_4] |= 0x03000000; pat[POS(board_start + 3, y)].large_list[(int)MD_5] |= 0x03F0000000;
                pat[POS(y, board_start + 4)].large_list[(int)MD_5] |= 0x0000000003;
                pat[POS(board_end - 4, y)].large_list[(int)MD_5] |= 0x0000000C00;
                pat[POS(y, board_end - 4)].large_list[(int)MD_5] |= 0x0000300000;
                pat[POS(board_start + 4, y)].large_list[(int)MD_5] |= 0x00C0000000;
            }
        }
        public static void CopyGame(ref Board dst, Board src)
        {
            DeepCopy(ref dst, src);
        }
        public static void InitializeBoard(ref Board game)
        {
            DeepCopy(ref game, EmptyBoard);
        }
        public static void InitializeConst()
        {
            short i;
            komi[0] = default_komi;
            komi[(short)S_BLACK] = default_komi + 1.0f;
            komi[(short)S_WHITE] = default_komi - 1.0f;
            i = 0;
            for (short index = 0; index < onboard_pos_2pure.Length; index++)
                onboard_pos_2pure[index] = PURE_BOARD_MAX;
            for (short y = board_start; y <= board_end; y++)
            {
                for (short x = board_start; x <= board_end; x++)
                {
                    var pos = POS(x, y);
                    onboard_pos_2pure[pos] = i;
                    onboard_pos_2full[i++] = pos;
                    board_x[pos] = x;
                    board_y[pos] = y;
                }
            }
            for (short y = board_start; y <= board_end; y++)
            {
                for (short x = board_start; x <= (board_start + pure_board_size / 2); x++)
                {
                    border_dis_x[POS(x, y)] = (short)(x - (OB_SIZE - 1));
                    border_dis_x[POS((short)(board_end + OB_SIZE - x), y)] = (short)(x - (OB_SIZE - 1));
                    border_dis_y[POS(y, x)] = (short)(x - (OB_SIZE - 1));
                    border_dis_y[POS(y, (short)(board_end + OB_SIZE - x))] = (short)(x - (OB_SIZE - 1));
                }
            }
            for (short y = 0; y < pure_board_size; y++)
            {
                for (short x = 0; x < pure_board_size; x++)
                {
                    move_dis[x, y] = (short)(x + y + ((x > y) ? x : y));
                    if (move_dis[x, y] >= MOVE_DISTANCE_MAX) move_dis[x, y] = MOVE_DISTANCE_MAX - 1;
                }
            }
            Array.Clear(board_pos_id, 0, board_pos_id.Length);
            i = 1;
            for (short y = board_start; y <= (board_start + pure_board_size / 2); y++)
            {
                for (short x = board_start; x <= y; x++)
                {
                    board_pos_id[POS(x, y)] = i;
                    board_pos_id[POS((short)(board_end + OB_SIZE - x), y)] = i;
                    board_pos_id[POS(y, x)] = i;
                    board_pos_id[POS(y, (short)(board_end + OB_SIZE - x))] = i;
                    board_pos_id[POS(x, (short)(board_end + OB_SIZE - y))] = i;
                    board_pos_id[POS((short)(board_end + OB_SIZE - x), (short)(board_end + OB_SIZE - y))] = i;
                    board_pos_id[POS((short)(board_end + OB_SIZE - y), x)] = i;
                    board_pos_id[POS((short)(board_end + OB_SIZE - y), (short)(board_end + OB_SIZE - x))] = i;
                    i++;
                }
            }
            cross[0] = -board_size - 1;
            cross[1] = -board_size + 1;
            cross[2] = board_size - 1;
            cross[3] = board_size + 1;
            corner[0] = POS(board_start, board_start);
            corner[1] = POS(board_start, board_end);
            corner[2] = POS(board_end, board_start);
            corner[3] = POS(board_end, board_end);
            corner_neighbor[0, 0] = EAST(POS(board_start, board_start));
            corner_neighbor[0, 1] = SOUTH(POS(board_start, board_start));
            corner_neighbor[1, 0] = NORTH(POS(board_start, board_end));
            corner_neighbor[1, 1] = EAST(POS(board_start, board_end));
            corner_neighbor[2, 0] = WEST(POS(board_end, board_start));
            corner_neighbor[2, 1] = SOUTH(POS(board_end, board_start));
            corner_neighbor[3, 0] = NORTH(POS(board_end, board_end));
            corner_neighbor[3, 1] = WEST(POS(board_end, board_end));
            InitializeNeighbor();
            InitializeEye();
            InitializeTerritory();
        }
        public static bool IsAlreadyCaptured(Board game, Stone color, short id, short[] player_id, short player_ids)
        {
            String_t[] @string = game.@string;
            var string_id = game.string_id;
            short lib1, lib2;
            bool @checked;
            short[] neighbor4;
            int i, j;
            if (@string[id].libs == 1)
            {
                return true;
            }
            else if (@string[id].libs == 2)
            {
                lib1 = @string[id].lib[0];
                lib2 = @string[id].lib[lib1];
                GetNeighbor4(out neighbor4, lib1);
                @checked = false;
                for (i = 0; i < neighbor4.Length; i++)
                {
                    for (j = 0; j < player_ids; j++)
                    {
                        if (player_id[j] == string_id[neighbor4[i]])
                        {
                            @checked = true;
                            player_id[j] = 0;
                        }
                    }
                }
                if (@checked == false) return false;
                GetNeighbor4(out neighbor4, lib2);
                @checked = false;
                for (i = 0; i < neighbor4.Length; i++)
                {
                    for (j = 0; j < player_ids; j++)
                    {
                        if (player_id[j] == string_id[neighbor4[i]])
                        {
                            @checked = true;
                            player_id[j] = 0;
                        }
                    }
                }
                if (@checked == false) return false;
                for (i = 0; i < player_ids; i++)
                {
                    if (player_id[i] != 0)
                    {
                        return false;
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsCapturableAtari(Board game, short pos, Stone color, int opponent_pos)
        {
            Stone other = FLIP_COLOR(color);
            int neighbor;
            int id;
            int libs;
            if (!IsLegal(game, pos, color))
            {
                return false;
            }
            capturable_game = game.Clone() as Board;
            PutStone(capturable_game, pos, color);
            var @string = capturable_game.@string;
            var string_id = capturable_game.string_id;
            id = string_id[opponent_pos];
            neighbor = @string[id].neighbor[0];
            while (neighbor != NEIGHBOR_END)
            {
                if (@string[neighbor].libs == 1)
                {
                    return false;
                }
                neighbor = @string[id].neighbor[neighbor];
            }
            if (!IsLegal(capturable_game, @string[string_id[opponent_pos]].lib[0], other))
            {
                return true;
            }
            PutStone(capturable_game, @string[string_id[opponent_pos]].lib[0], other);
            libs = @string[string_id[opponent_pos]].libs;
            return libs == 1;
        }
        public static bool IsCapturableAtariForSimulation(Board game, short pos, Stone color, short id)
        {
            var board = game.board;
            var @string = game.@string;
            var string_id = game.string_id;
            Stone other = FLIP_COLOR(color);
            bool neighbor = false;
            int index_distance;
            int connect_libs = 0;
            int tmp_id;
            var lib = @string[id].lib[0];
            if (lib == pos)
            {
                lib = @string[id].lib[lib];
            }
            index_distance = lib - pos;
            var pat3 = Pat3(game.pat, lib);
            var empty = nb4_empty[pat3];
            if (empty == S_OB)
            {
                return false;
            }
            if (index_distance == 1) neighbor = true;
            if (index_distance == -1) neighbor = true;
            if (index_distance == board_size) neighbor = true;
            if (index_distance == -board_size) neighbor = true;
            if ((neighbor && empty >= S_OB) || (!neighbor && empty >= S_WHITE))
            {
                return false;
            }
            if (board[NORTH(lib)] == other &&
                string_id[NORTH(lib)] != id)
            {
                tmp_id = string_id[NORTH(lib)];
                if (@string[tmp_id].libs > 2)
                {
                    return false;
                }
                else
                {
                    connect_libs += @string[tmp_id].libs - 1;
                }
            }
            if (board[WEST(lib)] == other &&
                string_id[WEST(lib)] != id)
            {
                tmp_id = string_id[WEST(lib)];
                if (@string[tmp_id].libs > 2)
                {
                    return false;
                }
                else
                {
                    connect_libs += @string[tmp_id].libs - 1;
                }
            }
            if (board[EAST(lib)] == other &&
                string_id[EAST(lib)] != id)
            {
                tmp_id = string_id[EAST(lib)];
                if (@string[tmp_id].libs > 2)
                {
                    return false;
                }
                else
                {
                    connect_libs += @string[tmp_id].libs - 1;
                }
            }
            if (board[SOUTH(lib)] == other &&
                string_id[SOUTH(lib)] != id)
            {
                tmp_id = string_id[SOUTH(lib)];
                if (@string[tmp_id].libs > 2)
                {
                    return false;
                }
                else
                {
                    connect_libs += @string[tmp_id].libs - 1;
                }
            }
            if ((neighbor && connect_libs < 2) ||
                (!neighbor && connect_libs < 1))
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        public static bool IsCapturableNeighborNone(Board game, int id)
        {
            String_t[] @string = game.@string;
            int neighbor = @string[id].neighbor[0];
            while (neighbor != NEIGHBOR_END)
            {
                if (@string[neighbor].libs == 1)
                    return false;
                neighbor = @string[id].neighbor[neighbor];
            }
            return true;
        }
        public static bool IsDeadlyExtension(Board game, Stone color, int id)
        {
            Board search_game = new Board();
            Stone other = FLIP_COLOR(color);
            short pos = game.@string[id].lib[0];
            if (nb4_empty[Pat3(game.pat, pos)] == 0 && IsSuicide(game, game.@string, other, pos))
                return true;
            CopyGame(ref search_game, game);
            PutStone(search_game, pos, other);
            if (search_game.@string[search_game.string_id[pos]].libs == 1)
                return true;
            else
                return false;
        }
        public static bool IsLegal(Board game, short pos, Stone color)
        {
            if (game.board[pos] != S_EMPTY)
            {
                return false;
            }
            if (nb4_empty[Pat3(game.pat, pos)] == 0 && IsSuicide(game, game.@string, color, pos))
            {
                return false;
            }
            if (game.ko_pos == pos && game.ko_move == (game.Moves - 1))
            {
                return false;
            }
            if (check_superko && pos != PASS)
            {
                Stone other = FLIP_COLOR(color);
                var @string = game.@string;
                var string_id = game.string_id;
                var string_next = game.string_next;
                ulong hash = game.positional_hash;
                short[] neighbor4;
                var check = new short[4];
                short @checked = 0, id, str_pos;
                bool flag;
                GetNeighbor4(out neighbor4, pos);
                for (short i = 0; i < neighbor4.Length; i++)
                {
                    if (game.board[neighbor4[i]] == other)
                    {
                        id = string_id[neighbor4[i]];
                        if (@string[id].libs == 1)
                        {
                            flag = false;
                            for (int j = 0; j < @checked; j++)
                            {
                                if (check[j] == id)
                                {
                                    flag = true;
                                }
                            }
                            if (flag)
                            {
                                continue;
                            }
                            str_pos = @string[id].origin;
                            do
                            {
                                hash ^= hash_bit[str_pos, (int)other];
                                str_pos = string_next[str_pos];
                            } while (str_pos != STRING_END);
                        }
                        check[@checked++] = id;
                    }
                }
                //避免同型局面的出现
                hash ^= hash_bit[pos, (int)color];
                if (game.preHashCodes.Contains(hash))
                    return false;
                //for (int i = 0; i < game.Moves; i++)
                //{
                //    if (game.record[i].hash == hash)
                //        return false;
                //}
            }
            return true;
        }
        public static bool IsLegalNotEye(Board game, short pos, Stone color)
        {
            var string_id = game.string_id;
            String_t[] @string = game.@string;
            if (game.board[pos] != S_EMPTY)
            {
                //game.candidates[onboard_pos_2pure[pos]] = false;
                return false;
            }
            if (game.seki[onboard_pos_2pure[pos]])
            {
                return false;
            }
            bool has_lib_1Str = false;
            GetNeighbor4(out short[] nblib1_4, pos);
            for (int i = 0; i < nblib1_4.Length; i++)
            {
                has_lib_1Str = has_lib_1Str || @string[string_id[nblib1_4[i]]].libs == 1;
            }
            if (eye[Pat3(game.pat, pos)] != color || has_lib_1Str)
            {
                if (nb4_empty[Pat3(game.pat, pos)] == 0 && IsSuicide(game, @string, color, pos))
                {
                    return false;
                }
                if (game.ko_pos == pos && game.ko_move == (game.Moves - 1))
                {
                    return false;
                }
                if (false_eye[Pat3(game.pat, pos)] == color)
                {
                    if (IsFalseEyeConnection(game, pos, color))
                    {
                        return true;
                    }
                    else
                    {
                        //game.candidates[onboard_pos_2pure[pos]] = false;
                        return false;
                    }
                }
                return true;
            }
            //game.candidates[onboard_pos_2pure[pos]] = false;
            return false;
        }
        public static bool IsSelfAtari(Board game, Stone color, short pos)
        {
            //Stone[] board = game.board;
            var board = game.board;
            String_t[] @string = game.@string;
            var string_id = game.string_id;
            Stone other = FLIP_COLOR(color);
            var already = new int[4];
            int already_num = 0;
            int lib, count = 0, libs = 0;
            var lib_candidate = new int[10];
            int i;
            int id;
            bool @checked;
            //if (board[NORTH(pos)] == S_EMPTY) lib_candidate[libs++] = NORTH(pos);
            //if (board[WEST(pos)] == S_EMPTY) lib_candidate[libs++] = WEST(pos);
            //if (board[EAST(pos)] == S_EMPTY) lib_candidate[libs++] = EAST(pos);
            //if (board[SOUTH(pos)] == S_EMPTY) lib_candidate[libs++] = SOUTH(pos);
            GetNeighbor4(out short[] SourroundPos, pos);
            for (int s_pos = 0; s_pos < SourroundPos.Length; s_pos++)
                if (board[s_pos] == S_EMPTY) lib_candidate[libs++] = s_pos;
            if (libs >= 2) return false;
            for (int s_pos = 0; s_pos < SourroundPos.Length; s_pos++)
            {
                if (board[s_pos] == color)
                {
                    id = string_id[s_pos];
                    if (@string[id].libs > 2) return false;
                    lib = @string[id].lib[0];
                    count = 0;
                    while (lib != LIBERTY_END)
                    {
                        if (lib != pos)
                        {
                            @checked = false;
                            for (i = 0; i < libs; i++)
                            {
                                if (lib_candidate[i] == lib)
                                {
                                    @checked = true;
                                    break;
                                }
                            }
                            if (!@checked)
                            {
                                lib_candidate[libs + count] = lib;
                                count++;
                            }
                        }
                        lib = @string[id].lib[lib];
                    }
                    libs += count;
                    already[already_num++] = id;
                    if (libs >= 2) return false;
                }
                else if (board[s_pos] == other && @string[string_id[s_pos]].libs == 1)
                {
                    return false;
                }
            }
            #region OLDCode
            //if (board[NORTH(pos)] == color)
            //{
            //    id = string_id[NORTH(pos)];
            //    if (@string[id].libs > 2) return false;
            //    lib = @string[id].lib[0];
            //    count = 0;
            //    while (lib != LIBERTY_END)
            //    {
            //        if (lib != pos)
            //        {
            //            @checked = false;
            //            for (i = 0; i < libs; i++)
            //            {
            //                if (lib_candidate[i] == lib)
            //                {
            //                    @checked = true;
            //                    break;
            //                }
            //            }
            //            if (!@checked)
            //            {
            //                lib_candidate[libs + count] = lib;
            //                count++;
            //            }
            //        }
            //        lib = @string[id].lib[lib];
            //    }
            //    libs += count;
            //    already[already_num++] = id;
            //    if (libs >= 2) return false;
            //}
            //else if (board[NORTH(pos)] == other && @string[string_id[NORTH(pos)]].libs == 1)
            //{
            //    return false;
            //}
            //if (board[WEST(pos)] == color)
            //{
            //    id = string_id[WEST(pos)];
            //    if (already[0] != id)
            //    {
            //        if (@string[id].libs > 2) return false;
            //        lib = @string[id].lib[0];
            //        count = 0;
            //        while (lib != LIBERTY_END)
            //        {
            //            if (lib != pos)
            //            {
            //                @checked = false;
            //                for (i = 0; i < libs; i++)
            //                {
            //                    if (lib_candidate[i] == lib)
            //                    {
            //                        @checked = true;
            //                        break;
            //                    }
            //                }
            //                if (!@checked)
            //                {
            //                    lib_candidate[libs + count] = lib;
            //                    count++;
            //                }
            //            }
            //            lib = @string[id].lib[lib];
            //        }
            //        libs += count;
            //        already[already_num++] = id;
            //        if (libs >= 2) return false;
            //    }
            //}
            //else if (board[WEST(pos)] == other && @string[string_id[WEST(pos)]].libs == 1)
            //{
            //    return false;
            //}
            //if (board[EAST(pos)] == color)
            //{
            //    id = string_id[EAST(pos)];
            //    if (already[0] != id && already[1] != id)
            //    {
            //        if (@string[id].libs > 2) return false;
            //        lib = @string[id].lib[0];
            //        count = 0;
            //        while (lib != LIBERTY_END)
            //        {
            //            if (lib != pos)
            //            {
            //                @checked = false;
            //                for (i = 0; i < libs; i++)
            //                {
            //                    if (lib_candidate[i] == lib)
            //                    {
            //                        @checked = true;
            //                        break;
            //                    }
            //                }
            //                if (!@checked)
            //                {
            //                    lib_candidate[libs + count] = lib;
            //                    count++;
            //                }
            //            }
            //            lib = @string[id].lib[lib];
            //        }
            //        libs += count;
            //        already[already_num++] = id;
            //        if (libs >= 2) return false;
            //    }
            //}
            //else if (board[EAST(pos)] == other && @string[string_id[EAST(pos)]].libs == 1)
            //{
            //    return false;
            //}
            //if (board[SOUTH(pos)] == color)
            //{
            //    id = string_id[SOUTH(pos)];
            //    if (already[0] != id && already[1] != id && already[2] != id)
            //    {
            //        if (@string[id].libs > 2) return false;
            //        lib = @string[id].lib[0];
            //        count = 0;
            //        while (lib != LIBERTY_END)
            //        {
            //            if (lib != pos)
            //            {
            //                @checked = false;
            //                for (i = 0; i < libs; i++)
            //                {
            //                    if (lib_candidate[i] == lib)
            //                    {
            //                        @checked = true;
            //                        break;
            //                    }
            //                }
            //                if (!@checked)
            //                {
            //                    lib_candidate[libs + count] = lib;
            //                    count++;
            //                }
            //            }
            //            lib = @string[id].lib[lib];
            //        }
            //        libs += count;
            //        already[already_num++] = id;
            //        if (libs >= 2) return false;
            //    }
            //}
            //else if (board[SOUTH(pos)] == other && @string[string_id[SOUTH(pos)]].libs == 1)
            //{
            //    return false;
            //}
            #endregion
            return true;
        }
        public static bool IsSelfAtariCapture(Board game, short pos, Stone color, int id)
        {
            int string_pos = game.@string[id].origin;
            if (!IsLegal(game, pos, color))
                return false;
            CopyGame(ref capture_game, game);
            PutStone(capture_game, pos, color);
            var @string = capture_game.@string;
            var string_id = capture_game.string_id;
            if (@string[string_id[string_pos]].libs == 1)
                return true;
            else
                return false;
        }
        public static bool IsSelfAtariCaptureForSimulation(Board game, short pos, Stone color, short lib)
        {
            var board = game.board;
            var @string = game.@string;
            var string_id = game.string_id;
            Stone other = FLIP_COLOR(color);
            int id;
            int size = 0;
            if (lib != pos ||
                nb4_empty[Pat3(game.pat, pos)] != 0)
            {
                return false;
            }
            if (board[NORTH(pos)] == color)
            {
                id = string_id[NORTH(pos)];
                if (@string[id].libs > 1)
                {
                    return false;
                }
            }
            else if (board[NORTH(pos)] == other)
            {
                id = string_id[NORTH(pos)];
                size += @string[id].size;
                if (size > 1)
                {
                    return false;
                }
            }
            if (board[WEST(pos)] == color)
            {
                id = string_id[WEST(pos)];
                if (@string[id].libs > 1)
                {
                    return false;
                }
            }
            else if (board[WEST(pos)] == other)
            {
                id = string_id[WEST(pos)];
                size += @string[id].size;
                if (size > 1)
                {
                    return false;
                }
            }
            if (board[EAST(pos)] == color)
            {
                id = string_id[EAST(pos)];
                if (@string[id].libs > 1)
                {
                    return false;
                }
            }
            else if (board[EAST(pos)] == other)
            {
                id = string_id[EAST(pos)];
                size += @string[id].size;
                if (size > 1)
                {
                    return false;
                }
            }
            if (board[SOUTH(pos)] == color)
            {
                id = string_id[SOUTH(pos)];
                if (@string[id].libs > 1)
                {
                    return false;
                }
            }
            else if (board[SOUTH(pos)] == other)
            {
                id = string_id[SOUTH(pos)];
                size += @string[id].size;
                if (size > 1)
                {
                    return false;
                }
            }
            return true;
        }
        public static void PutStone(Board game, short pos, Stone color)
        {
            var string_id = game.string_id;
            var board = game.board;
            String_t[] @string = game.@string;
            Stone other = FLIP_COLOR(color);
            int connection = 0;
            var connect = new short[4];
            short prisoner = 0;
            short[] neighbor;
            game.previous2_hash = game.previous1_hash;
            game.previous1_hash = game.current_hash;
            bool recorded = false;
            if (game.ko_move != 0 && game.ko_move == game.Moves)
            {
                game.current_hash ^= hash_bit[game.ko_pos, (int)HASH_KO];
            }
            if (game.Moves < MAX_RECORDS)
            {
                Move_t newmove = new Move_t(color, Max(pos, RESIGN), 0);
                game.record.Add(newmove);
                recorded = true;
            }
            if (pos == PASS || pos == RESIGN)
            {
                if (recorded)
                {
                    game.LatestRecord.hash = game.positional_hash;
                }
                game.current_hash ^= hash_bit[game.pass_count++, (int)HASH_PASS];
                if (game.pass_count >= BOARD_MAX)
                {
                    game.pass_count = 0;
                }
                return;
            }
            board[pos] = color;
            game.candidates[onboard_pos_2pure[pos]] = false;
            game.current_hash ^= hash_bit[pos, (int)color];
            game.positional_hash ^= hash_bit[pos, (int)color];
            UpdatePatternStone(game.pat, color, pos);
            GetNeighbor4(out neighbor, pos);
            for (int i = 0; i < neighbor.Length; i++)
            {
                if (board[neighbor[i]] == color)
                {
                    RemoveLiberty(game, @string[string_id[neighbor[i]]], pos);
                    connect[connection++] = string_id[neighbor[i]];
                }
                else if (board[neighbor[i]] == other)
                {
                    RemoveLiberty(game, @string[string_id[neighbor[i]]], pos);
                    if (@string[string_id[neighbor[i]]].libs == 0)
                    {
                        prisoner += RemoveString(game, @string[string_id[neighbor[i]]]);
                        game.prisoner[(int)color - 1] += prisoner;
                    }
                }
            }
            if (connection == 0)
            {
                MakeString(game, pos, color);
                if (prisoner == 1 && @string[string_id[pos]].libs == 1)
                {
                    game.ko_move = game.Moves;
                    game.ko_pos = @string[string_id[pos]].lib[0];
                    game.current_hash ^= hash_bit[game.ko_pos, (int)HASH_KO];
                }
            }
            else if (connection == 1)
            {
                AddStone(game, pos, color, connect[0]);
            }
            else
            {
                ConnectString(game, pos, color, connection, connect);
            }
            if (game.Moves < MAX_RECORDS)
            {
                game.LatestRecord.hash = game.positional_hash;
            }
            game.preHashCodes.Add(game.positional_hash);
            CheckSeki(game);
            for (int index = 0; index < pure_board_max; index++)
            {
                game.candidates[index] = IsLegal(game, onboard_pos_2full[index], game.Board_CurrentPlayer) && IsLegalNotEye(game, onboard_pos_2full[index], game.Board_CurrentPlayer);
            }
        }
        public static void RemoveDeadlyStones(Board game)
        {
            return;
            //for (int ppos = 0; ppos < pure_board_max; ppos++)
            //{
            //    var fpos = onboard_pos_2full[ppos];
            //    Stone other = FLIP_COLOR(game.board[fpos]);
            //    var id = game.string_id[fpos];
            //    if (!game.@string[id].flag) continue;
            //    var string_neigbours = game.@string[id].neighbor;
            //    bool 
            //    foreach (var ids in string_neigbours)
            //    {
            //        if (!game.@string[ids].flag) continue;
            //    }
            //    while (@string.neighbor[neighbor] != id)
            //    {
            //       var neighbor = @string.neighbor[neighbor];
            //    }
            //    if (game.@string[id].libs == 1 &&)
            //}
        }
        public static bool ReplaceMove(Board game, short pos, Stone color, int[] replace, ref int replace_num)
        {
            var string_id = game.string_id;
            String_t[] @string = game.@string;
            Stone other = FLIP_COLOR(color);
            bool has_lib_1Str = false;
            GetNeighbor4(out short[] nblib1_4, pos);
            for (int i = 0; i < nblib1_4.Length; i++)
            {
                has_lib_1Str = has_lib_1Str || @string[string_id[nblib1_4[i]]].libs == 1;
            }
            if (eye[Pat3(game.pat, pos)] != color || has_lib_1Str)
            {
                bool has_lib_m1Str = true;
                for (int i = 0; i < nblib1_4.Length; i++)
                {
                    has_lib_m1Str = has_lib_1Str && @string[string_id[nblib1_4[i]]].libs > 1;
                }
                if (falsy_eye[Pat3(game.pat, pos)] == color && has_lib_m1Str)
                {
                    short[] check = { NORTH_WEST(pos), NORTH_EAST(pos), SOUTH_WEST(pos), SOUTH_EAST(pos) };
                    foreach (var p in check)
                    {
                        if (game.board[p] != other)
                            continue;
                        String_t s = @string[string_id[p]];
                        if (s.libs == 1)
                        {
                            var lib = s.lib[0];
                            replace[replace_num++] = lib;
                        }
                        else if (s.libs <= 2)
                        {
                            var lib = s.lib[0];
                            while (lib != LIBERTY_END)
                            {
                                if (IsCapturableAtariForSimulation(game, lib, color, string_id[p]))
                                {
                                    replace[replace_num++] = lib;
                                }
                                lib = s.lib[lib];
                            }
                        }
                    }
                    return true;
                }
            }
            return false;
        }
        private static void AddStone(Board game, short pos, Stone color, short id)
        {
            String_t[] @string = game.@string;
            String_t add_str;
            var board = game.board;
            var string_id = game.string_id;
            short lib_add = 0;
            Stone other = FLIP_COLOR(color);
            short neighbor, i;
            short[] neighbor4;
            string_id[pos] = id;
            add_str = @string[id];
            AddStoneToString(game, add_str, pos, 0);
            GetNeighbor4(out neighbor4, pos);
            for (i = 0; i < neighbor4.Length; i++)
            {
                if (board[neighbor4[i]] == S_EMPTY)
                {
                    lib_add = AddLiberty(add_str, neighbor4[i], lib_add);
                }
                else if (board[neighbor4[i]] == other)
                {
                    neighbor = string_id[neighbor4[i]];
                    AddNeighbor(@string[neighbor], id, 0);
                    AddNeighbor(@string[id], neighbor, 0);
                }
            }
        }
        private static void AddStoneToString(Board game, String_t @string, short pos, short head)
        {
            var string_next = game.string_next;
            int str_pos;
            if (pos == STRING_END) return;
            if (@string.origin > pos)
            {
                string_next[pos] = @string.origin;
                @string.origin = pos;
            }
            else
            {
                if (head != 0)
                {
                    str_pos = head;
                }
                else
                {
                    str_pos = @string.origin;
                }
                while (string_next[str_pos] < pos)
                {
                    if (str_pos == string_next[str_pos])
                    {
                        Console.Error.WriteLine("Illegal @string");
                        Environment.Exit(-1);
                    }
                    str_pos = string_next[str_pos];
                }
                string_next[pos] = string_next[str_pos];
                string_next[str_pos] = pos;
            }
            @string.size++;
        }
        private static void CheckBentFourInTheCorner(Board game)
        {
            var board = game.board;
            var @string = game.@string;
            var string_id = game.string_id;
            var string_next = game.string_next;
            int pos;
            int i;
            int id;
            int neighbor;
            Stone color;
            int lib1, lib2;
            int neighbor_lib1, neighbor_lib2;
            for (i = 0; i < 4; i++)
            {
                id = string_id[corner[i]];
                if (@string[id].size == 3 && @string[id].libs == 2 && @string[id].neighbors == 1)
                {
                    color = @string[id].color;
                    lib1 = @string[id].lib[0];
                    lib2 = @string[id].lib[lib1];
                    if ((board[corner_neighbor[i, 0]] == S_EMPTY || board[corner_neighbor[i, 0]] == color) && (board[corner_neighbor[i, 1]] == S_EMPTY || board[corner_neighbor[i, 1]] == color))
                    {
                        neighbor = @string[id].neighbor[0];
                        if (@string[neighbor].libs == 2 && @string[neighbor].size > 6)
                        {
                            neighbor_lib1 = @string[neighbor].lib[0];
                            neighbor_lib2 = @string[neighbor].lib[neighbor_lib1];
                            if ((neighbor_lib1 == lib1 && neighbor_lib2 == lib2) || (neighbor_lib1 == lib2 && neighbor_lib2 == lib1))
                            {
                                pos = @string[neighbor].origin;
                                while (pos != STRING_END)
                                {
                                    board[pos] = color;
                                    pos = string_next[pos];
                                }
                                pos = @string[neighbor].lib[0];
                                board[pos] = color;
                                pos = @string[neighbor].lib[pos];
                                board[pos] = color;
                            }
                        }
                    }
                }
            }
        }
        private static void ConnectString(Board game, short pos, Stone color, int connection, short[] id)
        {
            short min = id[0];
            String_t[] @string = game.@string;
            String_t[] str = new String_t[3];
            int connections = 0;
            bool flag = true;
            for (int i = 1; i < connection; i++)
            {
                flag = true;
                for (int j = 0; j < i; j++)
                {
                    if (id[j] == id[i])
                    {
                        flag = false;
                        break;
                    }
                }
                if (flag)
                {
                    if (min > id[i])
                    {
                        str[connections] = @string[min];
                        min = id[i];
                    }
                    else
                    {
                        str[connections] = @string[id[i]];
                    }
                    connections++;
                }
            }
            AddStone(game, pos, color, min);
            if (connections > 0)
            {
                MergeString(game, game.@string[min], str, connections);
            }
        }
        private static bool IsFalseEyeConnection(Board game, short pos, Stone color)
        {
            var @string = game.@string;
            var string_id = game.string_id;
            var board = game.board;
            var @checked_string = new short[4];
            var string_liberties = new short[4];
            int strings = 0;
            short id, lib, libs = 0, lib_sum = 0;
            var liberty = new int[STRING_LIB_MAX];
            short count;
            bool @checked;
            short[] neighbor4;
            int neighbor;
            bool already_checked;
            Stone other = FLIP_COLOR(color);
            var player_id = new short[4];
            short player_ids = 0;
            GetNeighbor4(out neighbor4, pos);
            for (int i = 0; i < neighbor4.Length; i++)
            {
                @checked = false;
                for (int j = 0; j < player_ids; j++)
                {
                    if (player_id[j] == string_id[neighbor4[i]])
                    {
                        @checked = true;
                    }
                }
                if (!@checked)
                {
                    player_id[player_ids++] = string_id[neighbor4[i]];
                }
            }
            for (int i = 0; i < 4; i++)
            {
                if (board[pos + cross[i]] == other)
                {
                    id = string_id[pos + cross[i]];
                    if (IsAlreadyCaptured(game, other, id, player_id, player_ids))
                    {
                        return false;
                    }
                }
            }
            for (int i = 0; i < neighbor4.Length; i++)
            {
                if (board[neighbor4[i]] == color)
                {
                    id = string_id[neighbor4[i]];
                    if (@string[id].libs == 2)
                    {
                        lib = @string[id].lib[0];
                        if (lib == pos) lib = @string[id].lib[lib];
                        if (IsSelfAtari(game, color, lib)) return true;
                    }
                    already_checked = false;
                    for (int j = 0; j < strings; j++)
                    {
                        if (@checked_string[j] == id)
                        {
                            already_checked = true;
                            break;
                        }
                    }
                    if (already_checked) continue;
                    lib = @string[id].lib[0];
                    count = 0;
                    while (lib != LIBERTY_END)
                    {
                        if (lib != pos)
                        {
                            @checked = false;
                            for (i = 0; i < libs; i++)
                            {
                                if (liberty[i] == lib)
                                {
                                    @checked = true;
                                    break;
                                }
                            }
                            if (!@checked)
                            {
                                liberty[libs + count] = lib;
                                count++;
                            }
                        }
                        lib = @string[id].lib[lib];
                    }
                    libs += count;
                    string_liberties[strings] = @string[id].libs;
                    @checked_string[strings++] = id;
                }
            }
            for (int i = 0; i < strings; i++)
            {
                lib_sum += (short)(string_liberties[i] - 1);
            }
            neighbor = @string[@checked_string[0]].neighbor[0];
            while (neighbor != NEIGHBOR_END)
            {
                if (@string[neighbor].libs == 1 &&
                    @string[@checked_string[1]].neighbor[neighbor] != 0)
                {
                    return false;
                }
                neighbor = @string[@checked_string[0]].neighbor[neighbor];
            }
            if (strings == 1)
            {
                return false;
            }
            if (libs == lib_sum)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static bool IsSuicide(Board game, String_t[] @string, Stone color, short pos)
        {
            //Stone[] board = game.board;
            var board = game.board;
            var string_id = game.string_id;
            Stone other = FLIP_COLOR(color);
            short[] neighbor4;
            int i;
            GetNeighbor4(out neighbor4, pos);
            for (i = 0; i < neighbor4.Length; i++)
            {
                if (board[neighbor4[i]] == other && @string[string_id[neighbor4[i]]].libs == 1)
                {
                    return false;
                }
                else if (board[neighbor4[i]] == color && @string[string_id[neighbor4[i]]].libs > 1)
                {
                    return false;
                }
            }
            return true;
        }
        private static void MakeString(Board game, short pos, Stone color)
        {
            String_t[] @string = game.@string;
            String_t new_string;
            var board = game.board;
            var string_id = game.string_id;
            short id = 1;
            short lib_add = 0;
            Stone other = FLIP_COLOR(color);
            short neighbor, i;
            short[] neighbor4;
            while (@string[id].flag) { id++; }
            new_string = game.@string[id];
            Array.Clear(new_string.lib, 0, new_string.lib.Length);
            Array.Clear(new_string.neighbor, 0, new_string.neighbor.Length);
            new_string.lib[0] = LIBERTY_END;
            new_string.neighbor[0] = NEIGHBOR_END;
            new_string.libs = 0;
            new_string.color = color;
            new_string.origin = pos;
            new_string.size = 1;
            new_string.neighbors = 0;
            game.string_id[pos] = id;
            game.string_next[pos] = STRING_END;
            GetNeighbor4(out neighbor4, pos);
            for (i = 0; i < neighbor4.Length; i++)
            {
                if (board[neighbor4[i]] == S_EMPTY)
                {
                    lib_add = AddLiberty(new_string, neighbor4[i], lib_add);
                }
                else if (board[neighbor4[i]] == other)
                {
                    neighbor = string_id[neighbor4[i]];
                    AddNeighbor(@string[neighbor], id, 0);
                    AddNeighbor(@string[id], neighbor, 0);
                }
            }
            new_string.flag = true;
        }
        private static void MergeString(Board game, String_t dst, String_t[] src, int n)
        {
            short tmp, pos, prev, neighbor;
            var string_next = game.string_next;
            var string_id = game.string_id;
            short id = string_id[dst.origin], rm_id;
            String_t[] @string = game.@string;
            for (int i = 0; i < n; i++)
            {
                rm_id = string_id[src[i].origin];
                prev = 0;
                pos = src[i].lib[0];
                while (pos != LIBERTY_END)
                {
                    prev = AddLiberty(dst, pos, prev);
                    pos = src[i].lib[pos];
                }
                prev = 0;
                pos = src[i].origin;
                while (pos != STRING_END)
                {
                    string_id[pos] = id;
                    tmp = string_next[pos];
                    AddStoneToString(game, dst, pos, prev);
                    prev = pos;
                    pos = tmp;
                }
                prev = 0;
                neighbor = src[i].neighbor[0];
                while (neighbor != NEIGHBOR_END)
                {
                    RemoveNeighborString(@string[neighbor], rm_id);
                    AddNeighbor(dst, neighbor, prev);
                    AddNeighbor(@string[neighbor], id, prev);
                    prev = neighbor;
                    neighbor = src[i].neighbor[neighbor];
                }
                src[i].flag = false;
            }
        }
        private static void RemoveLiberty(Board game, String_t @string, int pos)
        {
            int lib = 0;
            if (@string.lib[pos] == 0) return;
            while (@string.lib[lib] != pos)
            {
                lib = @string.lib[lib];
            }
            @string.lib[lib] = @string.lib[@string.lib[lib]];
            @string.lib[pos] = 0;
            @string.libs--;
            if (@string.libs == 1)
            {
                game.candidates[onboard_pos_2pure[@string.lib[0]]] = true;
            }
        }
        private static void RemoveNeighborString(String_t @string, int id)
        {
            int neighbor = 0;
            if (@string.neighbor[id] == 0) return;
            while (@string.neighbor[neighbor] != id)
            {
                neighbor = @string.neighbor[neighbor];
            }
            @string.neighbor[neighbor] = @string.neighbor[@string.neighbor[neighbor]];
            @string.neighbor[id] = 0;
            @string.neighbors--;
        }
        private static short RemoveString(Board game, String_t @string)
        {
            var str = game.@string;
            var string_next = game.string_next;
            var string_id = game.string_id;
            short pos = @string.origin;
            short next;
            var board = game.board;
            var candidates = game.candidates;
            int neighbor, rm_id = string_id[@string.origin];
            var removed_color = board[pos];
            var poisons = game.capture_pos;
            do
            {
                poisons[(int)FLIP_COLOR(board[pos]) - 1, onboard_pos_2pure[pos]]++;
                board[pos] = S_EMPTY;
                candidates[onboard_pos_2pure[pos]] = true;
                UpdatePatternEmpty(game.pat, pos);
                game.current_hash ^= hash_bit[pos, (int)removed_color];
                game.positional_hash ^= hash_bit[pos, (int)removed_color];
                if (board[NORTH(pos)] != S_OB && str[string_id[NORTH(pos)]].flag) AddLiberty(str[string_id[NORTH(pos)]], pos, 0);
                if (board[WEST(pos)] != S_OB && str[string_id[WEST(pos)]].flag) AddLiberty(str[string_id[WEST(pos)]], pos, 0);
                if (board[EAST(pos)] != S_OB && str[string_id[EAST(pos)]].flag) AddLiberty(str[string_id[EAST(pos)]], pos, 0);
                if (board[SOUTH(pos)] != S_OB && str[string_id[SOUTH(pos)]].flag) AddLiberty(str[string_id[SOUTH(pos)]], pos, 0);
                next = string_next[pos];
                string_next[pos] = 0;
                string_id[pos] = 0;
                pos = next;
            } while (pos != STRING_END);
            neighbor = @string.neighbor[0];
            while (neighbor != NEIGHBOR_END)
            {
                RemoveNeighborString(str[neighbor], rm_id);
                neighbor = @string.neighbor[neighbor];
            }
            @string.flag = false;
            return @string.size;
        }
        #region 公共变量
        private static Board capturable_game = new Board();
        private static Board capture_game = new Board();
        private static Board liberty_game = new Board();
        private static Board oiotoshi_game = new Board();
        #endregion
        #region 棋盘设置
        /// <summary>
        /// 静态参数初始化
        /// </summary>
        static Board()
        {
            BoardInfo = $"Go BoardSize {PURE_BOARD_SIZE}";
            #region 初始化空白棋盘模板
            InitializeHash();
            InitializeConst();
            SetKomi(KOMI);
            SetSuperKo(true);
            EmptyBoard = new Board();
            short i, x, y, pos;
            EmptyBoard.record.Clear();
            for (int index = 0; index < EmptyBoard.pat.Length; index++)
                EmptyBoard.pat[index].Reset();
            for (int index = 0; index < EmptyBoard.@string.Length; index++)
                EmptyBoard.@string[index].Reset();
            Array.Clear(EmptyBoard.board, 0, EmptyBoard.board.Length);
            Array.Clear(EmptyBoard.candidates, 0, EmptyBoard.candidates.Length);
            Array.Clear(EmptyBoard.prisoner, 0, EmptyBoard.prisoner.Length);
            Array.Clear(EmptyBoard.seki, 0, EmptyBoard.seki.Length);
            Array.Clear(EmptyBoard.string_id, 0, EmptyBoard.string_id.Length);
            Array.Clear(EmptyBoard.string_next, 0, EmptyBoard.string_next.Length);
            for (int index = 0; index < (int)S_OB - 1; index++)
                for (int j = 0; j < PURE_BOARD_MAX; j++)
                    EmptyBoard.capture_pos[index, j] = 0;
            EmptyBoard.preHashCodes.Clear();
            EmptyBoard.current_hash = 0;
            EmptyBoard.ko_move = 0;
            EmptyBoard.ko_pos = 0;
            EmptyBoard.positional_hash = 0;
            EmptyBoard.previous1_hash = 0;
            EmptyBoard.previous2_hash = 0;
            for (y = 0; y < board_size; y++)
            {
                for (x = 0; x < OB_SIZE; x++)
                {
                    EmptyBoard.board[POS(board_size - 1 - x, y)] = S_OB;
                    EmptyBoard.board[POS(x, y)] = S_OB;
                    EmptyBoard.board[POS(y, board_size - 1 - x)] = S_OB;
                    EmptyBoard.board[POS(y, x)] = S_OB;
                }
            }
            for (y = board_start; y <= board_end; y++)
            {
                for (x = board_start; x <= board_end; x++)
                {
                    pos = POS(x, y);
                    EmptyBoard.candidates[onboard_pos_2pure[pos]] = true;
                }
            }
            for (i = 0; i < EmptyBoard.@string.Length; i++)
                EmptyBoard.@string[i].Reset();
            ClearPattern(EmptyBoard.pat);
            #endregion
        }
        public static sbyte Convert2Num(Stone stone)
        {
            switch (stone)
            {
                case S_BLACK:
                    return 1;
                case S_WHITE:
                    return -1;
                default:
                    return 0;
            }
        }
        private static sbyte[] AreaMap(Stone[] data)
        {
            short height = pure_board_size;
            short width = height;
            sbyte[] map = new sbyte[width * height];
            for (short pos = 0; pos < pure_board_max; pos++)
            {
                if (map[pos] != 0) continue;
                if (data[onboard_pos_2full[pos]] != 0)
                {
                    map[pos] = Convert2Num(data[onboard_pos_2full[pos]]);
                    continue;
                }
                var chain = GetChain(data, onboard_pos_2full[pos]);
                sbyte sign = 0;
                sbyte indicator = 1;
                foreach (var c in chain)
                {
                    if (indicator == 0) break;
                    GetNeighbor4(out short[] neighbors, c);
                    foreach (var n in neighbors)
                    {
                        if (data[n] == S_EMPTY) continue;
                        var temp_sign = Convert2Num(data[n]);
                        if (sign == 0)
                        {
                            sign = temp_sign;
                        }
                        else if (sign != temp_sign)
                        {
                            indicator = 0;
                            break;
                        }
                    }
                }
                foreach (var c in chain)
                {
                    map[onboard_pos_2pure[c]] = (sbyte)(sign * indicator);
                }
            }
            return map;
        }
        private static List<short> GetChain(Stone[] data, short v, List<short> result = null, Dictionary<short, bool> done = null, Stone sign = S_EMPTY)
        {
            if (sign == S_EMPTY) sign = data[v];
            GetNeighbor4(out short[] neighbors, v);
            if (result == null) result = new List<short>();
            result.Add(v);
            if (done == null) done = new Dictionary<short, bool>();
            if (!done.ContainsKey(v)) done.Add(v, true);
            foreach (var n_pos in neighbors)
            {
                if (data[n_pos] != sign || (done.ContainsKey(n_pos) && done[n_pos]))
                    continue;
                GetChain(data, n_pos, result, done, sign);
            }
            return result;
        }
        private static List<short> GetChain(sbyte[] data, short v, List<short> result = null, Dictionary<short, bool> done = null, sbyte sign = 0)
        {
            if (sign == 0) sign = data[v];
            GetNeighbor4P(out short[] neighbors, v);
            if (result == null) result = new List<short>();
            result.Add(v);
            if (done == null) done = new Dictionary<short, bool>();
            if (!done.ContainsKey(v)) done.Add(v, true);
            foreach (var n_pos in neighbors)
            {
                if (data[n_pos] != sign || (done.ContainsKey(n_pos) && done[n_pos]))
                    continue;
                GetChain(data, n_pos, result, done, sign);
            }
            return result;
        }
        #endregion
    }
}
