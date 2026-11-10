import type { ErrandDto, LocationDto, PlanScheduleDto } from '../../../core/api/generated';

export interface ItineraryStop {
  id: string;
  title: string | null;
  locationLabel: string | null;
  arrivalAtUtc: string;
  startAtUtc: string;
  finishAtUtc: string;
  travelMinutes: number | null;
  waitingMinutes: number | null;
  serviceMinutes: number | null;
}

function parseInstant(value: string): number | null {
  if (!/(?:Z|[+-]\d{2}:\d{2})$/i.test(value)) {
    return null;
  }

  const timestamp = Date.parse(value);
  return Number.isFinite(timestamp) ? timestamp : null;
}

export function elapsedMinutes(startAtUtc: string, finishedAtUtc: string | null): number | null {
  if (!finishedAtUtc) {
    return null;
  }

  const start = parseInstant(startAtUtc);
  const finish = parseInstant(finishedAtUtc);

  if (start === null || finish === null || finish < start) {
    return null;
  }

  return Math.ceil((finish - start) / 60_000);
}

export function wholeMinutes(seconds: number | string): number | null {
  const value = Number(seconds);
  return Number.isFinite(value) && value >= 0 ? Math.ceil(value / 60) : null;
}

export function isValidTimeZone(timeZone: string): boolean {
  try {
    new Intl.DateTimeFormat('en', { timeZone }).format(new Date(0));
    return true;
  } catch {
    return false;
  }
}

export function formatItineraryTime(
  utcValue: string,
  language: 'es' | 'en',
  timeZone: string,
): string {
  const instant = parseInstant(utcValue);

  if (instant === null) {
    return '—';
  }

  try {
    return new Intl.DateTimeFormat(language === 'es' ? 'es-ES' : 'en-GB', {
      timeZone,
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
      hourCycle: 'h23',
      timeZoneName: 'shortOffset',
    }).format(new Date(instant));
  } catch {
    return '—';
  }
}

export function buildItineraryStops(
  schedule: PlanScheduleDto,
  errands: readonly ErrandDto[],
  locations: readonly LocationDto[],
): ItineraryStop[] {
  const errandsById = new Map(errands.map((errand) => [errand.id, errand]));
  const locationsById = new Map(locations.map((location) => [location.id, location]));

  return schedule.scheduledErrands.map((item) => ({
    id: item.errandId,
    title: errandsById.get(item.errandId)?.title ?? null,
    locationLabel: locationsById.get(item.locationId)?.label ?? null,
    arrivalAtUtc: item.arrivalAtUtc,
    startAtUtc: item.startAtUtc,
    finishAtUtc: item.finishAtUtc,
    travelMinutes: wholeMinutes(item.travelDurationSeconds),
    waitingMinutes: wholeMinutes(item.waitingDurationSeconds),
    serviceMinutes: elapsedMinutes(item.startAtUtc, item.finishAtUtc),
  }));
}
