import { HttpErrorResponse } from '@angular/common/http';
import { CdkDrag, CdkDragDrop, CdkDragHandle, CdkDropList } from '@angular/cdk/drag-drop';
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
  takeUntil,
} from 'rxjs';

import type {
  ErrandDto,
  LocationDto,
  OptimizedPlanRouteDto,
  PlanDto,
} from '../../../../core/api/generated';

import { LanguageService } from '../../../../i18n/language.service';
import type { SupportedLanguage } from '../../../../i18n/language.service';

import { ErrandForm } from '../../components/errand-form/errand-form';
import type { NewErrandValue, UpdatedErrandValue } from '../../components/errand-form/errand-form';

import {
  PlanEndpointPicker,
  type EndpointKind,
  type EndpointSelection,
} from '../../components/plan-endpoint-picker/plan-endpoint-picker';

import { ErrandScheduleForm } from '../../components/errand-schedule-form/errand-schedule-form';
import { ErrandsApiService } from '../../data-access/errands-api.service';
import { LocationsApiService } from '../../data-access/location-api.service';
import { PlansApiService } from '../../data-access/plans-api.service';

import { formatPlanDate } from '../../utils/format-plan-date';

import { RouteMap } from '../../../../shared/map/components/route-map/route-map';
import type { MapCoordinates } from '../../../../shared/map/models/map-coordinates';
import type { MapStop } from '../../../../shared/map/models/map-stop.model';
import { PlanRouteApiService, SupportedTravelMode } from '../../data-access/plan-route-api.service';
import { mapRoutePath } from '../../utils/map-route-path';
import { formatScheduleDateTime, type SchedulePayload } from '../../utils/errand-schedule-time';
import { moveErrandIdsToIndex } from '../../utils/reorder-errands';
import { RouteComparison } from '../../components/route-comparision/route-comparision';

type MapPickTarget = 'errand' | EndpointKind;
type EndpointError = 'conflict' | 'generic' | 'notAssigned';

@Component({
  selector: 'app-plan-editor',
  imports: [
    CdkDrag,
    CdkDragHandle,
    CdkDropList,
    RouterLink,
    TranslocoDirective,
    ErrandForm,
    ErrandScheduleForm,
    PlanEndpointPicker,
    RouteMap,
    RouteComparison,
  ],
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
  private readonly routeApi = inject(PlanRouteApiService);

  private readonly routeInvalidated$ = new Subject<void>();

  protected readonly travelMode = signal<SupportedTravelMode>(1);

  protected readonly calculatingRoute = signal(false);

  protected readonly calculatedRoute = signal<OptimizedPlanRouteDto | null>(null);

  protected readonly routeError = signal<
    'conflict' | 'unavailable' | 'notFound' | 'invalid' | 'generic' | null
  >(null);

  protected readonly routeDetails = computed(() =>
    mapRoutePath(this.calculatedRoute()?.path ?? null),
  );

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

  protected readonly editingErrandId = signal<string | null>(null);
  protected readonly schedulingErrandId = signal<string | null>(null);
  protected readonly savingSchedule = signal(false);
  protected readonly scheduleError = signal<'conflict' | 'notFound' | 'invalid' | 'generic' | null>(
    null,
  );

  protected readonly savingEditedErrand = signal(false);
  protected readonly confirmingDeleteId = signal<string | null>(null);
  protected readonly deletingErrandId = signal<string | null>(null);
  protected readonly reorderingErrands = signal(false);
  protected readonly errandMutationError = signal<'conflict' | 'notFound' | 'generic' | null>(null);

  protected readonly errandMutationBusy = computed(
    () =>
      this.savingErrand() ||
      this.savingEditedErrand() ||
      this.savingSchedule() ||
      this.schedulingErrandId() !== null ||
      this.deletingErrandId() !== null ||
      this.reorderingErrands(),
  );

  protected readonly originalOrderErrands = computed(() =>
    [...this.errands()].sort((a, b) => Number(a.originalPosition) - Number(b.originalPosition)),
  );

  protected readonly canReorderErrands = computed(
    () =>
      !this.errandMutationBusy() &&
      !this.creatingErrand() &&
      this.editingErrandId() === null &&
      this.confirmingDeleteId() === null &&
      this.editingEndpoint() === null &&
      !this.savingEndpoint() &&
      this.errandMutationError() !== 'conflict',
  );

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

  protected readonly orderedRouteErrands = computed<ErrandDto[]>(() => {
    const errands = this.errands();
    const result = this.calculatedRoute();

    const scheduled = result?.planning.optimized?.scheduledErrands;

    if (!result?.path || !scheduled || scheduled.length !== errands.length) {
      return errands;
    }

    const errandsById = new Map(errands.map((errand) => [errand.id, errand]));

    const ordered = scheduled.flatMap((scheduledErrand) => {
      const errand = errandsById.get(scheduledErrand.errandId);

      return errand ? [errand] : [];
    });

    return ordered.length === errands.length ? ordered : errands;
  });

  protected readonly mapStops = computed<MapStop[]>(() => {
    const currentPlan = this.plan();
    const locations = this.locationById();

    const stops: MapStop[] = this.orderedRouteErrands().flatMap((errand, index) => {
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
          this.invalidateRoute();
          this.loading.set(true);
          this.loadError.set(false);
          this.notFound.set(false);

          this.plan.set(null);
          this.errands.set([]);
          this.locations.set([]);

          this.resetEndpointEditor();
          this.editingErrandId.set(null);
          this.schedulingErrandId.set(null);
          this.scheduleError.set(null);
          this.confirmingDeleteId.set(null);
          this.errandMutationError.set(null);
          this.mapPoint.set(null);

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
    this.cancelEditingErrand();
    this.confirmingDeleteId.set(null);
    this.errandMutationError.set(null);
    this.refresh$.next();
  }

  protected displayDate(value: string): string {
    return formatPlanDate(value, this.language.activeLanguage() as SupportedLanguage);
  }

  protected startAddingErrand(): void {
    if (
      this.savingEndpoint() ||
      this.errandMutationBusy() ||
      this.editingErrandId() !== null ||
      this.confirmingDeleteId() !== null ||
      this.editingEndpoint() !== null
    ) {
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

    if (!currentPlan || this.errandMutationBusy() || this.savingEndpoint()) {
      return;
    }
    this.invalidateRoute();
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
    if (
      this.creatingErrand() ||
      this.errandMutationBusy() ||
      this.editingErrandId() !== null ||
      this.confirmingDeleteId() !== null ||
      this.savingEndpoint()
    ) {
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

    if (
      !currentPlan ||
      !target ||
      this.savingEndpoint() ||
      this.errandMutationBusy() ||
      this.editingErrandId() !== null
    ) {
      return;
    }
    this.invalidateRoute();
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

  protected startEditingErrand(errand: ErrandDto): void {
    if (
      this.errandMutationBusy() ||
      this.creatingErrand() ||
      this.editingEndpoint() !== null ||
      this.savingEndpoint() ||
      this.errandMutationError() === 'conflict'
    ) {
      return;
    }

    this.editingErrandId.set(errand.id);
    this.confirmingDeleteId.set(null);
    this.errandMutationError.set(null);
    this.mapPoint.set(null);
    this.mapPickTarget.set(null);
  }

  protected cancelEditingErrand(): void {
    if (this.savingEditedErrand()) {
      return;
    }

    this.editingErrandId.set(null);
    this.mapPoint.set(null);
    if (this.mapPickTarget() === 'errand') {
      this.mapPickTarget.set(null);
    }
  }

  protected updateErrand(value: UpdatedErrandValue): void {
    const currentPlan = this.plan();
    const errandId = this.editingErrandId();

    if (!currentPlan || !errandId || this.errandMutationBusy()) {
      return;
    }

    const location =
      value.location.type === 'new'
        ? { newLocation: value.location.newLocation }
        : { locationId: value.location.locationId };

    this.invalidateRoute();
    this.errandMutationError.set(null);
    this.savingEditedErrand.set(true);
    this.mapPickTarget.set(null);

    this.errandsApi
      .update(currentPlan.id, errandId, {
        title: value.title,
        durationMinutes: value.durationMinutes,
        expectedRevision: currentPlan.revision,
        ...location,
      })
      .pipe(
        finalize(() => this.savingEditedErrand.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.editingErrandId.set(null);
          this.mapPoint.set(null);
          this.refresh$.next();
        },
        error: (error: unknown) => this.handleErrandMutationError(error),
      });
  }

  protected askDeleteErrand(errandId: string): void {
    if (
      this.errandMutationBusy() ||
      this.editingErrandId() !== null ||
      this.creatingErrand() ||
      this.editingEndpoint() !== null ||
      this.savingEndpoint() ||
      this.errandMutationError() === 'conflict'
    ) {
      return;
    }

    this.errandMutationError.set(null);
    this.confirmingDeleteId.set(errandId);
  }

  protected cancelDeleteErrand(): void {
    if (this.deletingErrandId() !== null) {
      return;
    }
    this.confirmingDeleteId.set(null);
  }

  protected deleteErrand(errandId: string): void {
    const currentPlan = this.plan();
    if (!currentPlan || this.confirmingDeleteId() !== errandId || this.errandMutationBusy()) {
      return;
    }

    this.invalidateRoute();
    this.errandMutationError.set(null);
    this.deletingErrandId.set(errandId);

    this.errandsApi
      .delete(currentPlan.id, errandId, currentPlan.revision)
      .pipe(
        finalize(() => this.deletingErrandId.set(null)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: () => {
          this.confirmingDeleteId.set(null);
          this.refresh$.next();
        },
        error: (error: unknown) => this.handleErrandMutationError(error),
      });
  }

  protected onErrandDropped(event: CdkDragDrop<ErrandDto[]>): void {
    if (!this.canReorderErrands()) {
      return;
    }

    const errand = this.originalOrderErrands()[event.previousIndex];

    if (!errand) {
      return;
    }

    this.changeErrandPosition(errand.id, event.currentIndex);
  }

  protected onErrandPositionChanged(errandId: string, event: Event): void {
    const select = event.target as HTMLSelectElement;
    const targetIndex = Number(select.value) - 1;

    this.changeErrandPosition(errandId, targetIndex);

    const originalIndex = this.originalOrderErrands().findIndex((errand) => errand.id === errandId);
    select.value = String(originalIndex + 1);
  }

  private changeErrandPosition(errandId: string, targetIndex: number): void {
    const currentPlan = this.plan();

    if (!currentPlan || !this.canReorderErrands()) {
      return;
    }

    const errandIds = moveErrandIdsToIndex(this.originalOrderErrands(), errandId, targetIndex);

    if (!errandIds) {
      return;
    }

    this.invalidateRoute();
    this.errandMutationError.set(null);
    this.reorderingErrands.set(true);

    this.errandsApi
      .reorder(currentPlan.id, {
        expectedRevision: currentPlan.revision,
        errandIds,
      })
      .pipe(
        finalize(() => this.reorderingErrands.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.errands.set(result.errands);
          this.plan.update((plan) => (plan ? { ...plan, revision: result.planRevision } : plan));
        },
        error: (error: unknown) => this.handleErrandMutationError(error),
      });
  }

  protected openErrandSchedule(errandId: string): void {
    if (
      this.errandMutationBusy() ||
      this.creatingErrand() ||
      this.editingEndpoint() !== null ||
      this.editingErrandId() !== null ||
      this.confirmingDeleteId() !== null ||
      this.savingEndpoint() ||
      this.errandMutationError() === 'conflict'
    ) {
      return;
    }

    this.schedulingErrandId.set(errandId);
    this.scheduleError.set(null);
  }

  protected cancelErrandSchedule(): void {
    if (this.savingSchedule()) {
      return;
    }

    this.schedulingErrandId.set(null);
    this.scheduleError.set(null);
  }

  protected saveErrandSchedule(value: SchedulePayload): void {
    const currentPlan = this.plan();
    const errandId = this.schedulingErrandId();

    if (!currentPlan || !errandId || this.savingSchedule()) {
      return;
    }

    this.savingSchedule.set(true);
    this.scheduleError.set(null);

    this.errandsApi
      .setSchedule(currentPlan.id, errandId, {
        ...value,
        expectedRevision: currentPlan.revision,
      })
      .pipe(
        finalize(() => this.savingSchedule.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (result) => {
          this.invalidateRoute();
          this.errands.update((errands) =>
            errands.map((errand) => (errand.id === errandId ? result.errand : errand)),
          );
          this.plan.update((plan) => (plan ? { ...plan, revision: result.planRevision } : plan));
          this.schedulingErrandId.set(null);
        },
        error: (error: unknown) => {
          const status = error instanceof HttpErrorResponse ? error.status : 0;
          this.scheduleError.set(
            status === 409
              ? 'conflict'
              : status === 404
                ? 'notFound'
                : status === 400
                  ? 'invalid'
                  : 'generic',
          );
        },
      });
  }

  protected formatErrandScheduleTime(value: string, timeZone: string): string {
    return formatScheduleDateTime(
      value,
      timeZone,
      this.language.activeLanguage() as SupportedLanguage,
    );
  }

  private handleErrandMutationError(error: unknown): void {
    const status = error instanceof HttpErrorResponse ? error.status : 0;
    this.errandMutationError.set(
      status === 409 ? 'conflict' : status === 404 ? 'notFound' : 'generic',
    );
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

  protected calculateRoute(): void {
    const currentPlan = this.plan();

    if (
      !currentPlan ||
      !currentPlan.originLocationId ||
      this.errands().length === 0 ||
      this.calculatingRoute() ||
      this.creatingErrand() ||
      this.editingEndpoint() !== null ||
      this.savingErrand() ||
      this.savingEndpoint() ||
      this.editingErrandId() !== null ||
      this.confirmingDeleteId() !== null ||
      this.errandMutationBusy()
    ) {
      return;
    }

    this.invalidateRoute();
    this.calculatingRoute.set(true);

    const selectedTravelMode = this.travelMode();

    this.routeApi
      .getOptimizedRoute(currentPlan.id, selectedTravelMode)
      .pipe(
        takeUntil(this.routeInvalidated$),
        takeUntilDestroyed(this.destroyRef),
        finalize(() => this.calculatingRoute.set(false)),
      )
      .subscribe({
        next: (result) => {
          const activePlan = this.plan();

          if (
            !activePlan ||
            activePlan.id !== currentPlan.id ||
            String(activePlan.revision) !== String(currentPlan.revision) ||
            String(result.planning.original.revision) !== String(currentPlan.revision) ||
            String(result.planning.original.revision) !== String(currentPlan.revision) ||
            result.planning.original.travelMode !==
            (selectedTravelMode === 1 ? 'Walking' : 'Driving')
          ) {
            this.routeError.set('conflict');
            return;
          }

          if (result.path && !mapRoutePath(result.path)) {
            this.routeError.set('invalid');
            return;
          }

          this.calculatedRoute.set(result);
        },

        error: (error: unknown) => {
          if (!(error instanceof HttpErrorResponse)) {
            this.routeError.set('generic');
            return;
          }

          switch (error.status) {
            case 404:
              this.routeError.set('notFound');
              break;

            case 409:
              this.routeError.set('conflict');
              break;

            case 503:
              this.routeError.set('unavailable');
              break;

            default:
              this.routeError.set('generic');
          }
        },
      });
  }

  protected formatRouteDistance(meters: number): string {
    const locale = this.language.activeLanguage() as SupportedLanguage;

    if (meters < 1000) {
      return `${new Intl.NumberFormat(locale, {
        maximumFractionDigits: 0,
      }).format(meters)} m`;
    }

    return `${new Intl.NumberFormat(locale, {
      maximumFractionDigits: 2,
    }).format(meters / 1000)} km`;
  }

  protected formatTravelMinutes(seconds: number): string {
    const locale = this.language.activeLanguage() as SupportedLanguage;

    return new Intl.NumberFormat(locale).format(Math.ceil(seconds / 60));
  }

  protected setTravelMode(mode: SupportedTravelMode): void {
    if (this.travelMode() === mode) {
      return;
    }

    this.travelMode.set(mode);
    this.invalidateRoute();
  }

  private invalidateRoute(): void {
    this.routeInvalidated$.next();

    this.calculatedRoute.set(null);
    this.routeError.set(null);
    this.calculatingRoute.set(false);
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
