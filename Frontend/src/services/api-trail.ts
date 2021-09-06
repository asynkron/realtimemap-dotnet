import { PositionsDto } from '@/signalr-hub'
import apiInstance from './api-base'

export const GetTrail = async (assetId: string): Promise<PositionsDto> => {
  const res = await apiInstance.get(`trail/${assetId}`)
  return res?.data as PositionsDto
}
