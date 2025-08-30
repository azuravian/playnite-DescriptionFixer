using DescriptionFixer.Helpers;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

namespace DescriptionFixer.Utilities
{
    internal class VideoUtils
    {
        public static double GetVideoDuration(string videoPath)
        {
            var settings = DescriptionFixer.Instance.SettingsVM.Settings;
            var ffprobePath = FfmpegHelper.GetFfprobePath(settings);
            var ffprobe = new ProcessStartInfo
            {
                FileName = ffprobePath,
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

        public static List<string> ExtractFrames(Game game, string videoPath, uint frameCount, ILogger logger)
        {
            var settings = DescriptionFixer.Instance.SettingsVM.Settings;
            
            string outputDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(outputDir);
            var extractedFrames = new List<string>();
            double duration = GetVideoDuration(videoPath); // Still sync here

            var options = new GlobalProgressOptions(
                $"{game.Name}: Extracting frames from video...",
                true
            )
            {
                IsIndeterminate = false
            };

            API.Instance.Dialogs.ActivateGlobalProgress(progress =>
            {
                progress.ProgressMaxValue = frameCount;
                for (int i = 0; i < frameCount; i++)
                {
                    progress.CurrentProgressValue = i + 1;
                    double timestamp = (duration * i) / frameCount;
                    string outputFile = Path.Combine(outputDir, $"frame_{i + 1:D3}.png");

                    var ffmpegPath = FfmpegHelper.GetFfmpegPath(settings);
                    var ffmpeg = new ProcessStartInfo
                    {
                        FileName = ffmpegPath,
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
                }
            }, options);
            return extractedFrames;
        }
    }
}