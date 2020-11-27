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
    using static PlayInterface;
    using static Console;
    using static ParameterStateMachine_MODE;
    using static GTPMessageRet;
    public static class PlayInterface
    {
        public const string DELIM = " ";
        public const string PROGRAM_NAME = "MUCGO";
        public const string PROGRAM_VERSION = "1.0";
        public const string PROTOCOL_VERSION = "2";

        static PlayInterface() { }
        /*训练1小时Q表*/
        public static int TrainTime { get; set; }

        /*GTP通信命令对应的函数调用*/
        private static Dictionary<string, Action> GtpCmd = new Dictionary<string, Action>() {
            { "protocol_version", GetProtocolVersion},
            { "boardsize", SetBoardSize},
            { "clear_board", ClearBoard },
            { "name", GetName },
            { "genmove", Genmove },
            { "play", Play },
            { "known_command", LegalCommand },
            { "list_commands", ListCommand },
            { "quit", Quit },
            { "komi", SetKomi },
            { "get_komi", GetKomi },
            { "final_score", FinalScore },
            { "time_settings", SetTime },
            { "version", Version },
            { "genmove_black", Genmove },
            { "genmove_white", Genmove },
            { "black", Play },
            { "white", Play },
            { "showboard", ShowBoard }
        };
        private static Dictionary<string, uint> GtpCmdSubId = new Dictionary<string, uint>() {
            { "protocol_version", 0x0U },
            { "boardsize", 0x0U },
            { "clear_board", 0x0U },
            { "name", 0x0U },
            { "genmove", 0x0U },
            { "play", 0x0U },
            { "known_command", 0x0U },
            { "list_commands", 0x0U },
            { "quit", 0x0U },
            { "komi", 0x0U },
            { "get_komi", 0x0U },
            { "final_score", 0x0U },
            { "time_settings", 0x0U },
            { "version", 0x0U },
            { "genmove_black", 0x1U },
            { "genmove_white", 0x2U },
            { "black", 0x1U },
            { "white", 0x2U },
            { "showboard", 0x0U }
        };
        private static Dictionary<string, uint> GtpCmdArgc = new Dictionary<string, uint>() {
            { "protocol_version", 0U },
            { "boardsize", 1U },
            { "clear_board", 0U },
            { "name", 0U },
            { "genmove", 1U },
            { "play", 2U },
            { "known_command", 1U },
            { "list_commands", 0U },
            { "quit", 0U },
            { "komi", 1U },
            { "get_komi", 0U },
            { "final_score", 0U },
            { "time_settings", 1U },
            { "version", 0U },
            { "genmove_black", 0U },
            { "genmove_white", 0U },
            { "black", 1U },
            { "white", 1U },
            { "showboard", 0U }
        };
        private static List<string> GtpMessage = new List<string>() {
            "",
            "? unknown communication",
            "input gemmove color",
            "play color point",
            "komi value must be float",
            "Error Arguments"
        };
        private static IGame PlayGame = new IGame();
        private static Queue<string> CmdStrides = new Queue<string>();
        private static bool continueRunning = true;
        private static uint subID = 0;
        private static uint argc = 0;
        private static string Command = "";
        // 参数分析（程序启动的时候需要查找的）
        public static void ParameterAnaylsis(string[] args)
        {
            //PlayGame = new IGame();
            bool SetupPlayout = false;
            bool SetupTime = false;
            bool SetupKomi = false;
            bool SetupThread = false;
            bool SetupModel = false;
            bool SetupRlMode = false;
            bool Selfplay = false;
            bool SetupRecord = false;
            int selfplay_hours = 5;

            void ErrorInfoAndExit() { WriteLine("Error commend!Exit fewer minutes later!"); Environment.Exit(0x1); };
            /*设置哪个参数*/
            uint WhatToSet(ref int index)
            {
                switch (args[index++])
                {
                    case "-playmode":
                        return 1;
                    case "-komi":
                        return 2;
                    case "-thread":
                        return 3;
                    case "-model":
                        return 4;
                    case "-rlmode":
                        return 5;
                    case "-selfplay":
                        return 6;
                    case "-recordSGF":
                        return 7;
                    default:
                        return 0;
                }
            }
            uint Peek(int index)
            {
                switch (args[index])
                {
                    case "-playmode":
                        return 1;
                    case "-komi":
                        return 2;
                    case "-thread":
                        return 3;
                    case "-model":
                        return 4;
                    case "-rlmode":
                        return 5;
                    case "-selfplay":
                        return 6;
                    case "-recordSGF":
                        return 7;
                    default:
                        return 0;
                }
            }
            /*设置游戏模式*/
            void SetGameMode(ref int index)
            {
                int invailed = ~0x0;
                int value = 0x0 ^ invailed;
                int time = 10000;
                int playsout = 10000;
                while (Peek(index) == 0)
                {
                    switch (args[index])
                    {
                        case "time":
                            if (SetupTime) return;
                            index++;
                            if (!int.TryParse(args[index++], out time))
                                ErrorInfoAndExit();
                            SetupTime = true;
                            break;
                        case "playsout":
                            if (SetupPlayout) return;
                            index++;
                            if (!int.TryParse(args[index++], out playsout))
                                ErrorInfoAndExit();
                            SetupPlayout = true;
                            break;
                        default:
                            ErrorInfoAndExit();
                            break;
                    }
                }
                if (SetupTime && SetupPlayout)
                    PlayGame.SetBoth(time, playsout);
                else if (SetupTime && !SetupPlayout)
                    PlayGame.SetMode_ConstTime(time);
                else if (!SetupTime && SetupPlayout)
                    PlayGame.SetMode_PlaysOuts(playsout);
                else
                    ErrorInfoAndExit();
            };
            /*默认游戏模式*/
            void DefaultGameModeSetup()
            {
                if (SetupPlayout || SetupTime) return;
                PlayGame.SetMode_ConstTime(1000);
            };
            /*贴目设置*/
            void SetKomi(ref int index)
            {
                if (SetupKomi) return;
                float value;
                if (!float.TryParse(args[index++], out value))
                    ErrorInfoAndExit();
                PlayGame.Komi = value;
                SetupKomi = true;
            };
            /*默认贴目设置*/
            void DefaultKomiSetup()
            {
                if (SetupKomi) return;
                PlayGame.Komi = 7.5f;
            };
            /*设置线程数目*/
            void SetThread(ref int index)
            {
                if (SetupThread) return;
                uint value = 4;

                if (!uint.TryParse(args[index++], out value))
                    ErrorInfoAndExit();
                PlayGame.SetThreads(value);
                SetupThread = true;
            };
            /*默认线程数目*/
            void DefaultThreadSetup()
            {
                if (SetupThread) return;
                PlayGame.SetThreads(20U);
            };
            /*设置模型*/
            void SetExpModel(ref int index)
            {
                if (SetupModel) return;
                string temp = args[index++];

                NN_Model model = NN_Model.Conv_NN;
                if (temp == ("none"))
                {
                    model = NN_Model.None_NN;
                }
                else if (temp == ("mln"))
                {
                    model = NN_Model.Mutl_Layer_NN;
                }
                else if (temp == ("cnn"))
                {
                    model = NN_Model.Conv_NN;
                }
                else if (temp == ("resnet"))
                {
                    model = NN_Model.ResNet_NN;
                }
                else if (temp == ("unet"))
                {
                    model = NN_Model.UNet_NN;
                }
                else if (temp == ("elm_mln"))
                {
                    model = NN_Model.Mutl_Layer_ELM;
                }
                else if (temp == ("elm_cnn"))
                {
                    model = NN_Model.Conv_ELM;
                }
                else if (temp == ("elm_resnet"))
                {
                    model = NN_Model.ResNet_ELM;
                }
                else if (temp == ("elm_unet"))
                {
                    model = NN_Model.UNet_ELM;
                }
                PlayGame.SetModel(model);
                SetupModel = true;
            };
            /*默认模型*/
            void DefaultExpModel()
            {
                if (SetupModel) return;
                PlayGame.SetModel(NN_Model.Conv_NN);
            };
            /*设置学习模式*/
            void SetRlMode(ref int index)
            {
                if (SetupRlMode) return;
                string temp = args[index++];


                RL_Mode mode = RL_Mode.QLearning;
                if (temp == ("q"))
                {
                    mode = RL_Mode.QLearning;
                }
                else if (temp == ("qs"))
                {
                    mode = RL_Mode.QLearning_Sarsa;
                }
                else
                {
                    ErrorInfoAndExit();
                }
                PlayGame.SetRLMode(mode);
                SetupRlMode = true;
            };
            /*默认学习模式*/
            void DefaultRlMode()
            {
                if (SetupRlMode) return;
                PlayGame.SetRLMode(RL_Mode.QLearning);
            };
            /*自对弈设置*/
            void SelfPlayEnabled(ref int index)
            {
                if (Selfplay) return;
                if (!int.TryParse(args[index++], out selfplay_hours))
                    ErrorInfoAndExit();
                Selfplay = true;
            };
            /*设置记录棋谱许可*/
            void SetRecordEnable(ref int index)
            {
                if (SetupRecord) return;
                string value = args[index++];

                if (value == "true")
                    PlayGame.SetRecordEnabled(true);
                else if (value == "false")
                    PlayGame.SetRecordEnabled(false);
                else
                    ErrorInfoAndExit();
                SetupRecord = true;
            };
            /*默认记录棋谱许可*/
            void DefaultRecordEnable()
            {
                if (SetupRecord) return;
                PlayGame.SetRecordEnabled(false);
            };
            if (args.Length == 0) return;
            for (int i = 0; i < args.Length;)
            {
                switch (WhatToSet(ref i))
                {
                    case 1U:
                        {
                            if (i >= args.Length)
                                ErrorInfoAndExit();
                            SetGameMode(ref i);
                            break;
                        }
                    case 2U:
                        {
                            if (i >= args.Length)
                                ErrorInfoAndExit();
                            SetKomi(ref i);
                            break;
                        }
                    case 3U:
                        {
                            if (i >= args.Length)
                                ErrorInfoAndExit();
                            SetThread(ref i);
                            break;
                        }
                    case 4U:
                        {
                            if (i >= args.Length)
                                ErrorInfoAndExit();
                            SetExpModel(ref i);
                            break;
                        }
                    case 5U:
                        {
                            if (i >= args.Length)
                                ErrorInfoAndExit();
                            SetRlMode(ref i);
                            break;
                        }
                    case 6U:
                        {
                            if (i >= args.Length)
                                ErrorInfoAndExit();
                            SelfPlayEnabled(ref i);
                            break;
                        }
                    case 7U:
                        {
                            if (i >= args.Length)
                                ErrorInfoAndExit();
                            SetRecordEnable(ref i);
                            break;
                        }
                    default:
                        ErrorInfoAndExit();
                        break;
                }
            }
            DefaultGameModeSetup();
            DefaultKomiSetup();
            DefaultThreadSetup();
            DefaultExpModel();
            DefaultRlMode();
            DefaultRecordEnable();
            if (Selfplay)
            {
                PlayGame.Reset();
                /*try
                {
                    PlayGame.SelfPlay(selfplay_hours);
                }
                catch (const std.exception&e)
                {
                    cerr + e.what() + Environment.NewLine;
                }*/
                PlayGame.SelfPlay(selfplay_hours);
                Environment.Exit(0x0);
            }
        }
        // 命令分析（通过输入流分析命令，放入命令读取队列）(做好,待测试)
        private static bool CmdAnaylsis()
        {
            CmdStrides.Clear();
            var temp = ReadLine();
            var temp_arrays = temp.Split(new string[] { " ", "\t", Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            if (temp_arrays == null || temp_arrays.Length == 0 || !GtpCmd.ContainsKey(temp_arrays[0])) return false;
            for (int i = 0; i < temp_arrays.Length; i++)
                CmdStrides.Enqueue(temp_arrays[i]);
            Command = CmdStrides.Dequeue();
            subID = GtpCmdSubId[Command];
            argc = GtpCmdArgc[Command];
            return true;
        }
        #region GTP部分函数
        // GPT通信主函数
        public static void MainGTP()
        {
            while (continueRunning)
            {
                if (CmdAnaylsis())
                    GtpCmd[Command]();
            }
        }
        /// <summary>
        /// 得到通信的版本号
        /// </summary>
        private static void GetProtocolVersion()
        {
            Response(PROTOCOL_VERSION, true);
        }
        /// <summary>
        /// 得到版本号
        /// </summary>
        private static void Version()
        {
            Response(PROGRAM_VERSION, true);
        }
        /// <summary>
        /// 是否是合法命令
        /// </summary>
        private static void LegalCommand()
        {
            var arg = CmdStrides.Peek();
            CmdStrides.Dequeue();
            if (GtpCmd.ContainsKey(arg))
                Response("true", true);
            else
                Response("false", false);
        }
        /// <summary>
        /// 列出命令
        /// </summary>
        private static void ListCommand()
        {
            string cmds = "";
            foreach (var node in GtpCmd)
            {
                cmds += Environment.NewLine + node.Key;
            }
            Response(cmds, true);
        }
        /// <summary>
        /// 设置棋盘大小
        /// </summary>
        private static void SetBoardSize()
        {
            uint size = 19;
            var command = CmdStrides.Peek();
            CmdStrides.Dequeue();
            if (uint.TryParse(command, out size))
            {
                PlayGame.SetBoardSize(size);
                Response(GtpMessage[(int)brank], true);
            }
            else
            {
                Response(GtpMessage[(int)err_arguements], false);
            }
        }
        // 设置走棋思考时间
        private static void SetTime()
        {
            int value = 10000;
            if (int.TryParse(CmdStrides.Peek(), out value))
            {
                PlayGame.SetTime(value);
                /*反馈响应给GTP通信*/
                Response(GtpMessage[(int)brank], true);
            }
            else
                Response(GtpMessage[(int)err_arguements], false);
        }
        // 设置让目数
        private static void SetKomi()
        {
            float value = 7.5f;
            if (float.TryParse(CmdStrides.Peek(), out value))
            {
                PlayGame.Komi = value;
                /*反馈响应给GTP通信*/
                Response(GtpMessage[(int)brank], true);
            }
            else
                Response(GtpMessage[(int)err_arguements], true);
        }

        // 得到让目数
        private static void GetKomi()
        {
            string response = string.Format("{0:00.0}", PlayGame.Komi);
            Response(response, true);
        }
        // 得到程序名字
        private static void GetName()
        {
            Response(PROGRAM_NAME, true);
        }
        // 清理盘面
        private static void ClearBoard()
        {
            PlayGame.Reset();
            Response(GtpMessage[(int)brank], true);
        }
        // 让计算机走棋
        private static void Genmove()
        {
            Stone color = S_OB;
            int point = PASS;
            string pos = "";
            /*获得当前走子的颜色*/
            switch (subID)
            {
                case 0x0U://genmove b/w
                    {
                        var arg = CmdStrides.Peek();
                        CmdStrides.Dequeue();
                        color = arg == "b" ? S_BLACK : (arg == "w" ? S_WHITE : S_OB);
                        break;
                    }
                case 0x1U://genmove_black
                    {
                        color = S_BLACK;
                        break;
                    }
                case 0x2U://genmove_white
                    {
                        color = S_WHITE;
                        break;
                    }
                default:
                    {
                        Response(GtpMessage[(int)err_genmove], true);
                        return;
                    }
            }
            /*判断是否是合法的颜色*/
            if (color == S_OB) { Response(GtpMessage[(int)err_genmove], true); return; }
            /*产生走子*/
            point = PlayGame.Genmove(color);
            /*反馈走子至GTP通信*/
            IntegerToString(point, out pos);
            Response(pos, true);
        }
        // 玩家落子
        private static void Play()
        {
            Stone color = S_OB;
            int point = PASS;
            /*获取颜色和落子的位置*/
            switch (subID)
            {
                case 0x0U://play b/w pos
                    {
                        var arg = CmdStrides.Peek();
                        CmdStrides.Dequeue();
                        color = arg == "b" ? S_BLACK : (arg == "w" ? S_WHITE : S_OB);
                        arg = CmdStrides.Peek();
                        CmdStrides.Dequeue();
                        point = StringToInteger(arg);
                        break;
                    }
                case 0x1U://black pos
                    {
                        var arg = CmdStrides.Peek();
                        CmdStrides.Dequeue();
                        point = StringToInteger(arg);
                        color = S_BLACK;
                        break;
                    }
                case 0x2U://white pos
                    {
                        var arg = CmdStrides.Peek();
                        CmdStrides.Dequeue();
                        point = StringToInteger(arg);
                        color = S_WHITE;
                        break;
                    }
                default:
                    {
                        Response(GtpMessage[(int)err_play], true);
                        return;
                    }
            }
            /*判断是否是合法的颜色*/
            if (color == S_OB) { Response(GtpMessage[(int)err_play], true); return; }
            /*落子*/
            PlayGame.Play(point, color);
            /*反馈响应给GTP通信*/
            Response(GtpMessage[(int)brank], true);
        }
        // 计算最后的得分(做好,待测试)
        private static void FinalScore()
        {
            var score = PlayGame.GetScore();
            string str = (score > 0) ? "B+" : (score < 0) ? "W+" : "Tei";
            str += string.Format("{0:000.0}", Abs(score));
            Response(str, true);
        }
        // 显示盘面信息
        private static void ShowBoard()
        {
            PlayGame.ShowBoard();
            Response(GtpMessage[(int)brank], true);
        }
        // 退出程序
        private static void Quit()
        {
            Response(GtpMessage[(int)brank], true);
            continueRunning = false;
        }
        // GTP通信反馈函数
        private static void Response(string res, bool success)
        {
            if (success)
                Write("= " + res + Environment.NewLine + Environment.NewLine);
            else
            {
                if (res != "")
                    Error.WriteLine(res);
                Write("?" + Environment.NewLine + Environment.NewLine);
            }
        }
        #endregion
    }
    enum GTPMessageRet : int
    {
        brank,
        err_command,
        err_genmove,
        err_play,
        err_komi,
        err_arguements
    };
    enum ParameterStateMachine_MODE : byte
    {
        MODE_PLAYOUT,
        MODE_TIME,
        MODE_KOMI,
        MODE_THREAD,
        MODE_SELFPLAY,
        FILE_PATH,
        FILE_PATH_NN
    };
    class IGame
    {
        private bool RecordEnalbed = false;
        /*UCT搜索树*/
        private UCT UCT_Tree = new UCT();
        /// <summary>
        /// 投降标志(属性的定义)
        /// </summary>
        public bool IsResigned { get; private set; }
        private game_info_t Current;
        /*棋子颜色（当前落子方）*/
        private Stone player_color;
        public IGame()
        {
            Init();
        }
        // 重置游戏
        public void Reset()
        {
            player_color = S_EMPTY;
            IsResigned = false;
            InitializeHash();
            InitializeConst();
            Current = new game_info_t();
            InitializeBoard(ref Current);
            UCT_Tree.Clear();
        }
        // 初始化环境
        public void Init()
        {
            // TODO: 在此处添加实现代码.
            UCT_Tree.InitSearch(Environment.ProcessorCount - 1, 422, 1.0f, 1.0f);
            InitializeHash();
            InitializeConst();
            if (Current == null)
            {
                Current = new game_info_t();
                InitializeBoard(ref Current);
            }
            IsResigned = false;
        }
        // 电脑走棋
        public int Genmove(Stone color)
        {
            if (color != S_BLACK && color != S_WHITE) return PASS;
            player_color = S_BLACK;
            int point = PASS;
            /*game_info_tnextsetp(new game_info_t());
            CopyGame(nextsetp, GamePath.top());*/
            point = UCT_Tree.Genmove(Current, color);
            // 投降的时候不记录路径
            if (point != RESIGN)
            {
                PutStone(Current, point, color);
                //GamePath.emplace(nextsetp);
                IsResigned = false;
            }
            else
            {
                IsResigned = true;
            }
            PrintBoard(Current);
            if (point == RESIGN)
            {
                SGF sgfrec = new SGF();
                PutStone(Current, PASS, color);
                sgfrec.Record(Current);
                sgfrec.Save(RL_SGF_NM);
            }
            return point;
        }
        // 棋盘落子
        public void Play(int pos, Stone color)
        {
            // 复制当前局面保存
            /*game_info_tnextsetp(new game_info_t());
            CopyGame(nextsetp, GamePath.top());*/
            // 投降的时候不记录路径
            if (pos != RESIGN)
            {
                PutStone(Current, pos, color);
                //GamePath.emplace(nextsetp);
                IsResigned = false;
            }
            else
            {
                IsResigned = true;
            }
            PrintBoard(Current);
        }
        // 悔棋函数
        public void Backup(int step)
        {
            ///*表示不反悔*/
            //if (step == 0)return;
            ///*查看输入是否在可到达的范围之内*/
            //var backstep = min(step - 1, GamePath.size());
            ///*弹出博弈路径*/
            //for (int i = 0; i <= backstep; i++)
            //	GamePath.Dequeue();
        }
        // 自对弈完善Q表
        public void SelfPlay(int PlayTime = 5)
        {
            // 自对弈强化学习
            UCT_Tree.ReinforceLearning(PlayTime, TimeType.hour);
        }
        // 获得分数
        public float GetScore()
        {
            return UCT_Tree.Analysis(Current);
        }
        // 设置思考时间
        public void SetTime(int millisecond = 10000)
        {
            // TODO: 在此处添加实现代码.
            UCT_Tree.SetMode(CONST_TIME_MODE, millisecond);
        }
        // 显示盘面
        public void ShowBoard()
        {
            PrintBoard(Current);
        }
        // 设置棋盘大小
        public void SetBoardSize(uint size = 19U)
        {
            // TODO: 在此处添加实现代码.
            if (pure_board_size != size && size <= PURE_BOARD_SIZE && size > 0)
                SetBoardSize(size);
            Reset();
        }
        // 搜索模式使用定时搜索
        public void SetMode_ConstTime(int value = 10000)
        {
            // TODO: 在此处添加实现代码.
            UCT_Tree.SetMode(CONST_TIME_MODE, value);
        }
        // 采用走棋次数搜索
        public void SetMode_PlaysOuts(int value = 1200)
        {
            // TODO: 在此处添加实现代码.
            UCT_Tree.SetMode(CONST_PLAYOUT_MODE, value);
        }
        public void SetBoth(int ConstTime = 10000, int PlaysOuts = 1200)
        {
            UCT_Tree.SetMode(CONST_PLAYOUT_TIME_MODE, ConstTime, PlaysOuts);
        }
        // 设置线程数量
        public void SetThreads(uint value = 20U)
        {
            // TODO: 在此处添加实现代码.
            UCT_Tree.SetThreads(value);
        }
        //设置Q表文件路径
        public void SetModel(NN_Model value = NN_Model.Conv_NN)
        {
            UCT_Tree.Model = value;
            UCT_Tree.NNInit();
        }
        // 设置NN的权重文件位置
        public void SetRLMode(RL_Mode value = RL_Mode.QLearning_Sarsa)
        {
            // TODO: 在此处添加实现代码.
            UCT_Tree.RLMode = value == RL_Mode.QLearning_Sarsa;
        }
        /*设置是否允许记录*/
        public void SetRecordEnabled(bool RecordEnalbed = false)
        {
            UCT_Tree.EnableRecord = this.RecordEnalbed = RecordEnalbed;
        }
        // 读写让目(属性的定义)
        public float Komi { get { return KomiSetup; } set { SetKomi(value); } }
    };
}
