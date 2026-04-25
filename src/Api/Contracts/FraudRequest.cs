namespace Rinha.Fraud.Contracts;

internal sealed record FraudRequest(
    string Id,
    TransactionPayload Transaction,
    CustomerPayload Customer,
    MerchantPayload Merchant,
    TerminalPayload Terminal,
    LastTransactionPayload? LastTransaction);

internal sealed record TransactionPayload(
    double Amount,
    int Installments,
    string RequestedAt);

internal sealed record CustomerPayload(
    double AvgAmount,
    int TxCount24h,
    IReadOnlyList<string> KnownMerchants);

internal sealed record MerchantPayload(
    string Id,
    string Mcc,
    double AvgAmount);

internal sealed record TerminalPayload(
    bool IsOnline,
    bool CardPresent,
    double KmFromHome);

internal sealed record LastTransactionPayload(
    string Timestamp,
    double KmFromCurrent);
