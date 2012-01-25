using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using MCAEmotiv.GUI.Configurations;
using MCAEmotiv.GUI.Animation;

namespace MCAEmotiv.GUI.Controls
{
    class StimulusClassTab : CustomTab
    {
        #region ---- Stimulus Item ----
        private class StimulusItem
        {
            public Stimulus Stimulus { get; private set; }
            private readonly StimulusClass stimulusClass;

            public StimulusItem(StimulusClass stimulusClass, Stimulus stimulus)
            {
                this.stimulusClass = stimulusClass;
                this.Stimulus = stimulus;
            }

            public override string ToString()
            {
                var name = GUIUtils.Strings.ImageExtensions.Contains(Path.GetExtension(this.Stimulus.PathOrText))
                    ? Path.GetFileNameWithoutExtension(this.Stimulus.PathOrText)
                    : this.Stimulus.PathOrText;
                if (string.IsNullOrWhiteSpace(this.stimulusClass.Settings.Answer1)
                    && string.IsNullOrWhiteSpace(this.stimulusClass.Settings.Answer2))
                    return name;
                else if (this.Stimulus.Subclass == null)
                    return name + " \t(" + GUIUtils.Strings.UNCLASSIFIED + ")";
                else if ((bool)this.Stimulus.Subclass)
                    return name + " \t(" + this.stimulusClass.Settings.Answer1 + ")";
                else
                    return name + " \t(" + this.stimulusClass.Settings.Answer2 + ")";
            }
        }
        #endregion

        public StimulusClass StimulusClass { get; private set; }

        public Stimulus SelectedStimulus { get { return this.getSelectedStimulus(); } }
        private Func<Stimulus> getSelectedStimulus;

        public StimulusClassTab(StimulusClass stimulusClass)
            : base()
        {
            this.StimulusClass = stimulusClass;
            this.BuildView();
        }

        public override string ToString()
        {
            return this.StimulusClass.Settings.Name;
        }

        #region ---- View ----
        private void BuildView()
        {
            this.SuspendLayout();
            this.Text = this.StimulusClass.Settings.Name;
            var cols = GUIUtils.CreateTable(new double[] { .33, .33, .33 }, Direction.Horizontal);

            // settings
            var settingsConfig = new ConfigurationPanel(this.StimulusClass.Settings) { Dock = DockStyle.Fill };

            // image panel
            var imagePanel = new ImagePanel() { Dock = DockStyle.Fill };

            // dropdown
            var dropDown = new ComboBox() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Bottom };
            dropDown.MouseWheel += (sender, args) => ((HandledMouseEventArgs)args).Handled = true;
            dropDown.Items.Add(new DisplayPointer(() => this.StimulusClass.Settings.Answer1, true));
            dropDown.Items.Add(new DisplayPointer(() => this.StimulusClass.Settings.Answer2, false));
            dropDown.Items.Add(new DisplayPointer(GUIUtils.Strings.UNCLASSIFIED, null));

            // source folder
            var sourceFolderLink = new LinkLabel() { AutoSize = true, Text = Path.GetFileName(this.StimulusClass.SourceFolder), Dock = DockStyle.Top };
            sourceFolderLink.Click += (sender, args) =>
            {
                try { System.Diagnostics.Process.Start("explorer.exe", this.StimulusClass.SourceFolder); }
                catch (Exception) { GUIUtils.Alert("Failed to open " + this.StimulusClass.SourceFolder); }
            };
            this.ToolTip.SetToolTip(sourceFolderLink, "Open " + this.StimulusClass.SourceFolder);

            // image list
            var imageList = new CheckedListBox() { Dock = DockStyle.Fill };
            imageList.AddContextMenu();
            foreach (var stimulus in this.StimulusClass.Stimuli)
                imageList.Items.Add(new StimulusItem(this.StimulusClass, stimulus), stimulus.Used);
            EventHandler setImage = (sender, args) =>
            {
                if (imageList.Items.Count > 0)
                {
                    var stimulus = ((StimulusItem)(imageList.SelectedItem ?? imageList.Items[0])).Stimulus;
                    imagePanel.ImagePath = stimulus.PathOrText;
                    dropDown.Visible = true;
                    switch (stimulus.Subclass)
                    {
                        case true: dropDown.SelectedIndex = 0; break;
                        case false: dropDown.SelectedIndex = 1; break;
                        case null: dropDown.SelectedIndex = 2; break;
                    }
                }
                else
                {
                    imagePanel.ImagePath = null;
                    dropDown.Visible = false;
                }
            };
            setImage(imageList, EventArgs.Empty); // first set
            imageList.SelectedIndexChanged += setImage;
            settingsConfig.PropertyChanged += args =>
            {
                this.StimulusClass.Settings.SetProperty(args.Property, args.Getter());
                this.Text = this.StimulusClass.Settings.Name;
                imageList.Invalidate();

                // force a refresh
                int selectedIndex = dropDown.SelectedIndex;
                var items = dropDown.Items.Cast<DisplayPointer>().ToArray();
                dropDown.Items.Clear();
                dropDown.Items.AddRange(items);
                dropDown.SelectedIndex = selectedIndex;
            };
            imageList.ItemCheck += (sender, args) => ((StimulusItem)imageList.Items[args.Index]).Stimulus.Used = (args.NewValue == CheckState.Checked);
            dropDown.SelectedIndexChanged += (sender, args) =>
            {
                ((StimulusItem)(imageList.SelectedItem ?? imageList.Items[0])).Stimulus.Subclass =
                    (bool?)((DisplayPointer)dropDown.SelectedItem).Key;
                imageList.Invalidate();
            };
            this.getSelectedStimulus = () => imageList.SelectedItem == null ? null : ((StimulusItem)imageList.SelectedItem).Stimulus;

            // selection info label
            var selectionInfoLabel = new Label() { Dock = DockStyle.Bottom, AutoSize = true };
            PaintEventHandler updateSelectionInfoLabel = (sender, args) =>
            {
                var items = imageList.Items.Cast<StimulusItem>();
                selectionInfoLabel.Text = string.Format("{0}/{1} selected", items.Count(s => s.Stimulus.Used), imageList.Items.Count);
                if (!string.IsNullOrWhiteSpace(this.StimulusClass.Settings.Answer1)
                    || !string.IsNullOrWhiteSpace(this.StimulusClass.Settings.Answer2))
                    selectionInfoLabel.Text += string.Format(" ({0}/{1} {2}, {3}/{4} {5}, {6}/{7} {8})", items.Count(s => s.Stimulus.Subclass == true && s.Stimulus.Used),
                        items.Count(s => s.Stimulus.Subclass == true),
                        this.StimulusClass.Settings.Answer1,
                        items.Count(s => s.Stimulus.Subclass == false && s.Stimulus.Used),
                        items.Count(s => s.Stimulus.Subclass == false),
                        this.StimulusClass.Settings.Answer2,
                        items.Count(s => s.Stimulus.Subclass == null && s.Stimulus.Used),
                        items.Count(s => s.Stimulus.Subclass == null),
                        GUIUtils.Strings.UNCLASSIFIED);
            };
            imageList.Paint += updateSelectionInfoLabel;
            updateSelectionInfoLabel(null, null);

            // button table
            var buttonTable = GUIUtils.CreateButtonTable(Direction.Horizontal, DockStyle.Bottom,
            GUIUtils.CreateFlatButton("Classify", b =>
            {
                MainForm.Instance.Animate(new StimulusClassSetupProvider(this.StimulusClass), this.Invalidate);
            }, this.ToolTip, "Launch a tool to quickly answer this class's question for all stimuli"),
            GUIUtils.CreateFlatButton("Refresh", b =>
            {
                this.StimulusClass.RefreshStimuli();
                this.Invalidate();
            }, this.ToolTip, "Reload the stimuli from the file system"),
            GUIUtils.CreateFlatButton("Save", b =>
            {
                bool saved = this.StimulusClass.TrySave();
                GUIUtils.Alert((saved ? "Saved" : "Failed to save")
                    + " stimulus class info to " + this.StimulusClass.SavePath,
                    (saved ? MessageBoxIcon.Information : MessageBoxIcon.Error));
            }, this.ToolTip, "Save configuration information to " + this.StimulusClass.SavePath));

            // add all controls
            Panel panel;

            // left column
            panel = new Panel() { Dock = DockStyle.Fill };
            panel.Controls.Add(settingsConfig);
            panel.Controls.Add(sourceFolderLink);
            panel.Controls.Add("Folder".ToLabel());
            panel.Controls.Add(buttonTable);
            cols.Controls.Add(panel, 0, 0);

            // middle column
            panel = new Panel() { Dock = DockStyle.Fill };
            panel.Controls.Add(imageList);
            panel.Controls.Add(selectionInfoLabel);
            panel.Controls.Add("Stimuli".ToLabel());
            cols.Controls.Add(panel, 1, 0);

            // right column
            panel = new Panel() { Dock = DockStyle.Fill };
            panel.Controls.Add(imagePanel);
            panel.Controls.Add(dropDown);
            cols.Controls.Add(panel, 2, 0);

            this.Controls.Add(cols);
            this.ResumeLayout(false);
        }
        #endregion
    }
}
