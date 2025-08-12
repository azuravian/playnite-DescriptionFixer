using DescriptionFixer.Utilities;
using ImageMagick;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptionFixer.Services
{
    internal class ImageService
    {
        private readonly DescriptionFixerSettings settings;
        private readonly ILogger logger;
        private readonly string dataPath;

        public ImageService(DescriptionFixerSettings settings, ILogger logger, string dataPath)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataPath = dataPath;
        }

        public async Task<string> ProcessImages(Game game, string html)
        {
            var images = HtmlParser.ExtractImageTags(html);

            foreach (var imgTag in images)
            {
                var src = HtmlParser.GetSrcFromImgTag(imgTag);
                if (src != null && src.ToLower().Contains(".avif"))
                {
                    bool isTransparent = ImageUtils.IsImageTransparent(src, settings.TransparencyThreshold);
                    if (isTransparent)
                    {
                        // Convert to PNG
                        logger.Info($"Converting AVIF image to PNG: {src}");
                        
                        MagickImage avif = await ImageUtils.GetAvifData(src);
                        string newImage = ImageUtils.ConvertImage(avif, settings.Quality, "png", dataPath, game);
                        // Replace src in html
                        html = HtmlParser.ReplaceImageSrc(html, src, newImage);
                    }
                    else
                    {
                        var format = settings.UseJpeg ? "jpg" : "webp";
                        switch (format)
                        {
                            case "webp":
                                // Convert to WebP
                                logger.Info($"Converting AVIF image to WebP: {src}");
                                break;
                            case "jpg":
                                // Convert to JPG
                                logger.Info($"Converting AVIF image to JPG: {src}");
                                break;
                            default:
                                continue;
                        }
                        MagickImage avif = await ImageUtils.GetAvifData(src);
                        string newImage = ImageUtils.ConvertImage(avif, settings.Quality, format, dataPath, game);
                        html = HtmlParser.ReplaceImageSrc(html, src, newImage);
                    }
                }
                else if (src != null && src.ToLower().Contains(".webp"))
                {
                    // Convert WebP to PNG or JPG
                    logger.Info($"Converting WebP image: {src}");
                    MagickImage webp = await ImageUtils.GetAvifData(src);
                    if (ImageUtils.IsImageTransparent(src, settings.TransparencyThreshold))
                    {
                        logger.Info($"WebP image is transparent, converting to PNG: {src}");
                        string pngImage = ImageUtils.ConvertImage(webp, settings.Quality, "png", dataPath, game);
                        html = HtmlParser.ReplaceImageSrc(html, src, pngImage);
                        continue;
                    }
                    else
                    {
                        // No need to convert if not transparent
                        logger.Info($"WebP image is not transparent, keeping as WebP: {src}");
                    }
                }
            }
            return html;
        }
    }
}
