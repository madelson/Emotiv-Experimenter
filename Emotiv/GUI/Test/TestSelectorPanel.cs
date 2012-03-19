using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.Test
{
    /// <summary>
    /// The file selector panel for the Karpicke-Roediger with Monitoring Software
    /// </summary>
    public class TestSelectorPanel : Panel 
    {
        private readonly LinkLabel presentationLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom, };
        private readonly LinkLabel testLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
        private readonly LinkLabel ansLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
        private readonly OpenFileDialog openDialog = new OpenFileDialog()
        {
            Title = "Load Stimuli",
            Filter = "Text files|*.txt",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = false
        };

        /// <summary>
        /// The file from which study phase stimuli are read
        /// </summary>
        public string PresentationFile { get { return this.presentationLink.Text; } set {
            presentationLink.Text = value;
        }
        }
        /// <summary>
        /// The file from which test phase stimuli are read
        /// </summary>
        public string TestFile
        {
            get { return this.testLink.Text; }
            set
            {
                testLink.Text = value;
            }
        }
        /// <summary>
        /// The file from which answers for test phase stimuli are read
        /// </summary>
        public string AnsFile
        {
            get { return this.ansLink.Text; }
            set
            {
                ansLink.Text = value;
            }
        }
        /// <summary>
        /// The selector panel for files for the Karpicke-Roediger monitoring experiment
        /// </summary>
        public TestSelectorPanel() : base()
        {
            var presentationLabel = "Presentation Stimuli".ToLabel(DockStyle.Bottom);
            var testLabel = "Test Stimuli".ToLabel(DockStyle.Bottom);
            var ansLabel = "Answers".ToLabel(DockStyle.Bottom);
            
            presentationLink.Click += (sender, args) =>
                {
                    if (this.openDialog.ShowDialog() != DialogResult.OK)
                        return;
                    presentationLink.Text = this.openDialog.FileName;
                };
            this.Controls.Add(presentationLabel);
            this.Controls.Add(presentationLink);

            testLink.Click += (sender, args) =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;
                testLink.Text = this.openDialog.FileName;
            };
            this.Controls.Add(testLabel);
            this.Controls.Add(testLink);

            ansLink.Click += (sender, args) =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;
                ansLink.Text = this.openDialog.FileName;
            };
            this.Controls.Add(ansLabel);
            this.Controls.Add(ansLink);

        }
    }
}
