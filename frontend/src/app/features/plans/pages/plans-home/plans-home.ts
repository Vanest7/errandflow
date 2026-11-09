import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { Router, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import { finalize } from 'rxjs';
import type { PlanSummaryDto } from '../../../../core/api/generated';
import { LanguageService } from '../../../../i18n/language.service';
import type { SupportedLanguage } from '../../../../i18n/language.service';
import { PlanFormComponent } from '../../components/plan-form/plan-form.component';
import type { PlanFormValue } from '../../components/plan-form/plan-form.component';
import { PlansApiService } from '../../data-access/plans-api.service';
import { formatPlanDate } from '../../utils/format-plan-date';

@Component({
  selector: 'app-plans-home',
  imports: [TranslocoDirective, RouterLink, PlanFormComponent],
  templateUrl: './plans-home.html',
})
export class PlansHome {
  private readonly plansApi = inject(PlansApiService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  protected readonly language = inject(LanguageService);

  protected readonly plans = signal<PlanSummaryDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadingError = signal(false);
  protected readonly creating = signal(false);
  protected readonly saving = signal(false);
  protected readonly savingError = signal(false);

  constructor() {
    this.loadPlans();
  }

  protected loadPlans(): void {
    this.loading.set(true);
    this.loadingError.set(false);

    this.plansApi
      .list()
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (plans) => this.plans.set(plans),
        error: () => this.loadingError.set(true),
      });
  }

  protected openCreation(): void {
    this.savingError.set(false);
    this.creating.set(true);
  }

  protected closeCreation(): void {
    this.creating.set(false);
    this.savingError.set(false);
  }

  protected createPlan(value: PlanFormValue): void {
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    this.savingError.set(false);

    this.plansApi
      .create(value)
      .pipe(
        finalize(() => this.saving.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (plan) => void this.router.navigate(['/plans', plan.id]),
        error: () => this.savingError.set(true),
      });
  }

  protected displayDate(value: string): string {
    return formatPlanDate(value, this.language.activeLanguage() as SupportedLanguage);
  }
}
