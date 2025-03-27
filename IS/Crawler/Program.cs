using System.Net.Mime;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

const string pages = "pages";
const string indexTxt = "index.txt";
HashSet<string> ignoreTags = ["script", "style", "head", "meta", "link"];

var cts = new CancellationTokenSource();

var links = new Queue<Uri>(args.Select(x => new Uri(x)));
var usedLinks = new HashSet<string>(args);
List<string> resPages = [];

if (!Directory.Exists(pages)) Directory.CreateDirectory(pages);

while (links.Count != 0 && resPages.Count < 100) {
    var link = links.Dequeue();
    var html = await GetHtml(link, cts.Token);
    var doc = new HtmlDocument();
    doc.LoadHtml(html);

    foreach (var anchor in GetAnchors(doc.DocumentNode, link))
    {
        if (!usedLinks.Add(anchor.ToString())) continue;
        Console.WriteLine(anchor);
        links.Enqueue(anchor);
    }

    var text = ExtractReadableText(doc.DocumentNode);
    var textWords = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    if (textWords.Length < 1000) continue;
    
    resPages.Add(link.ToString());
    await File.WriteAllTextAsync(Path.Combine(pages, $"{resPages.Count}.txt"), text, cts.Token);
    Console.WriteLine($"{link.ToString()} saved ({resPages.Count})");
}

await File.WriteAllTextAsync(indexTxt, string.Join('\n', resPages.Select((link, i) => $"{i + 1} - {link}")), cts.Token);

return;

async Task<string> GetHtml(Uri url, CancellationToken ct)
{
    try
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url, ct);

        return response.Content.Headers.ContentType?.MediaType == MediaTypeNames.Text.Html
            ? await response.Content.ReadAsStringAsync(ct)
            : string.Empty;
    }
    catch (HttpRequestException e)
    {
        Console.WriteLine($"Cannot get HTML from {url}, {e.HttpRequestError}, {e.Message}");
        return string.Empty;
    }
}

string ExtractReadableText(HtmlNode html)
{
    switch (html.NodeType)
    {
        case HtmlNodeType.Comment:
            return string.Empty;
        case HtmlNodeType.Text:
            return html.InnerText.Trim() + '\n';
        case HtmlNodeType.Document:
        case HtmlNodeType.Element:
        default:
        {
            return ignoreTags.Contains(html.Name)
                ? string.Empty
                : string.Join(' ', html.ChildNodes.Select(ExtractReadableText).Where(x => !string.IsNullOrWhiteSpace(x)));
        }
    }
}

IEnumerable<Uri> GetAnchors(HtmlNode html, Uri baseUri)
{
    return html.Descendants("a")
        .Where(x => x.Attributes["href"] != null)
        .Select(x => x.Attributes["href"].Value)
        .Where(x => WebsiteRegex().IsMatch(x))
        .Select(x => new Uri(baseUri, x));
}

partial class Program
{
    [GeneratedRegex(@"^(https?:\/\/[\w\-\.]+(\.[a-z]{2,})?(:\d+)?(\/[\w\-\.]*)*|(\.?\.?\/)?[\w\-.]+(\/[\w\-.]*)*)$")]
    private static partial Regex WebsiteRegex();
}
