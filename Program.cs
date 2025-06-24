using System;
using System.IO;
using System.Windows.Forms;

namespace ServerLauncher
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            // Create icon file if it doesn't exist
            string iconPath = Path.Combine(Application.StartupPath, "app.ico");
            if (!File.Exists(iconPath))
            {
                try
                {
                    IconGenerator.CreateDefaultIcon(iconPath);
                }
                catch
                {
                    // Icon creation failed, but this is not critical
                }
            }
            
            Application.Run(new MainForm());
        }
    }
} 