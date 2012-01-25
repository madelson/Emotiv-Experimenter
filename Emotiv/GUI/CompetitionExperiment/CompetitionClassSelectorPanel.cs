using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.CompetitionExperiment
{
    public class CompetitionClassSelectorPanel : Panel 
    {
        private readonly LinkLabel presentationLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom, };
        private readonly LinkLabel class1Link = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
        private readonly LinkLabel class2Link = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
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
        public string Class1File
        {
            get { return this.class1Link.Text; }
            set
            {
                class1Link.Text = value;
            }
        }
        public string Class2File
        {
            get { return this.class2Link.Text; }
            set
            {
                class2Link.Text = value;
            }
        }

        public CompetitionClassSelectorPanel() : base()
        {
            var presentationLabel = "Presentation Stimuli".ToLabel(DockStyle.Bottom);
            var class1Label = "Class 1 Stimuli".ToLabel(DockStyle.Bottom);
            var class2Label = "Class 2 Stimuli".ToLabel(DockStyle.Bottom);

            
            presentationLink.Click += (sender, args) =>
                {
                    if (this.openDialog.ShowDialog() != DialogResult.OK)
                        return;
                    presentationLink.Text = this.openDialog.FileName;
                };
            this.Controls.Add(presentationLabel);
            this.Controls.Add(presentationLink);

            class1Link.Click += (sender, args) =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;
                class1Link.Text = this.openDialog.FileName;
            };
            this.Controls.Add(class1Label);
            this.Controls.Add(class1Link);

            class2Link.Click += (sender, args) =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;
                class2Link.Text = this.openDialog.FileName;
            };
            this.Controls.Add(class2Label);
            this.Controls.Add(class2Link);
        }
    }
}
