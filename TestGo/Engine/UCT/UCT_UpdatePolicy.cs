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
    using System.Threading;
    using static ConstValues;
    using static Math;
    using static Board;
    public class UCT_UpdatePolicy
    {
        /// <summary>
        /// 贪婪系数
        /// </summary>
        public static float E_greedy = 0.8f;
        /// <summary>
        /// Saras（Lambda）权重系数
        /// </summary>
        public static float Gamma = 0.28f;
        /// <summary>
        /// 衰减率
        /// </summary>
        private static readonly float[] Lambdas;
        /// <summary>
        /// 精度
        /// </summary>
        private const int precious = 7;
        /// <summary>
        /// 最大更新长度
        /// </summary>
        private static readonly int MaxLambdasLength;

        public static float RewardFactor = 1f;
        public static float PunishFactor = 1f;
        /// <summary>
        /// UCT搜索初始化（静态对象）
        /// </summary>
        static UCT_UpdatePolicy()
        {
            MaxLambdasLength = 0;
            Lambdas = new float[EngineBoardMax];
            for (int i = Lambdas.Length - 1; i >= 0; i--)
                Lambdas[i] = (float)Pow(lambdaSarsa, Lambdas.Length - 1 - i) * E_greedy * Gamma;
            for (int i = Lambdas.Length - 1; i >= 0; i--)
            {
                if (Round(Lambdas[i], precious) > 0)
                    MaxLambdasLength++;
            }
            //ThreadPool.SetMaxThreads(Environment.ProcessorCount * 3 / 2, Environment.ProcessorCount * 6);
        }
        /// <summary>
        /// Q表回溯（Q-Learning结合UCT）
        /// </summary>
        /// <param name="game">当前终局时的棋盘</param>
        /// <param name="QPath">QLearning的搜索路径</param>
        protected static void QLearningUpdateByPath(List<(QUCTNode state, int action)> QPath)
        {
            if (QPath.Count < 1) return;
            //得到最终分数
            var final_score = QPath[QPath.Count - 1].state.Score_Board;
            //胜利者，对路径上1的策略奖励
            var final_winner = Sign(final_score);
            //失败者，对路径上2的策略惩罚(出来挨打)
            for (int index = QPath.Count - 1; index >= 0; index--)
            {
                var state = QPath[index].state;
                var action = QPath[index].action;
                var factor = (state.CurrentPlayer == final_winner) ? RewardFactor : PunishFactor;
                var backValue = final_winner * factor;
                state.W += backValue;
                Thread.VolatileWrite(ref state.W_sa[action], Thread.VolatileRead(ref state.W_sa[action]) + backValue);
                Thread.VolatileWrite(ref state.Q_sa[action], Thread.VolatileRead(ref state.Q_sa[action]) * (1 - E_greedy) + E_greedy * (Thread.VolatileRead(ref state.W_sa[action]) / Thread.VolatileRead(ref state.N_sa[action])));
                state.Z = final_winner;
            }
        }
        protected static void UpdateWinner(List<(QUCTNode state, int action)> QPath)
        {
            if (QPath.Count < 1) return;
            var final_winner = Sign(QPath[QPath.Count - 1].state.Score_Board);
            for (int index = 0; index < QPath.Count; index++)
                QPath[index].state.Z = final_winner;
        }

        protected static void QLearningUpdateByPathEndGame(List<(QUCTNode state, int action)> QPath, sbyte Winner)
        {
            for (int index = QPath.Count - 1; index >= 0; index--)
            {
                var state = QPath[index].state;
                var action = QPath[index].action;
                var factor = (state.CurrentPlayer == Winner) ? RewardFactor : PunishFactor;
                var backValue = Winner * factor;
                state.W += backValue;
                Thread.VolatileWrite(ref state.W_sa[action], Thread.VolatileRead(ref state.W_sa[action]) + backValue);
                Thread.VolatileWrite(ref state.Q_sa[action], Thread.VolatileRead(ref state.Q_sa[action]) * (1 - E_greedy) + E_greedy * (Thread.VolatileRead(ref state.W_sa[action]) / Thread.VolatileRead(ref state.N_sa[action])));
                state.Z = Winner;
            }
        }
        /// <summary>
        /// Saras(Lambda)路径更新
        /// </summary>
        /// <param name="QPath">Q表路径</param>
        /// <param name="CurrentRoot">当前的根节点</param>
        protected static void SarsaLambdaUpdateByPath(List<(QUCTNode state, int action)> QPath)
        {
            if (Gamma == 0) return;
            if (QPath.Count < Max(MaxLambdasLength / 4, 3)) return;
            var LambdaStart = Lambdas.Length - 1;
            var LambdaEnd = Lambdas.Length - MaxLambdasLength - 1;
            var Reward = QPath[QPath.Count - 1].state.Score_Board - QPath[QPath.Count - 2].state.Score_Board;
            if (Abs(Reward) < 1) Reward = 0;
            var Advancer = Reward;
            if (Advancer != 0)
                for (int index = QPath.Count - 1; (index >= 0) && (LambdaStart > LambdaEnd); index--, LambdaStart--)
                {
                    var state = QPath[index].state;
                    var action = QPath[index].action;
                    var factor = (state.CurrentPlayer == Advancer) ? RewardFactor : PunishFactor;
                    var backValue = factor * Lambdas[LambdaStart] * Advancer;
                    state.W += backValue;
                    Thread.VolatileWrite(ref state.W_sa[action], Thread.VolatileRead(ref state.W_sa[action]) + backValue);
                    Thread.VolatileWrite(ref state.Q_sa[action], Thread.VolatileRead(ref state.Q_sa[action]) * (1 - E_greedy) + E_greedy * (Thread.VolatileRead(ref state.W_sa[action]) / Thread.VolatileRead(ref state.N_sa[action])));
                }
        }
    }
}

