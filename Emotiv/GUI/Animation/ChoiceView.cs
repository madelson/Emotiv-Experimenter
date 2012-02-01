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
        // contains the "home row" keys which will be used as keyboard shortcuts
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

            // we'll use this tooltip to display mouseover shortcuts for the buttons
            var tooltip = this.RegisterDisposable(new ToolTip());

            // a panel to hold all controls created by the view
            var panel = this.RegisterDisposable(new Panel() { Dock = DockStyle.Fill });
            
            // a table to hold the buttons (TODO change this to use the GUIUtils convenience methods for creating tables)
            var table = new TableLayoutPanel() { Dock = DockStyle.Fill, RowCount = 1, ColumnCount = options.Count };

            // a list of the click-handler functions for each button
            var handlerList = new List<KeyPressEventHandler>();

            // this variable will later be filled in with the current form
            Form form = null;

            // create each button
            for (int i = 0; i < table.ColumnCount; i++)
            {
                // divide the width equally between all buttons
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 1.0f / table.ColumnCount));
                
                // create a button for the option
                var button = GUIUtils.CreateFlatButton(
                    options[i], 
                    // when the button is clicked, set this view's result to the text of the button and finish
                    b => { this.SetResult(b.Text); this.Finish(); }, 
                    tooltip, 
                    // the mouseover text depends on the current index
                    i < HOME_ROW.Length ? "Shortcut: " + HOME_ROW[i] : "No shortcut"
                );
                button.Font = GUIUtils.Constants.DISPLAY_FONT;

                // register a keypad shortcut
                if (i < HOME_ROW.Length)
                {
                    char keyChar = HOME_ROW[i];
                    handlerList.Add((sender, args) =>
                    {
                        // when the key for the button is pressed, fire a button click event 
                        if (args.KeyChar == keyChar)
                            button.PerformClick();
                    });
                }

                // ensures that the buttons don't steal the form's key-presses!
                // unfortunately, both of these "solutions" seem to cause other issues, so they're staying commented out
                //button.KeyPress += (sender, args) => handlerList.ForEach(h => h(form ?? sender, args));
                //button.GotFocus += (sender, args) => button.FindForm().Focus();
                button.KeyPress += handlerList.LastItem();

                // add the button to the table
                table.Controls.Add(button, i, 0);
            }

            /* 
             * This is just here so that we can register the keypad shortcuts at the form level.
             * There's no form available when the view is being constructed, but when it's being displayed
             * we can find one with the FindForm() method. The paint event won't be fired until that point
             */
            table.Paint += (sender, args) =>
            {
                // don't do anything if we've already found a form or can't find one
                if (form != null || (form = table.FindForm()) == null)
                    return;

                // register our key press handlers with the form
                foreach (var handler in handlerList)
                    form.KeyPress += handler;
            };

            // when the view finishes, we need to un-register the key press handlers so they don't affect future views
            this.DoOnFinishing(() => 
            {
                if (form != null)
                    foreach (var handler in handlerList)
                        form.KeyPress -= handler;
            });

            // add the button table to the panel
            panel.Controls.Add(table);

            // create a label to display the view's text, if there is any
            var label = this.RegisterDisposable(new Label() { Dock = DockStyle.Top, TextAlign = System.Drawing.ContentAlignment.MiddleCenter, Font = GUIUtils.Constants.DISPLAY_FONT });

            // when the view deploys, install its controls
            this.DoOnDeploy(c => 
            {
                // if we have text to display, set up the label. We can't do this before deploying because
                // the text property can be changed between calling the view constructor and deploying the
                // view
                if (!string.IsNullOrEmpty(this.Text))
                {
                    label.Text = this.Text;
                    panel.Controls.Add(label);
                }

                // add the panel to the control c
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
