using System;
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
    using System.IO;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using static Board;
    using static ConstValues;
    using static Math;
    using static SEARCH_MODE;
    using static Utils;
    /// <summary>
    /// UCT搜索树存储空间
    /// </summary>
    public class UCT_RL : UCTPlay
    {
        #region Fields
        protected int MaxiumNodes = 0;
        protected int WorkerCount = 12;
        private readonly Random random_dev = new Random(0x76532FFD);
        private int count_play = 0;
        private ExpectionProtection expectionProtection;
        private bool gameInit = false;
        private int pass_count = 0;
        private bool resign = false;
        private DateTime StartTime = DateTime.Now;
        private int Count = 0;
        /// <summary>
        /// 搜索模式
        /// </summary>
        private SEARCH_MODE SearchMode = CONST_PLAYOUT_MODE;
        private State stateMachine = State.Init;
        private bool TimeRemained = true;
        private bool Start = false;
        private static string NodesCachesPath = GetExpFilePath() + "NodesCachesPath.bin";
        private readonly float[] NormalizeDis = new float[MAX_MOVES];
        public void NodesCachesPath_Save()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(NodesCachesPath, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, NodesCaches);
            stream.Close();
        }
        public void NodesCachesPath_Load()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(NodesCachesPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            NodesCaches = formatter.Deserialize(stream) as List<List<QUCTNode>>;
            stream.Close();
        }
        private List<List<QUCTNode>> NodesCaches;
        #endregion Fields
        #region Constructors
        /// <summary>
        /// 搜索树存储的构造函数
        /// </summary>
        /// <param name="depth">默认搜索深度</param>
        public UCT_RL(int depth = 422) : base(depth)
        {
            depth = EngineBoardMax;
            NodesCaches = new List<List<QUCTNode>>(new List<QUCTNode>[depth]);
            for (int i = 0; i < NodesCaches.Count; i++)
                NodesCaches[i] = new List<QUCTNode>();
            for (int index = 0; index < NormalizeDis.Length; index++)
                NormalizeDis[index] = (float)Exp(Pow(index - PURE_BOARD_MAX / 2, 2) / (50)) + 1e-5f;
        }
        #endregion Constructors
        #region Properties
        public bool EnableRecord { get; set; } = true;
        public NN_Model Model { get; set; } = NN_Model.Conv_NN;
        public bool RLMode { get; set; } = false;
        public int TrainingTime { get; private set; } = 0;
        #endregion Properties
        #region Methods   
        public void InitSearch(int threads = 4, int depth = 422, float cpuct = 1.0f, float defaultUCB = 1.0f)
        {
            WorkerCount = Min(Environment.ProcessorCount - 1, Max(threads, 4));
            Cpuct = Max(cpuct, 0);
        }
        /// <summary>
        /// 初始化神经网络
        /// </summary>
        public void NNInit()
        {
            LoadOrCreate(Model);
            EvalLoop();
        }

        /// <summary>
        /// 从一个初始盘面开始搜索，之后的搜索使用空白棋盘初始化，如果发生异常，则从断点处开始搜索
        /// </summary>
        /// <param name="game"></param>
        public void ReinforceLearningByStep(Board game, bool ShowRunning = true)
        {
            //Board ThisGame = (game == null ? (game as Board) : (MyGame as Board));
            void GetBoard(ref Board myboard)
            {
                expectionProtection.board = myboard;
            }
            if (Start == false)
            {
                Console.Error.WriteLine($"Training Start At:{StartTime.ToLocalTime()}");
                WriteLog($"Training Start At:{StartTime.ToLocalTime()}");
                Start = true;
            }
            switch (stateMachine)
            {
                case State.Init:
                    {
                        if (ExpectionProtection.ExisitedExpection())
                        {
                            expectionProtection = ExpectionProtection.Load();
                            PlayPath = expectionProtection.Path;
                            ExpectionProtection.ClearFile();
                            expectionProtection.board.Copy(ref game);
                            stateMachine = expectionProtection.m_State;
                            if (PlayPath.Count > 0)
                            {
                                CurrentRoot = PlayPath[0].node;
                                for (int pathindex = 0; pathindex < PlayPath.Count - 1; pathindex++)
                                    PlayPath[pathindex].node.AddNodeAtBegin(PlayPath[pathindex + 1].node, PlayPath[pathindex].ppos);
                            }
                        }
                        else
                        {
                            expectionProtection = new ExpectionProtection();
                            PlayPath = new List<(QUCTNode node, int ppos)>(EngineBoardMax * 2);
                            expectionProtection.Path = PlayPath;
                            stateMachine = State.Prepare;
                        }
                        gameInit = true;
                        //if (File.Exists(NodesCachesPath))
                        //    NodesCachesPath_Load();
                        break;
                    }
                case State.Prepare:
                    {
                        if (!gameInit)
                        {
                            game.Clear();
                            gameInit = true;
                            PlayPath.Clear();
                        }
                        pass_count = 0;
                        resign = false;
                        expectionProtection.m_State = stateMachine = State.Playing;
                        break;
                    }
                case State.Playing:
                    {
                        StartTime = DateTime.Now;
                        if (!DNN.StopEval) EvalLoop();
                        NodeCount = 0;
                        //ThisGame = (MyGame as Board);
                        MyEventOut += GetBoard;
                        Selfplay(game);
                        count_play = PlayPath.Count;
                        expectionProtection.Path = PlayPath;
                        expectionProtection.m_State = stateMachine = State.Training;
                        //expectionProtection.board = 
                        //MyGame = ThisGame;
                        var EndTime = DateTime.Now;
                        var TimeSpin = (EndTime - StartTime).Milliseconds;
                        Console.Error.WriteLine($"Training Epouch:{++Count}" + Environment.NewLine +
                          $"Training Time Cost(ms):{TimeSpin}" + Environment.NewLine +
                         $"Current Time:{DateTime.Now.ToLocalTime()}" + Environment.NewLine +
                          $"Current Play Steps:{count_play}" + Environment.NewLine +
                       $"Total Simulate:{TotalSimulateCount}" + Environment.NewLine);
                        break;
                    }
                case State.Training:
                    {
                        ClearByPath(PlayPath);
                        NNTrainer(PlayPath, GetPolicyFactions);
                        expectionProtection?.Path.Clear();
                        SGF sgf = new SGF();
                        sgf.Record(game);
                        sgf.Save(GetExpFilePath() + RL_SGF_NM);
                        pass_count++;
                        WriteLog($"Training Epouch:{Count}" + Environment.NewLine +
                           $"Current Time:{DateTime.Now.ToLocalTime()}" + Environment.NewLine +
                            $"Current Play Steps:{count_play}" + Environment.NewLine +
                         $"Total Simulate:{TotalSimulateCount}" + Environment.NewLine);
                        expectionProtection.m_State = stateMachine = State.Prepare;
                        gameInit = false;
                        game.Clear();
                        //MyGame = EmptyBoard.Clone();
                        foreach (var (node, pos) in PlayPath)
                            node.ClearEdges();
                        CurrentRoot = null;
                        PlayPath.Clear();
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        while (EndSearchForMemeryNotEnough()) ;
                    }
                    break;
            }
        }
        private void ClearByPath(List<(QUCTNode node, int ppos)> Path)
        {
            foreach (var (node, ppos) in Path)
                node.ClearEdges(ppos);
        }
        /// <summary>
        /// 存储断点
        /// </summary>
        public void SaveBreakpoint(Board board)
        {
            if (expectionProtection != null)
            {
                if (board != null)
                    expectionProtection.board = board;
                if (expectionProtection.Path.Count == 0)
                    expectionProtection.Path = PlayPath;
                ExpectionProtection.Save(expectionProtection);
            }

            //NodesCachesPath_Save();
        }
        /// <summary>
        /// 强化学习训练过程
        /// </summary>
        /// <param name="Training_Time"></param>
        /// <param name="timetype"></param>
        public void ReinforceLearningGTP(int Training_Time, TimeType timetype)
        {
            Board ThisGame = EmptyBoard.Clone();
            try
            {
               
                /*自对弈过程*/
                while (TimeRemained)
                {
                    if (!resign)
                        EvalLoop();
                    else
                        EvalDisabled();
                    ReinforceLearningByStep(ThisGame);
                    count_play++;
                    /*显示所用的时间*/
                    if (resign)
                    {

                        /*经验回放机制*/
                        List<QUCTNode> TrainingDatas = new List<QUCTNode>();
                        TrainingDatas.Clear();
                        PlayPath.Clear();
                    }
                }
            }
            catch (Exception e)
            {
                SaveBreakpoint(ThisGame);
                WriteLog(e.Message);
            }
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
        public float GetPolicyFactions(int step)
        {
            if (step < NormalizeDis.Length && step >= 0)
                return NormalizeDis[step];
            else
                return 1e-4f;
        }
        #endregion Methods
        /*最大允许2M个经验节点*/
        /*最大允许6000次不清理节点的自对弈*/
        /*分析棋盘，用以返回最后的评分（估算）*/
        /*自对弈经验累积*/
        /*搜索走子*/
    }
}