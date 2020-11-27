using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace MUCGO_zero_CS
{
    using System.Threading;
    using System.IO;
    using System.Windows.Forms;
    public class MainClass
    {
        public static GameConfig config;
        //delegate void TM();
        [STAThread]
        static void Main(string[] args)
        {
            if (!File.Exists(GameConfig.Path))
            {
                config = new GameConfig(true);
                GameConfig.Save(config);
            }
            else
            {
                try
                {
                    config = GameConfig.Load();
                    GameConfig.Save(config);
                }
                catch (Exception)
                {
                    config = new GameConfig(true);
                    GameConfig.Save(config);
                }
            }
            if (config.ProgramMode.ToUpper() == "GUI")
            {
                GUIServer.InitGUIServer(config);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new BoardUI());
            }
            else
            {
                //GTPPlayInterface.ParameterAnaylsis(args);
                GTPPlayInterface.ReadConfigs(config);
                GTPPlayInterface.MainGTP();
            }
        }
    }
}
