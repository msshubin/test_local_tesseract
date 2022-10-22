using OpenCvSharp;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace test_local_tesseract
{
    internal class Program
    {
        const string baseDir = @"C:\temp\cap\";
        static void Main(string[] args)
        {
            
            Console.OutputEncoding = Encoding.UTF8;

            
            var files = Directory.GetFiles(baseDir);
            foreach(var filePath in files)
            {
                var start = DateTime.Now;
                var newFilePath = ConvertImage(filePath);
                //var newFilePath = ModifyImage(filePath);
                var result = ResolveImage(newFilePath);
                Console.WriteLine($"   {(DateTime.Now - start).TotalMilliseconds} ms");
            }
            //var filePath = @"C:\temp\captcha\8с8нм.jpg";
            
            Console.ReadKey();
        }

        static string ConvertImage(string filePath)
        {
            var newFile = $"{baseDir}\\im\\{Path.GetFileNameWithoutExtension(filePath)}_{Path.GetExtension(filePath)}";
            Process process = new Process();
            process.StartInfo.FileName = @"C:\Program Files\ImageMagick-7.1.0-Q16-HDRI\convert.exe";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = @"C:\Program Files\ImageMagick-7.1.0-Q16-HDRI";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            //process.StartInfo.Arguments = $"{filePath} -colorspace Gray -blur 0 -level 0,60% {newFile}";
            //process.StartInfo.Arguments = $"{filePath} -colorspace Gray -gaussian-blur 0 -threshold 60% -paint 1 -fill black -draw \"color 0,0 floodfill\" {newFile}";
            process.StartInfo.Arguments = $"{filePath} -colorspace Gray -gaussian-blur 0 -threshold 60% -paint 1 -fill black -draw \"color 0,0 floodfill\" {newFile}";
            // fill black надо делать, если фон черный, иначе - fill white!
            process.Start();
            var outputStr = process.StandardOutput.ReadToEnd();
            Console.WriteLine($"convert {filePath} -> {newFile}");
            process.WaitForExit();
            return newFile;
        }

        static string ModifyImage(string filePath)
        {
            var newFile = $"{baseDir}\\cv\\{Path.GetFileNameWithoutExtension(filePath)}_{Path.GetExtension(filePath)}";
            var src = new Mat(filePath, ImreadModes.Grayscale);
            var dst = new Mat();

            //Cv2.Canny(src, dst, 50, 200);
            //Cv2.Threshold(src,dst,)
            Cv2.Threshold(src, dst, 255, 255, ThresholdTypes.Otsu);

            dst.SaveImage(newFile);
            Console.WriteLine($"cv modify {filePath} -> {newFile}");
            return newFile;
        }

        static string ModifyImage2(string filePath)
        {
            var newFile = $"{baseDir}\\cv\\{Path.GetFileNameWithoutExtension(filePath)}_{Path.GetExtension(filePath)}";
            // load the file
            using (var src = new Mat(filePath))
            {
                using (var binaryMask = new Mat())
                {
                    // lines color is different than text
                    var linesColor = Scalar.FromRgb(0x70, 0x70, 0x70);

                    // build a mask of lines
                    Cv2.InRange(src, linesColor, linesColor, binaryMask);
                    using (var masked = new Mat())
                    {
                        // build the corresponding image
                        // dilate lines a bit because aliasing may have filtered borders too much during masking
                        src.CopyTo(masked, binaryMask);
                        new Window("dst image", masked);
                        Console.ReadKey();
                        int linesDilate = 3;
                        using (var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(linesDilate, linesDilate)))
                        {
                            Cv2.Dilate(masked, masked, element);
                        }

                        // convert mask to grayscale
                        Cv2.CvtColor(masked, masked, ColorConversionCodes.BGR2GRAY);
                        using (var dst = src.EmptyClone())
                        {
                            // repaint big lines
                            Cv2.Inpaint(src, masked, dst, 3, InpaintMethod.NS);

                            // destroy small lines
                            linesDilate = 2;
                            using (var element = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(linesDilate, linesDilate)))
                            {
                                Cv2.Dilate(dst, dst, element);
                            }

                            Cv2.GaussianBlur(dst, dst, new Size(5, 5), 0);
                            using (var dst2 = dst.BilateralFilter(5, 75, 75))
                            {
                                // basically make it B&W
                                Cv2.CvtColor(dst2, dst2, ColorConversionCodes.BGR2GRAY);
                                Cv2.Threshold(dst2, dst2, 255, 255, ThresholdTypes.Otsu);

                                // save the file
                                dst2.SaveImage(newFile);
                            }
                        }
                    }
                }
            }
            Console.WriteLine($"cv modify {filePath} -> {newFile}");
            return newFile;
        }

        static string ResolveImage(string file)
        {
            Process process = new Process();
            process.StartInfo.FileName = @"C:\Program Files\Tesseract-OCR\tesseract.exe";
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = @"C:\Program Files\Tesseract-OCR";
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
            //process.StartInfo.Arguments = $"\"{file}\" stdout --oem 0 -l rus --psm 8"; //-c tessedit_char_whitelist=ABCDEFGHIJKLMOPQRSTUVWXYZ
            //process.StartInfo.Arguments = $"\"{file}\" stdout --oem 0 -l rus -c tessedit_char_whitelist=2456789ЖМСПВДРТЛНГК --psm 8"; //
            process.StartInfo.Arguments = $"\"{file}\" stdout --oem 0 -l rus --psm 8"; //
            process.Start();
            var outputStr = process.StandardOutput.ReadToEnd();
            Console.WriteLine($"{file} Result: {outputStr}");
            process.WaitForExit();
            return outputStr;

        }
        // Идея - делать параллельно несколько видов преобразований через imagemagick, затем параллельно отправлять их на распознавание, затем выбирать подходящий?...
    }
}
