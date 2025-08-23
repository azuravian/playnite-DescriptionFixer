using DescriptionFixer.Utilities;
using DescriptionFixer.Views;
using ImageMagick;
using Playnite.SDK;
using Playnite.SDK.Models;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace DescriptionFixer.Services
{
    internal class ImageService
    {
        private readonly DescriptionFixerSettings settings;
        private readonly ILogger logger;
        private readonly string dataPath;
        private readonly IPlayniteAPI playniteAPI;

        public ImageService(DescriptionFixerSettings settings, ILogger logger, string dataPath, IPlayniteAPI playniteAPI)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataPath = dataPath;
            this.playniteAPI = playniteAPI;
        }

        public async Task<string> ProcessImages(Game game, string html)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.LoadHtml(html);
            var imgNodes = doc.DocumentNode.SelectNodes("//img");

            if (imgNodes == null) return html; // no images
            for (var i = 0; i < imgNodes.Count; i++)
            {
                var imgNode = imgNodes[i];
                var src = imgNode.Attributes["src"];
                if (src != null && src.Value.ToLower().Contains(".avif"))
                {
                    bool isTransparent = ImageUtils.IsImageTransparent(src.Value, settings.TransparencyThreshold);
                    if (isTransparent)
                    {
                        // Convert to PNG
                        logger.Info($"Converting AVIF image to PNG: {src.Value}");
                        
                        MagickImage avif = await ImageUtils.GetAvifData(src.Value);
                        string newImage = ImageUtils.ConvertImage(avif, settings.Quality, "png", dataPath, game);
                        // Replace src in html
                        imgNode.SetAttributeValue("src", newImage);
                    }
                    else
                    {
                        var format = settings.UseJpeg ? "jpg" : "webp";
                        switch (format)
                        {
                            case "webp":
                                // Convert to WebP
                                logger.Info($"Converting AVIF image to WebP: {src.Value}");
                                break;
                            case "jpg":
                                // Convert to JPG
                                logger.Info($"Converting AVIF image to JPG: {src.Value}");
                                break;
                            default:
                                continue;
                        }
                        MagickImage avif = await ImageUtils.GetAvifData(src.Value);
                        string newImage = ImageUtils.ConvertImage(avif, settings.Quality, format, dataPath, game);
                        imgNode.SetAttributeValue("src", newImage);
                    }
                }
                else if (src != null && src.Value.ToLower().Contains(".webp"))
                {
                    // Convert WebP to PNG
                    logger.Info($"Converting WebP image to PNG: {src.Value}");
                    MagickImage webp = await ImageUtils.GetAvifData(src.Value);
                    if (ImageUtils.IsImageTransparent(src.Value, settings.TransparencyThreshold))
                    {
                        logger.Info($"WebP image is transparent, converting to PNG: {src.Value}");
                        string pngImage = ImageUtils.ConvertImage(webp, settings.Quality, "png", dataPath, game);
                        imgNode.SetAttributeValue("src", pngImage);
                        continue;
                    }
                    else
                    {
                        // No need to convert if not transparent
                        logger.Info($"WebP image is not transparent, keeping as WebP: {src.Value}");
                    }
                }
                else if (src != null && src.Value.ToLower().Contains(".gif") && settings.ConvertGifs)
                {
                    // Convert GIF to single frame PNG
                    // Show frame selection window
                    var window = playniteAPI.Dialogs.CreateWindow(new WindowCreationOptions
                    {
                        ShowCloseButton = true,
                        ShowMaximizeButton = false,
                        ShowMinimizeButton = false
                    });

                    var frames = await ImageUtils.ExtractGifFramesAsync(src.Value, settings.FrameCount, logger);
                    var frameSelection = new FrameSelectionControl(frames);
                    window.Content = frameSelection;
                    window.Title = "Select Frame";
                    window.SizeToContent = SizeToContent.WidthAndHeight;

                    if (window.ShowDialog() == true)
                    {
                        var selectedFrame = frameSelection.SelectedFramePath;
                        string fileName = $"gif_{i}_{Path.GetFileName(selectedFrame)}";
                        string directory = Path.Combine(dataPath, game.Id.ToString());
                        Directory.CreateDirectory(directory);
                        string fullPath = Path.Combine(directory, fileName);
                        File.Copy(selectedFrame, fullPath, true);
                        imgNode.SetAttributeValue("src", fullPath);
                    }
                }
            }
            return doc.DocumentNode.OuterHtml;
        }
    }
}
