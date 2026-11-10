import { Component, computed, inject, input } from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import type { ErrandDto, LocationDto, PlanScheduleDto } from '../../../../core/api/generated';
import { LanguageService, type SupportedLanguage } from '../../../../i18n/language.service';
import {
  buildItineraryStops,
  elapsedMinutes,
  formatItineraryTime,
  isValidTimeZone,
} from '../../utils/itinerary';

@Component({
  selector: 'app-plan-itinerary',
  imports: [TranslocoDirective],
  templateUrl: './plan-itinerary.html',
})
export class PlanItinerary {
  private readonly language = inject(LanguageService);

  readonly schedule = input.required<PlanScheduleDto>();
  readonly errands = input.required<readonly ErrandDto[]>();
  readonly locations = input.required<readonly LocationDto[]>();
  readonly originLocationId = input<string | null>(null);
  readonly destinationLocationId = input<string | null>(null);
  readonly timeZone = input.required<string>();

  protected readonly timeZoneValid = computed(() => isValidTimeZone(this.timeZone()));

  protected readonly stops = computed(() =>
    buildItineraryStops(this.schedule(), this.errands(), this.locations()),
  );

  protected readonly locationById = computed(
    () => new Map(this.locations().map((location) => [location.id, location])),
  );

  protected readonly originLabel = computed(
    () => this.locationById().get(this.originLocationId() ?? '')?.label ?? null,
  );

  protected readonly destinationLabel = computed(
    () => this.locationById().get(this.destinationLocationId() ?? '')?.label ?? null,
  );

  protected readonly totalMinutes = computed(() => {
    const schedule = this.schedule();

    return schedule.isFeasible
      ? elapsedMinutes(schedule.departureAtUtc, schedule.finishedAtUtc)
      : null;
  });

  protected formatTime(utcValue: string): string {
    return formatItineraryTime(
      utcValue,
      this.language.activeLanguage() as SupportedLanguage,
      this.timeZone(),
    );
  }

  protected formatMinutes(minutes: number | null): string {
    if (minutes === null) {
      return '—';
    }

    const locale = this.language.activeLanguage() === 'es' ? 'es-ES' : 'en-GB';
    const formatter = new Intl.NumberFormat(locale);
    const hours = Math.floor(minutes / 60);
    const remainingMinutes = minutes % 60;

    if (hours === 0) {
      return `${formatter.format(minutes)} min`;
    }

    return remainingMinutes === 0
      ? `${formatter.format(hours)} h`
      : `${formatter.format(hours)} h ${formatter.format(remainingMinutes)} min`;
  }
}
