# M3 Slice 11 Public-Origin Smoke Runbook

## Purpose

This runbook closes the last remaining gate for **M3 slice 11**.

Use it to verify, against the **real deployed public HTTPS origin**, that:

1. the default-on guarded planner path succeeds through `POST /_internal/planner/calculate`,
2. explicit rollback still returns planner solves to `POST /v2/solver`, and
3. the reverse proxy / TLS terminator is forwarding sanitized public authority to the ASP.NET host.

This runbook is intentionally about the **real public origin**. It is **not** satisfied by localhost, SSH port-forwarding, synthetic local curl with spoofed headers, or in-memory `WebApplicationFactory` tests.

## Preconditions

- The deployed environment is running the branch that includes the local slice-11 commits through `4b62427` or a descendant with the same behavior.
- The public deployment is reachable through its normal HTTPS hostname.
- The reverse proxy or TLS terminator:
  - overwrites or sanitizes `X-Forwarded-Proto`,
  - overwrites or sanitizes `X-Forwarded-Host`,
  - allows only intended public hostnames through to ASP.NET.
- You have one reproducible planner/share URL ready for testing.
- You can observe browser network traffic through DevTools or equivalent tooling.
- You can temporarily set `Planner__UseInternalCalculate=false` in the deployed environment for the rollback smoke.

## Test Inputs To Prepare

Record these before you begin:

- **Environment:** `<staging|production|other>`
- **Public host:** `https://<public-hostname>`
- **Planner/share URL:** `https://<public-hostname>/<version>/production?share=<id>`
- **Deployment identifier:** `<commit SHA / build number / container tag>`
- **Observer:** `<name>`
- **Timestamp start:** `<UTC timestamp>`

## Smoke 1 - Default-On Guarded Path

### Objective

Prove that the live public site uses the guarded internal route under the normal default-on configuration.

### Setup

- Ensure `Planner__UseInternalCalculate` is **unset** or otherwise left in its normal default-on state.
- Open the prepared public planner/share URL in a browser.
- Open DevTools Network before triggering a solve.

### Steps

1. Load the public planner/share URL.
2. Wait for the planner UI to finish loading.
3. Trigger a planner solve or any planner action that causes a fresh calculation.
4. Filter network traffic to `planner`, `solver`, or `calculate`.
5. Inspect the request that performed the solve.

### Expected Result

- The solve request is `POST /_internal/planner/calculate`.
- The request returns **200**.
- Results render successfully in the planner UI.
- There is **no fallback** solve request to `POST /v2/solver` for that action.

### Record

- request path used
- response status
- screenshot or HAR of the solve request
- note whether results rendered correctly

## Smoke 2 - Explicit Rollback Path

### Objective

Prove that the one-flag rollback still works in the deployed public-origin topology.

### Setup

- Set `Planner__UseInternalCalculate=false` in the deployed ASP.NET environment.
- Restart or reload the deployment as required.
- Re-open the same public planner/share URL in a clean browser tab.
- Open DevTools Network before triggering a solve.

If the deployment still renders raw `www/index.php` directly rather than the ASP.NET shell, the equivalent fallback flag is `USE_INTERNAL_PLANNER_CALCULATE=false`.

### Steps

1. Load the same public planner/share URL.
2. Wait for the planner UI to finish loading.
3. Trigger a planner solve.
4. Inspect the solve request in the Network panel.

### Expected Result

- The solve request is `POST /v2/solver`.
- There is **no** `POST /_internal/planner/calculate` for that solve action.
- The request returns **200**.
- Results render successfully in the planner UI.

### Record

- request path used
- response status
- screenshot or HAR of the solve request
- note that rollback was enabled for this run

## Failure Handling

If either smoke fails, record the exact failure mode before changing anything:

- public URL used
- deployment identifier
- whether default-on or rollback mode was active
- expected request path
- actual request path
- status code / error text
- screenshot or HAR

### Common Failure Interpretation

- **Default-on request still goes to `/v2/solver`:** guarded path is not active in deployment or shell config is stale.
- **Default-on request hits `/_internal/planner/calculate` but returns `403`:** forwarded public authority is not being trusted or sanitized correctly by the deployment proxy chain.
- **Default-on request hits `/_internal/planner/calculate` but returns non-200 validation/runtime error:** this is no longer a topology-only issue; record the payload path and error details for follow-up.
- **Rollback still hits `/_internal/planner/calculate`:** rollback flag is not applied in the deployed host/shell path you are testing.

## Evidence Template

Copy and fill this block into the next handoff or continuity update.

```md
## M3 Slice 11 Deployment Smoke Evidence

- Environment:
- Public host:
- Deployment identifier:
- Observer:
- Timestamp:

### Default-On Guarded Path
- Planner URL:
- Request path observed:
- Response status:
- Results rendered: yes/no
- Evidence artifact:
- Notes:

### Rollback Path
- Rollback flag used:
- Planner URL:
- Request path observed:
- Response status:
- Results rendered: yes/no
- Evidence artifact:
- Notes:
```

## Completion Rule

**Do not mark M3 slice 11 complete until both smokes pass and their evidence is recorded.**

Once both pass:

1. update the current slice-11 handoff,
2. update the resume guide / plan / route parity docs to mark slice 11 complete,
3. name the next slice explicitly.
