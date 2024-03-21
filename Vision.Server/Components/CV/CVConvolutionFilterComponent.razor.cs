 
using System.ComponentModel;
using Microsoft.JSInterop;
using OpenCvSharp;
using System.Text.Json;
using ImagingOps;
using Vision.Server.Services; 

namespace Vision.Server.Components.CV
{
    public partial class CVConvolutionFilterComponent
    {
        protected override async Task Process(Mat startFrameMat, Mat endFrame)
        {
            using var grayMat = new Mat();
            using var dstMat = new Mat();
            Cv2.CvtColor(startFrameMat, grayMat, ColorConversionCodes.BGR2GRAY); 


            await SetDstImage(dstMat);
        }
          
        private FilterType selectedFilter;
        private Random rand;
        private int[,] imageMatrix;
        private int[,] outputMatrix; 
         

        private int[,] calculatedValues = new int[3, 3]; // For storing the multiplication results
        private int[,] imageSlice = new int[3, 3]; // For storing the current slice of the image
        private string[,] colorSlice = new string[3, 3]; // For storing color values  
        private (int, int) currentStep = (-1, -1); // Tracks the current processing step (row, column)

        protected FilterType SelectedFilter
        {
            get => selectedFilter;
            set
            {
                selectedFilter = value;

                var filter = GetFilter(selectedFilter);
                var serializedMatrix = SerializedMatrix(filter);
                InvokeAsync(async () =>
                {
                    await jsRuntime.InvokeVoidAsync("displayImageMatrix", "filterTable", serializedMatrix);
                    await Reset();
                });
            }
        }
         

        protected override async Task OnInitializedAsync()
        { 
            await base.OnInitializedAsync();
            rand = new Random(); 

            imageMatrix = await CreateImageMatrix();
            outputMatrix = new int[imageMatrix.GetLength(0), imageMatrix.GetLength(1)];
            Array.Copy(imageMatrix, outputMatrix, imageMatrix.Length);

        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                SelectedFilter = FilterType.Identity;
            }
            await base.OnAfterRenderAsync(firstRender);
        }

        private string SerializedMatrix(int[,] matrix)
        {
            var rows = matrix.GetLength(0);
            var cols = matrix.GetLength(1);
            var nestedArray = new int[rows][];
            for (int i = 0; i < rows; i++)
            {
                nestedArray[i] = new int[cols];
                for (int j = 0; j < cols; j++)
                {
                    nestedArray[i][j] = matrix[i, j];
                }
            }

            // Serialize the nested array
            var serializedMatrix = JsonSerializer.Serialize(nestedArray);
            return serializedMatrix;
        }

        public async Task<int[,]> CreateImageMatrix()
        { 
                imageMatrix = new int[10, 10];

                for (int i = 0; i < imageMatrix.GetLength(0); i++)
                {
                    for (int j = 0; j < imageMatrix.GetLength(1); j++)
                    {
                        // Fill each element with a random integer from 0 to 9
                        imageMatrix[i, j] = rand.Next(0, 255);
                    }
                }

                var fileService = new FileService();
                var mat = await fileService.GetMandrill(ImreadModes.Grayscale);
                await SetSrcImage(mat);

                StateHasChanged();
         

            return imageMatrix;
        }

        private async Task ProcessNextStep()
        {
            int rows = imageMatrix.GetLength(0);
            int cols = imageMatrix.GetLength(1);

            if (currentStep.Item1 == -1 && currentStep.Item2 == -1)
            {
                // Start from the beginning if this is the first step
                currentStep = (0, 0);
            }
            else
            {
                // Move to the next step
                if (currentStep.Item2 < cols - 1)
                {
                    // Move to the next column in the current row
                    currentStep.Item2++;
                }
                else if (currentStep.Item1 < rows - 1)
                {
                    // Move to the next row
                    currentStep.Item1++;
                    currentStep.Item2 = 0; // Reset column index to 0
                }
                else
                {
                    // Reached the end of the image matrix, stop processing
                    return;
                }
            }

            // Apply convolution filter to the current step
            var filter = GetFilter(selectedFilter);
            outputMatrix = ApplyConvolutionFilterAtStep(imageMatrix, outputMatrix, filter, currentStep.Item1, currentStep.Item2);

            // Serialize the processed matrix and pass it to JavaScript to display
            var serializedMatrix = SerializedMatrix(outputMatrix);
            await jsRuntime.InvokeVoidAsync("displayImageMatrix", "processedImageTable", serializedMatrix);
            await jsRuntime.InvokeVoidAsync("colorCell", "imageTable", currentStep.Item1, currentStep.Item2, filter.GetLength(0), true);
            await jsRuntime.InvokeVoidAsync("colorCell", "processedImageTable", currentStep.Item1, currentStep.Item2, filter.GetLength(0));

            StateHasChanged(); // Refresh UI to display updates
        }

        private async Task ProcessEntireImage()
        {
            var filter = GetFilter(selectedFilter);
            var matrix = ApplyConvolutionFilter(imageMatrix, filter);
            var serializedMatrix = SerializedMatrix(matrix);
            await jsRuntime.InvokeVoidAsync("displayImageMatrix", "processedImageTable", serializedMatrix);

            var mat = SrcMat.Clone();
            var matMatrix = mat.ToIntArray();

            matMatrix = ApplyConvolutionFilter(matMatrix, filter);
              

            // Decide if normalization is needed based on the filter type
            bool needsNormalization = selectedFilter == FilterType.GaussianBlur ||
                selectedFilter == FilterType.Blur ||
                selectedFilter == FilterType.BoxBlur ||
                selectedFilter == FilterType.LowPass ||
                selectedFilter == FilterType.MotionBlur;

            if (needsNormalization)
            {
                matMatrix = NormalizeOrScale(matMatrix);
            }
             

            int rows = matMatrix.GetLength(0);
            int cols = matMatrix.GetLength(1);
            byte[] flatArray = matMatrix.FlattenArrayToBytes();


            // Create a new Mat of type CV_8UC1.
            Mat processedMat = new Mat(rows, cols, MatType.CV_8UC1);

            processedMat.SetArray(flatArray);

            await SetDstImage(processedMat);


        }

        public int[,] ApplyConvolutionFilter(int[,] matrix, int[,] filter)
        {
            int width = matrix.GetLength(1);
            int height = matrix.GetLength(0);
            int filterWidth = filter.GetLength(1);
            int filterHeight = filter.GetLength(0);

            int offsetW = filterWidth / 2;
            int offsetH = filterHeight / 2;
            int[,] result = new int[height, width];

            for (int y = 0; y < height; y++) // Start from 0 to include borders
            {
                for (int x = 0; x < width; x++) // Start from 0 to include borders
                {
                    int pixelValue = 0;
                    for (int ky = -offsetH; ky <= offsetH; ky++)
                    {
                        for (int kx = -offsetW; kx <= offsetW; kx++)
                        {
                            int currentRow = y + ky;
                            int currentCol = x + kx;
                            // Only add to pixelValue if within bounds
                            if (currentRow >= 0 && currentRow < height && currentCol >= 0 && currentCol < width)
                            {
                                pixelValue += matrix[currentRow, currentCol] * filter[ky + offsetH, kx + offsetW];
                            }
                        }
                    }
                    result[y, x] = pixelValue;
                }
            }
            return result;
        }
         
        public int[,] ApplyConvolutionFilterAtStep(int[,] inputMatrix, int[,] outputMatrix, int[,] filter, int rowIndex, int colIndex)
        {
            int filterWidth = filter.GetLength(1);
            int filterHeight = filter.GetLength(0);

            // Check if filter dimensions are as expected
            if (filterWidth != 3 || filterHeight != 3)
            {
                throw new ArgumentException("Filter must be 3x3 for this calculation.");
            }

            int offsetW = filterWidth / 2;
            int offsetH = filterHeight / 2;

            // Initialize calculatedValues and imageSlice arrays
            Array.Clear(calculatedValues, 0, calculatedValues.Length);
            Array.Clear(imageSlice, 0, imageSlice.Length);

            // Populate imageSlice with appropriate values, handling out-of-bounds with zero padding
            for (int ky = -offsetH, ci = 0; ky <= offsetH; ky++, ci++)
            {
                for (int kx = -offsetW, cj = 0; kx <= offsetW; kx++, cj++)
                {
                    int currentRow = rowIndex + ky;
                    int currentCol = colIndex + kx;
                    if (currentRow >= 0 && currentRow < inputMatrix.GetLength(0) && currentCol >= 0 && currentCol < inputMatrix.GetLength(1))
                    {
                        imageSlice[ci, cj] = inputMatrix[currentRow, currentCol];
                    }
                    else
                    {
                        imageSlice[ci, cj] = 0; // Zero padding for out-of-bounds
                    }
                }
            }

            int result = 0;
            for (int ky = 0; ky < filterHeight; ky++)
            {
                for (int kx = 0; kx < filterWidth; kx++)
                {
                    int pixelValue = imageSlice[ky, kx];
                    int filterValue = filter[ky, kx];
                    calculatedValues[ky, kx] = pixelValue * filterValue; // Store multiplication result
                    result += calculatedValues[ky, kx];
                }
            }

            // Update the output matrix with the convolution result
            if (rowIndex >= 0 && rowIndex < outputMatrix.GetLength(0) && colIndex >= 0 && colIndex < outputMatrix.GetLength(1))
            {
                outputMatrix[rowIndex, colIndex] = result;
            }

            // Update the colorSlice matrix
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    int value = imageSlice[i, j];
                    // Example: Darker for lower values, lighter for higher values
                    int colorIntensity = value;// Math.Min(255, value * 10); // Simplified logic
                    colorSlice[i, j] = $"rgb({colorIntensity}, {colorIntensity}, {colorIntensity})";
                }
            }

            return outputMatrix;
        }
          
        public int[,] ApplyThreeByThreeConvolutionFilter(int[,] matrix, int[,] filter)
        {
            int width = matrix.GetLength(1);
            int height = matrix.GetLength(0);
            // Ensure the filter is 3x3
            if (filter.GetLength(0) != 3 || filter.GetLength(1) != 3)
            {
                throw new ArgumentException("Filter must be a 3x3 matrix.");
            }

            int[,] result = new int[height, width];

            // Iterate through the matrix, excluding the border
            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int pixelValue = 0;

                    // Apply the filter to the current pixel and its neighbors
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            pixelValue += matrix[y + ky, x + kx] * filter[ky + 1, kx + 1];
                        }
                    }

                    // Assign the computed value to the result matrix
                    result[y, x] = pixelValue;
                }
            }

            return result;
        }

        public int[,] NormalizeOrScale(int[,] resultMatrix, int targetMin = 0, int targetMax = 255)
        {
            int width = resultMatrix.GetLength(1);
            int height = resultMatrix.GetLength(0);
            int[,] normalizedMatrix = new int[height, width];

            // Find min and max values in the result matrix
            int minVal = int.MaxValue;
            int maxVal = int.MinValue;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (resultMatrix[y, x] < minVal) minVal = resultMatrix[y, x];
                    if (resultMatrix[y, x] > maxVal) maxVal = resultMatrix[y, x];
                }
            }

            // Scale and normalize the values
            double range = maxVal - minVal;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (range > 0)
                    {
                        normalizedMatrix[y, x] = (int)(((resultMatrix[y, x] - minVal) / range) * (targetMax - targetMin) + targetMin);
                    }
                    else
                    {
                        normalizedMatrix[y, x] = targetMin;
                    }
                }
            }

            return normalizedMatrix;
        }



        public int[,] GetFilter(FilterType filterType)
        {
            switch (filterType)
            {
                case FilterType.SobelVertical:
                    return new int[,]
                    {
                        { -1, 0, 1 },
                        { -2, 0, 2 },
                        { -1, 0, 1 }
                    };

                case FilterType.SobelHorizontal:
                    return new int[,]
                    {
                        { -1, -2, -1 },
                        { 0, 0, 0 },
                        { 1, 2, 1 }
                    };

                case FilterType.Identity:
                    return new int[,]
                    {
                        { 0, 0, 0 },
                        { 0, 1, 0 },
                        { 0, 0, 0 }
                    };

                case FilterType.Blur:
                    return new int[,]
                    {
                        { 1, 1, 1 },
                        { 1, 1, 1 },
                        { 1, 1, 1 }
                    };

                case FilterType.GaussianBlur:
                    return new int[,]
                    {
                        { 1, 2, 1 },
                        { 2, 4, 2 },
                        { 1, 2, 1 }
                    };

                case FilterType.Sharpen:
                    return new int[,]
                    {
                        { 0, -1, 0 },
                        { -1, 5, -1 },
                        { 0, -1, 0 }
                    };

                case FilterType.PrewittVertical:
                    return new int[,]
                    {
                        { -1, 0, 1 },
                        { -1, 0, 1 },
                        { -1, 0, 1 }
                    };

                case FilterType.PrewittHorizontal:
                    return new int[,]
                    {
                        { 1, 1, 1 },
                        { 0, 0, 0 },
                        { -1, -1, -1 }
                    };

                case FilterType.Laplacian:
                    return new int[,]
                    {
                        { 0, 1, 0 },
                        { 1, -4, 1 },
                        { 0, 1, 0 }
                    };

                case FilterType.HighPass:
                    return new int[,]
                    {
                        { -1, -1, -1 },
                        { -1, 8, -1 },
                        { -1, -1, -1 }
                    };
                case FilterType.LowPass:
                    // Simplified example, similar to a blur
                    return new int[,]
                    {
                        { 1, 1, 1 },
                        { 1, 1, 1 },
                        { 1, 1, 1 }
                    };

                case FilterType.ScharrVertical:
                    return new int[,]
                    {
                        { -3, 0, 3 },
                        { -10, 0, 10 },
                        { -3, 0, 3 }
                    };

                case FilterType.ScharrHorizontal:
                    return new int[,]
                    {
                        { -3, -10, -3 },
                        { 0, 0, 0 },
                        { 3, 10, 3 }
                    };

                case FilterType.UnsharpMasking:
                    // Example using a simple sharpening filter to illustrate the concept
                    return new int[,]
                    {
                        { -1, -1, -1 },
                        { -1, 9, -1 },
                        { -1, -1, -1 }
                    };
                case FilterType.BoxBlur:
                    return new int[,]
                    {
                        { 1, 1, 1 },
                        { 1, 1, 1 },
                        { 1, 1, 1 }
                    };

                case FilterType.MotionBlur: // Simplified example for horizontal motion blur
                    return new int[,]
                    {
                        { 1, 0, 0 },
                        { 0, 1, 0 },
                        { 0, 0, 1 }
                    };

                //case FilterType.RobertsCross: // Not directly applicable as a single matrix
                //case FilterType.Median: // Requires a specific algorithm for median calculation
                //case FilterType.Bilateral: // Not implementable as a simple kernel operation
                //case FilterType.Canny: // Multi-step process, not a single kernel
                default:
                    throw new NotImplementedException(
                        $"Filter type '{filterType}' is not implemented or not applicable as a simple kernel.");
            }
        }

        public enum FilterType
        {
            [Description("Detects vertical edges")]
            SobelVertical,  

            [Description("Detects horizontal edges")]
            SobelHorizontal,  

            [Description("Leaves the image unchanged")]
            Identity,   

            [Description("Applies a simple averaging blur")]
            Blur,  

            [Description("Applies a Gaussian smoothing")]
            GaussianBlur,  

            [Description("Enhances edges and details")]
            Sharpen,   

            [Description("Detects vertical edges using the Prewitt operator")]
            PrewittVertical,  

            [Description("Detects horizontal edges using the Prewitt operator")]
            PrewittHorizontal,  

            [Description("Highlights regions of rapid intensity change")]
            Laplacian,   

            [Description("Enhances high-frequency parts of the image")]
            HighPass,  

            [Description("Averages out rapid changes in intensity, reducing noise")]
            LowPass,  

            [Description("Detects vertical edges using the Scharr operator")]
            ScharrVertical, 

            [Description("Detects horizontal edges using the Scharr operator")]
            ScharrHorizontal,   

            [Description("Sharpens by subtracting a blurred version of the image")]
            UnsharpMasking,  

            [Description("Applies a uniform blur over a box-shaped neighborhood")]
            BoxBlur,  

            [Description("Simulates the effect of movement, blurring the image in a specific direction")]
            MotionBlur  


            //RobertsCross, // Detects edges using the Roberts Cross operator
            //Median, // Reduces noise without significantly blurring edges
            //Bilateral, // Reduces noise while preserving edges, using spatial and intensity differences
            //Canny, // Multi-step edge detector that uses gradient and non-maximum suppression
        }

        private async Task Reset()
        {
            currentStep = (-1, -1);
            imageMatrix = await CreateImageMatrix(); 
            Array.Copy(imageMatrix, outputMatrix, imageMatrix.Length);

            await jsRuntime.InvokeVoidAsync("displayImageMatrix", "imageTable", SerializedMatrix(imageMatrix));
            await jsRuntime.InvokeVoidAsync("displayImageMatrix", "processedImageTable", SerializedMatrix(outputMatrix));

            await jsRuntime.InvokeVoidAsync("resetColors", "imageTable");
            await jsRuntime.InvokeVoidAsync("resetColors", "processedImageTable");

        }
    }
}