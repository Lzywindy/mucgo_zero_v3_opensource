using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MUCGO_zero_CS
{
    using static ConstValues;

    using static Board;
    public static class GUIServer
    {
        public static GameConfig Configs { get; set; }
        public static UCT_RL SearchCore { get; set; }
        public static UCTPlay NNComparer { get; set; }
        public static Board board { get; set; }
        public static bool ComputerBlack { get; set; } = true;
        public static bool PlaySide { get; set; } = true;
        public static bool NNComparerLoaded { get; private set; } = false;
        public static bool OtherProgramLoaded { get; private set; } = false;
        public static bool Resigned { get; private set; }
        private static int pass_count = 0;
        private static Stack<Board> PlayPath = new Stack<Board>();
        /// <summary>
        /// 初始化界面服务
        /// </summary>
        /// <param name="MyConfigs"></param>
        public static void InitGUIServer(GameConfig MyConfigs)
        {
            //初始化参数
            Configs = MyConfigs;
            SearchCore = new UCT_RL(Configs.Pure_Board_Max);
            NNComparer = new UCTPlay(Configs.Pure_Board_Max);
            board = EmptyBoard.Clone();
            //初始化自己的网络
            SearchCore.Model = MyConfigs.Model;
            SearchCore.RLMode = true;
            ResetParameters();
            NNComparerLoaded = false;
            Resigned = false;
        }
        /// <summary>
        /// 重置配置参数
        /// </summary>
        public static void ResetParameters()
        {
            //UCT搜索公共参数初始化
            UCT_Search.Cpuct = (float)Configs.Cpuct;
            UCT_Search.MaxiumCacheNodes = Configs.MaxUCTNodes;
            UCT_Search.MaxiumSearchCounts = Configs.MaxiumSearchCounts;
            UCT_Search.MaxThinkingMs = Configs.MaxThinkingMs;
            UCT_Search.CpuLoad = Configs.CpuLoad;
            UCT_Search.VitrualLoss = Configs.VitrualLoss;
            UCT_Search.MaxiumMemeryUseGigaByte = Math.Max(1, Configs.MaxMemoryAlloc);
            UCT_Search.MaxSelfplaySimulate = Math.Max(1, Configs.MaxSelfplaySimulate);
            UCT_UpdatePolicy.E_greedy = (float)Configs.Egready;
            UCT_UpdatePolicy.Gamma = (float)Configs.Gamma;
            //神经网络搜索公共参数初始化
            DQN_DNN_Class.ModelPerTrainingSize = Configs.ModelPerTrainingSize;
            DQN_DNN_Class.ModelType = Configs.Model;
            DQN_DNN_Class.LearningRate = Configs.LearningRate;
            DQN_DNN_Class.Epouch = Configs.Epouch;
            DQN_DNN_Class.MiniBatch = Configs.MiniBatch;
            SetKomi((float)Configs.Komi);
            SearchCore.NNInit();
            board = EmptyBoard;
            pass_count = 0;
            Resigned = false;
        }
        /// <summary>
        /// 一步一步自对弈
        /// </summary>
        public static void SetPlayer()
        {
            SearchCore.ReinforceLearningByStep(board, false);
        }
        /// <summary>
        /// 设置当前搜索核心的玩家所属方
        /// </summary>
        /// <param name="Side">默认为电脑执黑</param>
        public static void SetComputerPlayer(bool Side = true)
        {
            ComputerBlack = Side;
        }
        /// <summary>
        /// 交换先后手
        /// </summary>
        public static void ChangeSide()
        {
            ComputerBlack = !ComputerBlack;
        }
        /// <summary>
        /// 自对弈
        /// </summary>
        public static void Selfplay()
        {
            SearchCore.ReinforceLearningByStep(board, false);
        }
        /// <summary>
        /// 保存断点
        /// </summary>
        public static void SaveLastState()
        {
            SearchCore.SaveBreakpoint(board);
        }
        public static void SaveConfig()
        {
            GameConfig.Save(Configs);
        }
        /// <summary>
        /// 设置自身比较的网络
        /// </summary>
        /// <param name="path"></param>
        public static void SetSelfCompareNN(string path)
        {
            NNComparer.LoadNN(path);
            NNComparerLoaded = true;
        }
        public static void ResetBoard()
        {
            board = EmptyBoard.Clone() as Board;
            PlayPath.Clear();
            Resigned = false;
            PlaySide = true;
        }
        /// <summary>
        /// 人机对弈的时候使用的函数
        /// </summary>
        public static void NNThinking()
        {
            if (board == null)
                board = EmptyBoard.Clone() as Board;
            if (ComputerBlack == PlaySide)
            {
                short pos = (short)SearchCore.Genmove(board as Board);
                if (pos != RESIGN && pass_count < 2)
                {
                    PutStone(board, pos, board.Board_CurrentPlayer);
                    PlaySide = !PlaySide;
                }
                else
                {
                    Resigned = true;
                }
            }
        }
        public static void Play(short pos)
        {
            if (board == null)
                board = EmptyBoard.Clone() as Board;
            if (ComputerBlack != PlaySide)
            {
                if (pos != RESIGN && pass_count < 2)
                {
                    PlayPath.Push(board.Clone());
                    PutStone(board, pos, board.Board_CurrentPlayer);
                    PlaySide = !PlaySide;
                }
                else
                {
                    Resigned = true;
                }
            }
        }
        public static void Backup()
        {
            if (PlayPath.Count > 0)
                board = PlayPath.Pop();
        }
        public static void AreaEsimate()
        {
            if (board == null)
                board = EmptyBoard.Clone();
            SearchCore.Analysis(board);
        }
    }
}
