using InvIndexAndBoolSearch;

const string invIndexFile = "invIndex.txt";

// if -g => genInvIndex
// else read invIndex fromFile
// then go to interactive mode
await Main(args);
return;

async Task Main(string[] args)
{
    Dictionary<string, HashSet<string>> invIndex;
    if (args.Length == 1) invIndex = await ReadInvIndexFile();
    else if (args[1] == "-g")
    {
        var lemmasDir = args[2];
        Console.WriteLine($"Generating inverted index according to lemmas: {lemmasDir}");
        invIndex = GenInvIndex(await ReadLemmas(lemmasDir));
        await WriteInvIndexFile(invIndex);
        Console.WriteLine($"Save generated inverted index to file: {invIndexFile}");
    }
    else throw new ArgumentException("Incorrect arguments. Expected \"<indexFile> -g <lemmasDir>\"");

    var index = await ReadIndexFile(args[0]);
    var documents = index.Select(x => x.Key).ToHashSet();

    while (true)
    {
        var input = Console.ReadLine();
        await StartSearch(input ?? string.Empty, invIndex, documents, index);
    }
}
// пенсионер & конкурс | семья
// пенсионер | конкурс | семья
// пенсионер & конкурс & семья
// пенсионер & !конкурс | !семья
// пенсионер | !конкурс | !семья

async Task<Dictionary<string, string>> ReadIndexFile(string indexFile)
{
    var lines = await File.ReadAllLinesAsync(indexFile);
    return lines.Select(line => line.Split(" - ")).ToDictionary(x => x[0], x => x[1]);
}

async Task<Dictionary<string, string[]>> ReadLemmas(string lemmasDir)
{
    var res = new Dictionary<string, string[]>();
    foreach (var filePath in Directory.EnumerateFiles(lemmasDir))
        res[Path.GetFileNameWithoutExtension(filePath)] = await File.ReadAllLinesAsync(filePath);
    return res;
}

async Task<Dictionary<string, HashSet<string>>> ReadInvIndexFile()
{
    Console.WriteLine($"Reading inverted index from file: {invIndexFile}");
    var invIndex = new Dictionary<string, HashSet<string>>();
    var invIndexLines = await File.ReadAllLinesAsync(invIndexFile);
    foreach (var line in invIndexLines)
    {
        var split = line.Split(':');
        invIndex[split[0]] = split[1].Split(',').ToHashSet();
    }

    return invIndex;
}

Dictionary<string, HashSet<string>> GenInvIndex(Dictionary<string, string[]> lemmas)
{
    var res = new Dictionary<string, HashSet<string>>();
    foreach (var (document, terms) in lemmas)
    foreach (var term in terms)
    {
        res.TryAdd(term, []);
        res[term].Add(document);
    }

    return res;
}

Task WriteInvIndexFile(Dictionary<string, HashSet<string>> invIndex) =>
    File.WriteAllLinesAsync(
        invIndexFile,
        invIndex
            .OrderBy(pair => pair.Key)
            .Select(pair => $"{pair.Key}:{string.Join(",", pair.Value.OrderBy(val => val))}"));

async Task StartSearch(string input, Dictionary<string, HashSet<string>> invIndex, HashSet<string> documents,
    Dictionary<string, string> index)
{
    var ast = new Parser(input).Parse();
    var foundDocuments = ast.FindDocuments(invIndex, documents).ToList();
    input = ast.ToString()!;

    await File.WriteAllLinesAsync(Path.ChangeExtension(input, ".txt"),
        new[] { input, string.Join(", ", foundDocuments) }.Concat(foundDocuments.Select(doc => index[doc])));
    Console.WriteLine(string.Join(", ", foundDocuments));
}
