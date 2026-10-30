// Browser SVG selections become standalone vector files; unrelated or incomplete selections remain ignored.

using System.Text;
using System.Xml.Linq;
using Filee.Core.Web;

namespace Filee.Core.Tests;

public class InlineSvgTests
{
    private static BrowserDragContent Drag(string text, string format = BrowserDragFormats.PlainText) =>
        new() { Texts = new Dictionary<string, string> { [format] = text } };

    [Theory]
    [InlineData("<svg width='1024' height='1024'/>")]
    [InlineData("<svg><path/></svg>")]
    [InlineData("<svg><defs><path d='M0 0L1 1'/></defs></svg>")]
    public void Empty_svg_shells_do_not_become_blank_successful_conversions(string text) =>
        Assert.Empty(InlineSvg.ReadMany(Drag(text)));

    [Fact]
    public void Empty_plain_text_does_not_hide_complete_html_drawing()
    {
        var content = new BrowserDragContent { Texts = new Dictionary<string, string>
        {
            [BrowserDragFormats.PlainText] = "<svg/>",
            [BrowserDragFormats.Html] = "<svg width='20' height='20'><rect width='20' height='20'/></svg>",
        }};
        Assert.Single(InlineSvg.ReadMany(content));
    }

    [Theory]
    [InlineData(BrowserDragFormats.PlainText)]
    [InlineData(BrowserDragFormats.Svg)]
    [InlineData(BrowserDragFormats.MacSvg)]
    [InlineData(BrowserDragFormats.Html)]
    public void Complete_svg_keeps_its_vector_content_and_gets_a_namespace(string format)
    {
        var bytes = InlineSvg.Read(Drag("<svg viewBox='0 0 20 20'><path d='M0 0L20 20'/><text>中文 &amp; SVG</text></svg>", format));
        Assert.NotNull(bytes);
        var root = XElement.Parse(Encoding.UTF8.GetString(bytes));
        XNamespace ns = "http://www.w3.org/2000/svg";
        Assert.Equal(ns + "svg", root.Name);
        Assert.Equal("0 0 20 20", (string?)root.Attribute("viewBox"));
        Assert.Equal("M0 0L20 20", (string?)root.Element(ns + "path")?.Attribute("d"));
        Assert.Equal("中文 & SVG", root.Element(ns + "text")?.Value);
    }

    [Fact]
    public void A_windows_html_fragment_ignores_the_page_context_and_keeps_nested_svg()
    {
        var html = "Version:0.9\r\nSourceURL:https://example.com/page\r\n<html><body>Outside" +
                   "<!--StartFragment--><a href='https://example.com'><svg xmlns='http://www.w3.org/2000/svg'>" +
                   "<title>Diagram</title><svg><text>Label</text></svg></svg></a><!--EndFragment-->Outside</body></html>";
        var result = InlineSvg.Read(Drag(html, BrowserDragFormats.WindowsHtml));
        Assert.NotNull(result);
        Assert.Contains("Label", Encoding.UTF8.GetString(result));
        Assert.DoesNotContain("Outside", Encoding.UTF8.GetString(result));
    }

    [Fact]
    public void Xml_declarations_namespaces_and_local_references_survive()
    {
        const string source = "<?xml version='1.0' encoding='utf-16'?><svg xmlns='http://www.w3.org/2000/svg' " +
                              "xmlns:xlink='http://www.w3.org/1999/xlink'><defs><path id='shape' d='M0 0L1 1'/></defs>" +
                              "<use xlink:href='#shape'/></svg>";
        var bytes = InlineSvg.Read(Drag(source));
        Assert.NotNull(bytes);
        var root = XElement.Parse(Encoding.UTF8.GetString(bytes));
        Assert.Equal("#shape", root.Descendants().Single(e => e.Name.LocalName == "use")
            .Attribute(XName.Get("href", "http://www.w3.org/1999/xlink"))?.Value);
        Assert.DoesNotContain("utf-16", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void Real_files_keep_priority_over_a_code_selection()
    {
        using var file = WebDropItem.FromBytes("existing.png", [1, 2, 3]);
        var content = new BrowserDragContent
        {
            Files = [file],
            Texts = new Dictionary<string, string> { [BrowserDragFormats.PlainText] = "<svg><rect width='1' height='1'/></svg>" },
        };
        Assert.Same(file, Assert.Single(BrowserDrag.Files(content)));
    }

    [Theory]
    [InlineData("ordinary text")]
    [InlineData("https://example.com/image.svg")]
    [InlineData("<svg><path></svg>")]
    [InlineData("<svg/ ><p>text</p>")]
    [InlineData("<svg xmlns='urn:not-svg'/>")]
    [InlineData("<!DOCTYPE svg [<!ENTITY x SYSTEM 'file:///not-read'>]><svg>&x;</svg>")]
    [InlineData("<html><body>text</body></html>")]
    public void Other_text_invalid_xml_and_external_entities_are_not_files(string text)
    {
        Assert.Null(InlineSvg.Read(Drag(text)));
        Assert.Empty(BrowserDrag.Files(Drag(text)));
    }

    [Theory]
    [InlineData("<p>Caption<svg><path d='M0 0L1 1'/></svg></p>", 1)]
    [InlineData("<svg><path d='M0 0L1 1'/></svg><svg><path d='M0 0L1 1'/></svg>", 2)]
    [InlineData("<img src='other.png'><svg><path d='M0 0L1 1'/></svg>", 1)]
    [InlineData("<div><svg><svg><rect width='1' height='1'/></svg></svg><span>Caption</span><svg><rect width='1' height='1'/></svg><img src='x'><svg><rect width='1' height='1'/></svg></div>", 3)]
    [InlineData("<pre><span>&lt;svg</span>&gt;&lt;text&gt;A &amp;amp; B&lt;/text&gt;&lt;/svg&gt;</pre>", 1)]
    [InlineData("<!--<svg><rect width='1' height='1'/></svg>--><script>let x = '<svg><rect width='1' height='1'/></svg>';</script><svg><rect width='1' height='1'/></svg>", 1)]
    public void Complete_svgs_are_extracted_from_mixed_and_escaped_selections(string html, int count)
    {
        Assert.Equal(count, InlineSvg.ReadMany(Drag(html, BrowserDragFormats.Html)).Count);
        var files = BrowserDrag.Files(Drag(html));
        Assert.Equal(count, files.Count);
        Assert.Equal(count, files.Select(f => f.Name).Distinct().Count());
        foreach (var file in files) file.Dispose();
    }

    [Fact]
    public void Incomplete_and_excessive_batches_are_ignored()
    {
        Assert.Empty(InlineSvg.ReadMany(Drag("<svg><rect width='1' height='1'/></svg><svg>")));
        Assert.Empty(InlineSvg.ReadMany(Drag(string.Concat(Enumerable.Repeat("<svg><rect width='1' height='1'/></svg>", 129)))));
    }

    [Fact]
    public void Oversized_selection_is_ignored() =>
        Assert.Null(InlineSvg.Read(Drag("<svg>" + new string(' ', InlineSvg.MaxCharacters) + "</svg>")));

    [Fact]
    public async Task Svg_is_saved_through_the_existing_browser_file_pipeline()
    {
        using var dir = new TempDir();
        using var downloader = new WebDownloader();
        using var item = Assert.Single(BrowserDrag.Files(Drag("<svg width='20' height='20'><rect width='20' height='20'/></svg>")));
        Assert.Equal("image.svg", item.Name);
        var path = await item.SaveAsync(dir.Path, downloader, null, CancellationToken.None);
        Assert.Equal("svg", XElement.Load(path).Name.LocalName);
    }
}
