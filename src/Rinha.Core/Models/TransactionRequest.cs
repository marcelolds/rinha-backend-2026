using System.Text.Json.Serialization;

namespace Rinha.Core.Models;

public sealed class TransactionRequest
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("transaction")]
    public TransactionInfo Transaction { get; set; } = new();

    [JsonPropertyName("customer")]
    public CustomerInfo Customer { get; set; } = new();

    [JsonPropertyName("merchant")]
    public MerchantInfo Merchant { get; set; } = new();

    [JsonPropertyName("terminal")]
    public TerminalInfo Terminal { get; set; } = new();

    [JsonPropertyName("last_transaction")]
    public LastTransactionInfo? LastTransaction { get; set; }
}

public sealed class TransactionInfo
{
    [JsonPropertyName("amount")]
    public float Amount { get; set; }

    [JsonPropertyName("installments")]
    public int Installments { get; set; }

    [JsonPropertyName("requested_at")]
    public DateTimeOffset RequestedAt { get; set; }
}

public sealed class CustomerInfo
{
    [JsonPropertyName("avg_amount")]
    public float AvgAmount { get; set; }

    [JsonPropertyName("tx_count_24h")]
    public int TxCount24h { get; set; }

    [JsonPropertyName("known_merchants")]
    public string[] KnownMerchants { get; set; } = [];
}

public sealed class MerchantInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("mcc")]
    public string Mcc { get; set; } = "";

    [JsonPropertyName("avg_amount")]
    public float AvgAmount { get; set; }
}

public sealed class TerminalInfo
{
    [JsonPropertyName("is_online")]
    public bool IsOnline { get; set; }

    [JsonPropertyName("card_present")]
    public bool CardPresent { get; set; }

    [JsonPropertyName("km_from_home")]
    public float KmFromHome { get; set; }
}

public sealed class LastTransactionInfo
{
    [JsonPropertyName("timestamp")]
    public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("km_from_current")]
    public float KmFromCurrent { get; set; }
}
