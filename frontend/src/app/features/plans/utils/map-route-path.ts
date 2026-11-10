import type { RoutePathDto } from '../../../core/api/generated';
import type { MapCoordinates } from '../../../shared/map/models/map-coordinates';

export interface MapRoutePath {
  legs: MapCoordinates[][];
  distanceMeters: number;
  travelDurationSeconds: number;
}

export function mapRoutePath(path: RoutePathDto | null): MapRoutePath | null {
  if (!path || !Array.isArray(path.legs)) {
    return null;
  }

  const legs: MapCoordinates[][] = [];

  let distanceMeters = 0;
  let travelDurationSeconds = 0;

  for (const leg of path.legs) {
    const distance = Number(leg.distanceMeters);
    const duration = Number(leg.travelDurationSeconds);

    if (
      !Number.isFinite(distance) ||
      !Number.isFinite(duration) ||
      distance < 0 ||
      duration < 0 ||
      !Array.isArray(leg.coordinates) ||
      leg.coordinates.length < 2
    ) {
      return null;
    }

    const coordinates: MapCoordinates[] = [];

    for (const coordinate of leg.coordinates) {
      const latitude = Number(coordinate.latitude);
      const longitude = Number(coordinate.longitude);

      if (
        !Number.isFinite(latitude) ||
        !Number.isFinite(longitude) ||
        latitude < -90 ||
        latitude > 90 ||
        longitude < -180 ||
        longitude > 180
      ) {
        return null;
      }

      coordinates.push({
        latitude,
        longitude,
      });
    }

    legs.push(coordinates);

    distanceMeters += distance;
    travelDurationSeconds += duration;
  }

  if (!Number.isFinite(distanceMeters) || !Number.isFinite(travelDurationSeconds)) {
    return null;
  }

  return {
    legs,
    distanceMeters,
    travelDurationSeconds,
  };
}
