using System;
using System.Collections.Generic;
using System.Linq;
namespace MUCGO_zero_CS
{
    using CNTK;
    using static ConstValues;
    using static Math;
    using static Utils;
    using static Board;
    using System.Threading;
    using System.Threading.Tasks;
    /// <summary>
    /// 只有估值的神经网络对象
    /// </summary>
    public class DNN_Eval_Only : MyPrefabsNN
    {
        #region 公共变量、名称
        protected const string FeaturesStr = "Features";
        protected const string PolicyStr = "Policy";
        protected const string ValueStr = "Value";
        protected Function EvalModel;
        protected DeviceDescriptor device;
        public static int BoardSize = EngineBoardSize;
        public static int PolicySize = BoardSize * BoardSize + 1;
        public const int MaxQueueLength = 6000;
        #endregion
        #region 内部变量
        public volatile bool EnableEvalThread = false;
        public volatile bool DisableSleep = false;
        /// <summary>
        /// 队列限制
        /// </summary>
        public int SetupQueueLength { get; set; } = 1000;
        public int CurrentQueueLength { get; private set; }
        #endregion
        #region 通用函数
        /// <summary>
        /// 获取输入变量
        /// </summary>
        /// <param name="Name">名称</param>
        /// <returns>变量名</returns>
        private Variable GetInputs(string Name)
        {
            for (var i = 0; i < EvalModel.Arguments.Count; i++)
                if (string.Equals(EvalModel.Arguments[i].Name, Name))
                    return EvalModel.Arguments[i];
            return null;
        }
        /// <summary>
        /// 获取输出变量
        /// </summary>
        /// <param name="Name">名称</param>
        /// <returns>变量名</returns>
        private Variable GetOutputs(string Name)
        {
            for (var i = 0; i < EvalModel.Outputs.Count; i++)
                if (string.Equals(EvalModel.Outputs[i].Name, Name))
                    return EvalModel.Outputs[i];
            return null;
        }
        /// <summary>
        /// 检查是否有可用的GPU
        /// </summary>
        /// <returns></returns>
        public bool HaveGPU()
        {
            return DeviceDescriptor.UseDefaultDevice().Type != DeviceKind.CPU;
        }
        /// <summary>
        /// 加载神经网络
        /// </summary>
        /// <returns></returns>
        public bool Load(string Path)
        {
            if (!CheckFile(Path)) return false;
            if (device == null) device = DeviceDescriptor.UseDefaultDevice();
            Console.Error.WriteLine($"Using {device.AsString()} as NN Runner!");
            EvalModel = Function.Load(Path, device);
            if (EvalModel == null)
                return false;
            return true;
        }
        #endregion
        #region 构造函数
        public DNN_Eval_Only()
        {
            //EvalStart();
        }
        public bool IsGPUBusy()
        {
            return EvalQueue.Count > 0 || WritebackQueue.Count > 0;
        }
        public void WaitGPUIdle()
        {
            DisableSleep = true;
            while (IsGPUBusy()) ;
            DisableSleep = false;
        }
        #endregion

        #region 回写逻辑

        /// <summary>
        /// 估值队列
        /// </summary>
        private readonly List<(QUCTNode node, IList<float> BoardStatus)> EvalQueue = new List<(QUCTNode, IList<float>)>(MaxQueueLength);
        /// <summary>
        /// 回写对象队列
        /// </summary>
        private readonly List<QUCTNode> WritebackQueue = new List<QUCTNode>(MaxQueueLength);
        /// <summary>
        /// 策略估值回写
        /// </summary>
        private readonly List<IList<float>> Policys = new List<IList<float>>(MaxQueueLength);
        /// <summary>
        /// 价值估值回写
        /// </summary>
        private readonly List<IList<float>> Values = new List<IList<float>>(MaxQueueLength);
        /// <summary>
        /// 估值同步器
        /// </summary>
        private readonly Mutex EvalMutex = new Mutex();
        /// <summary>
        /// 回写同步器
        /// </summary>
        private readonly Mutex WBMutex = new Mutex();
        /// <summary>
        /// 估值用的函数
        /// </summary>
        private void EvalThreadFunc(object obj = null)
        {
            Eval_Alive = true;
            Thread.CurrentThread.IsBackground = true;
            var Features_var = GetInputs(FeaturesStr);
            var Policy_var = GetOutputs(PolicyStr);
            var Value_var = GetOutputs(ValueStr);
            while (!StopEval)
            {
                try
                {
                    Eval_Alive = true;
                    if (EvalQueue.Count < 1) Thread.Sleep(2);
                    if (EvalQueue.Count < 1) continue;

                    List<(QUCTNode node, IList<float> BoardStatus)> evalList = null;
                    EvalMutex.WaitOne();
                    var range = Min(EvalQueue.Count, SetupQueueLength);
                    evalList = EvalQueue.GetRange(0, range);
                    EvalQueue.RemoveRange(0, range);
                    EvalMutex.ReleaseMutex();
                    if (evalList == null) continue;
                    if (evalList != null && evalList.Count > 0)
                        foreach (var (node, BoardStatus) in evalList)
                            node.Evaled = true;
                    evalList.RemoveAll(((QUCTNode node, IList<float> BoardStatus) item) => item.node == null);
                    List<IList<float>> features = new List<IList<float>>(new IList<float>[evalList.Count]);
                    Parallel.For(0, evalList.Count, (int index) => { features[index] = evalList[index].BoardStatus; });
                    Dictionary<Variable, Value> Out_Value_Maps = new Dictionary<Variable, Value>() { { Policy_var, null }, { Value_var, null } };
                    if (features.Count < 1) continue;
                    Dictionary<Variable, Value> In_Value_Maps = new Dictionary<Variable, Value>() { { Features_var, Value.CreateBatchOfSequences(new int[] { BoardSize, BoardSize, 3 }, features, device, true) } };
                    EvalModel.Evaluate(In_Value_Maps, Out_Value_Maps, device);
                    (List<IList<float>> PolicyList, List<IList<float>> ValueList) return_values = (new List<IList<float>>(), new List<IList<float>>());
                    return_values.PolicyList.AddRange(Out_Value_Maps[Policy_var].GetDenseData<float>(Policy_var));
                    return_values.ValueList.AddRange(Out_Value_Maps[Value_var].GetDenseData<float>(Value_var));
                    ThreadPool.QueueUserWorkItem((object item) =>
                    {
                        List<(QUCTNode node, IList<float> BoardStatus)> WBList = new List<(QUCTNode node, IList<float> BoardStatus)>();
                        WBList.AddRange(evalList);
                        (List<IList<float>> PolicyList, List<IList<float>> ValueList) _return_values = ((List<IList<float>>, List<IList<float>>))item;
                        if (WBList.Count > 0 && _return_values.PolicyList.Count > 0 && _return_values.ValueList.Count > 0 && (_return_values.PolicyList.Count == _return_values.ValueList.Count))
                        {
                            var CurrentCount = Min(WBList.Count, Min(_return_values.PolicyList.Count, _return_values.ValueList.Count));

                            //Parallel.For(0, CurrentCount, (int index) =>
                            //{
                            //    if (WBList[index].node != null) WBList[index].node.Writeback(_return_values.PolicyList[index], _return_values.ValueList[index]);
                            //});

                            for (int index = 0; index < CurrentCount; index++)
                            {
                                if (WBList[index].node != null)
                                    WBList[index].node.Writeback(_return_values.PolicyList[index], _return_values.ValueList[index]);
                            }
                        }
                    }, return_values);


                    //AddToWBQueue(evalList, Out_Value_Maps[Policy_var].GetDenseData<float>(Policy_var), Out_Value_Maps[Value_var].GetDenseData<float>(Value_var));
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.StackTrace);
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.Source);
                }
            }
            Eval_Alive = false;
        }
        /// <summary>
        /// 回写用的函数
        /// </summary>
        private void WriteBackThreadFunc(object obj = null)
        {
            WB_Alive = true;
            Thread.CurrentThread.IsBackground = true;
            bool RunningWriteBackCondition = false;
            int CurrentCount = 0;
            while (!StopEval && false)
            {
                try
                {
                    WB_Alive = true;
                    RunningWriteBackCondition = WritebackQueue.Count > 0 && Policys.Count > 0 && Values.Count > 0 && (Policys.Count == Values.Count);
                    if (RunningWriteBackCondition)
                    {
                        WBMutex.WaitOne();
                        CurrentCount = Min(WritebackQueue.Count, Min(Policys.Count, Values.Count));
                        for (int index = 0; index < CurrentCount; index++)
                        {
                            if (WritebackQueue[index] != null)
                            { WritebackQueue[index].Writeback(Policys[index], Values[index]); }

                        }
                        //Parallel.For(0, CurrentCount, (int index) => { if (WritebackQueue[index] != null) { WritebackQueue[index].Writeback(Policys[index], Values[index]); } });
                        WritebackQueue.RemoveRange(0, CurrentCount);
                        Policys.RemoveRange(0, CurrentCount);
                        Values.RemoveRange(0, CurrentCount);
                        WBMutex.ReleaseMutex();
                    }
                    else { if (!DisableSleep) Thread.Sleep(7); else Thread.Sleep(3); }
                }
                catch (Exception e)
                {
                    Console.Error.WriteLine(e.StackTrace);
                    Console.Error.WriteLine(e.Message);
                    Console.Error.WriteLine(e.Source);
                }
            }
            WB_Alive = false;
        }
        /// <summary>
        /// 加入回写队列
        /// </summary>
        /// <param name="EvalList"></param>
        /// <param name="_Policy"></param>
        /// <param name="_Value"></param>
        private void AddToWBQueue(List<(QUCTNode node, IList<float> BoardStatus)> EvalList, IList<IList<float>> _Policy, IList<IList<float>> _Value)
        {
            WBMutex.WaitOne();
            WritebackQueue.AddRange(EvalList.ConvertAll(((QUCTNode node, IList<float> BoardStatus) evalednode) => evalednode.node));
            Policys.AddRange(_Policy);
            Values.AddRange(_Value);
            WBMutex.ReleaseMutex();
        }
        /// <summary>
        /// 值入队列
        /// </summary>
        /// <param name="node"></param>
        public void EvalEnqueue(QUCTNode node, bool SelfPlay = false)
        {
            if (node == null) return;
            var PrePro = Thread.CurrentThread.Priority;
            while (EvalQueue.Count >= SetupQueueLength)
            {
                if (SelfPlay) break;
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                if (!DisableSleep)
                    Thread.Sleep(3);
            }
            Thread.CurrentThread.Priority = PrePro;
            if (node == null) return;
            EvalMutex.WaitOne();
            EvalQueue.Add((node, node.GetBoardState()));
            EvalMutex.ReleaseMutex();
        }
        /// <summary>
        /// 估值队列开始
        /// </summary>
        public void EvalStart()
        {
            if (!Eval_Alive)
                ThreadPool.QueueUserWorkItem(EvalThreadFunc);
            if (!WB_Alive)
                ThreadPool.QueueUserWorkItem(WriteBackThreadFunc);
        }
        /// <summary>
        /// 是否停止估值
        /// </summary>
        public bool StopEval { get; set; } = true;
        /// <summary>
        /// 估值队列线程是否活着
        /// </summary>
        public bool Eval_Alive { get; private set; } = false;
        /// <summary>
        /// 回写队列线程是否活着
        /// </summary>
        public bool WB_Alive { get; private set; } = false;
        /// <summary>
        /// 清除数据
        /// </summary>
        public void Clear()
        {
            WritebackQueue.Clear();
            EvalQueue.Clear();
            Policys.Clear();
            Values.Clear();
        }
        #endregion
    }



}
