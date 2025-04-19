using System.Globalization;

if (args.Length != 2) throw new ArgumentException("Not enough arguments");

var idfInput = args[0];
var idf = await ReadIdf(idfInput);
var tfidfInput = args[1];
var (tfidf, docs) = await ReadTfIdf(tfidfInput);

var input = Console.ReadLine();
var lemmas = Lemmatizer.Lemmatizer.Lemmatize(input ?? string.Empty);
var clearedLemmas = await Lemmatizer.Lemmatizer.ClearLemmasAsync(lemmas);
var knownLemmas = clearedLemmas.Where(token => tfidf.Any(pair => pair.Key == token)).ToList();

var queryTfIdf = knownLemmas
    .GroupBy(lemma => lemma)
    .ToDictionary(
        g => g.Key, 
        g => (double)g.Count() / knownLemmas.Count * idf[g.Key]
    );

// [document] -> cos sim
var results = new Dictionary<string, double>();
foreach (var doc in docs)
    results[doc] = CosineSimilarity(queryTfIdf, tfidf, doc);


Console.WriteLine(
    string.Join('\n', 
        results
            .Where(kvp => !double.IsNaN(kvp.Value))
            .OrderByDescending(x => x.Value)
            .Take(10)
            .Select(x => $"{x.Key}: {x.Value:F2}")));
return;

double CosineSimilarity(Dictionary<string, double> queryVector, Dictionary<string, Dictionary<string, double>> tfIdf, string document)
{
    double queryVectorLength = 0, docVectorLength = 0, res = 0;
    foreach (var (term, qtfidf) in queryVector)
    {
        var docVal = tfIdf[term][document];
        queryVectorLength += qtfidf * qtfidf;
        docVectorLength += docVal;
        res += docVal * qtfidf;
    }

    return res / Math.Sqrt(queryVectorLength) / Math.Sqrt(docVectorLength);
}

async Task<Dictionary<string, double>> ReadIdf(string inputPath)
{
    var lines = await File.ReadAllLinesAsync(inputPath);
    var res = new Dictionary<string, double>();
    for (var i = 1; i < lines.Length; i++)
    {
        var cols = lines[i].Split(';');
        res[cols[0]] = double.Parse(cols[1], CultureInfo.InvariantCulture);
    }
    return res;
}

// [term][doc] -> tfidf
async Task<(Dictionary<string, Dictionary<string, double>> tfidf, List<string> docs)> ReadTfIdf(string inputPath)
{
    var lines = await File.ReadAllLinesAsync(inputPath);
    var res = new Dictionary<string, Dictionary<string, double>>();
    var docs = lines[0].Split(';').Skip(1).ToList();
    for (var i = 1; i < lines.Length; i++)
    {
        var cols = lines[i].Split(';');
        res[cols[0]] = [];
        for (var j = 0; j < docs.Count; j++)
            res[cols[0]][docs[j]] = double.Parse(cols[1 + j], CultureInfo.InvariantCulture);
    }
    return (res, docs);
}



