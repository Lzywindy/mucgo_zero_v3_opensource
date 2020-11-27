using CNTK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyCNTKPrefebs
{
    using static CNTKLib;
    public static class CNNAC
    {
        public const string Policy_NM = "Policy";
        public const string Value_NM = "Value";
        public static Function U_Net(Variable Input, DeviceDescriptor device)
        {
            int basesize = 16;
            string str_conv = "ConvLayer";
            string str_tconv = "TransConvLayer";
            string str_BN_Layer = "BN-Layer";
            int block_count = 1;

            var shape = Math.Min(Input.Shape[0], Input.Shape[1]);
            bool dim9 = shape <= 9;
            bool dim11 = shape <= 11;
            bool dim13 = shape <= 13;
            bool dim15 = shape <= 15;
            bool dim17 = shape <= 17;
            //Conv_Block_1
            var conv_1_1 = ReLU(Conv2d(Input, basesize, 3, 1, 1, device, $"{str_conv}_{block_count}_1", false));
            var conv_1_2 = ReLU(Conv2d(conv_1_1, basesize, 3, 1, 4, device, $"{str_conv}_{block_count}_2", false));
            block_count++;
            //Conv_Block_2
            var conv_2_1 = ReLU(Conv2d(BN_Layer(conv_1_2, device, $"{str_BN_Layer}_{block_count - 1}"), basesize, 3, 1, 1, device, $"{str_conv}_{block_count}_1", false));
            var conv_2_2 = ReLU(Conv2d(conv_2_1, basesize * 2, 3, 1, 4, device, $"{str_conv}_{block_count}_2", dim9));
            var conv_2_3 = ReLU(BN_Layer(Conv2d(conv_2_2, basesize * 2, 3, 1, 4, device, $"{str_conv}_{block_count}_2", dim9 || dim11), device, $"{str_BN_Layer}_{block_count}"));
            block_count++;
            //Conv_Block_3
            var conv_3_1 = ReLU(Conv2d(conv_2_3, basesize * 2, 3, 1, 1, device, $"{str_conv}_{block_count}_1", dim9 || dim11 || dim13));
            var conv_3_2 = ReLU(Conv2d(conv_3_1, basesize * 4, 3, 1, 4, device, $"{str_conv}_{block_count}_2", dim9 || dim11 || dim13 || dim15));
            var conv_3_3 = ReLU(BN_Layer(Conv2d(conv_3_2, basesize * 4, 3, 1, 4, device, $"{str_conv}_{block_count}_2", dim9 || dim11 || dim13 || dim15 || dim17), device, $"{str_BN_Layer}_{block_count}"));
            block_count++;
            //Conv_Block_4
            var conv_4_1 = ReLU(Conv2d(conv_3_3, basesize * 8, 1, 1, 1, device, $"{str_conv}_{block_count}_1", false));
            var conv_4_2 = ReLU(Conv2d(conv_4_1, basesize * 8, 1, 1, 4, device, $"{str_conv}_{block_count}_2", false));
            var conv_4_3 = ReLU(BN_Layer(Conv2d(conv_4_2, basesize * 8, 1, 1, 4, device, $"{str_conv}_{block_count}_2", false), device, $"{str_BN_Layer}_{block_count}"));
            block_count++;
            //Conv_Block_1_Transpose
            var conv_t_5_1 = Copy_Crop(ReLU(TConv2d(conv_4_3, basesize * 4, 3, 1, device, $"{str_tconv}_{block_count}_1", dim9 || dim11 || dim13 || dim15 || dim17)), conv_3_2);
            var conv_t_5_2 = ReLU(TConv2d(conv_t_5_1, basesize * 4, 3, 1, device, $"{str_tconv}_{block_count}_2", dim9 || dim11 || dim13 || dim15));
            var conv_t_5_3 = ReLU(BN_Layer(TConv2d(conv_t_5_2, basesize * 4, 3, 1, device, $"{str_tconv}_{block_count}_3", dim9 || dim11 || dim13), device, $"{str_BN_Layer}_{block_count}"));
            block_count++;
            //Conv_Block_2_Transpose
            var conv_t_6_1 = Copy_Crop(ReLU(TConv2d(conv_t_5_3, basesize * 2, 3, 1, device, $"{str_tconv}_{block_count}_1", dim9 || dim11)), conv_2_2);
            var conv_t_6_2 = ReLU(TConv2d(conv_t_6_1, basesize * 2, 3, 1, device, $"{str_tconv}_{block_count}_2", dim9));
            var conv_t_6_3 = ReLU(BN_Layer(TConv2d(conv_t_6_2, basesize * 2, 3, 1, device, $"{str_tconv}_{block_count}_3", false), device, $"{str_BN_Layer}_{block_count}"));
            block_count++;
            //Conv_Block_3_Transpose
            var conv_t_7_1 = Copy_Crop(ReLU(TConv2d(conv_t_6_3, basesize, 3, 1, device, $"{str_tconv}_{block_count}_1", false)), conv_1_1);
            var conv_t_7_2 = ReLU(TConv2d(conv_t_7_1, basesize, 3, 1, device, $"{str_tconv}_{block_count}_2", false));
            var conv_t_7_3 = ReLU(BN_Layer(Conv2d(conv_t_7_2, basesize, 1, 1, 1, device, $"{str_conv}_{block_count}_3", false), device, $"{str_BN_Layer}_{block_count}"));
            block_count++;
            //Conv_Block_5
            var conv_out_1 = ReLU(Conv2d(conv_t_7_3, basesize, 3, 1, 1, device, $"{str_conv}_{block_count}_1", true));
            var conv_out_2 = ReLU(Conv2d(conv_out_1, basesize, 3, 1, 4, device, $"{str_conv}_{block_count}_2", true));
            //Output_Policy
            return Conv2d(conv_out_2, 1, 1, 1, 1, device, $"{Policy_NM}", true);
        }
        public static Function ResNeXt(Variable Input, DeviceDescriptor device)
        {
            int Maps = 128;
            var InputConv = Conv2d_Relu(Input, Maps, 3, 1, 1, device, "InputConvLayer", true);
            var Block_1 = ResNeXtNode(InputConv, device, Maps, $"Block_{1}");
            var Block_2 = ResNeXtNode(Block_1, device, Maps, $"Block_{2}");
            var Block_3 = ResNeXtNode(Block_2, device, Maps, $"Block_{3}");
            var Block_4 = ResNeXtNode(Block_3, device, Maps, $"Block_{4}");
            var Block_5 = ResNeXtNode(Block_4, device, Maps, $"Block_{5}");
            var Block_6 = ResNeXtNode(Block_5, device, Maps, $"Block_{6}");
            var Block_7 = ResNeXtNode(Block_6, device, Maps, $"Block_{7}");
            var Block_8 = ResNeXtNode(Block_7, device, Maps, $"Block_{8}");
            var PolicyOut = Conv2d(Block_8, 1, 1, 1, 1, device, $"{Policy_NM}", true);
            var ValueOut = Tanh(Pooling(Block_8, PoolingType.Average, Block_8.Output.Shape, Block_8.Output.Shape), Value_NM);
            //Out-Predict
            return Combine(new VariableVector() { PolicyOut, ValueOut }, "Predict");
        }
        public static Function Conv2d(Variable Input, int outFeatureMapCount, int kernel_size, int stride, int group, DeviceDescriptor device, string Name, bool autoPadding = true)
        {
            int numInputChannels = Input.Shape[Input.Shape.Rank - 1];
            var convParams = new Parameter(new int[] { kernel_size, kernel_size, numInputChannels / group, outFeatureMapCount }, DataType.Float, NormalInitializer(2.5), device, $"{Name}_Parameter_W");
            var basis = new Parameter(new int[] { 1, 1, outFeatureMapCount }, DataType.Float, 0, device, $"{Name}_Parameter_B");
            return Plus(Convolution(convParams, Input, new int[] { stride, stride, numInputChannels }, new BoolVector() { true }, new BoolVector() { autoPadding }, new int[] { 1 }, 1, (uint)group), basis, Name);
        }
        public static Function BN_Layer(Variable Input, DeviceDescriptor device, string Name, bool spatial = true, double bnTimeConst = 4096)
        {
            var biasParams = new Parameter(new int[] { NDShape.InferredDimension }, DataType.Float, 0, device, $"{Name}_Parameter_B");
            var scaleParams = new Constant(new int[] { NDShape.InferredDimension }, DataType.Float, 0.25f, device, $"{Name}_Parameter_W");//  new Parameter(new int[] { NDShape.InferredDimension }, 2.0f, device, "");
            var runningMean = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device, $"{Name}_runningMean");
            var runningInvStd = new Constant(new int[] { NDShape.InferredDimension }, 0.0f, device, $"{Name}_runningInvStd");
            var runningCount = Constant.Scalar(0.0f, device);
            return BatchNormalization(Input, scaleParams, biasParams, runningMean, runningInvStd, runningCount, spatial, bnTimeConst, 0.0, 1e-5 /* epsilon */);
        }
        public static Function FCN(Variable Input, int outputDim, string Name, DeviceDescriptor device)
        {
            int inputDim = 1;
            foreach (var node in Input.Shape.Dimensions)
                inputDim *= node;
            var timesParam = new Parameter(new int[] { outputDim, inputDim }, DataType.Float, GlorotUniformInitializer(DefaultParamInitScale, SentinelValueForInferParamInitRank, SentinelValueForInferParamInitRank, 1), device, $"{Name}_Paramter_W");
            var timesFunction = Times(timesParam, Dropout(Input, 0.5, 0x1231377C));
            return Plus(new Parameter(new int[] { outputDim }, DataType.Float, 0.0f, device, $"{Name}_Paramter_B"), timesFunction);
        }
        public static Function TConv2d(Variable Input, int outFeatureMapCount, int kernel_size, int stride, DeviceDescriptor device, string Name, bool autoPadding = true)
        {
            int numInputChannels = Input.Shape[Input.Shape.Rank - 1];
            var TconvParams = new Parameter(new int[] { kernel_size, kernel_size, outFeatureMapCount, numInputChannels }, DataType.Float, HeNormalInitializer(2), device, $"{Name}_Parameter_W");
            var basis = new Parameter(new int[] { 1, 1, outFeatureMapCount }, DataType.Float, 0, device, $"{Name}_Parameter_B");
            return Plus(ConvolutionTranspose(TconvParams, Input, new int[] { stride, stride, outFeatureMapCount }, new BoolVector() { true }, new BoolVector() { autoPadding }), basis);
        }
        public static Function Copy_Crop(Variable Input_1, Variable Input_2)
        {
            var result = Splice(new VariableVector() { Input_1, Input_2 }, Axis.EndStaticAxis());
            var newShape = new int[] { result.Output.Shape[0], result.Output.Shape[1], result.Output.Shape[2] * result.Output.Shape[3] };
            return Reshape(result, newShape);
        }
        public static Function GlobalAveragePooling(Variable Input)
        {
            var result = Pooling(Input, PoolingType.Average, new int[] { Input.Shape[0], Input.Shape[1], 1 }, new int[] { Input.Shape[0], Input.Shape[1], 1 });
            var newShape = new int[] { result.Output.Shape[0] * result.Output.Shape[1] * result.Output.Shape[2] };
            return Reshape(result, newShape);
        }

        public static Function ResNeXtNode(Variable Input, DeviceDescriptor device, int maps, string Name)
        {
            var Conv_1 = Conv2d_Relu(Input, maps / 2, 3, 1, 32, device, $"{Name}_InnerConv_1");
            var Conv_2 = Conv2d(Conv_1, maps, 1, 1, 1, device, $"{Name}_InnerConv_2");
            return ReLU(BN_Layer(Plus(Conv_2, Input), device, $"{Name}_BN"));
        }
        public static Function Conv2d_Relu(Variable Input, int outFeatureMapCount, int kernel_size, int stride, int group, DeviceDescriptor device, string Name, bool autoPadding = true)
        {
            return ReLU(Conv2d(Input, outFeatureMapCount, kernel_size, stride, group, device, Name, autoPadding));
        }
        //public static Function ResNetCreator(Variable input, DeviceDescriptor device, int BoardSize = 19)
        //{
        //    double convWScale = 7.07;
        //    double convBValue = 0;
        //    double scValue = 1;
        //    int bnTimeConst = 4096;
        //    int kernelWidth = 3;
        //    int kernelHeight = 3;
        //    double conv1WScale = 0.26;
        //    int cMap1 = 16;
        //    /*输入卷积层*/
        //    var conv1 = ConvBatchNormalizationReLULayer(input, cMap1, kernelWidth, kernelHeight, 1, 1, conv1WScale, convBValue, scValue, bnTimeConst, true /*spatial*/, device);
        //    /*一级残差层，16特征图，3ResBlock*/
        //    var rn1_1 = ResNetNode(conv1, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    var rn1_2 = ResNetNode(rn1_1, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    var rn1_3 = ResNetNode(rn1_2, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    int cMap2 = 32;
        //    /*二级残差层，32特征图，3ResBlock*/
        //    var rn2_1 = ResNetNode(rn1_3, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    var rn2_2 = ResNetNode(rn2_1, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    var rn2_3 = ResNetNode(rn2_2, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    int cMap3 = 64;
        //    /*三级残差层，64特征图，3ResBlock*/
        //    var rn3_1 = ResNetNode(rn2_3, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    var rn3_2 = ResNetNode(rn3_1, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    var rn3_3 = ResNetNode(rn3_2, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
        //    /*全连接决策层，含价值、策略子网络*/
        //    var fc_1 = FCN(rn3_3, 4000, device, ReLU_Func);
        //    var fc_1_1 = FCN(fc_1, 1000, device, ReLU_Func, "PolicyIn");

        //    var fc2_1 = Dense(fc_1_1, BoardSize * BoardSize + 1, device, ReLU_Func, "PolicyC");
        //    var fc2_1_out = Plus(fc2_1, new Constant(fc2_1.Output.Shape, DataType.Float, float.Epsilon), device, "PolicyConst"), Policy_NM);
           
        //}
        //public static Function ResNetNode(Variable input, int InternalMapCounts, int kernelWidth, int kernelHeight, double wScale, double bValue, double scValue, int bnTimeConst, bool spatial, DeviceDescriptor device)
        //{
        //    int map_counts = input.Shape[input.Shape.Rank - 1];
        //    var c1 = ConvBatchNormalizationReLULayer(input, InternalMapCounts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
        //    var c2 = ConvBatchNormalizationLayer(c1, map_counts, kernelWidth, kernelHeight, 1, 1, wScale, bValue, scValue, bnTimeConst, spatial, device);
        //    var p = Plus(c2, input);
        //    return ReLU(p);
        //}
    }
}
