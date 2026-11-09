import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  computed,
  DestroyRef,
  ElementRef,
  inject,
  signal,
  ViewChild,
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { TranslocoDirective } from '@jsverse/transloco';
import {
  catchError,
  combineLatest,
  distinctUntilChanged,
  finalize,
  forkJoin,
  map,
  of,
  startWith,
  Subject,
  switchMap,
} from 'rxjs';

import type { ErrandDto, LocationDto, PlanDto } from '../../../../core/api/generated';

import { LanguageService } from '../../../../i18n/language.service';
import type { SupportedLanguage } from '../../../../i18n/language.service';

import { ErrandForm } from '../../components/errand-form/errand-form';
import type { NewErrandValue } from '../../components/errand-form/errand-form';

import { ErrandsApiService } from '../../data-access/errands-api.service';
import { PlansApiService } from '../../data-access/plans-api.service';
import { formatPlanDate } from '../../utils/format-plan-date';
import {LocationsApiService} from '../../data-access/location-api.service';
import {RouteMap} from '../../../../shared/map/components/route-map/route-map';
import {MapStop} from '../../../../shared/map/models/map-stop.model';
import {MapCoordinates} from "../../../../shared/map/models/map-coordinates";

@Component({
  selector: 'app-plan-editor',
  imports: [RouterLink, TranslocoDirective, ErrandForm, RouteMap],
  templateUrl: './plan-editor.html',
})
export class PlanEditor {
  @ViewChild('mapPanel')
  private mapPanel?: ElementRef<HTMLElement>;

  @ViewChild('formPanel')
  private formPanel?: ElementRef<HTMLElement>;

  protected readonly pickingOnMap = signal(false);

  protected readonly mapPoint = signal<MapCoordinates | null>(null);

  protected readonly areaCenter = signal<MapCoordinates | null>(null);
  private readonly route = inject(ActivatedRoute);
  private readonly plansApi = inject(PlansApiService);
  private readonly errandsApi = inject(ErrandsApiService);
  private readonly locationsApi = inject(LocationsApiService);
  private readonly destroyRef = inject(DestroyRef);

  private readonly refresh$ = new Subject<void>();

  protected readonly language = inject(LanguageService);

  protected readonly plan = signal<PlanDto | null>(null);
  protected readonly errands = signal<ErrandDto[]>([]);
  protected readonly locations = signal<LocationDto[]>([]);

  protected readonly locationById = computed(
    () => new Map(this.locations().map((location) => [location.id, location])),
  );

  protected readonly mapStops = computed<MapStop[]>(() => {
    const locations = this.locationById();

    return this.errands().flatMap((errand, index) => {
      const location = locations.get(errand.locationId);

      if (!location) {
        return [];
      }

      return [
        {
          id: errand.id,
          title: errand.title,
          locationLabel: location.label,
          latitude: Number(location.latitude),
          longitude: Number(location.longitude),
          position: index + 1,
        },
      ];
    });
  });

  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly notFound = signal(false);

  protected readonly creatingErrand = signal(false);
  protected readonly savingErrand = signal(false);

  protected readonly createError = signal<'conflict' | 'generic' | null>(null);

  constructor() {
    const planId$ = this.route.paramMap.pipe(
      map((params) => params.get('planId')),
      distinctUntilChanged(),
    );

    combineLatest([planId$, this.refresh$.pipe(startWith(undefined))])
      .pipe(
        switchMap(([planId]) => {
          this.loading.set(true);
          this.loadError.set(false);
          this.notFound.set(false);

          this.plan.set(null);
          this.errands.set([]);
          this.locations.set([]);

          if (!planId) {
            this.notFound.set(true);
            return of(null);
          }

          return forkJoin({
            plan: this.plansApi.getById(planId),
            errands: this.errandsApi.list(planId),
            locations: this.locationsApi.list(planId),
          }).pipe(
            catchError((error: unknown) => {
              this.notFound.set(error instanceof HttpErrorResponse && error.status === 404);

              this.loadError.set(true);

              return of(null);
            }),
          );
        }),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe((result) => {
        this.plan.set(result?.plan ?? null);
        this.errands.set(result?.errands ?? []);
        this.locations.set(result?.locations ?? []);

        this.loading.set(false);
      });
  }

  protected retry(): void {
    this.refresh$.next();
  }

  protected startAddingErrand(): void {
    this.createError.set(null);
    this.mapPoint.set(null);
    this.pickingOnMap.set(false);
    this.creatingErrand.set(true);
  }

  protected cancelAddingErrand(): void {
    this.creatingErrand.set(false);
    this.createError.set(null);
    this.mapPoint.set(null);
    this.pickingOnMap.set(false);
  }

  protected saveErrand(value: NewErrandValue): void {
    const currentPlan = this.plan();

    if (!currentPlan || this.savingErrand()) {
      return;
    }

    this.createError.set(null);
    this.savingErrand.set(true);

    this.errandsApi
      .create(currentPlan.id, {
        title: value.title,
        durationMinutes: value.durationMinutes,
        expectedRevision: currentPlan.revision,
        newLocation: value.newLocation,
      })
      .pipe(
        finalize(() => this.savingErrand.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.creatingErrand.set(false);
          this.pickingOnMap.set(false);
          this.mapPoint.set(null);
          this.refresh$.next();
        },

        error: (error: unknown) => {
          this.createError.set(
            error instanceof HttpErrorResponse && error.status === 409 ? 'conflict' : 'generic',
          );
        },
      });
  }

  protected displayDate(value: string): string {
    return formatPlanDate(value, this.language.activeLanguage() as SupportedLanguage);
  }
  protected beginMapPick(): void {
    this.pickingOnMap.set(true);

    requestAnimationFrame(() => {
      this.mapPanel?.nativeElement.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    });
  }

  protected stopMapPick(): void {
    this.pickingOnMap.set(false);
  }

  protected onAreaSelected(point: MapCoordinates): void {
    this.areaCenter.set(point);
  }

  protected onMapPointPicked(point: MapCoordinates): void {
    this.mapPoint.set(point);
    this.pickingOnMap.set(false);

    requestAnimationFrame(() => {
      this.formPanel?.nativeElement.scrollIntoView({
        behavior: 'smooth',
        block: 'start',
      });
    });
  }
}
