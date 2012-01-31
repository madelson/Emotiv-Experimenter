using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.KRMonitor
{
    public class KRMonitorSelectorPanel : Panel 
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

        public string PresentationFile { get { return this.presentationLink.Text; } set {
            presentationLink.Text = value;
        }
        }
        public string TestFile
        {
            get { return this.testLink.Text; }
            set
            {
                testLink.Text = value;
            }
        }
        public string AnsFile
        {
            get { return this.ansLink.Text; }
            set
            {
                ansLink.Text = value;
            }
        }
        public KRMonitorSelectorPanel() : base()
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
