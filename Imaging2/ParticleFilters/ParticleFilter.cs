  
using ImagingOps;
using Numpy;
using Numpy.Models;
using OpenCvSharp;
using Python.Runtime;

namespace Imaging2
{
    public interface IParticleFilter
    { 
        double SigmaExp { get; set; }
        double SigmaDyn { get; set; }
   
        //NDarray Template { get; set; }
        NDarray GetTemplate();
        Mat GetDisplayableTemplate();
        void SetTemplate(NDarray template);
        void Process(NDarray frame); 
        void SetInitialFrame(NDarray frame);
        NDarray GetParticles();
        NDarray GetWeights();
    }
    public class ParticleFilter : IParticleFilter
    { 
        // Default similarity value used when an error occurs in the GetErrorMetric method.
        private const double DefaultSimilarity = 1e-16;

        // The rectangle that defines the template area in the image.
        protected Rectangle TemplateRect { get; }

        // The number of particles in the filter.
        protected readonly int numParticles;

        // Sigma value used for the similarity metric.
        public double SigmaExp { get; set; }

        // Sigma value that can be used when adding gaussian noise to U and V.
        public double SigmaDyn { get; set; }

        // Array of N weights, one for each particle.
        protected NDarray Weights { get; set; }

        // N x 2 array where N = Num_Particles, in x y format.
        protected virtual NDarray Particles { get; set; }

        // Cropped section of the first video frame that is used as the template to track.
        protected NDarray Template { get; set; }


        public void SetInitialFrame(NDarray frame)
        {
            activeFrame = frame.astype(np.uint8);

            this.Particles = CreateInitialParticles(numParticles, TemplateRect);

            var template = ExtractTemplate(frame, TemplateRect);
            template = ImageOps.Resize(template.ToMat(), TemplateRect.w, TemplateRect.h).ToNDArray();
            this.SetTemplate(template);
        }

        public Mat GetDisplayableTemplate()
        {
            using (Py.GIL())
            {
                return GetTemplate().copy().astype(np.uint8).ToMat();
            } 
        }
        public virtual NDarray GetTemplate()
        {
            return Template;
        }

        public virtual void SetTemplate(NDarray template)
        {
            Template = template.astype(np.uint8);
        }

        public NDarray GetParticles()
        {
            return Particles.copy();
        }

        public NDarray GetWeights()
        {
            return Weights.copy();
        }

        public NDarray ExtractTemplate(NDarray frame, Rectangle templateRect)
        { 
            var x = templateRect.x;
            var y = templateRect.y;
            var h = templateRect.h;
            var w = templateRect.w;
            var template = frame[$"{y}:{y + h}, {x}:{x + w}"];

            return template;
        }

        // Total error of the particle filter.
        protected double TotalError { get; set; }

        // Current iteration of the particle filter.
        protected int Iteration { get; set; }

        protected NDarray activeFrame;

        public ParticleFilter(int numParticles, double sigmaExp, double sigmaDyn, Rectangle templateRect)
        { 
            this.numParticles = numParticles;
            this.SigmaExp = sigmaExp;
            this.SigmaDyn = sigmaDyn;
            this.TemplateRect = templateRect;

            this.Weights = np.ones(numParticles) / numParticles;
            this.TotalError = 0;
            this.Iteration = 0;
            this.Template = null;

        }

        // This method creates the initial set of particles for the particle filter.
        // Each particle is represented as a 2D point (x, y) in the image.
        // The initial position of each particle is set to the center of the template rectangle.
        protected virtual NDarray CreateInitialParticles(int numberOfParticles, Rectangle templateRect)
        {
            // Create a 2D array with numberOfParticles rows and 2 columns, initialized to zeros.
            var particles = np.zeros(new Shape(numberOfParticles, 2), np.int32);

            // Get the center of the template rectangle.
            int x = templateRect.x;
            int y = templateRect.y;
            int w = templateRect.w;
            int h = templateRect.h;

            // Set the x-coordinate of each particle to the x-coordinate of the center of the template rectangle.
            particles[":, 0"] = (NDarray)x + (w / 2);

            // Set the y-coordinate of each particle to the y-coordinate of the center of the template rectangle.
            particles[":, 1"] = (NDarray)y + (h / 2);
             
            return particles;
        }
 
        // This method calculates the error metric between the template and a cutout from the frame.
        // The error metric is based on the Mean Squared Error (MSE) between the template and the frame cutout.
        // The MSE is then transformed using a Gaussian function to get the final error metric.
        // If an error occurs during the calculation, the method returns a default similarity value.
        protected virtual double GetErrorMetric(NDarray template, NDarray frameCutout)
        { 
            try
            {
                // Calculate the difference between the template and the frame cutout.
                var dif = np.subtract(template.astype(np.float64), frameCutout.astype(np.float64));

                // Calculate the Mean Squared Error (MSE) between the template and the frame cutout.
                var mse = (1 / (double)template.size) * dif.pow(2).sum();

                // Transform the MSE using a Gaussian function to get the final error metric.
                var error = np.exp((-1 * mse) / (2 * (SigmaExp * SigmaExp)));

                // Return the error metric.
                return error.item<double>();
            }
            catch
            {
                // If an error occurs, return the default similarity value.
                return DefaultSimilarity;
            }
        }

        // This method resamples the particles based on their weights.
        // Resampling is a common step in particle filters that helps to focus on areas of the state space where the posterior density is high.
        // The method ensures that each particle has a chance of being selected proportional to its weight.
        //protected virtual NDarray ResampleParticles()
        //{
        //    // Create a copy of the current particles.
        //    var resampleParticles = Particles.copy();

        //    // Extract the x and y coordinates of the particles.
        //    var px = resampleParticles[":, 0"];
        //    var py = resampleParticles[":, 1"];

        //    // Resample the x and y coordinates based on the weights of the particles.
        //    // The np.random.choice function randomly selects elements from the px and py arrays.
        //    // The probability of each element being selected is given by the weights of the particles.
        //    px = np.random.choice(px, new[] { numParticles }, true, Weights);
        //    py = np.random.choice(py, new[] { numParticles }, true, Weights);

        //    // Update the x and y coordinates of the resampled particles.
        //    resampleParticles[":, 0"] = px;
        //    resampleParticles[":, 1"] = py;

        //    return resampleParticles;
        //}


        // Resample base on stochastic universal resampling, used to prevent
        // the problem of particle deprivation, where too many particles end up having very low weights 
        protected virtual NDarray ResampleParticles()
        {
            // Normalize weights
            var normalizedWeights = Weights / np.sum(Weights);
            // Calculate the cumulative sum of the normalized weights
            var cumulativeSum = np.cumsum(normalizedWeights);
            // Initialize the resampled particles array
            var resampledParticles = np.zeros_like(Particles);

            // Determine the equally spaced intervals for the pointers
            var step = 1.0 / numParticles;
            var pointer = np.random.rand(1).asscalar<double>() * step;

            int index = 0;
            for (int i = 0; i < numParticles; i++)
            {
                while (pointer > cumulativeSum[index].asscalar<double>())
                {
                    index++;
                } 
                resampledParticles[i] = Particles[index];
                pointer += step;
            }

            return resampledParticles;
        }


        // This method calculates the start and end indices for a given particle in the image.
        // These indices are used to extract a section of the image that corresponds to the particle.
        // The method ensures that the indices are within the bounds of the image.
        protected (int, int, int, int) GetStartAndEndIndices(NDarray image, NDarray particle)
        {
            // Get the height and width of the template.
            var height = Template.shape[0];
            var width = Template.shape[1];

            // Calculate half of the height and width of the template.
            // These values are used to center the particle in the extracted section of the image.
            var halfWidth = width / 2;
            var halfHeight = height / 2;

            // Get the x and y coordinates of the particle.
            int px = particle.GetData<int>()[0];
            int py = particle.GetData<int>()[1];

            // Calculate the start and end indices for the height (y-coordinate) of the image.
            var startHeight = Math.Max(0, py - halfHeight);
            startHeight = Math.Min(startHeight, image.shape[0] - height);
            var endHeight = startHeight + height;

            // Calculate the start and end indices for the width (x-coordinate) of the image.
            var startWidth = Math.Max(0, px - halfWidth);
            startWidth = Math.Min(startWidth, image.shape[1] - width);
            var endWidth = startWidth + width;

            // Return the start and end indices for the height and width of the image.
            return (startHeight, endHeight, startWidth, endWidth);
        }

        #region Logging
        public void LogIteration(int iteration)
        {
            Console.WriteLine();
            Console.WriteLine($"Iteration {iteration}");
        }

        public void LogTime(DateTime start, DateTime end)
        {
            Console.WriteLine($"Time taken : {end - start}");
        }

        public void LogTimeMessage( string message)
        {
            //Console.WriteLine(message);
        }

        public void LogTotalError(double totalError)
        {
            Console.WriteLine($"Total Error : {totalError}");
        }

        public void LogPredictedCenter()
        {

            double u_weighted_mean = 0;
            double v_weighted_mean = 0;
            for (int i = 0; i < numParticles; i++)
            {
                var particle = Particles[i];
                var weightValue = Convert.ToDouble(Weights[i].repr);
                int px = particle.GetData<int>()[0];
                int py = particle.GetData<int>()[1];
                u_weighted_mean += px * weightValue;
                v_weighted_mean += py * weightValue;
            }

            var predictedCenter = (u_weighted_mean, v_weighted_mean);

            Console.WriteLine($"predicted center {predictedCenter}");
        }

        public void LogBestParticleIndices(int startY, int endY, int startX, int endX)
        {
            Console.WriteLine($"Best Particle Indices {startY} {endY} {startX} {endX}");
        }

        public void LogTemplateDiff(double diff)
        {
            Console.WriteLine($"Template Diff {diff}");
        }

        private void LogProcessArrays(NDarray template, NDarray particles, NDarray weights)
        {
            Console.WriteLine($"Particles sum: {particles.sum()}");
            Console.WriteLine($"Weights sum: {weights.sum()}");
            Console.WriteLine($"Template sum: {template.sum()}");
        }

        #endregion

        // This method processes a frame of the video.
        // It updates the particles and their weights based on the frame.
        // If the iteration is greater than 0, it adds Gaussian noise to the particles.
        // It then calculates the error metric for each particle and updates the weights accordingly.
        // If the total error is 0, it resets the weights to be equal.
        // Finally, it resamples the particles based on their weights.
        public virtual void Process(NDarray frame)
        {
            activeFrame = frame.copy().astype(np.uint8);

            // If the iteration is greater than 0, add Gaussian noise to the particles.
            if (Iteration > 0)
            {
                var processNoise = np.random.normal(0, (float)SigmaDyn, Particles.shape.Dimensions).astype(np.int32);
                Particles += processNoise;
            }

            Iteration++;
             
            // Calculate the error metric for each particle and update the weights.
            double totalError = 0;
            for (int i = 0; i < numParticles; i++)
            {
                var p = Particles[i];
                var (startHeight, endHeight, startWidth, endWidth) = GetStartAndEndIndices(activeFrame, p);
                var frameCutout = activeFrame[$"{startHeight}:{endHeight}, {startWidth}:{endWidth}"];
                var err = GetErrorMetric(Template, frameCutout);

                Weights[i] = (NDarray)err;
                totalError += err;
            }



            // LogIteration(Iteration);
            // LogTimeMessage($"Time taken by particles: {numparticleTimer.Elapsed.TotalMilliseconds} ms");
            // LogProcessArrays(Template, Particles, Weights);
            // LogTime(start, end);
            // LogTotalError(totalError);

            // If the total error is 0, reset the weights so that they are normalized.
            if (totalError == 0)
            {
                Weights = np.ones((numParticles)) / numParticles;
            }
            else
            {
                Weights = Weights / totalError;
            }

            // Resample the particles based on their weights.
            Particles = ResampleParticles();
            // LogPredictedCenter(); 
        }

    }
}