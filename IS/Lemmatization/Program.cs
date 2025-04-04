﻿using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lemmatization;

const string stopWordsFile = "StopWords.txt";
const string myStemExe = "mystem.exe";
const string pagesDir = "pages";

var cts = new CancellationTokenSource();

var stopWordsText = await File.ReadAllTextAsync(stopWordsFile, cts.Token);
var stopWords = stopWordsText
    .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
    .ToImmutableHashSet();


var indexFile = args[0];
var pagesPath = Path.Combine(Path.GetDirectoryName(indexFile)!, pagesDir);
if (!Directory.Exists(pagesDir)) Directory.CreateDirectory(pagesDir);

var indexContent = await File.ReadAllTextAsync(indexFile, cts.Token);
foreach (var line in indexContent.Split('\n', StringSplitOptions.RemoveEmptyEntries))
{
    var curFile = Path.Combine(pagesPath, $"{line.Split(" - ")[0]}.txt");
    var curContent = await File.ReadAllTextAsync(curFile, cts.Token);
    var curLemmas = Lemmatize(curContent);
    var curTokens = curLemmas
        .SelectMany(lemma => TokenizerRegex()
            .Matches(lemma ?? string.Empty)
            .Select(m => m.Value)
            .Where(w => !stopWords.Contains(w))
        );
    var resFile = Path.Combine(pagesDir, $"{line.Split(" - ")[0]}.txt");
    await File.WriteAllLinesAsync(resFile, curTokens, cts.Token);
    Console.WriteLine($"Lemmatization finished for {curFile}, data saved to {resFile}");
}

return;

IEnumerable<Lemma?> Analyze(string text)
{
    using var process = new Process();
    process.StartInfo = new ProcessStartInfo
    {
        FileName = myStemExe,
        // i - Печатать грамматическую информацию
        // g - Склеивать информацию словоформ при одной лемме
        // d - Применить контекстное снятие омонимии
        Arguments = "--format json -igd",
        RedirectStandardInput = true,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
        StandardInputEncoding = Encoding.UTF8,
        StandardOutputEncoding = Encoding.UTF8,
    };
    process.Start();
    process.StandardInput.WriteLine(text);
    process.StandardInput.Close();
    var output = process.StandardOutput.ReadToEnd();
    process.WaitForExit();
    
    return output
        .Split('\n', StringSplitOptions.RemoveEmptyEntries)
        .SelectMany(line => JsonSerializer.Deserialize<Lemma[]>(line) ?? []);
}

List<string?> Lemmatize(string text) =>
    text.Split('\n')
        .SelectMany(Analyze)
        .Select(lemma => lemma?.Analysis?.FirstOrDefault()?.Lexema ?? lemma?.Text)
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .ToList();

partial class Program
{
    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex TokenizerRegex();
}
