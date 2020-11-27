using CNTK;

namespace MyCNTKPrefebs
{
    using static CNTKLib;
    using static Activation;

    /// <summary>
    /// 基础神经网络元素
    /// </summary>
    public static class BasicModel_NN
    {
        /// <summary>
        /// 卷积批量正则化自动填充边界
        /// </summary>
        public static Function ConvBatchNormalizationLayer(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount }, DataType.Float, GlorotUniformInitializer(wScale, -1, 1), device);
            var convFunction = Convolution(convParams, input, new int[] { hStride, vStride, numInputChannels });
            var biasParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)bValue, device, "");
            var scaleParams = new Parameter(new int[] { NDShape.InferredDimension }, (float)scValue, device, "");
            var runningMean = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningInvStd = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device);
            var runningCount = Constant.Scalar(0.0f, device);
            return BatchNormalization(convFunction, scaleParams, biasParams, runningMean, runningInvStd, runningCount, spatial, bnTimeConst, 0.0, 1e-5 /* epsilon */);
        }
        /// <summary>
        /// 卷积层
        /// </summary>
        public static Function ConvLayer(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, DeviceDescriptor device)
        {
            int numInputChannels = input.Shape[input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { kernelWidth, kernelHeight, numInputChannels, outFeatureMapCount }, DataType.Float, GlorotNormalInitializer(1), device);
            return Convolution(convParams, input, new int[] { hStride, vStride, numInputChannels });
        }
        /// <summary>
        /// 卷积批量正则化自动填充边界带ReLU
        /// </summary>
        public static Function ConvBatchNormalizationReLULayer(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            return ReLU(ConvBatchNormalizationLayer(input, outFeatureMapCount, kernelWidth, kernelHeight, hStride, vStride, wScale, bValue, scValue, bnTimeConst, spatial, device));
        }
        /// <summary>
        /// 卷积批量正则化不自动填充边界
        /// </summary>
        public static Function ConvBatchNormalizationLayerNoPadding(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
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
        /// 反卷积批量正则化不自动填充边界
        /// </summary>
        public static Function TransConvBatchNormalizationLayerNoPadding(Variable input, int outFeatureMapCount, int kernelWidth, int kernelHeight, int hStride, int vStride, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
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
        public static Function FullyConnectedLinearLayer(Variable input, int outputDim, DeviceDescriptor device)
        {
            int inputDim = 1;
            foreach (var node in input.Shape.Dimensions)
                inputDim *= node;
            var timesParam = new Parameter(new int[] { outputDim, inputDim }, DataType.Float, GlorotUniformInitializer(DefaultParamInitScale, SentinelValueForInferParamInitRank, SentinelValueForInferParamInitRank, 1), device, "timesParam");
            var timesFunction = Times(timesParam, input);
            return Plus(new Parameter(new int[] { outputDim }, DataType.Float, 0.0f, device), timesFunction);
        }
        /// <summary>
        /// 全连接层
        /// </summary>
        public static Function FullyConnectedLinearLayer(Variable input, int outputDim, DeviceDescriptor device, string outputName)
        {
            int inputDim = 1;
            foreach (var node in input.Shape.Dimensions)
                inputDim *= node;
            var timesParam = new Parameter(new int[] { outputDim, inputDim }, DataType.Float, GlorotUniformInitializer(DefaultParamInitScale, SentinelValueForInferParamInitRank, SentinelValueForInferParamInitRank, 1), device, "timesParam");
            var timesFunction = Times(timesParam, input);
            return Plus(new Parameter(new int[] { outputDim }, DataType.Float, 0.0f, device), timesFunction, outputName);
        }
        /// <summary>
        /// 全连接层带封装
        /// </summary>
        public static Function Dense(Variable input, int outputDim, DeviceDescriptor device, Activation activation = None_Func, string outputName = "")
        {
            int newDim = 1;
            foreach (var node in input.Shape.Dimensions)
                newDim *= node;
            var D_Input = Dropout(input, 0.5);
            bool UsingDropout = outputDim > 1000;
            var K_Input = UsingDropout ? D_Input : input.ToFunction();
            bool defaultname = outputName == null || outputName == "";
            switch (activation)
            {
                default:
                case None_Func:
                    if (defaultname)
                        return FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device);
                    else
                        return FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device, outputName);
                case ReLU_Func:
                    if (defaultname)
                        return ReLU(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device));
                    else
                        return ReLU(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device), outputName);
                case Sigmoid_Func:
                    if (defaultname)
                        return Sigmoid(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device));
                    else
                        return Sigmoid(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device), outputName);
                case Tanh_Func:
                    if (defaultname)
                        return Tanh(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device));
                    else
                        return Tanh(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device), outputName);
            }
        }
        /// <summary>
        /// 全连接层带封装
        /// </summary>
        public static Function Dense(Variable input, int outputDim, DeviceDescriptor device, Activation activation = None_Func)
        {
            int newDim = 1;
            foreach (var node in input.Shape.Dimensions)
                newDim *= node;
            var D_Input = Dropout(input, 0.5);
            bool UsingDropout = outputDim > 1000;
            var K_Input = UsingDropout ? D_Input : input.ToFunction();
            switch (activation)
            {
                default:
                case None_Func:
                    return FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device);
                case ReLU_Func:
                    return ReLU(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device));
                case Sigmoid_Func:
                    return Sigmoid(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device));
                case Tanh_Func:
                    return Tanh(FullyConnectedLinearLayer(Reshape(K_Input, new int[] { newDim }), outputDim, device));
            }
        }
        /// <summary>
        /// 残差层
        /// </summary>
        public static Function ResNetNode(Variable input, int InternalMapCounts, int kernelWidth, int kernelHeight, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int map_counts = input.Shape[input.Shape.Rank - 1];
            var c1 = ConvBatchNormalizationReLULayer(input, InternalMapCounts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var c2 = ConvBatchNormalizationLayer(c1, map_counts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var p = Plus(c2, input);
            return ReLU(p);
        }
        /// <summary>
        /// 残差扩充层
        /// </summary>
        public static Function ResNetNodeInc(Variable input, int InternalMapCounts, int outFeatureMapCount, int kernelWidth, int kernelHeight, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        {
            int map_counts = input.Shape[input.Shape.Rank - 1];
            var c1 = ConvBatchNormalizationReLULayer(input, InternalMapCounts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var c2 = ConvBatchNormalizationLayer(c1, map_counts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
            var p = Plus(c2, input);
            return ConvBatchNormalizationReLULayer(p, outFeatureMapCount, 1, 1, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
        }

    }
}
