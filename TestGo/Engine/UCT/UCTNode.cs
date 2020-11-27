using System;
using System.Linq;
using System.Collections.Generic;
/// <summary>
/// 这个是UCT+Q-Learning+Sarsa的搜索、强化学习过程
/// 模拟规则采用交替对手学习训练
/// UCT搜索树分为以下几个步骤
/// 1、选择（Sarsa作用，走一步然后值回传）
/// 2、扩展（Sarsa作用，走一步然后值回传）
/// 3、模拟（Sarsa作用，走一步然后值回传）
/// 4、值回传（Q-Learning作用，走完全程，值回传）
/// </summary>
namespace MUCGO_zero_CS
{
    using static Math;
    using static ConstValues;

    using static Board;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// 选择是返回Q_sa还是U_sa
    /// </summary>
    public enum UCTNodeElement { Q_sa, U_sa }



    public class BoardState : IEquatable<BoardState>
    {
        public static short BoardSize => EngineBoardMax;
        public sbyte[] Features { get; private set; }
        public byte[] EnabledPos { get; private set; }
        public short Moves { get; private set; }
        public sbyte CurrentPlayer { get; private set; }
        public float Score_Board { get; private set; }
        public static BoardState CreateBoardState(IBoard board)
        {
            BoardState state = new BoardState()
            {
                Features = board.Features,
                EnabledPos = board.EnabledPos,
                Moves = (short)board.Moves,
                CurrentPlayer = board.CurrentPlayer,
                Score_Board = board.Score_Final
            };
            return state;
        }
        public override bool Equals(object obj)
        {
            if (obj == null || GetType() != obj.GetType()) { return false; }
            var _obj = obj as BoardState;
            if (_obj == null) return false;
            for (int index = 0; index < BoardSize; index++)
            {
                if (_obj.Features[index] != Features[index])
                    return false;
                if (_obj.EnabledPos[index] != EnabledPos[index])
                    return false;
            }
            if (_obj.Moves != Moves || _obj.CurrentPlayer != CurrentPlayer)
                return false;
            return true;
        }
        public bool Equals(BoardState other)
        {
            for (int index = 0; index < BoardSize; index++)
            {
                if (other.Features[index] != Features[index])
                    return false;
                if (other.EnabledPos[index] != EnabledPos[index])
                    return false;
            }
            if (other.Moves != Moves || other.CurrentPlayer != CurrentPlayer)
                return false;
            return true;
        }
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        public static bool operator ==(BoardState lhs, BoardState rhs)
        {
            return lhs.Equals(rhs);
        }
        public static bool operator !=(BoardState lhs, BoardState rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
    public class StateWithValue
    {
        public volatile float Z;
        public volatile float V;
        public volatile float W;
        public volatile float Q;
        public volatile float P;
        public volatile int N;
    }
    public class DataGrids
    {
        /// <summary>
        /// 这个是出现过的状态以及它所在的索引
        /// </summary>
        List<BoardState> State_RowIndex { get; } = new List<BoardState>();
        /// <summary>
        /// 这个是个数据存储网格
        /// 包含了Q_sa、W、P、N
        /// </summary>
        Dictionary<int, Dictionary<int, StateWithValue>> Values { get; } = new Dictionary<int, Dictionary<int, StateWithValue>>();

        public int CreateState(IBoard board)
        {
            var temp_state = BoardState.CreateBoardState(board);
            if (State_RowIndex.Exists((BoardState state) => state == temp_state))
                return State_RowIndex.FindIndex((BoardState state) => state == temp_state);
            State_RowIndex.Add(temp_state);
            return State_RowIndex.Count - 1;

        }
    }
    public class UCTNode
    {
        public sbyte[] Features;
        public byte[] EnabledPos;
        public short Moves;
        public sbyte CurrentPlayer;
        public float Score_Board;
        public volatile float Z;
        public volatile float V;
        public volatile float W;
        public volatile float Q;
        public volatile float P;
        public volatile int N;
        public volatile bool Evaled = false;
        public volatile bool Writebacked = false;
        public static short BoardSize => EngineBoardMax;
        public UCTNode[] Children { get; } = new UCTNode[BoardSize + 1];
    }

    /// <summary>
    /// 搜索树节点信息
    /// </summary>
    [Serializable]
    public class QUCTNode
    {
        #region 棋盘状态
        public static short BoardSize => EngineBoardMax;
        public readonly short Moves;
        public readonly sbyte CurrentPlayer;
        public sbyte[] Features;
        public byte[] EnabledPos;
        public readonly float Score_Board;
        public bool EndGameModel { get; private set; }
        #endregion
        #region 下一个状态转换参数统计       
        /// <summary>
        /// 返回的标签
        /// </summary>
        public volatile float Z = 0;
        /// <summary>
        /// 输赢之后的总目数之和
        /// </summary>
        public volatile float W = 0;
        /// <summary>
        /// 神经网络的估值，会在扩展节点之后回传给上一个状态转换
        /// </summary>
        public volatile float V = 0;
        /// <summary>
        /// 每一次前向访问该节点，都会使值+1
        /// </summary>
        public volatile int N = 1;
        /// <summary>
        /// 是否用经验估算过
        /// </summary>
        [NonSerialized]
        private volatile bool evaluated = false;
        /// <summary>
        /// 是否完成了回写动作
        /// </summary>
        [NonSerialized]
        public volatile bool Writebacked = false;
        /// <summary>
        /// 下一个动作的胜负值累积
        /// </summary>
        public readonly float[] W_sa = new float[EngineBoardMax + 1];
        /// <summary>
        /// 下一个动作的访问次数
        /// </summary>
        public readonly int[] N_sa = new int[EngineBoardMax + 1];
        /// <summary>
        /// 下一个节点的UCB值
        /// </summary>
        public float[] P_sa = new float[EngineBoardMax + 1];
        /// <summary>
        /// 状态转移值
        /// </summary>
        public readonly float[] Q_sa = new float[EngineBoardMax + 1];
        /// <summary>
        /// 孩子结点
        /// </summary>
        [NonSerialized]
        public QUCTNode[] Children = new QUCTNode[EngineBoardMax + 1];
        #endregion
        /// <summary>
        /// 是否使用DNN估计过
        /// </summary>
        public bool Evaled { get { return evaluated; } set { evaluated = value; } }
        /// <summary>
        /// 所有可执行的动作长度
        /// </summary>
        public int ActionsLength => BoardSize + 1;
        /// <summary>
        /// 初始化棋盘
        /// </summary>
        /// <param name="board">棋盘</param>
        public QUCTNode(Board board)
        {
            Moves = (short)board.Moves;
            CurrentPlayer = board.CurrentPlayer;
            Features = board.Features;
            EnabledPos = board.EnabledPos;
            Score_Board = board.Score_UCT;
            evaluated = false;
            Writebacked = false;
            Children = new QUCTNode[EngineBoardMax + 1];
            EndGameModel = (EnabledPos.Count((byte item) => item > 0) == 0) && Moves > PURE_BOARD_MAX / 3 * 2;
            /*如果双方无空位可下，则转换为填子模式（填掉所有周围既有白棋又有黑棋的空）*/
            if (EndGameModel)
                EnabledPos = (board as Board).EnabledPos4Analysis;
        }
        /// <summary>
        /// 得到最大的转移动作
        /// </summary>
        /// <returns>那个动作</returns>
        public short MaxiumAction(bool Selfplay = true, bool UseNN = true, bool FinalReview = false)
        {
            bool CanResigned = false;
            if (Children == null) Children = new QUCTNode[ActionsLength];
            float[] Values = new float[ActionsLength];
            if (Selfplay)
                CanResigned = Moves > SELFPLAY_MINSTEP;
            else
                CanResigned = Moves > PLAY_MINSTEP;
            var movepass = PassMove();
            var HeadOfUCB = (float)Sqrt(2 * Log(N));
            Parallel.For(0, ActionsLength, (int action) =>
            {
                Values[action] = float.NegativeInfinity;
                if ((action < movepass && EnabledPos[action] == 0) || (action == movepass && !CanResigned)) return;
                if (FinalReview && Children[action] == null) return;
                var N_s_a = Thread.VolatileRead(ref N_sa[action]);
                var Q_s_a = Thread.VolatileRead(ref Q_sa[action]) * CurrentPlayer; // ((N_s_a > 0) ? (W_s_a / N_s_a) : 0.0f);
                var U_s_a = HeadOfUCB / (N_s_a + 1);
                var P_s_a = (UseNN ? Thread.VolatileRead(ref P_sa[action]) : 1);
                Values[action] = Q_s_a + Cpuct * (P_s_a * U_s_a);
            });
            var MaxiumValue = Values.Max();
            Parallel.For(0, ActionsLength, (int action) =>
            {
                if (float.IsNegativeInfinity(Values[action]) || Values[action] < MaxiumValue)
                    Values[action] = float.NegativeInfinity;
                else
                    Values[action] *= (float)randomData.NextDouble();
            });
            return (short)Array.IndexOf(Values, Values.Max());
        }
        /// <summary>
        /// 得到棋盘状态
        /// </summary>
        /// <returns></returns>
        public IList<float> GetBoardState()
        {
            lock (Features)
                return Features.ToList().ConvertAll((sbyte data) => (float)data);
        }
        /// <summary>
        /// 得到Pie的数值
        /// </summary>
        /// <returns></returns>
        public IList<float> GetPieData(Func<int, float> GetPolicyFactions = null)
        {
            var pie = new float[ActionsLength];
            var Faction = GetPolicyFactions?.Invoke(Moves);
            var N_sa_Sum = N_sa.Sum();
            for (int i = 0; i < pie.Length; i++)
            {
                pie[i] = N_sa[i] / N_sa_Sum;
                if (Faction.HasValue)
                    pie[i] = pie[i] * Faction.Value;
            }
            return pie.ToList();
        }
        /// <summary>
        /// 得到Z的数值
        /// </summary>
        /// <returns></returns>
        public IList<float> GetZData()
        {
            return new List<float>(new float[1] { Z });
        }
        /// <summary>
        /// 回写NN的估值
        /// </summary>
        /// <param name="Policy"></param>
        /// <param name="Value"></param>
        public void Writeback(IList<float> Policy, IList<float> Value)
        {
            V += Value[0];
            W += V;
            lock (this) P_sa = Policy.ToArray();
            //lock (P_sa) Policy.CopyTo(P_sa, 0);
            Writebacked = true;
        }

        private void DeleteNodes(ref QUCTNode root)
        {
            if (root == null) return;
            for (int index = 0; index < root.Children.Length; index++)
                DeleteNodes(ref root.Children[index]);
            root = null;
        }
        public void ClearEdges(int ExceptPos = -1)
        {
            if ((ExceptPos < 0 || ExceptPos >= ActionsLength) && Children != null)
                Parallel.For(0, ActionsLength, (int pos_edge) =>
                {
                    Children[pos_edge] = null;// DeleteNodes(ref Children[pos_edge]);
                });
            else
            {
                if (Children != null)
                    Parallel.For(0, ActionsLength, (int pos_edge) =>
                    {
                        if (pos_edge != ExceptPos)
                            Children[pos_edge] = null;
                    });
            }
        }
        public QUCTNode ExpandOrVisit(Board board, int pos)
        {
            if (Children == null) Children = new QUCTNode[ActionsLength];
            lock (Children)
            {
                if (Children[pos] == null)
                {
                    Children[pos] = new QUCTNode(board);
                    return Children[pos];
                }
            }
            Children[pos].N++;
            Thread.VolatileWrite(ref N_sa[pos], Thread.VolatileRead(ref N_sa[pos]) + 1);
            return Children[pos];

        }
        public void AddNodeAtBegin(QUCTNode node, int pos)
        {
            if (Children == null) Children = new QUCTNode[ActionsLength];
            if (node == null || pos >= PassMove()) return;
            Children[pos] = node;
        }

        public short PassMove()
        {
            return BoardSize;
        }
        public float PosValue(int move)
        {
            if (move > PassMove() || move < 0 || N_sa[move] < 1) return 0;
            return W_sa[move] / N_sa[move];
        }

    };
}
