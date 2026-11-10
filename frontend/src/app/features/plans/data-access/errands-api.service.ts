import { inject, Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import {
  ErrandsService,
  type CreateErrandInput,
  type ErrandDto,
  type ErrandMutationResult,
  type SetErrandScheduleInput,
  type ReorderErrandsInput,
  type ReorderErrandsResult,
  type UpdateErrandInput,
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

  update(
    planId: string,
    errandId: string,
    body: UpdateErrandInput,
  ): Observable<ErrandMutationResult> {
    return this.api.apiPlansPlanIdErrandsErrandIdPut$Json({
      planId,
      errandId,
      body,
    });
  }

  delete(planId: string, errandId: string, expectedRevision: number | string): Observable<void> {
    return this.api.apiPlansPlanIdErrandsErrandIdDelete({
      planId,
      errandId,
      expectedRevision,
    });
  }

  reorder(planId: string, body: ReorderErrandsInput): Observable<ReorderErrandsResult> {
    return this.api.apiPlansPlanIdErrandsOrderPut$Json({
      planId,
      body,
    });
  }

  setSchedule(
    planId: string,
    errandId: string,
    body: SetErrandScheduleInput,
  ): Observable<ErrandMutationResult> {
    return this.api.apiPlansPlanIdErrandsErrandIdSchedulePut$Json({
      planId,
      errandId,
      body,
    });
  }
}
