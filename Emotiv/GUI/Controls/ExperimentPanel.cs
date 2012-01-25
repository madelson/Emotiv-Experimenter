using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MCAEmotiv.GUI.Configurations;
using System.IO;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// A panel for configuring the general experiment settings
    /// </summary>
    public class ExperimentPanel : Panel
    {
        /// <summary>
        /// The current configured settings
        /// </summary>
        public ExperimentSettings ExperimentSettings { get { return this.getExperimentSettings(); } }
        private Func<ExperimentSettings> getExperimentSettings;

        private readonly ToolTip toolTip = new ToolTip();
        private readonly SaveFileDialog saveDialog = new SaveFileDialog()
        {
            Title = "Save experiment settings",
            Filter = "Experiment settings files|*" + ExperimentSettings.EXTENSION,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        private readonly OpenFileDialog openDialog = new OpenFileDialog()
        {
            Title = "Select the saved experiment settings (" + ClassificationScheme.EXTENSION + ") file",
            Filter = "Experiment settings files|*" + ExperimentSettings.EXTENSION,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            Multiselect = true
        };
        private readonly FolderBrowserDialog folderDialog = new FolderBrowserDialog()
        {
            ShowNewFolderButton = false,
            Description = "Select a folder to store experiment logs and raw data",
            SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        /// <summary>
        /// Construct a control with default initial settings
        /// </summary>
        public ExperimentPanel() : base() { this.BuildView(); }

        /// <summary>
        /// Disposes the control
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.toolTip.Dispose();
                this.saveDialog.Dispose();
                this.openDialog.Dispose();
                this.folderDialog.Dispose();
            }
            base.Dispose(disposing);
        }

        #region ---- Build View ----
        private void BuildView()
        {
            // config panel
            var config = ConfigurationPanel.Create<ExperimentSettings>();

            // output folder
            var outputLabel = "Data Output Folder".ToLabel(DockStyle.Bottom);
            var outputLink = new LinkLabel() { Text = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), Dock = DockStyle.Bottom };
            outputLink.Click += (sender, args) =>
            {
                if (this.folderDialog.ShowDialog() != DialogResult.OK)
                    return;

                outputLink.Text = this.folderDialog.SelectedPath;
            };
            this.getExperimentSettings = () =>
            {
                var settings = (ExperimentSettings)config.GetConfiguredObject();
                settings.OutputFolder = outputLink.Text;

                return settings;
            };

            // button table
            var buttonTable = GUIUtils.CreateButtonTable(Direction.Horizontal, DockStyle.Bottom,
            GUIUtils.CreateFlatButton("Save", b =>
            {
                var settings = this.ExperimentSettings;
                this.saveDialog.FileName = string.IsNullOrWhiteSpace(settings.ExperimentName) ? "my experiment" : settings.ExperimentName;
                if (this.saveDialog.ShowDialog() != DialogResult.OK)
                    return;

                bool saved = settings.TrySerializeToFile(this.saveDialog.FileName);
                GUIUtils.Alert((saved ? "Saved" : "Failed to save")
                    + " experiment info to " + this.saveDialog.FileName,
                    (saved ? MessageBoxIcon.Information : MessageBoxIcon.Error));

                string directory = Path.GetDirectoryName(this.saveDialog.FileName);
                if (Directory.Exists(directory))
                    this.saveDialog.InitialDirectory = directory;
            }, this.toolTip, "Save experiment configuration information"),
            GUIUtils.CreateFlatButton("Load", b =>
            {
                if (this.openDialog.ShowDialog() != DialogResult.OK)
                    return;

                ExperimentSettings settings;
                foreach (var path in this.openDialog.FileNames)
                {
                    if (Utils.TryDeserializeFile(this.openDialog.FileName, out settings))
                        config.SetConfiguredObject(settings);
                    else
                        GUIUtils.Alert("Failed to load experiment info from " + path, MessageBoxIcon.Error);
                }
            }, this.toolTip, "Load a previously saved experiment settings file"));

            // add all controls
            this.Controls.Add(config);
            this.Controls.Add(outputLabel);
            this.Controls.Add(outputLink);
            this.Controls.Add(buttonTable);
        }
        #endregion
    }
}
