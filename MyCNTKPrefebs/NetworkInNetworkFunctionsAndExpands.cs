using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CNTK;

namespace MyCNTKPrefebs
{
    using static CNTKLib;
    /// <summary>
    /// 网中网的复现和扩展
    /// </summary>
    public static class NetworkInNetworkFunctionsAndExpands
    {
        #region NIN Layers 层制作
        /// <summary>
        /// 基于通道的平均最大化输出，内含一个卷积层，适用于卷积层
        /// </summary>
        public static Function MaxAverageOutLayer(Variable Input, DeviceDescriptor device, int OutputDim = 1, double dropout_rate = 0.5, int c_group = 2, int coresize = 1, int step = 1)
        {
            var NewSet = MaxAverageOutPartical2(Input, device, OutputDim, dropout_rate, c_group, coresize, step);
            return Reshape(NewSet, new int[] { NewSet.Output.Shape[0], NewSet.Output.Shape[1], GetFinalDim(NewSet) });
        }
        /// <summary>
        /// 基于通道的平均最大化输出，内含一个卷积层，适用于卷积层
        /// </summary>
        public static Function MaxAverageOutLayer(Variable Input, DeviceDescriptor device, string name, int OutputDim = 1, double dropout_rate = 0.5, int c_group = 2, int coresize = 1, int step = 1)
        {
            var NewSet = MaxAverageOutPartical2(Input, device, OutputDim, dropout_rate, c_group, coresize, step);
            return Reshape(NewSet, new int[] { NewSet.Output.Shape[0], NewSet.Output.Shape[1], GetFinalDim(NewSet) }, name);
        }
        /// <summary>
        /// 最大化输出，适用于全连接层
        /// </summary>
        public static Function MaxoutLayer(Variable Input, DeviceDescriptor device, int OutputDim = 1, double dropout_rate = 0.5, int c_group = 2)
        {
            var ElementsInputCount = GetFCFinalDim(Input);
            var R_Input = Reshape(Input, new int[] { ElementsInputCount });
            var D_Input = Dropout(R_Input, dropout_rate);
            var NewSet = MaxoutPartical2(D_Input, device, OutputDim, c_group);
            var ElementsOutputCount = GetFCFinalDim(NewSet);
            return Reshape(NewSet, new int[] { ElementsOutputCount });
        }
        /// <summary>
        /// 最大化输出，适用于全连接层
        /// </summary>
        public static Function MaxoutLayer2(Variable Input, DeviceDescriptor device, string name, int OutputDim = 1, double dropout_rate = 0.5, int c_group = 2)
        {
            var ElementsInputCount = GetFCFinalDim(Input);
            var R_Input = Reshape(Input, new int[] { ElementsInputCount });
            var D_Input = Dropout(R_Input, dropout_rate);
            var NewSet = MaxoutPartical2(D_Input, device, OutputDim, c_group);
            var ElementsOutputCount = GetFCFinalDim(NewSet);
            return Reshape(NewSet, new int[] { ElementsOutputCount }, name);
        }
        /// <summary>
        /// 全局平均池化层
        /// </summary>
        public static Function GAPLayer(Variable Input, DeviceDescriptor device, int OutputDim = 1)
        {
            int numInputChannels = Input.Shape[Input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { 1, 1, numInputChannels, OutputDim }, DataType.Float, GlorotNormalInitializer(2), device);
            var conv_layer = Convolution(convParams, Input, new int[] { 1, 1, numInputChannels });
            return GlobalAveragePooling(conv_layer);
        }
        /// <summary>
        /// 全局平均池化层
        /// </summary>
        public static Function GAPLayer(Variable Input, DeviceDescriptor device, string name, int OutputDim = 1)
        {
            int numInputChannels = Input.Shape[Input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { 1, 1, numInputChannels, OutputDim }, DataType.Float, GlorotNormalInitializer(2), device);
            var conv_layer = Convolution(convParams, Input, new int[] { 1, 1, numInputChannels });
            return GlobalAveragePooling(conv_layer, name);
        }
        /// <summary>
        /// 多层全连卷积层
        /// </summary>
        public static Function MLN_Conv(Variable Input, DeviceDescriptor device, int OutputDim = 1, int HiddenDim = 1, bool BNEnabled = false, bool Spatial = false)
        {
            Function InputData;
            if (BNEnabled)
            {
                InputData = BatchNormalization(Input, new Parameter(new int[] { NDShape.InferredDimension }, 0.26f, device, ""),
                new Parameter(new int[] { NDShape.InferredDimension }, 0.0f, device, ""),
                new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device),
                new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device),
                 Constant.Scalar(0.0f, device), Spatial, BNTime);
            }
            else
            {
                InputData = Input;
            }
            int numInputChannels_layer_1 = InputData.Output.Shape[InputData.Output.Shape.Rank - 1];
            var conv_layer_1 = ReLU(Convolution(new Parameter(new int[] { 3, 3, numInputChannels_layer_1, HiddenDim }, DataType.Float,
                GlorotNormalInitializer(2), device), InputData, new int[] { 1, 1,
                    numInputChannels_layer_1 }));
            int numInputChannels_layer_2 = conv_layer_1.Output.Shape[conv_layer_1.Output.Shape.Rank - 1];
            return ReLU(Convolution(new Parameter(new int[] { 1, 1, numInputChannels_layer_2, OutputDim }, DataType.Float,
                GlorotNormalInitializer(2), device), conv_layer_1,
                new int[] { 1, 1, numInputChannels_layer_2 }));
        }
        /// <summary>
        /// 多层全连卷积层
        /// </summary>
        public static Function MLN_Conv(Variable Input, DeviceDescriptor device, string name, int OutputDim = 1, int HiddenDim = 1, bool BNEnabled = false, bool Spatial = false)
        {
            Function InputData;
            if (BNEnabled)
            {
                InputData = BatchNormalization(Input, new Parameter(new int[] { NDShape.InferredDimension }, 0.26f, device, ""),
                new Parameter(new int[] { NDShape.InferredDimension }, 0.0f, device, ""),
                new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device),
                new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device),
                 Constant.Scalar(0.0f, device), Spatial, BNTime);
            }
            else
            {
                InputData = Input;
            }
            int numInputChannels_layer_1 = InputData.Output.Shape[InputData.Output.Shape.Rank - 1];
            var conv_layer_1 = ReLU(Convolution(new Parameter(new int[] { 3, 3, numInputChannels_layer_1, HiddenDim }, DataType.Float,
                GlorotNormalInitializer(2), device), InputData, new int[] { 1, 1,
                    numInputChannels_layer_1 }));
            int numInputChannels_layer_2 = conv_layer_1.Output.Shape[conv_layer_1.Output.Shape.Rank - 1];
            return ReLU(Convolution(new Parameter(new int[] { 1, 1, numInputChannels_layer_2, OutputDim }, DataType.Float,
                GlorotNormalInitializer(2), device), conv_layer_1,
                new int[] { 1, 1, numInputChannels_layer_2 }), name);
        }
        #endregion
        #region Maxout、GAP基础函数
        /// <summary>
        /// 全局特征图平均值
        /// 要求大小为2*2*n
        /// 其中n>=1
        /// 若是一维向量
        /// 返回则是其中Average值
        /// 约等于为Average函数
        /// </summary>
        private static Function GlobalAveragePooling(Variable Input)
        {
            if (Input.Shape.Rank < 2) return Pooling(Input, PoolingType.Average, new int[] { Input.Shape[0] }, new int[] { Input.Shape[0] }, new BoolVector { false });
            return Pooling(Input, PoolingType.Average, new int[] { Input.Shape[0], Input.Shape[1] }, new int[] { Input.Shape[0], Input.Shape[1] }, new BoolVector { false });
        }
        private static Function GlobalAveragePooling(Variable Input, string name)
        {
            if (Input.Shape.Rank < 2) return Pooling(Input, PoolingType.Average, new int[] { Input.Shape[0] }, new int[] { Input.Shape[0] }, new BoolVector { false });
            return Pooling(Input, PoolingType.Average, new int[] { Input.Shape[0], Input.Shape[1] }, new int[] { Input.Shape[0], Input.Shape[1] }, new BoolVector { false }, false, false, name);
        }
        private static Function MaxoutPartical2(Variable Input, DeviceDescriptor device, int OutputDim = 1, int c_group = 2)
        {
            var ElementsInputCount = GetFCFinalDim(Input);
            var R_Input = Reshape(Input, new int[] { ElementsInputCount });
            VariableVector variables = new VariableVector();
            for (int i = 0; i < OutputDim; i++)
                variables.Add(MaxoutPartical(R_Input, device, c_group));
            return Splice(variables, Axis.EndStaticAxis());
        }
        private static Function MaxoutPartical(Variable Input, DeviceDescriptor device, int c_group = 2)
        {
            int inputDim = GetFCFinalDim(Input);
            var timesParam = new Parameter(new int[] { c_group, inputDim }, DataType.Float, GlorotUniformInitializer(DefaultParamInitScale, SentinelValueForInferParamInitRank, SentinelValueForInferParamInitRank, 1), device);
            var timesFunction = Times(timesParam, Input);
            var Node = Plus(new Parameter(new int[] { c_group }, DataType.Float, 0.0f, device), timesFunction);
            return ReduceMax(Node, Axis.EndStaticAxis());
        }
        private static Function MaxAverageOutPartical2(Variable Input, DeviceDescriptor device, int OutputDim = 1, double dropout_rate = 0.5, int c_group = 2, int coresize = 1, int step = 1)
        {
            int numInputChannels = Input.Shape[Input.Shape.Rank - 1];
            var ConstData = new Constant(new int[3] { 1, 1, numInputChannels }, DataType.Float, 1);
            var DropoutInfo = Dropout(ConstData, dropout_rate);
            var SwitchOut = ElementTimes(Input, DropoutInfo);
            VariableVector variables = new VariableVector();
            for (int i = 0; i < OutputDim; i++)
                variables.Add(MaxAverageOutPartical(SwitchOut, device, c_group, coresize, step));
            return Splice(variables, Axis.EndStaticAxis());
        }
        private static Function MaxAverageOutPartical(Variable Input, DeviceDescriptor device, int c_group = 2, int coresize = 1, int step = 1)
        {
            int numInputChannels = Input.Shape[Input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { coresize, coresize, numInputChannels, c_group }, DataType.Float, GlorotNormalInitializer(2), device);
            var conv_layer = Convolution(convParams, Input, new int[] { step, step, numInputChannels });
            var PoolingAvg = Pooling(conv_layer, PoolingType.Average, new int[] { conv_layer.Output.Shape[0], conv_layer.Output.Shape[1] }, new int[] { conv_layer.Output.Shape[0], conv_layer.Output.Shape[1], 1 });
            var MaxValue = Reshape(Pooling(PoolingAvg, PoolingType.Max, PoolingAvg.Output.Shape, PoolingAvg.Output.Shape), new int[] { 1 });
            var GE = GreaterEqual(PoolingAvg, MaxValue);
            var SwitchOut = ElementTimes(GE, conv_layer);
            var convConst_Input = SwitchOut.Output.Shape[SwitchOut.Output.Shape.Rank - 1];
            var convConst = new Constant(new int[] { 1, 1, convConst_Input, 1 }, DataType.Float, 1.0, device);
            var convConstConv = Convolution(convConst, SwitchOut, new int[] { step, step, convConst_Input });
            return convConstConv;
        }
        private static int GetFinalDim(Variable Input)
        {
            int count = 1;
            for (int i = 2; i < Input.Shape.Rank; i++)
                count *= Input.Shape[i];
            return count;
        }
        private static int GetFCFinalDim(Variable Input)
        {
            int count = 1;
            for (int i = 0; i < Input.Shape.Rank; i++)
                count *= Input.Shape[i];
            return count;
        }
        #endregion
        #region Fixed Paramters
        const int BNTime = 4096;
        #endregion
    }
}
