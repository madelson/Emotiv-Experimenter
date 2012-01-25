using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Displays a training message until all classifiers are finished training
    /// </summary>
    public class TrainView : View
    {
        /// <summary>
        /// Text to be displayed as a title
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Construct a view with the specified classifier managers
        /// </summary>
        public TrainView(IArrayView<ClassifierManager> classifiers)
            : base()
        {
            var panel = this.RegisterDisposable(new Panel() { Dock = DockStyle.Fill, UseWaitCursor = true });
            int count = classifiers.Count;

            this.DoOnDeploy(c =>
            {
                c.Controls.Add(panel);
                foreach (int i in classifiers.Indices().Reverse())
                {
                    var classifier = classifiers[i];

                    var label = ("Training " + classifier.Settings.Name + "...").ToLabel(DockStyle.Top, ContentAlignment.MiddleCenter, false);
                    label.Font = GUIUtils.Constants.DISPLAY_FONT;
                    panel.Controls.Add(label);
                    
                    // perform training in a separate thread
                    ThreadPool.QueueUserWorkItem(ignored =>
                    {
                        classifier.Train();
                        this.Invoke(() =>
                        {
                            label.Text = classifier.Settings.Name + " trained";

                            if (--count > 0)
                                return;

                            panel.UseWaitCursor = false;
                            var continuePanel = new Panel() { Dock = DockStyle.Bottom };
                            this.DeploySubView(new ChoiceView("Continue".Enumerate()), continuePanel);
                            panel.Controls.Add(continuePanel);
                        });
                    });
                }

                if (!string.IsNullOrWhiteSpace(this.Text))
                    panel.Controls.Add(new Label() { Text = this.Text, Dock = DockStyle.Top, TextAlign = ContentAlignment.MiddleCenter, Font = GUIUtils.Constants.DISPLAY_FONT });
            });
        }
    }
}
