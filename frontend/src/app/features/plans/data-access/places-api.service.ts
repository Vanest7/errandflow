import { inject, Injectable } from '@angular/core';
import { map, Observable } from 'rxjs';

import { PlacesService, type PlaceSearchResultDto } from '../../../core/api/generated';

export interface SearchCenter {
  latitude: number;
  longitude: number;
}

const SEARCH_RADIUS_KM = 3;
const EARTH_RADIUS_KM = 6371.0088;

@Injectable({ providedIn: 'root' })
export class PlacesApiService {
  private readonly api = inject(PlacesService);

  searchArea(query: string): Observable<PlaceSearchResultDto[]> {
    return this.api.apiPlacesSearchGet$Json({
      Query: query,
      Limit: 5,
    });
  }

  search(query: string, center: SearchCenter): Observable<PlaceSearchResultDto[]> {
    return this.api
      .apiPlacesSearchGet$Json({
        Query: query,
        Latitude: center.latitude,
        Longitude: center.longitude,
        RadiusKilometers: SEARCH_RADIUS_KM,
        Limit: 10,
      })
      .pipe(
        map((places) =>
          places
            .map((place) => ({
              place,
              distance: this.calculateDistance(
                center,
                Number(place.latitude),
                Number(place.longitude),
              ),
            }))
            .filter(({ distance }) => Number.isFinite(distance) && distance <= SEARCH_RADIUS_KM)
            .sort((a, b) => a.distance - b.distance)
            .slice(0, 5)
            .map(({ place }) => place),
        ),
      );
  }

  private calculateDistance(center: SearchCenter, latitude: number, longitude: number): number {
    const toRadians = (degrees: number): number => (degrees * Math.PI) / 180;

    const latitudeDifference = toRadians(latitude - center.latitude);

    const longitudeDifference = toRadians(longitude - center.longitude);

    const firstLatitude = toRadians(center.latitude);
    const secondLatitude = toRadians(latitude);

    const haversine =
      Math.sin(latitudeDifference / 2) ** 2 +
      Math.cos(firstLatitude) * Math.cos(secondLatitude) * Math.sin(longitudeDifference / 2) ** 2;

    const centralAngle =
      2 * Math.atan2(Math.sqrt(haversine), Math.sqrt(Math.max(0, 1 - haversine)));

    return EARTH_RADIUS_KM * centralAngle;
  }
}
