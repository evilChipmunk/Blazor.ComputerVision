using Imaging2;
using ImagingOps; 
using Numpy;
using OpenCvSharp;
using Python.Runtime;
using System.Diagnostics;

namespace Vision.Server.Services
{
    public class ParticleProcessor
    {
        private readonly FileService fileService;
        private IParticleRenderService particleRenderService;
        private IParticleFilter filter;
        private readonly Scaler filterScaler; 
        public double Sigma_MSE;
        public float Sigma_Dyn;
        public double Alpha;
        public int Particles;

        public ParticleProcessor(FileService fileService,  Scaler filterScaler)
        {
            this.fileService = fileService; 
            this.filterScaler = filterScaler; 
        }

        public IParticleFilter CreateFilter(Rectangle templateRect, ParticleModelType modelType)
        {
            particleRenderService = new ParticleRenderService();
            switch (modelType)
            {
                case ParticleModelType.Appearance:
                    filter = new AppearanceModelParticleFilter(Particles, Sigma_MSE, Sigma_Dyn, Alpha, templateRect);
                    break;
                case ParticleModelType.Particle:
                    filter = new ParticleFilter(Particles, Sigma_MSE, Sigma_Dyn, templateRect);
                    break;
                case ParticleModelType.MDParticleFilter:
                    filter = new MDParticleFilter(Particles, Sigma_MSE, Sigma_Dyn, Alpha, templateRect);
                    break;
                case ParticleModelType.Velocity:
                    filter = new VelocityParticleFilter(Particles, Sigma_MSE, Sigma_Dyn, Alpha, templateRect);
                    particleRenderService = new VectorParticleRenderService();
                    break;
            }
            return filter;
        }


        public void AlterFilter(ParticleModelType modelType)
        {
            if (filter == null) return;

            filter.SigmaExp = Sigma_MSE;
            filter.SigmaDyn = Sigma_Dyn;
             
            switch (modelType)
            {
                case ParticleModelType.Appearance:

                    ((AppearanceModelParticleFilter)filter).Alpha = Alpha;

                    break;
                case ParticleModelType.Particle:
                    break;
                case ParticleModelType.MDParticleFilter:
                    break;
                case ParticleModelType.Velocity:
                    break;
            }
        }

        public Mat ExtractTemplate(Mat frameMat, Rectangle templateRect)
        {
            Mat mat;
            using (Py.GIL())
            {
                var frame = frameMat.ToNDArray();
                var x = templateRect.x;
                var y = templateRect.y;
                var h = templateRect.h;
                var w = templateRect.w;
                var template = frame[$"{y}:{y + h}, {x}:{x + w}"];

                mat = template.copy().astype(np.uint8).ToMat(); 
            }

            return mat;
        } 
        public async Task<(Mat, Mat)> ProcessImage(byte[] imageData, ParticleModelType modelType)
        { 
            Mat frame = await fileService.GetMat(imageData, ImreadModes.Color);

            return await _process(frame, modelType);

        }
        public async Task<(Mat, Mat)> ProcessImage(Mat frame, ParticleModelType modelType)
        { 
            return await _process(frame, modelType);

        }

        public async Task<(Mat, Mat)> ProcessImage(string filePath, ParticleModelType modelType, DataSetType dataSetType)
        {
            fileService.Files.Remove(filePath); // remove the file from the list so we don't process it again

            if (string.IsNullOrEmpty(filePath))
            {
                return (new Mat(), new Mat());
            }

            Mat frame = await fileService.GetMat(filePath, dataSetType, ImreadModes.Color);

            return await _process(frame, modelType);

        }

        public async Task<(Mat, Mat)> _process(Mat frame, ParticleModelType modelType)
        {
            var totalTimer = new Stopwatch();
            totalTimer.Start();
            var viewableFrame = new Mat();
            var filterTemplate = new Mat();
            try
            { 
                Rectangle templateRect = new Rectangle(filterScaler.X, filterScaler.Y, filterScaler.TemplateWidth, filterScaler.TemplateHeight);

  
                var grayFrame = new Mat();
                Cv2.CvtColor(frame, grayFrame, ColorConversionCodes.BGR2GRAY); 
                grayFrame = ImageOps.Resize(grayFrame, filterScaler.ImageWidth, filterScaler.ImageHeight);

                frame.CopyTo(viewableFrame);
                viewableFrame = ImageOps.Resize(viewableFrame, filterScaler.ImageWidth, filterScaler.ImageHeight);

                ProcessFrame(templateRect, grayFrame, viewableFrame, modelType);


                filterTemplate = filter.GetDisplayableTemplate();
 
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            totalTimer.Stop();
            LogTime($"Total time for run and display {totalTimer.ElapsedMilliseconds} ms");
            LogTime(""); //intentional blank line


            return (filterTemplate, viewableFrame);
        }

        private void ProcessFrame(Rectangle templateRect, Mat frameMat, Mat viewableFrame, ParticleModelType modelType)
        {
            var timer = new Stopwatch();
            timer.Start();

            // when running on different threads you must lock!
            using (Py.GIL())
            { 
                //convert frame to NDarray and unit8 for more accurate calculations and not worry about clipping
                NDarray frame = frameMat.ToNDArray().astype(np.uint8);

                if (filter == null)
                {
                    filter = CreateFilter(templateRect, modelType);
                    filter.SetInitialFrame(frame);
                }

                try
                {
                    var processTimer = Stopwatch.StartNew();
                    filter.Process(frame);
                    LogTime($"Process time {processTimer.ElapsedMilliseconds} ms");
                     
                    var renderTimer = Stopwatch.StartNew();
                    particleRenderService.Render(viewableFrame, filter);
                    LogTime($"Render time {renderTimer.ElapsedMilliseconds} ms");
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }

            timer.Stop();
            LogTime($"Total run time {timer.ElapsedMilliseconds} ms");
        }


        public void DestroyFilter()
        {
            filter = null;
        }

        private void LogTime(string message)
        {
            Console.WriteLine(message);
        }

    }
}