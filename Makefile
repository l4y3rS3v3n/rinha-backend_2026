SHELL       := /usr/bin/env bash
.SHELLFLAGS := -eu -o pipefail -c
.DEFAULT_GOAL := help

VERSION         ?= v0.3.1
IMAGE_NAME      ?= rinha-2026-dotnet
IMAGE           ?= $(IMAGE_NAME):$(VERSION)
COMPOSE_FILE    ?= infra/docker-compose.yml
HOSTNET_FILE    ?= infra/docker-compose.hostnet.yml

REFERENCE_LIMIT ?= 0
INDEX_BACKEND   ?= simd
HNSW_EF_SEARCH  ?= 64

GITHUB_USER     ?= l4y3rS3v3n
# GHCR/OCI image refs must be lowercase — keep this in sync with $(GITHUB_USER).
GITHUB_REPO     ?= l4y3rs3v3n/rinha-backend_2026

EXPORT_ENV := \
  RINHA_IMAGE=$(IMAGE) \
  RINHA_REFERENCE_LIMIT=$(REFERENCE_LIMIT) \
  RINHA_INDEX_BACKEND=$(INDEX_BACKEND) \
  RINHA_HNSW_EF_SEARCH=$(HNSW_EF_SEARCH)

K6_BIN := $(shell mise which k6 2>/dev/null || echo k6)

RINHA_UPSTREAM  ?= $(abspath $(CURDIR)/..)/rinha-de-backend-2026

.PHONY: help
help:   ## Show this help
	@awk 'BEGIN {FS = ":.*?## "} \
	     /^[a-zA-Z_-]+:.*?## / {printf "  \033[36m%-18s\033[0m %s\n", $$1, $$2}' \
	     $(MAKEFILE_LIST)

.PHONY: info
info:   ## Show resolved config
	@echo "IMAGE           : $(IMAGE)"
	@echo "VERSION         : $(VERSION)"
	@echo "BACKEND         : $(INDEX_BACKEND)"
	@echo "REFERENCE_LIMIT : $(REFERENCE_LIMIT)"
	@echo "HNSW_EF_SEARCH  : $(HNSW_EF_SEARCH)"
	@echo "K6              : $(K6_BIN)"
	@echo "RINHA_UPSTREAM  : $(RINHA_UPSTREAM)"
	@echo "GITHUB_REPO     : $(GITHUB_REPO)"

.PHONY: fetch-resources
fetch-resources:   ## Copy references + mcc + normalization from sibling rinha repo
	@if [[ ! -d "$(RINHA_UPSTREAM)/resources" ]]; then \
	   echo "error: rinha upstream not found at $(RINHA_UPSTREAM)" >&2; \
	   echo "set RINHA_UPSTREAM=/path/to/rinha-de-backend-2026 or clone it as a sibling" >&2; \
	   exit 1; fi
	@mkdir -p resources
	@cp "$(RINHA_UPSTREAM)/resources/references.json.gz" resources/
	@cp "$(RINHA_UPSTREAM)/resources/mcc_risk.json"      resources/
	@cp "$(RINHA_UPSTREAM)/resources/normalization.json" resources/
	@echo "copied resources from $(RINHA_UPSTREAM)/resources -> ./resources"

.PHONY: build
build:   ## Build linux/amd64 AOT image — `make build VERSION=v0.3.1`
	@if [[ ! "$(VERSION)" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$$ ]]; then \
	   echo "error: VERSION=$(VERSION) is not semver (vX.Y.Z)"; exit 1; fi
	docker build --platform linux/amd64 --network=host \
	  -f infra/Dockerfile -t $(IMAGE) .

.PHONY: up
up:   ## Bring up stack (bridge — requires veth module)
	$(EXPORT_ENV) docker compose -f $(COMPOSE_FILE) up -d --wait

.PHONY: up-hostnet
up-hostnet:   ## Bring up stack with host network (fallback for hosts without veth)
	$(EXPORT_ENV) docker compose -f $(COMPOSE_FILE) -f $(HOSTNET_FILE) up -d --wait

.PHONY: down
down:   ## Tear down stack + volumes
	@docker compose -f $(COMPOSE_FILE) -f $(HOSTNET_FILE) down -v 2>/dev/null \
	  || docker compose -f $(COMPOSE_FILE) down -v

.PHONY: restart
restart: down up   ## down + up (bridge)

.PHONY: restart-hostnet
restart-hostnet: down up-hostnet   ## down + up-hostnet

.PHONY: logs
logs:   ## Tail container logs
	docker compose -f $(COMPOSE_FILE) logs -f --tail=200

.PHONY: smoke
smoke:   ## curl /ready and /fraud-score
	@curl -fsS -o /dev/null -w "READY  http=%{http_code} t=%{time_total}s\n" http://localhost:9999/ready
	@curl -fsS -X POST http://localhost:9999/fraud-score \
	  -H 'content-type: application/json' \
	  -d '{"id":"smoke","transaction":{"amount":41.12,"installments":2,"requested_at":"2026-03-11T18:45:53Z"},"customer":{"avg_amount":82.24,"tx_count_24h":3,"known_merchants":["MERC-003","MERC-016"]},"merchant":{"id":"MERC-016","mcc":"5411","avg_amount":60.25},"terminal":{"is_online":false,"card_present":true,"km_from_home":29.23},"last_transaction":null}' \
	  | sed 's/^/SCORE  /'; echo

.PHONY: test
test:   ## Run k6 load test against the running stack
	@for i in $$(seq 1 30); do \
	   curl -fsS -o /dev/null http://localhost:9999/ready && break; sleep 1; done
	@cd "$(RINHA_UPSTREAM)" && $(K6_BIN) run test/test.js
	@cat "$(RINHA_UPSTREAM)/test/results.json"
	@rm -f "$(RINHA_UPSTREAM)/test/results.json"

.PHONY: diag
diag:   ## Run Python vectorizer vs gold-standard diagnostic
	@RINHA_UPSTREAM="$(RINHA_UPSTREAM)" python3 scripts/diag-vectorizer.py

.PHONY: submission
submission:   ## Materialize submission-branch contents into ./submission-out/
	@if [[ ! "$(VERSION)" =~ ^v[0-9]+\.[0-9]+\.[0-9]+$$ ]]; then \
	   echo "error: VERSION=$(VERSION) is not semver (vX.Y.Z)" >&2; exit 1; fi
	@if [[ -z "$(GITHUB_REPO)" ]]; then \
	   echo "error: GITHUB_REPO is empty" >&2; exit 1; fi
	@rm -rf submission-out
	@mkdir -p submission-out
	@cp info.json         submission-out/info.json
	@cp infra/nginx.conf  submission-out/nginx.conf
	@PINNED="ghcr.io/$(GITHUB_REPO):$(VERSION)"; \
	 sed -E "s|image: \\\$$\\{RINHA_IMAGE:-[^}]*\\}|image: $$PINNED|g" \
	     infra/docker-compose.yml > submission-out/docker-compose.yml; \
	 echo "Wrote submission-out/"; \
	 echo "  - docker-compose.yml  (image -> $$PINNED)"; \
	 echo "  - nginx.conf"; \
	 echo "  - info.json"

.PHONY: ghcr-login
ghcr-login:   ## Authenticate docker with GHCR using gh CLI token
	@gh auth token | docker login ghcr.io -u $(GITHUB_USER) --password-stdin

.PHONY: publish
publish: build   ## Build + tag + push image to GHCR (replaces CI when Actions unavailable)
	docker tag $(IMAGE) ghcr.io/$(GITHUB_REPO):$(VERSION)
	docker push ghcr.io/$(GITHUB_REPO):$(VERSION)
	@echo "published ghcr.io/$(GITHUB_REPO):$(VERSION)"
