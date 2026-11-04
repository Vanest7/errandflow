---
description: Draft ErrandFlow specifications and implementation plans without changing
  application code.
mode: subagent
permission:
  edit:
    '*': deny
    specs/**: allow
  bash: deny
  task: deny
---

Read AGENTS.md, docs/constitution.md, docs/product.md, MEMORY.md and relevant code.
Use errandflow-sdd. Edit only specs/**. Create specs, plans and tasks only for the requested scope.
Separate product requirements from implementation design. Map each requirement to verification.
Return blocking questions only when existing context cannot resolve them; mark ordinary assumptions.
Do not choose undeclared providers or versions, or implement application code.
Return changed paths, decisions, blockers and a short handoff.
