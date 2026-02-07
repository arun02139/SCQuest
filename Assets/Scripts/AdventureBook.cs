using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AdventureBook
{
    public const int RESTART = -1;

    public struct Choice
    {
        public string label;
        public int targetPageId; // RESTART (-1) means restart the game
    }

    public struct Page
    {
        public int id;
        public string bodyText;
        public string imagePrompt;
        public string item; // null if no item reward on this page
        public List<Choice> choices;
    }

    public Dictionary<int, Page> Pages { get; private set; } = new();

    /// <summary>
    /// Parses the adventure text format:
    ///
    ///   === PAGE 1 ===
    ///   IMAGE: A dimly lit forest clearing at dawn, fantasy painting style
    ///   ITEM: Green Gem
    ///
    ///   Body text here, can span multiple lines.
    ///
    ///   [Choice label -> 2]
    ///   [Another choice -> 3]
    ///   [Play again -> restart]
    ///
    /// Pages with no choices are endings.
    /// Target "restart" clears visited pages and goes back to page 1.
    /// IMAGE: line is optional — if present, a static image will be shown.
    /// ITEM: line is optional — awards an item when the page is reached.
    /// </summary>
    public static AdventureBook Parse(string raw)
    {
        var book = new AdventureBook();

        // Split on page headers
        var pagePattern = new Regex(@"===\s*PAGE\s+(\d+)\s*===", RegexOptions.IgnoreCase);
        var choicePattern = new Regex(@"\[(.+?)\s*->\s*(\w+)\]");
        var imagePattern = new Regex(@"^IMAGE:\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var itemPattern = new Regex(@"^ITEM:\s*(.+)$", RegexOptions.Multiline | RegexOptions.IgnoreCase);

        var headerMatches = pagePattern.Matches(raw);

        for (int i = 0; i < headerMatches.Count; i++)
        {
            var match = headerMatches[i];
            int pageId = int.Parse(match.Groups[1].Value);

            // Extract the block of text between this header and the next (or end)
            int blockStart = match.Index + match.Length;
            int blockEnd = (i + 1 < headerMatches.Count) ? headerMatches[i + 1].Index : raw.Length;
            string block = raw.Substring(blockStart, blockEnd - blockStart);

            // Pull out image prompt if present
            string imagePrompt = null;
            var imageMatch = imagePattern.Match(block);
            if (imageMatch.Success)
            {
                imagePrompt = imageMatch.Groups[1].Value.Trim();
                block = imagePattern.Replace(block, "");
            }

            // Pull out item if present
            string item = null;
            var itemMatch = itemPattern.Match(block);
            if (itemMatch.Success)
            {
                item = itemMatch.Groups[1].Value.Trim();
                block = itemPattern.Replace(block, "");
            }

            // Pull out choices
            var choices = new List<Choice>();
            var choiceMatches = choicePattern.Matches(block);
            foreach (Match cm in choiceMatches)
            {
                string target = cm.Groups[2].Value.Trim();
                int targetId = target.ToLowerInvariant() == "restart"
                    ? RESTART
                    : int.Parse(target);

                choices.Add(new Choice
                {
                    label = cm.Groups[1].Value.Trim(),
                    targetPageId = targetId
                });
            }

            // Body text is everything with the choice lines removed, trimmed
            string body = choicePattern.Replace(block, "").Trim();

            book.Pages[pageId] = new Page
            {
                id = pageId,
                bodyText = body,
                imagePrompt = imagePrompt,
                item = item,
                choices = choices
            };
        }

        return book;
    }
}
