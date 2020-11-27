using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace TestGo
{
    using static UCT;
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
    using static DQN_DNN_Class;
    using static Utils;
    using System.Security.Cryptography;
    using System.Threading;
    /// <summary>
    /// 盘面节点信息
    /// </summary>
    class NodeData : IEqualityComparer<NodeData>, IEquatable<NodeData>
    {
        /*落子颜色*/
        public Stone color;
        /*棋盘状态*/
        public readonly Stone[] boardpos = new Stone[PURE_BOARD_MAX];
        /*禁着点*/
        public readonly bool[] forbidpos = new bool[PURE_BOARD_MAX];
        /*上一步落子位置*/
        public short pos;
        /*当前走到的步数*/
        public int moves;
        public NodeData()
        {
            Reset();
        }
        public NodeData(NodeData _NodeData)
        {
            color = _NodeData.color;
            Array.Copy(_NodeData.boardpos, boardpos, boardpos.Length);
            Array.Copy(_NodeData.forbidpos, forbidpos, forbidpos.Length);
            pos = _NodeData.pos;
            moves = _NodeData.moves;
        }
        public NodeData(game_info_t game)
        {
            Reset(game);
        }
        public void Reset(game_info_t game = null)
        {
            if (game == null)
            {
                color = S_EMPTY;
                Array.Clear(boardpos, 0, boardpos.Length);
                Array.Clear(forbidpos, 0, forbidpos.Length);
                pos = 0;
                moves = 0;
            }
            else
            {
                moves = game.moves;
                pos = (short)game.record[moves - 1].pos;
                if (moves == 1)
                    color = S_BLACK;
                else
                    color = FLIP_COLOR(game.record[moves - 1].color);
                for (int pos = 0; pos < PURE_BOARD_MAX; pos++)
                {
                    /*当前局面*/
                    boardpos[pos] = game.board[GetFullBoardPos(pos)];
                    /*下一手对方的禁着点*/
                    forbidpos[pos] = game.candidates[GetFullBoardPos(pos)] && IsLegal(game, GetFullBoardPos(pos), color);
                }
            }
        }
        public static void DeepCopy(NodeData dis, NodeData src)
        {
            dis.color = src.color;
            Array.Copy(src.boardpos, dis.boardpos, dis.boardpos.Length);
            Array.Copy(src.forbidpos, dis.forbidpos, dis.forbidpos.Length);
            dis.pos = src.pos;
            dis.moves = src.moves;
        }
        /// <summary>
        /// 判断两结构体是否一致
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public bool Equals(NodeData x, NodeData y)
        {
            bool equals = true;
            equals = (equals && x.color == y.color);
            equals = (equals && x.pos == y.pos);
            equals = (equals && x.moves == y.moves);
            for (int i = 0; i < PURE_BOARD_MAX; i++)
            {
                equals = (equals && x.boardpos[i] == y.boardpos[i]);
                equals = (equals && x.forbidpos[i] == y.forbidpos[i]);
            }
            return equals;
        }
        /// <summary>
        /// 得到这个结构体的哈希值
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public int GetHashCode(NodeData obj)
        {
            var MD5Code = new MD5CryptoServiceProvider();
            var tmpBytes = MD5Code.ComputeHash(obj.GetBytes());
            int HashCode = 0;
            for (int i = 0; i < tmpBytes.Length / sizeof(int); i++)
                HashCode ^= BitConverter.ToInt32(tmpBytes, i * sizeof(int));
            return HashCode;
        }
        /// <summary>
        /// 得到这个结构体所有的字节
        /// </summary>
        /// <returns>字节串</returns>
        public byte[] GetBytes()
        {
            List<byte> bytes = new List<byte>(sizeof(byte) * (PURE_BOARD_MAX + 2) + sizeof(short) + sizeof(int) + sizeof(bool) * (PURE_BOARD_MAX + 1));
            bytes.AddRange(BitConverter.GetBytes((byte)color));
            for (int i = 0; i < boardpos.Length; i++)
                bytes.AddRange(BitConverter.GetBytes((byte)boardpos[i]));
            for (int i = 0; i < forbidpos.Length; i++)
                bytes.AddRange(BitConverter.GetBytes(forbidpos[i]));
            bytes.AddRange(BitConverter.GetBytes(pos));
            bytes.AddRange(BitConverter.GetBytes(moves));
            return bytes.ToArray();
        }
        /// <summary>
        /// 判断和其他结构体是否相等
        /// </summary>
        /// <param name="other">其他同类结构体</param>
        /// <returns>是否相等</returns>
        public bool Equals(NodeData other)
        {
            bool equals = true;
            equals = (equals && other.color == color);
            equals = (equals && other.pos == pos);
            equals = (equals && other.moves == moves);
            for (int i = 0; i < PURE_BOARD_MAX; i++)
            {
                equals = (equals && other.boardpos[i] == boardpos[i]);
                equals = (equals && other.forbidpos[i] == forbidpos[i]);
            }
            return equals;
        }
        /*把盘面信息转化为NN认识的值*/
        public List<float> GetBoardInfos()
        {
            List<float> Features = new List<float>(new float[PURE_BOARD_MAX * 2]);
            var inf_color = FLIP_COLOR(color);
            for (var i = 0; i < PURE_BOARD_MAX * 2; i++)
            {
                if (i < PURE_BOARD_MAX)
                    Features[i] = boardpos[i] == inf_color ? 1.0f : boardpos[i] == color ? -1.0f : 0.0f;
                else
                    Features[i] = forbidpos[i % PURE_BOARD_MAX] ? 1.0f : 0.0f;
            }
            return Features;
        }
    }
    /// <summary>
    /// 搜索树节点信息
    /// </summary>
    class UCTNode
    {
        /*终节点点反向回传的胜利值*/
        public volatile int value_win = 0;
        /*每一次前向访问该节点，都会使值+1*/
        public volatile int visit_count = 1;
        /*下一个动作的胜负值*/
        public readonly int[] value_win_node = new int[PURE_BOARD_MAX + 1];
        /*下一个动作的访问次数*/
        public readonly int[] visit_count_node = new int[PURE_BOARD_MAX + 1];
        /*依据经验得到的概率*/
        public readonly float[] P = new float[PURE_BOARD_MAX + 1];
        /*依据经验得到的价值*/
        public volatile float V = 0;
        /*是否用经验估算过*/
        public volatile bool Evaled = false;
        /**/
        public readonly Mutex thisLock = new Mutex();
        public void ReLUSoomthP()
        {
            lock (this)
            {
                float sum = 0;
                for (var i = 0; i < P.Length; i++)
                    P[i] += Max(sum, 0.0f);
                sum = (sum > 0 ? sum : 1);
                for (var i = 0; i < P.Length; i++)
                    P[i] = P[i] / sum;
            }
        }
        public UCTNode()
        {
            Reset();
        }
        public UCTNode(UCTNode _UCT_Node)
        {
            value_win = _UCT_Node.value_win;
            visit_count = _UCT_Node.visit_count;
            V = _UCT_Node.V;
            Evaled = _UCT_Node.Evaled;
            Array.Copy(_UCT_Node.value_win_node, value_win_node, value_win_node.Length);
            Array.Copy(_UCT_Node.visit_count_node, visit_count_node, visit_count_node.Length);
            Array.Copy(_UCT_Node.P, P, P.Length);
        }
        /*重置节点*/
        public void Reset()
        {
            value_win = 0;
            visit_count = 1;
            V = 0;
            Evaled = false;
            Array.Clear(value_win_node, 0, value_win_node.Length);
            Array.Clear(visit_count_node, 0, visit_count_node.Length);
            for (int i = 0; i < P.Length; i++)
                P[i] = 1 / P.Length;
        }
        /*初始化节点*/
        public void InitNode()
        {
            value_win = 0;
            visit_count = 1;
        }
        /// <summary>
        /// 节点的线性叠加
        /// </summary>
        /// <param name="node">其他Hash值一致的节点</param>
        public void LinerAddOn(UCTNode node)
        {
            lock (this)
            {
                value_win += node.value_win;
                visit_count += node.visit_count;
                for (var i = 0; i <= PURE_BOARD_MAX; i++)
                {
                    value_win_node[i] += node.value_win_node[i];
                    visit_count_node[i] += node.visit_count_node[i];
                }
            }
        }
        /// <summary>
        /// 依据禁着点更新自己的概率
        /// </summary>
        /// <param name="forbidpos">禁着点</param>
        public void Update(bool[] forbidpos = null, float E_gready = 0.1f)
        {
            lock (this)
            {
                if (forbidpos == null || forbidpos.Length != PURE_BOARD_MAX) return;
                float sum = 0;
                for (int i = 0; i <= PURE_BOARD_MAX; i++)
                {
                    P[i] = Max((i < PURE_BOARD_MAX && (!forbidpos[i])) ? 0 : P[i] + E_gready * visit_count_node[i] / ((float)visit_count == 0 ? 1.0f : visit_count), 0);
                    sum += Max(P[i], 0);
                }
                sum = sum == 0 ? 1 : sum;
                for (int i = 0; i <= PURE_BOARD_MAX; i++)
                    P[i] = P[i] / sum;
                //V = (V + E_gready * value_win / (visit_count == 0 ? 1 : visit_count)) / (1 + E_gready);
            }
            V = value_win / (float)visit_count;
        }
        /// <summary>
        /// 获得搜索后的经验值
        /// </summary>
        /// <param name="Pie">UCT搜索树搜出的结果策略</param>
        /// <param name="Z">UCT搜索树搜出的结果价值</param>
        public void GetExp(out List<float> Pie, out List<float> Z)
        {
            lock (this)
            {
                Pie = new List<float>(new float[PURE_BOARD_MAX]);
                Z = new List<float>(new float[1] { V });
                for (int i = 0; i < PURE_BOARD_MAX; i++)
                {
                    Pie[i] = P[i];
                }
            }
        }
        /// <summary>
        /// 提升（降低）下次走子某个位置概率
        /// </summary>
        /// <param name="pos"></param>
        /// <param name="value"></param>
        /// <param name="forbidpos"></param>
        public void SetAdvantagePos(int pos, float value, bool[] forbidpos = null)
        {
            if (forbidpos == null || forbidpos.Length != PURE_BOARD_MAX) return;
            lock (this)
            {
                var ppos = GetPureBoardPos(pos);
                P[ppos] += forbidpos[ppos] ? value : 0;
            }
        }
    };
    /// <summary>
    /// UCT搜索树存储空间
    /// </summary>
    class UCTStorge
    {
        protected readonly Dictionary<NodeData, UCTNode> DataSet = new Dictionary<NodeData, UCTNode>();
        protected readonly List<List<KeyValuePair<NodeData, UCTNode>>> LayerModel = new List<List<KeyValuePair<NodeData, UCTNode>>>();
        protected readonly List<int> LayerSizeLimit = new List<int>();
        protected int MaxiumNodes = 0;
        protected int WorkerCount = 12;
        /// <summary>
        /// 搜索树存储的构造函数
        /// </summary>
        /// <param name="depth">默认搜索深度</param>
        public UCTStorge(int depth = 422)
        {
            MaxiumNodes = 0;
            for (int i = 0; i < depth; i++)
            {
                var value = (int)(pure_board_max * Sqrt(2 * Log((i + 1.0f))) + 1.0f);
                MaxiumNodes += value;
                LayerSizeLimit.Add(value);
                LayerModel.Add(new List<KeyValuePair<NodeData, UCTNode>>(value));
            }
        }
        /// <summary>
        /// 获取节点
        /// </summary>
        /// <param name="key">节点的关键字</param>
        /// <returns>对应节点（没有则返回空）</returns>
        protected UCTNode GetNode(NodeData key)
        {
            lock (DataSet)
            {
                UCTNode data = null;
                DataSet.TryGetValue(key, out data);
                return data;
            }
        }
        /// <summary>
        /// 更新节点信息（如果没有，则向表中追加）
        /// </summary>
        /// <param name="Key">关键字</param>
        /// <param name="Value">值</param>
        protected void UpdateNode(NodeData Key, UCTNode Value)
        {
            lock (DataSet)
            {
                UCTNode data = null;
                bool existed = DataSet.TryGetValue(Key, out data);
                if (Value == null)
                {
                    Value = new UCTNode();
                    Value.Update(Key.forbidpos);
                }
                if (!existed)
                {
                    DataSet.Add(Key, Value);
                    LayerModel[Key.moves].Add(new KeyValuePair<NodeData, UCTNode>(Key, Value));
                }
                else
                    data.LinerAddOn(Value);
            }
        }
        /// <summary>
        /// 删除超出限制的节点
        /// </summary>
        protected void ClearOutbandNodes()
        {
            //LayerModel.AsParallel().ForAll((ref List<KeyValuePair<NodeData, UCTNode>> layer) =>
            //{
            //    layer.Sort((KeyValuePair<NodeData, UCTNode> node1, KeyValuePair<NodeData, UCTNode> node2) =>
            //    {
            //        if (node1.Value == null && node2.Value != null) return -1;
            //        else if (node1.Value != null && node2.Value == null) return 1;
            //        else return Min(Max((node2.Value.visit_count - Abs(node2.Value.value_win)) - (node1.Value.visit_count - Abs(node1.Value.value_win)), -1), 1);
            //    });
            //});
            /*删除多余部分节点的动作*/

            Parallel.For(2, LayerModel.Count, (int layerid) =>
            {
                List<KeyValuePair<NodeData, UCTNode>> layer;
                int limited = 0;
                /*判断是否出界*/
                lock (LayerModel)
                {
                    if (layerid > LayerModel.Count) return;
                    layer = LayerModel[layerid];
                    limited = (int)(LayerSizeLimit[layerid] / 1.5);
                    if (layer.Count <= limited + 1) return;
                }
                if (layer == null) return;
                layer.Sort((KeyValuePair<NodeData, UCTNode> node1, KeyValuePair<NodeData, UCTNode> node2) =>
                {
                    if (node1.Value == null && node2.Value != null) return -1;
                    else if (node1.Value != null && node2.Value == null) return 1;
                    else return Min(Max((node2.Value.visit_count - Abs(node2.Value.value_win)) - (node1.Value.visit_count - Abs(node1.Value.value_win)), -1), 1);
                });
                for (int j = limited + 1; j < layer.Count; j++)
                    lock (DataSet) { if (DataSet.ContainsKey(layer[j].Key)) DataSet.Remove(layer[j].Key); }
                layer.RemoveRange(limited + 1, layer.Count - limited - 1);
            });
        }
        /// <summary>
        /// 清空搜索树
        /// </summary>
        public void Clear()
        {
            DataSet.Clear();
            foreach (var item in LayerModel)
                item.Clear();
        }
        /// <summary>
        /// 这个用于创建或者得到节点的
        /// </summary>
        /// <param name="game">当前棋盘</param>
        /// <returns>该棋盘对应的元组</returns>
        public KeyValuePair<NodeData, UCTNode> GetOrInsertNode(game_info_t game)
        {
            try
            {
                lock (DataSet)
                {
                    var Key = new NodeData(game);
                    UCTNode Value = null;
                    bool existed = DataSet.TryGetValue(Key, out Value);
                    if (!existed)
                    {
                        Value = new UCTNode();
                        Value.Update(Key.forbidpos);
                        DataSet.Add(Key, Value);
                        LayerModel[Key.moves - 1].Add(new KeyValuePair<NodeData, UCTNode>(Key, Value));
                    }
                    return new KeyValuePair<NodeData, UCTNode>(Key, Value);
                }
            }
            catch (Exception e)
            {

                Console.Error.WriteLine(e.StackTrace);
                return default(KeyValuePair<NodeData, UCTNode>);
            }

        }

        public int NodeCounts { get { return (DataSet.Count); } }
    }
    /// <summary>
    /// UCT搜索树
    /// </summary>
    partial class UCT : UCTStorge
    {
        /*最大允许2M个经验节点*/
        public int MAX_UCT_NODE = 2097152;
        /*最大允许6000次不清理节点的自对弈*/
        public int MAX_UCT_SEARCH_PER_EPORCH = 6000;
        private int SearchDepth = 422;
        private float Cpuct = 1.0f;
        private float DefaultUCB = 1.0f;
        private Random random_dev = new Random(0x76532FFD);
        public UCT(int SearchDepth = 422) : base(SearchDepth) { this.SearchDepth = SearchDepth; }
        public bool EnableRecord { get; set; }
        public bool RLMode { get; set; } = false;
        public NN_Model Model { get; set; }
        /*分析棋盘，用以返回最后的评分（估算）*/
        public float Analysis(game_info_t game_root_state)
        {
            return GetorComputeScore(game_root_state, true);
        }
        /*自对弈经验累积*/
        public void ReinforceLearning(int Training_Time, TimeType timetype)
        {
            var totaltime = 0;
            var TrainingTime = Training_Time;
            bool TimeRemained = true;
            string pos_char = "";
            game_info_t game = AllocateGame();
            GameTimer timer = new GameTimer();
            timer.Reset();
            ThreadPool.SetMaxThreads(WorkerCount, WorkerCount);
            /*经验回放机制*/
            List<KeyValuePair<NodeData, UCTNode>> ExpRoot = new List<KeyValuePair<NodeData, UCTNode>>(SearchDepth);
            /*自对弈过程*/
            while (TimeRemained)
            {
                /*重置搜索空间*/
                Clear();
                InitializeBoard(ref game);
                Stone color = S_BLACK;
                int pos = PASS;
                bool resign = false;
                var play_depth = 422;
                var pass_count = 0;
                /*自对弈开始*/
                do
                {
                    pos = Search(game, color, random_dev);
                    Console.WriteLine("genmove " + ((color == S_BLACK) ? "b" : (color == S_WHITE) ? "w" : "e"));
                    if (pos != RESIGN && pass_count < 2)
                    {
                        PutStone(game, pos, color);
                        ExpRoot.Add(GetOrInsertNode(game));
                    }
                    else if (pos == PASS)
                        pass_count++;
                    else
                        resign = true;
                    color = FLIP_COLOR(color);
                    PrintBoard(game);
                    IntegerToString(pos, out pos_char);
                    Console.WriteLine("=" + pos_char);
                    play_depth--;
                } while (play_depth > 0 && !resign && pass_count < 2);
                /*经验回放过程*/
                for (int i = ExpRoot.Count - 1; i >= 1; i--)
                {
                    var ppos = GetPureBoardPos(ExpRoot[i].Key.pos);
                    ExpRoot[i - 1].Value.value_win_node[ppos] = ExpRoot[i].Value.value_win;
                    ExpRoot[i - 1].Value.visit_count_node[ppos] = ExpRoot[i].Value.visit_count;
                    ExpRoot[i - 1].Value.visit_count = 0;
                    ExpRoot[i - 1].Value.value_win = 0;
                    for (var j = 0; j < pure_board_max; j++)
                    {
                        ExpRoot[i - 1].Value.visit_count -= ExpRoot[i - 1].Value.visit_count_node[j];
                        ExpRoot[i - 1].Value.value_win += ExpRoot[i - 1].Value.value_win_node[j];
                    }
                }
                /*更新Q表信息*/
                ExpRoot.AsParallel().ForAll((KeyValuePair<NodeData, UCTNode> node) => { node.Value.Update(); });
                TrainAndSave();
                /*训练NN*/
                //Training();
                if (EnableRecord)
                {
                    SGF sgfrec = new SGF();
                    sgfrec.Record(game);
                    sgfrec.Save(RL_SGF_NM);
                }
                /*保存Q表*/
                //Qtable.Save();
                /*显示所用的时间*/
                Console.Error.Write("Total Cost Time:");
                switch (timetype)
                {
                    case TimeType.second:
                        totaltime = timer.elapsed_seconds();
                        TimeRemained = (TrainingTime >= totaltime);
                        Console.Error.Write(totaltime + "Seconds");
                        break;
                    case TimeType.minute:
                        totaltime = timer.elapsed_minutes();
                        TimeRemained = (TrainingTime >= totaltime);
                        Console.Error.Write(totaltime + "Minutes");
                        break;
                    case TimeType.hour:
                        totaltime = timer.elapsed_hours();
                        TimeRemained = (TrainingTime >= totaltime);
                        Console.Error.Write(totaltime + "Hours");
                        break;
                    default:
                        break;
                }
            }
            FreeGame(ref game);
        }
        /*搜索走子*/
        public int Genmove(game_info_t game_root_state, Stone player_color)
        {
            return Search(game_root_state, player_color, random_dev);
        }
        public void SetMode(SEARCH_MODE mode, int value = 10000)
        {
            SearchMode = mode;
            switch (mode)
            {
                case CONST_PLAYOUT_MODE:
                    SearchMode = mode;
                    MaxSearchCount = value;
                    break;
                case CONST_TIME_MODE:
                    SearchMode = mode;
                    MaxThinkingMs = value;
                    break;
                default:
                    break;
            }
        }
        public void SetMode(SEARCH_MODE mode, int value_time = 10000, int value_playsout = 10000)
        {
            switch (mode)
            {
                case CONST_PLAYOUT_TIME_MODE:
                    SearchMode = mode;
                    MaxSearchCount = value_playsout;
                    MaxThinkingMs = value_time;
                    break;
                default:
                    break;
            }
        }
        public void SetThreads(uint value)
        {
            WorkerCount = Min(Environment.ProcessorCount - 1, Max((int)value, 4));
        }
        public void InitSearch(int threads = 4, int depth = 422, float cpuct = 1.0f, float defaultUCB = 1.0f)
        {
            WorkerCount = Min(Environment.ProcessorCount - 1, Max(threads, 4));
            SearchDepth = Min(MAX_MOVES - 1, Max(depth, 400));
            Cpuct = Max(cpuct, 0);
            DefaultUCB = Max(defaultUCB, 0);

        }
    }
    partial class UCT : UCTStorge
    {
        DQN_DNN_Class DNN = new DQN_DNN_Class();
        public void NNInit()
        {
            LoadOrCreate();
        }
        void LoadOrCreate()
        {
            DNN.SetupModel(Model);
            if (!DNN.Load())
                DNN.CreateModel(CNTK.DeviceDescriptor.UseDefaultDevice(), Model);
        }
        void EvalNode(KeyValuePair<NodeData, UCTNode> node)
        {
            DNN.EvalNode(node);
        }
        void EvalLoop()
        {
            DNN.Eval();
        }
        void EvalDisabled()
        {
            DNN.EvalDisabled();
        }
        void TrainAndSave()
        {
            DNN.Train(DataSet.ToList());
            DNN.Save();
        }
    }
    /// <summary>
    /// UCT搜索树（传统搜索模块）
    /// </summary>
    partial class UCT : UCTStorge
    {
        private SEARCH_MODE SearchMode;
        /// <summary>
        /// 最大搜索次数
        /// </summary>
        private int MaxSearchCount = 10000;
        /// <summary>
        /// 最大思考时间
        /// </summary>
        private int MaxThinkingMs = 10000;
        /// <summary>
        /// 搜索中计时器如果到点了会吧值置为真
        /// </summary>
        private bool TimeUsedUp { get { lock (MyTimer) return MyTimer.elapsed() >= MaxThinkingMs; } }
        /// <summary>
        /// 搜索次数累计
        /// </summary>
        private volatile int Search_Counter = 0;
        /// <summary>
        /// 是否可以退出搜索了
        /// </summary>
        private bool Enable_to_quit
        {
            get
            {
                bool quit = false;
                if (CONST_PLAYOUT_MODE == SearchMode || SearchMode == CONST_PLAYOUT_TIME_MODE)
                    quit = quit || (Search_Counter >= MaxSearchCount);
                if (CONST_TIME_MODE == SearchMode || SearchMode == CONST_PLAYOUT_TIME_MODE)
                    quit = quit || TimeUsedUp;
                quit = quit || OverflowNodeCounts;
                return quit;
            }
        }
        /// <summary>
        /// 定时器
        /// </summary>
        private GameTimer MyTimer = new GameTimer();
        private string DefaultFileName;

        /// <summary>
        /// 对当前节点搜索出一个解
        /// </summary>
        /// <param name="game">当前棋盘</param>
        /// <param name="player">当前搜索方</param>
        /// <param name="random">随机数</param>
        /// <returns>坐标</returns>
        private int Search(game_info_t game, Stone player, Random random)
        {
            int count = 0;

            /*当前盘面对应的节点*/
            KeyValuePair<NodeData, UCTNode> CurrentRoot = default(KeyValuePair<NodeData, UCTNode>);
            /*扩展数列*/
            //List<ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>>> ExpandedList;
            ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>, int>[] ExpandedList = null;
            /*存储输出时用的坐标*/
            int pos = PASS;
            /*搜索路径记录，好返回结果*/
            List<KeyValuePair<NodeData, UCTNode>> NeedsToUpdate = new List<KeyValuePair<NodeData, UCTNode>>();
            /*扩展根节点*/
            /* List<ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>>>*/
            void Expand(bool NewNodes = true)
            {
                CurrentRoot = GetOrInsertNode(game);
                var pos_ucbs = Expectation(CurrentRoot.Value, game, player);
                var next_color = FLIP_COLOR(player);
                if (NewNodes || ExpandedList == null)
                {
                    ExpandedList = new ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>, int>[pos_ucbs.Count];
                    Parallel.For(0, pos_ucbs.Count, (int i) =>
                    {
                        ExpandedList[i].Item1 = new game_info_t();
                        CopyGame(ref ExpandedList[i].Item1, game);
                        PutStone(ExpandedList[i].Item1, pos_ucbs[i].Item1, player);
                        ExpandedList[i].Item2 = next_color;
                        ExpandedList[i].Item3 = pos_ucbs[i].Item2;
                        ExpandedList[i].Item4 = GetOrInsertNode(ExpandedList[i].Item1);
                        if (!ExpandedList[i].Item4.Value.Evaled)
                            EvalNode(ExpandedList[i].Item4);
                    });
                }
                else
                {
                    Parallel.ForEach(pos_ucbs, (ValueTuple<int, float> pos_ucbs_node) =>
                    {
                        var thispos = pos_ucbs_node.Item1;
                        var index = Array.FindIndex(ExpandedList, (ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>, int> v_data) =>
                        {
                            return v_data.Item1.record[v_data.Item1.moves - 1].pos == thispos;
                        });
                        ExpandedList[index].Item3 = pos_ucbs_node.Item2;
                    });
                }
                Array.Sort(ExpandedList, (ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>, int> A, ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>, int> B) =>
                {
                    var cmp = A.Item3 - B.Item3;
                    return cmp > 0 ? -1 : cmp < 0 ? 1 : 0;
                });
                Parallel.For(0, pos_ucbs.Count, (int i) =>
                {
                    ExpandedList[i].Item5 = Max(WorkerCount - i + 1, 1);
                });
            }
            /*是否可以投降*/
            bool CanPutResign(int moves)
            {
                return ((pure_board_max / 2 - 10) < moves);
            }
            /*得到最佳走子*/
            int GetPos(KeyValuePair<NodeData, UCTNode> currentRoot, game_info_t game_root_state, Stone player_color)
            {
                //var pos = SelectMaxChild(currentRoot, game_root_state, player_color, *mts[0], Cpuct);
                pos = SelectBestPos(currentRoot.Value, game_root_state, player_color, random);
                if (CanPutResign(currentRoot.Key.moves))
                {
                    var pure_pos = GetPureBoardPos(pos);
                    var pass_wp = (float)currentRoot.Value.value_win_node[pure_board_max] / (currentRoot.Value.visit_count_node[pure_board_max] + 1);
                    var best_wp = currentRoot.Value.value_win_node[pure_pos] / (currentRoot.Value.visit_count_node[pure_pos] + 1);
                    if (pass_wp >= PASS_THRESHOLD && (game_root_state.record[game_root_state.moves - 1].pos == PASS))
                    {
                        pos = PASS;
                    }
                    else if (game_root_state.moves >= MAX_MOVES)
                    {
                        pos = PASS;
                    }
                    else if (game_root_state.record[game_root_state.moves - 1].pos == PASS && game_root_state.record[game_root_state.moves - 3].pos == PASS)
                    {
                        pos = PASS;
                    }
                    else if (best_wp <= RESIGN_THRESHOLD)
                    {
                        pos = RESIGN;
                    }
                }
                return pos;
            };
            /*进行N次模拟*/
            void SimulateN(ValueTuple<game_info_t, Stone, float, KeyValuePair<NodeData, UCTNode>, int> ThreadData)
            {
                Volatile.Write(ref count, Volatile.Read(ref count) + 1);
                var FeedBackPath = new List<KeyValuePair<NodeData, UCTNode>>(SearchDepth);
                /*棋盘*/
                game_info_t game_t = new game_info_t();
                while (!Enable_to_quit)
                {
                    /*初始化*/
                    CopyGame(ref game_t, ThreadData.Item1);
                    var pass_count = 0;
                    var maxium_move = SearchDepth - game_t.moves;
                    var color = ThreadData.Item2;
                    var current_root = ThreadData.Item4;
                    var pre_node = current_root;
                    current_root.Value.visit_count++;
                    FeedBackPath.Add(current_root);
                    /*前向搜索,先做扩展*/
                    while (maxium_move != 0 && (pass_count < 2))
                    {
                        maxium_move--;
                        /*前一个节点*/
                        pre_node = current_root;
                        /*选择走子，如果要使用Sarsa这个地方要加入优势评估函数*/
                        var next_pos = SelectBestPos(current_root.Value, game_t, color, random);
                        if (next_pos == PASS) { pass_count++; continue; }
                        var ppos = GetPureBoardPos(next_pos);
                        /*得到纯棋盘坐标*/
                        pre_node.Value.visit_count_node[ppos]++;
                        /*落子*/
                        PutStone(game_t, next_pos, color);
                        /*模拟走子（走一步）并入栈*/
                        current_root = GetOrInsertNode(game_t);
                        /*加入搜索队列*/
                        FeedBackPath.Add(current_root);
                        /*如果有NN估值器，则使用NN*/
                        if (!current_root.Value.Evaled)
                            EvalNode(current_root);
                        /*若要使用Sarsa，该地方要更新权值(Online)*/
                        if (RLMode)
                        {
                            current_root.Value.Update(current_root.Key.forbidpos, 0.05f);
                            /*优势计算*/
                            AdvantageCalc(ref pre_node, game_t);
                        }
                        /*记录虚着次数*/
                        pass_count = (GetLastPos(game_t) == PASS) ? (pass_count + 1) : pass_count;
                        /*反转颜色*/
                        color = FLIP_COLOR(color);
                    }
                    /*值回传:由后往前传回值*/
                    var Socre = GetorComputeScore(game_t, false);
                    var winner = Socre >= 0 ? S_BLACK : S_WHITE;
                    /*胜利计数*/
                    var value = ((Socre > 0 ? 1 : Socre == 0 ? 0 : -1));
                    for (var j = 0; j < FeedBackPath.Count; j++)
                    {
                        int faction = (FeedBackPath[j].Key.color != S_WHITE ? 1 : -1);
                        lock (FeedBackPath[j].Value)
                        {
                            FeedBackPath[j].Value.value_win += value * faction;
                            if (j + 1 < FeedBackPath.Count)
                            {
                                var fpos = FeedBackPath[j + 1].Key.pos;
                                var ppos = GetPureBoardPos(fpos);
                                ppos = ppos < 0 ? pure_board_max : ppos;
                                int faction_1 = (FeedBackPath[j + 1].Key.color != S_WHITE ? 1 : -1);
                                FeedBackPath[j].Value.value_win_node[ppos] += faction * faction_1 * value;
                            }
                        }
                    }
                    /*Sarsa在线更新(需要设定)*/
                    foreach (var node in FeedBackPath)
                    {
                        lock (node.Value)
                            node.Value.Update(node.Key.forbidpos, 0.15f);
                    }
                    lock (NeedsToUpdate)
                        NeedsToUpdate.AddRange(FeedBackPath);
                    /*离线更新*/
                    //if (!RLMode)
                    //    lock (NeedsToUpdate)
                    //        NeedsToUpdate.AddRange(FeedBackPath);
                    //FeedBackPath.Clear();
                    Search_Counter++;
                }
                Volatile.Write(ref count, Volatile.Read(ref count) - 1);
            }
            /*仿真信息回传*/
            void FeedBackNodes()
            {
                Parallel.For(0, ExpandedList.Length, (int i) =>
                {
                    var ppos = GetPureBoardPos(ExpandedList[i].Item4.Key.pos);
                    if (CurrentRoot.Key.color == FLIP_COLOR(ExpandedList[i].Item4.Key.color))
                    {
                        CurrentRoot.Value.value_win_node[ppos] -= ExpandedList[i].Item4.Value.value_win;
                        CurrentRoot.Value.value_win -= ExpandedList[i].Item4.Value.value_win;
                    }
                    else
                    {
                        CurrentRoot.Value.value_win_node[ppos] += ExpandedList[i].Item4.Value.value_win;
                        CurrentRoot.Value.value_win += ExpandedList[i].Item4.Value.value_win;
                    }
                    CurrentRoot.Value.visit_count_node[ppos] += ExpandedList[i].Item4.Value.visit_count;
                    CurrentRoot.Value.visit_count += ExpandedList[i].Item4.Value.visit_count;
                });
                CurrentRoot.Value.Update(CurrentRoot.Key.forbidpos);
            }
            /*当前的搜索深度*/
            var current_depth = game.moves;

            /*重置搜索计数*/
            Search_Counter = 0;
            /*是否达到溢出条件（溢出之后便可以退出搜索）*/
            bool Overflow = false;
            /*设置最大的搜索线程数量*/
            ThreadPool.SetMaxThreads(WorkerCount, WorkerCount);
            /*重置定时器*/
            MyTimer.Reset();
            /*依据当前盘面扩展棋盘*/
            //ExpandedList = Expand();
            /*搜索开始*/
            Expand(false);
            /*多线程模拟N次*/
            var counts = Min(ExpandedList.Length, WorkerCount);
            if (counts > 0)
                Parallel.For(0, Max(counts, 1), (int i) =>
                {

                    if (i == counts - 1)
                    {
                        SimulateN(ExpandedList[i]);
                        EvalDisabled();
                    }
                    else if (i == counts - 2)
                        EvalLoop();
                    else
                        SimulateN(ExpandedList[i]);
                });
            /*更新所有搜索产生的节点*/
            if (NeedsToUpdate.Count > 0)
            {
                NeedsToUpdate.AsParallel().ForAll((KeyValuePair<NodeData, UCTNode> item) => { item.Value.Update(); });
                NeedsToUpdate.Clear();
            }
            /*子节点的值返回根节点*/
            FeedBackNodes();
            /*清理超出界限的节点*/
            ClearOutbandNodes();
            /*是否是满了或者是次数到了*/
            var ms_time = MyTimer.elapsed();
            /*获取走子点*/
            pos = GetPos(CurrentRoot, game, player);
            /*清理超出界限的节点*/
            //ClearOutbandNodes();
            Console.Error.WriteLine("Step Count:" + Search_Counter);
            Console.Error.WriteLine("Time Cost :" + ms_time + "ms");
            Console.Error.WriteLine("UCT Nodes :" + NodeCounts);
            /*返回走子点*/
            return pos;
        }
        /// <summary>
        /// 是否超过了节点数量限制
        /// </summary>
        /// <returns>是否超过了这个阈值</returns>
        private bool OverflowNodeCounts { get { lock (DataSet) return DataSet.Count >= MaxiumNodes; } }
        /// <summary>
        /// 计算期望值使用U+Q
        /// </summary>
        /// <param name="current">当前节点位置</param>
        /// <param name="game">当前节点对应的棋盘</param>
        /// <param name="player_color">玩家所属方</param>
        /// <returns>UCB值与合法坐标点的评估</returns>
        private List<ValueTuple<int, float>> Expectation(UCTNode current, game_info_t game, Stone player_color)
        {
            List<ValueTuple<int, float>> ExpectationPoses = new List<ValueTuple<int, float>>();
            /*每次运算的时候+1或者-1不变，其值已经加权平均已经包含在分子里了*/
            float total_win = current.value_win;
            /*这个是总访问次数是分母*/
            float total_visit_count = current.visit_count;
            float Expand_Up_Elem = (float)Sqrt(2 * Log(total_visit_count));
            /*计算UCB值*/
            for (var p_pos = 0; p_pos < pure_board_max; p_pos++)
            {
                /*得到该点在全棋盘上的坐标（因为做过padding所以会很大）*/
                var f_pos = GetFullBoardPos(p_pos);
                /*是否合法判断*/
                var islegal = IsLegal(game, f_pos, player_color) && game.candidates[f_pos];
                /*压入合法的点并且ucb值大于投降阈值*/
                if (islegal)
                {
                    /*使用UCB公式计算*/
                    float ucb = 0;
                    ucb = current.P[p_pos] * current.V + Cpuct * (Expand_Up_Elem / (current.visit_count_node[p_pos] + 1.0f));
                    /*如果不是投降阈值，则是合法走步*/
                    if (ucb > RESIGN)
                        ExpectationPoses.Add(new ValueTuple<int, float>(f_pos, ucb));
                }
            }
            //if (ExpectationPoses.Count > 0)
            //    ExpectationPoses.Sort((ValueTuple<int, float> data1, ValueTuple<int, float> data2) => { var cmp = data2.Item2 - data1.Item2; return cmp > 0 ? 1 : cmp < 0 ? -1 : 0; });
            return ExpectationPoses;
        }
        /// <summary>
        /// 使用U+Q公式选择最大走子
        /// </summary>
        /// <param name="current">当前节点位置</param>
        /// <param name="game">当前节点对应的棋盘</param>
        /// <param name="player_color">玩家所属方</param>
        /// <param name="random">随机数</param>
        /// <returns></returns>
        private int SelectBestPos(UCTNode current, game_info_t game, Stone player_color, Random random)
        {
            return GetMaxPos(Expectation(current, game, player_color), random);
        }
        /// <summary>
        /// 选择价值最大的位置
        /// </summary>
        /// <param name="legal_pos_array">筛选过节点的合法数组</param>
        /// <param name="random">随机数发生器</param>
        /// <returns>最好的那个坐标</returns>
        private int GetMaxPos(List<ValueTuple<int, float>> legal_pos_array, Random random)
        {
            /*如果没有可以走的点*/
            if (legal_pos_array.Count == 0) return PASS;
            /*坐标排序*/
            legal_pos_array.Sort((ValueTuple<int, float> A, ValueTuple<int, float> B) => { return (B.Item2 - A.Item2) > 0 ? 1 : (B.Item2 - A.Item2) < 0 ? -1 : 0; });
            /*得到最大值*/
            var value_max = legal_pos_array.Max((ValueTuple<int, float> data) => { return data.Item2; });
            /*统计最大值的数量*/
            var max_value_count = 0;
            for (int i = 0; i < legal_pos_array.Count; i++)
                max_value_count += (value_max == legal_pos_array[i].Item2) ? 1 : 0;
            /*找出最大的那个，并返回坐标*/
            var pos = legal_pos_array[random.Next(0, max_value_count - 1)].Item1;
            return pos;
        }
        /// <summary>
        /// 得到最近下棋的位置
        /// </summary>
        /// <param name="game">当前棋盘</param>
        /// <returns>位置</returns>
        private int GetLastPos(game_info_t game)
        {
            return game.record[game.moves - 1].pos;
        }
        /// <summary>
        /// 优势计算(Online决策的目标函数)
        /// </summary>
        /// <param name="pre_root">前一个节点</param>
        /// <param name="cur_game">当前游戏</param>
        private void AdvantageCalc(ref KeyValuePair<NodeData, UCTNode> pre_root, game_info_t cur_game)
        {
            var value = (CalculateScore(cur_game, true) / (float)PURE_BOARD_MAX + cur_game.capture_num[(int)S_BLACK] - cur_game.capture_num[(int)S_WHITE]) * cur_game.moves / MAX_MOVES;
            value = pre_root.Key.color == S_BLACK ? value : pre_root.Key.color == S_WHITE ? -value : 0;
            pre_root.Value.SetAdvantagePos(cur_game.record[cur_game.moves - 1].pos, value, pre_root.Key.forbidpos);
        }
        /// <summary>
        /// 得到文件名(存储搜索树的节点的文件名)
        /// </summary>
        /// <returns>文件名称</returns>
        private string GetFileName()
        {
            DefaultFileName = "";
            DefaultFileName += Path;
            DefaultFileName += RL_NM;
            DefaultFileName += Model_expand[(int)NN_Model.None_NN];
            return DefaultFileName;
        }
    }
}
