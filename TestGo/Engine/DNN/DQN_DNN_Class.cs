using System;
using System.Collections.Generic;
using System.Linq;
namespace MUCGO_zero_CS
{
    using CNTK;
    using static CNTK.CNTKLib;
    using static NN_Model;
    using static ConstValues;
    using static Math;
    using static Utils;
    using static Board;
    /// <summary>
    /// 深度强化学习的常用功能部件
    /// </summary>
    public class DQN_DNN_Class : DNN_Eval_Only
    {
        #region 公共变量、名称
        private const string LabelsPieStr = "LabelsPie";
        private const string LabelsZStr = "LabelsZ";
        #endregion
        #region 内部变量
        private string DefaultFileName;
        private int TrainingSize;
        /// <summary>
        /// 学习率
        /// </summary>
        public static double LearningRate { get; set; } = 0.001;
        /// <summary>
        /// 最小批次
        /// </summary>
        public static uint MiniBatch { get; set; } = 64;
        /// <summary>
        /// 迭代次数
        /// </summary>
        public static uint Epouch { get; set; } = 10;
        #endregion
        #region 静态方法
        /// <summary>
        /// 损失函数
        /// </summary>
        /// <param name="LabelsPie"></param>
        /// <param name="Policy_var"></param>
        /// <param name="LabelsZ"></param>
        /// <param name="Value_var"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private static Function MyLossFunction(Variable LabelsPie, Variable Policy_var, Variable LabelsZ, Variable Value_var, DeviceDescriptor device)
        {
            var Value_Loss = Square(Minus(Value_var, LabelsZ), "Value_Loss");
            var ReshapeP = Reshape(Plus(Policy_var, new Constant(new int[] { PolicySize }, DataType.Float, 2e-8f, device)), new int[] { PolicySize, 1 });
            var ReshapePie = Reshape(LabelsPie, new int[] { 1, PolicySize });
            var CrossE_P = Reshape(Times(ReshapePie, Log(ReshapeP)), new int[] { 1 }, "Policy_Loss");
            return Minus(Value_Loss, CrossE_P, "TotalLoss");
        }
        /// <summary>
        /// 错误率函数
        /// </summary>
        /// <param name="LabelsPie"></param>
        /// <param name="Policy_var"></param>
        /// <param name="LabelsZ"></param>
        /// <param name="Value_var"></param>
        /// <param name="device"></param>
        /// <returns></returns>
        private static Function MyErrorFunction(Variable LabelsPie, Variable Policy_var, Variable LabelsZ, Variable Value_var, DeviceDescriptor device)
        {
            var Value_Error = Square(Minus(Value_var, LabelsZ), "Value_Error");
            var Policy_Error = Reshape(Times(Reshape(Square(Minus(Policy_var, LabelsPie)), new int[] { 1, PolicySize }), new Constant(new int[] { PolicySize, 1 }, DataType.Float, 1, device)), new int[] { 1 }, "Policy_Error");
            return Plus(Policy_Error, Value_Error, "TotalError");
        }
        #endregion
        #region 构造函数
        public DQN_DNN_Class() : base()
        {
            //ModelPerTrainingSize = new List<(NN_Model model, int size)>() {(Mutl_Layer_NN,16 ),(Conv_NN,11 ),(ResNet_NN,9 ),(UNet_NN,12 ), (Mutl_Layer_ELM,16 ), (ResNet_ELM,16 ), (UNet_ELM,16 ), (Conv_ELM,16 )};
            //ModelType = Conv_NN;
            DefaultFileName = GetFileName(ModelType);
            device = DeviceDescriptor.UseDefaultDevice();
        }
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
        /// 创建神经网络
        /// </summary>
        /// <param name="device"></param>
        /// <param name="_nn_model"></param>
        /// <returns></returns>
        public bool CreateModel(DeviceDescriptor device, NN_Model _nn_model = Conv_NN)
        {
            this.device = device;
            ModelType = _nn_model;
            var Inputs = InputVariable(new int[] { BoardSize, BoardSize, 3 }, DataType.Float, FeaturesStr);
            DefaultFileName = GetFileName(ModelType);
            switch (ModelType)
            {
                case Mutl_Layer_NN:
                    EvalModel = MLNCreator(Inputs, this.device, BoardSize, PolicyStr, ValueStr);
                    Save();
                    return true;
                case Conv_NN:
                    EvalModel = CNNCreator(Inputs, this.device, BoardSize, PolicyStr, ValueStr);
                    Save();
                    return true;
                case ResNet_NN:
                    EvalModel = ResNetCreator(Inputs, this.device, BoardSize, PolicyStr, ValueStr);
                    Save();
                    return true;
                case UNet_NN:
                    EvalModel = UNetCreator(Inputs, this.device, BoardSize, PolicyStr, ValueStr);
                    Save();
                    return true;
                default:
                    return false;
            }
        }
        /// <summary>
        /// 加载神经网络
        /// </summary>
        /// <returns></returns>
        public bool Load()
        {
            DefaultFileName = GetFileName(ModelType);
            if (!CheckFile(DefaultFileName)) return false;
            if (device == null) device = DeviceDescriptor.UseDefaultDevice();
            Console.Error.WriteLine($"Using {device.AsString()} as NN Runner!");
            EvalModel = Function.Load(DefaultFileName, device);
            TrainingSize = ModelPerTrainingSize.Find(((NN_Model model, int size) item) => item.model == ModelType).size;
            if (EvalModel == null)
                return false;
            return true;
        }
        /// <summary>
        /// 保存神经网络
        /// </summary>
        public void Save()
        {
            DefaultFileName = GetFileName(ModelType);
            if (EvalModel != null)
            {
                EvalModel.Save(DefaultFileName);
            }
        }
        #endregion
        #region 训练用的函数
        /// <summary>
        /// 训练网络函数
        /// </summary>
        /// <param name="trainDatas"></param>
        /// <param name="learning_rate"></param>
        /// <param name="epouch"></param>
        /// <param name="minibatchsize"></param>
        public void Train(List<QUCTNode> trainDatas, Func<int, float> GetPolicyFactions = null)
        {
            try
            {
                var blocksize = 1 << TrainingSize;
                var remainblock_size = trainDatas.Count % blocksize;
                var blockcount = trainDatas.Count / blocksize;
                //var additionalLearningOptions = new AdditionalLearningOptions() { l2RegularizationWeight = 0.0001 };
                //var MomentumSchedulePerSample = new TrainingParameterScheduleDouble(new VectorPairSizeTDouble() { new PairSizeTDouble(DataUnit.Sample, 0.9), new PairSizeTDouble(2, 0.999), new PairSizeTDouble(3, 1e-8) }, (uint)epouch, (uint)minibatchsize);
                var Features_var = GetInputs(FeaturesStr);
                var Policy_var = GetOutputs(PolicyStr);
                var Value_var = GetOutputs(ValueStr);
                var LabelsZ = InputVariable(Value_var.Shape, DataType.Float, LabelsZStr);
                var LabelsPie = InputVariable(Policy_var.Shape, DataType.Float, LabelsPieStr);
                Dictionary<Variable, Value> TranstrainDatasToVector(List<QUCTNode> _trainDatas)
                {
                    var banch_size = _trainDatas.Count;
                    IList<float>[] Features = new IList<float>[banch_size];
                    IList<float>[] LabelPies = new IList<float>[banch_size];
                    IList<float>[] LabelZs = new IList<float>[banch_size];
                    for (var i = 0; i < banch_size; i++)
                    {
                        Features[i] = _trainDatas[i].GetBoardState();
                        LabelPies[i] = _trainDatas[i].GetPieData();
                        LabelZs[i] = _trainDatas[i].GetZData();
                    }
                    Dictionary<Variable, Value> datas = new Dictionary<Variable, Value>() {
                        { Features_var,Value.CreateBatchOfSequences(new int[]{ BoardSize,BoardSize, 3 }, Features, device, false)},
                        { LabelsPie,Value. CreateBatchOfSequences(new int[]{PolicySize }, LabelPies, device, false)},
                        { LabelsZ, Value.CreateBatchOfSequences(new int[]{ 1 }, LabelZs, device, false)}
                    };
                    return datas;
                }
                /*准备损失函数和错误率函数*/
                //Function lossFunction = Plus(Square(Minus(Value_var, LabelsZ)), CrossEntropyWithSoftmax(Policy_var, LabelsPie), "Loss Function");//   MyLossFunction(LabelsPie, Policy_var, LabelsZ, Value_var, device);
                //Function prediction = Plus(ClassificationError(Policy_var, LabelsPie), SquaredError(Value_var, LabelsZ), "Prediction");// MyErrorFunction(LabelsPie, Policy_var, LabelsZ, Value_var, device);              
                var MyLearner = Learner.SGDLearner(EvalModel.Parameters(), new TrainingParameterScheduleDouble(LearningRate, (uint)trainDatas.Count), new AdditionalLearningOptions() { l2RegularizationWeight = 0.0001 });
                var MyAdamLearner = AdamLearner(
                        /*需要调节的参数*/
                        new ParameterVector(EvalModel.Parameters().ToList()),
                    /*学习率以及最小批训练数目*/
                    new TrainingParameterScheduleDouble(0.0078125, 10),
                    /*beta1,动量的第一个参数*/
                    new TrainingParameterScheduleDouble(0.9),
                    /*默认为真*/
                    true,
                        /*beta2,动量的第二个参数*/
                        new TrainingParameterScheduleDouble(0.999),
                        /*epsilon*/
                        10e-8,
                        /*adam_max*/
                        false,
                        /*L2 Regularization Weight*/
                        new AdditionalLearningOptions() { l2RegularizationWeight = 0.0001 });
                var Path = GetFileName(ModelType) + "_CheckPoint";
                Function lossFunction = Plus(SquaredError(Value_var, LabelsZ), CrossEntropyWithSoftmax(Policy_var, LabelsPie), "Loss Function");
                Function prediction = Plus(ClassificationError(Policy_var, LabelsPie), ClassificationError(Value_var, LabelsZ), "Prediction");
                var trainer = Trainer.CreateTrainer(EvalModel, lossFunction, prediction, new List<Learner> { MyLearner });
                //CNTKDictionary dictionary = null;
                //if (CheckFile(Path))
                //    dictionary = trainer.RestoreFromCheckpoint(Path);
                for (int epouch = 0; epouch < Epouch; epouch++)
                {
                    int begin = 0;
                    int end = trainDatas.Count;
                    /*数据切块并训练*/
                    for (var blockpos = 1; blockpos <= blockcount; blockpos++)
                    {
                        trainer.TrainMinibatch(TranstrainDatasToVector(trainDatas.GetRange(begin, blocksize)), true, device);
                        begin += blocksize;
                    }
                    if (remainblock_size > 0)
                    {
                        trainer.TrainMinibatch(TranstrainDatasToVector(trainDatas.GetRange(begin, end - begin)), true, device);
                    }
                }
                //if (dictionary == null)
                //    trainer.SaveCheckpoint(Path);
                //else
                //    trainer.SaveCheckpoint(Path, dictionary);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.Source);
                throw e;
                //Console.ReadKey();
            }
        }
        #endregion
        #region 辅助函数
        /// <summary>
        /// 得到文件名
        /// </summary>
        /// <returns></returns>
        private static string GetFileName(NN_Model ModelType)
        {
            string DefaultFileName = "";
            DefaultFileName += _Path;
            DefaultFileName += RL_NM;
            DefaultFileName += Model_expand[(int)ModelType];
            return DefaultFileName;
        }
        /// <summary>
        /// 设置所使用的模型
        /// </summary>
        /// <param name="_nn_model"></param>
        /// <returns></returns>
        public string SetupModel(NN_Model _nn_model = Conv_NN)
        {
            ModelType = _nn_model;
            DefaultFileName = GetFileName(ModelType);
            return DefaultFileName;
        }
        #endregion
        #region 全局设置
        /// <summary>
        /// 模型类型
        /// </summary>
        public static NN_Model ModelType { get; set; }
        /// <summary>
        /// 模型大小对
        /// </summary>
        public static List<(NN_Model model, int size)> ModelPerTrainingSize { get; set; }
        #endregion
        #region 异常处理（显存溢出）
        /// <summary>
        /// 减少每批次的样本数量
        /// </summary>
        void ChangeSize()
        {
            if (TrainingSize > 1)
                TrainingSize--;
        }
        #endregion
    }
}
