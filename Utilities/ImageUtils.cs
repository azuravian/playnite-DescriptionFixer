using DescriptionFixer;
using ImageMagick;
using Playnite.SDK;
using Playnite.SDK.Models;
using Playnite.SDK.Plugins;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Text;
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
    }
}
