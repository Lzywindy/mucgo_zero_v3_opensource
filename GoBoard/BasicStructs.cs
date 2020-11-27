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
    using static Board;
    using System.Threading;
    using System.Collections;
    using System.Security.Cryptography;
    [Flags]
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
    [Serializable]
    public class Move_t : ICloneable
    {
        public Stone color;
        public short pos;
        public ulong hash;
        public Move_t()
        {
            color = S_EMPTY;
            pos = -1;
            hash = 0;
        }
        public Move_t(Stone color, short pos, ulong hash)
        {
            this.color = color;
            this.pos = pos;
            this.hash = hash;
        }
        public object Clone()
        {
            return new Move_t(color, pos, hash);
        }
    }
    [Serializable]
    public class String_t : ICloneable
    {
        public Stone color;
        public short libs;
        public readonly short[] lib = new short[STRING_LIB_MAX];
        public short neighbors;
        public readonly short[] neighbor = new short[MAX_NEIGHBOR];
        public short origin;
        public short size;
        public bool flag;
        public void Reset()
        {
            color = S_EMPTY;
            libs = 0;
            neighbors = 0;
            origin = 0;
            size = 0;
            flag = default(bool);
            Array.Clear(lib, 0, lib.Length);
            Array.Clear(neighbor, 0, neighbor.Length);
        }
        public void Clone(ref String_t @string)
        {
            if (@string == null) @string = new String_t();
            lib.CopyTo(@string.lib, 0);
            neighbor.CopyTo(@string.neighbor, 0);
            @string.color = color;
            @string.libs = libs;
            @string.neighbors = neighbors;
            @string.origin = origin;
            @string.size = size;
            @string.flag = flag;
        }

        public object Clone()
        {
            String_t @string = new String_t();
            @string.color = color;
            @string.libs = libs;
            @string.neighbors = neighbors;
            @string.origin = origin;
            @string.size = size;
            @string.flag = flag;
            Buffer.BlockCopy(lib, 0, @string.lib, 0, STRING_LIB_MAX);
            Buffer.BlockCopy(neighbor, 0, @string.neighbor, 0, MAX_NEIGHBOR);
            return @string;
        }
    }
    [Serializable]
    public class Pattern_t : ICloneable
    {
        public readonly uint[] list = new uint[(int)MD_MAX];
        public readonly ulong[] large_list = new ulong[(int)MD_LARGE_MAX];

        public object Clone()
        {
            Pattern_t pattern = new Pattern_t();
            list.CopyTo(pattern.list, 0);
            large_list.CopyTo(pattern.large_list, 0);
            return pattern;
        }
        public void Clone(ref Pattern_t pattern)
        {
            if (pattern == null) pattern = new Pattern_t();
            list.CopyTo(pattern.list, 0);
            large_list.CopyTo(pattern.large_list, 0);
        }
        public void Reset()
        {
            for (int i = 0; i < list.Length; i++)
                list[i] = 0;
            for (int i = 0; i < large_list.Length; i++)
                large_list[i] = 0;
        }
    }
    public class Pattern_hash_t
    {
        public readonly ulong[] list = new ulong[(int)MD_MAX + (int)MD_LARGE_MAX];
        public static void DeepCopy(ref Pattern_hash_t dst, ref Pattern_hash_t src)
        {
            if (src == null) return;
            if (dst == null) dst = new Pattern_hash_t();
            for (int i = 0; i < (int)MD_MAX + (int)MD_LARGE_MAX; i++)
                dst.list[i] = src.list[i];
        }
        public void Reset()
        {
            for (int i = 0; i < list.Length; i++)
                list[i] = 0;
        }
    }
    public class Statistic_t
    {
        public readonly float[] colors = new float[3];
        public float criticality { get; private set; }
        public int criticality_index { get; private set; }
        public int owner_index { get; private set; }

        public void Clear()
        {
            Array.Clear(colors, 0, 3);
            criticality = 0;
            criticality_index = 0;
        }
        public static Statistic_t[] CreateStatistic()
        {
            Statistic_t[] statistic_sss = new Statistic_t[pure_board_max];
            for (int index = 0; index < pure_board_max; index++)
                statistic_sss[index] = new Statistic_t();
            return statistic_sss;
        }
        public static void Statistic(ref Statistic_t[] statistics, Board board, sbyte winner)
        {
            if (statistics == null)
            {
                statistics = new Statistic_t[pure_board_max];
                for (int index = 0; index < pure_board_max; index++)
                    statistics[index] = new Statistic_t();
            }

            var Terrains = board.Terrains;
            sbyte color_index = 0;
            for (int pos = 0; pos < Terrains.Length; pos++)
            {
                if (Terrains[pos] == 1) color_index = 1;
                else if (Terrains[pos] == -1) color_index = 2;
                else color_index = 0;
                if (color_index != 0)
                    Thread.VolatileWrite(ref statistics[pos].colors[color_index], Thread.VolatileRead(ref statistics[pos].colors[color_index]) + 1);
                if (Terrains[pos] == winner)
                    Thread.VolatileWrite(ref statistics[pos].colors[0], Thread.VolatileRead(ref statistics[pos].colors[0]) + 1);
            }
        }
        public static void ClearAll(ref Statistic_t[] statistics)
        {
            if (statistics == null)
            {
                statistics = new Statistic_t[pure_board_max];
                for (int index = 0; index < pure_board_max; index++)
                    statistics[index] = new Statistic_t();
            }
            else
            {
                for (int index = 0; index < pure_board_max; index++)
                    statistics[index].Clear();
            }
        }
        //public static void CalculateCriticality(ref Statistic_t[] statistics, QUCTNode CurrentNode, sbyte Color, int count)
        //{
        //    var win = Max(Min(CurrentNode.W / CurrentNode.N, 1), -1);
        //    var lose = 1.0 - win;
        //    sbyte color = 0, other = 0;

        //    if (Color == 1) { color = 1; other = 2; }
        //    else if (Color == -1) { color = 2; other = 1; }
        //    else color = other = 0;

        //    for (int pos = 0; pos < pure_board_max; pos++)
        //    {
        //        var tmp = (float)((statistics[pos].colors[0] / (float)count) - (((statistics[pos].colors[color] / (float)count) * win) + ((statistics[pos].colors[other] / (float)count) * lose)));
        //        statistics[pos].criticality = tmp;
        //        statistics[pos].criticality_index = (int)(tmp * 40);
        //        if (statistics[pos].criticality_index > criticality_max - 1) statistics[pos].criticality_index = criticality_max - 1;
        //    }
        //}
        public static void CalculateOwner(ref Statistic_t[] statistics, int Color, int count)
        {
            sbyte color = 0;
            if (Color == 1) { color = 1; }
            else if (Color == -1) { color = 2; }
            else color = 0;
            for (int pos = 0; pos < pure_board_max; pos++)
            {
                statistics[pos].owner_index = (int)(statistics[pos].colors[color] * 10.0 / count + 0.5);
                if (statistics[pos].owner_index > OWNER_MAX - 1) statistics[pos].owner_index = OWNER_MAX - 1;
                if (statistics[pos].owner_index < 0) statistics[pos].owner_index = 0;
            }
        }
        public static void NormalizeAllStatistic(ref Statistic_t[] statistics)
        {
            foreach (var statistic in statistics)
            {
                var total = statistic.colors[1] + statistic.colors[2];
                statistic.colors[0] = statistic.colors[0] / total;
                statistic.colors[1] = statistic.colors[1] / total;
                statistic.colors[2] = statistic.colors[2] / total;
            }
        }
    }
    public struct Po_info_t
    {
        public int num;   // 接下来的搜索次数
        public int halt;  // 停止搜索次数
        public volatile int count;       // 现在搜索次数

        public void Clear()
        {
            count = 0;
        }
    };

    [Serializable]
    public class BoardHash : IEqualityComparer
    {
        const int MD5Length = 16;
        byte[] Hashdata;
        public BoardHash()
        {
            Hashdata = new byte[MD5Length];
        }
        public BoardHash(Stone[] board, MD5 md5class)
        {
            UpdateHash(board, md5class);
        }
        public void UpdateHash(Stone[] board, MD5 md5class)
        {
            var boarddata = new byte[pure_board_max];
            for (int index = 0; index < pure_board_max; index++)
                boarddata[index] = (byte)board[onboard_pos_2full[index]];
            Hashdata = md5class.ComputeHash(boarddata);
        }
        public new bool Equals(object x, object y)
        {
            return (x as BoardHash).Hashdata.SequenceEqual((y as BoardHash).Hashdata);
        }
        public int GetHashCode(object obj)
        {
            int HashCode = 0;
            int length = MD5Length / sizeof(int);
            for (int index = 0; index < length; index++)
                HashCode ^= BitConverter.ToInt32((obj as BoardHash).Hashdata, index * sizeof(int));
            return HashCode;
        }
    }
}
