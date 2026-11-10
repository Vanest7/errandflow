import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  PlansService,
  type PlanDto,
  type PlanInput,
  type PlanSummaryDto,
  type SetPlanLocationsInput,
} from '../../../core/api/generated';

@Injectable({ providedIn: 'root' })
export class PlansApiService {
  private readonly api = inject(PlansService);

  list(): Observable<PlanSummaryDto[]> {
    return this.api.apiPlansGet$Json();
  }

  getById(planId: string): Observable<PlanDto> {
    return this.api.getPlanById$Json({ planId });
  }

  create(input: PlanInput): Observable<PlanDto> {
    return this.api.apiPlansPost$Json({ body: input });
  }

  setLocations(planId: string, input: SetPlanLocationsInput): Observable<PlanDto> {
    return this.api.apiPlansPlanIdLocationsPut$Json({
      planId,
      body: input,
    });
  }
}
