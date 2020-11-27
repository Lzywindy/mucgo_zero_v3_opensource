using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace TestGo
{
    using CNTK;
    using MathNet.Numerics.LinearAlgebra.Single;
    using MathNet.Numerics.Distributions;
    using static CNTK.CNTKLib;
    using static Activation;
    using static RandomType;
    using static InitiateSeletction;
    using static NN_Model;
    using static RL_Mode;
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
    using static Math;
    using static Utils;
    using System.Threading;
    using System.Collections.ObjectModel;

    enum Activation : byte { None_Func, ReLU_Func, Sigmoid_Func, Tanh_Func };
    enum RandomType : byte
    {
        use_gamma_distribution,
        use_uniform_real_distribution,
        use_normal_distribution,
        use_lognormal_distribution,
        use_studentT_distribution,
        use_beta_distribution
    };
    enum InitiateSeletction : byte
    {
        None_Sel = 0,
        Orth_Sel = 0x1,
        RC_Sel = 0x2,
        Normalized_Sel = 0x4,
        Scale_Sel = 0x8
    };

    /// <summary>
    /// 基本NN模块
    /// </summary>
    class NNBasicModel
    {
        /// <summary>
        /// 卷积批量正则化自动填充边界
        /// </summary>
        /// <param name="input">入口</param>
        /// <param name="outFeatureMapCount">输出特征图</param>
        /// <param name="kernelWidth">卷积核宽度</param>
        /// <param name="kernelHeight">卷积核深度</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">垂直步长</param>
        /// <param name="wScale">初始化权重的大小</param>
        /// <param name="bValue">初始化权重的偏移</param>
        /// <param name="scValue">缩放</param>
        /// <param name="bnTimeConst">正则化次数</param>
        /// <param name="spatial">是否使用空间卷积</param>
        /// <param name="device">输入的设备</param>
        /// <returns>计算图</returns>
		protected static Function ConvBatchNormalizationLayer(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount }, DataType.Float, GlorotUniformInitializer(wScale, -1, 2), device);
            var convFunction = Convolution(convParams, input, new int[] { hStride, vStride, numInputChannels });
            var biasParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)bValue, device, "");
            var scaleParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)scValue, device, "");
            var runningMean = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningInvStd = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningCount = Constant.Scalar(0.0f, device);
            return BatchNormalization(convFunction, scaleParams, biasParams, runningMean, runningInvStd, runningCount, spatial, bnTimeConst, 0.0, 1e-5 /* epsilon */);
        }
        /// <summary>
        /// 卷积批量正则化自动填充边界ReLU激活
        /// </summary>
        /// <param name="input">入口</param>
        /// <param name="outFeatureMapCount">输出特征图</param>
        /// <param name="kernelWidth">卷积核宽度</param>
        /// <param name="kernelHeight">卷积核深度</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">垂直步长</param>
        /// <param name="wScale">初始化权重的大小</param>
        /// <param name="bValue">初始化权重的偏移</param>
        /// <param name="scValue">缩放</param>
        /// <param name="bnTimeConst">正则化次数</param>
        /// <param name="spatial">是否使用空间卷积</param>
        /// <param name="device">输入的设备</param>
        /// <returns>计算图</returns>
        protected static Function ConvBatchNormalizationReLULayer(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            return ReLU(ConvBatchNormalizationLayer(input, outFeatureMapCount, kernelWidth, kernelHeight, hStride, vStride, wScale, bValue, scValue, bnTimeConst, spatial, device));
        }
        /// <summary>
        /// 投影层
        /// </summary>
        /// <param name="wProj">投影层权重</param>
        /// <param name="input">输入</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">垂直步长</param>
        /// <param name="bValue">初始化权重的偏移</param>
        /// <param name="scValue">缩放</param>
        /// <param name="bnTimeConst">正则化次数</param>
        /// <param name="device">输入的设备</param>
        /// <returns>计算图</returns>
        protected static Function ProjectLayer(Variable wProj, Variable input, int hStride, int vStride, double bValue, double scValue, int bnTimeConst, DeviceDescriptor device)
        {
            var outFeatureMapCount = wProj.Shape[0];
            var b = new Parameter(new int[] { outFeatureMapCount }, (float)bValue, device, "");
            var sc = new Parameter(new int[] { outFeatureMapCount }, (float)scValue, device, "");
            var m = new Constant(new int[] { outFeatureMapCount }, 0.0f, device);
            var v = new Constant(new int[] { outFeatureMapCount }, 0.0f, device);
            var n = Constant.Scalar(0.0f, device);
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            var c = Convolution(wProj, input, new int[] { hStride, vStride, numInputChannels }, new bool[] { true }, new bool[] { false });
            return BatchNormalization(c, sc, b, m, v, n, true /*spatial*/, bnTimeConst, 0, 1e-5, false);
        }
        /// <summary>
        /// 得到投影层参数
        /// </summary>
        /// <param name="outputDim">输出维度</param>
        /// <param name="inputDim">输入维度</param>
        /// <param name="device">设备</param>
        /// <returns>计算图</returns>
        protected static Constant GetProjectionMap(int outputDim, int inputDim, DeviceDescriptor device)
        {
            if (inputDim > outputDim)
            {
                Console.Error.WriteLine("Can only project from lower to higher dimensionality");
                Environment.Exit(0x1);
            }
            var projectionMapValues = new float[inputDim * outputDim];
            for (int i = 0; i < inputDim * outputDim; i++)
                projectionMapValues[i] = 0;
            for (int i = 0; i < inputDim; ++i)
                projectionMapValues[(i * inputDim) + i] = 1.0f;
            NDArrayView projectionMap = new NDArrayView(DataType.Float, new int[] { 1, 1, inputDim, outputDim }, device);
            projectionMap.CopyFrom(new NDArrayView(new int[] { 1, 1, inputDim, outputDim }, projectionMapValues, device));
            return new Constant(projectionMap);
        }
        /// <summary>
        /// 不自动填充边界的卷积正则化层
        /// </summary>
        /// <param name="input">入口</param>
        /// <param name="outFeatureMapCount">输出特征图</param>
        /// <param name="kernelWidth">卷积核宽度</param>
        /// <param name="kernelHeight">卷积核深度</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">垂直步长</param>
        /// <param name="wScale">初始化权重的大小</param>
        /// <param name="bValue">初始化权重的偏移</param>
        /// <param name="scValue">缩放</param>
        /// <param name="bnTimeConst">正则化次数</param>
        /// <param name="spatial">是否使用空间卷积</param>
        /// <param name="device">输入的设备</param>
        /// <returns>计算图</returns>
        protected static Function ConvBatchNormalizationLayerNoPadding(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount }, DataType.Float, GlorotUniformInitializer(wScale, -1, 2), device);
            var convFunction = Convolution(convParams, input, new int[] { hStride, vStride, numInputChannels }, new BoolVector() { true }, new BoolVector() { false });
            var biasParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)bValue, device, "");
            var scaleParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)scValue, device, "");
            var runningMean = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningInvStd = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningCount = Constant.Scalar(0.0f, device);
            return BatchNormalization(convFunction, scaleParams, biasParams, runningMean, runningInvStd, runningCount, spatial, bnTimeConst, 0.0, 1e-5 /* epsilon */);
        }
        /// <summary>
        /// 不自动填充边界的反卷积正则化层
        /// </summary>
        /// <param name="input">入口</param>
        /// <param name="outFeatureMapCount">输出特征图</param>
        /// <param name="kernelWidth">卷积核宽度</param>
        /// <param name="kernelHeight">卷积核深度</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">垂直步长</param>
        /// <param name="wScale">初始化权重的大小</param>
        /// <param name="bValue">初始化权重的偏移</param>
        /// <param name="scValue">缩放</param>
        /// <param name="bnTimeConst">正则化次数</param>
        /// <param name="spatial">是否使用空间卷积</param>
        /// <param name="device">输入的设备</param>
        /// <returns>计算图</returns>
        protected static Function TransConvBatchNormalizationLayerNoPadding(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { kernelWidth, kernelHeight, outFeatureMapCount, numInputChannels }, DataType.Float, GlorotUniformInitializer(wScale, -1, 2), device);
            var convFunction = ConvolutionTranspose(convParams, input, new int[] { hStride, vStride, outFeatureMapCount }, new BoolVector() { true }, new BoolVector() { false });
            var biasParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)bValue, device, "");
            var scaleParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)scValue, device, "");
            var runningMean = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningInvStd = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningCount = Constant.Scalar(0.0f, device);
            return BatchNormalization(convFunction, scaleParams, biasParams, runningMean, runningInvStd, runningCount, spatial, bnTimeConst, 0.0, 1e-5 /* epsilon */);
        }
        /// <summary>
        /// 全连接层
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="outputDim">输出数量</param>
        /// <param name="device">使用的设备</param>
        /// <param name="outputName">层名称</param>
        /// <returns>计算图</returns>
        protected static Function FullyConnectedLinearLayer(Variable input, int outputDim, DeviceDescriptor device, string outputName = "")
        {
            int inputDim = 1;
            foreach (var node in input.Shape.Dimensions)
                inputDim *= node;
            var timesParam = new Parameter(new int[] { outputDim, inputDim }, DataType.Float, GlorotUniformInitializer(DefaultParamInitScale, SentinelValueForInferParamInitRank, SentinelValueForInferParamInitRank, 1), device, "timesParam");
            var timesFunction = Times(timesParam, input, "times");
            return Plus(new Parameter(new int[] { outputDim }, 0.0f, device, "plusParam"), timesFunction, outputName);
        }
        /*全连接层带封装*/
        protected static Function Dense(Variable input, int outputDim, DeviceDescriptor device, Activation activation = None_Func, string outputName = "")
        {
            int newDim = 1;
            foreach (var node in input.Shape.Dimensions)
                newDim *= node;
            switch (activation)
            {
                default:
                case None_Func:
                    return FullyConnectedLinearLayer(Reshape(input, new int[] { newDim }), outputDim, device, outputName);
                case ReLU_Func:
                    return ReLU(FullyConnectedLinearLayer(Reshape(input, new int[] { newDim }), outputDim, device), outputName);
                case Sigmoid_Func:
                    return Sigmoid(FullyConnectedLinearLayer(Reshape(input, new int[] { newDim }), outputDim, device), outputName);
                case Tanh_Func:
                    return Tanh(FullyConnectedLinearLayer(Reshape(input, new int[] { newDim }), outputDim, device), outputName);
            }
        }
        /*残差层*/
        protected static Function ResNetNode(Variable input, int InternalMapCounts, int kernelWidth, int kernelHeight, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int map_counts = input.Shape[input.Shape.Rank - 1];
            var c1 = ConvBatchNormalizationReLULayer(input, InternalMapCounts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var c2 = ConvBatchNormalizationLayer(c1, map_counts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var p = Plus(c2, input);
            return ReLU(p);
        }
        /*残差特征图扩充层*/
        protected static Function ResNetNodeInc(Variable input, int InternalMapCounts, int outFeatureMapCount, int kernelWidth, int kernelHeight, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int map_counts = input.Shape[input.Shape.Rank - 1];
            var c1 = ConvBatchNormalizationReLULayer(input, InternalMapCounts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var c2 = ConvBatchNormalizationLayer(c1, map_counts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var p = Plus(c2, input);
            return ConvBatchNormalizationReLULayer(p, outFeatureMapCount, 1, 1, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
        }
        /// <summary>
        /// 矩阵初始化部分(用于常数层初始化)
        /// </summary>
        /// <param name="row">行数</param>
        /// <param name="col">列数</param>
        /// <param name="scale">矩阵中值缩放</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>特定的随机矩阵</returns>
        protected static float[] MatrixInitiate(int row, int col, float scale = 1.0f, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            Random random = new Random(0x7FCD3241);
            IContinuousDistribution distribution;
            switch (type)
            {
                case use_gamma_distribution:
                    distribution = new Gamma(0.5, 0.5, random);
                    break;
                case use_uniform_real_distribution:
                    distribution = new ContinuousUniform(-1, 1, random);
                    break;
                case use_normal_distribution:
                    distribution = new Normal(0, 1, random);
                    break;
                case use_lognormal_distribution:
                    distribution = new LogNormal(1, 1);
                    break;
                case use_studentT_distribution:
                    distribution = new StudentT(0.5, 1, 0.25, random);
                    break;
                case use_beta_distribution:
                    distribution = new Beta(1, 2, random);
                    break;
                default:
                    distribution = new Stable(1, 0, 0.5, 0, random);
                    break;
            }
            var matrix = DenseMatrix.CreateRandom(row, col, distribution);
            if ((Seletction & Normalized_Sel) != 0)
            {
                if ((Seletction & RC_Sel) != 0)
                    matrix.NormalizeRows(2);
                else
                    matrix.NormalizeColumns(2);
            }
            if ((Seletction & Orth_Sel) != 0)
            {
                if (matrix.RowCount < matrix.ColumnCount)
                {
                    matrix = (DenseMatrix)matrix.Transpose();
                    matrix.GramSchmidt();
                    matrix = (DenseMatrix)matrix.NormalizeColumns(2);
                    matrix = (DenseMatrix)matrix.Transpose();
                }
                else
                {
                    matrix.GramSchmidt();
                    matrix = (DenseMatrix)matrix.NormalizeColumns(2);
                }
            }
            if ((Seletction & Scale_Sel) != 0)
            {
                matrix.Multiply(scale);
            }
            return matrix.ToRowMajorArray();
        }
        /// <summary>
        /// 全连接层参数设置(超限学习不调参层参数)
        /// </summary>
        /// <param name="inputDim">输入维度</param>
        /// <param name="outputDim">输出维度</param>
        /// <param name="device">使用的设备</param>
        /// <param name="scale">层参数缩放</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <param name="id">参数名称</param>
        /// <returns>固定参数</returns>
        protected static Variable ELMParameters4Dense(int inputDim, int outputDim, DeviceDescriptor device, float scale = 1.0f, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution, string id = "")
        {
            var MapValues = MatrixInitiate(outputDim, inputDim, scale, Seletction, type);
            NDArrayView MyMap = new NDArrayView(DataType.Float, new int[] { outputDim, inputDim }, device);
            MyMap.CopyFrom(new NDArrayView(new int[] { outputDim, inputDim }, MapValues, device));
            return new Constant(MyMap);
        }
        /// <summary>
        /// 卷积层参数设置(超限学习不调参层参数)
        /// </summary>
        /// <param name="kernelWidth">核宽度</param>
        /// <param name="kernelHeight">核高度</param>
        /// <param name="numInputChannels">输入的数量</param>
        /// <param name="outFeatureMapCount">特征图数量</param>
        /// <param name="device">设备</param>
        /// <param name="scale">大小</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>固定参数</returns>
        protected static Constant ELMParameters4Convolution(int kernelWidth, int kernelHeight, int numInputChannels, int outFeatureMapCount, DeviceDescriptor device, float scale = 1.0f, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int row_count = kernelWidth * kernelHeight * numInputChannels;
            int col_count = outFeatureMapCount;
            var MapValues = MatrixInitiate(row_count, col_count, scale, Seletction, type);
            NDArrayView MyMap = new NDArrayView(DataType.Float, new int[] { kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount }, device);
            MyMap.CopyFrom(new NDArrayView(new int[] { kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount }, MapValues, device));
            return new Constant(MyMap);
        }
        /// <summary>
        /// 反卷积层参数设置(超限学习不调参层参数)
        /// </summary>
        /// <param name="kernelWidth">核宽度</param>
        /// <param name="kernelHeight">核高度</param>
        /// <param name="numInputChannels">输入的数量</param>
        /// <param name="outFeatureMapCount">特征图数量</param>
        /// <param name="device">设备</param>
        /// <param name="scale">大小</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>固定参数</returns>
        protected static Constant ELMParameters4TransposeConvolution(int kernelWidth, int kernelHeight, int numInputChannels, int outFeatureMapCount, DeviceDescriptor device, float scale = 1.0f, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int row_count = kernelWidth * kernelHeight * outFeatureMapCount;
            int col_count = numInputChannels;
            var MapValues = MatrixInitiate(row_count, col_count, scale, Seletction, type);
            NDArrayView MyMap = new NDArrayView(DataType.Float, new int[] { kernelWidth, kernelHeight, outFeatureMapCount, numInputChannels }, device);
            MyMap.CopyFrom(new NDArrayView(new int[] { kernelWidth, kernelHeight, outFeatureMapCount, numInputChannels }, MapValues, device));
            return new Constant(MyMap);
        }
        /// <summary>
        /// 卷积自动填充边界
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="outFeatureMapCount">特征图数量</param>
        /// <param name="kernelWidth">核宽度</param>
        /// <param name="kernelHeight">核高度</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">纵向步长</param>
        /// <param name="device">设备</param>
        /// <param name="scale">缩放</param>
        /// <param name="spatial">是否空间卷积</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>计算图</returns>
        protected static Function ELMConvLayer(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, DeviceDescriptor device, float scale = 1.0f, bool spatial = true, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            return Convolution(ELMParameters4Convolution(kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount, device, scale, Seletction, type), input, new int[] { hStride, vStride, numInputChannels });
        }
        /// <summary>
        /// 不自动填充边界的卷积层
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="outFeatureMapCount">特征图数量</param>
        /// <param name="kernelWidth">核宽度</param>
        /// <param name="kernelHeight">核高度</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">纵向步长</param>
        /// <param name="device">设备</param>
        /// <param name="scale">缩放</param>
        /// <param name="spatial">是否空间卷积</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>计算图</returns>
        protected static Function ELMConvLayerNoPadding(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, DeviceDescriptor device, float scale = 1.0f, bool spatial = true, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            return Convolution(ELMParameters4Convolution(kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount, device, scale, Seletction, type), input, new int[] { hStride, vStride, numInputChannels }, new BoolVector() { true }, new BoolVector() { false });
        }
        /// <summary>
        /// 不自动填充边界的反卷积层
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="outFeatureMapCount">特征图数量</param>
        /// <param name="kernelWidth">核宽度</param>
        /// <param name="kernelHeight">核高度</param>
        /// <param name="hStride">横向步长</param>
        /// <param name="vStride">纵向步长</param>
        /// <param name="device">设备</param>
        /// <param name="scale">缩放</param>
        /// <param name="spatial">是否空间卷积</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>计算图</returns>
        protected static Function TransELMConvLayerNoPadding(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, DeviceDescriptor device, float scale = 1.0f, bool spatial = true, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            return ConvolutionTranspose(ELMParameters4TransposeConvolution(kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount, device, scale, Seletction, type), input, new int[] { hStride, vStride, outFeatureMapCount }, new BoolVector() { true }, new BoolVector() { false });
        }
        /// <summary>
        /// 全连接层
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="outputDim">输出维度</param>
        /// <param name="device">设备</param>
        /// <param name="scale">缩放</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>计算图</returns>
        protected static Function ELMFullyConnectedLinearLayer(Variable input, int outputDim, DeviceDescriptor device, float scale = 1.0f, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int inputDim = 1;
            for (int i = 0; i < input.Shape.Rank; i++)
                inputDim *= input.Shape[i];
            return Times(ELMParameters4Dense(inputDim, outputDim, device, scale, Seletction, type), Reshape(input, new int[] { inputDim }));
        }
        /// <summary>
        /// 全连接层带封装
        /// </summary>
        /// /// <param name="input">输入</param>
        /// <param name="outputDim">输出维度</param>
        /// <param name="device">设备</param>
        /// <param name="activation">激活函数选择</param>
        /// <param name="scale">缩放</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>计算图</returns>
        protected static Function ELMDense(Variable input, int outputDim, DeviceDescriptor device, Activation activation = None_Func, float scale = 1.0f, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int newDim = 1;
            foreach (var node in input.Shape.Dimensions)
                newDim *= node;
            var fullyConnected = ELMFullyConnectedLinearLayer(Reshape(input, new int[] { newDim }), outputDim, device, 1.0f, Seletction, type);
            switch (activation)
            {
                default:
                case None_Func:
                    return fullyConnected;
                case ReLU_Func:
                    return ReLU(fullyConnected);
                case Sigmoid_Func:
                    return Sigmoid(fullyConnected);
                case Tanh_Func:
                    return Tanh(fullyConnected);
            }
        }
        /// <summary>
        /// 残差层
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="InternalMapCounts">内部特征图数量</param>
        /// <param name="kernelWidth">核宽度</param>
        /// <param name="kernelHeight">核高度</param>
        /// <param name="device">设备</param>
        /// <param name="scale">缩放</param>
        /// <param name="spatial">是否空间卷积</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>计算图</returns>
        protected static Function ELMResNetNode(Variable input, int InternalMapCounts, int kernelWidth, int kernelHeight, DeviceDescriptor device, float scale = 1.0f, bool spatial = true, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int map_counts = input.Shape[input.Shape.Rank - 1];
            var c1 = ReLU(ELMConvLayer(input, InternalMapCounts, kernelWidth, kernelHeight, 1, 1, device, scale, spatial, Seletction, type));
            var c2 = ELMConvLayer(c1, map_counts, kernelWidth, kernelHeight, 1, 1, device, scale, spatial, Seletction, type);
            return ReLU(Plus(c2, input));
        }
        /// <summary>
        /// 残差特征图扩充层
        /// </summary>
        /// <param name="input">输入</param>
        /// <param name="InternalMapCounts">内部特征图数量</param>
        /// <param name="kernelWidth">核宽度</param>
        /// <param name="kernelHeight">核高度</param>
        /// <param name="device">设备</param>
        /// <param name="scale">缩放</param>
        /// <param name="spatial">是否空间卷积</param>
        /// <param name="Seletction">参数选择</param>
        /// <param name="type">使用的随机类型</param>
        /// <returns>计算图</returns>
        protected static Function ELMResNetNodeInc(Variable input, int InternalMapCounts, int OutputMapCounts, int kernelWidth, int kernelHeight, DeviceDescriptor device, float scale = 1.0f, bool spatial = true, InitiateSeletction Seletction = None_Sel, RandomType type = use_normal_distribution)
        {
            int map_counts = input.Shape[input.Shape.Rank - 1];
            var c1 = ReLU(ELMConvLayer(input, InternalMapCounts, kernelWidth, kernelHeight, 1, 1, device, scale, spatial, Seletction, type));
            var c2 = ELMConvLayer(c1, map_counts, kernelWidth, kernelHeight, 1, 1, device, scale, spatial, Seletction, type);
            var p = ReLU(Plus(c2, input));
            return ReLU(ELMConvLayer(p, OutputMapCounts, 1, 1, 1, 1, device, scale, spatial, Seletction, type));
        }
    };
    /// <summary>
    /// 事先做好的例子
    /// </summary>
    class MyPrefabsNN : NNBasicModel
    {
        #region 神经网络
        /*全连接测试网络*/
        protected static Function MLNCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            var dense_1 = Dense(input, BoardSize * BoardSize * 2, device, ReLU_Func);
            var dense_2 = Dense(dense_1, 2048, device, ReLU_Func);
            /*全连接决策层，含价值、策略子网络*/
            var fc_1 = Dense(dense_2, 1024, device, ReLU_Func);
            var fc2_1 = ReLU(Dense(fc_1, BoardSize * BoardSize, device, None_Func), PolicyNM);
            var fc2_2 = Dense(fc_1, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*MINIST改的*/
        protected static Function CNNCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            int kernelWidth = 3, kernelHeight = 3;
            int hStride = 1, vStride = 1;
            double convWScale = 7.07;
            double convBValue = 0;
            double scValue = 1;
            int bnTimeConst = 4096;
            var inputConvOutmaps = input.Shape[input.Shape.Rank - 1] * kernelWidth * kernelHeight;
            var conv_1 = ConvBatchNormalizationLayerNoPadding(input, inputConvOutmaps, kernelWidth, kernelHeight, hStride, vStride, convWScale, convBValue, scValue, bnTimeConst, true, device);
            var Conv1Outmaps = conv_1.Output.Shape[input.Shape.Rank - 1] * kernelWidth * kernelHeight;
            var conv_2 = ConvBatchNormalizationLayerNoPadding(conv_1, Conv1Outmaps, kernelWidth, kernelHeight, hStride, vStride, convWScale, convBValue, scValue, bnTimeConst, true, device);
            var Conv2Outmaps = Max((int)(conv_2.Output.Shape[input.Shape.Rank - 1] * Pow(0.618, 2)), 4);
            var conv_3 = ConvBatchNormalizationLayerNoPadding(conv_2, Conv2Outmaps, 1, 1, hStride, vStride, convWScale, convBValue, scValue, bnTimeConst, true, device);
            var Conv3Outmaps = (int)(conv_3.Output.Shape[input.Shape.Rank - 1] * kernelWidth * kernelHeight * Pow(0.618, 3));
            var conv_4 = ConvBatchNormalizationLayerNoPadding(conv_3, Conv3Outmaps, kernelWidth, kernelHeight, hStride, vStride, convWScale, convBValue, scValue, bnTimeConst, true, device);
            var Conv4Outmaps = Max((int)(conv_4.Output.Shape[input.Shape.Rank - 1] * Pow(0.618, 4)), 4);
            var conv_5 = ConvBatchNormalizationLayerNoPadding(conv_4, Conv4Outmaps, 1, 1, hStride, vStride, convWScale, convBValue, scValue, bnTimeConst, true, device);
            /*全连接决策层，含价值、策略子网络*/
            var NodesCount = 1;
            for (int i = 0; i < conv_4.Output.Shape.Rank; i++)
                NodesCount *= conv_4.Output.Shape[i];
            NodesCount = Max((int)(NodesCount * Pow(0.618, 5)), 1000);
            var fc_1 = Dense(conv_3, NodesCount, device, ReLU_Func);
            var fc2_1 = Dense(fc_1, BoardSize * BoardSize, device, ReLU_Func, PolicyNM);
            var fc2_2 = Dense(fc_1, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*这个ResNet是用图像分类改的*/
        protected static Function ResNetCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            double convWScale = 7.07;
            double convBValue = 0;
            double scValue = 1;
            int bnTimeConst = 4096;
            int kernelWidth = 3;
            int kernelHeight = 3;
            double conv1WScale = 0.26;
            int cMap1 = 16;
            /*输入卷积层*/
            var conv1 = ConvBatchNormalizationReLULayer(input, cMap1, kernelWidth, kernelHeight, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            /*一级残差层，16特征图，3ResBlock*/
            var rn1_1 = ResNetNode(conv1, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn1_2 = ResNetNode(rn1_1, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var rn1_3 = ResNetNode(rn1_2, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            int cMap2 = 32;
            /*二级残差层，32特征图，3ResBlock*/
            var rn2_1 = ResNetNodeInc(rn1_3, cMap1, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var rn2_2 = ResNetNode(rn2_1, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn2_3 = ResNetNode(rn2_2, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            int cMap3 = 64;
            /*三级残差层，64特征图，3ResBlock*/
            var rn3_1 = ResNetNodeInc(rn2_3, cMap2, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var rn3_2 = ResNetNode(rn3_1, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn3_3 = ResNetNode(rn3_2, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            /*全连接决策层，含价值、策略子网络*/
            var fc_1 = Dense(rn3_3, 1024, device, ReLU_Func);
            var fc2_1 = ReLU(Dense(fc_1, BoardSize * BoardSize, device, None_Func), PolicyNM);
            var fc2_2 = Dense(fc_1, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*UNet基于图像分割思想，这里稍作改动*/
        protected static Function UNetCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            double convBValue = 0;
            double scValue = 1;
            int bnTimeConst = 4096;
            int kernelWidth = 3;
            int kernelHeight = 3;
            double conv1WScale = 0.26;
            int Zoom_size_1 = 10;
            int Zoom_size_2 = Zoom_size_1 * 2;
            int Zoom_size_3 = Zoom_size_2 * 2;
            int Zoom_size_4 = Zoom_size_3 * 2;
            /*卷积层*/
            var proj1 = ConvBatchNormalizationLayerNoPadding(input, Zoom_size_1, 1, 1, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var conv1 = ReLU(ConvBatchNormalizationLayerNoPadding(input, Zoom_size_1, kernelWidth, kernelHeight, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var proj2 = ConvBatchNormalizationLayerNoPadding(conv1, Zoom_size_1, 1, 1, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var conv2 = ReLU(ConvBatchNormalizationLayerNoPadding(conv1, Zoom_size_2, kernelWidth, kernelHeight, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var proj3 = ConvBatchNormalizationLayerNoPadding(conv2, Zoom_size_2, 1, 1, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var conv3 = ReLU(ConvBatchNormalizationLayerNoPadding(conv2, Zoom_size_2, kernelWidth, kernelHeight, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var proj4 = ConvBatchNormalizationLayerNoPadding(conv3, Zoom_size_3, 1, 1, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var conv4 = ReLU(ConvBatchNormalizationLayerNoPadding(conv3, Zoom_size_4, kernelWidth, kernelHeight, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var proj5 = ConvBatchNormalizationLayerNoPadding(conv4, Zoom_size_4, 1, 1, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
            var trans_conv4 = ReLU(TransConvBatchNormalizationLayerNoPadding(proj5, Zoom_size_3, 3, 3, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var fusion_3 = Plus(proj4, trans_conv4);
            var trans_conv3 = ReLU(TransConvBatchNormalizationLayerNoPadding(fusion_3, Zoom_size_2, 3, 3, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var fusion_2 = Plus(proj3, trans_conv3);
            var trans_conv2 = ReLU(TransConvBatchNormalizationLayerNoPadding(fusion_2, Zoom_size_1, 3, 3, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var fusion_1 = Plus(proj2, trans_conv2);
            var trans_conv1 = ReLU(TransConvBatchNormalizationLayerNoPadding(fusion_1, Zoom_size_1, 3, 3, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device));
            var fusion_0 = Plus(proj1, trans_conv1);
            /*决策层*/
            var policy_0 = Dense(fusion_0, 2000, device, ReLU_Func);
            var policy_1 = ReLU(Dense(policy_0, BoardSize * BoardSize, device, None_Func), PolicyNM);
            var value_0 = Dense(fusion_0, 100, device, Tanh_Func);
            var value_1 = Dense(value_0, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { policy_1, value_1 });
        }
        #endregion
        #region 超限学习机
        /*全连接测试网络*/
        protected static Function ELM_MLNCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            float Scale = 2;
            InitiateSeletction Seletction = Orth_Sel | RC_Sel | Scale_Sel | Normalized_Sel;
            RandomType type = use_uniform_real_distribution;
            var dense = ELMDense(input, 2048, device, ReLU_Func, Scale, Seletction, type);
            var dense_n = ELMDense(dense, 2048, device, ReLU_Func, Scale, Seletction, type);
            /*全连接决策层，含价值、策略子网络*/
            var fc_1 = Dense(dense_n, 2048, device, ReLU_Func);
            var fc2_1 = ReLU(Dense(fc_1, BoardSize * BoardSize, device, None_Func), PolicyNM);
            var fc2_2 = Dense(fc_1, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*MINIST改的*/
        protected static Function ELM_CNNCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            float Scale = 2;
            InitiateSeletction Seletction = Orth_Sel | Scale_Sel | Normalized_Sel;
            RandomType type = use_uniform_real_distribution;
            int kernelWidth = 3, kernelHeight = 3, hStride = 1, vStride = 1;
            var conv_1 = ReLU(ELMConvLayerNoPadding(input, 16, kernelWidth, kernelHeight, hStride, vStride, device, Scale, true, Seletction, type));
            var conv_2 = ReLU(ELMConvLayerNoPadding(conv_1, 32, kernelWidth, kernelHeight, hStride, vStride, device, Scale, true, Seletction, type));
            var conv_3 = ReLU(ELMConvLayerNoPadding(conv_2, 64, kernelWidth, kernelHeight, hStride, vStride, device, Scale, true, Seletction, type));
            /*全连接决策层，含价值、策略子网络*/
            var fc_1 = ELMDense(conv_3, 2048, device, ReLU_Func, Scale, Seletction, type);
            var fc2_1 = ReLU(Dense(fc_1, BoardSize * BoardSize, device, None_Func), PolicyNM);
            var fc2_2 = Dense(fc_1, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*这个ResNet是用图像分类改的*/
        protected static Function ELM_ResNetCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            float Scale = 2;
            InitiateSeletction Seletction = Orth_Sel | Scale_Sel | Normalized_Sel;
            RandomType type = use_uniform_real_distribution;
            int kernelWidth = 3;
            int kernelHeight = 3;
            int cMap1 = 16;
            /*输入卷积层*/
            var conv1 = ReLU(ELMConvLayer(input, 16, kernelWidth, kernelHeight, 1, 1, device, Scale, true, Seletction, type));
            /*一级残差层，16特征图，3ResBlock*/
            var rn1_1 = ELMResNetNode(conv1, cMap1, kernelWidth, kernelHeight, device, Scale, true, Seletction, type);
            var rn1_2 = ELMResNetNode(rn1_1, cMap1, kernelWidth, kernelHeight, device, Scale, false, Seletction, type);
            var rn1_3 = ELMResNetNode(rn1_2, cMap1, kernelWidth, kernelHeight, device, Scale, true, Seletction, type);
            int cMap2 = 32;
            /*二级残差层，32特征图，3ResBlock*/
            var rn2_1 = ELMResNetNodeInc(rn1_3, cMap1, cMap2, kernelWidth, kernelHeight, device, Scale, true, Seletction, type);
            var rn2_2 = ELMResNetNode(rn2_1, cMap2, kernelWidth, kernelHeight, device, Scale, false, Seletction, type);
            var rn2_3 = ELMResNetNode(rn2_2, cMap2, kernelWidth, kernelHeight, device, Scale, true, Seletction, type);
            int cMap3 = 64;
            /*三级残差层，64特征图，3ResBlock*/
            var rn3_1 = ELMResNetNodeInc(rn2_3, cMap2, cMap3, kernelWidth, kernelHeight, device, Scale, true, Seletction, type);
            var rn3_2 = ELMResNetNode(rn3_1, cMap3, kernelWidth, kernelHeight, device, Scale, false, Seletction, type);
            var rn3_3 = ELMResNetNode(rn3_2, cMap3, kernelWidth, kernelHeight, device, Scale, true, Seletction, type);
            /*全连接决策层，含价值、策略子网络*/
            var fc_1 = Dense(rn3_3, 1024, device, ReLU_Func);
            var fc2_1 = ReLU(Dense(fc_1, BoardSize * BoardSize, device, None_Func), PolicyNM);
            var fc2_2 = Dense(fc_1, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*UNet基于图像分割思想，这里稍作改动*/
        protected static Function ELM_UNetCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            float Scale = 2;
            InitiateSeletction Seletction = Orth_Sel | Scale_Sel | Normalized_Sel;
            RandomType type = use_uniform_real_distribution;
            int kernelWidth = 3;
            int kernelHeight = 3;
            int Zoom_size_1 = 10;
            int Zoom_size_2 = Zoom_size_1 * 2;
            int Zoom_size_3 = Zoom_size_2 * 2;
            int Zoom_size_4 = Zoom_size_3 * 2;
            /*卷积层*/
            var proj1 = ELMConvLayerNoPadding(input, Zoom_size_1, 1, 1, 1, 1, device, Scale, true, Seletction, type);
            var conv1 = ReLU(ELMConvLayerNoPadding(input, Zoom_size_1, kernelWidth, kernelHeight, 1, 1, device, Scale, true, Seletction, type));
            var proj2 = ELMConvLayerNoPadding(conv1, Zoom_size_1, 1, 1, 1, 1, device, Scale, true, Seletction, type);
            var conv2 = ReLU(ELMConvLayerNoPadding(conv1, Zoom_size_2, kernelWidth, kernelHeight, 1, 1, device, Scale, true, Seletction, type));
            var proj3 = ELMConvLayerNoPadding(conv2, Zoom_size_2, 1, 1, 1, 1, device, Scale, true, Seletction, type);
            var conv3 = ReLU(ELMConvLayerNoPadding(conv2, Zoom_size_2, kernelWidth, kernelHeight, 1, 1, device, Scale, true, Seletction, type));
            var proj4 = ELMConvLayerNoPadding(conv3, Zoom_size_3, 1, 1, 1, 1, device, Scale, true, Seletction, type);
            var conv4 = ReLU(ELMConvLayerNoPadding(conv3, Zoom_size_4, kernelWidth, kernelHeight, 1, 1, device, Scale, true, Seletction, type));
            var proj5 = ELMConvLayerNoPadding(conv4, Zoom_size_4, 1, 1, 1, 1, device, Scale, true, Seletction, type);
            var trans_conv4 = ReLU(TransELMConvLayerNoPadding(proj5, Zoom_size_3, 1, 1, 1, 1, device, Scale, true, Seletction, type));
            var fusion_3 = Plus(proj4, trans_conv4);
            var trans_conv3 = ReLU(TransELMConvLayerNoPadding(fusion_3, Zoom_size_2, 1, 1, 1, 1, device, Scale, true, Seletction, type));
            var fusion_2 = Plus(proj3, trans_conv3);
            var trans_conv2 = ReLU(TransELMConvLayerNoPadding(fusion_2, Zoom_size_1, 1, 1, 1, 1, device, Scale, true, Seletction, type));
            var fusion_1 = Plus(proj2, trans_conv2);
            var trans_conv1 = ReLU(TransELMConvLayerNoPadding(conv4, Zoom_size_1, 1, 1, 1, 1, device, Scale, true, Seletction, type));
            var fusion_0 = Plus(proj1, trans_conv1);
            /*决策层*/
            var policy_0 = Dense(fusion_0, 2000, device, ReLU_Func);
            var policy_1 = ReLU(Dense(policy_0, BoardSize * BoardSize, device, None_Func), PolicyNM);
            var value_0 = Dense(fusion_0, 100, device, Tanh_Func);
            var value_1 = Dense(value_0, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { policy_1, value_1 });
        }
        #endregion
    };
    /// <summary>
    /// 深度强化学习的常用功能部件
    /// </summary>
    class DQN_DNN_Class : MyPrefabsNN, IDQN_DNN_Class
    {
        ReadOnlyDictionary<NN_Model, int> BatchCounts = new ReadOnlyDictionary<NN_Model, int>(new Dictionary<NN_Model, int>()
        {
            {NN_Model.Mutl_Layer_NN,16 },{NN_Model.Conv_NN,15 },{NN_Model.ResNet_NN,10 },{NN_Model.UNet_NN,12 },
            {NN_Model.Mutl_Layer_ELM,16 }
        });
        /*内部变量*/
        private NN_Model nn_model;
        private Function EvalModel;
        private DeviceDescriptor device_info;
        private readonly Queue<KeyValuePair<NodeData, UCTNode>> EvalQueue = new Queue<KeyValuePair<NodeData, UCTNode>>();
        private string DefaultFileName;
        private const string FeaturesStr = "Features";
        private const string PolicyStr = "Policy";
        private const string ValueStr = "Value";
        private const string LabelsPieStr = "LabelsPie";
        private const string LabelsZStr = "LabelsZ";
        private const int BoardSize = PURE_BOARD_SIZE;
        private Thread threadEval;
        /*队列限制*/
        private const int queue_length = 3000;
        private void ReLUAvg(float[] values)
        {
            float sum = 0;
            for (var i = 0; i < values.Length; i++)
            {
                values[i] = Max(values[i], 0);
                sum += values[i];
            }
            sum = (sum == 0 ? 1 : sum);
            for (var i = 0; i < values.Length; i++)
                values[i] = values[i] / sum;
        }
        private void ReLUAvg(List<float> values)
        {
            float sum = 0;
            for (var i = 0; i < values.Count; i++)
            {
                values[i] = Max(values[i], 0);
                sum += values[i];
            }
            sum = (sum == 0 ? 1 : sum);
            for (var i = 0; i < values.Count; i++)
                values[i] = values[i] / sum;
        }
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
        /*损失函数*/
        private Function MyLossFunction(Variable LabelsPie, Variable Policy_var, Variable LabelsZ, Variable Value_var)
        {
            var Value_Loss = Square(Minus(Value_var, LabelsZ), "Value_Loss");
            var ReshapeP = Reshape(Plus(Policy_var, new Constant(new int[] { BoardSize * BoardSize }, DataType.Float, 2e-8f, device_info)), new int[] { BoardSize * BoardSize, 1 });
            var ReshapePie = Reshape(LabelsPie, new int[] { 1, BoardSize * BoardSize });
            var CrossE_P = Reshape(Times(ReshapePie, Log(ReshapeP)), new int[] { 1 }, "Policy_Loss");
            return Minus(Value_Loss, CrossE_P, "TotalLoss");
        }
        /*错误率函数*/
        private Function MyErrorFunction(Variable LabelsPie, Variable Policy_var, Variable LabelsZ, Variable Value_var)
        {
            var Value_Error = Square(Minus(Value_var, LabelsZ), "Value_Error");
            var Policy_Error = Reshape(Times(Reshape(Square(Minus(Policy_var, LabelsPie)), new int[] { 1, BoardSize * BoardSize }), new Constant(new int[] { BoardSize * BoardSize, 1 }, DataType.Float, 1, device_info)), new int[] { 1 }, "Policy_Error");
            return Plus(Policy_Error, Value_Error, "TotalError");
        }
        public volatile bool EnableEvalThread = false;
        public DQN_DNN_Class()
        {
            nn_model = Conv_NN;
            GetFileName();
            device_info = DeviceDescriptor.UseDefaultDevice();
        }
        /*检查是否有可用的GPU*/
        public bool HaveGPU()
        {
            return DeviceDescriptor.UseDefaultDevice().Type != DeviceKind.CPU;
        }
        /*创建神经网络*/
        public bool CreateModel(DeviceDescriptor device, NN_Model _nn_model = Conv_NN)
        {
            device_info = device;
            nn_model = _nn_model;
            var Inputs = InputVariable(new int[] { BoardSize, BoardSize, 2 }, DataType.Float, FeaturesStr);
            GetFileName();
            switch (nn_model)
            {
                case Mutl_Layer_NN:
                    EvalModel = MLNCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                case Conv_NN:
                    EvalModel = CNNCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                case ResNet_NN:
                    EvalModel = ResNetCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                case UNet_NN:
                    EvalModel = UNetCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                case Mutl_Layer_ELM:
                    EvalModel = ELM_MLNCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                case Conv_ELM:
                    EvalModel = ELM_CNNCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                case ResNet_ELM:
                    EvalModel = ELM_ResNetCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                case UNet_ELM:
                    EvalModel = ELM_UNetCreator(Inputs, device_info, BoardSize, PolicyStr, ValueStr);
                    return true;
                default:
                    return false;
            }
        }
        /*加载神经网络*/
        public bool Load()
        {
            GetFileName();
            if (!CheckFile(DefaultFileName)) return false;
            EvalModel = Function.Load(DefaultFileName, device_info);
            if (EvalModel == null)
                return false;
            return true;
        }
        /*保存神经网络*/
        public void Save()
        {
            GetFileName();
            if (EvalModel != null)
            {
                EvalModel.Save(DefaultFileName);
            }
        }
        delegate Dictionary<Variable, Value> FuncTrainData(List<KeyValuePair<NodeData, UCTNode>> data);
        /*训练网络*/
        public void Train(List<KeyValuePair<NodeData, UCTNode>> trainDatas, double learning_rate = 0.01f, int epouch = 100, int minibatchsize = 100)
        {
            var blocksize = 1 << BatchCounts[nn_model];
            var remainblock_size = trainDatas.Count % blocksize;
            var blockcount = trainDatas.Count / blocksize;
            /*准备训练的学习率和最小步长*/
            var learningRatePerSample = new TrainingParameterScheduleDouble(learning_rate, (uint)minibatchsize);
            var MomentumSchedulePerSample = new TrainingParameterScheduleDouble(new VectorPairSizeTDouble() { new PairSizeTDouble(1, 0.9), new PairSizeTDouble(2, 0.999), new PairSizeTDouble(3, 1e-8) }, (uint)epouch, (uint)minibatchsize);
            var LabelsZ = InputVariable(new int[] { 1 }, DataType.Float, LabelsZStr);
            var LabelsPie = InputVariable(new int[] { BoardSize * BoardSize }, DataType.Float, LabelsPieStr);
            var Features_var = GetInputs(FeaturesStr);
            var Policy_var = GetOutputs(PolicyStr);
            var Value_var = GetOutputs(ValueStr);
            /*准备损失函数和错误率函数*/
            Function lossFunction = MyLossFunction(LabelsPie, Policy_var, LabelsZ, Value_var);
            Function prediction = MyErrorFunction(LabelsPie, Policy_var, LabelsZ, Value_var);
            ParameterVector Parameters = new ParameterVector(EvalModel.Parameters().ToList());
            /*创建训练方法*/
            var trainer = Trainer.CreateTrainer(EvalModel, lossFunction, prediction, new List<Learner>{
                  //Learner.SGDLearner(EvalModel.Parameters(), learningRatePerSample)
                  AdamLearner(Parameters,learningRatePerSample,MomentumSchedulePerSample)
            });
            Dictionary<Variable, Value> TranstrainDatasToVector(List<KeyValuePair<NodeData, UCTNode>> _trainDatas)
            {
                var banch_size = _trainDatas.Count;
                List<float>[] Features = new List<float>[banch_size];
                List<float>[] LabelPies = new List<float>[banch_size];
                List<float>[] LabelZs = new List<float>[banch_size];
                for (var i = 0; i < banch_size; i++)
                {
                    Features[i] = _trainDatas[i].Key.GetBoardInfos();
                    _trainDatas[i].Value.GetExp(out LabelPies[i], out LabelZs[i]);
                }
                Dictionary<Variable, Value> datas = new Dictionary<Variable, Value>() {
                    { Features_var,Value .CreateBatchOfSequences<float>(new int[]{ BoardSize,BoardSize,2 }, Features, device_info, false)},
                    { LabelsPie, Value.CreateBatchOfSequences<float>(new int[]{ BoardSize* BoardSize }, LabelPies, device_info, false)},
                    { LabelsZ, Value.CreateBatchOfSequences<float>(new int[]{ 1 }, LabelZs, device_info, false)}
                };
                return datas;
            }
            List<KeyValuePair<NodeData, UCTNode>> Buffer = new List<KeyValuePair<NodeData, UCTNode>>();
            int begin = 0;
            int end = trainDatas.Count;
            /*数据切块并训练*/
            for (var blockpos = 1; blockpos <= blockcount; blockpos++)
            {
                trainer.TrainMinibatch(TranstrainDatasToVector(trainDatas.GetRange(begin, blocksize)), false, device_info);
                begin += blocksize;
            }
            if (remainblock_size > 0)
            {
                trainer.TrainMinibatch(TranstrainDatasToVector(trainDatas.GetRange(begin, end - begin)), false, device_info);
            }
            //try
            //{
            //    for (var blockpos = 1; blockpos <= blockcount; blockpos++)
            //    {
            //        trainer.TrainMinibatch(TranstrainDatasToVector(trainDatas.GetRange(begin, blocksize)), false, device_info);
            //        begin += blocksize;
            //    }
            //    if (remainblock_size > 0)
            //    {
            //        trainer.TrainMinibatch(TranstrainDatasToVector(trainDatas.GetRange(begin, end - begin)), false, device_info);
            //    }
            //}
            //catch (Exception e)
            //{
            //    Console.Error.WriteLine(e.Message);
            //}
        }
        /*启动评估函数（线程用的）*/
        public void Eval()
        {
            var Features_var = GetInputs(FeaturesStr);
            var Policy_var = GetOutputs(PolicyStr);
            var Value_var = GetOutputs(ValueStr);
            /*估值开始*/
            void EvaluateNode()
            {
                int index = 0;
                int batchSize = 0;
                lock (EvalQueue) { batchSize = EvalQueue.Count; }
                if (batchSize == 0)
                    Thread.Sleep(10);
                if (batchSize == 0) return;
                List<List<float>> Features = new List<List<float>>();
                List<List<float>> Policys = new List<List<float>>();
                List<List<float>> Values = new List<List<float>>();
                Queue<KeyValuePair<NodeData, UCTNode>> Queue4DataWriteBack = new Queue<KeyValuePair<NodeData, UCTNode>>();
                while (batchSize > 0)
                {
                    batchSize--;
                    if (EvalQueue.Count == 0) return;
                    var Node = EvalQueue.Dequeue();
                    if (Node.Value == null || Node.Value == null)
                        continue;
                    Node.Value.Evaled = true;
                    lock (EvalQueue)
                        Features.Add(Node.Key.GetBoardInfos());
                    Policys.Add(new List<float>(new float[BoardSize * BoardSize]));
                    Values.Add(new List<float>(new float[1]));
                    Queue4DataWriteBack.Enqueue(Node);
                    index++;
                }
                if (Features.Count <= 0) return;
                Dictionary<Variable, Value> Input_Value_Maps = new Dictionary<Variable, Value>() {
                    { Features_var,  Value.CreateBatchOfSequences(new int[]{ BoardSize, BoardSize, 2 }, Features, device_info,true) }
                };
                Dictionary<Variable, Value> Out_Value_Maps = new Dictionary<Variable, Value>() {
                    {Policy_var, Value.CreateBatchOfSequences<float>(new int[]{ BoardSize * BoardSize }, Policys, device_info,false) },
                    {Value_var, Value.CreateBatchOfSequences<float>(new int[]{ 1 },Values, device_info,false) }
                };
                EvalModel.Evaluate(Input_Value_Maps, Out_Value_Maps, device_info);
                var policys = Out_Value_Maps[Policy_var].GetDenseData<float>(Policy_var);
                var values = Out_Value_Maps[Value_var].GetDenseData<float>(Value_var);
                var writeBackCount = 0;
                while (Queue4DataWriteBack.Count != 0)
                {
                    var node = Queue4DataWriteBack.Dequeue();
                    lock (node.Value)
                    {
                        node.Value.V = values[writeBackCount][0];
                        for (var i = 0; i < BoardSize * BoardSize; i++)
                            node.Value.P[i] = policys[writeBackCount][i];
                        ReLUAvg(node.Value.P);
                    }
                    writeBackCount++;
                }
            }
            bool EnableToQuit = false;
            /*开始评估线程（循环，当EnableEvalThread值为false时线程停止）*/
            try
            {
                EvalQueue.Clear();
                while (EnableEvalThread || !EnableToQuit)
                {
                    /*再检查队列是否为空，为空则跳回到开始*/
                    if (EvalQueue.Count == 0)
                        Thread.Sleep(100);
                    if (EvalQueue.Count == 0)
                        EnableToQuit = true;
                    else
                        EvaluateNode();
                }
            }
            catch (Exception e)
            {

                Console.Error.WriteLine(e.StackTrace);
                Console.Error.WriteLine(e.Message);
                Console.Error.WriteLine(e.Source);
            }

        }
        /*将要评估的点压入队列*/
        public void EvalNode(KeyValuePair<NodeData, UCTNode> Node)
        {
            lock (EvalQueue)
            {
                if (queue_length < EvalQueue.Count)
                    Thread.Sleep(1);
                if (Node.Value == null || Node.Value == null) return;
                EvalQueue.Enqueue(Node);
            }
        }
        /*NN测试*/
        void TestNN()
        {
        }
        /*得到NN的队列参数*/
        public int GetQueueLength()
        {
#if DEBUG
            Console.Error.WriteLine("Queue Length:" + EvalQueue.Count);
#endif
            return EvalQueue.Count;
        }
        /*清空估值队列*/
        public void ClearQueue()
        {
            EvalQueue.Clear();
        }
        /*得到文件名*/
        private string GetFileName()
        {
            DefaultFileName = "";
            DefaultFileName += Path;
            DefaultFileName += RL_NM;
            DefaultFileName += Model_expand[(int)nn_model];
            return DefaultFileName;
        }
        /*设置所使用的模型*/
        public string SetupModel(NN_Model _nn_model = Conv_NN)
        {
            nn_model = _nn_model;
            return GetFileName();
        }
        /*得到设置的模型模式*/
        public NN_Model getModelType()
        {
            return nn_model;
        }
        public void EvalDisabled()
        {
            EnableEvalThread = false;
        }
    };

}
