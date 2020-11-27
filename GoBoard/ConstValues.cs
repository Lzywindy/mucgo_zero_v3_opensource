using System;
using System.Collections.ObjectModel;
namespace MUCGO_zero_CS
{
    public static class ConstValues
    {
        /*程序名称*/
        public static string Program_NM = "MUC-GO";
        /*程序版本*/
        public static string Version = "V1.0";
        /*强化学习阶段学习棋谱汇报*/
        public static string RL_SGF_NM = _Path + "MUC_GO_RL_RP.sgf";
        /*强化学习经验文件名（NN也是以这个开头的）*/
        public static string RL_NM = "MUC_GO_Exp";
        /*默认目录*/
        public static string _Path = Utils.GetExpFilePath();
        /*经验文件默认扩展名*/
        public static ReadOnlyCollection<string> Model_expand = Array.AsReadOnly(new string[] { ".bin", ".MLN", ".CNN", ".ResNet", ".UNet", ".EMLN", ".ECNN", ".EResNet", ".EUNet" });
        private static Guid MyGuid;
        public static Random randomData;
        static ConstValues()
        {
            MyGuid = Guid.NewGuid();
            int guidseed = BitConverter.ToInt32(MyGuid.ToByteArray(), 0);
            randomData = new Random(guidseed);
        }
        private const bool debug = false;
        public static float epsional_sarsa = 0.05f;
        public static float epsional_qlearning = 0.8f;
        public static float gamma = 0.31f;
        public static bool useNN = true;
        public static bool DebugModel = true;
        public static bool StepModel = false;
        public const int MaxNodesInCache = 0x7FFFF;

    }
    /*神经网络的选用*/
    [Serializable]
    public enum NN_Model : byte
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
    public enum RL_Mode : byte
    {
        QLearning,
        QLearning_Sarsa
    };
}
