using System.Drawing;
using ImagingOps; 
using OpenCvSharp;

namespace Vision.Server.Services
{
    public class WebCamImageArgs
    {
        public string ImageDataUrl;
        public byte[] ImageData;
    }

    public class FileService
    {
        public event EventHandler<WebCamImageArgs> ImageReceived;

        public List<string> Files = new();

        public List<string> CreateFiles(DataSetType dataSetType)
        {
            Files.Clear();

            switch (dataSetType)
            {
                case DataSetType.None:
                    break;

                case DataSetType.Mandrill: 
                    Files.Add("Mandrill.bmp");
                    break;

                default:
                    var imageCount = Directory.EnumerateFiles(GetFolderPath(dataSetType)).Count();
                    for (int i = 0; i < imageCount; i++)
                    {
                        var num = i.ToString("000");
                        var file = $"{num}.jpg";
                        Files.Add(file);
                    }

                    break;

            }

            Files.Sort();
            return Files;
        }

        public string GetFolderPath(DataSetType selectedDataset)
        {
            var basePath = GetBasePath();
            string folder = "";
            switch (selectedDataset)
            {
                case DataSetType.PresidentHand:
                    folder = "pres_debate";
                    break;
                case DataSetType.Pedestrians:
                    folder = @"pedestrians";
                    break;
            }

            return Path.Join(basePath, folder); 
        }

        private static string GetBasePath()
        {
            var basePath = $"{Directory.GetCurrentDirectory()}{@$"\wwwroot\images"}";
            return basePath;
        }

        public async Task<Mat> GetMat(byte[] bytes, ImreadModes mode = ImreadModes.Grayscale)
        {
            return ImageOps.LoadImage(bytes, mode);
        }

        public async Task<Mat> GetMat(string fileName, DataSetType dataSetType, ImreadModes mode = ImreadModes.Grayscale)
        {
            string basePath = GetFolderPath(dataSetType);
            var filePath = Path.Join(basePath, fileName);
            var bytes = await File.ReadAllBytesAsync(filePath);

            return ImageOps.LoadImage(bytes, mode);
        } 
   

        public async Task<Mat> GetMandrill(ImreadModes mode = ImreadModes.Color )
        {
            var basePath = GetBasePath();
            string fileName = "Mandrill.bmp";
            var filePath = $@"{basePath}\{fileName}";
            var bytes = await File.ReadAllBytesAsync(filePath);

            return ImageOps.LoadImage(bytes, mode);
        }

        public async Task<List<Mat>> GetOpticalFlowImages()
        {
            var files = CreateFiles(DataSetType.Pedestrians);

            var twoFrames = files.Where(x => x == "029.jpg" || x == "033.jpg");

            var mats = new List<Mat>();

            foreach (var frame in twoFrames)
            {
                var basePath = GetFolderPath(DataSetType.Pedestrians);
                var filePath = $@"{basePath}\{frame}";
                var bytes = await File.ReadAllBytesAsync(filePath);

                mats.Add(ImageOps.LoadImage(bytes, ImreadModes.Color));

            }

            return mats;
        }


        public async Task<Mat> GetFace()
        { 
            var files = CreateFiles(DataSetType.PresidentHand);

            var frame = files.First(x => x == "029.jpg");
             
            var basePath = GetFolderPath(DataSetType.PresidentHand);
            var filePath = $@"{basePath}\{frame}";
            var bytes = await File.ReadAllBytesAsync(filePath);

            return ImageOps.LoadImage(bytes, ImreadModes.Color); 
        }

        public string GetHaarCascade_Face()
        {
            var basePath = GetBasePath();
            return Path.Join(basePath, "haarcascade_frontalface.default.xml"); 
        }

        public void NotifyNewImageData(byte[] imageData, string imageDataUrl)
        {
            var args = new WebCamImageArgs
            {
                ImageData = imageData,
                ImageDataUrl = imageDataUrl
            };
            ImageReceived?.Invoke(this, args);
        } 
    }
}