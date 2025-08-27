using System.Diagnostics;
using System.IO;

namespace DescriptionFixer.Helpers
{
    public static class FfmpegHelper
    {
        public static string GetFfmpegPath(DescriptionFixerSettings settings)
        {
            // 1. Use the user-specified path if it exists
            if (!string.IsNullOrWhiteSpace(settings.FFmpegPath) && File.Exists(settings.FFmpegPath))
            {
                return settings.FFmpegPath;
            }

            // 2. Check PATH
            if (IsExecutableAvailable("ffmpeg"))
            {
                return "ffmpeg"; // can run from PATH
            }

            throw new FileNotFoundException(
                "ffmpeg executable not found. Please set FFmpegPath in plugin settings.  FFmpeg can be downloaded from https://www.gyan.dev/ffmpeg/builds/. The latest stable release is at https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
            );
        }

        public static string GetFfprobePath(DescriptionFixerSettings settings)
        {
            if (!string.IsNullOrWhiteSpace(settings.FFprobePath) && File.Exists(settings.FFprobePath))
            {
                return settings.FFprobePath;
            }

            if (IsExecutableAvailable("ffprobe"))
            {
                return "ffprobe";
            }

            throw new FileNotFoundException(
                "ffprobe executable not found. Please set FFprobePath in plugin settings. FFprobe can be downloaded from https://www.gyan.dev/ffmpeg/builds/. The latest stable release is at https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"
            );
        }

        private static bool IsExecutableAvailable(string exeName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exeName,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    process.WaitForExit(3000); // wait up to 3 seconds
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
