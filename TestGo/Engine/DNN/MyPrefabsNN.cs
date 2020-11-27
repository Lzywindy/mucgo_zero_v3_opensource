using System;
namespace MUCGO_zero_CS
{
    using CNTK;
    using MyCNTKPrefebs;
    using static CNTK.CNTKLib;
    using static MyCNTKPrefebs.BasicModel_NN;
    using static MyCNTKPrefebs.NetworkInNetworkFunctionsAndExpands;
    using static MyCNTKPrefebs.Activation;
    using static MyCNTKPrefebs.RandomType;
    using static MyCNTKPrefebs.InitiateSeletction;
    using static ConstValues;
    using static Math;
    using static Utils;
    using static Board;
    /// <summary>
    /// 事先做好的例子
    /// </summary>
    public class MyPrefabsNN
    {
        #region 神经网络
        /*全连接测试网络*/
        protected static Function MLNCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            var dense_1 = Dense(input,BoardSize * BoardSize *3, device, ReLU_Func);
            var dense_2 = Dense(dense_1, 2048, device, ReLU_Func);
            /*全连接决策层，含价值、策略子网络*/
            var fc_1 = Dense(dense_2, 1024, device, ReLU_Func);
            var fc2_1 = ReLU(Dense(fc_1, BoardSize * BoardSize + 1, device, None_Func), PolicyNM);
            var fc2_2 = Dense(fc_1, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*MINIST改的*/
        protected static Function CNNCreator(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            int basemaps = 40;
            var input_layer = MLN_Conv(input, device, basemaps * 2, basemaps);

            var conv_maxout_1 = MaxAverageOutLayer(input_layer, device, basemaps, 0.5, 5, 3, 1);
            var conv_bn_layer_1 = MLN_Conv(conv_maxout_1, device, basemaps, basemaps, true, true);
            var conv_layer_1 = MLN_Conv(conv_bn_layer_1, device, basemaps, basemaps);

            var conv_maxout_2 = MaxAverageOutLayer(conv_layer_1, device, basemaps, 0.5, 5, 3, 1);
            var conv_bn_layer_2 = MLN_Conv(conv_maxout_2, device, basemaps, basemaps, true, true);
            var conv_layer_2 = MLN_Conv(conv_bn_layer_2, device, basemaps, basemaps);

            var conv_maxout_3 = MaxAverageOutLayer(conv_layer_2, device, basemaps * 2, 0.5, 5, 1, 1);

            var conv_bn_layer_out_value_1 = MLN_Conv(conv_maxout_3, device, basemaps, basemaps, true, true);
            var conv_layer_out_value_1 = MLN_Conv(conv_bn_layer_out_value_1, device, basemaps, basemaps);
            var conv_maxout_out_value_1 = MaxAverageOutLayer(conv_layer_out_value_1, device, basemaps, 0.5, 5, 1, 1);

            var conv_bn_layer_out_policy_1 = MLN_Conv(conv_maxout_3, device, basemaps, basemaps, true, true);
            var conv_layer_out_policy_1 = MLN_Conv(conv_bn_layer_out_policy_1, device, basemaps, basemaps);
            var conv_maxout_out_policy_1 = MaxAverageOutLayer(conv_layer_out_policy_1, device, basemaps, 0.5, 5, 1, 1);

            var gap_valueout = Reshape(GAPLayer(conv_maxout_out_value_1, device, 1), new int[] { 1 }, ValueNM);
            var gap_policyout = Reshape(GAPLayer(conv_maxout_out_policy_1, device, (BoardSize * BoardSize + 1)), new int[] { (BoardSize * BoardSize + 1) }, PolicyNM);
            return Combine(new VariableVector() { gap_policyout, gap_valueout });
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
            var rn1_2 = ResNetNode(rn1_1, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn1_3 = ResNetNode(rn1_2, cMap1, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            int cMap2 = 32;
            /*二级残差层，32特征图，3ResBlock*/
            var rn2_1 = ResNetNode(rn1_3, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn2_2 = ResNetNode(rn2_1, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn2_3 = ResNetNode(rn2_2, cMap2, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            int cMap3 = 64;
            /*三级残差层，64特征图，3ResBlock*/
            var rn3_1 = ResNetNode(rn2_3, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn3_2 = ResNetNode(rn3_1, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            var rn3_3 = ResNetNode(rn3_2, cMap3, kernelWidth, kernelHeight, convWScale, convBValue, scValue, bnTimeConst, false /*spatial*/, device);
            /*全连接决策层，含价值、策略子网络*/
            var fc_1 = Dense(rn3_3, 4000, device, ReLU_Func);
            var fc_1_1 = Dense(fc_1, 1000, device, ReLU_Func, "PolicyIn");
            var fc_1_2 = Dense(fc_1, 100, device, ReLU_Func, "ValueIn");

            var fc2_1 = Dense(fc_1_1, BoardSize * BoardSize + 1, device, ReLU_Func, "PolicyC");
            var fc2_1_out = Plus(fc2_1, new Constant(fc2_1.Output.Shape, DataType.Float, Pow(10, -9), device, "PolicyConst"), PolicyNM);
            //var fc2_2 = Dense(fc_1_2, 1, device, Tanh_Func, "ValueC");
            //var fc2_2_out = Plus(fc2_2, new Constant(fc2_2.Output.Shape, DataType.Float, Pow(10, -9), device, "ValueConst"), ValueNM);
            //return Combine(new VariableVector() { fc2_1_out, fc2_2_out });
            var fc2_2 = Dense(fc_1_2, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1_out, fc2_2 });
            //var fc2_1 = Dense(fc_1_1, BoardSize * BoardSize + 1, device, ReLU_Func, PolicyNM);
            //var fc2_2 = Dense(fc_1_2, 1, device, Tanh_Func, ValueNM);
            //return Combine(new VariableVector() { fc2_1, fc2_2 });
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
            int Zoom_size_1 = 40;
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
            var policy_0 = Dense(fusion_0, 4096, device, ReLU_Func);
            var policy_1 = ReLU(Dense(policy_0, BoardSize * BoardSize + 1, device, None_Func), PolicyNM);
            var value_0 = Dense(fusion_0, 1024, device, Tanh_Func);
            var value_1 = Dense(value_0, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { policy_1, value_1 });
        }
        #endregion
        /*这个ResNet是用图像分类改的*/
        protected static Function ResNetCreatorWithoutValue(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
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
            var fc_1 = Dense(rn3_3, 6000, device, ReLU_Func);
            var fc_2 = Dense(fc_1, 2000, device, ReLU_Func);
            var fc2_1 = ReLU(Dense(fc_2, BoardSize * BoardSize + 1, device, None_Func), PolicyNM);
            var fc2_2 = Dense(fc_2, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { fc2_1, fc2_2 });
        }
        /*UNet基于图像分割思想，这里稍作改动*/
        protected static Function UNetWithoutValue(Variable input, DeviceDescriptor device, int BoardSize = 19, string PolicyNM = "Policy", string ValueNM = "Value")
        {
            double convBValue = 0;
            double scValue = 1;
            int bnTimeConst = 4096;
            int kernelWidth = 3;
            int kernelHeight = 3;
            double conv1WScale = 0.26;
            int Zoom_size_1 = 40;
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
            var policy_0 = Dense(fusion_0, 4096, device, ReLU_Func);
            var policy_1 = ReLU(Dense(policy_0, BoardSize * BoardSize + 1, device, None_Func), PolicyNM);
            var value_0 = Dense(fusion_0, 1024, device, Tanh_Func);
            var value_1 = Dense(value_0, 1, device, Tanh_Func, ValueNM);
            return Combine(new VariableVector() { policy_1, value_1 });
        }
    };
}
