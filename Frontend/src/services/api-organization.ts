import apiInstance from './api-base';

export interface OrganizationDto {
  id: string;
  name: string;
}

export interface GeofenceDto {
  name: string;
  radiusInMeters: number;
  longitude: number;
  latitude: number;
  vehiclesInZone: string[];
}

export interface OrganizationDetailsDto extends OrganizationDto {
  geofences: GeofenceDto[];
}

export const browseOrganizations = async (): Promise<OrganizationDto[]> => {
  const res = await apiInstance.get('organization');
  return res?.data as OrganizationDto[];
};

export const getDetails = async (
  id: string
): Promise<OrganizationDetailsDto> => {
  const res = await apiInstance.get(`organization/${id}`);
  return res?.data as OrganizationDetailsDto;
};
