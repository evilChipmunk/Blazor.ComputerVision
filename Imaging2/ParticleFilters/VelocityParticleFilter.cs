using Numpy;
using Numpy.Models;

namespace Imaging2
{
    public class VelocityParticleFilter : AppearanceModelParticleFilter
    {
        public VelocityParticleFilter(int numParticles, double sigmaExp, float sigmaDyn, double alpha, Rectangle templateRect) : base(numParticles, sigmaExp, sigmaDyn, alpha, templateRect)
        {
        }

        protected override NDarray CreateInitialParticles(int numberOfParticles, Rectangle templateRect)
        {
            // Create a 2D array with numberOfParticles rows and 4 columns (x, y, vx, vy), initialized to zeros.
            var particles = np.zeros(new Shape(numberOfParticles, 4), np.int32);

            // Initialization of position to the center of the template rectangle remains the same.
            int centerX = templateRect.x + (templateRect.w / 2);
            int centerY = templateRect.y + (templateRect.h / 2);
            particles[":, 0"] = (NDarray)centerX; // x position
            particles[":, 1"] = (NDarray)centerY; // y position

            // Velocity components (vx, vy) are initially set to 0.
            // particles[":, 2"] and particles[":, 3"] are already initialized to 0 by default.

            return particles; 
        }
        private NDarray previousParticles; // Add this field to keep track of the previous state of each particle

        public override void Process(NDarray frame)
        {
            Iteration += 1;
            activeFrame = frame.copy().astype(np.uint8);
            // If the iteration is greater than 0, update particle positions based on their velocities
            // and add Gaussian noise to both position and velocity.
            if (Iteration > 0)
            {
                previousParticles = Particles.copy();
                // Update positions based on velocity.
                Particles[":, 0"] += Particles[":, 2"]; // Update x position by vx
                Particles[":, 1"] += Particles[":, 3"]; // Update y position by vy

                // Add Gaussian noise to positions and velocities.
                var positionNoise = np.random.normal(0, (float)SigmaDyn, new int[] {numParticles, 2}).astype(np.int32);
                var velocityNoise = np.random.normal(0, (float)SigmaDyn, new int[] { numParticles, 2 }).astype(np.int32);
 
                Particles[":, 0:2"] += positionNoise; // Add noise to positions
                Particles[":, 2:4"] += velocityNoise; // Add noise to velocities
            }

            // The rest of the process method remains largely unchanged, focusing on calculating error metrics and resampling.
        }

        protected override double GetErrorMetric(NDarray template, NDarray frameCutout)
        {
            // Calculate the basic error metric as in the base class
            double basicError = base.GetErrorMetric(template, frameCutout);

            // Calculate an additional error based on the velocity of the particles
            // This is just a placeholder - you'll need to replace this with your actual calculation
            double velocityError = CalculateVelocityError();

            // Combine the basic error and the velocity error
            // This is also a placeholder - you'll need to decide how to combine these errors
            double totalError = CombineErrors(basicError, velocityError);

            return totalError;
        }

        private double CombineErrors(double basicError, double velocityError)
        {
            double totalError = basicError + velocityError;
            return totalError;
        }

        private double CalculateVelocityError()
        {
            // Calculate the predicted velocity of each particle
            // This is just a placeholder - you'll need to replace this with your actual calculation
            NDarray predictedVelocities = CalculatePredictedVelocities();

            // Calculate the actual velocity of each particle
            // This is just a placeholder - you'll need to replace this with your actual calculation
            NDarray actualVelocities = CalculateActualVelocities();

            // Calculate the difference between the predicted and actual velocities
            NDarray velocityDifferences = predictedVelocities - actualVelocities;

            // Calculate the error based on the velocity differences
            // This could be the sum of the absolute differences, for example
            double velocityError = Convert.ToDouble(np.sum(np.abs(velocityDifferences)).repr);

            return velocityError;
        }

        private NDarray CalculatePredictedVelocities()
        {
            // This is just a placeholder - you'll need to replace this with your actual calculation
            // For example, if you have a model of the dynamics of the particles, you could use that to predict the new velocity
            NDarray predictedVelocities = previousParticles;

            return predictedVelocities;
        }

        private NDarray CalculateActualVelocities()
        {
            // The actual velocity is the difference between the current and previous state
            NDarray actualVelocities = Particles - previousParticles;

            return actualVelocities;
        }
    }
}