export type MapStopKind = 'errand' | 'origin' | 'destination';

export interface MapStop {
  id: string;
  title: string;
  locationLabel: string;
  latitude: number;
  longitude: number;
  position: number;
  kind?: MapStopKind;
}
