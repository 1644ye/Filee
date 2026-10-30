using Filee.Core.Web;
namespace Filee.Core.Tests;

public class VideoDragTests
{
    [Fact]
    public void Html_video_preserves_signed_extensionless_address()
    {
        var html = "<video src='https://example.com/stream?id=1&amp;token=2'></video>";
        Assert.Equal("https://example.com/stream?id=1&token=2", BrowserDragFormats.VideoOnly(html));
        var files = BrowserDrag.Files(new BrowserDragContent { Texts = new Dictionary<string, string> { [BrowserDragFormats.Html] = html } });
        Assert.Single(files);
        Assert.EndsWith(".mp4", files[0].Name);
        foreach (var file in files) file.Dispose();
    }
    [Theory]
    [InlineData("blob:https://example.com/id")]
    [InlineData("file:///C:/private.mp4")]
    [InlineData("https://example.com/live.m3u8")]
    [InlineData("https://example.com/live.mpd")]
    public void Unsupported_sources_are_rejected(string source) =>
        Assert.Null(BrowserDragFormats.VideoOnly($"<video src='{source}'></video>"));
}
