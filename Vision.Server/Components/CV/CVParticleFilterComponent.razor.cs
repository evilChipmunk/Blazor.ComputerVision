 
using Imaging2; 
using Microsoft.JSInterop;
using OpenCvSharp;
using Vision.Server.Services;

namespace Vision.Server.Components.CV
{
    public partial class CVParticleFilterComponent
    {
        private ParticleProcessor particleProcessor;
        private ParticleModelType selectedModel;
        private Scaler filterScaler;   

        public ParticleModelType SelectedModel
        {
            get => selectedModel;
            set
            {
                selectedModel = value;
                StateHasChanged();
            }
        }

        protected override DataSetType SelectedDataset
        {
            get => base.SelectedDataset;
            set
            {
                base.SelectedDataset = value;

                switch (base.SelectedDataset)
                {
                    case DataSetType.PresidentHand:
                        Sigma_MSE = 3;
                        Sigma_Dyn = 17;
                        Alpha = .12;
                        break;
                    case DataSetType.Pedestrians:
                        Sigma_Dyn = 1;
                        Sigma_MSE = 4;
                        Alpha = .12;
                        break;
                }

                InvokeAsync(async () => await ResetOperation());
                StateHasChanged();
            }
        }

        public int Particles
        {
            get => particleProcessor.Particles;
            set
            {
                particleProcessor.Particles = value;
                particleProcessor.AlterFilter(SelectedModel);
            }
        }

        public double Sigma_MSE
        {
            get => particleProcessor.Sigma_MSE;
            set
            {
                particleProcessor.Sigma_MSE = value;
                particleProcessor.AlterFilter(SelectedModel);
            }
        }

        public float Sigma_Dyn
        {
            get => particleProcessor.Sigma_Dyn;
            set
            {
                particleProcessor.Sigma_Dyn = value;
                particleProcessor.AlterFilter(SelectedModel);
            }
        }

        public double AlphaBinding
        {
            get => Alpha * 100;
            set => Alpha = value / 100;
        }

        private double Alpha
        {
            get => particleProcessor.Alpha;
            set
            {
                particleProcessor.Alpha = value;
                particleProcessor.AlterFilter(SelectedModel);
            }
        }

        protected override Task OnInitializedAsync()
        {
            ShowTemplate = true;
            filterScaler = new Scaler();
            filterScaler.ScaleValue = 100;
            filterScaler.OnScaleValueChanged += StateHasChanged;
            particleProcessor = new ParticleProcessor(FileService, filterScaler);
            SelectedModel = ParticleModelType.Appearance; 

            Particles = 400;
            Sigma_MSE = 3;
            Sigma_Dyn = 17;
            Alpha = .12;

            return base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {

            if (firstRender)
            {
                // Initialize position and dimension variables 
                //var templateParams = new InitialTemplateParams
                //{
                //    TemplateRect = new Rectangle(),
                //    ImageRect = new Rectangle
                //    {
                //        h = srcMat.Height,
                //        w = srcMat.Cols
                //    }
                //};

                //// Initialize Scalers
                //CreateScalers(initialScaleValue, templateParams);
            }

            await base.OnAfterRenderAsync(firstRender);
        }

        [JSInvokable]
        public async Task OnSelectionMade(int x, int y, int width, int height)
        {
            //the cursor could have been dragged in any direction so we need to account for that,
            //it will be negative for width/height if the cursor was dragged to the left or up  
            var templateParams = new InitialTemplateParams
            {
                TemplateRect = new Rectangle
                {
                    x = Math.Min(x, x + width),
                    y = Math.Min(y, y + height),
                    h = Math.Abs(height),
                    w = Math.Abs(width),
                },
                ImageRect = new Rectangle
                {
                    h = SrcMat.Height,
                    w = SrcMat.Width,
                }
            };
             
            DisplayScaler.InitialTemplateWidth = templateParams.TemplateRect.w;
            DisplayScaler.InitialTemplateHeight = templateParams.TemplateRect.h;
            DisplayScaler.InitialX = templateParams.TemplateRect.x;
            DisplayScaler.InitialY = templateParams.TemplateRect.y;
            DisplayScaler.ScaleTemplate();
            StateHasChanged();

            filterScaler.InitialImageWidth = templateParams.ImageRect.w;
            filterScaler.InitialImageHeight = templateParams.ImageRect.h;
            filterScaler.InitialTemplateWidth = templateParams.TemplateRect.w;
            filterScaler.InitialTemplateHeight = templateParams.TemplateRect.h;
            filterScaler.InitialX = templateParams.TemplateRect.x;
            filterScaler.InitialY = templateParams.TemplateRect.y;
            filterScaler.Scale();

            var template = particleProcessor.ExtractTemplate(SrcMat, templateParams.TemplateRect);
            await SetTemplate(template);

            StateHasChanged();

            await ResetOperation();
            // await ProcessImage();
        }
 
        protected override async Task Process(Mat startFrameMat, Mat endFrameMat)
        { 
            var (filterTemplate, viewableFrame) = await particleProcessor.ProcessImage(startFrameMat, selectedModel);
            await SetTemplate(filterTemplate);
            await SetDstImage(viewableFrame);
        } 
        protected override async Task ResetOperation()
        {
            await base.ResetOperation();
            particleProcessor.DestroyFilter(); 
        }  
    }
}