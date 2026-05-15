using Rinha.Core.Models;

namespace Rinha.Core.Fraud;

public sealed class FraudScorer
{
    private readonly ReferenceIndex _index;
    private readonly TransactionVectorizer _vectorizer = new();

    public FraudScorer(ReferenceIndex index)
    {
        _index = index;
    }

    public FraudDecision Score(TransactionRequest request)
    {
        Span<float> query = stackalloc float[DetectionConstants.Dimensions];
        _vectorizer.Vectorize(request, query);

        var frauds = _index.GetFraudCountAmongNearest5(query);
        var score = frauds / 5f;
        return new FraudDecision(score < 0.6f, score);
    }
}

public readonly record struct FraudDecision(bool Approved, float FraudScore);
