import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import { PlanningService, type OptimizedPlanRouteDto } from '../../../core/api/generated';
export type SupportedTravelMode = 1 | 3;

@Injectable({ providedIn: 'root' })
export class PlanRouteApiService {
  private readonly api = inject(PlanningService);

  getOptimizedRoute(
    planId: string,
    travelMode: SupportedTravelMode,
  ): Observable<OptimizedPlanRouteDto> {
    return this.api.apiPlansPlanIdPlanningOptimizedRouteGet$Json({
      planId,
      travelMode,
    });
  }
}
