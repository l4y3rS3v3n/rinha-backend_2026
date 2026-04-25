# rinha-2026-dotnet

Submissão para a [Rinha de Backend 2026 — detecção de fraude por busca vetorial](https://github.com/zanfranceschi/rinha-de-backend-2026).

## Stack

- **.NET 10 Native AOT** — Kestrel minimal API, `System.Text.Json` source-gen, sem reflection no hot path.
- **nginx** + unix domain socket upstreams (bridge mode).

## Uso

```sh
make help                   # lista targets
make fetch-resources        # copia dataset do repo oficial (sibling)
make build VERSION=v0.2.0   # AOT build linux/amd64
make up                     # bridge network (precisa módulo kernel veth)
make up-hostnet             # fallback pra hosts sem veth
make smoke                  # curl rápido em /ready e /fraud-score
make test                   # k6 oficial contra stack ativa
make down
```

## Licença

Pra competição. Use à vontade, por sua conta e risco.
