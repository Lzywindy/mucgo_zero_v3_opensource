using System;
using System.Collections.ObjectModel;
namespace TestGo
{
    static class ConstValues
    {
        public const int PURE_BOARD_SIZE = 19;
        public const int OB_SIZE = 5;
        public const int BOARD_SIZE = (PURE_BOARD_SIZE + OB_SIZE + OB_SIZE);
        public const int PURE_BOARD_MAX = (PURE_BOARD_SIZE * PURE_BOARD_SIZE);
        public const int BOARD_MAX = (BOARD_SIZE * BOARD_SIZE);
        public const int MAX_STRING = (PURE_BOARD_MAX * 4 / 5);
        public const int MAX_NEIGHBOR = MAX_STRING;
        public const int BOARD_START = OB_SIZE;
        public const int BOARD_END = (PURE_BOARD_SIZE + OB_SIZE - 1);
        public const int STRING_LIB_MAX = (BOARD_SIZE * (PURE_BOARD_SIZE + OB_SIZE));
        public const int STRING_POS_MAX = (BOARD_SIZE * (PURE_BOARD_SIZE + OB_SIZE));
        public const int STRING_END = (STRING_POS_MAX - 1);
        public const int NEIGHBOR_END = (MAX_NEIGHBOR - 1);
        public const int LIBERTY_END = (STRING_LIB_MAX - 1);
        public const int MAX_RECORDS = (PURE_BOARD_MAX * 3);
        public const int MAX_MOVES = (MAX_RECORDS - 1);
        public const int PASS = 0;
        public const int RESIGN = -1;
        public const float KOMI = 6.5f;
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
        public const int UCT_PHYSICALS_MAX = (1 << 14);
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
        /*程序名称*/
        public const string Program_NM = "MUC-GO";
        /*程序版本*/
        public const string Version = "V1.0";
        /*强化学习阶段学习棋谱汇报*/
        public const string RL_SGF_NM = Path + "MUC_GO_RL_RP.sgf";
        /*强化学习经验文件名（NN也是以这个开头的）*/
        public const string RL_NM = "MUC_GO_Exp";
        /*默认目录*/
        public const string Path = "H://MUCGO//mucgo_1_0//";
        /*经验文件默认扩展名*/
        public static ReadOnlyCollection<string> Model_expand = Array.AsReadOnly(new string[] { ".bin", ".MLN", ".CNN", ".ResNet", ".UNet", ".EMLN", ".ECNN", ".EResNet", ".EUNet" });
        private const bool debug = false;
    }
    /*神经网络的选用*/
    enum NN_Model : byte
    {
        None_NN,
        Mutl_Layer_NN,
        Conv_NN,
        ResNet_NN,
        UNet_NN,
        Mutl_Layer_ELM,
        Conv_ELM,
        ResNet_ELM,
        UNet_ELM
    };
    /*强化学习的使用*/
    enum RL_Mode : byte
    {
        QLearning,
        QLearning_Sarsa
    };
}
