namespace InvIndexAndBoolSearch;

public enum TokenType
{
    Word,
    And,
    Or,
    Not,
    LParen,
    RParen,
    End
}

public class Token
{
    public TokenType Type { get; set; }
    public string? Value { get; set; }

    public Token(TokenType tokenType, string? value = null) => (Type, Value) = (tokenType, value);

    public override string ToString() => Type == TokenType.Word ? $"Word({Value})" : Type.ToString();
}

public class Lexer
{
    private readonly string _input;
    private int _position;

    public Lexer(string input)
    {
        _input = input;
        _position = 0;
    }

    public Token GetNextToken()
    {
        SkipWhiteSpace();
        if (_position >= _input.Length) return new Token(TokenType.End);
        
        var cur = _input[_position];

        if (char.IsLetter(cur))
        {
            var start = _position;
            while (_position < _input.Length && char.IsLetter(_input[_position])) _position++;
            return new Token(TokenType.Word, _input[start.._position]);
        }

        _position++;
        return new Token(cur switch
        {
            '&' => TokenType.And,
            '|' => TokenType.Or,
            '!' => TokenType.Not,
            '(' => TokenType.LParen,
            ')' => TokenType.RParen,
            _ => throw new Exception($"Unexpected character: {cur}")
        });
    }

    private void SkipWhiteSpace()
    {
        while (_position < _input.Length && char.IsWhiteSpace(_input[_position])) _position++;
    }
}
