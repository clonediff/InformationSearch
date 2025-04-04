using System.Text.Json.Serialization;

namespace Lemmatization;

class Analysis
{
    [JsonPropertyName("lex")]
    public string Lexema { get; set; } = null!;
    [JsonPropertyName("gr")]
    public string Grammems { get; set; } = null!;
}

class Lemma
{
    [JsonPropertyName("analysis")]
    public Analysis[]? Analysis { get; set; }
    [JsonPropertyName("text")]
    public string Text { get; set; } = null!;
}
