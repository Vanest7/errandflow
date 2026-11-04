---
name: errandflow-quality
description: Debug ErrandFlow failures or review requirements, architecture, tests,
  accessibility and release evidence.
---

# Quality workflow
1. Identify expected behavior from the spec and actual behavior from reproducible evidence.
2. For a bug, minimize reproduction, form a specific hypothesis and inspect the failing boundary.
3. Add a failing regression test for meaningful logic, apply the smallest fix and rerun it.
4. For review-only work, do not edit sources. Inspect the diff and requirement coverage.
5. Check inward dependencies, ownership, provider failures, time semantics, graph feasibility,
   translation parity, responsive UI and accessible alternatives where relevant.
6. Run available targeted checks; distinguish failures from missing tools or unavailable services.
   A mock proves local behavior, not the real provider's contract or availability.
7. Report requirement ID -> evidence -> PASS/FAIL/NOT RUN. Never mark NOT RUN as PASS.
8. Classify blocking defects separately from optional improvements; include file/line and impact.
9. Review output: PASS, CHANGES REQUIRED or INCOMPLETE, with concise evidence and limitations.
Prefer focused tests over tests that merely mirror implementation details.
