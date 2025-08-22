using Playnite.SDK;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace DescriptionFixer.Services
{
    internal class EmojiService
    {
        private readonly DescriptionFixerSettings settings;
        private readonly ILogger logger;
        private readonly string dataPath;
        private readonly IPlayniteAPI playniteAPI;

        public EmojiService(DescriptionFixerSettings settings, ILogger logger, string dataPath, IPlayniteAPI playniteAPI)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataPath = dataPath;
            this.playniteAPI = playniteAPI;
        }

        public string ProcessEmojis(string html)
        {
            if (!settings.ReplaceEmojis)
            {
                return html;
            }
            
            string regexPattern = @"[\uD800-\uDBFF][\uDC00-\uDFFF]";
            // var utf32Bytes = Encoding.UTF32.GetBytes(html);
            var matches = Regex.Matches(html, regexPattern);

            foreach (Match match in matches)
            {
                string emoji = match.Value;
                var codePoints = ConvertToUnicodeCodePoints(emoji)
                                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                // Convert hex strings ("U+XXXX") to ints
                int spcP;
                if (codePoints.Length == 2)
                {
                    int cP1 = Convert.ToInt32(codePoints[0].Substring(2), 16);
                    int cP2 = Convert.ToInt32(codePoints[1].Substring(2), 16);
                    spcP = 0x10000 + ((cP1 - 0xD800) * 0x400) + (cP2 - 0xDC00);
                }
                else if (codePoints.Length == 1)
                {
                    spcP = Convert.ToInt32(codePoints[0].Substring(2), 16);
                }
                else
                {
                    throw new InvalidOperationException("Unexpected number of code points.");
                }

                // Make HTML decimal entity
                string htmlEntityDecimal = $"&#{spcP};";

                html = html.Replace(emoji, htmlEntityDecimal);
            }
            return html;
        }

        private string ConvertToUnicodeCodePoints(string input)
        {
            var codePoints = new List<string>();

            for (int i = 0; i < input.Length; i++)
            {
                int codePoint = char.ConvertToUtf32(input, i);
                codePoints.Add($"U+{codePoint:X4}");

                if (char.IsHighSurrogate(input[i]))
                    i++;
            }

            // Matches PowerShell: return space-separated string
            return string.Join(" ", codePoints);
        }
    }
}