using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.FalseAdapt
{
    /// <summary>
    /// A selector panel for files for the False Adaptive Experiment
    /// </summary>
    public class FalseAdaptSelectorPanel : Panel 
    {
        private readonly LinkLabel presentationLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom, };
        private readonly LinkLabel studyLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom, };
        private readonly LinkLabel compLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom, };
        private readonly LinkLabel class1Link = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
        private readonly LinkLabel class2Link = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
        private readonly OpenFileDialog openDialog = new OpenFileDialog()
        {
            Title = "Load Stimuli",
            Filter = "Text files|*.txt",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = false
        };

        /// <summary>
        /// The file from which practice stimuli are read
        /// </summary>
        public string PresentationFile { get { return this.presentationLink.Text; } set {
            presentationLink.Text = value;
        }
        }

        /// <summary>
        /// The file from which initial study stimuli are read
        /// </summary>
        public string StudyFile
        {
            get { return this.studyLink.Text; }
            set
            {
                studyLink.Text = value;
            }
        }

        /// <summary>
        /// The file from which initial study stimuli are read
        /// </summary>
        public string CompFile
        {
            get { return this.compLink.Text; }
            set
            {
                compLink.Text = value;
            }
        }
        /// <summary>
        /// The file from which the first class (competitive or noncompetitive) is read
        /// </summary>
        public string Class1File
        {
            get { return this.class1Link.Text; }
            set
            {
                class1Link.Text = value;
            }
        }
        /// <summary>
        /// The file from which the second class (competitive or noncompetitive) is read
        /// </summary>
        public string Class2File
        {
            get { return this.class2Link.Text; }
            set
            {
                class2Link.Text = value;
            }
        }
        /// <summary>
        /// The selector panel for files for the competition experiment
        /// </summary>
        public FalseAdaptSelectorPanel() : base()
        {
            var presentationLabel = "Presentation Stimuli".ToLabel(DockStyle.Bottom);
            var studyLabel = "Study Stimuli".ToLabel(DockStyle.Bottom);
            var compLabel = "Competition Stimuli".ToLabel(DockStyle.Bottom);
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

            studyLink.Click += (sender, args) =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;
                studyLink.Text = this.openDialog.FileName;
            };
            this.Controls.Add(studyLabel);
            this.Controls.Add(studyLink);

            compLink.Click += (sender, args) =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;
                compLink.Text = this.openDialog.FileName;
            };
            this.Controls.Add(compLabel);
            this.Controls.Add(compLink);

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
