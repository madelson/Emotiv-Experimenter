using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MCAEmotiv.GUI.Configurations;
using MCAEmotiv.Interop;
using MCAEmotiv.Analysis;
using System.Threading;
using System.Drawing;
using System.Media;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// A panel which allows classification schemes to be created, and loaded. Also contains an artifact
    /// detection panel
    /// </summary>
    public class ClassificationSchemePanel : Panel
    {
        private Func<IArrayView<ClassificationScheme>> getSelectedClassifiers;
        
        /// <summary>
        /// An array containing the currently selected schemes
        /// </summary>
        public IArrayView<ClassificationScheme> SelectedClassifiers
        {
            get { return this.getSelectedClassifiers(); }
        }

        private readonly ArtifactDetectionSettings artifactDetection = new ArtifactDetectionSettings();
        /// <summary>
        /// The current artifact detection settings
        /// </summary>
        public ArtifactDetectionSettings ArtifactDetectionSettings { get { return this.artifactDetection.GetFactory()(); } } 

        private readonly List<ClassificationSchemeTab> classifierTabs = new List<ClassificationSchemeTab>();
        private readonly OpenFileDialog openDialog = new OpenFileDialog()
        {
            Title = "Select the saved classifier settings (" + ClassificationScheme.EXTENSION + ") file",
            Filter = "Classifier settings files|*" + ClassificationScheme.EXTENSION,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = true
        };

        /// <summary>
        /// Builds the panel view
        /// </summary>
        public ClassificationSchemePanel()
            : base()
        {
            this.BuildView();
        }

        /// <summary>
        /// Disposes the control
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.openDialog.Dispose();
            }
            base.Dispose(disposing);
        }

        #region ---- Build View ----
        private void BuildView()
        {
            this.SuspendLayout();
            var tabs = new CustomTabControl() { Dock = DockStyle.Fill };
            tabs.DisplayStyleProvider = new TabStyleVisualStudioProvider(tabs) { ShowTabCloser = true };
            tabs.TabClosing += (sender, args) => ((CustomTab)args.TabPage).RaiseClosingSafe(args);

            var startTab = new CustomTab() { Text = "Classifiers " }; // the ending space is necessary for some reason
            startTab.Closing += (sender, args) =>
            {
                args.Cancel = true;
                if (GUIUtils.IsUserSure("Reset classifiers?"))
                {
                    this.classifierTabs.Clear();
                    this.Controls.Remove(tabs);
                    tabs.Dispose();
                    this.BuildView();
                    this.OnSizeChanged(EventArgs.Empty);
                }
            };

            // classifier list
            var classifierList = new CheckedListBox() { Dock = DockStyle.Fill, CheckOnClick = true };
            classifierList.AddContextMenu();
            Action<ClassificationScheme> addClassifier = (scheme) =>
            {
                // get unique name if necessary
                string baseName = string.IsNullOrWhiteSpace(scheme.Settings.Name)
                    ? "new classifier"
                    : scheme.Settings.Name;
                if (!this.classifierTabs.Select(ct => ct.Text).Contains(baseName))
                    scheme.Settings.Name = baseName;
                else
                {
                    int i = 1;
                    while (this.classifierTabs
                        .Select(ct => ct.Text.ToLower())
                        .Contains(string.Format("{0} {1}", baseName, i)))
                        i++;
                    scheme.Settings.Name = string.Format("{0} {1}", baseName, i);
                }

                // create the tab
                var classifierTab = new ClassificationSchemeTab(scheme);
                classifierTab.TextChanged += (sender, args) => classifierList.Invalidate();
                classifierTab.Closing += (sender, args) =>
                {
                    this.classifierTabs.Remove(classifierTab);
                    classifierList.Items.Remove(classifierTab);
                };

                this.classifierTabs.Add(classifierTab);
                tabs.TabPages.Add(classifierTab);
                classifierList.Items.Add(classifierTab, true);
            };
            this.getSelectedClassifiers = () => classifierList.CheckedItems.Cast<ClassificationSchemeTab>().Select(cst => cst.ClassificationScheme).ToIArray();

            // buttons
            var buttonTable = GUIUtils.CreateButtonTable(Direction.Horizontal, DockStyle.Bottom,
            GUIUtils.CreateFlatButton("New", (b) =>
            {
                var classifier = classifierList.Items.Count > 0
                    ? ((ClassificationSchemeTab)(classifierList.SelectedItem ?? classifierList.Items[0])).ClassificationScheme
                    : new ClassificationScheme();

                classifier.Settings.Name = string.Empty;
                addClassifier(classifier);
            }, startTab.ToolTip, "Create a new classifier"),
            GUIUtils.CreateFlatButton("Load", (b) =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;

                ClassificationScheme scheme;
                foreach (var path in this.openDialog.FileNames)
                {
                    if (Utils.TryDeserializeFile(this.openDialog.FileName, out scheme))
                        addClassifier(scheme);
                    else
                        GUIUtils.Alert("Failed to load classifier info from " + path, MessageBoxIcon.Error);
                }
            }, startTab.ToolTip, "Load a previously saved classifier settings file"));

            // artifact detection config
            var artifactDetectionPanel = new ConfigurationPanel(this.artifactDetection);
            artifactDetectionPanel.PropertyChanged += args => this.artifactDetection.SetProperty(args.Property, args.Getter());

            // artifact detection label
            var artifactDetectionLabel = new Label() { Dock = DockStyle.Bottom, TextAlign = ContentAlignment.MiddleCenter, Visible = false };
            IEnumerable<EEGDataEntry> empty = new EEGDataEntry[0], entries = empty;
            var listener = new EEGDataListener(GUIUtils.GUIInvoker,
                source => artifactDetectionLabel.Visible = true,
                data =>
                {
                    if (!this.artifactDetection.UseArtifactDetection)
                    {
                        artifactDetectionLabel.Visible = false;
                        entries = empty;
                        return;
                    }

                    artifactDetectionLabel.Visible = true;
                    entries = entries.Concat(data);
                    if (data.LastItem().TimeStamp - entries.First().TimeStamp >= 500)
                    {
                        if (this.artifactDetection.HasMotionArtifact(entries))
                        {
                            artifactDetectionLabel.Text = "Motion artifact detected!";
                            artifactDetectionLabel.BackColor = Color.Red;
                            artifactDetectionLabel.ForeColor = Color.White;
                            if (this.artifactDetection.Beep)
                                GUIUtils.GUIInvoker.BeginInvoke(SystemSounds.Beep.Play);
                        }
                        else
                        {
                            artifactDetectionLabel.Text = "No artifacts detected";
                            artifactDetectionLabel.BackColor = Color.Green;
                            artifactDetectionLabel.ForeColor = Color.Black;
                        }

                        entries = empty;
                    }
                },
                source => artifactDetectionLabel.Visible = false);
            // avoid using the gui invoker before the handle has been created
            this.HandleCreated += (sender, args) => EmotivDataSource.Instance.AddListener(listener);
            artifactDetectionLabel.Disposed += (sender, args) => { EmotivDataSource.Instance.RemoveListener(listener); listener.Dispose(); };

            // right half
            var rightPanel = new Panel() { Dock = DockStyle.Fill };
            rightPanel.Controls.Add(classifierList);
            rightPanel.Controls.Add(buttonTable);

            // left half
            var leftPanel = new Panel() { Dock = DockStyle.Fill };
            leftPanel.Controls.Add(artifactDetectionPanel);
            leftPanel.Controls.Add(artifactDetectionLabel);

            var cols = GUIUtils.CreateTable(new double[] { .5, .5 }, Direction.Horizontal);
            cols.Controls.Add(rightPanel, 0, 0);
            cols.Controls.Add(leftPanel, 1, 0);
            startTab.Controls.Add(cols);

            tabs.TabPages.Add(startTab);
            this.Controls.Add(tabs);
            this.ResumeLayout(false);
        }
        #endregion
    }
}
