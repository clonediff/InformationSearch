using System.Globalization;

const string IDFResultFile = "IDFResult.csv";
const string TFIDFResultFile = "TFIDFResult.csv";

await Main(args);
return;

async Task Main(string[] args)
{
    var lemmasDir = args[0];
    var invIndexFile = args[1];
    // [term][doc] = freq;
    var lemmas = await ReadLemmas(lemmasDir);
    var tf = TF(lemmas);
    var invIndex = await ReadInvIndexFile(invIndexFile);
    var idf = IDF(lemmas.Count, invIndex);
    var tfidf = TFIDF(tf, idf);
    await PrintIDF(IDFResultFile, idf);
    await PrintTFIDF(TFIDFResultFile, lemmas.Select(x => x.Key).ToList(), tfidf);
    Console.WriteLine("Done");
}

async Task<Dictionary<string, string[]>> ReadLemmas(string lemmasDir)
{
    var res = new Dictionary<string, string[]>();
    foreach (var filePath in Directory.EnumerateFiles(lemmasDir))
        res[Path.GetFileNameWithoutExtension(filePath)] = await File.ReadAllLinesAsync(filePath);
    return res;
}

Dictionary<string, Dictionary<string, double>> TF(Dictionary<string, string[]> lemmas) {
    var res = new Dictionary<string, Dictionary<string, double>>(); // term -> doc -> freq
    foreach (var (doc, terms) in lemmas)
    {
        foreach (var (term, count) in terms.GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count()))
        {
            res.TryAdd(term, []);
            res[term][doc] = (double)count / terms.Length;
        }
    }
    return res;
}

async Task<Dictionary<string, HashSet<string>>> ReadInvIndexFile(string invIndexPath)
{
    Console.WriteLine($"Reading inverted index from file: {invIndexPath}");
    var invIndex = new Dictionary<string, HashSet<string>>();
    var invIndexLines = await File.ReadAllLinesAsync(invIndexPath);
    foreach (var line in invIndexLines)
    {
        var split = line.Split(':');
        invIndex[split[0]] = split[1].Split(',').ToHashSet();
    }

    return invIndex;
}

Dictionary<string, double> IDF(int totalDocs, Dictionary<string, HashSet<string>> invIndex) {
    var res = new Dictionary<string, double>();
    foreach (var (term, docs) in invIndex)
        res[term] = Math.Log2((double)totalDocs / docs.Count);
    return res;
}

Dictionary<string, Dictionary<string, double>> TFIDF(Dictionary<string, Dictionary<string, double>> tf, Dictionary<string, double> idf)
{
    var res = new Dictionary<string, Dictionary<string, double>>();
    foreach (var (term, invFreq) in idf)
    {
        foreach (var (doc, termFreq) in tf[term])
        {
            res.TryAdd(term, []);
            res[term][doc] = termFreq * invFreq;
        }
    }
    return res;
}

async Task PrintIDF(string outputPath, Dictionary<string, double> idf)
{
    const string header = "Terms;IDF";
    await File.WriteAllLinesAsync(outputPath, [header]);
    await File.AppendAllLinesAsync(outputPath,
        idf.OrderBy(pair => pair.Key)
            .Select(x => $"{x.Key};{x.Value.ToString("F5", CultureInfo.InvariantCulture)}")
    );
}

async Task PrintTFIDF(string outputPath, List<string> docs, Dictionary<string, Dictionary<string, double>> tfidf)
{
    docs = docs.OrderBy(d => d).ToList();
    var header = $"Terms\\Docs;{string.Join(';', docs)}";
    await File.WriteAllLinesAsync(outputPath, [header]);
    await File.AppendAllLinesAsync(outputPath,
        tfidf
            .OrderBy(x => x.Key)
            .Select(pair => string.Join(';',
                    new[] { pair.Key }.Concat(
                        docs.Select(d => pair.Value.GetValueOrDefault(d, 0).ToString("F5", CultureInfo.InvariantCulture))
                    )
                )
            )
    );
}
