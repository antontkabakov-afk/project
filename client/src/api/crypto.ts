import { apiRequest } from "./client";

export interface CryptoAsset {
  assetId: string;
  name: string;
  symbol: string;
  currentPrice: number;
}

export interface CryptoAssetPricePoint {
  timestamp: string;
  currentPrice: number;
}

export async function getCryptoAssets(): Promise<CryptoAsset[]> {
  return apiRequest<CryptoAsset[]>("/crypto/assets");
}

export async function getCryptoAssetHistory(
  assetId: string,
): Promise<CryptoAssetPricePoint[]> {
  return apiRequest<CryptoAssetPricePoint[]>(
    `/crypto/assets/${encodeURIComponent(assetId)}/history`,
  );
}
