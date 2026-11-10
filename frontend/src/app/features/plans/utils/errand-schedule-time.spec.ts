import {
  buildSchedulePayload,
  formatScheduleDateTime,
  resolveLocalDateTime,
  utcToLocalInput,
} from './errand-schedule-time';

describe('resolveLocalDateTime', () => {
  it('converts normal local times using the plan time zone', () => {
    expect(resolveLocalDateTime('2026-11-10T09:30', 'Europe/Madrid')).toEqual({
      ok: true,
      utc: '2026-11-10T08:30:00.000Z',
    });
  });

  it('respects summer daylight-saving time', () => {
    expect(resolveLocalDateTime('2026-07-10T09:30', 'Europe/Madrid')).toEqual({
      ok: true,
      utc: '2026-07-10T07:30:00.000Z',
    });
  });

  it('rejects a nonexistent spring DST time', () => {
    expect(resolveLocalDateTime('2026-03-29T02:30', 'Europe/Madrid')).toEqual({
      ok: false,
      error: 'nonexistentTime',
    });
  });

  it('rejects an ambiguous autumn DST time', () => {
    expect(resolveLocalDateTime('2026-10-25T02:30', 'Europe/Madrid')).toEqual({
      ok: false,
      error: 'ambiguousTime',
    });
  });

  it('rejects invalid calendar dates and zones', () => {
    expect(resolveLocalDateTime('2026-02-30T10:00', 'Europe/Madrid')).toEqual({
      ok: false,
      error: 'invalidDateTime',
    });
    expect(resolveLocalDateTime('2026-11-10T10:00', 'Invalid/Time_Zone')).toEqual({
      ok: false,
      error: 'invalidTimeZone',
    });
  });

  it('does not use the browser time zone when formatting a UTC instant', () => {
    expect(utcToLocalInput('2026-11-10T08:30:00Z', 'Europe/Madrid')).toBe(
      '2026-11-10T09:30',
    );
    expect(formatScheduleDateTime('2026-11-10T08:30:00Z', 'Europe/Madrid', 'es'))
      .toContain('9:30');
  });
});

describe('buildSchedulePayload', () => {
  const base = {
    mode: 'window' as const,
    earliestLocal: '2026-11-10T09:00',
    latestLocal: '2026-11-10T09:30',
    fixedLocal: '',
    durationMinutes: 20,
    timeZone: 'Europe/Madrid',
  };

  it('converts an allowed window to UTC', () => {
    expect(buildSchedulePayload(base)).toEqual({
      ok: true,
      payload: {
        earliestStartAt: '2026-11-10T08:00:00.000Z',
        latestFinishAt: '2026-11-10T08:30:00.000Z',
        fixedStartAt: null,
      },
    });
  });

  it('rejects a window that cannot fit the errand duration', () => {
    expect(buildSchedulePayload({ ...base, durationMinutes: 31 })).toEqual({
      ok: false,
      error: 'insufficientDuration',
    });
  });

  it('rejects an inverted window', () => {
    expect(buildSchedulePayload({ ...base, latestLocal: '2026-11-10T08:59' })).toEqual({
      ok: false,
      error: 'invalidWindow',
    });
  });

  it('supports an overnight window', () => {
    expect(buildSchedulePayload({ ...base, latestLocal: '2026-11-11T00:10' })).toMatchObject({
      ok: true,
      payload: { latestFinishAt: '2026-11-10T23:10:00.000Z' },
    });
  });

  it('converts a fixed appointment', () => {
    expect(buildSchedulePayload({
      ...base,
      mode: 'appointment',
      fixedLocal: '2026-11-10T11:30',
    })).toEqual({
      ok: true,
      payload: {
        earliestStartAt: null,
        latestFinishAt: null,
        fixedStartAt: '2026-11-10T10:30:00.000Z',
      },
    });
  });

  it('clears all restrictions in anytime mode', () => {
    expect(buildSchedulePayload({ ...base, mode: 'anytime' })).toEqual({
      ok: true,
      payload: { earliestStartAt: null, latestFinishAt: null, fixedStartAt: null },
    });
  });
});
