using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace Comet.Launcher.Controls
{
    [Description("Color Progress Bar")]
    [ToolboxBitmap(typeof(ProgressBar))]
    [Designer(typeof(ColorProgressBarDesigner))]
    public class ColorProgressBar : Control
    {
        //
        // set default values
        //
        private long mValue;
        private long mMinimum;
        private long mMaximum = 100;
        private long mStep = 10;

        private FillStyles mFillStyle = FillStyles.Solid;

        private Color mBarColor = Color.FromArgb(255, 128, 128);
        private Color mBorderColor = Color.Black;

        public enum FillStyles
        {
            Solid,
            Dashed
        }

        public ColorProgressBar()
        {
            Size = new Size(150, 15);
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.ResizeRedraw |
                ControlStyles.DoubleBuffer,
                true
            );
        }

        [Description("ColorProgressBar color")]
        [Category("ColorProgressBar")]
        public Color BarColor
        {
            get => mBarColor;
            set
            {
                mBarColor = value;
                Invalidate();
            }
        }

        [Description("ColorProgressBar fill style")]
        [Category("ColorProgressBar")]
        public FillStyles FillStyle
        {
            get => mFillStyle;
            set
            {
                mFillStyle = value;
                Invalidate();
            }
        }

        [Description("The current value for the ColorProgressBar, " +
                     "in the range specified by the Minimum and Maximum properties.")]
        [Category("ColorProgressBar")]
        // the rest of the Properties windows must be updated when this peroperty is changed.
        [RefreshProperties(RefreshProperties.All)]
        public long Value
        {
            get => mValue;
            set
            {
                if (value < mMinimum)
                {
                    throw new ArgumentException("'" + value + "' is not a valid value for 'Value'.\n" +
                                                "'Value' must be between 'Minimum' and 'Maximum'.");
                }

                if (value > mMaximum)
                {
                    throw new ArgumentException("'" + value + "' is not a valid value for 'Value'.\n" +
                                                "'Value' must be between 'Minimum' and 'Maximum'.");
                }

                mValue = value;
                Invalidate();
            }
        }

        [Description("The lower bound of the range this ColorProgressbar is working with.")]
        [Category("ColorProgressBar")]
        [RefreshProperties(RefreshProperties.All)]
        public long Minimum
        {
            get => mMinimum;
            set
            {
                mMinimum = value;

                if (mMinimum > mMaximum)
                    mMaximum = mMinimum;
                if (mMinimum > mValue)
                    mValue = mMinimum;

                Invalidate();
            }
        }

        [Description("The uppper bound of the range this ColorProgressbar is working with.")]
        [Category("ColorProgressBar")]
        [RefreshProperties(RefreshProperties.All)]
        public long Maximum
        {
            get => mMaximum;
            set
            {
                mMaximum = value;

                if (mMaximum < mValue)
                    mValue = mMaximum;
                if (mMaximum < mMinimum)
                    mMinimum = mMaximum;

                Invalidate();
            }
        }

        [Description("The amount to jump the current value of the control by when the Step() method is called.")]
        [Category("ColorProgressBar")]
        public long Step
        {
            get => mStep;
            set
            {
                mStep = value;
                Invalidate();
            }
        }

        [Description("The border color of ColorProgressBar")]
        [Category("ColorProgressBar")]
        public Color BorderColor
        {
            get => mBorderColor;
            set
            {
                mBorderColor = value;
                Invalidate();
            }
        }

        //
        // Call the PerformStep() method to increase the value displayed by the amount set in the Step property
        //
        public void PerformStep()
        {
            if (mValue < mMaximum)
                mValue += mStep;
            else
                mValue = mMaximum;

            Invalidate();
        }

        //
        // Call the PerformStepBack() method to decrease the value displayed by the amount set in the Step property
        //
        public void PerformStepBack()
        {
            if (mValue > mMinimum)
                mValue -= mStep;
            else
                mValue = mMinimum;

            Invalidate();
        }

        //
        // Call the Increment() method to increase the value displayed by an integer you specify
        // 
        public void Increment(int value)
        {
            if (mValue < mMaximum)
                mValue += value;
            else
                mValue = mMaximum;

            Invalidate();
        }

        //
        // Call the Decrement() method to decrease the value displayed by an integer you specify
        // 
        public void Decrement(int value)
        {
            if (mValue > mMinimum)
                mValue -= value;
            else
                mValue = mMinimum;

            Invalidate();
        }
        
        protected override void OnPaint(PaintEventArgs e)
        {
            //
            // Calculate matching colors
            //
            Color darkColor = ControlPaint.Dark(mBarColor);
            Color bgColor = ControlPaint.Dark(mBarColor);

            //
            // Fill background
            //
            var bgBrush = new SolidBrush(bgColor);
            e.Graphics.FillRectangle(bgBrush, ClientRectangle);
            bgBrush.Dispose();

            // 
            // Check for value
            //
            if (mMaximum == mMinimum || mValue == 0)
            {
                // Draw border only and exit;
                DrawBorder(e.Graphics);
                return;
            }

            //
            // The following is the width of the bar. This will vary with each value.
            //
            int fillWidth = (int) (Width * mValue / (mMaximum - mMinimum));

            //
            // GDI+ doesn't like rectangles 0px wide or high
            //
            if (fillWidth == 0)
            {
                // Draw border only and exti;
                DrawBorder(e.Graphics);
                return;
            }

            //
            // Rectangles for upper and lower half of bar
            //
            var topRect = new Rectangle(0, 0, fillWidth, Height / 2);
            var buttonRect = new Rectangle(0, Height / 2, fillWidth, Height / 2);

            //
            // The gradient brush
            //

            //
            // Paint upper half
            //
            var brush = new LinearGradientBrush(new Point(0, 0),
                                                new Point(0, Height / 2), darkColor, mBarColor);
            e.Graphics.FillRectangle(brush, topRect);
            brush.Dispose();

            //
            // Paint lower half
            // (this.Height/2 - 1 because there would be a dark line in the middle of the bar)
            //
            brush = new LinearGradientBrush(new Point(0, Height / 2 - 1),
                                            new Point(0, Height), mBarColor, darkColor);
            e.Graphics.FillRectangle(brush, buttonRect);
            brush.Dispose();

            //
            // Calculate separator's setting
            //
            var sepWidth = (int) (Height * .67);
            int sepCount = fillWidth / sepWidth;
            Color sepColor = ControlPaint.LightLight(mBarColor);

            //
            // Paint separators
            //
            switch (mFillStyle)
            {
                case FillStyles.Dashed:
                    // Draw each separator line
                    for (var i = 1; i <= sepCount; i++)
                    {
                        e.Graphics.DrawLine(new Pen(sepColor, 1),
                                            sepWidth * i, 0, sepWidth * i, Height);
                    }

                    break;

                case FillStyles.Solid:
                    // Draw nothing
                    break;
            }

            //
            // Draw border and exit
            //
            DrawBorder(e.Graphics);
        }

        //
        // Draw border
        //
        protected void DrawBorder(Graphics g)
        {
            var borderRect = new Rectangle(0, 0,
                                           ClientRectangle.Width - 1, ClientRectangle.Height - 1);
            g.DrawRectangle(new Pen(mBorderColor, 1), borderRect);
        }
    }
}