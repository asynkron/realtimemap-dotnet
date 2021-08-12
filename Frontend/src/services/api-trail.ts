import { PositionsDto } from '@/signalr-hub'
import ApiInstance from './api-base'

export const GetTrail = async (assetId: string): Promise<PositionsDto> => {
  const res = await ApiInstance.get(`trail/${assetId}`)
  return res?.data as PositionsDto
}
