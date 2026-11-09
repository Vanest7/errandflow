import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { LocationsService, type LocationDto } from '../../../core/api/generated';

@Injectable({ providedIn: 'root' })
export class LocationsApiService {
  private readonly api = inject(LocationsService);

  list(planId: string): Observable<LocationDto[]> {
    return this.api.apiPlansPlanIdLocationsGet$Json({ planId });
  }
}
