#!/usr/bin/env python3
"""Diagnostic: for every test payload, compute the 14-dim vector the way the
spec describes it (REGRAS_DE_DETECCAO.md) and compare against the ground-truth
`info.vector` present in the rinha test-data.json. Flags dimension-level
divergences so we can isolate bugs in our C# Vectorizer."""
from __future__ import annotations
import json, os, sys, datetime as dt
from collections import Counter

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
UPSTREAM = os.environ.get(
    "RINHA_UPSTREAM",
    os.path.normpath(os.path.join(ROOT, "..", "rinha-de-backend-2026")),
)
TEST_DATA = os.path.join(UPSTREAM, "test", "test-data.json")
MCC_PATH = os.path.join(UPSTREAM, "resources", "mcc_risk.json")

MAX_AMOUNT = 10000
MAX_INSTALLMENTS = 12
AMOUNT_VS_AVG_RATIO = 10
MAX_MINUTES = 1440
MAX_KM = 1000
MAX_TX_24H = 20
MAX_MERCH_AVG = 10000
DEFAULT_MCC = 0.5

mcc_risk = {k: float(v) for k, v in json.load(open(MCC_PATH)).items()}


def clamp(x: float) -> float:
    if x < 0: return 0.0
    if x > 1: return 1.0
    return x


def parse_iso(s: str) -> dt.datetime:
    # "2026-03-11T18:45:53Z"
    return dt.datetime.strptime(s, "%Y-%m-%dT%H:%M:%SZ").replace(tzinfo=dt.timezone.utc)


def vectorize(req: dict) -> list[float]:
    tx = req["transaction"]; cust = req["customer"]; mer = req["merchant"]; term = req["terminal"]
    last = req.get("last_transaction")
    t_req = parse_iso(tx["requested_at"])
    hour = t_req.hour
    # spec: Monday=0, Sunday=6. Python weekday(): Monday=0, Sunday=6. Same mapping.
    dow = t_req.weekday()

    amount_vs_avg = 1.0 if cust["avg_amount"] <= 0 else clamp(
        tx["amount"] / cust["avg_amount"] / AMOUNT_VS_AVG_RATIO)
    v = [
        clamp(tx["amount"] / MAX_AMOUNT),                 # 0
        clamp(tx["installments"] / MAX_INSTALLMENTS),     # 1
        amount_vs_avg,                                    # 2
        hour / 23.0,                                      # 3
        dow / 6.0,                                        # 4
        -1.0,                                             # 5 (placeholder)
        -1.0,                                             # 6 (placeholder)
        clamp(term["km_from_home"] / MAX_KM),             # 7
        clamp(cust["tx_count_24h"] / MAX_TX_24H),         # 8
        1.0 if term["is_online"] else 0.0,                # 9
        1.0 if term["card_present"] else 0.0,             # 10
        0.0 if mer["id"] in cust["known_merchants"] else 1.0,  # 11
        mcc_risk.get(mer["mcc"], DEFAULT_MCC),            # 12
        clamp(mer["avg_amount"] / MAX_MERCH_AVG),         # 13
    ]
    if last is not None:
        t_last = parse_iso(last["timestamp"])
        minutes = (t_req - t_last).total_seconds() / 60.0
        v[5] = clamp(minutes / MAX_MINUTES)
        v[6] = clamp(last["km_from_current"] / MAX_KM)
    return v


def close(a: float, b: float, tol: float = 1e-3) -> bool:
    # info.vector is rounded to 4 decimals in the file; tolerate 1e-3.
    return abs(a - b) <= tol


def main() -> int:
    data = json.load(open(TEST_DATA))
    total = 0
    vector_mismatch = 0
    dim_err = Counter()
    examples_per_dim: dict[int, list] = {i: [] for i in range(14)}

    for entry in data["entries"]:
        total += 1
        req = entry["request"]
        expected = entry["info"]["vector"]
        computed = vectorize(req)
        mismatched_dims = [i for i in range(14) if not close(computed[i], expected[i])]
        if mismatched_dims:
            vector_mismatch += 1
            for d in mismatched_dims:
                dim_err[d] += 1
                if len(examples_per_dim[d]) < 3:
                    examples_per_dim[d].append(
                        (req["id"], computed[d], expected[d]))

    print(f"total entries: {total}")
    print(f"vectors matching: {total - vector_mismatch} "
          f"({(total - vector_mismatch) / total * 100:.2f}%)")
    print(f"vectors with at least one wrong dim: {vector_mismatch}")
    print()
    print("per-dim errors (dim : count):")
    for d in range(14):
        if dim_err[d]:
            print(f"  dim {d:>2}: {dim_err[d]:>5}")
    print()
    for d, exs in examples_per_dim.items():
        if exs:
            print(f"dim {d} examples (id / computed / expected):")
            for i, c, e in exs:
                print(f"  {i}  ours={c:.6f}  gold={e:.6f}  diff={c - e:+.6f}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
