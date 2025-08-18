using DescriptionFixer.Utilities;
using DescriptionFixer.Views;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.IO;
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
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);

            var videoNodes = doc.DocumentNode.SelectNodes("//video");
            if (videoNodes == null) return html; // no videos

            for (var i = 0; i < videoNodes.Count; i++)
            {
                var node = videoNodes[i];
                var sourceNode = node.SelectSingleNode(".//source[@src]");
                if (sourceNode == null) continue;

                string videoUrl = sourceNode.GetAttributeValue("src", null);
                if (string.IsNullOrEmpty(videoUrl)) continue;

                logger.Info($"Found video URL: {videoUrl}");

                // Show frame selection window
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
                    string fileName = $"video_{i}_{Path.GetFileName(selectedFrame)}";
                    string directory = Path.Combine(dataPath, game.Id.ToString());
                    Directory.CreateDirectory(directory);
                    string fullPath = Path.Combine(directory, fileName);
                    File.Copy(selectedFrame, fullPath, true);

                    // Replace the <video> node with an <img> node
                    var imgNode = doc.CreateElement("img");
                    imgNode.SetAttributeValue("src", fullPath);
                    node.ParentNode.ReplaceChild(imgNode, node);
                }
                else
                {
                    // User cancelled
                    continue;
                }
            }

            return doc.DocumentNode.OuterHtml;
        }
    }
}
