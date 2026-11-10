import { Component, effect, input, output, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';

import type { ErrandDto } from '../../../../core/api/generated';
import {
  buildSchedulePayload,
  getScheduleMode,
  utcToLocalInput,
  type SchedulePayload,
  type ScheduleMode,
  type ScheduleValidationError,
} from '../../utils/errand-schedule-time';

@Component({
  selector: 'app-errand-schedule-form',
  imports: [ReactiveFormsModule, TranslocoDirective],
  templateUrl: './errand-schedule-form.html',
})
export class ErrandScheduleForm {
  readonly errand = input.required<ErrandDto>();
  readonly timeZone = input.required<string>();
  readonly planDate = input.required<string>();
  readonly saving = input(false);

  readonly submitted = output<SchedulePayload>();
  readonly cancelled = output<void>();

  protected readonly error = signal<ScheduleValidationError | null>(null);
  protected readonly form = new FormGroup({
    mode: new FormControl<ScheduleMode>('anytime', { nonNullable: true }),
    earliestLocal: new FormControl('', { nonNullable: true }),
    latestLocal: new FormControl('', { nonNullable: true }),
    fixedLocal: new FormControl('', { nonNullable: true }),
  });

  constructor() {
    effect(() => {
      const errand = this.errand();
      const timeZone = this.timeZone();
      const planDate = this.planDate();
      const validPlanDate = /^\d{4}-\d{2}-\d{2}$/.test(planDate);
      const initialStart = validPlanDate ? `${planDate}T09:00` : '';
      const initialFinish = validPlanDate ? `${planDate}T10:00` : '';
      const hasWindow = !!errand.earliestStartAt || !!errand.latestFinishAt;

      this.form.setValue({
        mode: getScheduleMode(errand),
        earliestLocal: utcToLocalInput(errand.earliestStartAt, timeZone) || (hasWindow ? '' : initialStart),
        latestLocal: utcToLocalInput(errand.latestFinishAt, timeZone) || (hasWindow ? '' : initialFinish),
        fixedLocal: utcToLocalInput(errand.fixedStartAt, timeZone) || initialStart,
      }, { emitEvent: false });
      this.error.set(null);
    });

    this.form.valueChanges.subscribe(() => this.error.set(null));
  }

  protected submit(): void {
    if (this.saving()) {
      return;
    }

    const values = this.form.getRawValue();
    const result = buildSchedulePayload({
      ...values,
      durationMinutes: Number(this.errand().durationMinutes),
      timeZone: this.timeZone(),
    });

    if (!result.ok) {
      this.error.set(result.error);
      return;
    }

    this.error.set(null);
    this.submitted.emit(result.payload);
  }
}
