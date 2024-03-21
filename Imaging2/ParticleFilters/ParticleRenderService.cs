using Numpy;
using OpenCvSharp;

namespace Imaging2
{
    public interface IParticleRenderService
    {
        void Render(Mat frameIn, IParticleFilter filter);
    }

    public class ParticleRenderService : IParticleRenderService
    { 
        // This method renders the particles on the frame.
        // It calculates the weighted mean of the x and y coordinates of the particles.
        // It then draws a circle at the position of each particle and a rectangle around the area of interest.
        // Finally, it calculates the distance from each particle to the weighted mean and draws a circle with this distance as the radius.
        public virtual void Render(Mat frameIn, IParticleFilter filter)
        {
            var template = filter.GetTemplate();
            var particles = filter.GetParticles();
            var weights = filter.GetWeights();
            var numParticles = particles.shape[0];

            // Get the height and width of the template.
            var tHeight = template.shape[0] / 2;
            var tWidth = template.shape[1] / 2;

            // Initialize the weighted mean of the x and y coordinates of the particles.
            var xWeightedMean = 0.0;
            var yWeightedMean = 0.0;

            // Define the radius and thickness of the circles that will be drawn at the position of each particle.
            const int circleRadius = 2;
            const int circleThickness = -1;

            // For each particle, calculate its weight, draw a circle at its position, and update the weighted mean of the x and y coordinates.
            for (int i = 0; i < numParticles; i++)
            {
                var particle = particles[i];
                var weightValue = Convert.ToDouble(weights[i].repr);

                var x = particle.GetData<int>()[0];
                var y = particle.GetData<int>()[1];

                xWeightedMean += x * weightValue;
                yWeightedMean += y * weightValue;

                // Draw a circle at the position of the particle.
                frameIn.Circle(x, y, circleRadius, Scalar.Red, circleThickness);
            }

            // Draw a rectangle around the area of interest.
            var point1 = new Point((int)xWeightedMean - tWidth, (int)yWeightedMean - tHeight);
            var point2 = new Point((int)xWeightedMean + tWidth, (int)yWeightedMean + tHeight);
            frameIn.Rectangle(point1, point2, Scalar.Green, thickness:4);

            // Calculate the distance from each particle to the weighted mean and draw a circle with this distance as the radius.
            double distance = 0;
            for (int i = 0; i < numParticles; i++)
            {
                var particle = particles[i];
                var weightValue = Convert.ToDouble(weights[i].repr);

                var weightedMean = new[] { xWeightedMean, yWeightedMean };
                var dist = (double)np.linalg.norm(particle.astype(np.float64) - weightedMean);
                distance += dist * weightValue;
            }

            // Draw a circle with the calculated distance as the radius.
            frameIn.Circle(new Point((int)xWeightedMean, (int)yWeightedMean), (int)distance, Scalar.Green);
        }
    }
     
    public class VectorParticleRenderService : IParticleRenderService
    {
        public void Render(Mat frameIn, IParticleFilter filter)
        {
            var template = filter.GetTemplate();
            var particles = filter.GetParticles();
            var weights = filter.GetWeights();
            var numParticles = particles.shape[0];

            // Get the height and width of the template.
            var tHeight = template.shape[0] / 2;
            var tWidth = template.shape[1] / 2;

            // Initialize the weighted mean of the x and y coordinates of the particles.
            var xWeightedMean = 0.0;
            var yWeightedMean = 0.0;
            var vxWeightedMean = 0.0;
            var vyWeightedMean = 0.0;

            // Define the radius and thickness of the circles that will be drawn at the position of each particle.
            const int circleRadius = 2;
            const int circleThickness = -1;

            // For each particle, calculate its weight, draw a circle at its position, and update the weighted mean of the x and y coordinates.
            for (int i = 0; i < numParticles; i++)
            {
                var particle = particles[i];
                var weightValue = Convert.ToDouble(weights[i].repr);

                var x = particle.GetData<int>()[0];
                var y = particle.GetData<int>()[1];
                var vx = particle.GetData<int>()[2];
                var vy = particle.GetData<int>()[3];

                xWeightedMean += x * weightValue;
                yWeightedMean += y * weightValue;
                vxWeightedMean += vx * weightValue;
                vyWeightedMean += vy * weightValue;

                // Draw a circle at the position of the particle.
                frameIn.Circle(x, y, circleRadius, Scalar.Red, circleThickness);
            }

            // Draw a rectangle around the area of interest.
            var point1 = new Point((int)xWeightedMean - tWidth, (int)yWeightedMean - tHeight);
            var point2 = new Point((int)xWeightedMean + tWidth, (int)yWeightedMean + tHeight);
            frameIn.Rectangle(point1, point2, Scalar.Green, thickness:4);

            // Calculate the distance from each particle to the weighted mean and draw a circle with this distance as the radius.
            double distance = 0;
            for (int i = 0; i < numParticles; i++)
            {
                var particle = particles[i];
                var weightValue = Convert.ToDouble(weights[i].repr);

                var weightedMean = new[] { xWeightedMean, yWeightedMean, vxWeightedMean, vyWeightedMean };
                var dist = (double)np.linalg.norm(particle.astype(np.float64) - weightedMean);
                distance += dist * weightValue;
            }

            // Draw a circle with the calculated distance as the radius.
            frameIn.Circle(new Point((int)xWeightedMean, (int)yWeightedMean), (int)distance, Scalar.Green);
        }
    }
}