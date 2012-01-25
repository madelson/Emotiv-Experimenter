using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using MCAEmotiv.GUI.Configurations;

namespace MCAEmotiv.GUI.Controls
{
    /// <summary>
    /// A panel which can display an image or two images superimposed
    /// </summary>
    class ImagePanel : Panel
    {
        private Image image = null;
        private bool imageIsText = false;
        private string imagePath = null;
        /// <summary>
        /// The image file path of the primary image, or arbitrary text to be displayed
        /// </summary>
        public string ImagePath
        {
            get { return this.imagePath; }
            set { this.loadImage(this.imagePath = value, ref this.image, out this.imageIsText); }
        }

        private bool secondaryImageIsText = false;
        private Image secondaryImage = null;
        private string secondaryImagePath = null;
        /// <summary>
        /// The image file path of the secondary image, or arbitrary text to be displayed
        /// </summary>
        public string SecondaryImagePath
        {
            get { return this.secondaryImagePath; }
            set { this.loadImage(this.secondaryImagePath = value, ref this.secondaryImage, out this.secondaryImageIsText); }
        }

        /// <summary>
        /// Did all image files load successfully?
        /// </summary>
        public bool ImagesLoaded
        {
            get 
            { 
                return (this.imagePath == null || this.imageIsText || this.image != null) 
                    && (this.secondaryImagePath == null || this.secondaryImageIsText || this.secondaryImage != null); 
            }
        }

        private int alpha;
        /// <summary>
        /// The alpha (transparency) value for superimposed images
        /// </summary>
        public int Alpha { get { return this.alpha; } set { this.alpha = value; this.Invalidate(); } }

        private Size maxImageSize;
        /// <summary>
        /// The maximum allowed size of either displayed image
        /// </summary>
        public Size MaxImageSize
        {
            get { return this.maxImageSize; }
            set
            {
                this.maxImageSize = value;
                if (!this.UseNativeSize)
                    this.Invalidate();
            }
        }

        private bool useNativeSize;
        /// <summary>
        /// If set to true, the MaxImageSize property is ignored in favor of the primary image's native size.
        /// Defaults to true.
        /// </summary>
        public bool UseNativeSize { get { return this.useNativeSize; } set { this.useNativeSize = value; this.Invalidate(); } }

        private bool useGrayscale;
        /// <summary>
        /// Should the images be displayed in grayscale?
        /// </summary>
        public bool UseGrayscale { get { return this.useGrayscale; } set { this.useGrayscale = value; this.Invalidate(); } }

        private bool superimposeImages;
        /// <summary>
        /// Should the images be superimposed?
        /// </summary>
        public bool SuperimposeImages { get { return this.superimposeImages; } set { this.superimposeImages = value; this.Invalidate(); } }

        /// <summary>
        /// Construct a control with default settings
        /// </summary>
        public ImagePanel()
            : base()
        {
            // double buffer to prevent flickering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.ResizeRedraw = true;
            this.Alpha = 0;
            this.MaxImageSize = this.ClientSize;
            this.UseNativeSize = true;
            this.UseGrayscale = false;
            this.SuperimposeImages = true;
            this.Paint += new PaintEventHandler(this.PaintImages);
        }

        /// <summary>
        /// Configure the control with the given settings
        /// </summary>
        public void Configure(ImageDisplaySettings settings)
        {
            this.Alpha = settings.Alpha;
            this.MaxImageSize = settings.ImageSize;
            this.UseNativeSize = false;
            this.SuperimposeImages = settings.SuperimposeImages;
            this.UseGrayscale = settings.UseGrayscale;
        }

        private void loadImage(string newPath, ref Image im, out bool isText)
        {
            if (im != null)
                im.Dispose();
            if (newPath == null)
            {
                im = null;
                isText = false;
            }
            else if (GUIUtils.Strings.ImageExtensions.Contains(Path.GetExtension(newPath)))
            {
                newPath.TryLoadImage(out im);
                isText = false;
            }
            else
            {
                im = null;
                isText = true;
            }
            this.Invalidate();
        }

        private void PaintImages(object sender, PaintEventArgs e)
        {
            // check primary image
            if (this.image == null && !this.imageIsText)
            {
                // represents a failed load
                if (!string.IsNullOrWhiteSpace(this.ImagePath))
                    this.DrawErrorString(this.ImagePath, e.Graphics);

                // otherwise draw nothing

                return;
            }

            // check secondary image
            if (this.secondaryImage == null && !this.secondaryImageIsText && !string.IsNullOrWhiteSpace(this.SecondaryImagePath))
            {
                this.DrawErrorString(this.SecondaryImagePath, e.Graphics);

                return;
            }

            // draw
            using (var attributes = new ImageAttributes())
            {
                var cm = this.UseGrayscale
                    ? new ColorMatrix(new float[][]
                        {
                            new float[] {.3f, .3f, .3f, 0, 0},
                            new float[] {.59f, .59f, .59f, 0, 0},
                            new float[] {.11f, .11f, .11f, 0, 0},
                            new float[] {0, 0, 0, 1, 0},
                            new float[] {0, 0, 0, 0, 1}
                        })
                    : new ColorMatrix();
                attributes.SetColorMatrix(cm);

                Rectangle rectangle1, rectangle2;
                this.GetImageRectangles(out rectangle1, out rectangle2);

                // draw primary image
                if (this.imageIsText)
                {
                    e.Graphics.FillRectangle(Brushes.White, rectangle1);
                    this.DrawText(this.ImagePath, rectangle1, e.Graphics, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                }
                else
                    e.Graphics.DrawImage(this.image, rectangle1,
                        0, 0, this.image.Width, this.image.Height, GraphicsUnit.Pixel, attributes);

                // draw draw secondary image
                if (this.secondaryImageIsText)
                {
                    e.Graphics.FillRectangle(Brushes.White, rectangle2);
                    this.DrawText(this.SecondaryImagePath, rectangle2, e.Graphics, GUIUtils.Constants.DISPLAY_FONT_LARGE);
                }
                else if (this.secondaryImage != null)
                {
                    if (this.SuperimposeImages)
                    {
                        cm.Matrix33 = this.Alpha / (float)255;
                        attributes.SetColorMatrix(cm);
                    }
                    var oldMode = e.Graphics.CompositingMode;
                    e.Graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
                    e.Graphics.DrawImage(this.secondaryImage, rectangle2,
                        0, 0, this.secondaryImage.Width, this.secondaryImage.Height, GraphicsUnit.Pixel, attributes);
                    e.Graphics.CompositingMode = oldMode;
                }
            }
        }

        private void GetImageRectangles(out Rectangle rectangle1, out Rectangle rectangle2)
        {
            // determine the size
            var maxImageSize = this.MaxImageSize;
            if (this.UseNativeSize)
            {
                if (this.image != null)
                    maxImageSize = this.image.Size;
                else if (this.secondaryImage != null)
                    maxImageSize = this.secondaryImage.Size;
                else
                    maxImageSize = this.ClientSize;
            }

            if (this.SuperimposeImages)
            {
                var imageSize = maxImageSize.ConstrainedTo(this.ClientSize);
                rectangle1 = new Rectangle(imageSize.CenteredAround(this.ClientRectangle.Center()), imageSize);
                rectangle2 = rectangle1;
            }
            else
                GUIUtils.GetSplitModeImageRectangles(this.ClientRectangle, maxImageSize, out rectangle1, out rectangle2);
        }

        private void DrawText(string text, Rectangle rectangle, Graphics graphics, Font font)
        {
            RectangleF layoutRectangle = rectangle;
            graphics.DrawString(text,
                font,
                Brushes.Black,
                layoutRectangle,
                new StringFormat()
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center,
                });
        }

        private void DrawErrorString(string path, Graphics graphics)
        {
            this.DrawText("Failed to load image: " + path,
                this.ClientRectangle,
                graphics,
                Control.DefaultFont);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (this.image != null)
                this.image.Dispose();
            if (this.secondaryImage != null)
                this.secondaryImage.Dispose();
        }
    }
}
