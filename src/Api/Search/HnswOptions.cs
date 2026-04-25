namespace Rinha.Fraud.Search;

internal sealed record HnswOptions(
    int M = 16,                 // max neighbours per node on upper layers
    int MMax0 = 32,             // max neighbours on layer 0 (conventionally 2*M)
    int EfConstruction = 200,   // candidate pool size during build
    int EfSearch = 64,          // candidate pool size during search
    ulong Seed = 1337UL);       // deterministic layer assignment
