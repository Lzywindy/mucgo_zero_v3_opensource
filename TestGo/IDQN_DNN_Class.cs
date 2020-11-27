using System.Collections.Generic;
using CNTK;

namespace TestGo
{
    interface IDQN_DNN_Class
    {
        void ClearQueue();
        bool CreateModel(DeviceDescriptor device, NN_Model _nn_model = NN_Model.Conv_NN);
        void Eval();
        void EvalNode(KeyValuePair<NodeData, UCTNode> node);
        NN_Model getModelType();
        int GetQueueLength();
        bool HaveGPU();
        bool Load();
        void Save();
        string SetupModel(NN_Model _nn_model = NN_Model.Conv_NN);
        void Train(List<KeyValuePair<NodeData, UCTNode>> trainDatas, double learning_rate = 0.0010000000474974513, int epouch = 10, int minibatchsize = 100);
    }
}