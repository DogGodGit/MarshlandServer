using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace AStarMesh
{
    static class Program
    {

        public static Form1 MainForm;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            MainForm = new Form1();
            
            Application.Idle += new EventHandler(AppLoop);
            Application.Run(MainForm);
        }

        static void AppLoop(object sender, EventArgs e)
        {
            //MainForm.mapView.Invalidate();
        }
    }
}
