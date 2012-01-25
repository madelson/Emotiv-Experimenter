using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCAEmotiv.GUI.Animation
{
    /// <summary>
    /// Presents the user with buttons to choose between options.
    /// The result is the string corresponding with the chosen option.
    /// </summary>
    public class ChoiceView : View
    {
        private const string HOME_ROW = "asdfghjkl;";

        /// <summary>
        /// A text title to be displayed above the view
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Construct a view with the given options
        /// </summary>
        public ChoiceView(IEnumerable<string> optionsStrings)
            : base()
        {
            var options = optionsStrings.ToIArray();
            var tooltip = this.RegisterDisposable(new ToolTip());
            var panel = this.RegisterDisposable(new Panel() { Dock = DockStyle.Fill });
            var table = new TableLayoutPanel() { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = options.Count };
            var handlerList = new List<KeyPressEventHandler>();
            Form form = null;
            for (int i = 0; i < table.ColumnCount; i++)
            {
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1.0f / table.ColumnCount));
                var button = GUIUtils.CreateFlatButton(options[i], 
                    b => { this.SetResult(b.Text); this.Finish(); }, 
                    tooltip, i < HOME_ROW.Length ? "Shortcut: " + HOME_ROW[i] : "No shortcut");
                button.Font = GUIUtils.Constants.DISPLAY_FONT;
                if (i < HOME_ROW.Length)
                {
                    char keyChar = HOME_ROW[i];
                    handlerList.Add((sender, args) =>
                    {
                        if (args.KeyChar == keyChar)
                            button.PerformClick();
                    });
                }

                // ensures that the buttons don't steal the form's key-presses!
                // unfortunately, both of these "solutions" seem to cause other issues, so they're staying commented out
                //button.KeyPress += (sender, args) => handlerList.ForEach(h => h(form ?? sender, args));
                //button.GotFocus += (sender, args) => button.FindForm().Focus();
                button.KeyPress += handlerList.LastItem();

                table.Controls.Add(button, i, 0);
            }

            table.Paint += (sender, args) =>
            {
                if (form != null || (form = table.FindForm()) == null)
                    return;
                foreach (var handler in handlerList)
                    form.KeyPress += handler;
            };

            this.DoOnFinishing(() => 
            {
                if (form != null)
                    foreach (var handler in handlerList)
                        form.KeyPress -= handler;
            });

            panel.Controls.Add(table);
            var label = this.RegisterDisposable(new Label() { Dock = DockStyle.Top, TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Font = GUIUtils.Constants.DISPLAY_FONT });

            this.DoOnDeploy(c => 
            {
                if (!string.IsNullOrEmpty(this.Text))
                {
                    label.Text = this.Text;
                    panel.Controls.Add(label);
                }

                c.Controls.Add(panel);
            });
        }

        /// <summary>
        /// Construct a view with the given options and with inline retrieval of the result.
        /// </summary>
        public ChoiceView(IEnumerable<string> optionsStrings, out IViewResult result)
            : this(optionsStrings)
        {
            result = this.Result;
        }
    }
}
