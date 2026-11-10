import {
  AfterViewInit,
  Component,
  effect,
  ElementRef,
  inject,
  input,
  OnDestroy,
  output,
  signal,
  untracked,
  ViewChild,
} from '@angular/core';
import { TranslocoDirective } from '@jsverse/transloco';

import Feature from 'ol/Feature.js';
import OlMap from 'ol/Map.js';
import View from 'ol/View.js';

import { defaults as defaultControls } from 'ol/control/defaults.js';
import LineString from 'ol/geom/LineString.js';
import Point from 'ol/geom/Point.js';
import TileLayer from 'ol/layer/Tile.js';
import VectorLayer from 'ol/layer/Vector.js';
import { fromLonLat, toLonLat } from 'ol/proj.js';
import OSM from 'ol/source/OSM.js';
import VectorSource from 'ol/source/Vector.js';

import { Circle as CircleStyle, Fill, Stroke, Style, Text } from 'ol/style.js';

import { ThemeService } from '../../../../core/theme/theme.service';

import type { MapStop } from '../../models/map-stop.model';
import type { MapCoordinates } from '../../models/map-coordinates';

@Component({
  selector: 'app-route-map',
  imports: [TranslocoDirective],
  templateUrl: './route-map.html',
})
export class RouteMap implements AfterViewInit, OnDestroy {
  private readonly theme = inject(ThemeService);

  @ViewChild('mapElement')
  private mapElement!: ElementRef<HTMLDivElement>;

  @ViewChild('accentSample')
  private accentSample!: ElementRef<HTMLSpanElement>;

  @ViewChild('primarySample')
  private primarySample!: ElementRef<HTMLSpanElement>;

  readonly stops = input.required<readonly MapStop[]>();

  readonly routeLegs = input<readonly (readonly MapCoordinates[])[]>([]);

  readonly picking = input(false);

  readonly pickTarget = input<'errand' | 'origin' | 'destination'>('errand');

  readonly draftPoint = input<MapCoordinates | null>(null);

  readonly focusPoint = input<MapCoordinates | null>(null);

  readonly pointPicked = output<MapCoordinates>();

  protected readonly selectedStop = signal<MapStop | null>(null);

  private map?: OlMap;

  private readonly routeSource = new VectorSource<Feature<LineString>>();

  private readonly markerSource = new VectorSource<Feature<Point>>();

  private readonly draftSource = new VectorSource<Feature<Point>>();

  constructor() {
    effect(() => {
      const stops = this.stops();

      untracked(() => {
        this.renderStops(stops);
      });
    });

    effect(() => {
      const point = this.draftPoint();

      untracked(() => {
        this.renderDraftPoint(point);
      });
    });

    effect(() => {
      const point = this.focusPoint();

      untracked(() => {
        this.focusOn(point);
      });
    });

    effect(() => {
      const picking = this.picking();

      untracked(() => {
        this.updateCursor(picking);

        if (picking) {
          this.selectedStop.set(null);
        }
      });
    });

    effect(() => {
      const isDark = this.theme.isDark();

      untracked(() => {
        if (!this.map) {
          return;
        }

        this.updateMarkerStyles(isDark);
        this.updateRouteStyles();
        this.renderDraftPoint(this.draftPoint());
      });
    });

    effect(() => {
      const legs = this.routeLegs();

      untracked(() => {
        this.renderRouteLegs(legs);
      });
    });
  }

  ngAfterViewInit(): void {
    this.initializeMap();

    this.renderStops(this.stops());
    this.renderDraftPoint(this.draftPoint());
    this.focusOn(this.focusPoint());

    this.renderRouteLegs(this.routeLegs());

    this.updateCursor(this.picking());
  }

  ngOnDestroy(): void {
    this.map?.dispose();
  }

  private initializeMap(): void {
    this.map = new OlMap({
      target: this.mapElement.nativeElement,

      layers: [
        new TileLayer({
          source: new OSM(),
        }),

        new VectorLayer({
          source: this.routeSource,
        }),

        new VectorLayer({
          source: this.markerSource,
        }),

        new VectorLayer({
          source: this.draftSource,
        }),
      ],

      view: new View({
        center: fromLonLat([0, 20]),
        zoom: 2,
      }),

      controls: defaultControls({
        attributionOptions: {
          collapsible: false,
        },
      }),
    });

    this.map.on('singleclick', (event) => {
      if (this.picking()) {
        const [longitude, latitude] = toLonLat(event.coordinate);

        if (
          !Number.isFinite(latitude) ||
          !Number.isFinite(longitude) ||
          latitude < -90 ||
          latitude > 90 ||
          longitude < -180 ||
          longitude > 180
        ) {
          return;
        }

        this.pointPicked.emit({
          latitude: Number(latitude.toFixed(6)),
          longitude: Number(longitude.toFixed(6)),
        });

        return;
      }

      const feature = this.map?.forEachFeatureAtPixel(event.pixel, (candidate) => candidate, {
        hitTolerance: 8,
      });

      const stop = feature?.get('stop') as MapStop | undefined;

      this.selectedStop.set(stop ?? null);
    });

    requestAnimationFrame(() => {
      this.map?.updateSize();
    });
  }

  private renderStops(stops: readonly MapStop[]): void {
    if (!this.map) {
      return;
    }

    this.markerSource.clear();
    this.selectedStop.set(null);

    const validStops = stops.filter(
      (stop) =>
        Number.isFinite(stop.latitude) &&
        Number.isFinite(stop.longitude) &&
        stop.latitude >= -90 &&
        stop.latitude <= 90 &&
        stop.longitude >= -180 &&
        stop.longitude <= 180,
    );

    for (const stop of validStops) {
      const coordinates = fromLonLat([stop.longitude, stop.latitude]);

      const feature = new Feature<Point>({
        geometry: new Point(coordinates),
      });

      feature.set('stop', stop);

      feature.setStyle(this.createMarkerStyle(stop, this.theme.isDark()));

      this.markerSource.addFeature(feature);
    }

    if (validStops.length === 0) {
      const focusPoint = this.focusPoint();

      if (focusPoint) {
        this.focusOn(focusPoint);
      } else {
        this.map.getView().setCenter(fromLonLat([0, 20]));
        this.map.getView().setZoom(2);
      }

      return;
    }

    this.map.updateSize();

    const extent = this.routeSource.isEmpty()
      ? this.markerSource.getExtent()
      : this.routeSource.getExtent();

    if (extent === null) {
      return;
    }

    this.map.getView().fit(extent, {
      padding: [60, 60, 60, 60],
      maxZoom: 15,
      duration: 250,
    });
  }

  private renderDraftPoint(point: MapCoordinates | null): void {
    if (!this.map) {
      return;
    }

    this.draftSource.clear();

    if (!point) {
      return;
    }

    if (
      !Number.isFinite(point.latitude) ||
      !Number.isFinite(point.longitude) ||
      point.latitude < -90 ||
      point.latitude > 90 ||
      point.longitude < -180 ||
      point.longitude > 180
    ) {
      return;
    }

    const coordinates = fromLonLat([point.longitude, point.latitude]);

    const feature = new Feature<Point>({
      geometry: new Point(coordinates),
    });

    feature.setStyle(this.createDraftStyle());

    this.draftSource.addFeature(feature);
  }

  private createMarkerStyle(stop: MapStop, isDark: boolean): Style {
    const accentColor = getComputedStyle(this.accentSample.nativeElement).color;

    const primaryColor = getComputedStyle(this.primarySample.nativeElement).color;

    const isOrigin = stop.kind === 'origin';
    const isDestination = stop.kind === 'destination';

    const markerColor = isOrigin ? primaryColor : accentColor;

    const markerText = isOrigin ? 'A' : isDestination ? 'B' : String(stop.position);

    const textColor = isDark ? '#1b2834' : '#ffffff';
    const borderColor = isDark ? '#253746' : '#ffffff';

    return new Style({
      image: new CircleStyle({
        radius: isOrigin || isDestination ? 21 : 19,

        fill: new Fill({
          color: markerColor,
        }),

        stroke: new Stroke({
          color: borderColor,
          width: 3,
        }),
      }),

      text: new Text({
        text: markerText,
        font: 'bold 14px Roboto, sans-serif',

        fill: new Fill({
          color: textColor,
        }),
      }),
    });
  }

  private createDraftStyle(): Style {
    const primaryColor = getComputedStyle(this.primarySample.nativeElement).color;

    const isDark = this.theme.isDark();

    return new Style({
      image: new CircleStyle({
        radius: 14,

        fill: new Fill({
          color: primaryColor,
        }),

        stroke: new Stroke({
          color: isDark ? '#253746' : '#ffffff',
          width: 3,
        }),
      }),

      text: new Text({
        text: '+',
        font: 'bold 18px Roboto, sans-serif',

        fill: new Fill({
          color: isDark ? '#082c2e' : '#ffffff',
        }),
      }),
    });
  }

  private updateMarkerStyles(isDark: boolean): void {
    for (const feature of this.markerSource.getFeatures()) {
      const stop = feature.get('stop') as MapStop;

      feature.setStyle(this.createMarkerStyle(stop, isDark));
    }
  }

  private focusOn(point: MapCoordinates | null): void {
    if (!this.map || !point) {
      return;
    }

    if (
      !Number.isFinite(point.latitude) ||
      !Number.isFinite(point.longitude) ||
      point.latitude < -90 ||
      point.latitude > 90 ||
      point.longitude < -180 ||
      point.longitude > 180
    ) {
      return;
    }

    this.map.getView().animate({
      center: fromLonLat([point.longitude, point.latitude]),
      zoom: 14,
      duration: 300,
    });
  }

  private updateCursor(picking: boolean): void {
    if (!this.map) {
      return;
    }

    const viewport = this.map.getViewport();

    if (viewport) {
      viewport.style.cursor = picking ? 'crosshair' : '';
    }
  }
  private renderRouteLegs(legs: readonly (readonly MapCoordinates[])[]): void {
    if (!this.map) {
      return;
    }

    this.routeSource.clear();

    for (const leg of legs) {
      if (
        leg.length < 2 ||
        leg.some(
          (point) =>
            !Number.isFinite(point.latitude) ||
            !Number.isFinite(point.longitude) ||
            point.latitude < -90 ||
            point.latitude > 90 ||
            point.longitude < -180 ||
            point.longitude > 180,
        )
      ) {
        continue;
      }

      const coordinates = leg.map((point) => fromLonLat([point.longitude, point.latitude]));

      const feature = new Feature<LineString>({
        geometry: new LineString(coordinates),
      });

      this.routeSource.addFeature(feature);
    }

    this.updateRouteStyles();

    if (this.routeSource.getFeatures().length === 0) {
      return;
    }

    this.map.updateSize();

    const extent = this.routeSource.getExtent();

    if (!extent) {
      return;
    }

    this.map.getView().fit(extent, {
      padding: [60, 60, 60, 60],
      maxZoom: 16,
      duration: 350,
    });
  }

  private updateRouteStyles(): void {
    if (!this.map) {
      return;
    }

    const routeColor = getComputedStyle(this.primarySample.nativeElement).color;

    const style = new Style({
      stroke: new Stroke({
        color: routeColor,
        width: 6,
      }),
    });

    for (const feature of this.routeSource.getFeatures()) {
      feature.setStyle(style);
    }
  }
}
