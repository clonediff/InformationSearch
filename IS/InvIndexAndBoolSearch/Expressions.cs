namespace InvIndexAndBoolSearch;

public abstract class BoolSearchExpressions
{
    public abstract IEnumerable<string> FindDocuments(Dictionary<string, HashSet<string>> invIndex,
        HashSet<string> documents);
}

public abstract class UnaryExpression : BoolSearchExpressions
{
    public abstract string Operation { get; }
    public BoolSearchExpressions InnerExpression { get; }

    protected UnaryExpression(BoolSearchExpressions innerExpression) => InnerExpression = innerExpression;

    public override string ToString() => $"{Operation}{InnerExpression}";
}

public abstract class BinaryExpression(BoolSearchExpressions leftExpression, BoolSearchExpressions rightExpression)
    : BoolSearchExpressions
{
    public abstract string Operation { get; }
    public BoolSearchExpressions Left { get; } = leftExpression;
    public BoolSearchExpressions Right { get; } = rightExpression;

    public override string ToString() => $"({Left} {Operation} {Right})";
}

public abstract class BoolSearchExpressions<T> : BoolSearchExpressions
{
    public T Content { get; }
    public BoolSearchExpressions(T content) => Content = content;

    public override string? ToString() => Content?.ToString();
}

public class WordExpression(string word) : BoolSearchExpressions<string>(word)
{
    public override HashSet<string> FindDocuments(Dictionary<string, HashSet<string>> invIndex, HashSet<string> _) =>
        invIndex.TryGetValue(Content, out var res) ? res : [];
}

public class NotExpression(BoolSearchExpressions inner) : UnaryExpression(inner)
{
    public override string Operation => "!";

    public override IEnumerable<string> FindDocuments(Dictionary<string, HashSet<string>> invIndex,
        HashSet<string> documents) => documents.Except(InnerExpression.FindDocuments(invIndex, documents));
}

public class AndExpression(BoolSearchExpressions left, BoolSearchExpressions right) : BinaryExpression(left, right)
{
    public override string Operation => "&";

    public override IEnumerable<string> FindDocuments(Dictionary<string, HashSet<string>> invIndex,
        HashSet<string> documents) =>
        Left.FindDocuments(invIndex, documents).Intersect(Right.FindDocuments(invIndex, documents));
}

public class OrExpression(BoolSearchExpressions left, BoolSearchExpressions right) : BinaryExpression(left, right)
{
    public override string Operation => "+";

    public override IEnumerable<string> FindDocuments(Dictionary<string, HashSet<string>> invIndex,
        HashSet<string> documents) =>
        Left.FindDocuments(invIndex, documents).Union(Right.FindDocuments(invIndex, documents));
}
