import { PositionDto } from '@/hub';
import { getBoundsWithMargin } from './boundsWithMargin';
import { GeoJSONSourceData } from "./mapUtils";

export interface VehiclePosition {
  lat: number;
  lng: number;
  heading: number;
}

export interface VehicleState {
  vehicleId: string;
  steps: number;
  delta: VehiclePosition;
  currentPosition: VehiclePosition;
  speed: number;
  nextPosition: VehiclePosition;
  icon: string;
}

export type VehicleStates = { [vehicleId: string]: VehicleState };

export function handlePositionEvent(vehicleStates: VehicleStates, positionDto: PositionDto) {
  if (!vehicleStates[positionDto.vehicleId]) {
    vehicleStates[positionDto.vehicleId] = createVehicleFromState(positionDto);
  }

  updateVehicleFromEvent(vehicleStates, positionDto);
}

function createVehicleFromState(e: PositionDto): VehicleState {
  return {
    vehicleId: e.vehicleId,
    speed: 0,
    steps: 0,
    nextPosition: { lng: e.longitude, lat: e.latitude, heading: e.heading },
    currentPosition: { lng: e.longitude, lat: e.latitude, heading: e.heading },
    delta: { lat: 0, lng: 0, heading: 0 },
    icon: '',
  };
}

function updateVehicleFromEvent(
  vehicleStates: VehicleStates,
  positionDto: PositionDto
) {
  const vehicleState = vehicleStates[positionDto.vehicleId];

  vehicleState.currentPosition = {
    lat: positionDto.latitude,
    lng: positionDto.longitude,
    heading: positionDto.heading
  };

  if (positionDto.doorsOpen) {
    vehicleState.icon = 'doors-open';
  } else if (positionDto.speed != undefined && positionDto.speed > 0) {
    vehicleState.icon = 'moving';
  } else {
    // todo: use better icon
    vehicleState.icon = 'moving';
  }
}

export function mapVehiclesToGeoJson(
  vehicleStates: VehicleStates,
  predicate: (vehicleState: VehicleState) => boolean
): GeoJSONSourceData {
  return {
    type: 'FeatureCollection',
    features: Object.values(vehicleStates)
      .filter(predicate)
      .map((vehicleState) => ({
        type: 'Feature',
        geometry: {
          type: 'Point',
          coordinates: [
            vehicleState.currentPosition.lng,
            vehicleState.currentPosition.lat,
          ],
        },
        properties: {
          course: vehicleState.currentPosition.heading,
          vehicleId: vehicleState.vehicleId,
          speed: vehicleState.speed,
          icon: vehicleState.icon,
        },
      })),
  };
}

export function clearVehiclesOutsideOfViewbox(map: mapboxgl.Map, vehicleStates: VehicleStates) {

  const biggerBounds = getBoundsWithMargin(map);

  Object.values(vehicleStates)
    .filter(vehicleState => !biggerBounds.contains(vehicleState.currentPosition))
    .map(vehicleState => vehicleState.vehicleId)
    .forEach(id => delete vehicleStates[id]);

}
