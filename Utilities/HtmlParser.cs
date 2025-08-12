using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DescriptionFixer.Utilities
{
    internal static class HtmlParser
    {
        public static List<string> ExtractVideoTags(string html)
        {
            var videoTags = new List<string>();
            var startIndex = 0;
            while ((startIndex = html.IndexOf("<video", startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                var endIndex = html.IndexOf("</video>", startIndex, StringComparison.OrdinalIgnoreCase);
                if (endIndex == -1)
                {
                    break; // No closing tag found
                }
                var videoTag = html.Substring(startIndex, endIndex - startIndex + "</video>".Length);
                videoTags.Add(videoTag);
                startIndex = endIndex + "</video>".Length; // Move past the closing tag
            }
            return videoTags;
        }
        public static string GetPosterFromVideoTag(string videoTag)
        {
            var posterIndex = videoTag.IndexOf("poster=\"", StringComparison.OrdinalIgnoreCase);
            if (posterIndex != -1)
            {
                posterIndex += 8; // Move past 'poster="'
                var endIndex = videoTag.IndexOf("\"", posterIndex);
                if (endIndex != -1)
                {
                    return videoTag.Substring(posterIndex, endIndex - posterIndex);
                }
            }
            return null; // No poster found
        }

        public static List<string> ExtractImageTags(string html)
        {
            var imageTags = new List<string>();
            var startIndex = 0;
            while ((startIndex = html.IndexOf("<img", startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
            {
                var endIndex = html.IndexOf(">", startIndex);
                if (endIndex == -1)
                {
                    break; // No closing tag found
                }
                var imageTag = html.Substring(startIndex, endIndex - startIndex + 1);
                imageTags.Add(imageTag);
                startIndex = endIndex + 1; // Move past the closing tag
            }
            return imageTags;
        }

        public static string GetSrcFromImgTag(string imgTag)
        {
            var srcIndex = imgTag.IndexOf("src=\"", StringComparison.OrdinalIgnoreCase);
            if (srcIndex != -1)
            {
                srcIndex += 5; // Move past 'src="'
                var endIndex = imgTag.IndexOf("\"", srcIndex);
                if (endIndex != -1)
                {
                    return imgTag.Substring(srcIndex, endIndex - srcIndex);
                }
            }
            return null; // No src found
        }

        public static string ReplaceImageSrc(string html, string oldSrc, string newSrc)
        {
            // Replace the src attribute in the img tag
            return html.Replace($"src=\"{oldSrc}\"", $"src=\"{newSrc}\"");
        }
        public static string ReplaceVideoWithImage(string html, string videotag, string imgsource)
        {
            return html.Replace(videotag, $"<img src=\"{imgsource}\" alt=\"Video frame\" />");
        }
    }
}
