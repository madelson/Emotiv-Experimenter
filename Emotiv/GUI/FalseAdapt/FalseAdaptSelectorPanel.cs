using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.FalseAdapt
{
    /// <summary>
    /// A selector panel for files for the Competition Experiment
    /// </summary>
    public class FalseAdaptSelectorPanel : Panel 
    {
        private readonly LinkLabel presentationLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom, };
        private readonly OpenFileDialog openDialog = new OpenFileDialog()
        {
            Title = "Load Stimuli",
            Filter = "Text files|*.txt",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = false
        };

        /// <summary>
        /// The file from which initial study stimuli are read
        /// </summary>
        public string PresentationFile { get { return this.presentationLink.Text; } set {
            presentationLink.Text = value;
        }
        }
        
        /// <summary>
        /// The selector panel for files forthe competition experiment
        /// </summary>
        public FalseAdaptSelectorPanel() : base()
        {
            var presentationLabel = "Study Stimuli".ToLabel(DockStyle.Bottom);

            presentationLink.Click += (sender, args) =>
                {
                    if (this.openDialog.ShowDialog() != DialogResult.OK)
                        return;
                    presentationLink.Text = this.openDialog.FileName;
                };
            this.Controls.Add(presentationLabel);
            this.Controls.Add(presentationLink);

        }
    }
}
