import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';

import {
  LocationsService,
  type CreateLocationInput,
  type LocationDto,
  type LocationMutationResult,
} from '../../../core/api/generated';

@Injectable({ providedIn: 'root' })
export class LocationsApiService {
  private readonly api = inject(LocationsService);

  list(planId: string): Observable<LocationDto[]> {
    return this.api.apiPlansPlanIdLocationsGet$Json({
      planId,
    });
  }

  create(planId: string, input: CreateLocationInput): Observable<LocationMutationResult> {
    return this.api.apiPlansPlanIdLocationsPost$Json({
      planId,
      body: input,
    });
  }
}
