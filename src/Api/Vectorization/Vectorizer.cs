using Rinha.Fraud.Contracts;

namespace Rinha.Fraud.Vectorization;

internal sealed class Vectorizer
{
    private readonly MccRiskTable _mccRiskTable;

    public Vectorizer(MccRiskTable mccRiskTable)
    {
        ArgumentNullException.ThrowIfNull(mccRiskTable);
        _mccRiskTable = mccRiskTable;
    }

    public void Write(FraudRequest request, Span<float> dst)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestedAtTicks = IsoTimestamp.ParseToUtcTicks(request.Transaction.RequestedAt);
        var hour = IsoTimestamp.HourOfDay(request.Transaction.RequestedAt);
        var dow = IsoTimestamp.DayOfWeekMondayZero(requestedAtTicks);

        dst[0] = Clamp((float)(request.Transaction.Amount / NormalizationConstants.MaxAmount));
        dst[1] = Clamp((float)(request.Transaction.Installments / NormalizationConstants.MaxInstallments));
        dst[2] = AmountVsAvg(request);
        dst[3] = (float)(hour / 23d);
        dst[4] = (float)(dow / 6d);

        if (request.LastTransaction is { } last)
        {
            var lastTicks = IsoTimestamp.ParseToUtcTicks(last.Timestamp);
            var minutes = IsoTimestamp.MinutesBetween(lastTicks, requestedAtTicks);
            dst[5] = Clamp((float)(minutes / NormalizationConstants.MaxMinutes));
            dst[6] = Clamp((float)(last.KmFromCurrent / NormalizationConstants.MaxKm));
        }
        else
        {
            dst[5] = NormalizationConstants.NullHistorySentinel;
            dst[6] = NormalizationConstants.NullHistorySentinel;
        }

        dst[7] = Clamp((float)(request.Terminal.KmFromHome / NormalizationConstants.MaxKm));
        dst[8] = Clamp((float)(request.Customer.TxCount24h / NormalizationConstants.MaxTxCount24h));
        dst[9] = request.Terminal.IsOnline ? 1f : 0f;
        dst[10] = request.Terminal.CardPresent ? 1f : 0f;
        dst[11] = IsUnknownMerchant(request) ? 1f : 0f;
        dst[12] = _mccRiskTable.Get(request.Merchant.Mcc);
        dst[13] = Clamp((float)(request.Merchant.AvgAmount / NormalizationConstants.MaxMerchantAvgAmount));
    }

    private static float AmountVsAvg(FraudRequest r)
    {
        if (r.Customer.AvgAmount <= 0d)
            return 1f;

        var ratio = r.Transaction.Amount / r.Customer.AvgAmount / NormalizationConstants.AmountVsAvgRatio;
        return Clamp((float)ratio);
    }

    private static bool IsUnknownMerchant(FraudRequest r)
    {
        var known = r.Customer.KnownMerchants;
        if (known is null || known.Count == 0) return true;

        var merchantId = r.Merchant.Id;
        for (var i = 0; i < known.Count; i++)
        {
            if (string.Equals(known[i], merchantId, StringComparison.Ordinal))
                return false;
        }
        return true;
    }

    private static float Clamp(float x) => x switch
    {
        < 0f => 0f,
        > 1f => 1f,
        _ => x,
    };
}
