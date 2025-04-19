const string PagesInputDir = "pages";
const string PagesDir = "pagesTest";

var cts = new CancellationTokenSource();

var indexFile = args[0];
var pagesPath = Path.Combine(Path.GetDirectoryName(indexFile)!, PagesInputDir);
if (!Directory.Exists(PagesDir)) Directory.CreateDirectory(PagesDir);

var indexContent = await File.ReadAllTextAsync(indexFile, cts.Token);
foreach (var line in indexContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
{
    var curFile = Path.Combine(pagesPath, $"{line.Split(" - ")[0]}.txt");
    var curContent = await File.ReadAllTextAsync(curFile, cts.Token);
    var curLemmas = Lemmatizer.Lemmatizer.Lemmatize(curContent);
    var curTokens = await Lemmatizer.Lemmatizer.ClearLemmasAsync(curLemmas);
    var resFile = Path.Combine(PagesDir, $"{line.Split(" - ")[0]}.txt");
    await File.WriteAllLinesAsync(resFile, curTokens, cts.Token);
    Console.WriteLine($"Lemmatization finished for {curFile}, data saved to {resFile}");
}
