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
    using static Utils;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using System.IO;
    [Serializable]
    public enum State : byte { Init, Prepare, Playing, Training };
    [Serializable]
    public class ExpectionProtection
    {
        static string path = GetExpFilePath() + "ExpectionBreakPos.bin";
        public State m_State { get; set; }
        public Board board;
        public List<(QUCTNode node, int ppos)> Path;
        public static void Save(ExpectionProtection expectionProtection)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, expectionProtection);
            stream.Close();
        }
        public static ExpectionProtection Load()
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            ExpectionProtection expectionProtection = (ExpectionProtection)formatter.Deserialize(stream);
            stream.Close();
            return expectionProtection;
        }
        public static void ClearFile()
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        public static bool ExisitedExpection()
        {
            return File.Exists(path);
        }
    }
}
