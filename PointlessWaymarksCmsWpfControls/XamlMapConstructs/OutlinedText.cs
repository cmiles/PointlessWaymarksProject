using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace PointlessWaymarksCmsWpfControls.XamlMapConstructs
{
    public class OutlinedText : FrameworkElement
    {
        public static readonly DependencyProperty BackgroundProperty = TextBlock.BackgroundProperty.AddOwner(
            typeof(OutlinedText),
            new FrameworkPropertyMetadata(Brushes.White, (o, e) => ((OutlinedText) o)._glyphRun = null)
            {
                AffectsMeasure = true
            });

        public static readonly DependencyProperty FontFamilyProperty = TextBlock.FontFamilyProperty.AddOwner(
            typeof(OutlinedText),
            new FrameworkPropertyMetadata((o, e) => ((OutlinedText) o)._glyphRun = null) {AffectsMeasure = true});

        public static readonly DependencyProperty FontSizeProperty = TextBlock.FontSizeProperty.AddOwner(
            typeof(OutlinedText),
            new FrameworkPropertyMetadata((o, e) => ((OutlinedText) o)._glyphRun = null) {AffectsMeasure = true});

        public static readonly DependencyProperty FontStretchProperty = TextBlock.FontStretchProperty.AddOwner(
            typeof(OutlinedText),
            new FrameworkPropertyMetadata((o, e) => ((OutlinedText) o)._glyphRun = null) {AffectsMeasure = true});

        public static readonly DependencyProperty FontStyleProperty = TextBlock.FontStyleProperty.AddOwner(
            typeof(OutlinedText),
            new FrameworkPropertyMetadata((o, e) => ((OutlinedText) o)._glyphRun = null) {AffectsMeasure = true});

        public static readonly DependencyProperty FontWeightProperty = TextBlock.FontWeightProperty.AddOwner(
            typeof(OutlinedText),
            new FrameworkPropertyMetadata((o, e) => ((OutlinedText) o)._glyphRun = null) {AffectsMeasure = true});

        public static readonly DependencyProperty ForegroundProperty = TextBlock.ForegroundProperty.AddOwner(
            typeof(OutlinedText),
            new FrameworkPropertyMetadata((o, e) => ((OutlinedText) o)._glyphRun = null) {AffectsMeasure = true});

        public static readonly DependencyProperty OutlineThicknessProperty = DependencyProperty.Register(
            "OutlineThickness", typeof(double), typeof(OutlinedText),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsMeasure,
                (o, e) => ((OutlinedText) o)._glyphRun = null));

        public static readonly DependencyProperty TextProperty = TextBlock.TextProperty.AddOwner(typeof(OutlinedText),
            new FrameworkPropertyMetadata((o, e) => ((OutlinedText) o)._glyphRun = null) {AffectsMeasure = true});

        private GlyphRun _glyphRun;
        private Geometry _outline;

        public Brush Background
        {
            get => (Brush) GetValue(BackgroundProperty);
            set => SetValue(BackgroundProperty, value);
        }

        public FontFamily FontFamily
        {
            get => (FontFamily) GetValue(FontFamilyProperty);
            set => SetValue(FontFamilyProperty, value);
        }

        public double FontSize
        {
            get => (double) GetValue(FontSizeProperty);
            set => SetValue(FontSizeProperty, value);
        }

        public FontStretch FontStretch
        {
            get => (FontStretch) GetValue(FontStretchProperty);
            set => SetValue(FontStretchProperty, value);
        }

        public FontStyle FontStyle
        {
            get => (FontStyle) GetValue(FontStyleProperty);
            set => SetValue(FontStyleProperty, value);
        }

        public FontWeight FontWeight
        {
            get => (FontWeight) GetValue(FontWeightProperty);
            set => SetValue(FontWeightProperty, value);
        }

        public Brush Foreground
        {
            get => (Brush) GetValue(ForegroundProperty);
            set => SetValue(ForegroundProperty, value);
        }

        public double OutlineThickness
        {
            get => (double) GetValue(OutlineThicknessProperty);
            set => SetValue(OutlineThicknessProperty, value);
        }

        public string Text
        {
            get => (string) GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            return CheckGlyphRun() ? _outline.Bounds.Size : new Size();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            if (!CheckGlyphRun()) return;

            var location = _outline.Bounds.Location;
            drawingContext.PushTransform(new TranslateTransform(-location.X, -location.Y));
            drawingContext.DrawGeometry(Background, null, _outline);
            drawingContext.DrawGlyphRun(Foreground, _glyphRun);
        }

        private bool CheckGlyphRun()
        {
            if (_glyphRun != null) return true;

            if (string.IsNullOrEmpty(Text)) return false;

            var typeface = new Typeface(FontFamily, FontStyle, FontWeight, FontStretch);

            if (!typeface.TryGetGlyphTypeface(out var glyphTypeface)) return false;

            var glyphIndices = new ushort[Text.Length];
            var advanceWidths = new double[Text.Length];

            for (var i = 0; i < Text.Length; i++)
            {
                var glyphIndex = glyphTypeface.CharacterToGlyphMap[Text[i]];
                glyphIndices[i] = glyphIndex;
                advanceWidths[i] = glyphTypeface.AdvanceWidths[glyphIndex] * FontSize;
            }

            _glyphRun = new GlyphRun(glyphTypeface, 0, false, FontSize, 1f, glyphIndices, new Point(), advanceWidths,
                null, null, null, null, null, null);

            _outline = _glyphRun.BuildGeometry().GetWidenedPathGeometry(new Pen(null, OutlineThickness * 2d));

            return true;
        }
    }
}