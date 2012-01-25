using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using MCAEmotiv.GUI.Controls;
using MCAEmotiv.GUI.Animation;
using MCAEmotiv.GUI;

namespace CompetitionExp
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

            CompetitionForm.Instance.BuildCompetitionExperimenterView();
            //CompetitionForm.Instance.VisibleChanged += (o, e) => CompetitionForm.Instance.Animate(new CompetitionExperimentProvider());
            Application.Run(CompetitionForm.Instance);


            /*var animator = new Animator();
            animator.Start(new CompetitionExperimentProvider(), () => GUIUtils.Alert("Hi!"));*/
            //MainForm.Instance.Animate(new CompetitionExperimentProvider());
            //Application.Run(new Form1());
        }
    }
}
