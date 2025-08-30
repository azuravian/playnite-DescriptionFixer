using DescriptionFixer.Utilities;
using DescriptionFixer.Views;
using HtmlAgilityPack;
using ImageMagick;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Xml;

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

        public Tuple<HtmlDocument, int, int, int, List<HtmlNode>> ProcessImages(Game game, string html)
        {
            int changesAvifToPng = 0;
            int changesAvifToWebp = 0;
            int changesWebpToPng = 0;
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var imgNodes = doc.DocumentNode.SelectNodes("//img");

            if (imgNodes == null) return Tuple.Create(doc, 0, 0, 0, new List<HtmlNode>()); // no images

            var options = new GlobalProgressOptions(
                $"{game.Name}: Processing Images...",
                true
            )
            {
                IsIndeterminate = false
            };

            var gifNodes = new List<HtmlNode>();

            API.Instance.Dialogs.ActivateGlobalProgress(progress =>
            {
                progress.ProgressMaxValue = imgNodes.Count;
                for (int i = 0; i < imgNodes.Count; i++)
                {
                    progress.CurrentProgressValue = i + 1;
                    progress.Text = $"Processing image {i + 1} of {imgNodes.Count}...";
                    // Process each image node

                    var imgNode = imgNodes[i];
                    var src = imgNode.Attributes["src"];
                    if (src != null && src.Value.ToLower().Contains(".avif"))
                    {
                        bool isTransparent = ImageUtils.IsImageTransparent(src.Value, settings.TransparencyThreshold);
                        if (isTransparent)
                        {
                            // Convert to PNG
                            logger.Info($"Converting AVIF image to PNG: {src.Value}");

                            MagickImage avif = ImageUtils.GetImageData(src.Value);
                            string newImage = ImageUtils.ConvertImage(avif, settings.Quality, "png", dataPath, game);
                            // Replace src in html
                            imgNode.SetAttributeValue("src", newImage);
                            changesAvifToPng++;
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
                            MagickImage avif = ImageUtils.GetImageData(src.Value);
                            string newImage = ImageUtils.ConvertImage(avif, settings.Quality, format, dataPath, game);
                            imgNode.SetAttributeValue("src", newImage);
                            changesAvifToWebp++;
                        }
                    }
                    else if (src != null && src.Value.ToLower().Contains(".webp"))
                    {
                        // Convert WebP to PNG
                        logger.Info($"Converting WebP image to PNG: {src.Value}");
                        MagickImage webp = ImageUtils.GetImageData(src.Value);
                        if (ImageUtils.IsImageTransparent(src.Value, settings.TransparencyThreshold))
                        {
                            logger.Info($"WebP image is transparent, converting to PNG: {src.Value}");
                            string pngImage = ImageUtils.ConvertImage(webp, settings.Quality, "png", dataPath, game);
                            imgNode.SetAttributeValue("src", pngImage);
                            changesWebpToPng++;
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
                        gifNodes.Add(imgNode);
                    }
                }
            }, options);

            return Tuple.Create(doc, changesAvifToPng, changesAvifToWebp, changesWebpToPng, gifNodes);
        }

        public Tuple<List<HtmlNode>, int> ProcessGifs(Game game, List<HtmlNode> gifNodes)
        {
            int changes = 0;
            if (gifNodes.Count == 0) return Tuple.Create(gifNodes, 0); // no gifs to process

            for(int i = 0; i < gifNodes.Count; i++)
            {
                var gifNode = gifNodes[i];
                var src = gifNode.Attributes["src"];
                var frames = ImageUtils.ExtractGifFrames(game, src.Value, settings.FrameCount, logger);

                // Convert GIF to single frame PNG
                // Show frame selection window
                var gifWindow = playniteAPI.Dialogs.CreateWindow(new WindowCreationOptions
                {
                    ShowCloseButton = true,
                    ShowMaximizeButton = false,
                    ShowMinimizeButton = false
                });

                var frameSelection = new FrameSelectionControl(frames);
                gifWindow.Owner = playniteAPI.Dialogs.GetCurrentAppWindow();
                gifWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                gifWindow.Content = frameSelection;
                gifWindow.Title = "Select Frame";
                gifWindow.SizeToContent = SizeToContent.WidthAndHeight;

                if (gifWindow.ShowDialog() == true)
                {
                    var selectedFrame = frameSelection.SelectedFramePath;
                    string fileName = $"gif_{i}_{Path.GetFileName(selectedFrame)}";
                    string directory = Path.Combine(dataPath, game.Id.ToString());
                    Directory.CreateDirectory(directory);
                    string fullPath = Path.Combine(directory, fileName);
                    File.Copy(selectedFrame, fullPath, true);
                    gifNode.SetAttributeValue("src", fullPath);
                    changes++;
                }
            }
            return Tuple.Create(gifNodes, changes);
        }
    }
}