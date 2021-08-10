import ApiInstance from './api-base';

export interface OrganizationDto {
  id: string;
  name: string;
}

export interface GeofenceDto {
  name: string;
  vehiclesInZone: string[];
}

export const BrowseOrganizations = async (): Promise<OrganizationDto[]> => {
  const res = await ApiInstance.get('organization');
  return res?.data as OrganizationDto[];
};

export const GetDetails = async (id: string): Promise<GeofenceDto[]> => {
  const res = await ApiInstance.get(`organization/${id}`);
  return res?.data as GeofenceDto[];
};
