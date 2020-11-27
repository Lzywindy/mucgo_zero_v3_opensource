using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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
    public enum Stone : byte
    {
        S_EMPTY,
        S_BLACK,
        S_WHITE,
        S_OB,
        S_MAX
    };
    public enum Eye_condition : byte
    {
        E_NOT_EYE,
        E_COMPLETE_HALF_EYE,
        E_HALF_3_EYE,
        E_HALF_2_EYE,
        E_HALF_1_EYE,
        E_COMPLETE_ONE_EYE,
        E_MAX,
    };
    public enum MD : byte
    {
        MD_2,
        MD_3,
        MD_4,
        MD_MAX
    };
    public enum LARGE_MD : byte
    {
        MD_5,
        MD_LARGE_MAX
    };
    public enum KOMI_MODE : byte
    {
        DK_OFF,
        DK_LINEAR,
        DK_VALUE,
    };
    public enum LIBERTY_STATE : byte
    {
        L_DECREASE,
        L_EVEN,
        L_INCREASE,
    };
    public enum SEARCH_MODE : byte
    {
        CONST_PLAYOUT_MODE = 0x1,
        CONST_TIME_MODE = 0x2,
        CONST_PLAYOUT_TIME_MODE = 0x3
    };
    public enum HashInfo : byte
    {
        HASH_PASS,
        HASH_BLACK,
        HASH_WHITE,
        HASH_KO,
    };
    public class move_t
    {
        public Stone color;
        public int pos;
        public ulong hash;
        public void Reset()
        {
            color = S_EMPTY;
            pos = 0;
            hash = 0;
        }
        public static void DeepCopy(ref move_t dst, ref move_t src)
        {
            if (src == null) return;
            if (dst == null) dst = new move_t();
            dst.color = src.color;
            dst.pos = src.pos;
            dst.hash = src.hash;
        }
    }
    public class string_t
    {
        public Stone color;
        public int libs;
        public readonly short[] lib = new short[STRING_LIB_MAX];
        public int neighbors;
        public readonly short[] neighbor = new short[MAX_NEIGHBOR];
        public int origin;
        public int size;
        public bool flag;
        public void Reset()
        {
            color = S_EMPTY;
            libs = 0;
            neighbors = 0;
            origin = 0;
            size = 0;
            flag = default(bool);
            for (int i = 0; i < lib.Length; i++)
                lib[i] = 0;
            for (int i = 0; i < neighbor.Length; i++)
                neighbor[i] = 0;
        }
        public static void DeepCopy(ref string_t dst, ref string_t src)
        {
            if (src == null) return;
            if (dst == null) dst = new string_t();
            dst.color = src.color;
            dst.libs = src.libs;
            dst.neighbors = src.neighbors;
            dst.origin = src.origin;
            dst.size = src.size;
            dst.flag = src.flag;
            for (int i = 0; i < STRING_LIB_MAX; i++)
                dst.lib[i] = src.lib[i];
            for (int i = 0; i < MAX_NEIGHBOR; i++)
                dst.neighbor[i] = src.neighbor[i];
        }
    }
    public class pattern_t
    {
        public readonly uint[] list = new uint[(int)MD_MAX];
        public readonly ulong[] large_list = new ulong[(int)MD_LARGE_MAX];
        public static void DeepCopy(ref pattern_t dst, ref pattern_t src)
        {
            if (src == null) return;
            if (dst == null) dst = new pattern_t();
            for (int i = 0; i < (int)MD_MAX; i++)
                dst.list[i] = src.list[i];
            for (int i = 0; i < (int)MD_LARGE_MAX; i++)
                dst.large_list[i] = src.large_list[i];
        }
        public void Reset()
        {
            for (int i = 0; i < list.Length; i++)
                list[i] = 0;
            for (int i = 0; i < large_list.Length; i++)
                large_list[i] = 0;
        }
    }
    public class pattern_hash_t
    {
        public readonly ulong[] list = new ulong[(int)MD_MAX + (int)MD_LARGE_MAX];
        public static void DeepCopy(ref pattern_hash_t dst, ref pattern_hash_t src)
        {
            if (src == null) return;
            if (dst == null) dst = new pattern_hash_t();
            for (int i = 0; i < (int)MD_MAX + (int)MD_LARGE_MAX; i++)
                dst.list[i] = src.list[i];
        }
        public void Reset()
        {
            for (int i = 0; i < list.Length; i++)
                list[i] = 0;
        }
    }
    public class statistic_t
    {
        public readonly Stone[] colors = new Stone[3];
        public Stone this[int index]
        {
            get { if (index >= 0 || index < 3) lock (this) { return colors[index]; } return 0; }
            set { if (index >= 0 || index < 3) lock (this) { colors[index] = value; } }
        }
        public statistic_t()
        {
            Reset();
        }
        public static void DeepCopy(ref statistic_t dst, ref statistic_t src)
        {
            if (src == null) return;
            if (dst == null) dst = new statistic_t();
            for (int i = 0; i < 3; i++)
                dst[i] = src[i];
        }
        public void Reset()
        {
            for (int i = 0; i < 3; i++)
                this[i] = 0;
        }
    }
    //public class Board_Stone
    //{
    //    private int puresize = 19;
    //    private int fullsize { get { return OB_SIZE + puresize + OB_SIZE; } }
    //    private readonly Stone[] board = new Stone[PURE_BOARD_MAX];
    //    public int Size { get { return puresize; } set { puresize = Min(Max(value, 9), 19); } }
    //    public Stone this[int board_pos, bool pure = false]
    //    {
    //        get { return GetInfo(board_pos % fullsize, board_pos / fullsize, pure); }
    //        set { SetInfo(board_pos % fullsize, board_pos / fullsize, value, pure); }
    //    }
    //    public Stone this[int board_pos_x, int board_pos_y, bool pure = false]
    //    {
    //        get { return GetInfo(board_pos_x, board_pos_y, pure); }
    //        set { SetInfo(board_pos_x, board_pos_y, value, pure); }
    //    }
    //    private int Index2Pos(int x, int y, bool Pure = false)
    //    {
    //        if (Pure)
    //            return ((x) + (y) * puresize);
    //        else
    //            return ((x - OB_SIZE) + (y - OB_SIZE) * puresize);
    //    }
    //    private bool InRange(int pos)
    //    {
    //        return pos >= 0 && pos < puresize * puresize;
    //    }
    //    private Stone GetInfo(int x, int y, bool Pure = false)
    //    {
    //        var purepos = Index2Pos(x, y, Pure);
    //        var inrange = InRange(purepos);
    //        if (InRange(purepos)) return board[purepos];
    //        return S_OB;
    //    }
    //    private void SetInfo(int x, int y, Stone stone, bool Pure = false)
    //    {
    //        var purepos = Index2Pos(x, y, Pure);
    //        var inrange = InRange(purepos);
    //        if (InRange(purepos)) board[purepos] = stone;
    //    }
    //    public static void DeepCopy(Board_Stone dis, Board_Stone src)
    //    {
    //        Array.Copy(src.board, dis.board, PURE_BOARD_MAX);
    //        dis.puresize = src.puresize;
    //    }
    //    public void Reset()
    //    {
    //        for (int i = 0; i < board.Length; i++)
    //            board[i] = S_EMPTY;
    //        puresize = 19;
    //    }
    //}
    public class game_info_t
    {
        public readonly move_t[] record = new move_t[MAX_RECORDS];
        public readonly int[] prisoner = new int[(int)S_MAX];
        public readonly Stone[] board = new Stone[BOARD_MAX];
        //public readonly Board_Stone board = new Board_Stone();
        public readonly pattern_t[] pat = new pattern_t[BOARD_MAX];
        public readonly string_t[] @string = new string_t[MAX_STRING];
        public readonly int[] string_id = new int[STRING_POS_MAX];
        public readonly int[] string_next = new int[STRING_POS_MAX];
        public readonly bool[] candidates = new bool[BOARD_MAX];
        public readonly int[] capture_num = new int[(int)S_OB];
        public readonly int[,] capture_pos = new int[(int)S_OB, PURE_BOARD_MAX];
        public readonly bool[] seki = new bool[BOARD_MAX];
        public int moves;
        public int ko_pos;
        public int ko_move;
        public ulong current_hash;
        public ulong previous1_hash;
        public ulong previous2_hash;
        public ulong positional_hash;
        public int pass_count;
        public float Influence;
        public game_info_t()
        {
            if (record == null) record = new move_t[MAX_RECORDS];
            if (prisoner == null) prisoner = new int[(int)S_MAX];
            if (board == null) board = new Stone[BOARD_MAX];
            //if (board == null) board = new Board_Stone();
            if (pat == null) pat = new pattern_t[BOARD_MAX];
            if (@string == null) @string = new string_t[MAX_STRING];
            if (string_id == null) string_id = new int[STRING_POS_MAX];
            if (string_next == null) string_next = new int[STRING_POS_MAX];
            if (candidates == null) candidates = new bool[BOARD_MAX];
            if (capture_num == null) capture_num = new int[(int)S_OB];
            if (capture_pos == null) capture_pos = new int[(int)S_OB, PURE_BOARD_MAX];
            if (seki == null) seki = new bool[BOARD_MAX];
            for (int i = 0; i < record.Length; i++)
                record[i] = new move_t();
            for (int i = 0; i < pat.Length; i++)
                pat[i] = new pattern_t();
            for (int i = 0; i < @string.Length; i++)
                @string[i] = new string_t();
            moves = 1;
            ko_pos = 0;
            ko_move = 0;
            current_hash = 0;
            previous1_hash = 0;
            previous2_hash = 0;
            positional_hash = 0;
            pass_count = 0;
            Influence = 0;
        }
        /// <summary>
        /// 深度拷贝
        /// </summary>
        /// <param name="dis">目标位置</param>
        /// <param name="src">源位置</param>
        public static void DeepCopy(ref game_info_t dis, ref game_info_t src)
        {
            if (src == null) return;
            if (dis == null) dis = new game_info_t();
            for (int i = 0; i < dis.record.Length; i++)
                move_t.DeepCopy(ref dis.record[i], ref src.record[i]);
            for (int i = 0; i < dis.pat.Length; i++)
                pattern_t.DeepCopy(ref dis.pat[i], ref src.pat[i]);
            for (int i = 0; i < dis.@string.Length; i++)
                string_t.DeepCopy(ref dis.@string[i], ref src.@string[i]);
            Array.Copy(src.prisoner, dis.prisoner, (int)S_MAX);
            Array.Copy(src.board, dis.board, BOARD_MAX);
            //Board_Stone.DeepCopy(dis.board, src.board);
            Array.Copy(src.seki, dis.seki, BOARD_MAX);
            Array.Copy(src.candidates, dis.candidates, BOARD_MAX);
            Array.Copy(src.string_id, dis.string_id, STRING_POS_MAX);
            Array.Copy(src.string_next, dis.string_next, STRING_POS_MAX);
            Array.Copy(src.capture_num, dis.capture_num, (int)S_OB);
            for (int i = 0; i < (int)S_OB; i++)
                for (int j = 0; j < PURE_BOARD_MAX; j++)
                    dis.capture_pos[i, j] = src.capture_pos[i, j];
            dis.moves = src.moves;
            dis.ko_pos = src.ko_pos;
            dis.ko_move = src.ko_move;
            dis.current_hash = src.current_hash;
            dis.previous1_hash = src.previous1_hash;
            dis.previous2_hash = src.previous2_hash;
            dis.positional_hash = src.positional_hash;
            dis.pass_count = src.pass_count;
            dis.Influence = src.Influence;
        }
        public void Reset()
        {
            for (int i = 0; i < record.Length; i++)
                record[i].Reset();
            for (int i = 0; i < pat.Length; i++)
                pat[i].Reset();
            for (int i = 0; i < @string.Length; i++)
                @string[i].Reset();
            Array.Clear(prisoner, 0, prisoner.Length);
            Array.Clear(board, 0, board.Length);
            //board.Reset();
            Array.Clear(seki, 0, seki.Length);
            Array.Clear(candidates, 0, candidates.Length);
            Array.Clear(string_id, 0, string_id.Length);
            Array.Clear(string_next, 0, string_next.Length);
            Array.Clear(capture_num, 0, capture_num.Length);
            for (int i = 0; i < (int)S_OB; i++)
                for (int j = 0; j < PURE_BOARD_MAX; j++)
                    capture_pos[i, j] = 0;
            moves = 1;
            ko_pos = 0;
            ko_move = 0;
            current_hash = 0;
            previous1_hash = 0;
            previous2_hash = 0;
            positional_hash = 0;
            pass_count = 0;
            Influence = 0;
        }
        /// <summary>
        /// 计算控制力(气数量/子数量来代表棋串的活力)
        /// </summary>
        public void CalcCtrl()
        {
            Influence = 0;
            for (int i = 0; i < MAX_STRING; i++)
                Influence += (@string[i].color == S_BLACK ? 1.0f : @string[i].color == S_WHITE ? -1.0f : 0.0f) * (@string[i].libs / (@string[i].size + 1.0f)) * (0.98f * @string[i].libs + 0.02f * @string[i].size);
        }
    }
    static class Board
    {
        private static int HASH_VMIRROR = 1;
        private static int HASH_HMIRROR = 2;
        private static int HASH_XYFLIP = 4;
        private static float default_komi;
        private static readonly Stone[] false_eye = new Stone[PAT3_MAX];
        private static readonly Stone[] falsy_eye = new Stone[PAT3_MAX];
        private static readonly int[] corner = new int[4];
        private static readonly int[,] corner_neighbor = new int[4, 2];
        private static readonly int[] cross = new int[4];
        private static bool check_superko;
        private static readonly Dictionary<int, int> Full2PureBoardPos = new Dictionary<int, int>();
        private static readonly Dictionary<int, int> Pure2FullBoardPos = new Dictionary<int, int>();
        public static int pure_board_size = PURE_BOARD_SIZE;
        public static int pure_board_max = PURE_BOARD_MAX;
        public static int board_size = BOARD_SIZE;
        public static int board_max = BOARD_MAX;
        public static int board_start = BOARD_START;
        public static int board_end = BOARD_END;
        public static int first_move_candidates;
        public static float KomiSetup;
        public readonly static float[] komi = new float[(int)S_OB];
        public readonly static float[] dynamic_komi = new float[(int)S_OB];
        public readonly static int[] board_pos_id = new int[BOARD_MAX];
        public readonly static int[] board_x = new int[BOARD_MAX];
        public readonly static int[] board_y = new int[BOARD_MAX];
        public readonly static Stone[] eye = new Stone[PAT3_MAX];
        public readonly static Stone[] territory = new Stone[PAT3_MAX];
        public readonly static Stone[] nb4_empty = new Stone[PAT3_MAX];
        public readonly static Eye_condition[] eye_condition = new Eye_condition[PAT3_MAX];
        public readonly static int[] border_dis_x = new int[BOARD_MAX];
        public readonly static int[] border_dis_y = new int[BOARD_MAX];
        public readonly static int[,] move_dis = new int[PURE_BOARD_SIZE, PURE_BOARD_SIZE];
        public readonly static int[] onboard_pos = new int[PURE_BOARD_MAX];
        public readonly static int[] first_move_candidate = new int[PURE_BOARD_MAX];
        public static Stone FLIP_COLOR(Stone col) { return col ^ S_OB; }
        public static int CORRECT_X(int pos) { return ((pos) % board_size - OB_SIZE + 1); }
        public static int CORRECT_Y(int pos) { return ((pos) / board_size - OB_SIZE + 1); }
        public static int DIS(int pos1, int pos2) { return (move_dis[DX(pos1, pos2), DY(pos1, pos2)]); }
        public static int DX(int pos1, int pos2) { return (Abs(board_x[(pos1)] - board_x[(pos2)])); }
        public static int DY(int pos1, int pos2) { return (Abs(board_y[(pos1)] - board_y[(pos2)])); }
        public static int EAST(int pos) { return ((pos) + 1); }
        public static int NORTH(int pos) { return ((pos) - board_size); }
        public static int NORTH_EAST(int pos) { return ((pos) - board_size + 1); }
        public static int NORTH_WEST(int pos) { return ((pos) - board_size - 1); }
        public static int POS(int x, int y) { return ((x) + (y) * board_size); }
        public static int SOUTH(int pos) { return ((pos) + board_size); }
        public static int SOUTH_EAST(int pos) { return ((pos) + board_size + 1); }
        public static int SOUTH_WEST(int pos) { return ((pos) + board_size - 1); }
        public static int WEST(int pos) { return ((pos) - 1); }
        public static int X(int pos) { return ((pos) % board_size); }
        public static int Y(int pos) { return ((pos) / board_size); }
        public static int GetPureBoardPos(int f_pos)
        {
            lock (Full2PureBoardPos)
            {
                /*返回纯棋盘上的坐标*/
                if (Full2PureBoardPos.ContainsKey(f_pos))
                    return Full2PureBoardPos[f_pos];
                /*否则返回界外标志*/
                return -1;
            }
        }
        public static int GetFullBoardPos(int p_pos)
        {
            lock (Pure2FullBoardPos)
            {
                /*返回纯棋盘上的坐标*/
                if (Pure2FullBoardPos.ContainsKey(p_pos))
                    return Pure2FullBoardPos[p_pos];
                /*否则返回界外标志*/
                return -1;
            }
        }
        public static void fill_n<T>(T[] datas, T v)
        {
            for (int i = 0; i < datas.Length; i++)
                datas[i] = v;
        }
        public static game_info_t AllocateGame()
        {
            return new game_info_t();
        }
        public static bool IsNeighbor(int pos0, int pos1)
        {
            int index_distance = pos0 - pos1;
            return index_distance == 1
                || index_distance == -1
                || index_distance == board_size
                || index_distance == -board_size;
        }
        public static void GetNeighbor4(int[] neighbor4, int pos)
        {
            if (neighbor4 == null || neighbor4.Length != 4) return;
            neighbor4[0] = NORTH(pos);
            neighbor4[1] = WEST(pos);
            neighbor4[2] = EAST(pos);
            neighbor4[3] = SOUTH(pos);
        }
        public static void SetSuperKo(bool flag)
        {
            check_superko = flag;
        }
        public static void SetBoardSize(int size)
        {
            int i, x, y;
            pure_board_size = size;
            pure_board_max = size * size;
            board_size = size + 2 * OB_SIZE;
            board_max = board_size * board_size;
            board_start = OB_SIZE;
            board_end = (pure_board_size + OB_SIZE - 1);
            i = 0;
            for (y = board_start; y <= board_end; y++)
            {
                for (x = board_start; x <= board_end; x++)
                {
                    onboard_pos[i++] = POS(x, y);
                    board_x[POS(x, y)] = x;
                    board_y[POS(x, y)] = y;
                }
            }
            for (y = board_start; y <= board_end; y++)
            {
                for (x = board_start; x <= (board_start + pure_board_size / 2); x++)
                {
                    border_dis_x[POS(x, y)] = x - (OB_SIZE - 1);
                    border_dis_x[POS(board_end + OB_SIZE - x, y)] = x - (OB_SIZE - 1);
                    border_dis_y[POS(y, x)] = x - (OB_SIZE - 1);
                    border_dis_y[POS(y, board_end + OB_SIZE - x)] = x - (OB_SIZE - 1);
                }
            }
            for (y = 0; y < pure_board_size; y++)
            {
                for (x = 0; x < pure_board_size; x++)
                {
                    move_dis[x, y] = x + y + ((x > y) ? x : y);
                    if (move_dis[x, y] >= MOVE_DISTANCE_MAX) move_dis[x, y] = MOVE_DISTANCE_MAX - 1;
                }
            }
            fill_n(board_pos_id, 0);
            i = 1;
            for (y = board_start; y <= (board_start + pure_board_size / 2); y++)
            {
                for (x = board_start; x <= y; x++)
                {
                    board_pos_id[POS(x, y)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - x, y)] = i;
                    board_pos_id[POS(y, x)] = i;
                    board_pos_id[POS(y, board_end + OB_SIZE - x)] = i;
                    board_pos_id[POS(x, board_end + OB_SIZE - y)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - x, board_end + OB_SIZE - y)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - y, x)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - y, board_end + OB_SIZE - x)] = i;
                    i++;
                }
            }
            first_move_candidates = 0;
            for (y = board_start; y <= (board_start + board_end) / 2; y++)
            {
                for (x = board_end + board_start - y; x <= board_end; x++)
                {
                    first_move_candidate[first_move_candidates++] = POS(x, y);
                }
            }
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
        }
        public static void SetKomi(float new_komi)
        {
            default_komi = new_komi;
            KomiSetup = new_komi;
            komi[0] = dynamic_komi[0] = default_komi;
            komi[(int)S_BLACK] = dynamic_komi[(int)S_BLACK] = default_komi + 1;
            komi[(int)S_WHITE] = dynamic_komi[(int)S_WHITE] = default_komi - 1;
        }
        public static void FreeGame(ref game_info_t game) { game = null; }
        public static void InitializeBoard(ref game_info_t game)
        {
            game.Reset();
            Full2PureBoardPos.Clear();
            Pure2FullBoardPos.Clear();
            for (int y = 0; y < board_size; y++)
            {
                for (int x = 0; x < OB_SIZE; x++)
                {
                    game.board[POS(x, y)] = S_OB;
                    game.board[POS(y, x)] = S_OB;
                    game.board[POS(y, board_size - 1 - x)] = S_OB;
                    game.board[POS(board_size - 1 - x, y)] = S_OB;
                }
            }
            /*可下子点写入*/
            for (int y = board_start; y <= board_end; y++)
            {
                for (int x = board_start; x <= board_end; x++)
                {
                    int pos = POS(x, y);
                    game.candidates[pos] = true;
                }
            }
            var p_pos = 0;
            for (int y = board_start; y <= board_end; y++)
            {
                for (int x = board_start; x <= board_end; x++)
                {
                    var f_pos = x + y * board_size;
                    Full2PureBoardPos.Add(f_pos, p_pos);
                    Pure2FullBoardPos.Add(p_pos, f_pos);
                    p_pos++;
                }
            }
            for (int i = 0; i < MAX_STRING; i++)
            {
                game.@string[i].flag = false;
            }
            ClearPattern(game.pat);
        }
        public static void ClearBoard(game_info_t game)
        {
            int i, x, y, pos;
            game.Reset();
            game.current_hash = 0;
            game.previous1_hash = 0;
            game.previous2_hash = 0;
            game.moves = 1;
            game.pass_count = 0;
            for (i = 0; i < BOARD_MAX; i++)
            {
                game.candidates[i] = false;
            }
            for (y = 0; y < board_size; y++)
            {
                for (x = 0; x < OB_SIZE; x++)
                {
                    game.board[POS(x, y)] = S_OB;
                    game.board[POS(y, x)] = S_OB;
                    game.board[POS(y, board_size - 1 - x)] = S_OB;
                    game.board[POS(board_size - 1 - x, y)] = S_OB;
                }
            }
            for (y = board_start; y <= board_end; y++)
            {
                for (x = board_start; x <= board_end; x++)
                {
                    pos = POS(x, y);
                    game.candidates[pos] = true;
                }
            }
            for (i = 0; i < game.@string.Length; i++)
            {
                game.@string[i].Reset();
            }
            ClearPattern(game.pat);
        }
        public static void CopyGame(ref game_info_t dst, game_info_t src)
        {
            game_info_t.DeepCopy(ref dst, ref src);
        }
        public static void InitializeConst()
        {
            int i;
            komi[0] = default_komi;
            komi[(int)S_BLACK] = default_komi + 1.0f;
            komi[(int)S_WHITE] = default_komi - 1.0f;
            i = 0;
            for (int y = board_start; y <= board_end; y++)
            {
                for (int x = board_start; x <= board_end; x++)
                {
                    onboard_pos[i++] = POS(x, y);
                    board_x[POS(x, y)] = x;
                    board_y[POS(x, y)] = y;
                }
            }
            for (int y = board_start; y <= board_end; y++)
            {
                for (int x = board_start; x <= (board_start + pure_board_size / 2); x++)
                {
                    border_dis_x[POS(x, y)] = x - (OB_SIZE - 1);
                    border_dis_x[POS(board_end + OB_SIZE - x, y)] = x - (OB_SIZE - 1);
                    border_dis_y[POS(y, x)] = x - (OB_SIZE - 1);
                    border_dis_y[POS(y, board_end + OB_SIZE - x)] = x - (OB_SIZE - 1);
                }
            }
            for (int y = 0; y < pure_board_size; y++)
            {
                for (int x = 0; x < pure_board_size; x++)
                {
                    move_dis[x, y] = x + y + ((x > y) ? x : y);
                    if (move_dis[x, y] >= MOVE_DISTANCE_MAX) move_dis[x, y] = MOVE_DISTANCE_MAX - 1;
                }
            }
            fill_n(board_pos_id, 0);
            i = 1;
            for (int y = board_start; y <= (board_start + pure_board_size / 2); y++)
            {
                for (int x = board_start; x <= y; x++)
                {
                    board_pos_id[POS(x, y)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - x, y)] = i;
                    board_pos_id[POS(y, x)] = i;
                    board_pos_id[POS(y, board_end + OB_SIZE - x)] = i;
                    board_pos_id[POS(x, board_end + OB_SIZE - y)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - x, board_end + OB_SIZE - y)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - y, x)] = i;
                    board_pos_id[POS(board_end + OB_SIZE - y, board_end + OB_SIZE - x)] = i;
                    i++;
                }
            }
            first_move_candidates = 0;
            for (int y = board_start; y <= (board_start + board_end) / 2; y++)
            {
                for (int x = board_end + board_start - y; x <= board_end; x++)
                {
                    first_move_candidate[first_move_candidates++] = POS(x, y);
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
        private static void InitializeNeighbor()
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
        private static void InitializeEye()
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
            fill_n(eye_condition, E_NOT_EYE);
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
        private static void InitializeTerritory()
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
        public static bool IsLegal(game_info_t game, int pos, Stone color)
        {
            if (game.board[pos] != S_EMPTY)
            {
                return false;
            }
            if (nb4_empty[Pat3(game.pat, pos)] == 0 && IsSuicide(game, game.@string, color, pos))
            {
                return false;
            }
            if (game.ko_pos == pos && game.ko_move == (game.moves - 1))
            {
                return false;
            }
            if (check_superko && pos != PASS)
            {
                Stone other = FLIP_COLOR(color);
                string_t[] @string = game.@string;
                int[] string_id = game.string_id;
                int[] string_next = game.string_next;
                ulong hash = game.positional_hash;
                int[] neighbor4 = new int[4], check = new int[4];
                int @checked = 0, id, str_pos;
                bool flag;
                GetNeighbor4(neighbor4, pos);
                for (int i = 0; i < 4; i++)
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
                hash ^= hash_bit[pos, (int)color];
                for (int i = 0; i < game.moves; i++)
                {
                    if (game.record[i].hash == hash)
                    {
                        return false;
                    }
                }
            }
            return true;
        }
        private static bool IsFalseEyeConnection(game_info_t game, int pos, Stone color)
        {
            string_t[] @string = game.@string;
            int[] string_id = game.string_id;
            //Stone[] board = game.board;
            var board = game.board;
            int[] @checked_string = new int[4];
            int[] string_liberties = new int[4];
            int strings = 0;
            int id, lib, libs = 0, lib_sum = 0;
            int[] liberty = new int[STRING_LIB_MAX];
            int count;
            bool @checked;
            int[] neighbor4 = new int[4];
            int neighbor;
            bool already_checked;
            Stone other = FLIP_COLOR(color);
            int[] player_id = new int[4];
            int player_ids = 0;
            GetNeighbor4(neighbor4, pos);
            for (int i = 0; i < 4; i++)
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
            for (int i = 0; i < 4; i++)
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
                lib_sum += string_liberties[i] - 1;
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
        public static bool IsLegalNotEye(game_info_t game, int pos, Stone color)
        {
            int[] string_id = game.string_id;
            string_t[] @string = game.@string;
            if (game.board[pos] != S_EMPTY)
            {
                game.candidates[pos] = false;
                return false;
            }
            if (game.seki[pos])
            {
                return false;
            }
            if (eye[Pat3(game.pat, pos)] != color || @string[string_id[NORTH(pos)]].libs == 1 || @string[string_id[EAST(pos)]].libs == 1 || @string[string_id[SOUTH(pos)]].libs == 1 || @string[string_id[WEST(pos)]].libs == 1)
            {
                if (nb4_empty[Pat3(game.pat, pos)] == 0 && IsSuicide(game, @string, color, pos))
                {
                    return false;
                }
                if (game.ko_pos == pos &&
                    game.ko_move == (game.moves - 1))
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
                        game.candidates[pos] = false;
                        return false;
                    }
                }
                return true;
            }
            game.candidates[pos] = false;
            return false;
        }
        public static bool ReplaceMove(game_info_t game, int pos, Stone color, int[] replace, ref int replace_num)
        {
            int[] string_id = game.string_id;
            string_t[] @string = game.@string;
            Stone other = FLIP_COLOR(color);
            if (eye[Pat3(game.pat, pos)] != color || @string[string_id[NORTH(pos)]].libs == 1 || @string[string_id[EAST(pos)]].libs == 1 || @string[string_id[SOUTH(pos)]].libs == 1 || @string[string_id[WEST(pos)]].libs == 1)
            {
                if (falsy_eye[Pat3(game.pat, pos)] == color && @string[string_id[NORTH(pos)]].libs > 1 && @string[string_id[EAST(pos)]].libs > 1 && @string[string_id[SOUTH(pos)]].libs > 1 && @string[string_id[WEST(pos)]].libs > 1)
                {
                    int[] check = { NORTH_WEST(pos), NORTH_EAST(pos), SOUTH_WEST(pos), SOUTH_EAST(pos) };
                    foreach (int p in check)
                    {
                        if (game.board[p] != other)
                            continue;
                        string_t s = @string[string_id[p]];
                        if (s.libs == 1)
                        {
                            int lib = s.lib[0];
                            replace[replace_num++] = lib;
                        }
                        else if (s.libs <= 2)
                        {
                            int lib = s.lib[0];
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
        public static bool IsSuicide(game_info_t game, string_t[] @string, Stone color, int pos)
        {
            //Stone[] board = game.board;
            var board = game.board;
            int[] string_id = game.string_id;
            Stone other = FLIP_COLOR(color);
            int[] neighbor4 = new int[4];
            int i;
            GetNeighbor4(neighbor4, pos);
            for (i = 0; i < 4; i++)
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
        public static void PutStone(game_info_t game, int pos, Stone color)
        {
            int[] string_id = game.string_id;
            var board = game.board;
            string_t[] @string = game.@string;
            Stone other = FLIP_COLOR(color);
            int connection = 0;
            var connect = new int[4];
            int prisoner = 0;
            var neighbor = new int[4];
            game.capture_num[(int)color] = 0;
            game.Influence = 0;
            game.previous2_hash = game.previous1_hash;
            game.previous1_hash = game.current_hash;
            if (game.ko_move != 0 && game.ko_move == game.moves - 1)
            {
                game.current_hash ^= hash_bit[game.ko_pos, (int)HASH_KO];
            }
            if (game.moves < MAX_RECORDS)
            {
                game.record[game.moves].color = color;
                game.record[game.moves].pos = Max(pos, PASS);
            }
            if (pos == PASS || pos == RESIGN)
            {
                if (game.moves < MAX_RECORDS)
                {
                    game.record[game.moves].hash = game.positional_hash;
                }
                game.current_hash ^= hash_bit[game.pass_count++, (int)HASH_PASS];
                if (game.pass_count >= BOARD_MAX)
                {
                    game.pass_count = 0;
                }
                game.moves++;
                return;
            }
            board[pos] = color;
            game.candidates[pos] = false;
            game.current_hash ^= hash_bit[pos, (int)color];
            game.positional_hash ^= hash_bit[pos, (int)color];
            UpdatePatternStone(game.pat, color, pos);
            GetNeighbor4(neighbor, pos);
            for (int i = 0; i < 4; i++)
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
                    }
                }
            }
            game.prisoner[(int)color] += prisoner;
            if (connection == 0)
            {
                MakeString(game, pos, color);
                if (prisoner == 1 &&
                    @string[string_id[pos]].libs == 1)
                {
                    game.ko_move = game.moves;
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
            if (game.moves < MAX_RECORDS)
            {
                game.record[game.moves].hash = game.positional_hash;
            }
            game.moves++;
            CheckSeki(game, game.seki);
            game.CalcCtrl();
        }
        private static void MakeString(game_info_t game, int pos, Stone color)
        {
            string_t[] @string = game.@string;
            string_t new_string;
            var board = game.board;
            int[] string_id = game.string_id;
            int id = 1;
            int lib_add = 0;
            Stone other = FLIP_COLOR(color);
            int neighbor, i;
            var neighbor4 = new int[4];
            while (@string[id].flag) { id++; }
            new_string = game.@string[id];
            fill_n<short>(new_string.lib, 0);
            fill_n<short>(new_string.neighbor, 0);
            new_string.lib[0] = LIBERTY_END;
            new_string.neighbor[0] = NEIGHBOR_END;
            new_string.libs = 0;
            new_string.color = color;
            new_string.origin = pos;
            new_string.size = 1;
            new_string.neighbors = 0;
            game.string_id[pos] = id;
            game.string_next[pos] = STRING_END;
            GetNeighbor4(neighbor4, pos);
            for (i = 0; i < 4; i++)
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
        private static void AddStoneToString(game_info_t game, string_t @string, int pos, int head)
        {
            int[] string_next = game.string_next;
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
        private static void AddStone(game_info_t game, int pos, Stone color, int id)
        {
            string_t[] @string = game.@string;
            string_t add_str;
            var board = game.board;
            int[] string_id = game.string_id;
            int lib_add = 0;
            Stone other = FLIP_COLOR(color);
            int neighbor, i;
            int[] neighbor4 = new int[4];
            string_id[pos] = id;
            add_str = @string[id];
            AddStoneToString(game, add_str, pos, 0);
            GetNeighbor4(neighbor4, pos);
            for (i = 0; i < 4; i++)
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
        private static void ConnectString(game_info_t game, int pos, Stone color, int connection, int[] id)
        {
            int min = id[0];
            string_t[] @string = game.@string;
            string_t[] str = new string_t[3];
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
        private static void MergeString(game_info_t game, string_t dst, string_t[] src, int n)
        {
            int tmp, pos, prev, neighbor;
            int[] string_next = game.string_next;
            int[] string_id = game.string_id;
            int id = string_id[dst.origin], rm_id;
            string_t[] @string = game.@string;
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
        private static int AddLiberty(string_t @string, int pos, int head)
        {
            int lib;
            if (@string.lib[pos] != 0) return pos;
            lib = head;
            while (@string.lib[lib] < pos)
            {
                lib = @string.lib[lib];
            }
            @string.lib[pos] = @string.lib[lib];
            @string.lib[lib] = (short)pos;
            @string.libs++;
            return pos;
        }
        private static void RemoveLiberty(game_info_t game, string_t @string, int pos)
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
                game.candidates[@string.lib[0]] = true;
            }
        }
        private static int RemoveString(game_info_t game, string_t @string)
        {
            var str = game.@string;
            int[] string_next = game.string_next;
            int[] string_id = game.string_id;
            int pos = @string.origin; int next;
            var board = game.board;
            var candidates = game.candidates;
            int neighbor, rm_id = string_id[@string.origin];
            var removed_color = board[pos];
            var poisons = game.capture_pos;
            do
            {
                poisons[(int)FLIP_COLOR(board[pos]), GetPureBoardPos(pos)]++;
                board[pos] = S_EMPTY;
                candidates[pos] = true;
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
        private static void AddNeighbor(string_t @string, int id, int head)
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
        private static void RemoveNeighborString(string_t @string, int id)
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
        private static void CheckBentFourInTheCorner(game_info_t game)
        {
            var board = game.board;
            var @string = game.@string;
            int[] string_id = game.string_id;
            int[] string_next = game.string_next;
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
                if (@string[id].size == 3 &&
                    @string[id].libs == 2 &&
                    @string[id].neighbors == 1)
                {
                    color = @string[id].color;
                    lib1 = @string[id].lib[0];
                    lib2 = @string[id].lib[lib1];
                    if ((board[corner_neighbor[i, 0]] == S_EMPTY ||
                        board[corner_neighbor[i, 0]] == color) &&
                        (board[corner_neighbor[i, 1]] == S_EMPTY ||
                        board[corner_neighbor[i, 1]] == color))
                    {
                        neighbor = @string[id].neighbor[0];
                        if (@string[neighbor].libs == 2 &&
                            @string[neighbor].size > 6)
                        {
                            neighbor_lib1 = @string[neighbor].lib[0];
                            neighbor_lib2 = @string[neighbor].lib[neighbor_lib1];
                            if ((neighbor_lib1 == lib1 && neighbor_lib2 == lib2) ||
                                (neighbor_lib1 == lib2 && neighbor_lib2 == lib1))
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
        public static int CalculateScore(game_info_t game, bool fastScore = false)
        {
            var board = game.board;
            int i;
            int pos;
            Stone color;
            var scores = new int[(int)S_MAX];
            if (!fastScore)
                CheckBentFourInTheCorner(game);
            for (i = 0; i < pure_board_max; i++)
            {
                pos = onboard_pos[i];
                color = board[pos];
                if (color == S_EMPTY) color = territory[Pat3(game.pat, pos)];
                scores[(int)color]++;
            }
            return (scores[(int)S_BLACK] - scores[(int)S_WHITE]);
        }
        public static int TransformMove(int p, int i)
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
        public static int RevTransformMove(int p, int i)
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
        public static int GetLibs(game_info_t game, int p)
        {
            var c = game.board[p];
            if (c != S_EMPTY)
            {
                var @string = game.@string;
                int[] string_id = game.string_id;
                return @string[string_id[p]].libs;
            }
            return 0;
        }
        public static int PureBoardPos(int pos)
        {
            int x = X(pos) - OB_SIZE;
            int y = Y(pos) - OB_SIZE;
            return x + y * pure_board_size;
        }
        public static int GetWinner(game_info_t game)
        {
            float score = (CalculateScore(game) - KomiSetup);
            return score > 0 ? 1 : score < 0 ? -1 : 0;
        }
        public static float GetorComputeScore(game_info_t game, bool calculateKomi)
        {
            return (CalculateScore(game) - (calculateKomi ? KomiSetup : 0.0f));
        }
    }
    static class Pattern
    {
        public static void ClearPattern(pattern_t[] pat)
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
        public static void UpdatePat3Empty(pattern_t[] pat, int pos)
        {
            pat[pos + NW].list[(int)MD_2] &= 0xFF3FFF;
            pat[pos + N].list[(int)MD_2] &= 0xFFCFFF;
            pat[pos + NE].list[(int)MD_2] &= 0xFFF3FF;
            pat[pos + W].list[(int)MD_2] &= 0xFFFCFF;
            pat[pos + E].list[(int)MD_2] &= 0xFFFF3F;
            pat[pos + SW].list[(int)MD_2] &= 0xFFFFCF;
            pat[pos + S].list[(int)MD_2] &= 0xFFFFF3;
            pat[pos + SE].list[(int)MD_2] &= 0xFFFFFC;
        }
        public static void UpdatePat3Stone(pattern_t[] pat, Stone color, int pos)
        {
            pat[pos + NW].list[(int)MD_2] |= update_mask[0, (int)color];
            pat[pos + N].list[(int)MD_2] |= update_mask[1, (int)color];
            pat[pos + NE].list[(int)MD_2] |= update_mask[2, (int)color];
            pat[pos + W].list[(int)MD_2] |= update_mask[3, (int)color];
            pat[pos + E].list[(int)MD_2] |= update_mask[4, (int)color];
            pat[pos + SW].list[(int)MD_2] |= update_mask[5, (int)color];
            pat[pos + S].list[(int)MD_2] |= update_mask[6, (int)color];
            pat[pos + SE].list[(int)MD_2] |= update_mask[7, (int)color];
        }
        public static void UpdateMD2Empty(pattern_t[] pat, int pos)
        {
            pat[pos + NW].list[(int)MD_2] &= 0xFF3FFF;
            pat[pos + N].list[(int)MD_2] &= 0xFFCFFF;
            pat[pos + NE].list[(int)MD_2] &= 0xFFF3FF;
            pat[pos + W].list[(int)MD_2] &= 0xFFFCFF;
            pat[pos + E].list[(int)MD_2] &= 0xFFFF3F;
            pat[pos + SW].list[(int)MD_2] &= 0xFFFFCF;
            pat[pos + S].list[(int)MD_2] &= 0xFFFFF3;
            pat[pos + SE].list[(int)MD_2] &= 0xFFFFFC;
            pat[pos + NN].list[(int)MD_2] &= 0xCFFFFF;
            pat[pos + EE].list[(int)MD_2] &= 0x3FFFFF;
            pat[pos + SS].list[(int)MD_2] &= 0xFCFFFF;
            pat[pos + WW].list[(int)MD_2] &= 0xF3FFFF;
        }
        public static void UpdateMD2Stone(pattern_t[] pat, Stone color, int pos)
        {
            pat[pos + NW].list[(int)MD_2] |= update_mask[0, (int)color];
            pat[pos + N].list[(int)MD_2] |= update_mask[1, (int)color];
            pat[pos + NE].list[(int)MD_2] |= update_mask[2, (int)color];
            pat[pos + W].list[(int)MD_2] |= update_mask[3, (int)color];
            pat[pos + E].list[(int)MD_2] |= update_mask[4, (int)color];
            pat[pos + SW].list[(int)MD_2] |= update_mask[5, (int)color];
            pat[pos + S].list[(int)MD_2] |= update_mask[6, (int)color];
            pat[pos + SE].list[(int)MD_2] |= update_mask[7, (int)color];
            pat[pos + NN].list[(int)MD_2] |= update_mask[8, (int)color];
            pat[pos + EE].list[(int)MD_2] |= update_mask[9, (int)color];
            pat[pos + SS].list[(int)MD_2] |= update_mask[10, (int)color];
            pat[pos + WW].list[(int)MD_2] |= update_mask[11, (int)color];
        }
        public static void UpdatePatternEmpty(pattern_t[] pat, int pos)
        {
            pat[pos + NW].list[(int)MD_2] &= 0xFF3FFF;
            pat[pos + N].list[(int)MD_2] &= 0xFFCFFF;
            pat[pos + NE].list[(int)MD_2] &= 0xFFF3FF;
            pat[pos + W].list[(int)MD_2] &= 0xFFFCFF;
            pat[pos + E].list[(int)MD_2] &= 0xFFFF3F;
            pat[pos + SW].list[(int)MD_2] &= 0xFFFFCF;
            pat[pos + S].list[(int)MD_2] &= 0xFFFFF3;
            pat[pos + SE].list[(int)MD_2] &= 0xFFFFFC;
            pat[pos + NN].list[(int)MD_2] &= 0xCFFFFF;
            pat[pos + EE].list[(int)MD_2] &= 0x3FFFFF;
            pat[pos + SS].list[(int)MD_2] &= 0xFCFFFF;
            pat[pos + WW].list[(int)MD_2] &= 0xF3FFFF;
            pat[pos + NN + N].list[(int)MD_3] &= 0xFFCFFF;
            pat[pos + NN + E].list[(int)MD_3] &= 0xFF3FFF;
            pat[pos + EE + N].list[(int)MD_3] &= 0xFCFFFF;
            pat[pos + EE + E].list[(int)MD_3] &= 0xF3FFFF;
            pat[pos + EE + S].list[(int)MD_3] &= 0xCFFFFF;
            pat[pos + SS + E].list[(int)MD_3] &= 0x3FFFFF;
            pat[pos + SS + S].list[(int)MD_3] &= 0xFFFFFC;
            pat[pos + SS + W].list[(int)MD_3] &= 0xFFFFF3;
            pat[pos + WW + S].list[(int)MD_3] &= 0xFFFFCF;
            pat[pos + WW + W].list[(int)MD_3] &= 0xFFFF3F;
            pat[pos + WW + N].list[(int)MD_3] &= 0xFFFCFF;
            pat[pos + NN + W].list[(int)MD_3] &= 0xFFF3FF;
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
            pat[pos + NN + NN + N].large_list[(int)MD_5] &= 0xFFFFCFFFFF;
            pat[pos + NN + NN + E].large_list[(int)MD_5] &= 0xFFFF3FFFFF;
            pat[pos + NN + NE + E].large_list[(int)MD_5] &= 0xFFFCFFFFFF;
            pat[pos + NN + EE + E].large_list[(int)MD_5] &= 0xFFF3FFFFFF;
            pat[pos + NE + EE + E].large_list[(int)MD_5] &= 0xFFCFFFFFFF;
            pat[pos + EE + EE + E].large_list[(int)MD_5] &= 0xFF3FFFFFFF;
            pat[pos + SE + EE + E].large_list[(int)MD_5] &= 0xFCFFFFFFFF;
            pat[pos + SS + EE + E].large_list[(int)MD_5] &= 0xF3FFFFFFFF;
            pat[pos + SS + SE + E].large_list[(int)MD_5] &= 0xCFFFFFFFFF;
            pat[pos + SS + SS + E].large_list[(int)MD_5] &= 0x3FFFFFFFFF;
            pat[pos + SS + SS + S].large_list[(int)MD_5] &= 0xFFFFFFFFFC;
            pat[pos + SS + SS + W].large_list[(int)MD_5] &= 0xFFFFFFFFF3;
            pat[pos + SS + SW + W].large_list[(int)MD_5] &= 0xFFFFFFFFCF;
            pat[pos + SS + WW + W].large_list[(int)MD_5] &= 0xFFFFFFFF3F;
            pat[pos + SW + WW + W].large_list[(int)MD_5] &= 0xFFFFFFFCFF;
            pat[pos + WW + WW + W].large_list[(int)MD_5] &= 0xFFFFFFF3FF;
            pat[pos + NW + WW + W].large_list[(int)MD_5] &= 0xFFFFFFCFFF;
            pat[pos + NN + WW + W].large_list[(int)MD_5] &= 0xFFFFFF3FFF;
            pat[pos + NN + NW + W].large_list[(int)MD_5] &= 0xFFFFFCFFFF;
            pat[pos + NN + NN + W].large_list[(int)MD_5] &= 0xFFFFF3FFFF;
        }
        public static void UpdatePatternStone(pattern_t[] pat, Stone color, int pos)
        {
            pat[pos + NW].list[(int)MD_2] |= update_mask[0, (int)color];
            pat[pos + N].list[(int)MD_2] |= update_mask[1, (int)color];
            pat[pos + NE].list[(int)MD_2] |= update_mask[2, (int)color];
            pat[pos + W].list[(int)MD_2] |= update_mask[3, (int)color];
            pat[pos + E].list[(int)MD_2] |= update_mask[4, (int)color];
            pat[pos + SW].list[(int)MD_2] |= update_mask[5, (int)color];
            pat[pos + S].list[(int)MD_2] |= update_mask[6, (int)color];
            pat[pos + SE].list[(int)MD_2] |= update_mask[7, (int)color];
            pat[pos + NN].list[(int)MD_2] |= update_mask[8, (int)color];
            pat[pos + EE].list[(int)MD_2] |= update_mask[9, (int)color];
            pat[pos + SS].list[(int)MD_2] |= update_mask[10, (int)color];
            pat[pos + WW].list[(int)MD_2] |= update_mask[11, (int)color];
            pat[pos + NN + N].list[(int)MD_3] |= update_mask[12, (int)color];
            pat[pos + NN + E].list[(int)MD_3] |= update_mask[13, (int)color];
            pat[pos + EE + N].list[(int)MD_3] |= update_mask[14, (int)color];
            pat[pos + EE + E].list[(int)MD_3] |= update_mask[15, (int)color];
            pat[pos + EE + S].list[(int)MD_3] |= update_mask[16, (int)color];
            pat[pos + SS + E].list[(int)MD_3] |= update_mask[17, (int)color];
            pat[pos + SS + S].list[(int)MD_3] |= update_mask[18, (int)color];
            pat[pos + SS + W].list[(int)MD_3] |= update_mask[19, (int)color];
            pat[pos + WW + S].list[(int)MD_3] |= update_mask[20, (int)color];
            pat[pos + WW + W].list[(int)MD_3] |= update_mask[21, (int)color];
            pat[pos + WW + N].list[(int)MD_3] |= update_mask[22, (int)color];
            pat[pos + NN + W].list[(int)MD_3] |= update_mask[23, (int)color];
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
            pat[pos + NN + NN + N].large_list[(int)MD_5] |= large_mask[0, (int)color];
            pat[pos + NN + NN + E].large_list[(int)MD_5] |= large_mask[1, (int)color];
            pat[pos + NN + NE + E].large_list[(int)MD_5] |= large_mask[2, (int)color];
            pat[pos + NN + EE + E].large_list[(int)MD_5] |= large_mask[3, (int)color];
            pat[pos + NE + EE + E].large_list[(int)MD_5] |= large_mask[4, (int)color];
            pat[pos + EE + EE + E].large_list[(int)MD_5] |= large_mask[5, (int)color];
            pat[pos + SE + EE + E].large_list[(int)MD_5] |= large_mask[6, (int)color];
            pat[pos + SS + EE + E].large_list[(int)MD_5] |= large_mask[7, (int)color];
            pat[pos + SS + SE + E].large_list[(int)MD_5] |= large_mask[8, (int)color];
            pat[pos + SS + SS + E].large_list[(int)MD_5] |= large_mask[9, (int)color];
            pat[pos + SS + SS + S].large_list[(int)MD_5] |= large_mask[10, (int)color];
            pat[pos + SS + SS + W].large_list[(int)MD_5] |= large_mask[11, (int)color];
            pat[pos + SS + SW + W].large_list[(int)MD_5] |= large_mask[12, (int)color];
            pat[pos + SS + WW + W].large_list[(int)MD_5] |= large_mask[13, (int)color];
            pat[pos + SW + WW + W].large_list[(int)MD_5] |= large_mask[14, (int)color];
            pat[pos + WW + WW + W].large_list[(int)MD_5] |= large_mask[15, (int)color];
            pat[pos + NW + WW + W].large_list[(int)MD_5] |= large_mask[16, (int)color];
            pat[pos + NN + WW + W].large_list[(int)MD_5] |= large_mask[17, (int)color];
            pat[pos + NN + NW + W].large_list[(int)MD_5] |= large_mask[18, (int)color];
            pat[pos + NN + NN + W].large_list[(int)MD_5] |= large_mask[19, (int)color];
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
        public static void Pat3Transpose16(uint pat3, uint[] transp)
        {
            Pat3Transpose8(pat3, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = Pat3Reverse(transp[i]);
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
        public static void MD2Transpose16(uint md2, uint[] transp)
        {
            MD2Transpose8(md2, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD2Reverse(transp[i]);
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
        public static void MD3Transpose16(uint md3, uint[] transp)
        {
            MD3Transpose8(md3, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD3Reverse(transp[i]);
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
        public static void MD4Transpose16(uint md4, uint[] transp)
        {
            MD4Transpose8(md4, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD4Reverse(transp[i]);
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
        public static void MD5Transpose16(ulong md5, ulong[] transp)
        {
            MD5Transpose8(md5, transp);
            for (int i = 0; i < 8; i++)
            {
                transp[i + 8] = MD5Reverse(transp[i]);
            }
        }
        public static uint Pat3Reverse(uint pat3)
        {
            return ((pat3 >> 1) & 0x5555) | ((pat3 & 0x5555) << 1);
        }
        public static uint MD2Reverse(uint md2)
        {
            return ((md2 >> 1) & 0x555555) | ((md2 & 0x555555) << 1);
        }
        public static uint MD3Reverse(uint md3)
        {
            return ((md3 >> 1) & 0x555555) | ((md3 & 0x555555) << 1);
        }
        public static uint MD4Reverse(uint md4)
        {
            return ((md4 >> 1) & 0x55555555) | ((md4 & 0x55555555) << 1);
        }
        public static ulong MD5Reverse(ulong md5)
        {
            return ((md5 >> 1) & 0x5555555555) | ((md5 & 0x5555555555) << 1);
        }
        public static uint Pat3VerticalMirror(uint pat3)
        {
            return ((pat3 & 0xFC00) >> 10) | (pat3 & 0x03C0) | ((pat3 & 0x003F) << 10);
        }
        public static uint MD2VerticalMirror(uint md2)
        {
            return (uint)(((md2 & 0x00FC00) >> 10) | (md2 & 0x0003C0) | ((md2 & 0x00003F) << 10)
                              | (REV2((md2 & 0x330000) >> 16) << 16) | (md2 & 0xCC0000));
        }
        public static uint MD3VerticalMirror(uint md3)
        {
            return (uint)((REV6(md3 & 0x003003)) | (REV4((md3 & 0x000C0C) >> 2) << 2) | (REV2((md3 & 0x000330) >> 4) << 4) | (REV4((md3 & 0xC0C000) >> 14) << 14) | (REV2((md3 & 0x330000) >> 16) << 16) | (md3 & 0x0C00C0));
        }
        public static uint MD4VerticalMirror(uint md4)
        {
            return (uint)((REV8(md4 & 0x00030003)) | (REV6((md4 & 0x0000C00C) >> 2) << 2) | (REV4((md4 & 0x00003030) >> 4) << 4) | (REV2((md4 & 0x00000CC0) >> 6) << 6) | (REV6((md4 & 0xC00C0000) >> 18) << 18) | (REV4((md4 & 0x30300000) >> 20) << 20) | (REV2((md4 & 0x0CC00000) >> 22) << 22) | (md4 & 0x03000300));
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
        public static uint Pat3HorizontalMirror(uint pat3)
        {
            return (uint)((REV3((pat3 & 0xFC00) >> 10) << 10)
                              | (REV((pat3 & 0x03C0) >> 6) << 6)
                              | REV3((pat3 & 0x003F)));
        }
        public static uint MD2HorizontalMirror(uint md2)
        {
            return (uint)((REV3((md2 & 0x00FC00) >> 10) << 10)
                              | (REV((md2 & 0x0003C0) >> 6) << 6)
                              | REV3((md2 & 0x00003F))
                              | (REV2((md2 & 0xCC0000) >> 18) << 18)
                              | (md2 & 0x330000));
        }
        public static uint MD3HorizontalMirror(uint md3)
        {
            return (uint)((md3 & 0x003003)
                              | (REV10((md3 & 0xC0000C) >> 2) << 2) | (REV8((md3 & 0x300030) >> 4) << 4) | (REV6((md3 & 0x0C00C0) >> 6) << 6) | (REV4((md3 & 0x030300) >> 8) << 8) | (REV2((md3 & 0x00CC00) >> 10) << 10));
        }
        public static uint MD4HorizontalMirror(uint md4)
        {
            return (uint)((md4 & 0x00030003)
                              | (REV14((md4 & 0xC000000C) >> 2) << 2) | (REV12((md4 & 0x30000030) >> 4) << 4) | (REV10((md4 & 0x0C0000C0) >> 6) << 6) | (REV8((md4 & 0x03000300) >> 8) << 8) | (REV6((md4 & 0x00C00C00) >> 10) << 10) | (REV4((md4 & 0x00303000) >> 12) << 12) | (REV2((md4 & 0x000CC000) >> 14) << 14));
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
        public static uint Pat3Rotate90(uint pat3)
        {
            return ((pat3 & 0x0003) << 10)
                | ((pat3 & 0x0C0C) << 4)
                | ((pat3 & 0x3030) >> 4)
                | ((pat3 & 0x00C0) << 6)
                | ((pat3 & 0x0300) >> 6)
                | ((pat3 & 0xC000) >> 10);
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
        public static uint MD3Rotate90(uint md3)
        {
            return ((md3 & 0x00003F) << 18)
                | ((md3 & 0xFFFFC0) >> 6);
        }
        public static uint MD4Rotate90(uint md4)
        {
            return ((md4 & 0x000000FF) << 24)
                | ((md4 & 0xFFFFFF00) >> 8);
        }
        public static ulong MD5Rotate90(ulong md5)
        {
            return ((md5 & 0x00000003FF) << 30)
                | ((md5 & 0xFFFFFFFC00) >> 10);
        }
        public static uint Pat3(pattern_t[] pat, int pos)
        {
            return (pat[pos].list[(int)MD_2] & 0xFFFF);
        }
        public static uint MD2(pattern_t[] pat, int pos)
        {
            return (pat[pos].list[(int)MD_2]);
        }
        public static uint MD3(pattern_t[] pat, int pos)
        {
            return (pat[pos].list[(int)MD_3]);
        }
        public static uint MD4(pattern_t[] pat, int pos)
        {
            return (pat[pos].list[(int)MD_4]);
        }
        public static ulong MD5(pattern_t[] pat, int pos)
        {
            return (pat[pos].large_list[(int)MD_5]);
        }
        public static void DisplayInputPat3(uint pat3)
        {
            Console.WriteLine("\n");
            Console.WriteLine("%c%c%c\n", stone[pat3 & 0x3], stone[(pat3 >> 2) & 0x3], stone[(pat3 >> 4) & 0x3]);
            Console.WriteLine("%c*%c\n", stone[(pat3 >> 6) & 0x3], stone[(pat3 >> 8) & 0x3]);
            Console.WriteLine("%c%c%c\n", stone[(pat3 >> 10) & 0x3], stone[(pat3 >> 12) & 0x3], stone[(pat3 >> 14) & 0x3]);
        }
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
        public static void DisplayInputPattern(pattern_t pattern, int size)
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
        private static char[] stone = { '+', '@', 'O', '#' };
        private static ulong REV18(ulong p) { return (((p) >> 36) | (((p) & 0x3) << 36)); }
        private static ulong REV16(ulong p) { return (((p) >> 32) | (((p) & 0x3) << 32)); }
        private static ulong REV14(ulong p) { return (((p) >> 28) | (((p) & 0x3) << 28)); }
        private static ulong REV12(ulong p) { return (((p) >> 24) | (((p) & 0x3) << 24)); }
        private static ulong REV10(ulong p) { return (((p) >> 20) | (((p) & 0x3) << 20)); }
        private static ulong REV8(ulong p) { return (((p) >> 16) | (((p) & 0x3) << 16)); }
        private static ulong REV6(ulong p) { return (((p) >> 12) | (((p) & 0x3) << 12)); }
        private static ulong REV4(ulong p) { return (((p) >> 8) | (((p) & 0x3) << 8)); }
        private static ulong REV2(ulong p) { return (((p) >> 4) | (((p) & 0x3) << 4)); }
        private static ulong REV3(ulong p) { return (((p) >> 4) | ((p) & 0xC) | (((p) & 0x3) << 4)); }
        private static ulong REV(ulong p) { return (((p) >> 2) | (((p) & 0x3) << 2)); }
        private static int N { get { return (-board_size); } }
        private static int S { get { return (board_size); } }
        private static int E { get { return (1); } }
        private static int W { get { return (-1); } }
        private static int NN { get { return (N + N); } }
        private static int NE { get { return (N + E); } }
        private static int NW { get { return (N + W); } }
        private static int SS { get { return (S + S); } }
        private static int SE { get { return (S + E); } }
        private static int SW { get { return (S + W); } }
        private static int WW { get { return (W + W); } }
        private static int EE { get { return (E + E); } }
        private static readonly uint[,] update_mask = new uint[40, 3] {
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
        private static readonly ulong[,] large_mask = new ulong[20, 3] {
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
    };
    static class PatternHash
    {
        public static ulong[,] hash_bit = new ulong[BOARD_MAX, (int)HASH_KO + 1];
        public static ulong[] shape_bit = new ulong[BOARD_MAX];
        //private static uint used;
        //private static int oldest_move;
        private static readonly ulong[,] random_bitstrings = new ulong[BIT_MAX, (int)S_MAX]{
            { 0xc96d191cf6f6aea6LU, 0x401f7ac78bc80f1cLU, 0xb5ee8cb6abe457f8LU, 0xf258d22d4db91392LU },
            { 0x04eef2b4b5d860ccLU, 0x67a7aabe10d172d6LU, 0x40565d50e72b4021LU, 0x05d07b7d1e8de386LU },
            { 0x8548dea130821accLU, 0x583c502c832e0a3aLU, 0x4631aede2e67ffd1LU, 0x8f9fccba4388a61fLU },
            { 0x23d9a035f5e09570LU, 0x8b3a26b7aa4bcecbLU, 0x859c449a06e0302cLU, 0xdb696ab700feb090LU },
            { 0x7ff1366399d92b12LU, 0x6b5bd57a3c9113efLU, 0xbe892b0c53e40d3dLU, 0x3fc97b87bed94159LU },
            { 0x3d413b8d11b4cce2LU, 0x51efc5d2498d7506LU, 0xe916957641c27421LU, 0x2a327e8f39fc19a6LU },
            { 0x3edb3bfe2f0b6337LU, 0x32c51436b7c00275LU, 0xb744bed2696ed37eLU, 0xf7c35c861856282aLU },
            { 0xc4f978fb19ffb724LU, 0x14a93ca1d9bcea61LU, 0x75bda2d6bffcfca4LU, 0x41dbe94941a43d12LU },
            { 0xc6ec7495ac0e00fdLU, 0x957955653083196eLU, 0xf346de027ca95d44LU, 0x702751d1bb724213LU },
            { 0x528184b1277f75feLU, 0x884bb2027e9ac7b0LU, 0x41a0bc6dd5c28762LU, 0x0ba88011cd101288LU },
            { 0x814621bd927e0dacLU, 0xb23cb1552b043b6eLU, 0x175a1fed9bbda880LU, 0xe838ff59b1c9d964LU },
            { 0x07ea06b48fca72acLU, 0x26ebdcf08553011aLU, 0xfb44ea3c3a45cf1cLU, 0x9ed34d63df99a685LU },
            { 0x4c7bf671eaea7207LU, 0x5c7fc5fc683a1085LU, 0x7b20c584708499b9LU, 0x4c3fb0ceb4adb6b9LU },
            { 0x4902095a15d7f3d2LU, 0xec97f42c55bc9f40LU, 0xa0ffc0f9681bb9acLU, 0xc149bd468ac1ac86LU },
            { 0xb6c1a68207ba2fc9LU, 0xb906a73e05a92c74LU, 0x11e0d6ebd61d941dLU, 0x7ca12fb5b05b5c4dLU },
            { 0x16bf95defa2cd170LU, 0xc27697252e02cb81LU, 0x6c7f49bf802c66f5LU, 0x98d3daaa3b2e8562LU },
            { 0x161f5fc4ba37f6d7LU, 0x45e0c63e93fc6383LU, 0x9fb1dbfbc95c83a0LU, 0x38ddd8a535d2cbbdLU },
            { 0x39b6f08daf36ca87LU, 0x6f23d32e2a0fd7faLU, 0xfcc027348974b455LU, 0x360369eda9c0e07dLU },
            { 0xda6c4763c2c466d7LU, 0x48bbb7a741e6ddd9LU, 0xd61c0c76deb4818cLU, 0x5de152345f136375LU },
            { 0xef65d2fcbb279cfdLU, 0xdc22b9f9f9d7538dLU, 0x7dac563216d61e70LU, 0x05a6f16b79bbd6e9LU },
            { 0x5cb3b670ae90be6cLU, 0xbc87a781b47462ceLU, 0x84f579568a8972c8LU, 0x6c469ad3cba9b91aLU },
            { 0x076eb3891fd21cabLU, 0xe8c41087c07c91fcLU, 0x1cb7cd1dfbdab648LU, 0xfaec2f3c1e29110dLU },
            { 0xb0158aacd4dca9f9LU, 0x7cc1b5019ea1196dLU, 0xbc647d48e5e2aeb0LU, 0x96b30966f70500d8LU },
            { 0x87489ee810f7daa5LU, 0x74a51eba09dd373dLU, 0xd40bb2b0a7ca242dLU, 0xded20384ba4b0368LU },
            { 0x7dd248ab68b9df14LU, 0xf83326963d78833dLU, 0xe38821faf65bb505LU, 0x23654ff720304706LU },
            { 0x6fc1c8b51eec90b2LU, 0x580a8a7e936a997fLU, 0x1e7207fe6315d685LU, 0x8c59c6afcbfab7bfLU },
            { 0xc24f82b980d1fa2eLU, 0x084b779ccc9fbe44LU, 0x1a02f04511f6064eLU, 0x9640ec87ea1bee8aLU },
            { 0xb1ee0052dd55d069LU, 0xcab4f30bb95c5561LU, 0xd998babcaf69019fLU, 0xe0126bea2556ccd2LU },
            { 0x9b016f17c8800310LU, 0xf41cc5d147950f43LU, 0xfda9511773320334LU, 0xddf85a4c56345e4dLU },
            { 0xa4e47a8efae8deabLU, 0x9acaa313e6ded943LU, 0xe9a600be8f5c822bLU, 0x778d332a7e54ab53LU },
            { 0x1442a265cefe20caLU, 0xe78262e6b329807cLU, 0xd3ccfa96fed4ad17LU, 0x25b6315bb4e3d4f1LU },
            { 0xcea2b7e820395a1fLU, 0xab3b169e3f7ba6baLU, 0x237e6923d4000b08LU, 0xac1e02df1e10ef6fLU },
            { 0xd519dc015ebf61b2LU, 0xf4f51187fe96b080LU, 0xa137326e14771e17LU, 0x5b10d4a4c1fc81eaLU },
            { 0x52bed44bc6ec0a60LU, 0x10359cffb84288ceLU, 0x47d17b92cd7647a9LU, 0x41c9bafdb9158765LU },
            { 0x16676aa636f40c88LU, 0x12d8aefdff93ad5cLU, 0x19c55cbab761fc6eLU, 0x2174ee4468bdd89fLU },
            { 0xa0bd26f5eddaac55LU, 0x4fdda840f2bea00dLU, 0xf387cba277ee3737LU, 0xf90bba5c10dac7b4LU },
            { 0x33a43afbda5aeebeLU, 0xb9e3019d9af169bbLU, 0xad210ac8d15bbd2bLU, 0x9132a5599c996d32LU },
            { 0xb7e64eb925c34b07LU, 0x35cb859f0469f3c8LU, 0xbf1f44d40cbdfdaeLU, 0xbfbabeaa1611b567LU },
            { 0xe4ea67d4c915e61aLU, 0x1debfa223ca7efe1LU, 0xa77dfc79c3a3071aLU, 0x06cc239429a34614LU },
            { 0x4927012902f7e84cLU, 0x9ca15a0aff31237fLU, 0x5d9e9bc902c99ca8LU, 0x47fa9818255561ffLU },
            { 0xb613301ca773d9f1LU, 0xde64d791fb9ac4faLU, 0x1f5ac2193e8e6749LU, 0xe312b85c388acffbLU },
            { 0x986b17a971a64ff9LU, 0xcb8b41a1609c47bbLU, 0x9132359c66f27446LU, 0xfd13d5b1693465e5LU },
            { 0xf676c5b9c8c31decLU, 0x819c9d4648bde72eLU, 0xcb1b9807f2e17075LU, 0xb833da21219453aeLU },
            { 0x66f5c5f44fb6895fLU, 0x1db2622ebc8a5156LU, 0xd4d55c5a8d8e65c8LU, 0x57518131d59044b5LU },
            { 0xcfda297096d43d12LU, 0x3c92c59d9f4f4fc7LU, 0xef253867322ed69dLU, 0x75466261f580f644LU },
            { 0xda5501f76531dfafLU, 0xbff23daff1ecf103LU, 0x5ea264d24cafa620LU, 0xa4f6e95085e2c1d3LU },
            { 0x96fd21923d8280b4LU, 0xd7e000660c4e449dLU, 0x0175f4ea08c6d68fLU, 0x2fc41e957fb4d4c4LU },
            { 0x4c103d0c50171bc7LU, 0x56b4530e5704ae62LU, 0xb9d88e9704345821LU, 0xfe9bba04dff384a1LU },
            { 0xe6e0124e32eda8e3LU, 0xc45bfbf985540db8LU, 0x20f9dbcc42ded8c7LU, 0x47814256f39a4658LU },
            { 0x20dcfe42bcb14929LU, 0xe38adfbdc8aaba12LU, 0xce488f3a3480ba0dLU, 0x669aa0a29e8fba7cLU },
            { 0x87014f5f7986e0f5LU, 0x4c13ab920adf86f3LU, 0xeaec363831ef859dLU, 0xd012ad6ad0766d3eLU },
            { 0x849098d9f6e9e379LU, 0x99a456e8a46cf927LU, 0xd5756ecf52fa0945LU, 0x7a595501987485daLU },
            { 0x54440bc1354ae014LU, 0x979dad1d15e065ddLU, 0xd37e09f9234fd36fLU, 0x778f38e1b1ff715cLU },
            { 0x443d82e64256a243LU, 0xceb84e9fd0a49a60LU, 0x20bf8789b57f6a91LU, 0x5e2332efbdfa86ebLU },
            { 0x05017bb4eb9c21b1LU, 0x1fbfa8b6c8cd6444LU, 0x2969d7638335eb59LU, 0x6f51c81fe6160790LU },
            { 0xb111fe1560733b30LU, 0x16010e086db16febLU, 0xfcb527b00aaa9de5LU, 0x9e7078912213f6efLU },
            { 0x5f0564bea972c16eLU, 0x3c96a8ea4778734aLU, 0x28b01e6ae9968fb3LU, 0x0970867931d700aeLU },
            { 0x1974ede07597749aLU, 0xaf16f2f8d8527448LU, 0xf3be7db0fe807f1dLU, 0xc97fae4ba2516408LU },
            { 0x3c5c9fe803f69af3LU, 0x5d2fbe764a80fa7fLU, 0x5ced7949a12ab4a1LU, 0xef23ea8441cf5c53LU },
            { 0xffb5a3079c5f3418LU, 0x3373d7f543f1ab0dLU, 0x8d84012afc9aa746LU, 0xb287a6f25e5acdf8LU },
        };
        //private static uint uct_hash_size = UCT_HASH_SIZE;
        //private static uint uct_hash_limit = UCT_HASH_SIZE * 9 / 10;
        public static void patternHash(pattern_t pat, pattern_hash_t hash_pat)
        {
            uint[] md2_transp = new uint[16], md3_transp = new uint[16], md4_transp = new uint[16];
            ulong[] md5_transp = new ulong[16];
            uint tmp2, min2, tmp3, min3;
            ulong tmp4, min4, tmp5, min5;
            int index2, index3, index4, index5;
            MD2Transpose16(pat.list[(int)MD_2], md2_transp);
            MD3Transpose16(pat.list[(int)MD_3], md3_transp);
            MD4Transpose16(pat.list[(int)MD_4], md4_transp);
            MD5Transpose16(pat.large_list[(int)MD_5], md5_transp);
            index2 = index3 = index4 = index5 = 0;
            min2 = md2_transp[0];
            min3 = md3_transp[0] + md2_transp[0];
            min4 = (ulong)md4_transp[0] + md3_transp[0] + md2_transp[0];
            min5 = md5_transp[0] + md4_transp[0] + md3_transp[0] + md2_transp[0];
            for (int i = 1; i < 16; i++)
            {
                tmp2 = md2_transp[i];
                if (min2 > tmp2)
                {
                    index2 = i;
                    min2 = tmp2;
                }
                tmp3 = md3_transp[i] + md2_transp[i];
                if (min3 > tmp3)
                {
                    index3 = i;
                    min3 = tmp3;
                }
                tmp4 = (ulong)md4_transp[i] + md3_transp[i] + md2_transp[i];
                if (min4 > tmp4)
                {
                    index4 = i;
                    min4 = tmp4;
                }
                tmp5 = md5_transp[i] + md4_transp[i] + md3_transp[i] + md2_transp[i];
                if (min5 > tmp5)
                {
                    index5 = i;
                    min5 = tmp5;
                }
            }
            hash_pat.list[(int)MD_2] = MD2Hash(md2_transp[index2]);
            hash_pat.list[(int)MD_3] = MD3Hash(md3_transp[index3]) ^ MD2Hash(md2_transp[index3]);
            hash_pat.list[(int)MD_4] = MD4Hash(md4_transp[index4]) ^ MD3Hash(md3_transp[index4]) ^ MD2Hash(md2_transp[index4]);
            hash_pat.list[(int)MD_5 + (int)MD_MAX] = MD5Hash(md5_transp[index5]) ^ MD4Hash(md4_transp[index5]) ^ MD3Hash(md3_transp[index5]) ^ MD2Hash(md2_transp[index5]);
        }
        public static ulong MD2Hash(uint md2)
        {
            ulong hash = 0;
            for (int i = 0; i < 12; i++)
            {
                hash ^= random_bitstrings[i, (md2 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static ulong MD3Hash(uint md3)
        {
            ulong hash = 0;
            for (int i = 0; i < 12; i++)
            {
                hash ^= random_bitstrings[i + 12, (md3 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static ulong MD4Hash(uint md4)
        {
            ulong hash = 0;
            for (int i = 0; i < 16; i++)
            {
                hash ^= random_bitstrings[i + 24, (md4 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static ulong MD5Hash(ulong md5)
        {
            ulong hash = 0;
            for (int i = 0; i < 20; i++)
            {
                hash ^= random_bitstrings[i + 40, (md5 >> (i * 2)) & 0x3];
            }
            return hash;
        }
        public static void InitializeHash()
        {
            Random rnd = new Random(0x7F2358FF);
            byte[] bytearray = new byte[sizeof(ulong)];
            for (int i = 0; i < BOARD_MAX; i++)
            {
                rnd.NextBytes(bytearray);
                hash_bit[i, (int)HASH_PASS] = BitConverter.ToUInt64(bytearray, 0);
                rnd.NextBytes(bytearray);
                hash_bit[i, (int)HASH_BLACK] = BitConverter.ToUInt64(bytearray, 0);
                rnd.NextBytes(bytearray);
                hash_bit[i, (int)HASH_WHITE] = BitConverter.ToUInt64(bytearray, 0);
                rnd.NextBytes(bytearray);
                hash_bit[i, (int)HASH_KO] = BitConverter.ToUInt64(bytearray, 0);
                rnd.NextBytes(bytearray);
                shape_bit[i] = BitConverter.ToUInt64(bytearray, 0);
            }
        }
        public static int TRANS20(ulong hash) { return (int)(((hash & 0xFFFFFFFF) ^ ((hash >> 32) & 0xFFFFFFFF)) & 0xFFFFF); }
    };
    static class Seki
    {
        public static void CheckSeki(game_info_t game, bool[] seki)
        {
            int i, j, k, pos, id;
            var board = game.board;
            int[] string_id = game.string_id;
            string_t[] @string = game.@string;
            bool[] seki_candidate = new bool[BOARD_MAX];
            int lib1, lib2;
            int[] lib1_id = new int[4], lib2_id = new int[4];
            int lib1_ids, lib2_ids;
            int neighbor1_lib, neighbor2_lib;
            int[] neighbor4 = new int[4];
            bool already_checked;
            for (i = 0; i < pure_board_max; i++)
            {
                pos = onboard_pos[i];
                if (IsSelfAtari(game, S_BLACK, pos) && IsSelfAtari(game, S_WHITE, pos))
                {
                    seki_candidate[pos] = true;
                }
            }
            for (i = 0; i < MAX_STRING; i++)
            {
                if (!@string[i].flag || @string[i].libs != 2) continue;
                if (@string[i].size >= 6) continue;
                lib1 = @string[i].lib[0];
                lib2 = @string[i].lib[lib1];
                if (seki_candidate[lib1] &&
                    seki_candidate[lib2])
                {
                    GetNeighbor4(neighbor4, lib1);
                    lib1_ids = 0;
                    for (j = 0; j < 4; j++)
                    {
                        if (board[neighbor4[j]] == S_BLACK ||
                            board[neighbor4[j]] == S_WHITE)
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
                                {
                                    lib1_id[lib1_ids++] = id;
                                }
                            }
                        }
                    }
                    GetNeighbor4(neighbor4, lib2);
                    lib2_ids = 0;
                    for (j = 0; j < 4; j++)
                    {
                        if (board[neighbor4[j]] == S_BLACK ||
                            board[neighbor4[j]] == S_WHITE)
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
                                {
                                    lib2_id[lib2_ids++] = id;
                                }
                            }
                        }
                    }
                    if (lib1_ids == 1 && lib2_ids == 1)
                    {
                        neighbor1_lib = @string[lib1_id[0]].lib[0];
                        if (neighbor1_lib == lib1 ||
                            neighbor1_lib == lib2)
                        {
                            neighbor1_lib = @string[lib1_id[0]].lib[neighbor1_lib];
                        }
                        neighbor2_lib = @string[lib2_id[0]].lib[0];
                        if (neighbor2_lib == lib1 ||
                            neighbor2_lib == lib2)
                        {
                            neighbor2_lib = @string[lib2_id[0]].lib[neighbor2_lib];
                        }
                        if (neighbor1_lib == neighbor2_lib)
                        {
                            if (eye_condition[Pat3(game.pat, neighbor1_lib)] != E_NOT_EYE)
                            {
                                seki[lib1] = seki[lib2] = true;
                                seki[neighbor1_lib] = true;
                            }
                        }
                        else if (eye_condition[Pat3(game.pat, neighbor1_lib)] == E_COMPLETE_HALF_EYE && eye_condition[Pat3(game.pat, neighbor2_lib)] == E_COMPLETE_HALF_EYE)
                        {
                            int tmp_id1 = 0, tmp_id2 = 0;
                            GetNeighbor4(neighbor4, neighbor1_lib);
                            for (j = 0; j < 4; j++)
                            {
                                if (board[neighbor4[j]] == S_BLACK ||
                                    board[neighbor4[j]] == S_WHITE)
                                {
                                    id = string_id[neighbor4[j]];
                                    if (id != lib1_id[0] &&
                                        id != lib2_id[0] &&
                                        id != tmp_id1)
                                    {
                                        tmp_id1 = id;
                                    }
                                }
                            }
                            GetNeighbor4(neighbor4, neighbor2_lib);
                            for (j = 0; j < 4; j++)
                            {
                                if (board[neighbor4[j]] == S_BLACK ||
                                    board[neighbor4[j]] == S_WHITE)
                                {
                                    id = string_id[neighbor4[j]];
                                    if (id != lib1_id[0] &&
                                        id != lib2_id[0] &&
                                        id != tmp_id2)
                                    {
                                        tmp_id2 = id;
                                    }
                                }
                            }
                            if (tmp_id1 == tmp_id2)
                            {
                                seki[lib1] = seki[lib2] = true;
                                seki[neighbor1_lib] = seki[neighbor2_lib] = true;
                            }
                        }
                    }
                }
            }
        }
    };
    static class Semeai
    {
        private static game_info_t capturable_game = new game_info_t();
        private static game_info_t oiotoshi_game = new game_info_t();
        private static game_info_t liberty_game = new game_info_t();
        private static game_info_t capture_game = new game_info_t();
        public static bool IsCapturableAtari(game_info_t game, int pos, Stone color, int opponent_pos)
        {
            string_t[] @string;
            int[] string_id;
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
            @string = capturable_game.@string;
            string_id = capturable_game.string_id;
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
        public static int CheckOiotoshi(game_info_t game, int pos, Stone color, int opponent_pos)
        {
            string_t[] @string;
            int[] string_id;
            Stone other = FLIP_COLOR(color);
            int neighbor;
            int id, num = -1;
            if (!IsLegal(game, pos, color))
            {
                return -1;
            }
            CopyGame(ref oiotoshi_game, game);
            PutStone(oiotoshi_game, pos, color);
            @string = oiotoshi_game.@string;
            string_id = oiotoshi_game.string_id;
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
        public static int CapturableCandidate(game_info_t game, int id)
        {
            string_t[] @string = game.@string;
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
        public static bool IsDeadlyExtension(game_info_t game, Stone color, int id)
        {
            game_info_t search_game = new game_info_t();
            Stone other = FLIP_COLOR(color);
            int pos = game.@string[id].lib[0];
            if (nb4_empty[Pat3(game.pat, pos)] == 0 && IsSuicide(game, game.@string, other, pos))
                return true;
            CopyGame(ref search_game, game);
            PutStone(search_game, pos, other);
            if (search_game.@string[search_game.string_id[pos]].libs == 1)
                return true;
            else
                return false;
        }
        public static bool IsCapturableNeighborNone(game_info_t game, int id)
        {
            string_t[] @string = game.@string;
            int neighbor = @string[id].neighbor[0];
            while (neighbor != NEIGHBOR_END)
            {
                if (@string[neighbor].libs == 1)
                    return false;
                neighbor = @string[id].neighbor[neighbor];
            }
            return true;
        }
        public static bool IsSelfAtariCapture(game_info_t game, int pos, Stone color, int id)
        {
            string_t[] @string;
            int string_pos = game.@string[id].origin;
            int[] string_id;
            if (!IsLegal(game, pos, color))
                return false;
            CopyGame(ref capture_game, game);
            PutStone(capture_game, pos, color);
            @string = capture_game.@string;
            string_id = capture_game.string_id;
            if (@string[string_id[string_pos]].libs == 1)
                return true;
            else
                return false;
        }
        public static LIBERTY_STATE CheckLibertyState(game_info_t game, int pos, Stone color, int id)
        {
            string_t[] @string;
            int string_pos = game.@string[id].origin;
            int[] string_id;
            int libs = game.@string[id].libs;
            int new_libs;
            if (!IsLegal(game, pos, color))
                return L_DECREASE;
            CopyGame(ref liberty_game, game);
            PutStone(liberty_game, pos, color);
            @string = liberty_game.@string;
            string_id = liberty_game.string_id;
            new_libs = @string[string_id[string_pos]].libs;
            if (new_libs > libs + 1)
                return L_INCREASE;
            else if (new_libs > libs)
                return L_EVEN;
            else
                return L_DECREASE;
        }
        public static bool IsCapturableAtariForSimulation(game_info_t game, int pos, Stone color, int id)
        {
            var board = game.board;
            string_t[] @string = game.@string;
            int[] string_id = game.string_id;
            Stone other = FLIP_COLOR(color);
            int lib;
            bool neighbor = false;
            int index_distance;
            int connect_libs = 0;
            int tmp_id;
            lib = @string[id].lib[0];
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
        public static bool IsSelfAtariCaptureForSimulation(game_info_t game, int pos, Stone color, int lib)
        {
            var board = game.board;
            string_t[] @string = game.@string;
            int[] string_id = game.string_id;
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
        public static bool checkIsCapturableAtari(game_info_t game, int pos, Stone color, int opponent_pos)
        {
            string_t[] @string;
            int[] string_id;
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
            @string = capturable_game.@string;
            string_id = capturable_game.string_id;
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
        public static bool IsSelfAtari(game_info_t game, Stone color, int pos)
        {
            //Stone[] board = game.board;
            var board = game.board;
            string_t[] @string = game.@string;
            int[] string_id = game.string_id;
            Stone other = FLIP_COLOR(color);
            var already = new int[4];
            int already_num = 0;
            int lib, count = 0, libs = 0;
            var lib_candidate = new int[10];
            int i;
            int id;
            bool @checked;
            if (board[NORTH(pos)] == S_EMPTY) lib_candidate[libs++] = NORTH(pos);
            if (board[WEST(pos)] == S_EMPTY) lib_candidate[libs++] = WEST(pos);
            if (board[EAST(pos)] == S_EMPTY) lib_candidate[libs++] = EAST(pos);
            if (board[SOUTH(pos)] == S_EMPTY) lib_candidate[libs++] = SOUTH(pos);
            if (libs >= 2) return false;
            if (board[NORTH(pos)] == color)
            {
                id = string_id[NORTH(pos)];
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
            else if (board[NORTH(pos)] == other &&
                     @string[string_id[NORTH(pos)]].libs == 1)
            {
                return false;
            }
            if (board[WEST(pos)] == color)
            {
                id = string_id[WEST(pos)];
                if (already[0] != id)
                {
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
            }
            else if (board[WEST(pos)] == other &&
                     @string[string_id[WEST(pos)]].libs == 1)
            {
                return false;
            }
            if (board[EAST(pos)] == color)
            {
                id = string_id[EAST(pos)];
                if (already[0] != id && already[1] != id)
                {
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
            }
            else if (board[EAST(pos)] == other &&
                     @string[string_id[EAST(pos)]].libs == 1)
            {
                return false;
            }
            if (board[SOUTH(pos)] == color)
            {
                id = string_id[SOUTH(pos)];
                if (already[0] != id && already[1] != id && already[2] != id)
                {
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
            }
            else if (board[SOUTH(pos)] == other &&
                     @string[string_id[SOUTH(pos)]].libs == 1)
            {
                return false;
            }
            return true;
        }
        public static bool IsAlreadyCaptured(game_info_t game, Stone color, int id, int[] player_id, int player_ids)
        {
            string_t[] @string = game.@string;
            int[] string_id = game.string_id;
            int lib1, lib2;
            bool @checked;
            int[] neighbor4 = new int[4];
            int i, j;
            if (@string[id].libs == 1)
            {
                return true;
            }
            else if (@string[id].libs == 2)
            {
                lib1 = @string[id].lib[0];
                lib2 = @string[id].lib[lib1];
                GetNeighbor4(neighbor4, lib1);
                @checked = false;
                for (i = 0; i < 4; i++)
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
                GetNeighbor4(neighbor4, lib2);
                @checked = false;
                for (i = 0; i < 4; i++)
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
    };
}
