import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PlanningService, type OptimizedPlanRouteDto } from '../../../core/api/generated';

@Injectable({ providedIn: 'root' })
export class PlanRouteApiService {
  private readonly api = inject(PlanningService);

  getOptimizedRoute(planId: string): Observable<OptimizedPlanRouteDto> {
    return this.api.apiPlansPlanIdPlanningOptimizedRouteGet$Json({
      planId,
      travelMode: 1,
    });
  }
}
