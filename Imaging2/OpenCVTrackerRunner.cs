 
using Python.Runtime;

namespace Imaging2
{
    public class OpenCVTrackerRunner
    {
        private readonly Configuration config;

        public OpenCVTrackerRunner(Configuration config)
        {
            this.config = config;
        }

        public void Run()
        { 
            using (Py.GIL())
            { 
                using (var scope = Py.CreateScope())
                {
                    try
                    {  
                        //or scope.Import to limit only to this scope
                        //in this case it doesn't matter
                        dynamic tracker = Py.Import("tracker");
                        dynamic cvtracker = tracker.cvtracker();

                        var videoPath = Path.Join(config.OutputDirectory, "streetoriginal.mp4");
                        cvtracker.track(videoPath);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e); 
                    }
                }
            }  
        } 
    }
}