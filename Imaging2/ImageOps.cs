using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Numpy;
using Numpy.Models;
using OpenCvSharp;
using System;

namespace ImagingOps
{
    public static class ImageOps
    {
        public static Mat LoadImage(Stream stream)
        {
            var image = Mat.FromStream(stream, ImreadModes.Grayscale);
            return image;
        }

        public static Mat LoadImage(string fileName)
        {
            var image = Cv2.ImRead(fileName, ImreadModes.Grayscale);
            // image.ConvertTo(image, MatType.CV_32SC1);
            var tt = image.Type();
            return image;
        }

        public static Mat LoadImage(byte[] data, ImreadModes mode = ImreadModes.Grayscale)
        {
            var image = Mat.FromImageData(data, mode);
            // image.ConvertTo(image, MatType.CV_32SC1);
            var tt = image.Type();
            return image;
        }
        // public static Mat ConvertToGrayscale(Mat image)
        // {
        //     return image.CvtColor(ColorConversionCodes.BGR2GRAY);
        // }

        public static NDarray ConvertToGrayscale(NDarray rgb)
        {
            return rgb.ToMat().CvtColor(ColorConversionCodes.BGR2GRAY).ToNDArray();

            var r = rgb[":,:, 2"];
            var g = rgb[":,:, 1"];
            var b = rgb[":,:, 0"];
            var gray = 0.2989 * r + 0.5870 * g + 0.1140 * b;
            return gray.astype(np.int32);
        }

        public static NDarray ConvertMatToNDarray(Mat image)
        {
            // Ensure the image is in the expected format
            if (image.Type() != MatType.CV_8UC3)
            {
                throw new InvalidOperationException("Unsupported Mat type. Expected CV_8UC3.");
            }

            int rows = image.Rows;
            int cols = image.Cols;
            int channels = image.Channels();
            byte[,,] imageDataArray = new byte[rows, cols, channels];

            // Use the GetGenericIndexer to access pixel values
            var indexer = image.GetGenericIndexer<Vec3b>();

            for (int row = 0; row < rows; row++)
            {
                for (int col = 0; col < cols; col++)
                {
                    Vec3b color = indexer[row, col];
                    imageDataArray[row, col, 0] = color.Item0; // B
                    imageDataArray[row, col, 1] = color.Item1; // G
                    imageDataArray[row, col, 2] = color.Item2; // R
                }
            }

            // Convert the 3D byte array to an NDarray
            var ndArray = np.array(imageDataArray);
            return ndArray;
        }



        //
        // public static NDarray ConvertMatToNDarray(Mat image)
        // {
        //     // Assuming the image is an 8-bit, 3-channel image (common case)
        //     Mat imageConverted = new Mat();
        //     image.ConvertTo(imageConverted, MatType.CV_8UC3);
        //     var indexer = new MatOfByte3(imageConverted).GetIndexer();
        //
        //     byte[,,] imageDataArray = new byte[image.Rows, image.Cols, 3];
        //     for (int row = 0; row < image.Rows; row++)
        //     {
        //         for (int col = 0; col < image.Cols; col++)
        //         {
        //             Vec3b pixel = indexer[row, col];
        //             imageDataArray[row, col, 0] = pixel.Item0; // B
        //             imageDataArray[row, col, 1] = pixel.Item1; // G
        //             imageDataArray[row, col, 2] = pixel.Item2; // R
        //         }
        //     }
        //
        //     var ndArray = np.array(imageDataArray);
        //     return ndArray;
        // }
        public static byte[] FlattenArrayToBytes(this int[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            byte[] flat = new byte[rows * cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    // Ensure the value fits within a byte; clamp or adjust as necessary.
                    int value = array[i, j];
                    flat[i * cols + j] = (byte)Math.Max(0, Math.Min(value, 255));
                }
            }
            return flat;
        }

        public static int[] FlattenArray(this int[,] array)
        {
            int rows = array.GetLength(0);
            int cols = array.GetLength(1);
            int[] flat = new int[rows * cols];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    flat[i * cols + j] = array[i, j];
                }
            }
            return flat;
        }

        public static int[,] ToIntArray(this Mat image)
        {
            image.ConvertTo(image, MatType.CV_32SC1); 
            image.GetArray(out int[] flatArray);

            int[,] reshapedArray = new int[image.Rows, image.Cols];
            for (int i = 0; i < flatArray.Length; i++)
            {
                int row = i / image.Cols;
                int col = i % image.Cols;
                reshapedArray[row, col] = flatArray[i];
            }

            return reshapedArray;
        }

        public static NDarray GetNdIntArray(Mat image)
        {
            // var newImage = image.EmptyClone();
            var newImage = new Mat(new Size(image.Cols, image.Rows), MatType.CV_32SC1);

            image.ConvertTo(newImage, MatType.CV_32SC1);
            newImage.GetArray(out int[] imageDataArray);

            try
            {
                var imageArray = np.array(imageDataArray).reshape(image.Rows, image.Cols);
           
                return imageArray;

            }
            catch (Exception ex)
            {
                return null;
            }
        }
        //public static NDarray ToNDArray(this Mat mat)
        //{
        //    var matType = mat.Type();
        //    var channels = mat.Channels();
        //    var size = mat.Rows * mat.Cols * channels;
        //    var shape = channels == 1 ? new Shape(mat.Rows, mat.Cols) : new Shape(mat.Rows, mat.Cols, channels);
        //    if (matType == MatType.CV_32SC1 || matType == MatType.CV_32SC2)
        //    {
        //        var managedArray = new int[size];
        //        Marshal.Copy(mat.Data, managedArray, 0, size);
        //        var aslice = ArraySlice.FromArray(managedArray);
        //        return new NDarray(aslice, shape);
        //    }
        //    if (matType == MatType.CV_32FC1)
        //    {
        //        var managedArray = new float[size];
        //        Marshal.Copy(mat.Data, managedArray, 0, size);
        //        var aslice = ArraySlice.FromArray(managedArray);
        //        return new NDarray(aslice, shape);
        //    }
        //    if (matType == MatType.CV_64FC1)
        //    {
        //        var managedArray = new double[size];
        //        Marshal.Copy(mat.Data, managedArray, 0, size);
        //        var aslice = ArraySlice.FromArray(managedArray);
        //        return new NDarray(aslice, shape);
        //    }
        //    if (matType == MatType.CV_8UC1 || matType == MatType.CV_8UC3 || matType == MatType.CV_8UC4)
        //    {
        //        var managedArray = new byte[size];
        //        Marshal.Copy(mat.Data, managedArray, 0, size);
        //        var aslice = ArraySlice.FromArray(managedArray);
        //        return new NDarray(aslice, shape);
        //    }

        //    throw new Exception($"mat data type = {matType} is not supported");
        //} 
        public static NDarray ToNDArray(this Mat mat)
        {
            var matType = mat.Type();
            var channels = mat.Channels();
            var size = mat.Rows * mat.Cols * channels;
            var shape = channels == 1 ? new Shape(mat.Rows, mat.Cols) : new Shape(mat.Rows, mat.Cols, channels);

            try
            { 
                if (matType == MatType.CV_32SC1 || matType == MatType.CV_32SC2)
                {
                    var managedArray = new int[size];
                    Marshal.Copy(mat.Data, managedArray, 0, size);
                    return np.array(managedArray).reshape(shape);
                }

                if (matType == MatType.CV_32FC1)
                {
                    var managedArray = new float[size];
                    Marshal.Copy(mat.Data, managedArray, 0, size);
                    return np.array(managedArray).reshape(shape);
                }

                if (matType == MatType.CV_64FC1)
                {
                    var managedArray = new double[size];
                    Marshal.Copy(mat.Data, managedArray, 0, size);
                    return np.array(managedArray).reshape(shape);
                }

                if (matType == MatType.CV_8UC1 || matType == MatType.CV_8UC3 || matType == MatType.CV_8UC4)
                {
                    var managedArray = new byte[size];
                    Marshal.Copy(mat.Data, managedArray, 0, size);
                    var arr = np.array(managedArray, dtype: np.@byte).reshape(shape);
                    return arr;
                } 
            }
            catch (Exception ex)
            {
                string s = ex.Message;
            }

            throw new Exception($"I told you mat data type = {matType} is not supported");
        }


        //public static unsafe NDarray ToNDArrayUnsafe(this Mat mat)
    //{
    //    var matType = mat.Type();
    //    var channels = mat.Channels();
    //    var size = mat.Rows * mat.Cols * channels;
    //    var shape = channels == 1 ? new Shape(mat.Rows, mat.Cols) : new Shape(mat.Rows, mat.Cols, channels);
    //    if (matType == MatType.CV_32SC1 || matType == MatType.CV_32SC2)
    //    {
    //        int* ptr = (int*)mat.DataPointer;
    //        var block = new UnmanagedMemoryBlock<int>(ptr, shape.Size, () => DoNothing(IntPtr.Zero));
    //        var storage = new UnmanagedStorage(new ArraySlice<int>(block), shape);
    //        return new NDArray(storage);
    //    }
    //    if (matType == MatType.CV_32FC1)
    //    {
    //        float* ptr = (float*)mat.DataPointer;
    //        var block = new UnmanagedMemoryBlock<float>(ptr, shape.Size, () => DoNothing(IntPtr.Zero));
    //        var storage = new UnmanagedStorage(new ArraySlice<float>(block), shape);
    //        return new NDArray(storage);
    //    }
    //    if (matType == MatType.CV_64FC1)
    //    {
    //        double* ptr = (double*)mat.DataPointer;
    //        var block = new UnmanagedMemoryBlock<double>(ptr, shape.Size, () => DoNothing(IntPtr.Zero));
    //        var storage = new UnmanagedStorage(new ArraySlice<double>(block), shape);
    //        return new NDArray(storage);
    //    }
    //    if (matType == MatType.CV_8UC1 || matType == MatType.CV_8UC3 || matType == MatType.CV_8UC4)
    //    {
    //        byte* ptr = (byte*)mat.DataPointer;
    //        var block = new UnmanagedMemoryBlock<byte>(ptr, shape.Size, () => DoNothing(IntPtr.Zero));
    //        var storage = new UnmanagedStorage(new ArraySlice<byte>(block), shape);
    //        return new NDArray(storage);
    //    }

    //    throw new Exception($"mat data type = {matType} is not supported");
    //}

    [MethodImpl(MethodImplOptions.NoOptimization)]
        private static void DoNothing(IntPtr ptr)
        {
            var p = ptr;
        }

        public static Mat ToMat(this NDarray nDArray)
        {
            var t = nDArray.GetMatType();
            return new Mat(nDArray.shape[0], nDArray.shape[1], nDArray.GetMatType(), (Array)nDArray.GetData<byte>());
        }
            

        //public unsafe static Mat ToMatUnsafe(this NDarray nDArray) =>
        //    new Mat(nDArray.shape[0], nDArray.shape[1], nDArray.GetMatType(), new IntPtr(nDArray.Unsafe.Address));

        public static MatType GetMatType(this NDarray nDArray)
        {
            int channels = nDArray.ndim == 3 ? nDArray.shape[2] : 1;
            MatType matType;

            if (nDArray.dtype.Equals(np.int32))
            {
                if (channels == 1)
                {
                    matType = MatType.CV_32SC1;
                }
                else if (channels == 2)
                {
                    matType = MatType.CV_32SC2;
                }
                else
                {
                    throw new ArgumentException($"nDArray data type = {nDArray.dtype} & channels = {channels} is not supported");
                }
            }
            else if (nDArray.dtype.Equals(np.float_))
            {
                if (channels == 1)
                {
                    matType = MatType.CV_32FC1;
                }
                else
                {
                    throw new ArgumentException($"nDArray data type = {nDArray.dtype} & channels = {channels} is not supported");
                }
            }
            else if (nDArray.dtype.Equals(np.@double))
            {
                if (channels == 1)
                {
                    matType = MatType.CV_64FC1;
                }
                else
                {
                    throw new ArgumentException($"nDArray data type = {nDArray.dtype} & channels = {channels} is not supported");
                }
            }
            else if (nDArray.dtype.Equals(np.@byte))
            {
                if (channels == 1)
                {
                    matType = MatType.CV_8UC1;
                }
                else if (channels == 3)
                {
                    matType = MatType.CV_8UC3;
                }
                else if (channels == 4)
                {
                    matType = MatType.CV_8UC4;
                }
                else
                {
                    throw new ArgumentException($"nDArray data type = {nDArray.dtype} & channels = {channels} is not supported");
                }
            }
            else if (nDArray.dtype.Equals(np.uint8))
            {
                if (channels == 1)
                {
                    matType = MatType.CV_8UC1;
                }
                else if (channels == 3)
                {
                    matType = MatType.CV_8UC3;
                }
                else if (channels == 4)
                {
                    matType = MatType.CV_8UC4;
                }
                else
                {
                    throw new ArgumentException($"nDArray data type = {nDArray.dtype} & channels = {channels} is not supported");
                }
            }
            else
            {
                throw new ArgumentException($"nDArray data type = {nDArray.dtype} is not supported");
            }

            return matType;
        }

        //public static NDarray Resizea(NDarray image, int width, int height)
        //{
        //    NDarray resizedImage = Resize(image, new Shape(height, width),);

        //}

        public static Mat Resize(Mat image, int width, int height)
        {
            var size = new Size(width, height);
            var resized = image.Clone();
            Cv2.Resize(image, resized, size, interpolation: InterpolationFlags.Area);
            return resized;
        }

        public static Mat Resize(Mat image, double scale_percent)
        {

            var width = Math.Floor(image.Cols * scale_percent / 100);
            var height = Math.Floor(image.Rows * scale_percent / 100);

            var size = new Size(width, height);
            var resized = image.Clone();
            Cv2.Resize(image, resized, size, interpolation: InterpolationFlags.Area);
            return resized;
        }
         
        public static string MatToBase64(Mat image, string imageFormat = ".jpg")
        {
            if (image.Empty()) throw new ArgumentException("Cannot convert an empty Mat.", nameof(image));

            // Convert Mat to byte array
            Cv2.ImEncode(imageFormat, image, out var imageData);

            // Convert byte array to Base64 String
            return Convert.ToBase64String(imageData);
        }
    }
}