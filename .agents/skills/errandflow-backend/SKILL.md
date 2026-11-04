---
name: errandflow-backend
description: Implement or review ErrandFlow C# ASP.NET Core use cases, contracts,
  persistence and Clean Architecture boundaries.
---

# Backend feature workflow
Read backend/AGENTS.md and the active spec/plan before editing.
1. Locate existing equivalents and verify project references and selected SDK.
2. Put invariants in Domain and orchestration in a feature-specific Application use case.
3. Define minimal input/output DTOs and ports for required I/O. Avoid unnecessary interfaces.
4. Implement adapters in Infrastructure and connect them in Api's composition root.
5. Specify validation, cancellation, transaction scope, concurrency and error mapping.
6. Keep provider calls bounded and outside long database transactions. Retry only safe operations.
7. Add unit tests for changed rules and integration tests for meaningful storage/API behavior.
8. Run the actual solution build and relevant tests. Report commands and outcomes.
Before public data persistence, enforce ownership at queries and mutations, not only UI routes.
Do not choose a database, identity provider or framework version without recording the decision.
Output: use case, dependency changes, contracts, migration impact and verification.
