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

export interface WalletConnection {
  isConnected: boolean;
  walletAddress: string | null;
  chain: string;
  connectedAtUtc: string | null;
  lastSnapshotAtUtc: string | null;
  lastSnapshotValueUsd: number | null;
}

export interface ConnectWalletRequest {
  walletAddress: string;
  chain: string;
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

export async function getWalletConnection(): Promise<WalletConnection> {
  return apiRequest<WalletConnection>("/wallet");
}

export async function connectWallet(
  request: ConnectWalletRequest,
): Promise<WalletConnection> {
  return apiRequest<WalletConnection>("/wallet", {
    method: "PUT",
    body: JSON.stringify(request),
  });
}

export async function createSnapshot(): Promise<WalletSnapshot> {
  return apiRequest<WalletSnapshot>("/wallet/snapshot", {
    method: "POST",
  });
}

export async function getAssets(): Promise<WalletAssetSnapshot[]> {
  return apiRequest<WalletAssetSnapshot[]>("/portfolio/assets");
}

export async function getSnapshotHistory(): Promise<WalletSnapshot[]> {
  return getSnapshotHistoryForRange();
}

export async function getSnapshotHistoryForRange(
  days?: number,
): Promise<WalletSnapshot[]> {
  return apiRequest<WalletSnapshot[]>(`/portfolio/history${buildRangeQuery(days)}`);
}

export async function getPortfolioStatistics(
  days?: number,
): Promise<PortfolioStatistics> {
  return apiRequest<PortfolioStatistics>(`/portfolio/statistics${buildRangeQuery(days)}`);
}

function buildRangeQuery(days?: number): string {
  return days ? `?days=${days}` : "";
}
