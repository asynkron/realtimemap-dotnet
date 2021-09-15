import { PositionDto } from "@/signalr-hub";
import { getBoundsWithMargin } from './boundsWithMargin';

// const stepsInAnimation = 10;

// this must have this shape to be compatible with mapbox
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
  shouldAnimate: boolean;
  icon: string;
}

export type AssetStates = { [vehicleId: string]: VehicleState };

export function handlePositionEvent(assetStates: AssetStates, positionDto: PositionDto) {
  if (!assetStates[positionDto.vehicleId]) {
    assetStates[positionDto.vehicleId] = createAssetFromState(positionDto);
  }

  updateAssetFromEvent(assetStates, positionDto);
}

function createAssetFromState(e: PositionDto): VehicleState {
  return {
    vehicleId: e.vehicleId,
    speed: 0,
    steps: 0,
    nextPosition: { lng: e.longitude, lat: e.latitude, heading: e.heading },
    currentPosition: { lng: e.longitude, lat: e.latitude, heading: e.heading },
    delta: { lat: 0, lng: 0, heading: 0 },
    shouldAnimate: false,
    icon: '',
  };
}

function updateAssetFromEvent(
  assetStates: AssetStates,
  positionDto: PositionDto
) {
  const assetState = assetStates[positionDto.vehicleId];

  assetState.currentPosition = {
    lat: positionDto.latitude,
    lng: positionDto.longitude,
    heading: positionDto.heading
  };

  // console.log(positionDto.vehicleId)
  // console.log(assetState.nextPosition);
  // console.log(positionDto);

  // const lng = positionDto.longitude - assetState.nextPosition.lng;
  // const lat = positionDto.latitude - assetState.nextPosition.lat;
  // let heading = positionDto.heading - assetState.nextPosition.heading;

  // if (lng != 0) {
  //   console.log(lng, lat);
  // }

  // //prevent full rotations when next and current course cross between 0 and 360
  // if (heading > 180) {
  //   heading -= 360;
  // }

  // if (heading < -180) {
  //   heading += 360;
  // }

  // assetState.steps = stepsInAnimation;
  // assetState.delta = {
  //   lng: lng / stepsInAnimation,
  //   lat: lat / stepsInAnimation,
  //   heading: heading / stepsInAnimation,
  // };
  // //console.log(assetState.delta );
  // assetState.currentPosition = assetState.nextPosition;
  // assetState.nextPosition = {
  //   lng: positionDto.longitude,
  //   lat: positionDto.latitude,
  //   heading: positionDto.heading,
  // };
  // assetState.shouldAnimate =
  //   assetState.delta.lat != 0 ||
  //   assetState.delta.lng != 0 ||
  //   assetState.delta.heading != 0;

  if (positionDto.doorsOpen) {
    //console.log("doors open...")
    assetState.icon = 'doorsopen';
  } else if (
    (positionDto.speed != undefined && positionDto.speed > 0) ||
    assetState.shouldAnimate
  ) {
    assetState.icon = 'moving';
  } else {
    assetState.icon = 'parked';
  }
}

export function mapAssetsToGeoJson(
  assetStates: AssetStates,
  predicate: (assetState: VehicleState) => boolean
) {
  return {
    type: 'FeatureCollection',
    features: Object.values(assetStates)
      .filter(predicate)
      .map((assetState) => ({
        type: 'Feature',
        geometry: {
          type: 'Point',
          coordinates: [
            assetState.currentPosition.lng,
            assetState.currentPosition.lat,
          ],
        },
        properties: {
          course: assetState.currentPosition.heading,
          'asset-id': assetState.vehicleId,
          speed: assetState.speed,
          // 'asset-type': assetState.assetType,
          icon: assetState.icon,
        },
      })),
  };
}

export function clearAssetsOutsideOfViewbox(map: mapboxgl.Map, assetStates: AssetStates) {

  const biggerBounds = getBoundsWithMargin(map);

  Object.values(assetStates)
    .filter(assetState => !biggerBounds.contains(assetState.currentPosition))
    .map(assetState => assetState.vehicleId)
    .forEach(id => delete assetStates[id]);

}
