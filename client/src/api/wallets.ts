import { apiRequest } from "./client";

export interface WalletSnapshotItemView {
  id: number;
  walletId: number;
  timestamp: string;
  totalValue: number;
  currency: string | null;
  notes: string | null;
}

export interface WalletView {
  id: number;
  name: string;
  address: string;
  chain: string;
  userId: number;
  snapshots: WalletSnapshotItemView[];
}

export interface UserView {
  id: number;
  username: string;
  email: string;
  wallets: WalletView[];
}

export interface CreateWalletRequest {
  name: string;
  address: string;
  chain: string;
}

export interface CreateWalletSnapshotRequest {
  notes?: string | null;
}

export async function getUser(userId: number): Promise<UserView> {
  return apiRequest<UserView>(`/users/${userId}`);
}

export async function getWalletsByUser(userId: number): Promise<WalletView[]> {
  return apiRequest<WalletView[]>(`/users/${userId}/wallets`);
}

export async function createWallet(
  userId: number,
  request: CreateWalletRequest,
): Promise<WalletView> {
  return apiRequest<WalletView>(`/users/${userId}/wallets`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}

export async function deleteWallet(walletId: number): Promise<void> {
  return apiRequest<void>(`/wallets/${walletId}`, {
    method: "DELETE",
  });
}

export async function getWalletSnapshots(
  walletId: number,
): Promise<WalletSnapshotItemView[]> {
  return apiRequest<WalletSnapshotItemView[]>(`/wallets/${walletId}/snapshots`);
}

export async function addWalletSnapshot(
  walletId: number,
  request: CreateWalletSnapshotRequest,
): Promise<WalletSnapshotItemView> {
  return apiRequest<WalletSnapshotItemView>(`/wallets/${walletId}/snapshots`, {
    method: "POST",
    body: JSON.stringify(request),
  });
}
