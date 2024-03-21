 
using Imaging2;
using ImagingOps;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using OpenCvSharp; 
using Vision.Server.Services;

namespace Vision.Server.Components.CV
{
    public abstract partial class CVComponent
    {
        [Inject] protected IJSRuntime jsRuntime { get; set; }

        [Inject] protected Configuration Configuration { get; set; }

        [Parameter]
        public OperationType Operation { get; set; }

        private DotNetObjectReference<CVComponent> objRef;
        private WebCaptureComponent captureComponent;
        private ElementReference srcCanvas;
        private ElementReference dstCanvas;
        private ElementReference templateCanvas;
        private CanvasClient srcCanvasClient;
        private CanvasClient dstCanvasClient;
        private CanvasClient templateCanvasClient;
        const int templateWidthHeightSize = 255;
        private bool streaming;
        private TaskCompletionSource<bool> imageReadySignal = new(); 
        private Mat dstMat;
        private Mat templateMat;
        private DataSetType selectedDataset;
        private bool isPaused; 
        private int startFrame;
        private int endFrame;
        private int totalFrames;

        protected bool IsTimeSeriesFrame { get; set; } = false;

        protected FileService FileService { get; set; }

        protected Scaler DisplayScaler { get; set; }

        protected bool ShowCanvas { get; set; }

        protected bool ShowTemplate { get; set; }

        protected List<string> Files { get; set; }

        protected int StartFrame
        {
            get => startFrame;
            set
            {
                if (startFrame != value)
                {
                    startFrame = value;

                    InvokeAsync(async () => await ProcessImage());
                }
            }
        }

        protected int EndFrame
        {
            get => endFrame;
            set
            {
                if (endFrame != value)
                {
                    endFrame = value;
                    InvokeAsync(async () => await ProcessImage());
                }
            }
        }

        protected virtual DataSetType SelectedDataset
        {
            get => selectedDataset;
            set
            {
                selectedDataset = value;
                Files = FileService.CreateFiles(selectedDataset);
                startFrame = 0;
                endFrame = 0;
                totalFrames = FileService.Files.Count;

                if (selectedDataset == DataSetType.Stream)
                {
                    InvokeAsync(async () =>
                    {
                        using Mat temp = new Mat(new Size(600, 400), MatType.CV_8UC3);
                        DisplayScaler.Reset(temp.Width, temp.Height);
                        await SetSrcImage(temp);
                    });
                }
                
                if (selectedDataset != DataSetType.None)
                {
                    InvokeAsync(async () =>
                    {
                        var src = await FileService.GetMat(Files.First(), selectedDataset, ImreadModes.Color);
                        DisplayScaler.Reset(src.Width, src.Height);
                        StateHasChanged();
                        await SetSrcImage(src); 
                    });
                } 
                StateHasChanged();
            }
        }

        protected Mat SrcMat { get; private set; }

        protected override Task OnInitializedAsync()
        {
            ShowCanvas = true;
            FileService = new FileService();
            objRef = DotNetObjectReference.Create(this);

            CreateScaler();

            return base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            { 
                srcCanvasClient ??= new CanvasClient(jsRuntime, srcCanvas);
                dstCanvasClient ??= new CanvasClient(jsRuntime, dstCanvas);
                templateCanvasClient ??= new CanvasClient(jsRuntime, templateCanvas); 
                SelectedDataset = DataSetType.Mandrill;
            } 
            await base.OnAfterRenderAsync(firstRender);
        }


        #region Button click handlers and abstract/virtuals

        protected abstract Task Process(Mat startFrame, Mat endFrame);

        protected async Task ProcessImage(Mat streamImage = null, bool stayInPlace = false)
        {
            Mat initialMat;
            Mat lastMat = null;

            if (streamImage != null) //streaming
            {
                initialMat = streamImage;
            }
            else
            {
                initialMat =
                    await FileService.GetMat(FileService.Files[StartFrame], SelectedDataset, ImreadModes.Color);
                lastMat = await FileService.GetMat(FileService.Files[EndFrame], SelectedDataset, ImreadModes.Color);
                await SetSrcImage(initialMat);
            }


            await Process(initialMat, lastMat);


            if (!stayInPlace)
            {
                startFrame++;

                if (StartFrame >= Files.Count)
                {
                    startFrame = 0;
                }

            }

            StateHasChanged();
        }

        protected virtual async Task ProcessImageToEnd()
        {
            isPaused = false;

            foreach (var _ in FileService.Files.ToList())
            {
                if (isPaused)
                {
                    return;
                }

                await ProcessImage();
            }
        }

        protected virtual Task PauseOperation()
        {
            isPaused = true;
            return Task.CompletedTask;
        }

        protected virtual Task ResetOperation()
        {
            startFrame = 0;
            endFrame = 0;
            return Task.CompletedTask;
        }

        #endregion

        #region Canvas image display

        protected async Task SetSrcImage(Mat src, bool overwriteSource = true)
        {
            if (src != null && srcCanvasClient != null)
            {
                if (overwriteSource)
                {
                    SrcMat = await Task.Run(() =>
                        ImageOps.Resize(src, DisplayScaler.ImageWidth, DisplayScaler.ImageHeight));

                    await srcCanvasClient.DrawMatAsync(SrcMat);


                    if (SrcMat != null)
                    {
                        await jsRuntime.InvokeVoidAsync("createSelection.start", "overlayCanvas", objRef, SrcMat.Cols,
                            SrcMat.Rows);
                    } 
                }
                else
                {
                    await srcCanvasClient.DrawMatAsync(src);
                }
            }
        }

        protected async Task SetDstImage(Mat dst, bool overwriteSource = true)
        {
            if (dst != null && dstCanvasClient != null)
            {
                if (overwriteSource)
                {
                    dstMat = await Task.Run(() =>
                        ImageOps.Resize(dst, DisplayScaler.ImageWidth, DisplayScaler.ImageHeight));
                    await dstCanvasClient.DrawMatAsync(dstMat);
                }
                else
                {
                    await dstCanvasClient.DrawMatAsync(dst);
                }
            }
        }
         
        protected async Task SetTemplate(Mat filterTemplate, bool overwriteSource = true)
        {
            if (filterTemplate != null && templateCanvasClient != null)
            {
                if (overwriteSource)
                {
                    try
                    {
                        templateMat = await Task.Run(() =>
                            ImageOps.Resize(filterTemplate, DisplayScaler.TemplateWidth, DisplayScaler.TemplateHeight));

                        templateMat = ResizeImageWithPadding(templateMat);
                        await templateCanvasClient.DrawMatAsync(templateMat);
                    }
                    catch (Exception e)
                    {
                        //there are some edge cases with selection that I'm too lazy 
                        //to track down for this example
                        Console.WriteLine(e); 
                    }
                }
                else
                {
                    filterTemplate = ResizeImageWithPadding(filterTemplate);
                    await templateCanvasClient.DrawMatAsync(filterTemplate);
                }
            }

            StateHasChanged();
        }

        #endregion

        #region Scaling

        private void CreateScaler()
        {
            var initialScaleValue = 100;
            var imageHeight = 256;
            var imageWidth = 256;

            // Initialize position and dimension variables 
            var templateParams = new InitialTemplateParams
            {
                ImageRect = new Rectangle
                {
                    h = imageHeight,
                    w = imageWidth
                }
            };

            DisplayScaler = new Scaler
            {
                InitialImageWidth = templateParams.ImageRect.w,
                InitialImageHeight = templateParams.ImageRect.h,
                ScaleValue = initialScaleValue // Default scale value
            };
            DisplayScaler.OnScaleValueChanged += ScalerChanged;
        }

        public Mat ResizeImageWithPadding(Mat originalImage, int targetWidth = templateWidthHeightSize,
            int targetHeight = templateWidthHeightSize)
        {
            // Calculate the aspect ratio
            double aspectRatio = originalImage.Width / (double)originalImage.Height;
            int newWidth, newHeight;

            // Determine new dimensions within the target size while maintaining aspect ratio
            if (originalImage.Width > originalImage.Height)
            {
                newWidth = targetWidth;
                newHeight = (int)(targetWidth / aspectRatio);
            }
            else
            {
                newHeight = targetHeight;
                newWidth = (int)(targetHeight * aspectRatio);
            }

            // Resize the original image to the new dimensions
            Mat resizedImage = new Mat();
            Cv2.Resize(originalImage, resizedImage, new Size(newWidth, newHeight));

            // Create a black background
            Mat background = Mat.Zeros(targetHeight, targetWidth, originalImage.Type());

            // Calculate the top-left point to start copying the resized image onto the background
            int offsetX = (targetWidth - newWidth) / 2;
            int offsetY = (targetHeight - newHeight) / 2;

            // Copy the resized image onto the center of the background
            resizedImage.CopyTo(background[new Rect(offsetX, offsetY, newWidth, newHeight)]);

            return background;
        }

        private void ScalerChanged()
        {
            InvokeAsync(async () =>
            {
                if (SrcMat != null)
                {
                    var src = await Task.Run(() =>
                        ImageOps.Resize(SrcMat, DisplayScaler.ImageWidth, DisplayScaler.ImageHeight));
                    await SetSrcImage(src, false);
                }
            });

            InvokeAsync(async () =>
            {
                if (templateMat != null)
                {
                    var src = await Task.Run(() =>
                        ImageOps.Resize(templateMat, DisplayScaler.TemplateWidth, DisplayScaler.TemplateHeight));
                    await SetTemplate(src, false);
                }
            });


            InvokeAsync(async () =>
            {
                if (dstMat != null)
                {
                    dstMat = await Task.Run(() =>
                        ImageOps.Resize(dstMat, DisplayScaler.ImageWidth, DisplayScaler.ImageHeight));
                    await SetDstImage(dstMat, false);
                }
            });

            StateHasChanged();
        }


        #endregion
         
        #region Webcam

        private async Task StartCapture()
        {
            Console.WriteLine("started capture");
            streaming = true; 
            FileService.ImageReceived += OnImageReceived;
             
            await captureComponent.StartWebcam();
            Console.WriteLine("web cam started");
        }
         
        private async void OnImageReceived(object sender, WebCamImageArgs args)
        { 
            // Convert the received image data to a Mat directly, assuming imageData is in a format that can be converted to Mat.
            var initialMat = await FileService.GetMat(args.ImageData, ImreadModes.Color);

            // Resize the image if it doesn't match the expected dimensions.
            if (initialMat.Width != DisplayScaler.ImageWidth || initialMat.Height != DisplayScaler.ImageHeight)
            {
                initialMat = await Task.Run(() =>
                    ImageOps.Resize(initialMat, DisplayScaler.ImageWidth, DisplayScaler.ImageHeight));
            }

            // Update the source image for processing.
            await SetSrcImage(initialMat);

            // Signal that a new image is ready for processing.
            imageReadySignal.TrySetResult(true); 
        }
         
        private async Task StopCapture()
        {
            await captureComponent.StopWebcam(); 
            streaming = false;
        } 
         
        private async Task StartCaptureProcess()
        {
            Console.WriteLine("StartCaptureProcess");
            while (streaming)
            {
                await imageReadySignal.Task; // Wait for the signal that a new image is ready 
                await ProcessImage(SrcMat);  
                imageReadySignal = new TaskCompletionSource<bool>(); // Reset the signal
            }
        }

        #endregion
    } 
}