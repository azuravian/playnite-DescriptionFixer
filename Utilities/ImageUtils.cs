using ImageMagick;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DescriptionFixer.Utilities
{
    internal static class ImageUtils
    {
        public static async Task<MagickImage> GetAvifData(string avifPath)
        {
            using (var httpClient = new HttpClient())
            {
                var data = await httpClient.GetByteArrayAsync(avifPath);
                return new MagickImage(data);
            }
        }
        public static string ConvertImage(MagickImage image, uint quality, string format, string datapath, Game game)
        {
            switch (format.ToLowerInvariant())
            {
                case "webp":
                    image.Format = MagickFormat.WebP;
                    break;
                case "png":
                    image.Format = MagickFormat.Png;
                    break;
                case "jpg":
                    image.Format = MagickFormat.Jpeg;
                    break;
                default:
                    throw new NotSupportedException($"Unsupported image format: {format}");
            }

            image.Quality = quality;
            string fileName = $"{Path.GetRandomFileName()}.{format}";
            string directory = Path.Combine(datapath, game.Id.ToString());
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            string fullPath = Path.Combine(directory, fileName);
            image.WriteAsync(fullPath);
            return fullPath;
        }

        public static bool IsImageTransparent(string imagePath, uint transThreshold)
        {
            using (var image = new MagickImage(imagePath))
            {
                // Check if the image has an alpha channel and if any pixel is transparent
                if (image.HasAlpha && image.GetPixels().Any(p => p.GetChannel(3) < transThreshold))
                {
                    return true; // Image is transparent
                }
            }
            return false; // Image is not transparent
        }

        public static async Task<uint> GetGifFrameCount(string imagePath)
        {
            uint totalFrames = 0;
            
            var ffprobe = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -count_frames -select_streams v:0 -show_entries stream=nb_read_frames -of default=nokey=1:noprint_wrappers=1 \"{imagePath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(ffprobe))
            {
                string output = await process.StandardOutput.ReadToEndAsync();
                process.WaitForExit();
                totalFrames = uint.Parse(output.Trim());
            }

            return totalFrames;
        }

        public static async Task<List<string>> ExtractGifFramesAsync(string gifPath, uint frameCount, ILogger logger)
        {
            string outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(outputDir);
            var extractedFrames = new List<string>();

            // Get GIF frame count using ffprobe
            uint totalFrames = 0;
            try
            {
                totalFrames = await GetGifFrameCount(gifPath);
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to get GIF frame count: {ex.Message}");
                return extractedFrames;
            }

            if (totalFrames == 0)
            {
                logger.Warn("GIF contains no frames.");
                return extractedFrames;
            }

            // Calculate indices of frames to extract
            var frameIndices = Enumerable.Range(0, (int)frameCount)
                                         .Select(i => (int)((i * totalFrames) / (double)frameCount))
                                         .Distinct()
                                         .ToList();

            var tasks = new List<Task>();

            foreach (int index in frameIndices)
            {
                string outputFile = Path.Combine(outputDir, $"frame_{index + 1:D3}.png");

                tasks.Add(Task.Run(() =>
                {
                    var ffmpeg = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-i \"{gifPath}\" -vf \"select=eq(n\\,{index})\" -frames:v 1 \"{outputFile}\" -y",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (var process = Process.Start(ffmpeg))
                    {
                        string stdError = process.StandardError.ReadToEnd();
                        string stdOutput = process.StandardOutput.ReadToEnd();
                        process.WaitForExit();

                        if (process.ExitCode == 0)
                        {
                            lock (extractedFrames)
                            {
                                extractedFrames.Add(outputFile);
                                logger.Info($"Extracted frame {index} to {outputFile}");
                            }
                        }
                        else
                        {
                            logger.Error($"Failed to extract frame {index}: {stdError}");
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return extractedFrames;
        }

    }
}
