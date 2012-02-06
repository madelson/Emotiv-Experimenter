using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using MCAEmotiv.GUI.Animation;

namespace MCAEmotiv.GUI.KRMonitor
{
    class FreeResponseView : MCAEmotiv.GUI.Animation.View
    {
        /// <summary>
        /// A text title to be displayed above the view
        /// </summary>
        public string Text { get; set; }

        public TextBox textBox;

        /// <summary>
        /// Construct a view with the given options
        /// </summary>
        public FreeResponseView()
            : base()
        {
            var textPanel = new Panel { Dock = DockStyle.Fill };
            var submitPanel = new Panel { Dock = DockStyle.Fill };
            System.Drawing.Size txtboxsize = new System.Drawing.Size(400, 400);
            // Create an instance of a TextBox control.
            textBox = new TextBox();
            // textPanel.Controls.Add(textBox);
            //TO DO: Make the textbox centered with less sketchy code
            textBox.TextAlign = HorizontalAlignment.Center;
            textBox.Size = txtboxsize;

            var button = GUIUtils.CreateFlatButton(
                "Submit",
                b => { this.SetResult(textBox.Text); this.Finish(); });


            button.Font = GUIUtils.Constants.DISPLAY_FONT;

            // add the button to its panel
            submitPanel.Controls.Add(button);

            var table = GUIUtils.CreateTable(new[] { .75, .25 }, Direction.Vertical);
            var cols = GUIUtils.CreateTable(new[] { .35, .3, .35 }, Direction.Horizontal);
            
            cols.Controls.Add(textBox, 1, 0);
            
            table.Controls.Add(cols, 0, 0);
            // table.Controls.Add(textPanel, 0, 0);
            table.Controls.Add(submitPanel, 0, 1);

            // when the table is displayed on the screen, give input focus to the textbox
            table.Paint += (s, e) => textBox.Focus();


            // when the view deploys, install its controls
            this.DoOnDeploy(c =>
            {
            
                c.Controls.Add(table);
            
            });
        }

        /// <summary>
        /// Construct a view with the given options and with inline retrieval of the result.
        /// </summary>
        public FreeResponseView(out IViewResult result)
        {
            result = this.Result;
        }
    }
}

