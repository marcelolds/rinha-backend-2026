using Rinha.Core.Models;

namespace Rinha.Core.Fraud;

public sealed class TransactionVectorizer
{
    public void Vectorize(TransactionRequest request, Span<float> vector)
    {
        if (vector.Length < DetectionConstants.Dimensions)
        {
            throw new ArgumentException("Vector buffer must have at least 14 positions.", nameof(vector));
        }

        var tx = request.Transaction;
        var customer = request.Customer;
        var merchant = request.Merchant;
        var terminal = request.Terminal;

        vector[0] = DetectionConstants.Clamp01(tx.Amount / DetectionConstants.MaxAmount);
        vector[1] = DetectionConstants.Clamp01(tx.Installments / DetectionConstants.MaxInstallments);
        vector[2] = DetectionConstants.Clamp01(SafeDivide(tx.Amount, customer.AvgAmount) / DetectionConstants.AmountVsAvgRatio);
        vector[3] = tx.RequestedAt.UtcDateTime.Hour / 23f;
        vector[4] = ToMondayBasedDayOfWeek(tx.RequestedAt.UtcDateTime.DayOfWeek) / 6f;

        if (request.LastTransaction is null)
        {
            vector[5] = -1f;
            vector[6] = -1f;
        }
        else
        {
            var minutes = (float)(tx.RequestedAt - request.LastTransaction.Timestamp).TotalMinutes;
            vector[5] = DetectionConstants.Clamp01(minutes / DetectionConstants.MaxMinutes);
            vector[6] = DetectionConstants.Clamp01(request.LastTransaction.KmFromCurrent / DetectionConstants.MaxKm);
        }

        vector[7] = DetectionConstants.Clamp01(terminal.KmFromHome / DetectionConstants.MaxKm);
        vector[8] = DetectionConstants.Clamp01(customer.TxCount24h / DetectionConstants.MaxTxCount24h);
        vector[9] = terminal.IsOnline ? 1f : 0f;
        vector[10] = terminal.CardPresent ? 1f : 0f;
        vector[11] = IsKnownMerchant(merchant.Id, customer.KnownMerchants) ? 0f : 1f;
        vector[12] = DetectionConstants.GetMccRisk(merchant.Mcc);
        vector[13] = DetectionConstants.Clamp01(merchant.AvgAmount / DetectionConstants.MaxMerchantAvgAmount);
    }

    private static float SafeDivide(float numerator, float denominator)
    {
        return denominator <= 0f ? 1f : numerator / denominator;
    }

    private static int ToMondayBasedDayOfWeek(DayOfWeek day)
    {
        return day == DayOfWeek.Sunday ? 6 : (int)day - 1;
    }

    private static bool IsKnownMerchant(string merchantId, string[] knownMerchants)
    {
        for (var i = 0; i < knownMerchants.Length; i++)
        {
            if (string.Equals(merchantId, knownMerchants[i], StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
