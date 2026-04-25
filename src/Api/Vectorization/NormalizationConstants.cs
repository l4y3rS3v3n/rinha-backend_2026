namespace Rinha.Fraud.Vectorization;

internal static class NormalizationConstants
{
    public const double MaxAmount = 10000d;
    public const double MaxInstallments = 12d;
    public const double AmountVsAvgRatio = 10d;
    public const double MaxMinutes = 1440d;
    public const double MaxKm = 1000d;
    public const double MaxTxCount24h = 20d;
    public const double MaxMerchantAvgAmount = 10000d;

    public const double DefaultMccRisk = 0.5d;

    public const int VectorDimensions = 14;
    public const int KnnK = 5;
    public const double FraudThreshold = 0.6d;
    public const float NullHistorySentinel = -1f;
}
