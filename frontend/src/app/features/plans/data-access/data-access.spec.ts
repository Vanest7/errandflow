import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import {
  ErrandsService,
  LocationsService,
  PlacesService,
  PlanningService,
  PlansService,
} from '../../../core/api/generated';
import { ErrandsApiService } from './errands-api.service';
import { LocationsApiService } from './location-api.service';
import { PlacesApiService } from './places-api.service';
import { PlanRouteApiService } from './plan-route-api.service';
import { PlansApiService } from './plans-api.service';

describe('plans data-access services', () => {
  let plansApi: {
    apiPlansGet$Json: ReturnType<typeof vi.fn>;
    getPlanById$Json: ReturnType<typeof vi.fn>;
    apiPlansPost$Json: ReturnType<typeof vi.fn>;
    apiPlansPlanIdLocationsPut$Json: ReturnType<typeof vi.fn>;
  };
  let errandsApi: {
    apiPlansPlanIdErrandsGet$Json: ReturnType<typeof vi.fn>;
    apiPlansPlanIdErrandsPost$Json: ReturnType<typeof vi.fn>;
    apiPlansPlanIdErrandsErrandIdPut$Json: ReturnType<typeof vi.fn>;
    apiPlansPlanIdErrandsErrandIdDelete: ReturnType<typeof vi.fn>;
    apiPlansPlanIdErrandsOrderPut$Json: ReturnType<typeof vi.fn>;
    apiPlansPlanIdErrandsErrandIdSchedulePut$Json: ReturnType<typeof vi.fn>;
  };
  let locationsApi: {
    apiPlansPlanIdLocationsGet$Json: ReturnType<typeof vi.fn>;
    apiPlansPlanIdLocationsPost$Json: ReturnType<typeof vi.fn>;
  };
  let placesApi: {
    apiPlacesSearchGet$Json: ReturnType<typeof vi.fn>;
  };
  let planningApi: {
    apiPlansPlanIdPlanningOptimizedRouteGet$Json: ReturnType<typeof vi.fn>;
  };

  beforeEach(() => {
    plansApi = {
      apiPlansGet$Json: vi.fn(() => of([])),
      getPlanById$Json: vi.fn(() => of({})),
      apiPlansPost$Json: vi.fn(() => of({})),
      apiPlansPlanIdLocationsPut$Json: vi.fn(() => of({})),
    };
    errandsApi = {
      apiPlansPlanIdErrandsGet$Json: vi.fn(() => of([])),
      apiPlansPlanIdErrandsPost$Json: vi.fn(() => of({})),
      apiPlansPlanIdErrandsErrandIdPut$Json: vi.fn(() => of({})),
      apiPlansPlanIdErrandsErrandIdDelete: vi.fn(() => of(undefined)),
      apiPlansPlanIdErrandsOrderPut$Json: vi.fn(() => of({})),
      apiPlansPlanIdErrandsErrandIdSchedulePut$Json: vi.fn(() => of({})),
    };
    locationsApi = {
      apiPlansPlanIdLocationsGet$Json: vi.fn(() => of([])),
      apiPlansPlanIdLocationsPost$Json: vi.fn(() => of({})),
    };
    placesApi = {
      apiPlacesSearchGet$Json: vi.fn(() => of([])),
    };
    planningApi = {
      apiPlansPlanIdPlanningOptimizedRouteGet$Json: vi.fn(() => of({})),
    };

    TestBed.configureTestingModule({
      providers: [
        { provide: PlansService, useValue: plansApi },
        { provide: ErrandsService, useValue: errandsApi },
        { provide: LocationsService, useValue: locationsApi },
        { provide: PlacesService, useValue: placesApi },
        { provide: PlanningService, useValue: planningApi },
      ],
    });
  });

  it('passes plan requests to the generated client', () => {
    const service = TestBed.inject(PlansApiService);
    const input = {
      departureTime: '09:00',
      name: 'Saturday errands',
      planDate: '2026-11-14',
      timeZone: 'Europe/Madrid',
    };

    service.list().subscribe();
    service.getById('plan-1').subscribe();
    service.create(input).subscribe();
    service
      .setLocations('plan-1', {
        originLocationId: 'origin',
        destinationLocationId: 'destination',
        expectedRevision: 2,
      })
      .subscribe();

    expect(plansApi.apiPlansGet$Json).toHaveBeenCalledWith();
    expect(plansApi.getPlanById$Json).toHaveBeenCalledWith({ planId: 'plan-1' });
    expect(plansApi.apiPlansPost$Json).toHaveBeenCalledWith({ body: input });
    expect(plansApi.apiPlansPlanIdLocationsPut$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
      body: {
        originLocationId: 'origin',
        destinationLocationId: 'destination',
        expectedRevision: 2,
      },
    });
  });

  it('passes errand mutations and expected revisions unchanged', () => {
    const service = TestBed.inject(ErrandsApiService);
    const createBody = { title: 'Collect parcel', durationMinutes: 15, expectedRevision: 3 };
    const updateBody = { title: 'Collect larger parcel', durationMinutes: 20, expectedRevision: 4 };
    const scheduleBody = {
      earliestStartAt: null,
      latestFinishAt: null,
      fixedStartAt: null,
      expectedRevision: 4,
    };

    service.list('plan-1').subscribe();
    service.create('plan-1', createBody).subscribe();
    service.update('plan-1', 'errand-1', updateBody).subscribe();
    service.delete('plan-1', 'errand-1', '4').subscribe();
    service
      .reorder('plan-1', { errandIds: ['errand-2', 'errand-1'], expectedRevision: 4 })
      .subscribe();
    service.setSchedule('plan-1', 'errand-1', scheduleBody).subscribe();

    expect(errandsApi.apiPlansPlanIdErrandsGet$Json).toHaveBeenCalledWith({ planId: 'plan-1' });
    expect(errandsApi.apiPlansPlanIdErrandsPost$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
      body: createBody,
    });
    expect(errandsApi.apiPlansPlanIdErrandsErrandIdPut$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
      errandId: 'errand-1',
      body: updateBody,
    });
    expect(errandsApi.apiPlansPlanIdErrandsErrandIdDelete).toHaveBeenCalledWith({
      planId: 'plan-1',
      errandId: 'errand-1',
      expectedRevision: '4',
    });
    expect(errandsApi.apiPlansPlanIdErrandsOrderPut$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
      body: { errandIds: ['errand-2', 'errand-1'], expectedRevision: 4 },
    });
    expect(errandsApi.apiPlansPlanIdErrandsErrandIdSchedulePut$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
      errandId: 'errand-1',
      body: scheduleBody,
    });
  });

  it('passes location requests to the generated client', () => {
    const service = TestBed.inject(LocationsApiService);
    const input = {
      address: 'Calle Mayor',
      expectedRevision: 2,
      label: 'Pharmacy',
      latitude: 40.4,
      longitude: -3.7,
    };

    service.list('plan-1').subscribe();
    service.create('plan-1', input).subscribe();

    expect(locationsApi.apiPlansPlanIdLocationsGet$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
    });
    expect(locationsApi.apiPlansPlanIdLocationsPost$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
      body: input,
    });
  });

  it('sorts nearby places, removes out-of-radius results and limits them to five', () => {
    const places = Array.from({ length: 6 }, (_, index) => ({
      providerPlaceId: `place-${index}`,
      name: `Place ${index}`,
      latitude: 40.4 + index * 0.001,
      longitude: -3.7,
    }));
    places.push({
      providerPlaceId: 'outside',
      name: 'Outside',
      latitude: 41.5,
      longitude: -3.7,
    });
    placesApi.apiPlacesSearchGet$Json.mockReturnValue(of(places));
    const service = TestBed.inject(PlacesApiService);

    service.search('pharmacy', { latitude: 40.4, longitude: -3.7 }).subscribe((result) => {
      expect(result.map((place) => place.providerPlaceId)).toEqual([
        'place-0',
        'place-1',
        'place-2',
        'place-3',
        'place-4',
      ]);
    });

    expect(placesApi.apiPlacesSearchGet$Json).toHaveBeenCalledWith({
      Query: 'pharmacy',
      Latitude: 40.4,
      Longitude: -3.7,
      RadiusKilometers: 3,
      Limit: 10,
    });
  });

  it('uses the area search contract without a center', () => {
    const service = TestBed.inject(PlacesApiService);

    service.searchArea('Atocha').subscribe();

    expect(placesApi.apiPlacesSearchGet$Json).toHaveBeenCalledWith({
      Query: 'Atocha',
      Limit: 5,
    });
  });

  it('passes the selected travel mode to route planning', () => {
    const service = TestBed.inject(PlanRouteApiService);

    service.getOptimizedRoute('plan-1', 3).subscribe();

    expect(planningApi.apiPlansPlanIdPlanningOptimizedRouteGet$Json).toHaveBeenCalledWith({
      planId: 'plan-1',
      travelMode: 3,
    });
  });
});
