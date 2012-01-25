using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MCAEmotiv.GUI.Animation;
using MCAEmotiv.Interop;
using CompetitionExp;
using MCAEmotiv.GUI;
using MCAEmotiv.GUI.Controls;

namespace CompetitionExp
{
    /// <summary>
    /// The main form for the GUI
    /// </summary>
    public class CompetitionForm : Form
    {
        private static readonly CompetitionForm instance = new CompetitionForm();
        /// <summary>
        /// Retrieves the singleton instance of this class
        /// </summary>
        public static CompetitionForm Instance { get { return instance; } }

        private Animator animator = null;
        private MockEEGDataSource mockDataSource = null;

        private CompetitionForm() : base() { }

        /// <summary>
        /// Animates the provider, disabling the form for the duration of the animation.
        /// If onFinish is non-null, it is called when the animation stops.
        /// </summary>
        public void Animate(IViewProvider provider, Action onFinish = null)
        {
            // collect here so hopefully this won't happen in the middle
            GC.Collect();
            if (this.animator == null)
                this.animator = new Animator();
            var oldState = this.WindowState;
            this.Enabled = false;
            this.animator.Start(provider, () =>
            {
                this.Enabled = true;
                this.WindowState = oldState;
                this.BringToFront();
                if (this.mockDataSource != null)
                {
                    this.mockDataSource.Dispose();
                    this.mockDataSource = null;
                }
                if (onFinish != null)
                    onFinish();
            });
        }

        /// <summary>
        /// Disposes of the control
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.animator != null)
                    this.animator.Dispose();
                if (this.mockDataSource != null)
                    this.mockDataSource.Dispose();
            }
            base.Dispose(disposing);
        }

        #region ---- Build View ----
        /// <summary>
        /// Builds the application view for the competition experiment
        /// </summary>
        public void BuildCompetitionExperimenterView()
        {
            this.SuspendLayout();
            this.Text = GUIUtils.Strings.APP_NAME;
            this.Size = new System.Drawing.Size(1500, 750);

            // start button
            var startButton = GUIUtils.CreateFlatButton("Start Experiment", b =>
            { this.Animate(new CompetitionExperimentProvider()); });

            this.Controls.Add(startButton);

            this.ResumeLayout(false);
        }

        /// <summary>
        /// Builds the application view
        /// </summary>
        public void BuildExperimenterView()
        {
            this.SuspendLayout();
            this.Text = GUIUtils.Strings.APP_NAME;
            this.Size = new System.Drawing.Size(1500, 750);

            // experiment settings
            var experimentPanel = new ExperimentPanel() { Dock = DockStyle.Fill };

            // classifier settings
            var classifierPanel = new ClassificationSchemePanel() { Dock = DockStyle.Fill };

            // stimulus class settings
            var stimulusClassPanel = new StimulusClassPanel() { Dock = DockStyle.Fill };

            // status checker
            var statusChecker = new EmotivStatusCheckerPanel() { Dock = DockStyle.Fill };

            // start button
            var startButton = GUIUtils.CreateFlatButton("Start Experiment", b =>
            {
                                var experimentSettings = experimentPanel.ExperimentSettings;
                experimentSettings.ImageDisplaySettings = stimulusClassPanel.ImageDisplaySettings;
                experimentSettings.ArtifactDetectionSettings = classifierPanel.ArtifactDetectionSettings;

                // check stimulus classes
                if (stimulusClassPanel.StimulusClass1 == null || stimulusClassPanel.StimulusClass2 == null)
                {
                    GUIUtils.Alert("Two stimulus classes must be selected", MessageBoxIcon.Error);
                    return;
                }

                if (stimulusClassPanel.StimulusClass1.UsedStimuli(experimentSettings.QuestionMode).IsEmpty()
                    || stimulusClassPanel.StimulusClass2.UsedStimuli(experimentSettings.QuestionMode).IsEmpty())
                {
                    GUIUtils.Alert("Each stimulus class must have at least one valid stimulus for the selected question mode", MessageBoxIcon.Error);
                    return;
                }

                if (stimulusClassPanel.StimulusClass1.Settings.Marker == stimulusClassPanel.StimulusClass2.Settings.Marker)
                {
                    GUIUtils.Alert("The two selected stimulus classes must have different marker values", MessageBoxIcon.Error);
                    return;
                }

                // check classifiers
                var classifiers = classifierPanel.SelectedClassifiers;
                foreach (var classifier in classifiers)
                    if (classifier.Settings.FeatureCount <= 0
                        && !GUIUtils.IsUserSure("Classifier " + classifier.Settings.Name + " has no features. Continue without this classifier?"))
                        return;
                
                // check headset
                if (!statusChecker.HeadsetConnected
                    && !GUIUtils.IsUserSure("The Emotiv headset is not connected: run experiment with mock headset (generates random data for testing purposes)?"))
                    return;

                this.Animate(new ExperimentProvider(experimentSettings,
                    stimulusClassPanel.StimulusClass1,
                    stimulusClassPanel.StimulusClass2,
                    classifiers.Where(c => c.Settings.FeatureCount > 0).ToIArray(),
                    statusChecker.HeadsetConnected
                        ? EmotivDataSource.Instance
                        : this.mockDataSource ?? (this.mockDataSource = new MockEEGDataSource())));
            });

            // add all controls
            var rows = GUIUtils.CreateTable(new double[] { .5, .5 }, Direction.Vertical);

            // top row
            var topCols = GUIUtils.CreateTable(new double[] { .25, .75 }, Direction.Horizontal);
            topCols.Controls.Add(experimentPanel, 0, 0);
            topCols.Controls.Add(classifierPanel, 1, 0);
            rows.Controls.Add(topCols, 0, 0);

            // bottom row
            var bottomCols = GUIUtils.CreateTable(new double[] { .75, .25 }, Direction.Horizontal);
            bottomCols.Controls.Add(stimulusClassPanel, 0, 0);
            var bottomRightTable = GUIUtils.CreateTable(new double[] { .6, .4 }, Direction.Vertical);
            bottomRightTable.Controls.Add(statusChecker, 0, 0);
            bottomRightTable.Controls.Add(startButton, 0, 1);
            bottomCols.Controls.Add(bottomRightTable, 1, 0);
            rows.Controls.Add(bottomCols, 0, 1);

            this.Controls.Add(rows);
            this.ResumeLayout(false);
        }

        private static void SetStyle(Control control)
        {
            control.ForeColor = System.Drawing.Color.Orange;
            control.BackColor = System.Drawing.Color.Black;
        }

        private static void Stylize(object sender, ControlEventArgs args)
        {
            SetStyle(args.Control);
            foreach (Control child in args.Control.Controls)
                Stylize(null, new ControlEventArgs(child));
            args.Control.ControlAdded += Stylize;
        }
        #endregion
    }
}
