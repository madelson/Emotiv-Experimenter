using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using MCAEmotiv.GUI.Controls;
using System.Drawing;

namespace MCAEmotiv.GUI
{
    /// <summary>
    /// An enum for horizontal and vertical directions
    /// </summary>
    public enum Direction {
        /// <summary>
        /// Horizontal
        /// </summary>
        Horizontal,

        /// <summary>
        /// Vertical
        /// </summary>
        Vertical,
    }

    /// <summary>
    /// Contains static GUI-related utility methods
    /// </summary>
    public static class GUIUtils
    {
        #region ---- Inner Classes ----
        /// <summary>
        /// Contains string constants
        /// </summary>
        public static class Strings
        {
            private static readonly IArrayView<string> imageExtensions = new string[] { ".bmp", ".jpg", ".jpeg", ".gif", ".png", ".tiff" }.ToIArray();
            /// <summary>
            /// A collectin of image extension strings
            /// </summary>
            public static IArrayView<string> ImageExtensions { get { return imageExtensions; } }

            /// <summary>
            /// APP_NAME is the name of the application.
            /// UNCLASSIFIED is the string used to represent stimuli with no subclass.
            /// DONT_KNOW is the option string for user invalidation of a trial
            /// LOGGING_TITLE_FORMAT is a format string for a new section header in a log file
            /// CSV_EXTENSION is the comma separated value format extension
            /// TEXT_EXTENSION is the text file format extension
            /// </summary>
            public const string APP_NAME = "Experimenter",
                UNCLASSIFIED = "unclassified",
                DONT_KNOW = "I don't know (invalidate trial)",
                LOGGING_TITLE_FORMAT = "////////////////////////////// {0} //////////////////////////////", 
                CSV_EXTENSION = ".csv",
                TEXT_EXTENSION = ".txt";
        }

        /// <summary>
        /// Contains non-string constants
        /// </summary>
        public static class Constants
        {
            /// <summary>
            /// A slightly larger font used during experiments for readability
            /// </summary>
            public static readonly Font DISPLAY_FONT = new Font(Control.DefaultFont.FontFamily, 14f);

            /// <summary>
            /// A much larger font used during experiments for emphasis
            /// </summary>
            public static readonly Font DISPLAY_FONT_LARGE = new Font(DISPLAY_FONT.FontFamily, DISPLAY_FONT.Size * 2);

            /// <summary>
            /// The maximum size of a button table
            /// </summary>
            public static readonly Size MAX_BUTTON_TABLE_SIZE = new Size(int.MaxValue, 40);
        }

        private class SafeGUIInvoker : ISynchronizeInvoke
        {
            private static readonly SafeGUIInvoker instance = new SafeGUIInvoker();
            public static SafeGUIInvoker Instance { get { return instance; } }
   
            private readonly Control innerInvoker = MainForm.Instance;
            private volatile bool invokerIsReady = false;

            private SafeGUIInvoker() { }

            private ISynchronizeInvoke GetInnerInvoker()
            {
                if (this.invokerIsReady)
                    return this.innerInvoker;
                
                while (!this.invokerIsReady && !this.innerInvoker.IsHandleCreated)
                    System.Threading.Thread.Sleep(10);

                this.invokerIsReady = true;
                return this.innerInvoker;
            }

            public IAsyncResult BeginInvoke(Delegate method, object[] args)
            {
                try { return this.GetInnerInvoker().BeginInvoke(method, args); }
                catch (Exception) { return null; }
            }

            public object EndInvoke(IAsyncResult result)
            {
                try { return this.GetInnerInvoker().EndInvoke(result); }
                catch (Exception) { return null; }
            }

            public object Invoke(Delegate method, object[] args)
            {
                try { return this.GetInnerInvoker().Invoke(method, args); }
                catch (Exception) { return null; }
            }

            public bool InvokeRequired
            {
                get { return this.GetInnerInvoker().InvokeRequired; }
            }
        }
        #endregion

        /// <summary>
        /// Returns an ISynchronizeInvoke for the GUI thread
        /// </summary>
        public static ISynchronizeInvoke GUIInvoker { get { return SafeGUIInvoker.Instance; } }

        /// <summary>
        /// Creates a table with the specified proportions and dock style
        /// </summary>
        public static TableLayoutPanel CreateTable(IEnumerable<double> rowProportions, IEnumerable<double> columnProportions, DockStyle dockStyle = DockStyle.Fill)
        {
            double rowSum = rowProportions.Sum(), colSum = columnProportions.Sum();
            IArrayView<float> rowArray = rowProportions.Select(d => (float)(d / rowSum)).ToIArray(),
                colArray = columnProportions.Select(d => (float)(d / colSum)).ToIArray();

            var table = new TableLayoutPanel() { Dock = dockStyle, RowCount = rowArray.Count, ColumnCount = colArray.Count };
            foreach (float f in rowArray)
                table.RowStyles.Add(new RowStyle(SizeType.Percent, f));
            foreach (float f in colArray)
                table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, f));

            return table;
        }

        /// <summary>
        /// Creates a 1 x n (or n x 1) table with the specified proportions, direction, and dock style
        /// </summary>
        public static TableLayoutPanel CreateTable(IEnumerable<double> proportions, Direction direction, DockStyle dockStyle = DockStyle.Fill)
        {
            return direction == Direction.Vertical
                ? CreateTable(proportions, (1.0).Enumerate(), dockStyle)
                : CreateTable((1.0).Enumerate(), proportions, dockStyle);
        }

        /// <summary>
        /// Creates a table of buttons with the specified direction and dock style
        /// </summary>
        public static TableLayoutPanel CreateButtonTable(Direction direction, DockStyle dockStyle, params Button[] buttons)
        {
            var table = CreateTable((1.0).NCopies(buttons.Length), direction, dockStyle);
            for (int i = 0; i < buttons.Length; i++)
                if (direction == Direction.Horizontal)
                    table.Controls.Add(buttons[i], i, 0);
                else
                    table.Controls.Add(buttons[i], 0, i);

            table.MaximumSize = Constants.MAX_BUTTON_TABLE_SIZE;
            return table;
        }

        /// <summary>
        /// Creates a button which has been properly stylized for the application GUI
        /// </summary>
        public static Button CreateFlatButton(string text, Action<Button> clickHandler = null, ToolTip toolTip = null, string toolTipText = null)
        {
            var button = new Button() { Dock = DockStyle.Fill, Text = text, FlatStyle = FlatStyle.Flat, Margin = new Padding(0) };
            button.FlatAppearance.BorderSize = 0;

            if (clickHandler != null)
                button.Click += (sender, args) => clickHandler(button);

            if (toolTip != null && toolTipText != null)
                toolTip.SetToolTip(button, toolTipText);
            
            return button;
        }

        /// <summary>
        /// Returns true if the user responded to the question in the affirmative
        /// </summary>
        public static bool IsUserSure(string question)
        {
            return MessageBox.Show(question, Strings.APP_NAME + ": Are you sure?", MessageBoxButtons.YesNo) == DialogResult.Yes;
        }

        /// <summary>
        /// Alerts the user of the issue
        /// </summary>
        public static void Alert(string issue, MessageBoxIcon messageType = MessageBoxIcon.Information)
        {
            MessageBox.Show(issue, Strings.APP_NAME + ": Info", MessageBoxButtons.OK, messageType);
        }

        private const int MIN_BORDER = 50;

        /// <summary>
        /// Retrieves the rectangles in which images should be placed in side-by-side mode
        /// </summary>
        public static void GetSplitModeImageRectangles(Rectangle clientRectangle, Size maxImageSize, out Rectangle rectangle1, out Rectangle rectangle2)
        {
            var halfRectangle = new Rectangle(clientRectangle.X, clientRectangle.Y, clientRectangle.Width / 2, clientRectangle.Height);
            var imageSize = maxImageSize.ConstrainedTo(halfRectangle.Size);
            var center1 = halfRectangle.Center();
            var center2 = new Point(center1.X + halfRectangle.Width, center1.Y);

            rectangle1 = new Rectangle(imageSize.CenteredAround(center1), imageSize);            
            rectangle2 = new Rectangle(imageSize.CenteredAround(center2), imageSize);
            if (rectangle1.X - clientRectangle.X > MIN_BORDER)
            {
                int shift = rectangle1.X - clientRectangle.X - MIN_BORDER;
                rectangle1.X -= shift;
                rectangle2.X += shift;
            }
        }
    }
}
