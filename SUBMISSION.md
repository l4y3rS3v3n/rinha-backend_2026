# Checklist de submissão — l4y3rS3v3n

Passo a passo para enviar a submissão oficial à Rinha 2026.

- **Repositório**: https://github.com/l4y3rS3v3n/rinha-backend_2026
- **Imagem GHCR**: `ghcr.io/l4y3rs3v3n/rinha-backend_2026:vX.Y.Z`
- **Participant ID**: `l4y3rS3v3n-dotnet`

## Pré-requisitos (uma vez)

- [ ] Habilitar `veth` no kernel (para validar bridge mode localmente):
      ```
      sudo modprobe veth
      echo veth | sudo tee /etc/modules-load.d/veth.conf
      ```
- [ ] Criar repositório público `l4y3rS3v3n/rinha-backend_2026` no GitHub (se ainda não existir).
- [ ] Habilitar packages no repo: Settings → Actions → General → *"Read and write permissions"*.

## Publicar a imagem no GHCR

```sh
git init                                     # se ainda não inicializado
git add .
git commit -m "initial submission"
git remote add origin git@github.com:l4y3rS3v3n/rinha-backend_2026.git
git branch -M main
git push -u origin main

# Dispara o workflow `publish` — publica ghcr.io/l4y3rs3v3n/rinha-backend_2026:v0.1.0
git tag v0.1.0
git push origin v0.1.0
```

- [ ] Workflow `publish` verde em Actions.
- [ ] `docker pull ghcr.io/l4y3rs3v3n/rinha-backend_2026:v0.1.0` baixa a imagem.
- [ ] Tornar o package público: Packages → rinha-backend_2026 → Settings → Change visibility → Public.

## Validar localmente em **bridge** mode

```sh
make up                    # levanta nginx + 2 APIs em rede bridge
make smoke                 # sanidade
make test                  # k6 oficial do repo da rinha
make down
```

- [ ] p99 < 10 ms
- [ ] accuracy > 95 %
- [ ] 0 http_errors
- [ ] max < 50 ms

Se algum item falha em **bridge** mas passa em **hostnet** (`make up-hostnet`), investigar conexão inter-container antes de submeter.

## Gerar a branch `submission`

```sh
make submission VERSION=v0.1.0

git checkout --orphan submission
git rm -rf .
cp submission-out/docker-compose.yml submission-out/nginx.conf submission-out/info.json .
git add docker-compose.yml nginx.conf info.json
git commit -m "submission v0.1.0"
git push -u origin submission

git checkout main
```

- [ ] Branch `submission` existe no GitHub.
- [ ] `docker-compose.yml` na `submission` tem `image: ghcr.io/l4y3rs3v3n/rinha-backend_2026:v0.1.0` (semver, sem `:latest`).
- [ ] Apenas 3 arquivos na `submission`: `docker-compose.yml`, `nginx.conf`, `info.json`.

## Inscrever na Rinha

1. Fork do repo oficial `zanfranceschi/rinha-de-backend-2026` (já feito em `~/projects/rinha-de-backend-2026/`).
2. Adicionar `participants/l4y3rS3v3n.json`:
   ```json
   [{
     "id": "l4y3rS3v3n-dotnet",
     "repo": "https://github.com/l4y3rS3v3n/rinha-backend_2026"
   }]
   ```
3. Abrir PR pro repo oficial.

- [ ] PR aberto.

## Disparar o teste oficial

- [ ] Abrir issue em `zanfranceschi/rinha-de-backend-2026` com descrição: `rinha/test l4y3rS3v3n-dotnet`.
- [ ] Aguardar o bot `arinhadebackend` postar o resultado como comentário.
- [ ] Se reprovar, ajustar código, publicar nova tag `v0.1.1`, regerar `submission`, reabrir a mesma issue.

## Estimativas

Dev box (x86-64 moderno, Docker Linux), backend `simd` (default), k=5, threshold=0.6:
- p99 **1.05 ms** / accuracy 98.95% / FN=79 / FP=72
- raw_score local (k6): 13.895 — **fórmula simplificada do k6 ≠ engine oficial**

Backend `hnsw` (descartado): p99 0.69 ms mas FN=133 (-54 fraudes), raw_score 13.723.
Decisão: **SIMD brute-force exato** vence por accuracy.

Projeção no Mac Mini 2014 da Rinha:
- p99 esperado ~2.5–3.5 ms (hardware ~3× mais lento), `latency_multiplier` = 1.0.
- Score real do engine oficial é apurado por `rate_component` + `absolute_penalty`
  (ver issues #59 e #103 do repo oficial), não pelo raw do k6 local.
- Líder atual (Lothyriel Rust, vp-tree, issue #103): score **2.519**.
- Estimativa nossa no engine oficial: **2.500–2.800**, mesma ordem do líder.
