import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, input, output, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslocoDirective } from '@jsverse/transloco';
import { finalize } from 'rxjs';

import type { NewLocationInput, PlaceSearchResultDto } from '../../../../core/api/generated';

import { trimmedRequiredValidator } from '../../../../shared/validators/trimmed-required.validator';
import { PlacesApiService } from '../../data-access/places-api.service';
;

export interface NewErrandValue {
  title: string;
  durationMinutes: number;
  newLocation: NewLocationInput;
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

  readonly submitted = output<NewErrandValue>();
  readonly cancelled = output<void>();

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

  readonly placeQuery = new FormControl('', {
    nonNullable: true,
  });

  readonly results = signal<PlaceSearchResultDto[]>([]);
  readonly selectedPlace = signal<PlaceSearchResultDto | null>(null);

  readonly searching = signal(false);
  readonly searched = signal(false);
  readonly searchError = signal<SearchError | null>(null);

  constructor() {
    this.placeQuery.valueChanges.pipe(takeUntilDestroyed()).subscribe(() => {
      this.selectedPlace.set(null);
      this.results.set([]);
      this.searched.set(false);
      this.searchError.set(null);
    });
  }

  searchPlaces(): void {
    const query = this.placeQuery.value.trim();

    if (this.searching() || this.saving()) {
      return;
    }

    if (query.length < 3) {
      this.searchError.set('short');
      return;
    }

    this.selectedPlace.set(null);
    this.results.set([]);
    this.searched.set(false);
    this.searchError.set(null);
    this.searching.set(true);

    this.placesApi
      .search(query)
      .pipe(
        finalize(() => this.searching.set(false)),
        takeUntilDestroyed(this.destroyRef),
      )
      .subscribe({
        next: (places) => {
          if (query !== this.placeQuery.value.trim()) {
            return;
          }

          this.results.set(places);
          this.searched.set(true);
        },

        error: (error: unknown) => {
          if (query !== this.placeQuery.value.trim()) {
            return;
          }

          const status = error instanceof HttpErrorResponse ? error.status : 0;

          this.searchError.set(
            status === 400 ? 'specific' : status === 429 ? 'rateLimit' : 'unavailable',
          );
        },
      });
  }

  selectPlace(place: PlaceSearchResultDto): void {
    this.selectedPlace.set(place);
  }

  submit(): void {
    if (this.saving()) {
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const selected = this.selectedPlace();

    if (!selected) {
      return;
    }

    const { title, durationMinutes } = this.form.getRawValue();

    this.submitted.emit({
      title: title.trim(),
      durationMinutes,
      newLocation: {
        label: selected.label,
        address: selected.address,
        latitude: selected.latitude,
        longitude: selected.longitude,
      },
    });
  }
}
