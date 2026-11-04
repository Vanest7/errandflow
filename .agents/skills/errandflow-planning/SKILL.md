---
name: errandflow-planning
description: Design, implement or test ErrandFlow weighted graphs, visit ordering,
  time windows and route feasibility.
---

# Planning workflow
Load errandflow-time whenever schedules or time windows change.
1. Identify the objective, constraints, travel mode, size limit and input provenance from the spec.
   Ask about unresolved choices before claiming a meaningful optimum.
2. Build a directed travel-time matrix over locations. Do not assume symmetric costs.
   Represent unreachable pairs explicitly, never as zero. Preserve multiple errands per location.
3. Separate provider data retrieval from a pure solver. Inject data, budgets and tie-breaking.
4. Compute arrival from previous departure plus travel; service start respects arrival and opening;
   departure includes service duration. Enforce the specified appointment/window semantics.
5. Distinguish shortest paths between points from ordering multiple stops with constraints.
6. For a heuristic, return best found plus limitations. Budget exhaustion is not proof of infeasibility.
7. Validate every returned route independently: each mandatory errand exactly once, valid edges,
   fixed origin/destination and all temporal constraints. Reject stale results after plan edits.
8. Test asymmetric edges, unreachable nodes, shared locations, waiting, impossible appointments,
   zero/one errand, ties and size/budget limits. Compare tiny cases with exhaustive enumeration.
9. Compare baseline and proposed route under identical assumptions. Do not invent savings when
   baseline is infeasible; report feasibility separately. Label synthetic inputs.
Output: assumptions, method, complexity/budget, feasibility status and reproducible evidence.
