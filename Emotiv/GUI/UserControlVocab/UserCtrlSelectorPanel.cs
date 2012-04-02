using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.UserControlVocab
{
    /// <summary>
    /// The selector panel for files in the User-Controlled Vocab Software
    /// </summary>
    public class UserCtrlSelectorPanel : Panel 
    {
        private readonly LinkLabel presentationLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom, };
        private readonly LinkLabel testLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
        private readonly LinkLabel ansLink = new LinkLabel() { Text = "Please Select a File", Dock = DockStyle.Bottom };
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
        /// The file from which stimuli for the initial study phase are read
        /// </summary>
        public string PresentationFile { get { return this.presentationLink.Text; } set {
            presentationLink.Text = value;
        }
        }
        /// <summary>
        /// The file from which the quiz questions are read
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
        /// The file from which the answers are read
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
        /// The selector panel for files for the User Controlled Vocab software
        /// </summary>
        public UserCtrlSelectorPanel() : base()
        {
            var presentationLabel = "Presentation Stimuli".ToLabel(DockStyle.Bottom);
            var testLabel = "Test Stimuli".ToLabel(DockStyle.Bottom);
            var ansLabel = "Answers".ToLabel(DockStyle.Bottom);
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
