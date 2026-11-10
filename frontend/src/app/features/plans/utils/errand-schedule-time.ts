import type { ErrandDto, SetErrandScheduleInput } from '../../../core/api/generated';
import type { SupportedLanguage } from '../../../i18n/language.service';

export type ScheduleMode = 'anytime' | 'window' | 'appointment';

export type ScheduleValidationError =
  | 'required'
  | 'invalidDateTime'
  | 'invalidTimeZone'
  | 'nonexistentTime'
  | 'ambiguousTime'
  | 'invalidWindow'
  | 'insufficientDuration';

export type LocalTimeResolution =
  | { ok: true; utc: string }
  | { ok: false; error: ScheduleValidationError };

export interface ScheduleFormValue {
  mode: ScheduleMode;
  earliestLocal: string;
  latestLocal: string;
  fixedLocal: string;
  durationMinutes: number;
  timeZone: string;
}

export type SchedulePayload = Omit<SetErrandScheduleInput, 'expectedRevision'>;

export type ScheduleBuildResult =
  | { ok: true; payload: SchedulePayload }
  | { ok: false; error: ScheduleValidationError };

interface DateTimeParts {
  year: number;
  month: number;
  day: number;
  hour: number;
  minute: number;
}

function parseLocalDateTime(value: string): DateTimeParts | null {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);

  if (!match) {
    return null;
  }

  const [year, month, day, hour, minute] = match.slice(1).map(Number);
  const timestamp = new Date(0);
  timestamp.setUTCFullYear(year, month - 1, day);
  timestamp.setUTCHours(hour, minute, 0, 0);

  if (
    year < 1 ||
    timestamp.getUTCFullYear() !== year ||
    timestamp.getUTCMonth() !== month - 1 ||
    timestamp.getUTCDate() !== day ||
    timestamp.getUTCHours() !== hour ||
    timestamp.getUTCMinutes() !== minute
  ) {
    return null;
  }

  return { year, month, day, hour, minute };
}

function utcEquivalent(parts: DateTimeParts): number {
  const date = new Date(0);
  date.setUTCFullYear(parts.year, parts.month - 1, parts.day);
  date.setUTCHours(parts.hour, parts.minute, 0, 0);
  return date.getTime();
}

function zonedParts(formatter: Intl.DateTimeFormat, instant: number): DateTimeParts {
  const values = Object.fromEntries(
    formatter.formatToParts(new Date(instant))
      .filter((part) => part.type !== 'literal')
      .map((part) => [part.type, Number(part.value)]),
  );

  return {
    year: values['year'],
    month: values['month'],
    day: values['day'],
    hour: values['hour'],
    minute: values['minute'],
  };
}

function sameParts(a: DateTimeParts, b: DateTimeParts): boolean {
  return a.year === b.year &&
    a.month === b.month &&
    a.day === b.day &&
    a.hour === b.hour &&
    a.minute === b.minute;
}

export function resolveLocalDateTime(value: string, timeZone: string): LocalTimeResolution {
  const desired = parseLocalDateTime(value);

  if (!desired) {
    return { ok: false, error: 'invalidDateTime' };
  }

  let formatter: Intl.DateTimeFormat;
  try {
    formatter = new Intl.DateTimeFormat('en-GB', {
      timeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hourCycle: 'h23',
    });
  } catch (error) {
    if (error instanceof RangeError) {
      return { ok: false, error: 'invalidTimeZone' };
    }
    throw error;
  }

  const nominalTime = utcEquivalent(desired);
  const hour = 60 * 60 * 1000;
  const offsets = new Set<number>();

  // Collect offsets around the chosen date to detect DST gaps and overlaps.
  for (let delta = -36; delta <= 36; delta++) {
    const candidate = nominalTime + delta * hour;
    offsets.add(utcEquivalent(zonedParts(formatter, candidate)) - candidate);
  }

  const matches = [...offsets]
    .map((offset) => nominalTime - offset)
    .filter((candidate) => sameParts(zonedParts(formatter, candidate), desired));

  if (matches.length === 0) {
    return { ok: false, error: 'nonexistentTime' };
  }

  if (matches.length > 1) {
    return { ok: false, error: 'ambiguousTime' };
  }

  return { ok: true, utc: new Date(matches[0]).toISOString() };
}

export function utcToLocalInput(iso: string | null, timeZone: string): string {
  if (!iso || Number.isNaN(Date.parse(iso))) {
    return '';
  }

  try {
    const formatter = new Intl.DateTimeFormat('en-GB', {
      timeZone,
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      hour: '2-digit',
      minute: '2-digit',
      hourCycle: 'h23',
    });
    const parts = zonedParts(formatter, Date.parse(iso));
    const pad = (number: number): string => String(number).padStart(2, '0');
    return `${String(parts.year).padStart(4, '0')}-${pad(parts.month)}-${pad(parts.day)}T${pad(parts.hour)}:${pad(parts.minute)}`;
  } catch (error) {
    if (error instanceof RangeError) {
      return '';
    }
    throw error;
  }
}

export function formatScheduleDateTime(
  iso: string,
  timeZone: string,
  language: SupportedLanguage,
): string {
  const timestamp = Date.parse(iso);

  if (Number.isNaN(timestamp)) {
    return iso;
  }

  try {
    return new Intl.DateTimeFormat(language === 'es' ? 'es-ES' : 'en-GB', {
      timeZone,
      dateStyle: 'short',
      timeStyle: 'short',
    }).format(new Date(timestamp));
  } catch (error) {
    if (error instanceof RangeError) {
      return iso;
    }
    throw error;
  }
}

export function getScheduleMode(errand: ErrandDto): ScheduleMode {
  if (errand.fixedStartAt) {
    return 'appointment';
  }

  if (errand.earliestStartAt || errand.latestFinishAt) {
    return 'window';
  }

  return 'anytime';
}

export function buildSchedulePayload(value: ScheduleFormValue): ScheduleBuildResult {
  if (value.mode === 'anytime') {
    return {
      ok: true,
      payload: { earliestStartAt: null, latestFinishAt: null, fixedStartAt: null },
    };
  }

  if (value.mode === 'appointment') {
    if (!value.fixedLocal) {
      return { ok: false, error: 'required' };
    }

    const fixed = resolveLocalDateTime(value.fixedLocal, value.timeZone);
    return fixed.ok
      ? { ok: true, payload: { earliestStartAt: null, latestFinishAt: null, fixedStartAt: fixed.utc } }
      : { ok: false, error: fixed.error };
  }

  if (!value.earliestLocal || !value.latestLocal) {
    return { ok: false, error: 'required' };
  }

  const earliest = resolveLocalDateTime(value.earliestLocal, value.timeZone);
  if (!earliest.ok) {
    return { ok: false, error: earliest.error };
  }

  const latest = resolveLocalDateTime(value.latestLocal, value.timeZone);
  if (!latest.ok) {
    return { ok: false, error: latest.error };
  }

  const availableMinutes = (Date.parse(latest.utc) - Date.parse(earliest.utc)) / 60_000;
  if (availableMinutes <= 0) {
    return { ok: false, error: 'invalidWindow' };
  }

  if (!Number.isFinite(value.durationMinutes) || availableMinutes < value.durationMinutes) {
    return { ok: false, error: 'insufficientDuration' };
  }

  return {
    ok: true,
    payload: { earliestStartAt: earliest.utc, latestFinishAt: latest.utc, fixedStartAt: null },
  };
}
