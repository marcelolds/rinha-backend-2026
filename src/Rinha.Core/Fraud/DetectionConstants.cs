namespace Rinha.Core.Fraud;

public static class DetectionConstants
{
    public const int Dimensions = 14;
    public const int K = 5;

    public const float MaxAmount = 10_000f;
    public const float MaxInstallments = 12f;
    public const float AmountVsAvgRatio = 10f;
    public const float MaxMinutes = 1_440f;
    public const float MaxKm = 1_000f;
    public const float MaxTxCount24h = 20f;
    public const float MaxMerchantAvgAmount = 10_000f;

    public static float GetMccRisk(string? mcc)
    {
        return mcc switch
        {
            "5411" => 0.15f,
            "5812" => 0.30f,
            "5912" => 0.20f,
            "5944" => 0.45f,
            "7801" => 0.80f,
            "7802" => 0.75f,
            "7995" => 0.85f,
            "4511" => 0.35f,
            "5311" => 0.25f,
            "5999" => 0.50f,
            _ => 0.50f
        };
    }

    public static float Clamp01(float value)
    {
        if (float.IsNaN(value) || value <= 0f) return 0f;
        return value >= 1f ? 1f : value;
    }
}
