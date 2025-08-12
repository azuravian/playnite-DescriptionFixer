using DescriptionFixer.Utilities;
using DescriptionFixer.Views;
using Microsoft.SqlServer.Server;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DescriptionFixer.Services
{
    internal class VideoService
    {
        private readonly DescriptionFixerSettings settings;
        private readonly ILogger logger;
        private readonly string dataPath;
        private readonly IPlayniteAPI playniteAPI;

        public VideoService(DescriptionFixerSettings settings, ILogger logger, string dataPath, IPlayniteAPI playniteAPI)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataPath = dataPath;
            this.playniteAPI = playniteAPI;
        }

        public async Task<string> ProcessVideos(Game game, string html)
        {
            var videos = HtmlParser.ExtractVideoTags(html);
            foreach (var videoTag in videos)
            {
                // Check if the video tag has a source attribute
                var srcIndex = videoTag.IndexOf("src=\"", StringComparison.OrdinalIgnoreCase);
                if (srcIndex != -1)
                {
                    srcIndex += 5; // Move past 'src="'
                    var endIndex = videoTag.IndexOf("\"", srcIndex);
                    if (endIndex != -1)
                    {
                        var videoUrl = videoTag.Substring(srcIndex, endIndex - srcIndex);
                        logger.Info($"Found video URL: {videoUrl}");

                        var window = playniteAPI.Dialogs.CreateWindow(new WindowCreationOptions
                        {
                            ShowCloseButton = true,
                            ShowMaximizeButton = false,
                            ShowMinimizeButton = false
                        });

                        var frames = await VideoUtils.ExtractFramesAsync(videoUrl, settings.FrameCount, logger);
                        var frameSelection = new FrameSelectionControl(frames);
                        window.Content = frameSelection;
                        window.Title = "Select Frame";
                        window.SizeToContent = SizeToContent.WidthAndHeight;

                        if (window.ShowDialog() == true)
                        {
                            var selectedFrame = frameSelection.SelectedFramePath;
                            string fileName = Path.GetFileName(selectedFrame);
                            string directory = Path.Combine(dataPath, game.Id.ToString());
                            if (!Directory.Exists(directory))
                            {
                                Directory.CreateDirectory(directory);
                            }
                            string fullPath = Path.Combine(directory, fileName);
                            File.Copy(selectedFrame, fullPath, true); // Copy the selected frame to the game directory
                            html = HtmlParser.ReplaceVideoWithImage(html, videoTag, fullPath);
                        }
                        else
                        {
                            // User cancelled
                        }
                    }
                }
            }
            return html; // Return the modified HTML
        }
    }
}
