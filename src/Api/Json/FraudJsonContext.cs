using System.Text.Json.Serialization;
using Rinha.Fraud.Contracts;

namespace Rinha.Fraud.Json;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    GenerationMode = JsonSourceGenerationMode.Default,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never,
    WriteIndented = false)]
[JsonSerializable(typeof(FraudRequest))]
[JsonSerializable(typeof(FraudResponse))]
[JsonSerializable(typeof(TransactionPayload))]
[JsonSerializable(typeof(CustomerPayload))]
[JsonSerializable(typeof(MerchantPayload))]
[JsonSerializable(typeof(TerminalPayload))]
[JsonSerializable(typeof(LastTransactionPayload))]
internal sealed partial class FraudJsonContext : JsonSerializerContext;
