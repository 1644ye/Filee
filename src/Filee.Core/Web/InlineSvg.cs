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

    /// <summary>A complete SVG from the drag, encoded as a standalone UTF-8 file, or null.</summary>
    public static byte[]? Read(BrowserDragContent content)
    {
        foreach (var format in new[] { BrowserDragFormats.Svg, BrowserDragFormats.MacSvg, BrowserDragFormats.PlainText })
        {
            if (Parse(content.Text(format)) is { } svg)
                return svg;
        }
        foreach (var format in new[] { BrowserDragFormats.Html, BrowserDragFormats.WindowsHtml, BrowserDragFormats.MacHtml })
        {
            if (FromHtml(content.Text(format)) is { } svg)
                return svg;
        }
        return null;
    }

    private static byte[]? FromHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html) || html.Length > MaxCharacters)
            return null;
        var start = html.IndexOf("<!--StartFragment-->", StringComparison.OrdinalIgnoreCase);
        var end = html.IndexOf("<!--EndFragment-->", StringComparison.OrdinalIgnoreCase);
        if (start >= 0 && end > start)
            html = html[(start + "<!--StartFragment-->".Length)..end];
        if (Parse(html) is { } document)
            return document;
        try
        {
            var opening = SvgStart().Match(html);
            var endings = SvgEnd().Matches(html);
            if (!opening.Success || endings.Count == 0)
                return null;
            var last = endings[^1];
            var after = last.Index + last.Length;
            if (after <= opening.Index)
                return null;
            // Browser selections may wrap the graphic in a link or layout element. Text or other pictures outside
            // it mean a mixed selection, not one SVG. SVG's own <text>, <title>, styles and nested SVG stay intact.
            var outside = html[..opening.Index] + html[after..];
            if (WebUtility.HtmlDecode(Wrappers().Replace(outside, "")).Any(c => !char.IsWhiteSpace(c)))
                return null;
            return Parse(html[opening.Index..after]);
        }
        catch (RegexMatchTimeoutException)
        {
            return null;
        }
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

    [GeneratedRegex(@"<svg(?=\s|>)", RegexOptions.CultureInvariant, 100)]
    private static partial Regex SvgStart();

    [GeneratedRegex(@"</svg\s*>", RegexOptions.CultureInvariant, 100)]
    private static partial Regex SvgEnd();

    [GeneratedRegex(@"</?(?:html|body|div|span|p|a)\b[^>]*>|<meta\b[^>]*>|<!--.*?-->",
        RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.CultureInvariant, 100)]
    private static partial Regex Wrappers();
}
