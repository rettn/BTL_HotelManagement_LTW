using System.Text.RegularExpressions;

namespace HotelManagement.Security;

public static partial class RichTextSanitizer
{
    private static readonly Regex DangerousBlocks = new(
        @"<(script|style|iframe|object|svg|math)\b[^>]*>[\s\S]*?</\1\s*>|<!--[\s\S]*?-->",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex Tags = new(
        @"</?\s*(p|br|strong|b|em|i|u|s|ul|ol|li|h1|h2|h3|blockquote|div|span|a)\b[^>]*>",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string Sanitize(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var safe = DangerousBlocks.Replace(html, string.Empty);
        safe = Tags.Replace(safe, match =>
        {
            var nameMatch = Regex.Match(match.Value, @"</?\s*(?<name>[a-z0-9]+)", RegexOptions.IgnoreCase);
            if (!nameMatch.Success) return string.Empty;
            var name = nameMatch.Groups["name"].Value.ToLowerInvariant();
            var closing = match.Value.StartsWith("</", StringComparison.Ordinal);
            if (name == "br") return closing ? string.Empty : "<br>";
            if (name == "a" && !closing)
            {
                var href = Regex.Match(match.Value, @"\bhref\s*=\s*['""](?<url>https?://[^'""]+)['""]", RegexOptions.IgnoreCase);
                return href.Success ? $"<a href=\"{System.Net.WebUtility.HtmlEncode(href.Groups["url"].Value)}\" rel=\"noopener noreferrer\" target=\"_blank\">" : "<a>";
            }
            return closing ? $"</{name}>" : $"<{name}>";
        });
        return safe.Trim();
    }
}
