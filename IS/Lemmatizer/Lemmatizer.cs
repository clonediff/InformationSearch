using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Lemmatizer;

public static partial class Lemmatizer
{
    private const string StopWordsFile = "StopWords.txt";
    private const string MyStemExe = "mystem.exe";

    public static async Task<IEnumerable<string>> ClearLemmasAsync(List<string?> lemmas)
    {
        var stopWords = await GetStopWords();
        return lemmas
            .SelectMany(lemma => TokenizerRegex()
                .Matches(lemma ?? string.Empty)
                .Select(m => m.Value)
                .Where(w => !stopWords.Contains(w))
            );
    }

    private static async Task<ImmutableHashSet<string>> GetStopWords()
    {
        var stopWordsText = await File.ReadAllTextAsync(StopWordsFile);
        return stopWordsText
            .Split(Array.Empty<char>(), StringSplitOptions.RemoveEmptyEntries)
            .ToImmutableHashSet();
    }

    private static IEnumerable<Lemma?> Analyze(string text)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = MyStemExe,
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

    public static List<string?> Lemmatize(string text) =>
        Analyze(text)
            .Select(lemma => lemma?.Analysis?.FirstOrDefault()?.Lexema ?? lemma?.Text)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToList();
    
    [GeneratedRegex(@"\p{L}+")]
    private static partial Regex TokenizerRegex();
}
