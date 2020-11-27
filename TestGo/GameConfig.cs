using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
namespace MUCGO_zero_CS
{
    using static NN_Model;
    using static Board;
    using static ConstValues;
    /// <summary>
    /// 游戏总配置文件
    /// </summary>
    [Serializable]
    public class GameConfig
    {
        #region 配置文件参数
        /// <summary>
        /// 程序启动模式：GUI\GTP两种
        /// </summary>
        public string ProgramMode { get; set; } = "GUI";
        /// <summary>
        /// 默认的让目数
        /// </summary>
        public double Komi { get; set; } = 7.5;
        /// <summary>
        /// 默认扩展因子
        /// </summary>
        public double Cpuct { get; set; } = 1.0;
        /// <summary>
        /// 贪婪值
        /// </summary>
        public double Egready { get; set; } = 0.8;
        /// <summary>
        /// Sarsa（Lambda权重）
        /// </summary>
        public double Gamma { get; set; } = 0.31;
        /// <summary>
        /// UCT搜索的最大
        /// </summary>
        public int MaxUCTNodes { get; set; } = 2097152;
        /// <summary>
        /// 虚损失
        /// </summary>
        public int VitrualLoss { get; set; } = 3;
        /// <summary>
        /// 最大的搜索数目
        /// </summary>
        public int MaxiumSearchCounts { get; set; } = 200;
        /// <summary>
        /// 思考所需要的时间
        /// </summary>
        public int MaxThinkingMs { get; set; } = 1000;
        /// <summary>
        /// CPU负载系数
        /// </summary>
        public float CpuLoad { get; set; } = 2f;
        /// <summary>
        /// 所使用的网络模型
        /// </summary>
        public NN_Model Model { get; set; } = ResNet_NN;
        /// <summary>
        /// 基准测试程序（用来评分用的）
        /// </summary>
        public string BasicProgram { get; set; }
        /// <summary>
        /// 基准程序的分值
        /// </summary>
        public double BasicScore { get; set; }
        /// <summary>
        /// 学习率
        /// </summary>
        public double LearningRate { get; set; } = 0.001;
        /// <summary>
        /// 最小批次
        /// </summary>
        public uint MiniBatch { get; set; } = 64;
        /// <summary>
        /// 迭代次数
        /// </summary>
        public uint Epouch { get; set; } = 10;
        /// <summary>
        /// 最大内存分配(GB)
        /// </summary>
        public long MaxMemoryAlloc { get; set; } = 10;
        /// <summary>
        /// 自对弈最大单轮模拟次数
        /// </summary>
        public  uint MaxSelfplaySimulate { get; set; } = 1600;
        /// <summary>
        /// 模型及其大小
        /// </summary>
        [XmlArray(ElementName = "ModelPerTrainingSize")]
        public List<(NN_Model model, int size)> ModelPerTrainingSize;
        public GameConfig(bool Empty = false)
        {
            if (Empty)
                ModelPerTrainingSize = new List<(NN_Model model, int size)>()
            {
                        (Mutl_Layer_NN,16 ),(Conv_NN,11 ),(ResNet_NN,9 ),(UNet_NN,12 ), (Mutl_Layer_ELM,16 ), (ResNet_ELM,16 ), (UNet_ELM,16 ), (Conv_ELM,16 )
            };
        }
        protected GameConfig() { }
        #endregion
        #region 自动计算的参数
        /// <summary>
        /// 外棋盘边界大小
        /// </summary>
        public ushort Ob_Size { get { return OB_SIZE; } }
        /// <summary>
        /// 棋盘上所有点的数量
        /// </summary>
        public ushort Pure_Board_Max => PURE_BOARD_MAX;
        /// <summary>
        /// 全棋盘大小
        /// </summary>
        public ushort Full_Board_Size => BOARD_SIZE;
        /// <summary>
        /// 全棋盘的大小
        /// </summary>
        public ushort Full_Board_Max => PURE_BOARD_MAX;
        #endregion
        #region 序列化与反序列化
        public static void Save(GameConfig listTestXml)
        {
            FileStream fs = new FileStream(Path, FileMode.Create);
            XmlSerializer xs = new XmlSerializer(typeof(GameConfig));
            xs.Serialize(fs, listTestXml);
            fs.Close();
        }
        public static GameConfig Load()
        {
            FileStream fs = new FileStream(Path, FileMode.Open);
            XmlSerializer xs = new XmlSerializer(typeof(GameConfig));
            GameConfig gamecfg = xs.Deserialize(fs) as GameConfig;
            fs.Close();
            return gamecfg;
        }
        public static readonly string Path = Utils.GetExpFilePath() + "GameConfig.cfg";
        #endregion
    }
}
