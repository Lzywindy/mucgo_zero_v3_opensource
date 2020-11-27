namespace MUCGO_zero_CS
{
    public interface IBoard
    {
        sbyte[] Area { get; }
        Stone Board_CurrentPlayer { get; }
        int BoardSize { get; }
        int[] capture_num { get; }
        sbyte CurrentPlayer { get; }
        byte[] EnabledPos { get; }
        byte[] EnabledPos4Analysis { get; }
        sbyte[] Features { get; }
        bool GameOver { get; }
        ulong HashCode { get; }
        int LastestPos { get; }
        Move_t LatestRecord { get; }
        int Moves { get; }
        int MovesFinished { get; }
        float Score { get; }
        float Score_Final { get; }
        float Score_UCT { get; }
        sbyte[] Terrains { get; }
        sbyte Winner { get; }
        float WinnerF { get; }
        void Clear();
        Board Clone();
        void Copy(ref Board OutBoard);
        Board[] DeepCopy(int counts);
        bool IsCurrentStaticEmptyPos(short pos);
        bool LastestPassMove(int last_index);
        bool PutStone_UCT(int pos);
        int ToBoardPos(int pos, bool convertFull = false);
        void UpdateScoreEsimate((sbyte[] area, float score) data);
    }
}