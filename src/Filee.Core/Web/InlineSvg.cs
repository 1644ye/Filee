// SVG code selections and inline SVG pictures carried by browser drags. Parse only complete SVG documents;
// ordinary text remains text. No page access, script execution or external XML resolution is involved.

using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;

namespace Filee.Core.Web;

public static partial class InlineSvg
{
    public const int MaxCharacters = 4 * 1024 * 1024;
    private const string SvgNamespace = "http://www.w3.org/2000/svg";

    /// <summary>The single SVG of a selection, or null when it contains none or several.</summary>
    public static byte[]? Read(BrowserDragContent content)
    {
        var pictures = ReadMany(content);
        return pictures.Count == 1 ? pictures[0] : null;
    }

    /// <summary>Complete outer SVG elements, in selection order, without surrounding page text or images.</summary>
    public static IReadOnlyList<byte[]> ReadMany(BrowserDragContent content)
    {
        foreach (var format in new[] { BrowserDragFormats.Svg, BrowserDragFormats.MacSvg, BrowserDragFormats.PlainText,
                     BrowserDragFormats.Html, BrowserDragFormats.WindowsHtml, BrowserDragFormats.MacHtml })
        {
            var pictures = FromSelection(content.Text(format));
            if (pictures.Count > 0)
                return pictures;
        }
        return [];
    }

    private static IReadOnlyList<byte[]> FromSelection(string? html)
    {
        if (string.IsNullOrWhiteSpace(html) || html.Length > MaxCharacters)
            return [];
        var start = html.IndexOf("<!--StartFragment-->", StringComparison.OrdinalIgnoreCase);
        var end = html.IndexOf("<!--EndFragment-->", StringComparison.OrdinalIgnoreCase);
        if (start >= 0 && end > start)
            html = html[(start + "<!--StartFragment-->".Length)..end];
        try
        {
            var pictures = Extract(html);
            if (pictures.Count > 0)
                return pictures;
            // Code selections may arrive only as escaped HTML, with syntax-highlighting spans between tokens.
            // Decode once, after removing the presentation markup, so XML entities inside SVG stay XML entities.
            var code = WebUtility.HtmlDecode(Markup().Replace(html, ""));
            return code == html ? [] : Extract(code);
        }
        catch (RegexMatchTimeoutException)
        {
            return [];
        }
    }

    private static IReadOnlyList<byte[]> Extract(string source)
    {
        if (source.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase))
            return [];
        if (Parse(source) is { } document)
            return [document];
        var pictures = new List<byte[]>();
        var depth = 0;
        var start = 0;
        foreach (Match token in Markup().Matches(source))
        {
            if (token.Groups["name"].Value != "svg")
                continue;
            var closing = token.Groups["close"].Success;
            var empty = token.Value.EndsWith("/>", StringComparison.Ordinal);
            if (!closing)
            {
                if (depth == 0)
                    start = token.Index;
                depth++;
            }
            else if (depth == 0)
                return [];
            if (closing || empty)
                depth--;
            if (depth != 0)
                continue;
            // Nested SVG remains part of its parent; siblings become separate files with distinct names.
            if (Parse(source[start..(token.Index + token.Length)]) is not { } svg || pictures.Count >= 128)
                return [];
            pictures.Add(svg);
        }
        return depth == 0 ? pictures : [];
    }

    private static byte[]? Parse(string? source)
    {
        if (string.IsNullOrWhiteSpace(source) || source.Length > MaxCharacters)
            return null;
        source = source.Trim().TrimStart('\uFEFF');
        if (!source.StartsWith('<'))
            return null;
        try
        {
            using var input = new StringReader(source);
            using var reader = XmlReader.Create(input, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                MaxCharactersInDocument = MaxCharacters,
            });
            var document = XDocument.Load(reader, LoadOptions.PreserveWhitespace);
            if (document.Root is not { } root || root.Name.LocalName != "svg"
                || (root.Name.NamespaceName.Length != 0 && root.Name.NamespaceName != SvgNamespace))
                return null;
            // HTML inline SVG often omits xmlns. Standalone converters need the SVG namespace.
            if (root.Name.NamespaceName.Length == 0)
            {
                foreach (var element in root.DescendantsAndSelf().Where(e => e.Name.NamespaceName.Length == 0))
                    element.Name = XName.Get(element.Name.LocalName, SvgNamespace);
                root.SetAttributeValue("xmlns", SvgNamespace);
            }
            return Encoding.UTF8.GetBytes(root.ToString(SaveOptions.DisableFormatting));
        }
        catch (XmlException)
        {
            return null;
        }
    }

    // Consume comments, CDATA and script/style text as whole tokens. Quoted '>' characters are not tag boundaries.
    [GeneratedRegex("""<!--.*?-->|<!\[CDATA\[.*?\]\]>|<script\b[^>]*>.*?</script\s*>|<style\b[^>]*>.*?</style\s*>|<(?<close>/)?(?<name>[A-Za-z_][\w:.-]*)(?=\s|/?>)(?:"[^"]*"|'[^']*'|[^'">])*>""",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, 100)]
    private static partial Regex Markup();
}
