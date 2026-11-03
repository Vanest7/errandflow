# Backend rules
Apply root AGENTS.md first.
- Domain: entities, value objects, invariants, pure planning logic.
- Application: feature use cases, DTOs and required ports.
- Infrastructure: persistence, external routing adapters and migrations.
- Api: HTTP mapping, composition root and cross-cutting middleware.
- Allowed project references: Application -> Domain; Infrastructure -> Application/Domain;
  Api -> Application/Infrastructure (Infrastructure only for composition).
- Keep business rules out of endpoints and storage adapters. Avoid generic service dumping grounds.
- Use intent-specific persistence ports when needed; do not wrap every class in an interface.
- Keep DTOs separate from persisted entities. Validate input and protect domain invariants.
- Use async I/O and propagate CancellationToken. Do not parallelize operations on a shared DbContext.
- Centralize HTTP error mapping with stable codes; never disclose stack traces to clients.
- Define transaction boundaries, ownership checks and concurrency behavior per use case.
- Use named units for durations/distances and deterministic algorithm tie-breaking.
- Verify dotnet commands against the chosen SDK and solution path; do not assume scaffolding exists.
