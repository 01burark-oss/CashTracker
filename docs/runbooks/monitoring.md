# Systemcel Oracle monitoring baseline

This baseline applies to the Oracle VM Docker deployment. Production thresholds must be tuned from observed traffic; the current payment provider remains `Fake` until the company/provider gate.

## Health signals

- Liveness: `GET /api/health/live`; process is running. Do not page on one failed probe.
- Readiness: `GET /api/health/ready`; app can connect to PostgreSQL. Use this for traffic readiness.
- Deployment: Docker Compose container health, Caddy certificate status, image/deploy logs and restart count.
- Host: Oracle VM CPU, memory, disk and Docker volume usage.
- Database: PostgreSQL 18 connections, CPU, memory, disk, backup age and checksum.
- Product: checkout failures, webhook rejection/replay, subscription lifecycle failures, reminder delivery failures, import rejection rate and sustained `429` rate limiting.

## Initial alerts

| Signal | Warning | Critical | First response |
| --- | --- | --- | --- |
| Readiness | 2 failures in 5 minutes | 5 consecutive failures | Check app and PostgreSQL logs; stop rollout |
| HTTP 5xx | >2% for 10 minutes | >5% for 5 minutes | Correlate by trace ID; roll back code if schema-compatible |
| App restarts | 2 in 15 minutes | 3 in 10 minutes | Inspect OOM, exit and health events |
| PostgreSQL connections | >70% for 15 minutes | >85% for 5 minutes | Find leaked/long queries; diagnose before scaling |
| Host or PostgreSQL disk | >70% | >85% | Review growth and backups; expand before write risk |
| Backup age | >26 hours | >36 hours | Check backup timer and take a logical backup if safe |
| Checkout failure | 3 synthetic failures | >10% real attempts | Disable checkout flag; preserve event IDs |
| Webhook processing | Any synthetic signature mismatch | Sustained valid-event failures | Preserve provider event IDs; do not replay blindly |
| Rate limiting | >1% API responses for 15 minutes | >5% for 5 minutes | Separate abuse from bad client retry logic |

## Log contract

Every actionable server log should include UTC timestamp, level, event name, trace ID and a non-secret tenant/business identifier where appropriate. Payment logs may include internal payment/event IDs and state transitions, but never raw provider payloads, card data or auth headers.

Never log connection strings, passwords, AES keys, Clerk tokens or cookies, raw GİB credentials, uploaded documents, full customer records, provider signatures or complete webhook bodies.

## Triage order

1. Confirm scope with liveness/readiness, Docker Compose status and Caddy health.
2. Correlate sanitized logs by trace ID, commit SHA and container restart time.
3. Classify the issue as code, configuration, database, provider or abusive traffic.
4. Follow `release.md`: code rollback does not restore database data; restore requires a verified dump and reconciliation.
5. Record the incident timeline and follow-up owner without copying secrets or customer content.
