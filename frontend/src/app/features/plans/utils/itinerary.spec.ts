import type { ErrandDto, LocationDto, PlanScheduleDto } from '../../../core/api/generated';
import {
  buildItineraryStops,
  elapsedMinutes,
  formatItineraryTime,
  isValidTimeZone,
  wholeMinutes,
} from './itinerary';

const schedule: PlanScheduleDto = {
  planId: 'plan-1',
  revision: 3,
  travelMode: 'Walking',
  departureAtUtc: '2026-11-10T08:00:00Z',
  finishedAtUtc: '2026-11-10T08:45:00Z',
  isFeasible: true,
  conflict: null,
  scheduledErrands: [
    {
      errandId: 'errand-1',
      locationId: 'location-1',
      arrivalAtUtc: '2026-11-10T08:10:00Z',
      startAtUtc: '2026-11-10T08:15:00Z',
      finishAtUtc: '2026-11-10T08:30:00Z',
      travelDurationSeconds: 600,
      waitingDurationSeconds: 300,
    },
  ],
};

const errands: ErrandDto[] = [
  {
    id: 'errand-1',
    planId: 'plan-1',
    locationId: 'location-1',
    title: 'Collect prescription',
    durationMinutes: 15,
    originalPosition: 1,
    earliestStartAt: null,
    latestFinishAt: null,
    fixedStartAt: null,
    createdAt: '2026-11-09T12:00:00Z',
    updatedAt: '2026-11-09T12:00:00Z',
  },
];

const locations: LocationDto[] = [
  {
    id: 'location-1',
    planId: 'plan-1',
    label: 'Pharmacy',
    address: null,
    latitude: 39.47,
    longitude: -0.38,
  },
];

describe('itinerary', () => {
  it('preserves the scheduled order and distinguishes travel, waiting and service', () => {
    expect(buildItineraryStops(schedule, errands, locations)).toEqual([
      {
        id: 'errand-1',
        title: 'Collect prescription',
        locationLabel: 'Pharmacy',
        arrivalAtUtc: '2026-11-10T08:10:00Z',
        startAtUtc: '2026-11-10T08:15:00Z',
        finishAtUtc: '2026-11-10T08:30:00Z',
        travelMinutes: 10,
        waitingMinutes: 5,
        serviceMinutes: 15,
      },
    ]);
  });

  it('does not invent a finish time for an infeasible schedule', () => {
    expect(elapsedMinutes(schedule.departureAtUtc, null)).toBeNull();
  });

  it('calculates elapsed duration across midnight', () => {
    expect(elapsedMinutes('2026-11-10T23:40:00Z', '2026-11-11T00:10:00Z')).toBe(30);
  });

  it('does not reinterpret timestamps without an explicit offset', () => {
    expect(elapsedMinutes('2026-11-10T09:00:00', '2026-11-10T09:30:00Z')).toBeNull();
    expect(formatItineraryTime('2026-11-10T09:00:00', 'es', 'Europe/Madrid')).toBe('—');
  });

  it('handles the spring daylight-saving transition', () => {
    const time = formatItineraryTime('2026-03-29T01:30:00Z', 'es', 'Europe/Madrid');
    expect(time).toContain('03:30');
    expect(time).toContain('GMT+2');
  });

  it('disambiguates the repeated hour in autumn using the UTC offset', () => {
    const first = formatItineraryTime('2026-10-25T00:30:00Z', 'es', 'Europe/Madrid');
    const second = formatItineraryTime('2026-10-25T01:30:00Z', 'es', 'Europe/Madrid');

    expect(first).toContain('02:30 GMT+2');
    expect(second).toContain('02:30 GMT+1');
  });

  it('rejects unsupported time zones and invalid durations', () => {
    expect(isValidTimeZone('Europe/Madrid')).toBe(true);
    expect(isValidTimeZone('Invalid/Zone')).toBe(false);
    expect(wholeMinutes('invalid')).toBeNull();
    expect(wholeMinutes(61)).toBe(2);
  });
});
