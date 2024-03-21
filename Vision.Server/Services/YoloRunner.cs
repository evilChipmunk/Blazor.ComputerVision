using System.Text;
using ImagingOps; 
using OpenCvSharp;
using Python.Runtime; 
using Newtonsoft.Json;

namespace Imaging2
{
    public class ImageDataRequest
    {
        [JsonProperty("data")]
        public byte[] Data { get; set; }

        [JsonProperty("shape")]
        public int[] Shape { get; set; }
    }
    public class RootObject
    {
        [JsonProperty("results")]
        public List<YoloResult> Results { get; set; } = new List<YoloResult>();
    }
    public class YoloResult
    {
        [JsonProperty("img")]
        public string Data { get; set; }

        [JsonProperty("originalImageName")]
        public string OriginalImageName { get; set; }

        [JsonProperty("items")]
        public List<YoloItem> Items { get; set; } = new List<YoloItem>(); 

        public Mat Mat { get; set; }
    }

    public class YoloItem
    {
        [JsonProperty("label")]
        public string Label { get; set; }
        [JsonProperty("confidence")]
        public double Confidence { get; set; }
    }


    public class YoloRunner
    {
        private readonly Configuration config;

        public YoloRunner(Configuration config)
        {
            this.config = config;
        }
        public async Task<YoloResult> MakePrediction(Mat mat)
        {
            try
            {
                var imageRequest = new ImageDataRequest();
                using (Py.GIL())
                {
                    using (var scope = Py.CreateScope())
                    {
                        var nd = mat.ToNDArray(); 
                        imageRequest.Data = nd.flatten().GetData<byte>(); ;
                        imageRequest.Shape = nd.shape.Dimensions; 
                    }
                }

                var client = new HttpClient();
                client.BaseAddress = new Uri(config.YOLOAPI_URL);


                var json = JsonConvert.SerializeObject(imageRequest);

                HttpContent content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("predict", content);
             
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var results = JsonConvert.DeserializeObject<RootObject>(responseContent);

                    var result = results.Results.FirstOrDefault();
                    if (result == null) return null;
                     
                    var height = imageRequest.Shape[0];
                    var width = imageRequest.Shape[1];
                    // Decode the base64 string to a byte array
                    byte[] byteArray = Convert.FromBase64String(result.Data);

                    result.Mat = new Mat(height, width, MatType.CV_8UC3, byteArray);
                    return result;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e); 
            }

            return null; 
        } 
    }
}