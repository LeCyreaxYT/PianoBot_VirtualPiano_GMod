using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using PianoBot_VirtualPiano_GMod.GUI;

namespace PianoBot_VirtualPiano_GMod
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new GraphicalUserInterface());
        }
    }
}
