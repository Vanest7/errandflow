# Architecture decision: modular monolith
Status: agreed architectural direction.
One backend deployment, four projects under backend/src/: ErrandFlow.Domain,
ErrandFlow.Application, ErrandFlow.Infrastructure and ErrandFlow.Api.
Tests live under backend/tests/. Angular lives under frontend/.
Group application cases under Plans/CreatePlan, Plans/AddErrand and Planning/OptimizePlan.
Avoid cyclic module dependencies; introduce explicit application contracts when crossing modules.
Api calls a use case; the use case obtains data through ports; Domain computes a result;
Infrastructure implements I/O; Api maps the result to an HTTP contract.
The composition root connects implementations. Domain never resolves services itself.
Separate commands and queries without requiring a mediator, event sourcing or separate databases.
Scaling requires measuring algorithm cost, storage queries and provider limits; layering alone
is not a throughput guarantee. Add background jobs only when observed workloads justify them.
Record later material changes in docs/decisions/ with context, decision and consequences.
