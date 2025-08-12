using ImageMagick;
using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptionFixer.Utilities
{
    internal class VideoUtils
    {
        public static double GetVideoDuration(string videoPath)
        {
            var ffprobe = new ProcessStartInfo
            {
                FileName = "ffprobe",
                Arguments = $"-v error -show_entries format=duration -of default=noprint_wrappers=1:nokey=1 \"{videoPath}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = Process.Start(ffprobe))
            {
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();
                if (double.TryParse(output, NumberStyles.Any, CultureInfo.InvariantCulture, out var duration))
                {
                    return duration;
                }
                throw new InvalidOperationException("Failed to get video duration.");
            }
        }

        public static async Task<List<string>> ExtractFramesAsync(string videoPath, uint frameCount, ILogger logger)
        {
            string outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(outputDir);
            var extractedFrames = new List<string>();
            double duration = GetVideoDuration(videoPath); // Still sync here

            var tasks = new List<Task>();

            for (int i = 0; i < frameCount; i++)
            {
                double timestamp = (duration * i) / frameCount;
                string outputFile = Path.Combine(outputDir, $"frame_{i + 1:D3}.png");

                tasks.Add(Task.Run(() =>
                {
                    var ffmpeg = new ProcessStartInfo
                    {
                        FileName = "ffmpeg",
                        Arguments = $"-vcodec libvpx-vp9 -ss {timestamp.ToString(CultureInfo.InvariantCulture)} -i \"{videoPath}\" -frames:v 1 -pix_fmt yuva420p \"{outputFile}\" -y",
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
                                logger.Info($"Extracted frame at {timestamp} seconds to {outputFile}");
                            }
                        }
                        else
                        {
                            logger.Error($"Failed to extract frame at {timestamp} seconds: {stdError}");
                            // Optionally log the error
                        }
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return extractedFrames;
        }


    }
}
