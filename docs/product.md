# Product baseline

## Established direction
ErrandFlow organizes errands and appointments into a feasible visit schedule.
Stack direction: Angular, C#, ASP.NET Core; responsive web; Transloco ES/EN.
Use graphs/nodes and explainable optimization. GPS supplies only the initial origin.
Backend architecture: modular monolith, Clean Architecture, feature-based use cases.

## Proposed MVP (refine in a feature spec)
Create/save/edit plans, add errands, configure origin/destination and departure time,
calculate visit order, display a map and schedule, compare against the original order,
and explain conflicts. Distinguish Errand from Location: several errands may share a location.
Suggested model: Plan, Errand, Location, TimeWindow, RoutePlan, ScheduledStop.
Suggested initial rules: positive service duration, all errands mandatory, fixed appointments,
waiting before opening, recalculation after edits. These are proposals until specified.

## Deferred
Continuous GPS, live traffic, native mobile, notifications, calendar sync, collaboration,
priority-based omission, microservices and distributed messaging.

## Decide before dependent implementation
- .NET/Angular versions and package manager, based on supported compatible releases.
- Database, ORM and hosting; do not treat earlier suggestions as selected technologies.
- Map, geocoding and travel-time providers, usage terms and cost limits.
- One initial travel mode, optimization objective and maximum plan size.
- Time zone policy, opening-window semantics and whether service must finish before closing.
- Persistence ownership/authentication for any publicly accessible deployment.
- Algorithm and whether results are heuristic, exact or incomplete.

Do not call a route optimal unless an exact method proves it for the stated objective.
Clearly label synthetic travel times. A straight line is not a real road route.
