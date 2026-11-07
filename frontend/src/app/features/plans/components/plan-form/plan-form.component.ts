import { Component, computed, effect, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { TranslocoPipe } from '@jsverse/transloco';
import { FormInputComponent } from '../../../../shared/ui/form-fields/form-input.component';
import { trimmedRequiredValidator } from '../../../../shared/validators/trimmed-required.validator';

export interface PlanFormValue {
  name: string;
  planDate: string;
  departureTime: string;
  timeZone: string;
}

@Component({
  selector: 'app-plan-form',
  imports: [ReactiveFormsModule, MatButtonModule, TranslocoPipe, FormInputComponent],
  templateUrl: './plan-form.component.html',
})
export class PlanFormComponent {
  readonly initialValue = input<PlanFormValue | null>(null);
  readonly saving = input(false);
  readonly defaultTimeZone = input('Europe/Madrid');

  readonly submitted = output<PlanFormValue>();
  readonly cancelled = output<void>();

  readonly effectiveTimeZone = computed(
    () => this.initialValue()?.timeZone ?? this.defaultTimeZone(),
  );

  readonly form = new FormGroup({
    name: new FormControl('', {
      nonNullable: true,
      validators: [trimmedRequiredValidator, Validators.maxLength(150)],
    }),
    planDate: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    departureTime: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  constructor() {
    effect(() => {
      const initial = this.initialValue();
      if (!initial) {
        return;
      }

      this.form.reset({
        name: initial.name,
        planDate: initial.planDate,
        departureTime: initial.departureTime.slice(0, 5),
      });
    });
  }

  submit(): void {
    if (this.saving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const value = this.form.getRawValue();
    this.submitted.emit({
      name: value.name.trim(),
      planDate: value.planDate,
      departureTime: `${value.departureTime}:00`,
      timeZone: this.effectiveTimeZone(),
    });
  }
}
