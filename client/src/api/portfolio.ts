import { apiRequest } from "./client";

export interface WalletAssetSnapshot {
  assetId: string;
  assetName: string;
  assetSymbol: string;
  tokenAddress: string;
  amountHeld: number;
  amountHeldFormatted: string;
  priceUsd: number;
  currentValue: number;
  isNativeToken: boolean;
  chain: string;
  logoUrl: string | null;
}

export interface WalletSnapshot {
  id: number;
  walletAddress: string;
  chain: string;
  timestamp: string;
  totalValueUsd: number;
  assets: WalletAssetSnapshot[];
}

export interface PortfolioPerformanceSummary {
  startTimestampUtc: string | null;
  endTimestampUtc: string | null;
  startingValueUsd: number;
  currentValueUsd: number;
  changeValueUsd: number;
  changePercentage: number;
}

export interface HistoricalPerformancePoint {
  timestamp: string;
  totalValueUsd: number;
  changeValueUsd: number;
  changePercentage: number;
}

export interface AssetDistribution {
  assetSymbol: string;
  assetName: string;
  amountHeld: number;
  currentValue: number;
  percentage: number;
}

export interface AssetPerformance {
  assetSymbol: string;
  assetName: string;
  amountHeld: number;
  currentValue: number;
  changeValueUsd: number;
  changePercentage: number;
}

export interface PortfolioStatistics {
  performance: PortfolioPerformanceSummary;
  history: HistoricalPerformancePoint[];
  distribution: AssetDistribution[];
  bestAsset: AssetPerformance | null;
  worstAsset: AssetPerformance | null;
}

export async function getAssets(walletId?: number): Promise<WalletAssetSnapshot[]> {
  return apiRequest<WalletAssetSnapshot[]>(`/portfolio/assets${buildQuery(undefined, walletId)}`);
}

export async function getSnapshotHistory(): Promise<WalletSnapshot[]> {
  return getSnapshotHistoryForRange();
}

export async function getSnapshotHistoryForRange(
  days?: number,
  walletId?: number,
): Promise<WalletSnapshot[]> {
  return apiRequest<WalletSnapshot[]>(`/portfolio/history${buildQuery(days, walletId)}`);
}

export async function getPortfolioStatistics(
  days?: number,
  walletId?: number,
): Promise<PortfolioStatistics> {
  return apiRequest<PortfolioStatistics>(`/portfolio/statistics${buildQuery(days, walletId)}`);
}

function buildQuery(days?: number, walletId?: number): string {
  const params = new URLSearchParams();

  if (days) {
    params.set("days", String(days));
  }

  if (walletId) {
    params.set("walletId", String(walletId));
  }

  const query = params.toString();
  return query ? `?${query}` : "";
}
