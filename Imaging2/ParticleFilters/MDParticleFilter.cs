using ImagingOps;
using Numpy;
using OpenCvSharp;

namespace Imaging2
{
    public class MDParticleFilter : AppearanceModelParticleFilter
    {
        private double scale = 1;
        private double initialTemp = 1;
        private double decay = 0.0055;
        private double minTemp = 0.00009; 

        public MDParticleFilter(int numParticles,
            double sigmaExp, float sigmaDyn, double alpha, Rectangle templateRect) : base(numParticles,
            sigmaExp, sigmaDyn, alpha, templateRect)
        {
            decay = 0.0009;
        }

        private double GetScale()
        {
            if (Iteration <= 20)
                return 0.995;
            return 0.991;
        }

        private double GetTemperature()
        {
            if (Iteration == 10)
                decay = 0.0005;

            if (Iteration >= 70)
            {
                if (Iteration == 70)
                {
                    initialTemp = 0.605;
                    decay = 0.0015;
                    decay = 0.0003;
                }
                if (Iteration == 80)
                {
                    var a = 4;
                }
            }

            var temp = initialTemp - (decay * Iteration);

            if (temp < minTemp)
                temp = minTemp;

            return temp;
        }


        public override NDarray GetTemplate()
        {
            var template = base.Template;
            var frame = activeFrame;

            var scale = GetScale();

            var templateMat = template.ToMat();


            if (template.shape[1] <= 55)
            {
                var smallestWidth = 20;
                var smallestHeight = 55;

                scale = 0.998;
                var width = Math.Max((int)(template.shape[1] * scale), smallestWidth);
                var height = Math.Max((int)(template.shape[0] * scale), smallestHeight);
                var dim = new OpenCvSharp.Size(width, height);
                Cv2.Resize(templateMat, templateMat, dim, 0, 0, InterpolationFlags.Cubic);
            }
            else
            {
                Cv2.Resize(templateMat, templateMat, new OpenCvSharp.Size(), scale, scale, InterpolationFlags.Cubic);
            }

            base.SetTemplate(templateMat.ToNDArray());
            return base.Template;
        }

        //public override void SetTemplate(NDarray template)
        //{

        //    if (Iteration == 0)
        //    {
        //        base.SetTemplate(template);
        //        return;
        //    }
        //    var scale = GetScale();

        //    var templateMat = template.copy().ToMat();


        //    if (template.shape[1] <= 55)
        //    {
        //        var smallestWidth = 20;
        //        var smallestHeight = 55;

        //        scale = 0.998;
        //        var width = Math.Max((int)(template.shape[1] * scale), smallestWidth);
        //        var height = Math.Max((int)(template.shape[0] * scale), smallestHeight);
        //        var dim = new OpenCvSharp.Size(width, height);
        //        Cv2.Resize(templateMat, templateMat, dim, 0, 0, InterpolationFlags.Cubic);
        //    }
        //    else
        //    {
        //        Cv2.Resize(templateMat, templateMat, new OpenCvSharp.Size(), scale, scale, InterpolationFlags.Cubic);
        //    }

        //    base.SetTemplate(templateMat.ToNDArray());
        //}
    }
}