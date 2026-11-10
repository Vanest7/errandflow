import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, model, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { finalize } from 'rxjs';

import type { ErrandDto, LocationDto, NewLocationInput, PlaceSearchResultDto } from '../../../../core/api/generated';

import { trimmedRequiredValidator } from '../../../../shared/validators/trimmed-required.validator';
import { PlacesApiService, type SearchCenter } from '../../data-access/places-api.service';
import { MapCoordinates } from '../../../../shared/map/models/map-coordinates';

export interface NewErrandValue {
  title: string;
  durationMinutes: number;
  newLocation: NewLocationInput;
}

export interface UpdatedErrandValue {
  title: string;
  durationMinutes: number;
  location:
    | { type: 'existing'; locationId: string }
    | { type: 'new'; newLocation: NewLocationInput };
}

type SearchError = 'short' | 'specific' | 'rateLimit' | 'unavailable';

@Component({
  selector: 'app-errand-form',
  imports: [ReactiveFormsModule, TranslocoDirective],
  templateUrl: './errand-form.html',
})
export class ErrandForm {
  private readonly placesApi = inject(PlacesApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly saving = input(false);
  readonly errand = input<ErrandDto | null>(null);
  readonly currentLocation = input<LocationDto | null>(null);

  readonly updated = output<UpdatedErrandValue>();
  readonly submitted = output<NewErrandValue>();
  readonly cancelled = output<void>();
  readonly mapPoint = model<MapCoordinates | null>(null);

  readonly pickingOnMap = input(false);

  readonly pickOnMap = output<void>();
  readonly stopPicking = output<void>();
  readonly areaSelected = output<MapCoordinates>();

  readonly locationLabelControl = new FormControl('', {
    nonNullable: true,
    validators: [trimmedRequiredValidator, Validators.maxLength(150)],
  });
  readonly form = new FormGroup({
    title: new FormControl('', {
      nonNullable: true,
      validators: [trimmedRequiredValidator, Validators.maxLength(150)],
    }),

    durationMinutes: new FormControl(10, {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.min(1),
        Validators.max(65535),
        Validators.pattern(/^\d+$/),
      ],
    }),
  });

  readonly areaQuery = new FormControl('', {
    nonNullable: true,
  });

  readonly placeQuery = new FormControl('', {
    nonNullable: true,
  });

  readonly areaResults = signal<PlaceSearchResultDto[]>([]);
  readonly selectedArea = signal<PlaceSearchResultDto | null>(null);

  readonly results = signal<PlaceSearchResultDto[]>([]);
  readonly selectedPlace = signal<PlaceSearchResultDto | null>(null);

  readonly searchingArea = signal(false);
  readonly searching = signal(false);

  readonly areaSearched = signal(false);
  readonly searched = signal(false);

  readonly areaError = signal<SearchError | null>(null);
  readonly searchError = signal<SearchError | null>(null);

  constructor() {
    effect(() => {
      const errand = this.errand();
      if (errand) {
        this.form.patchValue({
          title: errand.title,
          durationMinutes: Number(errand.durationMinutes),
        });
      }
    });

    this.areaQuery.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.selectedArea.set(null);
      this.areaResults.set([]);
      this.areaSearched.set(false);
      this.areaError.set(null);

      this.resetPlaceSelection();
    });

    this.placeQuery.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.resetPlaceSelection();
    });
  }

  searchAreas(): void {
    const query = this.areaQuery.value.trim();

    if (this.searchingArea() || this.searching() || this.saving()) {
      return;
    }

    if (query.length < 3) {
      this.areaError.set('short');
      return;
    }

    this.areaResults.set([]);
    this.areaSearched.set(false);
    this.selectedArea.set(null);
    this.areaError.set(null);
    this.searchingArea.set(true);

    this.resetPlaceSelection();

    this.placesApi
      .searchArea(query)
      .pipe(
        finalize(() => this.searchingArea.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (areas) => {
          if (query !== this.areaQuery.value.trim()) {
            return;
          }

          this.areaResults.set(areas);
          this.areaSearched.set(true);
        },

        error: (error: unknown) => {
          if (query !== this.areaQuery.value.trim()) {
            return;
          }

          this.areaError.set(this.resolveError(error));
        },
      });
  }

  selectArea(area: PlaceSearchResultDto): void {
    this.selectedArea.set(area);

    this.areaResults.set([]);
    this.areaSearched.set(false);
    this.areaError.set(null);

    this.resetPlaceSelection();

    this.mapPoint.set(null);
    this.stopPicking.emit();

    this.areaSelected.emit({
      latitude: Number(area.latitude),
      longitude: Number(area.longitude),
    });
  }

  chooseOnMap(): void {
    if (this.saving()) {
      return;
    }

    this.selectedPlace.set(null);
    this.results.set([]);
    this.searched.set(false);
    this.searchError.set(null);

    this.pickOnMap.emit();
  }

  searchPlaces(): void {
    const area = this.selectedArea();
    const query = this.placeQuery.value.trim();

    if (!area || this.searching() || this.searchingArea() || this.saving()) {
      return;
    }

    if (query.length < 3) {
      this.searchError.set('short');
      return;
    }

    const center: SearchCenter = {
      latitude: Number(area.latitude),
      longitude: Number(area.longitude),
    };

    this.results.set([]);
    this.selectedPlace.set(null);
    this.searched.set(false);
    this.searchError.set(null);
    this.searching.set(true);

    this.placesApi
      .search(query, center)
      .pipe(
        finalize(() => this.searching.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (places) => {
          if (query !== this.placeQuery.value.trim() || this.selectedArea() !== area) {
            return;
          }

          this.results.set(places);
          this.searched.set(true);
        },

        error: (error: unknown) => {
          if (query !== this.placeQuery.value.trim() || this.selectedArea() !== area) {
            return;
          }

          this.searchError.set(this.resolveError(error));
        },
      });
  }

  selectPlace(place: PlaceSearchResultDto): void {
    this.selectedPlace.set(place);

    this.mapPoint.set(null);
    this.stopPicking.emit();

    this.results.set([]);
    this.searched.set(false);
  }

  submit(): void {
    if (this.saving() || this.searching() || this.searchingArea() || this.pickingOnMap()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const selected = this.selectedPlace();
    const point = this.mapPoint();
    const existing = this.errand();

    if (!selected && !point && !existing) {
      return;
    }

    const { title, durationMinutes } = this.form.getRawValue();
    let newLocation: NewLocationInput | null = null;

    if (selected) {
      newLocation = {
        label: selected.label,
        address: selected.address,
        latitude: selected.latitude,
        longitude: selected.longitude,
      };
    } else if (point) {
      if (this.locationLabelControl.invalid) {
        this.locationLabelControl.markAsTouched();
        return;
      }

      newLocation = {
        label: this.locationLabelControl.value.trim(),
        address: null,
        latitude: point.latitude,
        longitude: point.longitude,
      };
    }

    if (existing) {
      this.updated.emit({
        title: title.trim(),
        durationMinutes,
        location: newLocation
          ? { type: 'new', newLocation }
          : { type: 'existing', locationId: existing.locationId },
      });
      return;
    }

    if (!newLocation) {
      return;
    }

    this.submitted.emit({
      title: title.trim(),
      durationMinutes,
      newLocation,
    });
  }

  private resetPlaceSelection(): void {
    this.selectedPlace.set(null);
    this.results.set([]);
    this.searched.set(false);
    this.searchError.set(null);
  }

  private resolveError(error: unknown): SearchError {
    if (!(error instanceof HttpErrorResponse)) {
      return 'unavailable';
    }

    switch (error.status) {
      case 400:
        return 'specific';

      case 429:
        return 'rateLimit';

      default:
        return 'unavailable';
    }
  }
}
