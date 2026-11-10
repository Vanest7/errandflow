import type { RoutePathDto } from '../../../core/api/generated';
import { mapRoutePath } from './map-route-path';

describe('mapRoutePath', () => {
  it('maps route coordinates and calculates totals', () => {
    const path: RoutePathDto = {
      legs: [
        {
          fromLocationId: 'origin',
          toLocationId: 'destination',
          distanceMeters: 1200,
          travelDurationSeconds: 900,
          coordinates: [
            { latitude: 39.47, longitude: -0.38 },
            { latitude: 39.48, longitude: -0.37 },
          ],
        },
      ],
    };

    const result = mapRoutePath(path);

    expect(result).toEqual({
      legs: [
        [
          { latitude: 39.47, longitude: -0.38 },
          { latitude: 39.48, longitude: -0.37 },
        ],
      ],
      distanceMeters: 1200,
      travelDurationSeconds: 900,
    });
  });

  it('rejects invalid coordinates', () => {
    const path: RoutePathDto = {
      legs: [
        {
          fromLocationId: 'origin',
          toLocationId: 'destination',
          distanceMeters: 100,
          travelDurationSeconds: 60,
          coordinates: [
            { latitude: 95, longitude: 0 },
            { latitude: 39.48, longitude: -0.37 },
          ],
        },
      ],
    };

    expect(mapRoutePath(path)).toBeNull();
  });

  it('preserves an empty route', () => {
    expect(mapRoutePath({ legs: [] })).toEqual({
      legs: [],
      distanceMeters: 0,
      travelDurationSeconds: 0,
    });
  });
});
