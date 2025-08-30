using HtmlAgilityPack;
using Playnite.SDK;
using Playnite.SDK.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace DescriptionFixer.Services
{
    internal class CleanService
    {
        private readonly DescriptionFixerSettings settings;
        private readonly ILogger logger;
        private readonly string dataPath;
        private readonly IPlayniteAPI playniteAPI;

        public CleanService(DescriptionFixerSettings settings, ILogger logger, string dataPath, IPlayniteAPI playniteAPI)
        {
            this.settings = settings;
            this.logger = logger;
            this.dataPath = dataPath;
            this.playniteAPI = playniteAPI;
        }

        public Tuple<string, int> CleanDescription(string html)
        {
            int changes = 0;

            var doc = new HtmlDocument();
            doc.LoadHtml(html);

                // 1. Replace dashes outside of tags
            var dashMap = new Dictionary<string, string>
            {
                { "-", "&#8211;" },
                { "–", "&#8211;" },
                { "—", "&#8212;" }
            };

            foreach (var textNode in doc.DocumentNode.DescendantsAndSelf().Where(n => n.NodeType == HtmlNodeType.Text))
            {
                string original = textNode.InnerText;
                string updated = dashMap.Aggregate(original, (current, kvp) => current.Replace(kvp.Key, kvp.Value));
                if (updated != original)
                {
                    textNode.InnerHtml = HtmlEntity.Entitize(updated);
                    changes++;
                }
            }

            // 2. Fix misplaced punctuation after </i> or </strong>
            var symbolsToMove = new[] { '™', '®', '©', '.', ',', '…', '?', '!', ':', ';' };

            foreach (var textNode in doc.DocumentNode.DescendantsAndSelf()
                            .Where(n => n.NodeType == HtmlNodeType.Text)
                            .ToList()) // to avoid modifying collection during iteration
            {
                // Look for previous sibling element
                var prev = textNode.PreviousSibling;
                while (prev != null && prev.NodeType != HtmlNodeType.Element)
                    prev = prev.PreviousSibling;

                // Skip nodes not directly preceded by <strong> or <i>
                if (prev == null || (prev.Name != "strong" && prev.Name != "i"))
                    continue;

                string text = textNode.InnerText;
                if (string.IsNullOrEmpty(text))
                    continue;

                // Count leading symbols to move
                int i = 0;
                while (i < text.Length && symbolsToMove.Contains(text[i]))
                    i++;

                if (i == 0)
                    continue; // no symbols to move

                string leadingSymbols = text.Substring(0, i);
                textNode.InnerHtml = text.Substring(i); // remove from this node

                // Append leading symbols to the last text node inside prev
                var lastText = prev.DescendantsAndSelf()
                                   .Where(n => n.NodeType == HtmlNodeType.Text)
                                   .LastOrDefault();
                if (lastText != null)
                    lastText.InnerHtml += leadingSymbols;
                else
                    prev.AppendChild(HtmlNode.CreateNode(leadingSymbols));

                // Update counters
                changes += i; // counting each symbol moved
            }

            // 3. Flatten <li><p>...</p></li>
            foreach (var li in doc.DocumentNode.SelectNodes("//li[p]") ?? Enumerable.Empty<HtmlNode>())
            {
                var p = li.Element("p");
                if (p != null)
                {
                    var children = p.ChildNodes.ToList(); // copy first
                    foreach (var child in children)
                    {
                        li.PrependChild(child); // move each node
                    }
                    p.Remove();
                    changes++;
                }
            }

            // 4. Remove double <br> before <ul>
            foreach (var ul in doc.DocumentNode.SelectNodes("//ul") ?? Enumerable.Empty<HtmlNode>())
            {
                var prev = ul.PreviousSibling;
                while (prev != null && prev.Name == "br")
                {
                    var toRemove = prev;
                    prev = prev.PreviousSibling;
                    toRemove.Remove();
                    changes++;
                }
            }

            return Tuple.Create(doc.DocumentNode.OuterHtml, changes);
        }
    }
}
