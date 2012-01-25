using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace MCAEmotiv.GUI
{
    /// <summary>
    /// Specifies a direction for a drawn arrow
    /// </summary>
    public enum ArrowType 
    { 
        /// <summary>
        /// The arrow points left
        /// </summary>
        Left, 
        
        /// <summary>
        /// The arrow points right
        /// </summary>
        Right, 

        /// <summary>
        /// The arrow is bidirectional
        /// </summary>
        Bidi 
    };

    /// <summary>
    /// Contains useful GUI-related extension methods
    /// </summary>
    public static class Extensions
    {
        /// <summary>
        /// Invokes the action on the GUI thread if necessary.
        /// </summary>
        public static void GUIInvoke(this Action action)
        {
            GUIUtils.GUIInvoker.InvokeSafe(action);
        }

        /// <summary>
        /// OnComplete will be invoked on the GUI thread when condition is true. 
        /// This function need not be called from the GUI thread.
        /// </summary>
        public static void AsyncWaitOnGUIThread(this Func<bool> condition, Action onComplete)
        {
            new Action(() => condition.AsyncWaitOnGUIThreadHelper(onComplete)).GUIInvoke();
        }

        private static void AsyncWaitOnGUIThreadHelper(this Func<bool> condition, Action onComplete)
        {
            if (condition())
                onComplete();
            else
            {
                Thread.Yield();
                GUIUtils.GUIInvoker.BeginInvoke(new Action(() => condition.AsyncWaitOnGUIThreadHelper(onComplete)));
            }
        }

        /// <summary>
        /// The point at the center of the rectangle.
        /// </summary>
        public static Point Center(this Rectangle rectangle)
        {
            return new Point(rectangle.X + rectangle.Width / 2, rectangle.Y + rectangle.Height / 2);
        }

        /// <summary>
        /// Returns the point where this is centered around the location.
        /// </summary>
        public static Point CenteredAround(this Size size, Point location)
        {
            return new Point(location.X - (size.Width / 2), location.Y - (size.Height / 2));
        }

        /// <summary>
        /// If size fits inside the containing size, the original size is returned. Otherwise,
        /// a size is returned that is as large as possible, fits within the container, 
        /// and has the same aspect ratio as the original size.
        /// </summary>
        public static Size ConstrainedTo(this Size size, Size container)
        {
            var newSize = size;
            if (newSize.Width > container.Width)
            {
                newSize.Width = container.Width;
                newSize.Height = (size.Height * container.Width) / size.Width;
            }
            if (newSize.Height > container.Height)
            {
                newSize.Height = container.Height;
                newSize.Width = (size.Width * container.Height) / size.Height;
            }

            return newSize;
        }

        private const int CROSS_RADIUS = 30;
        private static readonly Pen crossPen = new Pen(Color.Black, 0.1f);

        /// <summary>
        /// Draws a fixation cross centered at location
        /// </summary>
        public static void DrawFixationCross(this Graphics graphics, Point center)
        {
            graphics.DrawLine(crossPen, center.X - CROSS_RADIUS, center.Y, center.X + CROSS_RADIUS, center.Y);
            graphics.DrawLine(crossPen, center.X, center.Y - CROSS_RADIUS, center.X, center.Y + CROSS_RADIUS);
        }

        /// <summary>
        /// Draws an arrow in the specified rectangle with the specified direction. Ratio is used on recursive calls
        /// withing this function to adjust the sizing of bidirectional arrows
        /// </summary>
        public static void DrawArrow(this Graphics graphics, Rectangle area, ArrowType arrowType, double ratio = 1.0 / 3.0)
        {
            if (arrowType == ArrowType.Bidi)
            {
                var halfSize = new Size(area.Width / 2, area.Height);
                graphics.DrawArrow(new Rectangle(area.Location, halfSize), ArrowType.Left, 2 * ratio);
                graphics.DrawArrow(new Rectangle(new Point(area.Center().X, area.Location.Y), halfSize), ArrowType.Right, 2 * ratio);
                return;
            }
   
            var oldMode = graphics.SmoothingMode;
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            float bodyHeight = area.Height / 3;
            float headWidth = (area.Width * ratio).Rounded();
            float bodyWidth = area.Width - headWidth;

            PointF[] points = 
            {
                new PointF(area.Left, area.Top + bodyHeight),
                new PointF(area.Left + bodyWidth, area.Top + bodyHeight),
                new PointF(area.Left + bodyWidth, area.Top),
                new PointF(area.Right, area.Top + (area.Height / 2)),
                new PointF(area.Left + bodyWidth, area.Bottom),
                new PointF(area.Left + bodyWidth, area.Bottom - bodyHeight),
                new PointF(area.Left, area.Bottom - bodyHeight),
            };

            if (arrowType == ArrowType.Left)
            {
                PointF center = area.Center();
                for (int i = 0; i < points.Length; i++)
                    points[i].X += (2 * (center.X - points[i].X));
            }

            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddPolygon(points);
                graphics.FillPath(Brushes.Black, path);
                graphics.DrawPath(crossPen, path);
            }

            graphics.SmoothingMode = oldMode;
        }

        /// <summary>
        /// Creates a label from text. The default DockStyle is set for usage in a top-down FlowLayoutPanel.
        /// </summary>
        public static Label ToLabel(this string text, DockStyle dockStyle = DockStyle.Top, ContentAlignment align = ContentAlignment.TopLeft, bool autoSize = true)
        {
            return new Label() { AutoSize = autoSize, Dock = dockStyle, Text = text, TextAlign = align };
        }

        /// <summary>
        /// If Image.FromFile succeeds and the result is not null, returns true and assigns the loaded
        /// value to image. Otherwise, image is set to null and false is returned.
        /// </summary>
        public static bool TryLoadImage(this string path, out Image image)
        {
            image = null;
            try { image = Image.FromFile(path); }
            catch (Exception) { }

            return image != null;
        }

        #region ---- Validation ----
        private static readonly Dictionary<TextBox, Func<string, bool>> validationFunctions = new Dictionary<TextBox, Func<string, bool>>();

        /// <summary>
        /// Adds fancy validation logic using isValid to determine the validity of the textbox's content.
        /// The onValidTextEntered function, if not null, is called from the TextChanged event only if the new text was valid.
        /// </summary>
        public static void EnableValidation(this TextBox textBox, Func<string, bool> isValid, Action onValidTextEntered = null)
        {
            Color defaultFore = textBox.ForeColor, defaultBack = textBox.BackColor;

            textBox.TextChanged += (sender, args) =>
            {
                if (isValid(textBox.Text))
                {
                    textBox.ForeColor = defaultFore;
                    if (onValidTextEntered != null)
                        onValidTextEntered();
                }
                else
                    textBox.ForeColor = Color.Red;
            };

            textBox.CausesValidation = true;
            textBox.Validating += (sender, args) =>
            {
                if (args.Cancel = !isValid(textBox.Text))
                {
                    textBox.BackColor = Color.Black;
                    textBox.Refresh();
                    Thread.Sleep(100);
                    textBox.BackColor = defaultBack;
                }
            };

            validationFunctions.Add(textBox, isValid);
            textBox.Disposed += (sender, args) => validationFunctions.Remove(textBox);
        }

        /// <summary>
        /// Returns true iff there is no validation function installed for the text box
        /// or if the current text value is declared valid by the installed validation function
        /// </summary>
        public static bool IsValid(this TextBox textBox)
        {
            return !validationFunctions.ContainsKey(textBox)
                || validationFunctions[textBox](textBox.Text);
        }
        #endregion

        /// <summary>
        /// Reverses the tab order of the control's children.
        /// </summary>
        public static void ReverseTabOrder(this Control control)
        {
            var children = new Control[control.Controls.Count];
            control.Controls.CopyTo(children, 0);

            int i = control.Controls.Count;
            foreach (var child in children.OrderBy(c => c.TabIndex))
                child.TabIndex = --i;

        }

        /// <summary>
        /// Adds a useful context menu to a checked list box
        /// </summary>
        public static void AddContextMenu(this CheckedListBox clb)
        {
            var menu = new ContextMenu();
            menu.MenuItems.Add(new MenuItem("Check all", (sender, args) =>
            {
                for (int i = 0; i < clb.Items.Count; i++)
                    clb.SetItemChecked(i, true);
            }));
            menu.MenuItems.Add(new MenuItem("Uncheck all", (sender, args) =>
            {
                for (int i = 0; i < clb.Items.Count; i++)
                    clb.SetItemChecked(i, false);
            }));

            clb.ContextMenu = menu;
        }
    }
}
