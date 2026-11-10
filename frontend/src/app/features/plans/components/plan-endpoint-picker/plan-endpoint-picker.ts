import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { finalize } from 'rxjs';

import type {
  LocationDto,
  NewLocationInput,
  PlaceSearchResultDto,
} from '../../../../core/api/generated';

import type { MapCoordinates } from '../../../../shared/map/models/map-coordinates';
import { trimmedRequiredValidator } from '../../../../shared/validators/trimmed-required.validator';
import { PlacesApiService } from '../../data-access/places-api.service';

export type EndpointKind = 'origin' | 'destination';

export type EndpointSelection =
  | {
      type: 'existing';
      locationId: string | null;
    }
  | {
      type: 'new';
      location: NewLocationInput;
    };

type SearchError = 'short' | 'specific' | 'rateLimit' | 'unavailable';

@Component({
  selector: 'app-plan-endpoint-picker',
  imports: [ReactiveFormsModule, TranslocoDirective],
  templateUrl: './plan-endpoint-picker.html',
})
export class PlanEndpointPicker {
  private readonly placesApi = inject(PlacesApiService);
  private readonly destroyRef = inject(DestroyRef);

  readonly kind = input.required<EndpointKind>();
  readonly locations = input.required<readonly LocationDto[]>();
  readonly currentLocationId = input<string | null>(null);
  readonly mapPoint = input<MapCoordinates | null>(null);
  readonly picking = input(false);
  readonly saving = input(false);

  readonly submitted = output<EndpointSelection>();
  readonly cancelled = output<void>();
  readonly pickOnMap = output<void>();
  readonly previewPoint = output<MapCoordinates | null>();

  readonly selectedLocationId = signal<string | null>(null);
  readonly selectedPlace = signal<PlaceSearchResultDto | null>(null);

  readonly searchResults = signal<PlaceSearchResultDto[]>([]);
  readonly searching = signal(false);
  readonly searched = signal(false);
  readonly searchError = signal<SearchError | null>(null);

  readonly searchQuery = new FormControl('', {
    nonNullable: true,
  });

  readonly manualLabel = new FormControl('', {
    nonNullable: true,
    validators: [trimmedRequiredValidator, Validators.maxLength(150)],
  });

  constructor() {
    effect(() => {
      this.selectedLocationId.set(this.currentLocationId());
    });

    this.searchQuery.valueChanges.pipe(takeUntilDestroyed(this.destroyRef)).subscribe(() => {
      this.selectedPlace.set(null);
      this.searchResults.set([]);
      this.searched.set(false);
      this.searchError.set(null);
      this.previewPoint.emit(null);
    });
  }

  selectExisting(event: Event): void {
    const select = event.target as HTMLSelectElement;

    this.selectedLocationId.set(select.value || null);
    this.selectedPlace.set(null);
    this.searchResults.set([]);
    this.searched.set(false);

    this.previewPoint.emit(null);
  }

  searchPlaces(): void {
    const query = this.searchQuery.value.trim();

    if (this.searching() || this.saving()) {
      return;
    }

    if (query.length < 3) {
      this.searchError.set('short');
      return;
    }

    this.selectedLocationId.set(null);
    this.selectedPlace.set(null);
    this.searchResults.set([]);
    this.searched.set(false);
    this.searchError.set(null);
    this.searching.set(true);

    this.previewPoint.emit(null);

    this.placesApi
      .searchArea(query)
      .pipe(
        finalize(() => this.searching.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (results) => {
          if (query !== this.searchQuery.value.trim()) {
            return;
          }

          this.searchResults.set(results);
          this.searched.set(true);
        },
        error: (error: unknown) => {
          if (query !== this.searchQuery.value.trim()) {
            return;
          }

          this.searchError.set(this.resolveSearchError(error));
        },
      });
  }

  selectPlace(place: PlaceSearchResultDto): void {
    this.selectedPlace.set(place);
    this.selectedLocationId.set(null);
    this.searchResults.set([]);
    this.searched.set(false);

    this.previewPoint.emit({
      latitude: Number(place.latitude),
      longitude: Number(place.longitude),
    });
  }

  chooseOnMap(): void {
    if (this.saving()) {
      return;
    }

    this.selectedPlace.set(null);
    this.selectedLocationId.set(null);
    this.searchResults.set([]);
    this.searched.set(false);

    this.previewPoint.emit(null);
    this.pickOnMap.emit();
  }

  submit(event: Event): void {
    event.preventDefault();
    if (this.saving() || this.searching() || this.picking()) {
      return;
    }

    const place = this.selectedPlace();

    if (place) {
      this.submitted.emit({
        type: 'new',
        location: {
          label: place.label,
          address: place.address,
          latitude: place.latitude,
          longitude: place.longitude,
        },
      });

      return;
    }

    const point = this.mapPoint();

    if (point) {
      if (this.manualLabel.invalid) {
        this.manualLabel.markAsTouched();
        return;
      }

      this.submitted.emit({
        type: 'new',
        location: {
          label: this.manualLabel.value.trim(),
          address: null,
          latitude: point.latitude,
          longitude: point.longitude,
        },
      });

      return;
    }

    this.submitted.emit({
      type: 'existing',
      locationId: this.selectedLocationId(),
    });
  }

  private resolveSearchError(error: unknown): SearchError {
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
