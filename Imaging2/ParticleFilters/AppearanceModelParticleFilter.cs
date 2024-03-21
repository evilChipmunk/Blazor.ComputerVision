using ImagingOps;
using Numpy;
using OpenCvSharp;

namespace Imaging2
{
    public class AppearanceModelParticleFilter : ParticleFilter, IParticleFilter
    {
        // The alpha value used for blending the old template and the best particle section.
        private double alpha;

        public double Alpha
        {
            get
            {
                return alpha;
            }
            set
            {
                alpha = value;
            }
        }

        public AppearanceModelParticleFilter(int numParticles,
            double sigmaExp, float sigmaDyn, double alpha, Rectangle templateRect) : base(numParticles, sigmaExp, sigmaDyn, templateRect)
        {
            this.alpha = alpha; 
        }

        // This method returns the start and end indices of the best particle in the frame.
        // The best particle is the one with the maximum weight.
        // The indices are used to extract a section of the frame that corresponds to the best particle.
        public (int, int, int, int) GetBestParticleIndices(NDarray frame)
        {
            // Find the maximum weight among all particles.
            var maxWeight = Weights.max();

            // Create a boolean array that is true at the indices where the particle's weight is equal to the maximum weight.
            var matched = Weights.equals(maxWeight);

            // Get the indices of the particles with the maximum weight.
            var bestWeights = np.nonzero(matched);
            var bestWeightsArr = bestWeights.First();

            // If there are multiple particles with the maximum weight, select the first one.
            var bestWeight = bestWeightsArr[0];

            // Get the best particle.
            var bestParticle = Particles[bestWeight];

            // Return the start and end indices for the best particle.
            return GetStartAndEndIndices(frame, bestParticle);
        }

        private void Update(NDarray frame)
        { 
            // Get the height and width of the template
            var templateHeight = Template.shape[0];
            var templateWidth = Template.shape[1];

            // Create a new size object with the template dimensions
            var templateSize = new Size(templateWidth, templateHeight);

            // Get the indices of the best particle
            var (startHeightIndex, endHeightIndex, startWidthIndex, endWidthIndex) = GetBestParticleIndices(frame);

            // Extract a section of the frame that corresponds to the best particle
            var bestParticleSection = frame[$"{startHeightIndex}:{endHeightIndex}, {startWidthIndex}:{endWidthIndex}"];

            // Convert the frame and the best particle section to Mat objects
            var frameAsMat = frame.ToMat();
            var bestParticleSectionAsMat = bestParticleSection.ToMat();

            // Resize the frame to match the size of the template
            Cv2.Resize(frameAsMat, bestParticleSectionAsMat, templateSize, interpolation: InterpolationFlags.Cubic);

            // Update the template by blending the old template and the best particle section
            var oldTemplate = this.Template.copy();
            var blendingFactor = alpha;
            var newTemplate = blendingFactor * bestParticleSection + (1 - blendingFactor) * oldTemplate;

            this.SetTemplate(newTemplate);
            // LogBestParticleIndices(startHeightIndex, endHeightIndex, startWidthIndex, endWidthIndex);
        }

        // This method overrides the Process method in the base ParticleFilter class.
        // It processes a frame of the video and updates the particles and their weights based on the frame.
        // Additionally, it updates the template used for tracking every nth frame, where n is a predefined interval.
        // The template is updated by blending the old template and the section of the frame that corresponds to the best particle.
        public override void Process(NDarray frame)
        { 
            // Call the Process method in the base ParticleFilter class to update the particles and their weights.
            base.Process(frame);


            // If the current iteration is a multiple of the update interval, update the template.
            if (Iteration % 1 == 0)
            {
                // Update the template by blending the old template and the section of the frame that corresponds to the best particle.
                Update(frame);
            }
        }
    }
}