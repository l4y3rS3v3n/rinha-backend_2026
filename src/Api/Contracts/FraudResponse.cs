namespace Rinha.Fraud.Contracts;

internal readonly record struct FraudResponse(bool Approved, double FraudScore);
