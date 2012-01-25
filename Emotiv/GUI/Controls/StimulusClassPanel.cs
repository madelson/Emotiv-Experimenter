using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MCAEmotiv.GUI.Configurations;
using System.IO;
using MCAEmotiv.Interop;
using System.Drawing;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// A control for creating and configuring stimulus classes
    /// </summary>
    public class StimulusClassPanel : Panel
    {
        /// <summary>
        /// The first selected stimulus class (null if none are selected)
        /// </summary>
        public StimulusClass StimulusClass1 { get; private set; }
        
        /// <summary>
        /// The second selected stimulus class (null if only one class is selected)
        /// </summary>
        public StimulusClass StimulusClass2 { get; private set; }

        /// <summary>
        /// The current image display settings
        /// </summary>
        public ImageDisplaySettings ImageDisplaySettings { get; private set; }

        private readonly List<StimulusClassTab> stimulusClassTabs = new List<StimulusClassTab>();
        private readonly FolderBrowserDialog folderDialog = new FolderBrowserDialog()
        {
            ShowNewFolderButton = false,
            Description = "Select the folder containing the stimuli",
            SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };
        private readonly OpenFileDialog fileDialog = new OpenFileDialog()
        {
            Title = "Select the saved stimulus class settings (" + StimulusClass.EXTENSION + ") file",
            Filter = "Stimulus class settings files|*" + StimulusClass.EXTENSION,
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        /// <summary>
        /// Construct the basic view
        /// </summary>
        public StimulusClassPanel()
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
                this.folderDialog.Dispose();
                this.fileDialog.Dispose();
            }
            base.Dispose(disposing);
        }

        #region ---- Build View ----
        private void BuildView()
        {
            this.SuspendLayout();
            this.StimulusClass1 = this.StimulusClass2 = null;

            // tab control
            var tabs = new CustomTabControl() { Dock = DockStyle.Fill };
            tabs.DisplayStyleProvider = new TabStyleVisualStudioProvider(tabs) { ShowTabCloser = true };
            tabs.TabClosing += (sender, args) => ((CustomTab)args.TabPage).RaiseClosingSafe(args);

            // start tab
            var startTab = new CustomTab() { Text = "Classes" };

            // columns
            var cols = GUIUtils.CreateTable(new double[] { .33, .33, .33 }, Direction.Horizontal);

            // image config
            var imageConfig = ConfigurationPanel.Create<ImageDisplaySettings>();

            // image panel
            var imagePanel = new ImagePanel() { Dock = DockStyle.Fill, UseNativeSize = false };
            bool cycle = true;
            var rand = new Random();
            Func<StimulusClass, string> getImageForClass = stimulusClass =>
            {
                var tab = this.stimulusClassTabs.First(t => t.StimulusClass == stimulusClass);
                if (tab.StimulusClass.Stimuli.Count == 0)
                    return null;
                if (!((ImageDisplaySettings)imageConfig.GetConfiguredObject()).CycleThroughImages)
                    return (tab.SelectedStimulus ?? tab.StimulusClass.Stimuli.First()).PathOrText;
                return tab.StimulusClass.Stimuli.ElementAt(rand.Next(tab.StimulusClass.Stimuli.Count)).PathOrText;
            };
            Action setImage = () =>
            {
                imagePanel.ImagePath = this.StimulusClass1 == null
                    ? null
                    : getImageForClass(this.StimulusClass1);
                imagePanel.SecondaryImagePath = this.StimulusClass2 == null
                    ? null
                    : getImageForClass(this.StimulusClass2);
            };
            setImage();
            var timer = new Timer() { Interval = 2500, Enabled = true };
            timer.Tick += (sender, args) =>
            {
                // just return if we're not cycling to avoid flicker
                if (!cycle && !timer.Enabled)
                    return;

                // if the form is valid, set a new image
                var activeTextBox = this.FindForm().ActiveControl as TextBox;
                if (activeTextBox == null || activeTextBox.IsValid())
                    setImage();
            };
            Action<ImageDisplaySettings> configurePanel = settings =>
            {
                imagePanel.Configure(settings);
                if (settings.CycleThroughImages != cycle)
                {
                    cycle = settings.CycleThroughImages;
                    setImage();
                }
                this.ImageDisplaySettings = settings;
            };
            configurePanel((ImageDisplaySettings)imageConfig.GetConfiguredObject());
            imageConfig.PropertyChanged += args => configurePanel((ImageDisplaySettings)imageConfig.GetConfiguredObject());

            // class list
            var classList = new CheckedListBox() { Dock = DockStyle.Fill, AllowDrop = true, CheckOnClick = true };
            classList.AddContextMenu();
            ItemCheckEventHandler refreshSelectedClasses = (sender, args) =>
            {
                // get the list of checked indices, including the possibly not-yet-changed item
                List<int> checkedIndices = classList.CheckedIndices.Cast<int>().ToList();
                if (args != null)
                {
                    if (args.NewValue == CheckState.Checked)
                    {
                        checkedIndices.Add(args.Index);
                        checkedIndices.Sort();
                    }
                    else
                        checkedIndices.Remove(args.Index);
                }

                this.StimulusClass1 = this.StimulusClass2 = null;
                if (checkedIndices.Count > 0)
                {
                    this.StimulusClass1 = ((StimulusClassTab)classList.Items[checkedIndices[0]]).StimulusClass;
                    if (checkedIndices.Count > 1)
                        this.StimulusClass2 = ((StimulusClassTab)classList.Items[checkedIndices[1]]).StimulusClass;
                }
                setImage();
            };
            Action<string> addClass = path =>
            {
                StimulusClass stimulusClass;
                if (!StimulusClass.TryLoad(path, out stimulusClass))
                    GUIUtils.Alert("Failed to load stimulus class from " + path, MessageBoxIcon.Error);
                else if (this.stimulusClassTabs
                    .Count(tp => tp.StimulusClass.SourceFolder.Equals(path, StringComparison.OrdinalIgnoreCase)
                        || tp.StimulusClass.SavePath.Equals(path, StringComparison.OrdinalIgnoreCase)) > 0)
                    GUIUtils.Alert("A class from " + path + " is already loaded!", MessageBoxIcon.Exclamation);
                else
                {
                    // get a unique marker unless this was the load of a saved class
                    if (!File.Exists(stimulusClass.SavePath))
                        stimulusClass.Settings.Marker = this.stimulusClassTabs.Count == 0
                            ? 1
                            : this.stimulusClassTabs.Max(s => s.StimulusClass.Settings.Marker) + 1;
                    var classTab = new StimulusClassTab(stimulusClass);
                    classTab.TextChanged += (sender, args) => classList.Invalidate();
                    classTab.Closing += (sender, args) =>
                    {
                        this.stimulusClassTabs.Remove(classTab);
                        classList.Items.Remove(classTab);
                        refreshSelectedClasses(classList, null);
                    };

                    this.stimulusClassTabs.Add(classTab);
                    tabs.TabPages.Add(classTab);
                    classList.Items.Add(classTab, true);
                    refreshSelectedClasses(classList, null);
                }
            };
            classList.ItemCheck += refreshSelectedClasses;
            classList.DragEnter += (sender, args) =>
            {
                if (args.Data.GetDataPresent(DataFormats.FileDrop, false)
                    && ((string[])args.Data.GetData(DataFormats.FileDrop)).Where(StimulusClass.IsValidLoadPath).Count() > 0)
                    args.Effect = DragDropEffects.All;
            };
            classList.DragDrop += (sender, args) =>
            {
                // check that the form is in a valid state
                var activeTextBox = this.FindForm().ActiveControl as TextBox;
                if (activeTextBox != null && !activeTextBox.IsValid())
                {
                    GUIUtils.Alert("All entered data must be valid in order for drag and drop to be enabled", MessageBoxIcon.Error);
                    return;
                }

                string[] data = (string[])args.Data.GetData(DataFormats.FileDrop);

                foreach (string path in data.Where(StimulusClass.IsValidLoadPath))
                    addClass(path);
            };

            // button table
            var buttonTable = GUIUtils.CreateButtonTable(Direction.Horizontal, DockStyle.Bottom,
            GUIUtils.CreateFlatButton("New", b =>
            {
                if (this.folderDialog.ShowDialog() == DialogResult.OK)
                    addClass(this.folderDialog.SelectedPath);
            }, startTab.ToolTip, "Create a new stimulus class from a folder of images"),
            GUIUtils.CreateFlatButton("Load", b =>
            {
                if (this.fileDialog.ShowDialog() == DialogResult.OK)
                    addClass(this.fileDialog.FileName);
            }, startTab.ToolTip, "Load a previously saved stimulus class settings file"));

            startTab.Closing += (sender, args) =>
            {
                args.Cancel = true;
                if (GUIUtils.IsUserSure("Reset stimulus classes?"))
                {
                    this.stimulusClassTabs.Clear();
                    this.Controls.Remove(tabs);
                    tabs.Dispose();
                    timer.Enabled = false;
                    timer.Dispose();
                    this.BuildView();
                    this.OnSizeChanged(EventArgs.Empty);
                }
            };

            // add all controls

            // left column
            var panel = new Panel() { Dock = DockStyle.Fill };
            panel.Controls.Add(classList);
            panel.Controls.Add(buttonTable);
            cols.Controls.Add(panel, 0, 0);

            // middle column
            cols.Controls.Add(imageConfig, 1, 0);

            // right column
            cols.Controls.Add(imagePanel, 2, 0);

            startTab.Controls.Add(cols);
            tabs.Controls.Add(startTab);
            this.Controls.Add(tabs);

            this.ResumeLayout(false);
        }
        #endregion
    }
}
