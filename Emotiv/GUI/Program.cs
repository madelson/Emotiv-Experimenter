using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MCAEmotiv.GUI.Controls;
using MCAEmotiv.GUI.Configurations;
using MCAEmotiv.Classification;
using MCAEmotiv.Interop;

namespace MCAEmotiv.GUI
{
    /// <summary>
    /// Contains the main entry point for the application
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            //MainForm.Instance.BuildExperimenterView();
            //MainForm.Instance.BuildKRMonitorView();
            //MainForm.Instance.BuildTestView();
            MainForm.Instance.BuildUserCtrlView();
            //MainForm.Instance.BuildCompetitionExperimenterView();
            //MainForm.Instance.BuildAdaptiveView();
            //MainForm.Instance.BuildFAdaptView();
            Application.Run(MainForm.Instance);
            
            EmotivDataSource.Instance.Dispose();
        }
    }
}
