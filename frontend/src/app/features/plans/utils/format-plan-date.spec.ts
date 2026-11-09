import { formatPlanDate } from './format-plan-date';

describe('formatPlanDate', () => {
  it('formats a calendar date without shifting the day', () => {
    expect(formatPlanDate('2026-11-14', 'es')).toContain('14');
    expect(formatPlanDate('2026-11-14', 'en')).toContain('14');
  });

  it('preserves unexpected input', () => {
    expect(formatPlanDate('invalid', 'es')).toBe('invalid');
  });
});
