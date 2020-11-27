using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MUCGO_zero_CS
{
    public class UCTPlay : UCT_Search
    {
        public UCTPlay(int depth) : base(depth) { }
      
        public int Genmove(Board board)
        {
            var ParentNode = PlayPath.Count > 0 ? PlayPath[PlayPath.Count - 1].node : null;
            /*启动评估*/
            EvalLoop();
            int pos = Search(board, ParentNode, false);
            var ppos = board.ToBoardPos(pos);
            if (ParentNode != null)
                ParentNode.ClearEdges(ppos);
            ParentNode = CreateRootOrVisit(board, ParentNode, true);
            PlayPath.Add((ParentNode, pos));
            GC.Collect();
            GC.WaitForPendingFinalizers();
            return pos;
        }
        public void Play(Board board, short pos)
        {
            var ParentNode = PlayPath.Count > 0 ? PlayPath[PlayPath.Count - 1].node : null;
            if (!board.PutStone_UCT(pos)) return;
            var ppos = board.ToBoardPos(pos);
            if (ParentNode != null)
                ParentNode.ClearEdges(ppos);
            ParentNode = CreateRootOrVisit(board, ParentNode, true);
            PlayPath.Add((ParentNode, pos));
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public void ClearPath()
        {
            PlayPath.Clear();
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }
        public void BackUp(int step)
        {
            if (step < PlayPath.Count && step > 0)
            {
                var index = PlayPath.Count - 1 - step;
                var count = step;
                PlayPath.RemoveRange(index, count);
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
    }
}
