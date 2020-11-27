using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace TestGo
{
    using System.Threading;
    using MathNet.Numerics.LinearAlgebra.Single;
    using MathNet.Numerics.Distributions;
    class Program
    {
        //delegate void TM();
        static void Main(string[] args)
        {
            //Random random = new Random(0x7FCD3241);
            //IContinuousDistribution distribution;
            //distribution = new Normal(0, 1, random);
            //var matrix = DenseMatrix.CreateRandom(2, 3, distribution);
            //matrix = (DenseMatrix)matrix.Transpose();
            //var schi = matrix.GramSchmidt();
            //matrix= (DenseMatrix)matrix.NormalizeColumns(2);
            //matrix = (DenseMatrix)matrix.Transpose();
            //var temp = matrix.TransposeAndMultiply(matrix);
            //temp= (DenseMatrix)temp.NormalizeRows(2);
            //Console.WriteLine(temp.ToString());
            //Console.ReadKey();
            //int maxium_thread = 12;
            //ThreadPool.SetMaxThreads(maxium_thread, maxium_thread);
            ////move_t x = new move_t() { color = 'w', pos = 233, hash = 12312312 };
            ////move_t y = x.DeepCopy();
            ////y.color = 'b';
            ////ThreadPool.QueueUserWorkItem
            //statistic_t temp = new statistic_t();
            //using (ManualResetEvent finish = new ManualResetEvent(false))
            //{
            //    for (int i = 0; i < 32; i++)
            //    {
            //        ThreadPool.QueueUserWorkItem((object index) => { temp[(int)index % 3] += 1; }, i);
            //        // 以原子操作的形式递减指定变量的值并存储结果。
            //        if (Interlocked.Decrement(ref maxium_thread) == 0)
            //        {
            //            // 将事件状态设置为有信号，从而允许一个或多个等待线程继续执行。
            //            finish.Set();
            //        }
            //    }
            //    finish.WaitOne();
            //}
            //SGF mysgf = new SGF();
            //mysgf.Load("H:/MUCGO/mucgo_1_0/MUC_GO_RL_RP.sgf");
            //mysgf.Save("H:/MUCGO/mucgo_1_0/MUC_GO_RL_RP.sgf");
            PlayInterface.ParameterAnaylsis(args);
            PlayInterface.MainGTP();
           
        }
    }
}
