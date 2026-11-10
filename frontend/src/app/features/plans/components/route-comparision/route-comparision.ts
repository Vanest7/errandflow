import { Component, computed, inject, input } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import type {
  ErrandDto,
  PlanOptimizationDto,
  PlanScheduleDto,
} from '../../../../core/api/generated';

import { LanguageService } from '../../../../i18n/language.service';

@Component({
  selector: 'app-route-comparison',
  imports: [TranslocoDirective],
  templateUrl: './route-comparision.html',
})
export class RouteComparison {
  private readonly language = inject(LanguageService);

  readonly planning = input.required<PlanOptimizationDto>();
  readonly errands = input.required<readonly ErrandDto[]>();
  readonly timeZone = input.required<string>();

  protected readonly originalErrands = computed(() =>
    [...this.errands()].sort((a, b) => Number(a.originalPosition) - Number(b.originalPosition)),
  );

  protected readonly optimizedErrands = computed(() => {
    const schedule = this.planning().optimized;

    if (!schedule?.isFeasible) {
      return null;
    }

    const errands = this.errands();

    if (schedule.scheduledErrands.length !== errands.length) {
      return null;
    }

    const errandsById = new Map(errands.map((errand) => [errand.id, errand]));

    const orderedIds = schedule.scheduledErrands.map((scheduledErrand) => scheduledErrand.errandId);

    if (
      new Set(orderedIds).size !== errands.length ||
      orderedIds.some((id) => !errandsById.has(id))
    ) {
      return null;
    }

    return orderedIds.map((id) => errandsById.get(id)!);
  });

  protected readonly originalDuration = computed(() =>
    this.getScheduleDuration(this.planning().original),
  );

  protected readonly optimizedDuration = computed(() => {
    const schedule = this.planning().optimized;

    return schedule ? this.getScheduleDuration(schedule) : null;
  });

  protected readonly savedTime = computed(() => {
    const result = this.planning();

    if (
      !result.original.isFeasible ||
      !result.optimized?.isFeasible ||
      result.timeSavedSeconds === null
    ) {
      return null;
    }

    const seconds = Number(result.timeSavedSeconds);

    if (!Number.isFinite(seconds) || seconds < 0) {
      return null;
    }

    return Math.round(seconds);
  });

  protected formatDuration(seconds: number | null): string {
    if (seconds === null || !Number.isFinite(seconds)) {
      return '—';
    }

    const totalSeconds = Math.round(seconds);

    const hours = Math.floor(totalSeconds / 3600);
    const minutes = Math.floor((totalSeconds % 3600) / 60);
    const remainingSeconds = totalSeconds % 60;

    const parts: string[] = [];

    if (hours > 0) {
      parts.push(`${hours} h`);
    }

    if (minutes > 0) {
      parts.push(`${minutes} min`);
    }

    if (remainingSeconds > 0 || parts.length === 0) {
      parts.push(`${remainingSeconds} s`);
    }

    return parts.join(' ');
  }

  protected formatDateTime(value: string | null): string {
    if (!value) {
      return '—';
    }

    const date = new Date(value);

    if (!Number.isFinite(date.getTime())) {
      return '—';
    }

    const locale = this.language.activeLanguage() === 'es' ? 'es-ES' : 'en-GB';

    try {
      return new Intl.DateTimeFormat(locale, {
        timeZone: this.timeZone(),
        day: '2-digit',
        month: 'short',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false,
      }).format(date);
    } catch {
      return '—';
    }
  }

  private getScheduleDuration(schedule: PlanScheduleDto): number | null {
    if (!schedule.isFeasible || !schedule.finishedAtUtc) {
      return null;
    }

    const start = Date.parse(schedule.departureAtUtc);
    const finish = Date.parse(schedule.finishedAtUtc);

    if (!Number.isFinite(start) || !Number.isFinite(finish) || finish < start) {
      return null;
    }

    return (finish - start) / 1000;
  }
}
