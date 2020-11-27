using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MUCGO_zero_CS
{
    using System.Drawing.Drawing2D;
    using static Board;
    using static Stone;
    using static PatternHash;
    using static Utils;
    using System.Threading;
    public enum PlayModel { None, PlayWithMachine, CompareBefore, Selfplay }
    public partial class BoardUI : Form
    {
        #region Fields
        /// <summary>
        /// 棋盘变量
        /// </summary>
        public Board my_board => GUIServer.board;
        static readonly SolidBrush EnabledColorBlack = new SolidBrush(Color.FromArgb(125, 0, 0, 0));
        static readonly SolidBrush EnabledColorWhite = new SolidBrush(Color.FromArgb(125, 255, 255, 255));
        static readonly SolidBrush HighLightLastStep = new SolidBrush(Color.Red);
        static readonly string[] Pos_Col = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T" };
        static readonly string[] Pos_Row = new string[] { "19", "18", "17", "16", "15", "14", "13", "12", "11", "10", "9", "8", "7", "6", "5", "4", "3", "2", "1" };
        static readonly SolidBrush RemovableColor = new SolidBrush(Color.OrangeRed);
        static readonly SolidBrush StarPoint = new SolidBrush(Color.Black);
        static readonly SolidBrush TerrainColorBlack = new SolidBrush(Color.FromArgb(128, 0, 0, 0));
        static readonly SolidBrush TerrainColorWhite = new SolidBrush(Color.FromArgb(128, 255, 255, 255));
        static readonly Color _DefaultBackColor = Color.FromArgb(255, 236, 181, 98);
        static readonly string DefaultPath = GetExpFilePath();
        //private bool Selfplay = false;
        //private bool PlayWithMachine = false;
        //private bool PlayWithHuman = false;
        private volatile bool IsThinking = false;
        private volatile string SelectPlayModel = "";
        private sbyte[] Terrian;
        Stack<Board> backup = new Stack<Board>();
        /// <summary>
        /// 棋盘对应坐标
        /// </summary>
        Point[,] board_poses;
        /// <summary>
        /// 对应坐标的棋子颜色
        /// </summary>
        Color[,] board_poses_color;
        /// <summary>
        /// 棋盘大小
        /// </summary>
        int BoardSize;
        volatile bool enabledThinking;
        //bool EnabledUserUseBoard;
        bool[] EnabledPos;
        /// <summary>
        /// 棋盘线间隔
        /// </summary>
        int interval;
        Pen PenForBoardLines = new Pen(Color.Black);
        /// <summary>
        /// 棋盘的起始位置
        /// </summary>
        Point start_pix;
        SoundPlayer StoneSound;
        public bool EnabledThinking { get => enabledThinking; set { enabledThinking = value; } }
        #endregion Fields
        #region Constructors
        public BoardUI()
        {
            InitializeComponent();
            InitCtrls();
            EnabledThinking = false;
            ClearBoard();
            //GUIServer.ComputerBlack = false;
            if (File.Exists(@"..\..\落子声.wav"))
                StoneSound = new SoundPlayer(@"..\..\落子声.wav");
            else
                StoneSound = null;
        }
        private void InitCtrls()
        {
            BoardPanel.Paint += BoardPanel_Paint;
            OpenFile.Click += OpenFile_Click;
            SaveFile.Click += SaveFile_Click;
            BackUpBT.Click += BackupBT_Click;
            BackUpBT.Click += UpdateInfo_1;
            NewGame.Click += NewGame_Click;
            NewGame.Click += UpdateInfo_1;
            Quit.Click += Quit_Click;
            BoardPanel.MouseDown += BoardPanel_MouseDown;
            BoardPanel.MouseDown += UpdateInfo_2;
            ModelSelectCBox.SelectedIndexChanged += ModelSelectCBox_SelectedIndexChanged;
            BoardPanel.BackColor = _DefaultBackColor;
            UpdateTimer.Tick += UpdateTimer_Func;
            UpdateTimer.Tick += UpdateInfo_1;
            PlayCheck.CheckedChanged += PlayCheck_CheckedChanged;
            ModelSelectCBox.Items.AddRange(new object[] { "机器执黑", "机器执白" });
            ModelSelectCBox.SelectedIndex = 0;
            CBoxModelSel.SelectedIndexChanged += CBoxModelSel_SelectedIndexChanged;
            CBoxModelSel.SelectedIndex = 0;
            TerrainCheck.CheckedChanged += TerrainCheck_CheckedChanged;
            UpdateTimer.Start();
            ThinkingWorker.DoWork += ThinkingWorker_DoWork;
            ThinkingWorker.RunWorkerCompleted += ThinkingWorker_RunWorkerCompleted;
        }

        private void TerrainCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (TerrainCheck.Checked)
            {
                GUIServer.AreaEsimate();
                Terrian = my_board.Terrains;
            }
        }

        /// <summary>
        /// 游戏模式选择
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CBoxModelSel_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (CBoxModelSel.SelectedItem as string)
            {
                case "人机对弈":
                    ModelSelectCBox.Items.Clear();
                    ModelSelectCBox.Items.AddRange(new object[] { "机器执黑", "机器执白" });
                    ModelSelectCBox.SelectedIndex = 0;
                    ModelSelectCBox.Enabled = true;
                    break;
                case "自对弈训练":
                    ModelSelectCBox.Items.Clear();
                    ModelSelectCBox.Items.AddRange(new object[] { "自对弈" });
                    ModelSelectCBox.SelectedIndex = 0;
                    ModelSelectCBox.Enabled = false;
                    break;
                default:
                    break;
            }
            SelectPlayModel = CBoxModelSel.SelectedItem as string;
        }
        /// <summary>
        /// 选择对弈方式
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ModelSelectCBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (ModelSelectCBox.SelectedItem as string)
            {
                case "机器执黑":
                case "现在执黑":
                    GUIServer.ComputerBlack = true;
                    break;
                case "机器执白":
                case "现在执白":
                    GUIServer.ComputerBlack = false;
                    break;
                default:
                    break;
            }
            //if (PlayWithHumanCheck.Checked)
            //{
            //    GUIServer.ComputerBlack = ModelSelectCBox.SelectedIndex == 0;
            //}
            //else if (SelfCmpCheck.Checked)
            //{
            //    GUIServer.ComputerBlack = ModelSelectCBox.SelectedIndex == 0;
            //}
        }
        private void ThinkingWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            IsThinking = false;
            BoardPanel.Invalidate();
            switch (CBoxModelSel.SelectedItem as string)
            {
                case "自对弈训练":
                    if (PlayCheck.Checked)
                        ThinkingWorker.RunWorkerAsync();
                    break;
                default:
                    break;
            }

        }

        private void ThinkingWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            IsThinking = true;
            switch (SelectPlayModel)
            {
                case "人机对弈":
                    if (GUIServer.PlaySide == GUIServer.ComputerBlack)
                    {
                        GUIServer.NNThinking();
                    }
                    break;
                case "自对弈训练":
                    GUIServer.Selfplay();
                    break;
                default:
                    break;
            }
        }
        #endregion Constructors
        #region Methods
        /// <summary>
        /// 移动窗体
        /// </summary>
        /// <param name="m"></param>
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            if (m.Msg == 0x84) { m.Result = (IntPtr)0x2; return; }
        }
        /// <summary>
        /// 悔棋
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BackupBT_Click(object sender, EventArgs e)
        {
            GUIServer.Backup();
            EnabledPos = my_board.candidates;
            //刷新
        }
        /// <summary>
        /// 棋盘_鼠标按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (IsThinking || !PlayCheck.Checked) return;
            var board_pos = ClickPos2BoardPos(e.X, e.Y, out int boardpos_x, out int boardpos_y);
            var pos = boardpos_x + boardpos_y * pure_board_size;
            if (InRange(boardpos_x, boardpos_y) && EnabledPos[pos])
            {
                backup.Push(my_board.Clone() as Board);
                if (!my_board.PutStone_UCT(pos))
                    backup.Pop();
                EnabledPos = my_board.candidates;
                GUIServer.PlaySide = !GUIServer.PlaySide;
                EnabledThinking = true;
            }
        }
        /// <summary>
        /// 棋盘_Paint
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BoardPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics 绘图 = e.Graphics;
            if (my_board == null) return;
            DrawBoard(e, PenForBoardLines);
            GetBoardStone(e);
            DrawEnabledPos(e, PenForBoardLines);
        }
        /// <summary>
        /// 棋盘的交叉点
        /// </summary>
        /// <param name="start_pix"></param>
        /// <param name="interval"></param>
        private void CalcBoardPoses(Point start_pix, int interval = 33)
        {
            board_poses = new Point[BoardSize, BoardSize];
            board_poses_color = new Color[BoardSize, BoardSize];
            for (int i = 0; i < BoardSize * BoardSize; i++)
            {
                var x = i % BoardSize;
                var y = i / BoardSize;
                board_poses[y, x].X = start_pix.X + x * interval;
                board_poses[y, x].Y = start_pix.Y + y * interval;
                board_poses_color[y, x] = Color.Transparent;
            }
        }
        /// <summary>
        /// 清空盘面
        /// </summary>
        private void ClearBoard()
        {
            BoardSize = PURE_BOARD_SIZE;
            interval = 33;
            start_pix = new Point(interval, interval);
            CalcBoardPoses(start_pix, interval);
            GUIServer.ResetBoard();
            EnabledPos = my_board.candidates;
            backup.Clear();
        }
        /// <summary>
        /// 将像素坐标转为棋盘上的点
        /// </summary>
        /// <param name="mouse_pos_x"></param>
        /// <param name="mouse_pos_y"></param>
        /// <param name="boardpos_x"></param>
        /// <param name="boardpos_y"></param>
        /// <returns></returns>
        private int ClickPos2BoardPos(int mouse_pos_x, int mouse_pos_y, out int boardpos_x, out int boardpos_y)
        {
            boardpos_x = (mouse_pos_x - (interval + 1) / 2) / interval;
            boardpos_y = (mouse_pos_y - (interval + 1) / 2) / interval;
            if (InRange(boardpos_x, boardpos_y))
                return boardpos_x + boardpos_y * BoardSize;
            return -1;
        }
        /// <summary>
        /// 绘制棋盘
        /// </summary>
        /// <param name="e"></param>
        /// <param name="pen"></param>
        void DrawBoard(PaintEventArgs e, Pen pen)
        {
            int[] xy_star = new int[3] { (pure_board_size >> 2) - 1, pure_board_size >> 1, (pure_board_size >> 1) + (pure_board_size >> 2) + 2 };
            //划线、盘面字
            int Fsize = 13;
            for (int offset = 0; offset < pure_board_size; offset++)
            {
                e.Graphics.DrawString(Pos_Row[offset], new Font(new Font("微软雅黑", Fsize), Font.Style), StarPoint, start_pix.X - Fsize * 2, start_pix.Y + offset * interval - Fsize / 2);
                e.Graphics.DrawString(Pos_Col[offset], new Font(new Font("微软雅黑", Fsize), Font.Style), StarPoint, start_pix.X + offset * interval - Fsize / 2, start_pix.Y - Fsize * 2);
                e.Graphics.DrawString(Pos_Row[offset], new Font(new Font("微软雅黑", Fsize), Font.Style), StarPoint, start_pix.X * pure_board_size + Fsize, start_pix.Y + offset * interval - Fsize / 2);
                e.Graphics.DrawString(Pos_Col[offset], new Font(new Font("微软雅黑", Fsize), Font.Style), StarPoint, start_pix.X + offset * interval - Fsize / 2, start_pix.Y * pure_board_size + Fsize);
                e.Graphics.DrawLine(pen, start_pix.X, start_pix.Y + offset * interval, start_pix.X + interval * (BoardSize - 1), start_pix.Y + offset * interval);
                e.Graphics.DrawLine(pen, start_pix.X + offset * interval, start_pix.Y, start_pix.X + offset * interval, start_pix.Y + interval * (BoardSize - 1));
            }
            //画星点
            int R_Hight = 9, R_Width = 9;
            for (int pos = 0; pos < pure_board_max; pos++)
            {
                int px = pos % pure_board_size;
                int py = pos / pure_board_size;
                if (Array.Exists(xy_star, (int item) => item == px) && Array.Exists(xy_star, (int item) => item == py))
                {
                    int x = px * interval;
                    int y = py * interval;
                    e.Graphics.FillEllipse(StarPoint, new Rectangle(x + interval - R_Width / 2, y + interval - R_Hight / 2, R_Width, R_Hight));
                }
            }
        }
        /// <summary>
        /// 绘制着子点
        /// </summary>
        /// <param name="e"></param>
        /// <param name="pen"></param>
        void DrawEnabledPos(PaintEventArgs e, Pen pen)
        {
            //轮到哪一方下棋
            WhoTurn.BackColor = my_board.Board_CurrentPlayer == S_BLACK ? Color.Black : Color.White;
            //画最近落子的位置
            if (my_board.record.Count > 0 && my_board.record[my_board.record.Count - 1].pos != PASS)
            {
                short pos = onboard_pos_2pure[my_board.record[my_board.record.Count - 1].pos];
                int last_x = (pos % pure_board_size + 1) * interval;
                int last_y = (pos / pure_board_size + 1) * interval;
                Point[] Trangle = new Point[3] { new Point(last_x - 10, last_y + 5), new Point(last_x + 10, last_y + 5), new Point(last_x, last_y - 10) };
                e.Graphics.FillPolygon(HighLightLastStep, Trangle);
            }
            int R_Hight = 9, R_Width = 9;
            //可着子点显示
            if (EnabledPosCheck.Checked)
            {
                var EnabledPos = my_board.EnabledPos4Analysis;
                var EnabledColor = my_board.Board_CurrentPlayer == S_BLACK ? EnabledColorBlack : EnabledColorWhite;
                for (int i = 0; i < BoardSize; i++)
                {
                    for (int j = 0; j < BoardSize; j++)
                    {
                        var pos = j + i * pure_board_size;
                        if (EnabledPos[pos] > 0)
                            e.Graphics.FillRectangle(EnabledColor, j * interval + interval - R_Width / 2, i * interval + interval - R_Hight / 2, R_Width, R_Hight);
                    }
                }
            }
        }
        /// <summary>
        /// 查看是否是合法点位
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EnabledPosCheck_CheckedChanged(object sender, EventArgs e)
        {
        }
        /// <summary>
        /// 虚着
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EndMyTurn_Click(object sender, EventArgs e)
        {
            my_board.PutStone_UCT(pure_board_max);
            EnabledPos = my_board.candidates;
            if (StoneSound != null) StoneSound.Play();
        }
        /// <summary>
        /// 绘制盘面的棋子
        /// </summary>
        /// <param name="e"></param>
        void GetBoardStone(PaintEventArgs e)
        {
            if (my_board == null) return;
            //绘制棋子
            for (int i = 0; i < BoardSize; i++)
            {
                for (int j = 0; j < BoardSize; j++)
                {
                    var pos = onboard_pos_2full[j + i * pure_board_size];
                    if (my_board.board[pos] != S_EMPTY)
                    {
                        var linearGradientBrush = new LinearGradientBrush(new Point((j + 1) * interval + interval / 2, (i + 1) * interval + interval / 2), new Point((j + 1) * interval + (int)(interval * 1.5), (i + 1) * interval + (int)(interval * 1.5)), my_board.board[pos] == S_BLACK ? Color.Black : Color.White, Color.Gray);
                        e.Graphics.FillEllipse(linearGradientBrush, board_poses[i, j].X - (interval - 1) / 2, board_poses[i, j].Y - (interval - 1) / 2, interval, interval);//绘制棋子 
                    }
                }
            }

            //绘制区域
            if (TerrainCheck.Checked && Terrian != null)
            {
                int R_Size = 33;
                for (int i = 0; i < Terrian.Length; i++)
                {
                    if (Terrian[i] != 0)
                    {
                        var x = (i % pure_board_size + 1) * interval;
                        var y = (i / pure_board_size + 1) * interval;
                        e.Graphics.FillRectangle(Terrian[i] == 1 ? TerrainColorBlack : TerrainColorWhite, x - R_Size / 2, y - R_Size / 2, R_Size, R_Size);
                    }
                }
            }
        }
        /// <summary>
        /// 判断点击是否在范围内
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        bool InRange(int x, int y)
        {
            return BoardSize > 0 && x >= 0 && y >= 0 && x < BoardSize && y < BoardSize;
        }
        /// <summary>
        /// 新局 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void NewGame_Click(object sender, EventArgs e)
        {
            //清盘
            ClearBoard();
        }
        /// <summary>
        ///  载入棋谱
        /// </summary>
        private void OpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = GetExpFilePath();
            openFileDialog.FileName = "棋谱.sgf";
            openFileDialog.Filter = "棋谱 (*.sgf)|*.sgf";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                backup.Clear();
                SGF sgf = new SGF();
                sgf.Load(openFileDialog.FileName);
                backup.Push(sgf.currentboard);
                //刷新
            }
        }
        /// <summary>
        /// 退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Quit_Click(object sender, EventArgs e)
        {
            GUIServer.SaveConfig();
            GUIServer.SaveLastState();
            Environment.Exit(0);
        }
        /// <summary>
        /// 保存棋谱
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SaveFile_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.InitialDirectory = DefaultPath;
            saveFileDialog.FileName = "棋谱.sgf";
            saveFileDialog.Filter = "棋谱 (*.sgf)|*.sgf";
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                SGF sgf = new SGF();
                sgf.Record(my_board);
                sgf.Save(saveFileDialog.FileName);
                //my_board.Save(saveFileDialog.FileName);
            }
        }
        #region 更新棋盘信息
        private void UpdateBoardInfo()
        {
            if (StoneSound != null) StoneSound.Play();
            if (my_board == null) return;
            BlackCount.Text = "" + my_board.prisoner[0];
            WhiteCount.Text = "" + my_board.prisoner[1];
            Steps.Text = "" + my_board.Moves;
            if (my_board == null) return;
            if (my_board.Area != null)
                AdvantageTitle.Text = Math.Sign(my_board.Score) == 1 ? $"B+{my_board.Score}" : Math.Sign(my_board.Score) == -1 ? $"W+{-my_board.Score}" : "Draw";
            else
                AdvantageTitle.Text = my_board.Winner == 1 ? $"B+{my_board.Score_Final}" : my_board.Winner == -1 ? $"W+{-my_board.Score_Final}" : "Draw";
            if (my_board.record.Count > 0)
            {
                var colorString = my_board.record[my_board.record.Count - 1].color == S_BLACK ? "B" : "W";
                var location_fpos = my_board.record[my_board.record.Count - 1].pos;
                var x = location_fpos % board_size - board_start;
                var y = location_fpos / board_size - board_start;
                if (x >= 0 && y >= 0 && x < pure_board_size && y < pure_board_size)
                    PosLocation.Text = $"genmove {colorString} = {Pos_Col[x]}{Pos_Row[y]}";
                else
                    PosLocation.Text = $"genmove {colorString} = PASS";
            }
            else PosLocation.Text = "";

        }
        private void UpdateInfo_1(object sender, EventArgs e)
        {
            UpdateBoardInfo();
        }
        private void UpdateInfo_2(object sender, MouseEventArgs e)
        {
            UpdateBoardInfo();
        }
        #endregion
        /// <summary>
        /// 是否对弈进行中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PlayCheck_CheckedChanged(object sender, EventArgs e)
        {
            if (PlayCheck.Checked)
            {
                PlayCheck.Text = "正在对弈。。。";
                if (GUIServer.Resigned)
                    GUIServer.ResetBoard();
            }

            else
                PlayCheck.Text = "准备对弈";
        }
        /// <summary>
        /// 每100ms更新界面
        /// </summary>
        /// <param name="myObject"></param>
        /// <param name="myEventArgs"></param>
        private void UpdateTimer_Func(object sender, EventArgs e)
        {
            if (GUIServer.Resigned)
                PlayCheck.Checked = true;
            if (PlayCheck.Checked && !IsThinking && !ThinkingWorker.IsBusy)
                ThinkingWorker.RunWorkerAsync();
            CBoxModelSel.Enabled = !(PlayCheck.Checked || IsThinking);
            bool EnabledPlayer = false;
            switch (SelectPlayModel)
            {
                case "人机对弈":
                    TuresName.Text = (GUIServer.ComputerBlack == GUIServer.PlaySide) ? "MUCGO Zero" : "你";
                    ModelSelectCBox.Enabled = !PlayCheck.Checked;
                    PlayCheck.Enabled = !IsThinking;
                    EnabledPlayer = true;
                    break;
                case "自对弈训练":
                    TuresName.Text = "自对弈";
                    ModelSelectCBox.Enabled = !PlayCheck.Checked;
                    PlayCheck.Enabled = true;
                    EnabledPlayer = false;
                    break;
                default:
                    TuresName.Text = "";
                    break;
            }
            ThinkingStatement.Style = ThinkingWorker.IsBusy ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            NewGame.Enabled = BackUpBT.Enabled = Resign.Enabled = !IsThinking && EnabledPlayer;
            //EnabledThinking = (GUIServer.ComputerBlack == GUIServer.PlaySide || RLCheck.Checked || (SelfCmpCheck.Checked && GUIServer.NNComparerLoaded)) && PlayCheck.Checked;
            //ThinkingStatement.Style = EnabledThinking ? ProgressBarStyle.Marquee : ProgressBarStyle.Blocks;
            //BasicCtrlPanel.Enabled = !RLCheck.Checked;
            //NewGame.Enabled = BackUpBT.Enabled = EndMyTurn.Enabled = Resign.Enabled = !SelfCmpCheck.Checked;
            //LoadOldNN.Enabled = SelfCmpCheck.Checked;
            //ModelSelectCBox.Enabled = GUIServer.NNComparerLoaded && (!PlayCheck.Checked);
            //PlayCheck.Enabled = !EnabledThinking || RLCheck.Checked;
            //EnabledUserUseBoard = (!EnabledThinking) && (PlayCheck.Checked);
            //GameModelPanel.Enabled = !PlayCheck.Checked;
            //if (PlayWithHumanCheck.Checked)
            //   
            //else if (RLCheck.Checked)
            //{
            //   
            //    my_board = GUIServer.board;
            //}
            //else if (SelfCmpCheck.Checked)
            //    
            //else
            //    
            //if (PlayWithHumanCheck.Checked || RLCheck.Checked)
            //{
            //   
            //}
            //else if (SelfCmpCheck.Checked)
            //{
            //    

            //}
            BoardPanel.Invalidate();
        }

        #endregion Methods

        private void LoadOldNN_Click(object sender, EventArgs e)
        {
            openFileDialogOldNN.InitialDirectory = GetExpFilePath();
            openFileDialogOldNN.FileName = "";
            if (openFileDialogOldNN.ShowDialog() == DialogResult.OK)
            {
                GUIServer.SetSelfCompareNN(openFileDialogOldNN.FileName);
            }
        }

        private void Resign_Click(object sender, EventArgs e)
        {
            GUIServer.Play(RESIGN);
        }
    }
}
