using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Configurations;
using MCAEmotiv.GUI.Controls;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Animates the stimulus class setup tool
    /// </summary>
    public class StimulusClassSetupProvider : AbstractEnumerable<View>, IViewProvider
    {
        private const string SAVE = "Save Results", SKIP = "Skip", BACK = "Back";

        private readonly StimulusClass stimulusClass;

        /// <summary>
        /// Construct a provider for the given stimulus class
        /// </summary>
        public StimulusClassSetupProvider(StimulusClass stimulusClass)
        {
            this.stimulusClass = stimulusClass;
        }

        /// <summary>
        /// Returns the name of the stimulus class
        /// </summary>
        public string Title { get { return this.stimulusClass.Settings.Name; } }

        /// <summary>
        /// Yields a sequence of views that implement the tool
        /// </summary>
        public override IEnumerator<View> GetEnumerator()
        {
            IViewResult result;
            
            // for each image, get the user's input
            var stimuli = this.stimulusClass.Stimuli.ToIArray();
            for (int i = 0; i < stimuli.Count; i++)
            {
                yield return new ClassifyView(stimuli[i], this.stimulusClass, out result);
                if (BACK.Equals(result.Value))
                {
                    i = Math.Max(-1, i - 2);
                    continue;
                }
                if (SKIP.Equals(result.Value))
                    continue;
                stimuli[i].Subclass = (bool?)result.Value;
            }

            // a brief break
            yield return new RestView(500);

            // offer to save
            yield return new ChoiceView(new string[] 
            { 
                SAVE,
                "Don't Save"
            }, out result);

            // save
            if (SAVE.Equals(result.Value))
            {
                if (this.stimulusClass.TrySave())
                    yield return new TextView("Results saved to " + this.stimulusClass.SavePath, 2000);
                else
                    GUIUtils.Alert("Failed to save results to " + this.stimulusClass.SavePath, MessageBoxIcon.Error);

            }
        }

        #region ---- Private Views ----
        /// <summary>
        /// The result is a bool? reflecting the chosen subclass, SKIP, or BACK.
        /// </summary>
        private class ClassifyView : View
        {
            public ClassifyView(Stimulus stimulus, StimulusClass stimulusClass, out IViewResult result)
                : base()
            {
                var table = this.RegisterDisposable(GUIUtils.CreateTable(new double[] { .7, .1, .2 }, Direction.Vertical));
                Panel topPanel = new Panel() { Dock = DockStyle.Fill }, bottomPanel = new Panel() { Dock = DockStyle.Fill };
                table.Controls.Add(topPanel, 0, 0);
                var label = new Label() 
                { 
                    Dock = DockStyle.Fill, 
                    AutoSize = true, 
                    TextAlign = ContentAlignment.MiddleCenter, 
                    Text = "Current selection: ", 
                    Font = GUIUtils.Constants.DISPLAY_FONT 
                };
                table.Controls.Add(label, 0, 1);
                table.Controls.Add(bottomPanel, 0, 2);

                var imageView = new ImageView(-1) { ImagePath = stimulus.PathOrText };
                var choiceView = new ChoiceView(new string[] { stimulusClass.Settings.Answer1, stimulusClass.Settings.Answer2, GUIUtils.Strings.UNCLASSIFIED, SKIP, BACK });

                this.DoOnDeploy(c =>
                {
                    this.DeploySubView(imageView, topPanel);
                    topPanel.Controls.Add(new Label() 
                    { 
                        Text = GUIUtils.Strings.ImageExtensions.Contains(Path.GetExtension(stimulus.PathOrText)) 
                            ? Path.GetFileNameWithoutExtension(stimulus.PathOrText)
                            : stimulus.PathOrText, 
                        Dock = DockStyle.Top, 
                        TextAlign = ContentAlignment.MiddleCenter, 
                        Font = GUIUtils.Constants.DISPLAY_FONT
                    });
                    if (stimulus.Subclass == null)
                        label.Text += GUIUtils.Strings.UNCLASSIFIED;
                    else
                        label.Text += (bool)stimulus.Subclass
                            ? stimulusClass.Settings.Answer1
                            : stimulusClass.Settings.Answer2;
                    this.DeploySubView(choiceView, bottomPanel);
                    c.Controls.Add(table);
                });
                this.DoOnFinishing(() =>
                {
                    if (!(bool)imageView.Result.Value)
                        this.SetResult(null);
                    else if (stimulusClass.Settings.Answer1.Equals(choiceView.Result.Value))
                        this.SetResult(true);
                    else if (stimulusClass.Settings.Answer2.Equals(choiceView.Result.Value))
                        this.SetResult(false);
                    else if (BACK.Equals(choiceView.Result.Value))
                        this.SetResult(BACK);
                    else if (SKIP.Equals(choiceView.Result.Value))
                        this.SetResult(SKIP);
                    else
                        this.SetResult(null);
                });

                result = this.Result;
            }
        }
        #endregion
    }
}
