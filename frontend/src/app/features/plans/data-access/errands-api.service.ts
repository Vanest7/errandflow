import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ErrandsService,
  type CreateErrandInput,
  type ErrandDto,
  type ErrandMutationResult,
} from '../../../core/api/generated';

@Injectable({ providedIn: 'root' })
export class ErrandsApiService {
  private readonly api = inject(ErrandsService);

  list(planId: string): Observable<ErrandDto[]> {
    return this.api.apiPlansPlanIdErrandsGet$Json({ planId });
  }

  create(planId: string, body: CreateErrandInput): Observable<ErrandMutationResult> {
    return this.api.apiPlansPlanIdErrandsPost$Json({
      planId,
      body,
    });
  }
}
