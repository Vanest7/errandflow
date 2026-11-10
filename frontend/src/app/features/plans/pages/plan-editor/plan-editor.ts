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
  Observable,
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

import {
  PlanEndpointPicker,
  type EndpointKind,
  type EndpointSelection,
} from '../../components/plan-endpoint-picker/plan-endpoint-picker';

import { ErrandsApiService } from '../../data-access/errands-api.service';
import { LocationsApiService } from '../../data-access/location-api.service';
import { PlansApiService } from '../../data-access/plans-api.service';

import { formatPlanDate } from '../../utils/format-plan-date';

import { RouteMap } from '../../../../shared/map/components/route-map/route-map';
import type { MapCoordinates } from '../../../../shared/map/models/map-coordinates';
import type { MapStop } from '../../../../shared/map/models/map-stop.model';

type MapPickTarget = 'errand' | EndpointKind;
type EndpointError = 'conflict' | 'generic' | 'notAssigned';

@Component({
  selector: 'app-plan-editor',
  imports: [RouterLink, TranslocoDirective, ErrandForm, PlanEndpointPicker, RouteMap],
  templateUrl: './plan-editor.html',
})
export class PlanEditor {
  @ViewChild('mapPanel')
  private mapPanel?: ElementRef<HTMLElement>;

  @ViewChild('formPanel')
  private formPanel?: ElementRef<HTMLElement>;

  @ViewChild('endpointPanel')
  private endpointPanel?: ElementRef<HTMLElement>;

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

  protected readonly loading = signal(true);
  protected readonly loadError = signal(false);
  protected readonly notFound = signal(false);

  protected readonly creatingErrand = signal(false);
  protected readonly savingErrand = signal(false);
  protected readonly createError = signal<'conflict' | 'generic' | null>(null);

  protected readonly editingEndpoint = signal<EndpointKind | null>(null);
  protected readonly savingEndpoint = signal(false);
  protected readonly endpointError = signal<EndpointError | null>(null);

  private readonly stagedEndpointLocationId = signal<string | null>(null);

  protected readonly mapPickTarget = signal<MapPickTarget | null>(null);
  protected readonly pickingOnMap = computed(() => this.mapPickTarget() !== null);

  protected readonly mapPoint = signal<MapCoordinates | null>(null);
  protected readonly endpointMapPoint = signal<MapCoordinates | null>(null);
  protected readonly areaCenter = signal<MapCoordinates | null>(null);

  protected readonly locationById = computed(
    () => new Map(this.locations().map((location) => [location.id, location])),
  );

  protected readonly mapStops = computed<MapStop[]>(() => {
    const currentPlan = this.plan();
    const locations = this.locationById();

    const stops: MapStop[] = this.errands().flatMap((errand, index) => {
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
          kind: 'errand' as const,
        },
      ];
    });

    if (currentPlan?.originLocationId) {
      const location = locations.get(currentPlan.originLocationId);

      if (location) {
        stops.unshift({
          id: `origin-${location.id}`,
          title: location.label,
          locationLabel: location.label,
          latitude: Number(location.latitude),
          longitude: Number(location.longitude),
          position: 0,
          kind: 'origin',
        });
      }
    }

    if (currentPlan?.destinationLocationId) {
      const location = locations.get(currentPlan.destinationLocationId);

      if (location) {
        stops.push({
          id: `destination-${location.id}`,
          title: location.label,
          locationLabel: location.label,
          latitude: Number(location.latitude),
          longitude: Number(location.longitude),
          position: stops.length,
          kind: 'destination',
        });
      }
    }

    return stops;
  });

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

          this.resetEndpointEditor();

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
      .subscribe({
        next: (result) => {
          this.plan.set(result?.plan ?? null);
          this.errands.set(result?.errands ?? []);
          this.locations.set(result?.locations ?? []);

          this.loading.set(false);
        },
        error: (error: unknown) => {
          this.notFound.set(error instanceof HttpErrorResponse && error.status === 404);

          this.loadError.set(true);
          this.loading.set(false);
        },
      });
  }

  protected retry(): void {
    this.resetEndpointEditor();
    this.refresh$.next();
  }

  protected displayDate(value: string): string {
    return formatPlanDate(value, this.language.activeLanguage() as SupportedLanguage);
  }

  protected startAddingErrand(): void {
    if (this.savingEndpoint()) {
      return;
    }

    this.resetEndpointEditor();

    this.createError.set(null);
    this.mapPoint.set(null);
    this.mapPickTarget.set(null);
    this.creatingErrand.set(true);
  }

  protected cancelAddingErrand(): void {
    this.creatingErrand.set(false);
    this.createError.set(null);
    this.mapPoint.set(null);
    this.mapPickTarget.set(null);
  }

  protected saveErrand(value: NewErrandValue): void {
    const currentPlan = this.plan();

    if (!currentPlan || this.savingErrand() || this.savingEndpoint()) {
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
          this.mapPickTarget.set(null);
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

  protected beginEndpointEdit(kind: EndpointKind): void {
    if (this.creatingErrand() || this.savingErrand() || this.savingEndpoint()) {
      return;
    }

    this.resetEndpointEditor();

    this.editingEndpoint.set(kind);
  }

  protected cancelEndpointEdit(): void {
    if (this.savingEndpoint()) {
      return;
    }

    this.resetEndpointEditor();
  }

  protected saveEndpoint(selection: EndpointSelection): void {
    const currentPlan = this.plan();
    const target = this.editingEndpoint();

    if (!currentPlan || !target || this.savingEndpoint()) {
      return;
    }

    this.endpointError.set(null);
    this.savingEndpoint.set(true);
    this.mapPickTarget.set(null);

    const stagedLocationId = this.stagedEndpointLocationId();

    let request$: Observable<PlanDto>;

    if (stagedLocationId) {
      request$ = this.assignEndpoint(currentPlan, target, stagedLocationId, currentPlan.revision);
    } else if (selection.type === 'existing') {
      request$ = this.assignEndpoint(
        currentPlan,
        target,
        selection.locationId,
        currentPlan.revision,
      );
    } else {
      request$ = this.locationsApi
        .create(currentPlan.id, {
          ...selection.location,
          expectedRevision: currentPlan.revision,
        })
        .pipe(
          switchMap((result) => {
            this.stagedEndpointLocationId.set(result.location.id);

            this.locations.update((locations) => [...locations, result.location]);

            this.plan.update((plan) => (plan ? { ...plan, revision: result.planRevision } : plan));

            return this.assignEndpoint(
              currentPlan,
              target,
              result.location.id,
              result.planRevision,
            );
          }),
        );
    }

    request$
      .pipe(
        finalize(() => this.savingEndpoint.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (updatedPlan) => {
          this.plan.set(updatedPlan);
          this.resetEndpointEditor();
        },
        error: (error: unknown) => {
          if (error instanceof HttpErrorResponse && error.status === 409) {
            this.endpointError.set('conflict');
            return;
          }

          this.endpointError.set(this.stagedEndpointLocationId() ? 'notAssigned' : 'generic');
        },
      });
  }

  private assignEndpoint(
    currentPlan: PlanDto,
    target: EndpointKind,
    locationId: string | null,
    revision: number | string,
  ): Observable<PlanDto> {
    return this.plansApi.setLocations(currentPlan.id, {
      expectedRevision: revision,
      originLocationId: target === 'origin' ? locationId : currentPlan.originLocationId,
      destinationLocationId:
        target === 'destination' ? locationId : currentPlan.destinationLocationId,
    });
  }

  private resetEndpointEditor(): void {
    this.editingEndpoint.set(null);
    this.endpointError.set(null);
    this.endpointMapPoint.set(null);
    this.stagedEndpointLocationId.set(null);

    if (this.mapPickTarget() !== 'errand') {
      this.mapPickTarget.set(null);
    }
  }

  protected beginMapPick(): void {
    this.mapPickTarget.set('errand');

    this.scrollToMap();
  }

  protected stopMapPick(): void {
    if (this.mapPickTarget() === 'errand') {
      this.mapPickTarget.set(null);
    }
  }

  protected beginEndpointMapPick(): void {
    const target = this.editingEndpoint();

    if (!target || this.savingEndpoint()) {
      return;
    }

    this.endpointMapPoint.set(null);
    this.mapPickTarget.set(target);

    this.scrollToMap();
  }

  protected onEndpointPreview(point: MapCoordinates | null): void {
    this.endpointMapPoint.set(point);

    if (point) {
      this.areaCenter.set(point);
    }

    if (this.mapPickTarget() !== 'errand') {
      this.mapPickTarget.set(null);
    }
  }

  protected onAreaSelected(point: MapCoordinates): void {
    this.areaCenter.set(point);
  }

  protected onMapPointPicked(point: MapCoordinates): void {
    const target = this.mapPickTarget();

    if (!target) {
      return;
    }

    this.mapPickTarget.set(null);

    if (target === 'errand') {
      this.mapPoint.set(point);
    } else {
      this.endpointMapPoint.set(point);
    }

    requestAnimationFrame(() => {
      const panel = target === 'errand' ? this.formPanel : this.endpointPanel;
      panel?.nativeElement.scrollIntoView({
        behavior: 'smooth',
        block: 'start',
      });
    });
  }

  private scrollToMap(): void {
    requestAnimationFrame(() => {
      this.mapPanel?.nativeElement.scrollIntoView({
        behavior: 'smooth',
        block: 'center',
      });
    });
  }
}
