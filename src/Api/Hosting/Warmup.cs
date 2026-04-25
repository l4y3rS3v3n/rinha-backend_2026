using Rinha.Fraud.Contracts;
using Rinha.Fraud.Scoring;

namespace Rinha.Fraud.Hosting;

internal static class Warmup
{
    private const int DefaultIterations = 500;

    public static void Run(FraudScorer scorer)
    {
        ArgumentNullException.ThrowIfNull(scorer);

        var iterations = ResolveIterations();
        var synthetic = BuildSyntheticRequest();

        for (var i = 0; i < iterations; i++)
            _ = scorer.Score(synthetic);
    }

    private static int ResolveIterations()
    {
        var raw = Environment.GetEnvironmentVariable(ResourcePaths.EnvWarmupIterations);
        return int.TryParse(raw, out var parsed) && parsed >= 0 ? parsed : DefaultIterations;
    }

    private static FraudRequest BuildSyntheticRequest() =>
        new(
            Id: "warmup",
            Transaction: new TransactionPayload(100d, 1, "2026-04-01T12:00:00Z"),
            Customer: new CustomerPayload(150d, 2, ["MERC-001"]),
            Merchant: new MerchantPayload("MERC-001", "5411", 120d),
            Terminal: new TerminalPayload(false, true, 5d),
            LastTransaction: null);
}
