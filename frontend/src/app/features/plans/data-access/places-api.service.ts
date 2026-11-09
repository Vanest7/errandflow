import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { PlacesService, type PlaceSearchResultDto } from '../../../core/api/generated';

@Injectable({ providedIn: 'root' })
export class PlacesApiService {
  private readonly api = inject(PlacesService);

  search(query: string): Observable<PlaceSearchResultDto[]> {
    return this.api.apiPlacesSearchGet$Json({
      Query: query,
      Limit: 5,
    });
  }
}
