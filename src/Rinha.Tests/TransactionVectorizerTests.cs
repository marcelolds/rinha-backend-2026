using Rinha.Core.Fraud;
using Rinha.Core.Models;
using Xunit;

namespace Rinha.Tests;

public sealed class TransactionVectorizerTests
{
    [Fact]
    public void Vectorize_LegitExampleFromDocs()
    {
        var request = new TransactionRequest
        {
            Id = "tx-1329056812",
            Transaction = new TransactionInfo
            {
                Amount = 41.12f,
                Installments = 2,
                RequestedAt = DateTimeOffset.Parse("2026-03-11T18:45:53Z")
            },
            Customer = new CustomerInfo
            {
                AvgAmount = 82.24f,
                TxCount24h = 3,
                KnownMerchants = ["MERC-003", "MERC-016"]
            },
            Merchant = new MerchantInfo
            {
                Id = "MERC-016",
                Mcc = "5411",
                AvgAmount = 60.25f
            },
            Terminal = new TerminalInfo
            {
                IsOnline = false,
                CardPresent = true,
                KmFromHome = 29.23f
            },
            LastTransaction = null
        };

        Span<float> vector = stackalloc float[DetectionConstants.Dimensions];
        new TransactionVectorizer().Vectorize(request, vector);

        AssertNear(0.0041f, vector[0]);
        AssertNear(0.1667f, vector[1]);
        AssertNear(0.05f, vector[2]);
        AssertNear(0.7826f, vector[3]);
        AssertNear(0.3333f, vector[4]);
        Assert.Equal(-1f, vector[5]);
        Assert.Equal(-1f, vector[6]);
        AssertNear(0.0292f, vector[7]);
        AssertNear(0.15f, vector[8]);
        Assert.Equal(0f, vector[9]);
        Assert.Equal(1f, vector[10]);
        Assert.Equal(0f, vector[11]);
        AssertNear(0.15f, vector[12]);
        AssertNear(0.0060f, vector[13]);
    }

    [Fact]
    public void Vectorize_FraudExampleFromDocs()
    {
        var request = new TransactionRequest
        {
            Id = "tx-3330991687",
            Transaction = new TransactionInfo
            {
                Amount = 9505.97f,
                Installments = 10,
                RequestedAt = DateTimeOffset.Parse("2026-03-14T05:15:12Z")
            },
            Customer = new CustomerInfo
            {
                AvgAmount = 81.28f,
                TxCount24h = 20,
                KnownMerchants = ["MERC-008", "MERC-007", "MERC-005"]
            },
            Merchant = new MerchantInfo
            {
                Id = "MERC-068",
                Mcc = "7802",
                AvgAmount = 54.86f
            },
            Terminal = new TerminalInfo
            {
                IsOnline = false,
                CardPresent = true,
                KmFromHome = 952.27f
            },
            LastTransaction = null
        };

        Span<float> vector = stackalloc float[DetectionConstants.Dimensions];
        new TransactionVectorizer().Vectorize(request, vector);

        AssertNear(0.9506f, vector[0]);
        AssertNear(0.8333f, vector[1]);
        Assert.Equal(1f, vector[2]);
        AssertNear(0.2174f, vector[3]);
        AssertNear(0.8333f, vector[4]);
        Assert.Equal(-1f, vector[5]);
        Assert.Equal(-1f, vector[6]);
        AssertNear(0.9523f, vector[7]);
        Assert.Equal(1f, vector[8]);
        Assert.Equal(0f, vector[9]);
        Assert.Equal(1f, vector[10]);
        Assert.Equal(1f, vector[11]);
        AssertNear(0.75f, vector[12]);
        AssertNear(0.0055f, vector[13]);
    }

    private static void AssertNear(float expected, float actual)
    {
        Assert.InRange(actual, expected - 0.0001f, expected + 0.0001f);
    }
}
