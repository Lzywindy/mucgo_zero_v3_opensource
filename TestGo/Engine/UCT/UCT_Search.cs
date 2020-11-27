using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
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
    using System.Diagnostics;
    using System.Threading;
    using static Board;
    using static ConstValues;
    /// <summary>
    /// UCT搜索算法
    /// </summary>
    public class UCT_Search : UCT_UpdatePolicy
    {
        public delegate void SelfPlayBoardStateOut(ref Board game);
        public delegate void PathSave(ref List<(QUCTNode node, int ppos)> path);
        #region 公共参数
        /// <summary>
        /// CPU负载率设置
        /// </summary>
        public static float CpuLoad = 2f;
        /// <summary>
        /// 最大UCT节点数目
        /// </summary>
        public static int MaxiumCacheNodes = 2097152;
        /// <summary>
        /// 最大不超过的搜索次数
        /// </summary>
        public static int MaxiumSearchCounts = 200;
        /// <summary>
        /// 可以控制搜索发散程度
        /// </summary>
        public static int VitrualLoss = 3;
        /// <summary>
        /// 最大搜索次数
        /// </summary>
        public static int MaxSearchCount = 200;
        /// <summary>
        /// 最大思考时间
        /// </summary>
        public static int MaxThinkingMs = 1000;
        /// <summary>
        /// UCT搜索树探索因子
        /// </summary>
        public static float Cpuct = 1.0f;
        /// <summary>
        /// 最大内存使用量
        /// </summary>
        public static long MaxiumMemeryUseGigaByte = 10;
        /// <summary>
        /// 自对弈中最大单轮模拟次数
        /// </summary>
        public static uint MaxSelfplaySimulate = 1600;
        #endregion
        #region 搜索用的参数      
        /// <summary>
        /// 总共模拟的次数
        /// </summary>
        protected volatile int TotalSimulateCount = 0;
        /// <summary>
        /// 最大思考用时
        /// </summary>
        //private readonly int CurrentThinkingMs = 1000;
        /// <summary>
        /// 搜索次数累计
        /// </summary>
        private volatile int Search_Counter = 0;

        protected int MaxThreads { get { return Math.Min((int)(Environment.ProcessorCount * CpuLoad / 3 * 2), 20); } }
        /// <summary>
        /// 对弈路径
        /// </summary>
        protected List<(QUCTNode node, int ppos)> PlayPath = new List<(QUCTNode node, int ppos)>();
        protected QUCTNode CurrentRoot;
        /// <summary>
        /// 最大搜索深度
        /// </summary>
        private readonly int SearchDepth = EngineBoardMax;
        private Po_info_t po_Info = new Po_info_t();
        private Statistic_t[] statistic_s = Statistic_t.CreateStatistic();
        protected volatile int NodeCount = 0;
        /// <summary>
        /// 构造函数
        /// </summary>
        public UCT_Search(int depth)
        {
            DNN = new DQN_DNN_Class();
            SearchDepth = depth;
        }
        /// <summary>
        /// 对当前节点搜索出一个解
        /// </summary>
        /// <param name="game">当前棋盘</param>
        /// <param name="player">当前搜索方</param>
        /// <param name="random">随机数</param>
        /// <returns>坐标</returns>
        protected int Search(Board game, QUCTNode ParentNode = null, bool Selfplay = true)
        {
            #region 搜索初始化设定
            /*重置搜索计数*/
            NodeCount = 0;
            Search_Counter = 0;
            DNN.EvalStart();
            //当前盘面对应的节点
            var CurrentRoot = CreateRootOrVisit(game, ParentNode);
            #endregion
            /*存储输出时用的坐标*/
            int pos = PASS;
            /*扩展根节点*/
            if (DebugModel && StepModel) Console.Error.WriteLine("Timer Start");
            if (DebugModel && StepModel) Console.Error.WriteLine("Running Simulation");
            try
            {
                Parallel.For(0, MaxThreads, (int i) =>
                      {
                          if (i < Environment.ProcessorCount)
                              Simulate(game, CurrentRoot, MaxiumSearchCounts, Selfplay, useNN);
                          else
                              SimulateMCTS(game, CurrentRoot, MaxiumSearchCounts, 0.2f, useNN);
                      });
            }
            catch (Exception) { }
            if (DebugModel && StepModel) Console.Error.WriteLine("Simulation Finished");
            DNN.Clear();
            pos = GetPos(CurrentRoot, game, randomData, false);
            if (DebugModel) Console.Error.WriteLine($"Total Simulate:{Search_Counter}");
            if (DebugModel) Console.Error.WriteLine($"Total Nodes:{NodeCount}");

            NodeCount = 0;
            /*返回走子点*/
            return pos;
        }

        protected QUCTNode CreateRootOrVisit(Board game, QUCTNode CurrenRoot = null, bool UseNN = true)
        {
            if (CurrenRoot == null)
                CurrenRoot = new QUCTNode(game);
            else
                CurrenRoot.N++;
            if (UseNN && !CurrenRoot.Evaled)
                EvalNode(CurrenRoot);
            return CurrenRoot;
        }
        /// <summary>
        /// 得到最佳的点位
        /// </summary>
        /// <param name="currentRoot">根节点</param>
        /// <param name="game">当前评估的棋局</param>
        /// <param name="random">随机可选点产生</param>
        /// <param name="Selfplay">自对弈</param>
        /// <returns>模拟出来决策最好的位置</returns>
        private static int GetPos(QUCTNode currentRoot, Board game, Random random, bool Selfplay)
        {
            bool enable_to_resigned = false;
            int MaxMove = 0;
            if (Selfplay)
                MaxMove = SELFPLAY_MINSTEP;
            else
                MaxMove = PLAY_MINSTEP;
            enable_to_resigned = (currentRoot.Moves >= MaxMove) || game.GameOver;
            //currentRoot.EnabledPos = game.EnabledPos;
            int pos = currentRoot.MaxiumAction(Selfplay, true);
            var pass_wp = currentRoot.PosValue(currentRoot.PassMove());
            var best_wp = currentRoot.PosValue(pos);
            pos = game.ToBoardPos(pos, true);
            if (enable_to_resigned)
            {
                if ((pass_wp >= PASS_THRESHOLD || (game.LastestPassMove(3)) && game.LastestPassMove(1)))
                {
                    pos = PASS;
                }
                else if (game.Moves >= MAX_MOVES)
                {
                    pos = PASS;
                }
                else if (best_wp <= RESIGN_THRESHOLD)
                {
                    pos = RESIGN;
                }
            }
            return pos;
        }
        public float Analysis(Board game_root_state)
        {
            (sbyte[] area, float score) statistic = default((sbyte[], float));
            statistic.area = new sbyte[pure_board_max];
            Array.Clear(statistic.area, 0, pure_board_max);
            po_Info.Clear();
            NodeCount = 0;
            Search_Counter = 0;
            var Board = game_root_state.Clone();
            QUCTNode node = new QUCTNode(Board);
            //return game_root_state.Score_Final;
            Statistic_t.ClearAll(ref statistic_s);
            int Count = 0;
            Parallel.For(0, Environment.ProcessorCount, (int i) =>
                {
                    try
                    {
                        var PassPos = Board.BoardSize;
                        int EvalSteps = MAX_MOVES << 1 - Board.Moves;
                        while (Thread.VolatileRead(ref Count) < 100 && !EndSearchForMemeryNotEnough())
                        {
                            Simulate_Analysis(Board, node);
                            Thread.VolatileWrite(ref Count, Thread.VolatileRead(ref Count) + 1);
                        }
                    }
                    catch (Exception) { }
                });
            Statistic_t.CalculateOwner(ref statistic_s, Board.CurrentPlayer, Count);
            //Statistic_t.NormalizeAllStatistic(ref statistic_s);
            int black = 0, white = 0;
            for (short pos = 0; pos < pure_board_max; pos++)
            {
                var sign = Math.Sign(statistic_s[pos].colors[1] - statistic_s[pos].colors[2]);
                if (sign > 0)
                {
                    black++;
                    statistic.area[pos] = 1;
                }
                else if (sign < 0)
                {
                    white++;
                    statistic.area[pos] = -1;
                }
            }
            statistic.score = black - white;
            (game_root_state as Board).UpdateScoreEsimate(statistic);
            node.ClearEdges();
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return statistic.score;
        }
        #endregion
        public SelfPlayBoardStateOut MyEventOut { get; set; }
        #region UCT搜索模拟
        /// <summary>
        /// 自对弈使用的模拟函数
        /// </summary>
        private void Simulate_Selfplay(Board rootBoard, QUCTNode rootNode)
        {
            Queue<short> PassMove = new Queue<short>();
            List<(QUCTNode state, int action)> FeedBackPath = new List<(QUCTNode state, int action)>();
            int EvalSteps = MAX_MOVES - rootBoard.Moves;
            Board game_t = null;
            (rootBoard as Board).Copy(ref game_t);
            QUCTNode current_root = rootNode;
            short next_pos = rootNode.PassMove();
            bool EndGameMode = current_root.EndGameModel;
            Search_Counter++;
            while (EvalSteps > 0 && PassMove.Count <= 2)
            {
                EndGameMode = current_root.EndGameModel;
                next_pos = current_root.MaxiumAction(true, useNN && (!EndGameMode), false);
                if (next_pos == current_root.PassMove())
                    PassMove.Enqueue(next_pos);
                else if (PassMove.Count > 0)
                    PassMove.Dequeue();
                current_root.N += VitrualLoss;
                Thread.VolatileWrite(ref current_root.N_sa[next_pos], Thread.VolatileRead(ref current_root.N_sa[next_pos]) + VitrualLoss);
                NodeCount++;
                game_t.PutStone_UCT(next_pos);
                current_root = current_root.ExpandOrVisit(game_t, next_pos);
                if (useNN && (!EndGameMode) && !current_root.Evaled)
                    EvalNode(current_root, true);
                FeedBackPath.Add((current_root, next_pos));
                EvalSteps--;
                if (!EndGameMode)
                    SarsaLambdaUpdateByPath(FeedBackPath);
            }
            FeedBackPath.Add((current_root, current_root.ActionsLength - 1));
            for (int index = 0; index < FeedBackPath.Count; index++)
            {
                var node = FeedBackPath[index].state;
                var action = FeedBackPath[index].action;
                node.N -= VitrualLoss;
                Thread.VolatileWrite(ref current_root.N_sa[action], Thread.VolatileRead(ref current_root.N_sa[action]) - VitrualLoss);
            }
            QLearningUpdateByPath(FeedBackPath);

        }
        private void Simulate_Rating(Board rootBoard, QUCTNode rootNode)
        {
            Queue<short> PassMove = new Queue<short>();
            List<(QUCTNode state, int action)> FeedBackPath = new List<(QUCTNode state, int action)>();
            int EvalSteps = MAX_MOVES - rootBoard.Moves;
            Board game_t = rootBoard.Clone();
            QUCTNode current_root = rootNode;
            short next_pos = rootNode.PassMove();
            bool EndGameMode = current_root.EndGameModel;
            Search_Counter++;
            while (EvalSteps > 0 && PassMove.Count <= 2)
            {
                EndGameMode = current_root.EndGameModel;
                next_pos = current_root.MaxiumAction(true, false, false);
                if (next_pos == current_root.PassMove())
                    PassMove.Enqueue(next_pos);
                else if (PassMove.Count > 0)
                    PassMove.Dequeue();
                current_root.N += VitrualLoss;
                Thread.VolatileWrite(ref current_root.N_sa[next_pos], Thread.VolatileRead(ref current_root.N_sa[next_pos]) + VitrualLoss);
                NodeCount++;
                game_t.PutStone_UCT(next_pos);
                current_root = current_root.ExpandOrVisit(game_t, next_pos);
                FeedBackPath.Add((current_root, next_pos));
                EvalSteps--;
            }
            FeedBackPath.Add((current_root, current_root.ActionsLength - 1));
            for (int index = 0; index < FeedBackPath.Count; index++)
            {
                var node = FeedBackPath[index].state;
                var action = FeedBackPath[index].action;
                node.N -= VitrualLoss;
                Thread.VolatileWrite(ref current_root.N_sa[action], Thread.VolatileRead(ref current_root.N_sa[action]) - VitrualLoss);
            }
            QLearningUpdateByPath(FeedBackPath);

        }
        /// <summary>
        /// 估算盘面使用的模拟函数
        /// </summary>
        private void Simulate_Analysis(Board rootBoard, QUCTNode rootNode)
        {
            Queue<short> PassMove = new Queue<short>();
            List<(QUCTNode state, int action)> FeedBackPath = new List<(QUCTNode state, int action)>(SearchDepth);
            int EvalSteps = MAX_MOVES - rootBoard.Moves;
            Board game_t = rootBoard.Clone();
            QUCTNode current_root = rootNode;
            short next_pos = rootNode.PassMove();
            bool EndGameMode = current_root.EndGameModel;
            while (EvalSteps > 0 && PassMove.Count <= 2)
            {
                EndGameMode = current_root.EndGameModel;
                next_pos = current_root.MaxiumAction(true, false, false);
                if (next_pos == current_root.PassMove())
                    PassMove.Enqueue(next_pos);
                else if (PassMove.Count > 0 && next_pos != current_root.PassMove())
                    PassMove.Dequeue();
                current_root.N += VitrualLoss;
                Thread.VolatileWrite(ref current_root.N_sa[next_pos], Thread.VolatileRead(ref current_root.N_sa[next_pos]) + VitrualLoss);
                game_t.PutStone_UCT(next_pos);
                current_root = current_root.ExpandOrVisit(game_t, next_pos);
                FeedBackPath.Add((current_root, next_pos));
                EvalSteps--;
            }
            FeedBackPath.Add((current_root, current_root.ActionsLength - 1));
            int Winner = Math.Sign(FeedBackPath[FeedBackPath.Count - 1].state.Score_Board);
            for (int index = 0; index < FeedBackPath.Count; index++)
            {
                var node = FeedBackPath[index].state;
                var action = FeedBackPath[index].action;
                node.N -= VitrualLoss;
                Thread.VolatileWrite(ref current_root.N_sa[action], Thread.VolatileRead(ref current_root.N_sa[action]) - VitrualLoss);
            }
            Statistic_t.Statistic(ref statistic_s, (game_t as Board), (sbyte)Math.Sign(game_t.Score_UCT));
        }
        /// <summary>
        /// 对战使用的模拟函数
        /// </summary>
        private void SimulateOnce(Board rootBoard, QUCTNode rootNode, bool Selfplay = false, bool UseNN = true)
        {
            Queue<short> PassMove = new Queue<short>();
            int EvalSteps = MAX_MOVES - rootBoard.Moves;
            List<(QUCTNode state, int action)> FeedBackPath = new List<(QUCTNode state, int action)>(SearchDepth);
            Board game_t = rootBoard.Clone();
            QUCTNode current_root = rootNode;
            short next_pos = rootNode.PassMove();
            while (EvalSteps > 0 && (!(game_t.GameOver && (!Selfplay))) && PassMove.Count <= 2)
            {
                next_pos = current_root.MaxiumAction(Selfplay, UseNN, false);
                if (next_pos == current_root.PassMove())
                    PassMove.Enqueue(next_pos);
                else
                   if (PassMove.Count > 0) PassMove.Dequeue();
                Thread.VolatileWrite(ref current_root.N_sa[next_pos], Thread.VolatileRead(ref current_root.N_sa[next_pos]) + VitrualLoss);
                NodeCount++;
                game_t.PutStone_UCT(next_pos);
                current_root = current_root.ExpandOrVisit(game_t, next_pos);
                if (UseNN && !current_root.Evaled)
                {
                    EvalNode(current_root);
                }
                FeedBackPath.Add((current_root, next_pos));
                EvalSteps--;
                SarsaLambdaUpdateByPath(FeedBackPath);
            }
            FeedBackPath.Add((current_root, current_root.ActionsLength - 1));
            for (int index = 0; index < FeedBackPath.Count; index++)
            {
                var node = FeedBackPath[index].state;
                var action = FeedBackPath[index].action;
                Thread.VolatileWrite(ref current_root.N_sa[action], Thread.VolatileRead(ref current_root.N_sa[action]) - VitrualLoss);
            }
            QLearningUpdateByPath(FeedBackPath);
            Search_Counter++;
        }
        private void SimulateOnce_MCTS(Board rootBoard, QUCTNode rootNode, float weight = 0.1f)
        {
            short PassMove = 0;
            List<(QUCTNode state, int action)> FeedBackPath = new List<(QUCTNode state, int action)>(SearchDepth);
            Board game_t = rootBoard.Clone();
            int EvalSteps = MAX_MOVES - game_t.Moves;
            short next_pos = rootNode.PassMove();
            bool firstMove = true;
            short backpos = 0;
            while (EvalSteps > 0 && (!game_t.GameOver) && PassMove <= 2)
            {
                float[] enabledPos = null;
                if (EvalSteps < EngineBoardMax)
                    enabledPos = Array.ConvertAll(game_t.EnabledPos, (byte data) => (float)data);
                else
                    enabledPos = Array.ConvertAll(game_t.EnabledPos4Analysis, (byte data) => (float)data);
                if (enabledPos.Count((float data) => (data > 0)) < 1)
                {
                    PassMove++;
                    continue;
                }
                for (int index = 0; index < enabledPos.Length - 1; index++)
                {
                    if (enabledPos[index] > 0)
                        enabledPos[index] = enabledPos[index] * (float)randomData.NextDouble();
                }
                EvalSteps--;
                next_pos = (short)Array.IndexOf(enabledPos, enabledPos.Max());
                if (firstMove) backpos = next_pos;
                firstMove = false;
                game_t.PutStone_UCT(next_pos);
            }
            Volatile.Write(ref rootNode.Q_sa[backpos], Volatile.Read(ref rootNode.Q_sa[backpos]) + E_greedy * game_t.Score_UCT * weight);
        }
        /// <summary>
        /// 对根节点进行模拟
        /// </summary>
        private void Simulate(Board rootBoard, QUCTNode rootNode, int maxCounts = 160, bool Selfplay = false, bool UseNN = true)
        {
            var PassPos = rootBoard.BoardSize;
            int EvalSteps = MAX_MOVES - rootBoard.Moves;
            while (Search_Counter < maxCounts && (MaxiumCacheNodes > NodeCount) && !EndSearchForMemeryNotEnough())
            {
                SimulateOnce(rootBoard, rootNode, Selfplay, UseNN);
            }
        }
        private void SimulateMCTS(Board rootBoard, QUCTNode rootNode, int maxCounts = 160, float weight = 0.1f, bool GPUModel = true)
        {
            var PassPos = rootBoard.BoardSize;
            int EvalSteps = MAX_MOVES - rootBoard.Moves;
            if (GPUModel)
                while (DNN.IsGPUBusy())
                    SimulateOnce_MCTS(rootBoard, rootNode, weight);
            else
                while (Search_Counter < maxCounts && (MaxiumCacheNodes > NodeCount) && !EndSearchForMemeryNotEnough())
                    SimulateOnce_MCTS(rootBoard, rootNode, weight);
        }
        /// <summary>
        /// 对根节点进行模拟(GPU繁忙的时候用CPU搜索模拟)
        /// </summary>
        private void SimulateNoNN(Board rootBoard, QUCTNode rootNode, bool Selfplay = false)
        {
            var PassPos = rootBoard.BoardSize;
            int EvalSteps = MAX_MOVES - rootBoard.Moves;
            //while ((!QuitSearch) || (Search_Counter < MaxiumSearchCounts))
            while (DNN.IsGPUBusy())
            {
                SimulateOnce(rootBoard, rootNode, Selfplay, false);
            }
        }
        #endregion
        #region 自对弈过程
        /// <summary>
        /// 自对弈
        /// </summary>
        /// <returns>对弈之后最好的路径</returns>
        public void Selfplay(Board InitBoard)
        {
            InitBoard.Clear();
            if (PlayPath == null) PlayPath = new List<(QUCTNode node, int ppos)>();
            DNN.EvalStart();
            bool MaxcountSetup = false;
            ThreadPool.SetMaxThreads((int)(Environment.ProcessorCount * CpuLoad), (int)(Environment.ProcessorCount * CpuLoad));
            Search_Counter = 0;
            QUCTNode rootNode = new QUCTNode(InitBoard);
            if (PlayPath.Count > 0)
                rootNode = PlayPath[PlayPath.Count - 1].node;
            DNN.EvalEnqueue(rootNode);
            var currentboard = InitBoard;
            var currentroot = rootNode;
            var PassPos = currentboard.BoardSize;
            int EvalSteps = MAX_MOVES - currentboard.Moves;

            int MaxCount = 0;
            if (PlayPath.Count > 0) MaxCount = PlayPath[0].node.N;
            MaxCount = Math.Max((int)MaxSelfplaySimulate, MaxCount);
            Queue<short> PassMove = new Queue<short>();
            do
            {
                if (currentroot.Children == null)
                    currentroot.N = 0;
                var temp = Search(currentboard, currentroot, true);
                if (temp == RESIGN) break;
                short pos = onboard_pos_2pure[temp];
                if (pos == currentroot.PassMove())
                    PassMove.Enqueue(pos);
                else if (PassMove.Count > 0)
                    PassMove.Dequeue();
                if (currentboard.PutStone_UCT(pos))
                    PlayPath.Add((currentroot, pos));
                else
                {
                    currentboard.PutStone_UCT(currentroot.PassMove());
                    PlayPath.Add((currentroot, pos));
                }
                currentroot.ClearEdges(pos);
                if (currentroot.Children[pos] == null)
                    currentroot.Children[pos] = new QUCTNode(currentboard);
                currentroot = currentroot.Children[pos];
                //MyEventOut?.Invoke(ref currentboard);
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Console.Error.WriteLine(
                       $"Current Time:{DateTime.Now.ToLocalTime()}" + Environment.NewLine +
                        $"Current Play Steps:{Search_Counter}" + Environment.NewLine +
                         $"Total Nodes:{NodeCount}" + Environment.NewLine);
                TotalSimulateCount += Search_Counter;
                Search_Counter = 0;
                NodeCount = 0;
            } while (currentroot != null && PassMove.Count <= 3);
            UpdateWinner(PlayPath);
        }
        /// <summary>
        /// 回放路径（最好的那一条）
        /// </summary>
        /// <param name="rootNode"></param>
        /// <param name="board"></param>
        /// <returns></returns>
        private List<(QUCTNode node, int pos)> ReplayPathFun(QUCTNode rootNode, Board board)
        {
            Random random = new Random();
            if (rootNode == null || board == null) return new List<(QUCTNode node, int pos)>();
            var Path = new List<(QUCTNode node, int pos)>();
            SetEvaledFalse(rootNode);
            var currentnode = rootNode;
            var currentboard = board as Board;
            int PassCount = 0;
            do
            {
                var enabledResigned = currentboard.Moves > SELFPLAY_MINSTEP;
                var pos = ((PassCount < 2) ? currentnode.MaxiumAction(true, true, true) : currentnode.MaxiumAction(true, false, true));
                Path.Add((currentnode, pos));
                bool success = currentboard.PutStone_UCT(pos);
                currentnode.ClearEdges(pos);
                if (board.EnabledPos.Count((byte _pos) => _pos > 0) == 0) PassCount++;
                //if (PassCount > PURE_BOARD_SIZE / 10) break;
                currentnode = currentnode.Children[pos];
            } while (currentnode != null);
            GC.Collect();
            GC.WaitForPendingFinalizers();
            UpdateWinner(Path);
            return Path;
        }
        private void SetEvaledFalse(QUCTNode rootNode)
        {
            if (rootNode == null) return;
            rootNode.Evaled = false;
            rootNode.Writebacked = false;
            foreach (var node in rootNode.Children)
                SetEvaledFalse(node);
        }

        #endregion
        #region 神经网络估值部分
        /// <summary>
        /// 估值用的深度神经网络
        /// </summary>
        protected DQN_DNN_Class DNN;
        /// <summary>
        /// 禁止估值队列
        /// </summary>
        protected void EvalDisabled()
        {
            if (!useNN) return;
            DNN.StopEval = true;
        }
        /// <summary>
        /// 启用估值队列
        /// </summary>
        protected void EvalLoop()
        {
            if (!useNN) return;
            DNN.StopEval = false;
            DNN.EvalStart();
        }
        /// <summary>
        /// 节点送入估值队列
        /// </summary>
        /// <param name="node"></param>
        private void EvalNode(QUCTNode node, bool Selfplay = false)
        {
            if (!DNN.StopEval && useNN)
                DNN.EvalEnqueue(node, Selfplay);
        }
        /// <summary>
        /// 加载神经网络的权值文件
        /// </summary>
        /// <param name="Path"></param>
        public void LoadNN(string Path)
        {
            if (!DNN.Load(Path))
                Console.Error.WriteLine("Weight Not Existed!");
        }
        /// <summary>
        /// 训练函数
        /// </summary>
        /// <param name="DataSet"></param>
        protected void NNTrainer(List<(QUCTNode node, int ppos)> DataSet, Func<int, float> GetPolicyFactions = null)
        {
            List<QUCTNode> TrainingDatas = new List<QUCTNode>();
            foreach (var (node, ppos) in DataSet)
                TrainingDatas.Add(node);
            DNN.Train(TrainingDatas, GetPolicyFactions);
            DNN.Save();
            DNN.Clear();
        }
        /// <summary>
        /// 建立或者创建NN模型
        /// </summary>
        protected void LoadOrCreate(NN_Model Model)
        {
            DNN.SetupModel(Model);
            if (!DNN.Load())
                DNN.CreateModel(CNTK.DeviceDescriptor.UseDefaultDevice(), Model);
        }
        #endregion
        #region 系统资源检查
        protected static bool EndSearchForMemeryNotEnough()
        {
            Process CurrentProcess = Process.GetCurrentProcess();
            var MemSize = (CurrentProcess.PrivateMemorySize64 >> 30);
            return (MemSize >= (int)(MaxiumMemeryUseGigaByte * 0.9));
        }
        #endregion

        #region 纯蒙特卡洛模拟
        private void SimulateMonteCarlo(Board rootBoard, QUCTNode rootNode, int SimCount = 100, int SimThread = 4)
        {
            float SimScore = 0;
            Parallel.For(0, SimThread, (int index) =>
            {
                Thread.VolatileWrite(ref SimScore, Thread.VolatileRead(ref SimScore) + SimulateMonteCarloPartial(rootBoard, rootNode, SimCount));
            });
            rootNode.N += 1;
            rootNode.W += (SimScore / SimThread);
        }
        private float SimulateMonteCarloPartial(Board rootBoard, QUCTNode rootNode, int SimulateCount = 100)
        {
            Random random = new Random();
            Board board = null;
            float Score = 0;
            for (int simcount = 0; simcount < SimulateCount; simcount++)
            {
                bool EndGamePlay = false;
                byte[] EnabledPos = null;
                int PassCount = 0;
                float Winner = 0;
                (rootBoard as Board).Copy(ref board);
                short pos = rootNode.MaxiumAction(true, !rootNode.EndGameModel && useNN, true);
                short rootpos = pos;
                if (pos == EngineBoardMax || !board.PutStone_UCT(pos)) PassCount++;
                do
                {
                    if (!EndGamePlay && board.EnabledPos.Count((byte pos_enabled) => pos_enabled > 0) == 0)
                        EndGamePlay = true;
                    if (!EndGamePlay)
                        EnabledPos = board.EnabledPos;
                    else
                        EnabledPos = board.EnabledPos4Analysis;
                    pos = GetNextPos(EnabledPos, random);
                    if (pos == EngineBoardMax || !board.PutStone_UCT(pos)) PassCount++;
                    else if (PassCount > 0) PassCount--;
                } while (PassCount <= 2);
                Winner = (Math.Sign(board.Score_UCT)) / SimulateCount;
                Score += Winner;
                Thread.VolatileWrite(ref rootNode.W_sa[rootpos], Thread.VolatileRead(ref rootNode.W_sa[rootpos]) + Score);
            }
            return Score;
        }
        private short GetNextPos(byte[] EnabledPos, Random random)
        {
            List<short> Pos = new List<short>();
            for (short index = 0; index < EnabledPos.Length; index++)
                if (EnabledPos[index] > 0) Pos.Add(index);
            if (Pos.Count < 1) return EngineBoardMax;
            if (Pos.Count == 1) return Pos[0];
            return Pos[random.Next(0, Pos.Count)];
        }
        #endregion
    }
}
