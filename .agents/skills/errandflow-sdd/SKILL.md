---
name: errandflow-sdd
description: Maintain ErrandFlow specifications, plans, tasks and requirement evidence.
  Use for feature definition, implementation planning or scope changes.
---

# Spec-driven workflow
Read root AGENTS.md, docs/constitution.md, docs/product.md and the active spec.
1. Capture the requested outcome and existing authorization. Separate settled decisions from proposals.
2. For substantial work create specs/NNN-feature/spec.md using references/templates.md.
   Use EARS-style observable requirements with IDs. Describe what/why, not implementation.
3. Resolve only questions that block correctness. Do not silently pick product policies.
4. Write plan.md mapping requirements to files, boundaries, contracts and tests.
5. Write dependency-ordered tasks.md, each with requirement IDs and verifiable completion.
6. Implement only the requested task or authorized batch. Keep the spec synchronized with changes.
7. Validate requirement by requirement; record pass/fail/not-run and evidence in validation.md.
8. Update MEMORY.md with verified state and next step. Never infer approval from elapsed time.
Use a short plan for small reversible fixes. Stop after one task only if the user requested one.
Output: changed paths, covered requirements, executed checks and remaining decisions.
