namespace Vision.Server.Services
{
    public class Scaler
    {
        public int InitialImageWidth { get; set; }
        public int InitialImageHeight { get; set; }
        public int InitialTemplateWidth { get; set; }
        public int InitialTemplateHeight { get; set; }
        public int InitialX { get; set; }
        public int InitialY { get; set; }

        public event Action OnScaleValueChanged;

        private int scaleValue;
        public int ScaleValue
        {
            get => scaleValue;
            set
            {
                if (scaleValue != value)
                {
                    scaleValue = value;
                    Scale();
                    OnScaleValueChanged?.Invoke(); 
                }
            }
        }

        public int ImageWidth { get; private set; }
        public int ImageHeight { get; private set; }
        public int TemplateWidth { get; private set; }
        public int TemplateHeight { get; private set; }
        public int X { get; private set; }
        public int Y { get; private set; }


        public void Reset(int width, int height)
        {
            InitialImageWidth = width;
            InitialImageHeight = height;
            scaleValue = 100;
            Scale();
        }

        public void Scale()
        {
            ImageWidth = ScaleDimension(InitialImageWidth, ScaleValue);
            ImageHeight = ScaleDimension(InitialImageHeight, ScaleValue);
            TemplateWidth = ScaleDimension(InitialTemplateWidth, ScaleValue);
            TemplateHeight = ScaleDimension(InitialTemplateHeight, ScaleValue);
            X = ScaleDimension(InitialX, ScaleValue);
            Y = ScaleDimension(InitialY, ScaleValue);
        }


        public void ScaleTemplate()
        { 
            TemplateWidth = ScaleDimension(InitialTemplateWidth, ScaleValue);
            TemplateHeight = ScaleDimension(InitialTemplateHeight, ScaleValue);
            X = ScaleDimension(InitialX, ScaleValue);
            Y = ScaleDimension(InitialY, ScaleValue);
        }

        private int ScaleDimension(int initialValue, int scaleValue)
        {
            return (int)Math.Floor((double)(initialValue * scaleValue / 100));
        }
    }
}