using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MCAEmotiv.GUI.Configurations;
using MCAEmotiv.Classification;
using System.Windows.Forms;
using System.IO;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// A custom tab for configuring a classifier and feature selection scheme
    /// </summary>
    public class ClassificationSchemeTab : CustomTab
    {
        /// <summary>
        /// The current configured scheme
        /// </summary>
        public ClassificationScheme ClassificationScheme
        {
            get { return new ClassificationScheme() { Classifier = this.getClassifier(), Settings = this.getSettings() }; }
        }

        private readonly SaveFileDialog saveDialog = new SaveFileDialog()
        {
            Title = "Save classifier settings",
            Filter = "Classifier settings files|*" + ClassificationScheme.EXTENSION,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        private Func<IClassifier> getClassifier;
        private Func<GeneralClassifierSettings> getSettings;

        /// <summary>
        /// Construct a tab set to the specified scheme
        /// </summary>
        public ClassificationSchemeTab(ClassificationScheme classificationScheme)
            : base()
        {
            this.BuildView(classificationScheme);
        }

        /// <summary>
        /// Returns the text property of this tab
        /// </summary>
        public override string ToString()
        {
            return this.Text;
        }

        /// <summary>
        /// Disposes of this tab
        /// </summary>
        protected override void DisposeOfManagedResources()
        {
            this.saveDialog.Dispose();
        }

        #region ---- Build View ----
        private class TimeBin
        {
            public int Index { get; private set; }
            public int BinWidth { get; set; }
            public bool Checked { get; set; }

            public TimeBin(int index)
            {
                this.Index = index;
            }

            public override string ToString()
            {
                int start = this.Index * this.BinWidth;
                return string.Format("Bin {0}: {1} ms - {2} ms", this.Index, start, start + this.BinWidth);
            }
        }

        private void BuildView(ClassificationScheme classificationScheme)
        {
            this.SuspendLayout();
            this.Text = classificationScheme.Settings.Name;
            var table = GUIUtils.CreateTable(new double[] { .33, .33, .33 }, Direction.Horizontal);

            // classifier
            var classifierSettings = new DerivedTypeConfigurationPanel(typeof(IClassifier), classificationScheme.Classifier);
            this.getClassifier = () => (IClassifier)classifierSettings.GetConfiguredObject();
            table.Controls.Add(classifierSettings, 0, 0);

            // general settings
            var generalSettings = new ConfigurationPanel(classificationScheme.Settings);
            table.Controls.Add(generalSettings, 1, 0);

            // bin selection
            var panel = new Panel() { Dock = DockStyle.Fill };

            var binList = new CheckedListBox() { Dock = DockStyle.Fill, CheckOnClick = true };
            binList.AddContextMenu();
            this.ToolTip.SetToolTip(binList, "Select which time bins from each trial will be used to train the classifier");
            var timeBins = GeneralClassifierSettings.MAX_BINS
                .CountTo()
                .Select(i => new TimeBin(i) { Checked = classificationScheme.Settings.SelectedBins.Contains(i) })
                .ToIArray();
            binList.ItemCheck += (sender, args) => ((TimeBin)binList.Items[args.Index]).Checked = (args.NewValue == CheckState.Checked);
            Action<int> refreshBinList = (binWidth) =>
            {
                // ensure the right number of items
                int binCount = GeneralClassifierSettings.GetBinCount(binWidth);
                if (binList.Items.Count < binCount)
                    binList.Items.AddRange(timeBins.SubView(binList.Items.Count, binCount - binList.Items.Count).ToArray());
                else
                    for (int i = binList.Items.Count - 1; i >= binCount; i--)
                        binList.Items.RemoveAt(i);

                // ensure correct width and uncheck all
                TimeBin timeBin;
                for (int i = 0; i < binCount; i++)
                {
                    timeBin = (TimeBin)binList.Items[i];
                    timeBin.BinWidth = binWidth;
                    binList.SetItemChecked(i, timeBin.Checked);
                }

                binList.Invalidate();
            };
            refreshBinList(classificationScheme.Settings.BinWidthMillis);
            var binWidthProp = typeof(GeneralClassifierSettings).GetProperty("BinWidthMillis");
            var nameProp = typeof(GeneralClassifierSettings).GetProperty("Name");
            if (binWidthProp == null || nameProp == null)
                throw new Exception("Failed to find properties!");
            generalSettings.PropertyChanged += args =>
            {
                if (args.Property.Equals(binWidthProp))
                    refreshBinList((int)args.Getter());
                else if (args.Property.Equals(nameProp))
                    this.Text = args.Getter().ToString();
            };
            this.getSettings = () =>
            {
                var settings = (GeneralClassifierSettings)generalSettings.GetConfiguredObject();
                settings.SelectedBins = binList.CheckedIndices.Cast<int>().ToIArray();

                return settings;
            };
            panel.Controls.Add(binList);
            panel.Controls.Add("Time Bins".ToLabel());
            var saveButton = GUIUtils.CreateFlatButton("Save", (b) =>
            {
                this.saveDialog.FileName = this.Text;
                if (this.saveDialog.ShowDialog() != DialogResult.OK)
                    return;

                bool saved = this.ClassificationScheme.TrySerializeToFile(this.saveDialog.FileName);
                GUIUtils.Alert((saved ? "Saved" : "Failed to save")
                    + " classifier info to " + this.saveDialog.FileName,
                    (saved ? MessageBoxIcon.Information : MessageBoxIcon.Error));  
              
                string directory = Path.GetDirectoryName(this.saveDialog.FileName);
                if (Directory.Exists(directory))
                    this.saveDialog.InitialDirectory = directory;
            });
            saveButton.Dock = DockStyle.Bottom;
            panel.Controls.Add(saveButton);
            table.Controls.Add(panel, 2, 0);

            this.Controls.Add(table);
            this.ResumeLayout(false);
        }
        #endregion
    }
}
