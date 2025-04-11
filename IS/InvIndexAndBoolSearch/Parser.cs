namespace InvIndexAndBoolSearch;

public class Parser
{
    private readonly Lexer _lexer;
    private Token _current;

    public Parser(string input)
    {
        _lexer = new Lexer(input);
        _current = _lexer.GetNextToken();
    }

    private void Eat(TokenType type)
    {
        if (_current.Type == type) _current = _lexer.GetNextToken();
        else throw new Exception($"Expected token {type}, caught {_current.Type}");
    }

    private static int Precedence(Token token) =>
        token.Type switch
        {
            TokenType.Not => 3,
            TokenType.And => 2,
            TokenType.Or => 1,
            _ => 0
        };

    private List<Token> ToPostfix()
    {
        var res = new List<Token>();
        var opStack = new Stack<Token>();

        while (_current.Type != TokenType.End)
        {
            var token = _current;

            switch (token.Type)
            {
                case TokenType.Word:
                    res.Add(token);
                    Eat(TokenType.Word);
                    break;
                case TokenType.Not:
                    opStack.Push(token);
                    Eat(TokenType.Not);
                    break;
                case TokenType.And:
                case TokenType.Or:
                    while (opStack.Count > 0 &&
                           opStack.Peek().Type != TokenType.LParen &&
                           Precedence(opStack.Peek()) >= Precedence(token))
                        res.Add(opStack.Pop());

                    opStack.Push(token);
                    Eat(token.Type);
                    break;
                case TokenType.LParen:
                    opStack.Push(token);
                    Eat(TokenType.LParen);
                    break;
                case TokenType.RParen:
                    while (opStack.Count > 0 && opStack.Peek().Type != TokenType.LParen) res.Add(opStack.Pop());
                    if (opStack.Count == 0) throw new Exception("Left Paren expected");
                    opStack.Pop();
                    Eat(TokenType.RParen);
                    break;
                case TokenType.End:
                default:
                    throw new Exception($"Unexpected token: {token.Type}");
            }
        }

        while (opStack.Count > 0)
        {
            var op = opStack.Pop();
            if (op.Type is TokenType.LParen or TokenType.RParen) throw new Exception($"Extra parens found: {op.Type}");
            res.Add(op);
        }

        return res;
    }

    private static BoolSearchExpressions BuildAst(List<Token> postfixTokens)
    {
        var stack = new Stack<BoolSearchExpressions>();

        foreach (var token in postfixTokens)
        {
            switch (token.Type)
            {
                case TokenType.Word:
                    stack.Push(new WordExpression(token.Value!));
                    break;
                case TokenType.Not:
                    if (stack.Count < 1) throw new Exception($"Not enough tokens for '{token.Type}'");
                    stack.Push(new NotExpression(stack.Pop()));
                    break;
                case TokenType.And:
                case TokenType.Or:
                    if (stack.Count < 2) throw new Exception($"Not enough tokens for {token.Type}");
                    BoolSearchExpressions right = stack.Pop(), left = stack.Pop();
                    stack.Push(Factory(left, right));
                    break;

                    BoolSearchExpressions Factory(BoolSearchExpressions l, BoolSearchExpressions r) =>
                        token.Type == TokenType.And ? new AndExpression(l, r) : new OrExpression(l, r);
                case TokenType.LParen:
                case TokenType.RParen:
                case TokenType.End:
                default:
                    throw new Exception($"Unexpected token is postfix order: {token.Type}");
            }
        }
        
        if (stack.Count != 1) throw new Exception("Incorrect input");
        
        return stack.Pop();
    }

    public BoolSearchExpressions Parse() => BuildAst(ToPostfix());
}
