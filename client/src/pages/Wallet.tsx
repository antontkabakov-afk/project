import { type FormEvent, useEffect, useState } from "react";

import { refresh } from "@/api/auth";
import { getApiErrorMessage } from "@/api/client";
import {
  addWalletSnapshot,
  createWallet,
  deleteWallet,
  getUser,
  getWalletSnapshots,
  type UserView,
  type WalletSnapshotItemView,
  type WalletView,
} from "@/api/wallets";
import DashboardCard from "@/components/dashboard-card";
import DashboardPageHeader from "@/components/dashboard-page-header";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

const walletChainOptions = [
  { label: "Ethereum", value: "eth" },
  { label: "Polygon", value: "polygon" },
  { label: "Base", value: "base" },
  { label: "Arbitrum", value: "arbitrum" },
  { label: "Optimism", value: "optimism" },
  { label: "BNB Chain", value: "bsc" },
  { label: "Avalanche", value: "avalanche" },
] as const;

export default function Wallet() {
  const [currentUserId, setCurrentUserId] = useState<number | null>(null);
  const [user, setUser] = useState<UserView | null>(null);
  const [selectedWalletId, setSelectedWalletId] = useState<number | null>(null);
  const [selectedWalletSnapshots, setSelectedWalletSnapshots] = useState<
    WalletSnapshotItemView[]
  >([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSnapshotsLoading, setIsSnapshotsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [walletFormErrorMessage, setWalletFormErrorMessage] = useState("");
  const [snapshotFormErrorMessage, setSnapshotFormErrorMessage] = useState("");
  const [isCreatingWallet, setIsCreatingWallet] = useState(false);
  const [isAddingSnapshot, setIsAddingSnapshot] = useState(false);
  const [isDeletingWalletId, setIsDeletingWalletId] = useState<number | null>(null);
  const [walletName, setWalletName] = useState("");
  const [walletAddress, setWalletAddress] = useState("");
  const [walletChain, setWalletChain] = useState<string>(walletChainOptions[0].value);
  const [snapshotNotes, setSnapshotNotes] = useState("");

  const selectedWallet =
    user?.wallets.find((wallet) => wallet.id === selectedWalletId) ?? null;

  useEffect(() => {
    let isMounted = true;

    const run = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const session = await refresh();

        if (!isMounted) {
          return;
        }

        if (!session?.isSuccess) {
          setCurrentUserId(null);
          setUser(null);
          setSelectedWalletId(null);
          setSelectedWalletSnapshots([]);
          setErrorMessage("Your session has expired. Please log in again.");
          return;
        }

        const nextUser = await getUser(session.id);

        if (!isMounted) {
          return;
        }

        setCurrentUserId(session.id);
        setUser(nextUser);
        setSelectedWalletId(nextUser.wallets[0]?.id ?? null);
      } catch (error) {
        if (!isMounted) {
          return;
        }

        setErrorMessage(getApiErrorMessage(error));
      } finally {
        if (isMounted) {
          setIsLoading(false);
        }
      }
    };

    run();

    return () => {
      isMounted = false;
    };
  }, []);

  useEffect(() => {
    let isMounted = true;

    if (!selectedWalletId) {
      setSelectedWalletSnapshots([]);
      setIsSnapshotsLoading(false);
      return;
    }

    const run = async () => {
      setIsSnapshotsLoading(true);
      setSnapshotFormErrorMessage("");

      try {
        const snapshots = await getWalletSnapshots(selectedWalletId);

        if (!isMounted) {
          return;
        }

        setSelectedWalletSnapshots(snapshots);
      } catch (error) {
        if (!isMounted) {
          return;
        }

        setSnapshotFormErrorMessage(getApiErrorMessage(error));
      } finally {
        if (isMounted) {
          setIsSnapshotsLoading(false);
        }
      }
    };

    run();

    return () => {
      isMounted = false;
    };
  }, [selectedWalletId]);

  async function loadUser(userId: number, preferredWalletId?: number | null) {
    const nextUser = await getUser(userId);
    const nextSelectedWalletId =
      preferredWalletId && nextUser.wallets.some((wallet) => wallet.id === preferredWalletId)
        ? preferredWalletId
        : selectedWalletId && nextUser.wallets.some((wallet) => wallet.id === selectedWalletId)
          ? selectedWalletId
          : nextUser.wallets[0]?.id ?? null;

    setUser(nextUser);
    setSelectedWalletId(nextSelectedWalletId);

    return nextSelectedWalletId;
  }

  async function loadSnapshots(walletId: number) {
    const snapshots = await getWalletSnapshots(walletId);
    setSelectedWalletSnapshots(snapshots);
  }

  async function handleCreateWallet(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setWalletFormErrorMessage("");

    if (!currentUserId) {
      setWalletFormErrorMessage("You must be logged in to add a wallet.");
      return;
    }

    if (!walletName.trim() || !walletAddress.trim()) {
      setWalletFormErrorMessage("Wallet name and address are required.");
      return;
    }

    setIsCreatingWallet(true);

    try {
      const wallet = await createWallet(currentUserId, {
        name: walletName.trim(),
        address: walletAddress.trim(),
        chain: walletChain,
      });

      setWalletName("");
      setWalletAddress("");
      setWalletChain(walletChainOptions[0].value);
      await loadUser(currentUserId, wallet.id);
    } catch (error) {
      setWalletFormErrorMessage(getApiErrorMessage(error));
    } finally {
      setIsCreatingWallet(false);
    }
  }

  async function handleDeleteWallet(wallet: WalletView) {
    if (!currentUserId) {
      setErrorMessage("You must be logged in to delete a wallet.");
      return;
    }

    if (!window.confirm(`Delete ${wallet.name} and all of its snapshots?`)) {
      return;
    }

    setErrorMessage("");
    setIsDeletingWalletId(wallet.id);

    try {
      await deleteWallet(wallet.id);
      setSelectedWalletSnapshots([]);
      await loadUser(currentUserId);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    } finally {
      setIsDeletingWalletId(null);
    }
  }

  async function handleAddSnapshot(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSnapshotFormErrorMessage("");

    if (!currentUserId || !selectedWalletId) {
      setSnapshotFormErrorMessage("Select a wallet before capturing a snapshot.");
      return;
    }

    setIsAddingSnapshot(true);

    try {
      await addWalletSnapshot(selectedWalletId, {
        notes: snapshotNotes.trim() || null,
      });

      setSnapshotNotes("");
      await loadUser(currentUserId, selectedWalletId);
      await loadSnapshots(selectedWalletId);
    } catch (error) {
      setSnapshotFormErrorMessage(getApiErrorMessage(error));
    } finally {
      setIsAddingSnapshot(false);
    }
  }

  return (
    <div className="mx-auto max-w-7xl px-6">
      <DashboardPageHeader
        eyebrow="Wallets"
        title="Manage multiple wallets and capture chain-aware snapshots"
        description="Every wallet belongs to one user and one chain. Wallet creation validates the address with Moralis, and each snapshot pulls balances and pricing automatically."
      />

      {isLoading ? (
        <DashboardCard>
          <p className="text-white/70">Loading wallets...</p>
        </DashboardCard>
      ) : errorMessage ? (
        <DashboardCard>
          <p className="text-sm text-rose-300">{errorMessage}</p>
        </DashboardCard>
      ) : !user ? (
        <DashboardCard>
          <p className="text-white/70">Unable to load the current user.</p>
        </DashboardCard>
      ) : (
        <div className="space-y-6">
          <div className="grid gap-6 xl:grid-cols-[minmax(320px,0.9fr)_minmax(0,1.1fr)]">
            <DashboardCard>
              <div className="flex items-start justify-between gap-4">
                <div>
                  <p className="text-xs uppercase tracking-[0.35em] text-[#00F5C8]/80">
                    Account
                  </p>
                  <h2 className="mt-3 text-2xl font-semibold text-white">
                    {user.username || "Unnamed user"}
                  </h2>
                  <p className="mt-2 text-sm text-white/60">{user.email}</p>
                </div>

                <div className="rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-3 text-right">
                  <p className="text-xs uppercase tracking-[0.3em] text-white/45">
                    Wallets
                  </p>
                  <p className="mt-2 text-2xl font-semibold text-white">
                    {user.wallets.length}
                  </p>
                </div>
              </div>

              <form className="mt-8 grid gap-4" onSubmit={handleCreateWallet}>
                <div>
                  <label className="text-xs uppercase tracking-[0.3em] text-white/45">
                    Wallet name
                  </label>
                  <input
                    className="mt-2 w-full rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                    onChange={(event) => setWalletName(event.target.value)}
                    placeholder="Binance"
                    type="text"
                    value={walletName}
                  />
                </div>

                <div>
                  <label className="text-xs uppercase tracking-[0.3em] text-white/45">
                    Wallet address
                  </label>
                  <input
                    className="mt-2 w-full rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                    onChange={(event) => setWalletAddress(event.target.value)}
                    placeholder="0x..."
                    type="text"
                    value={walletAddress}
                  />
                </div>

                <div>
                  <label className="text-xs uppercase tracking-[0.3em] text-white/45">
                    Chain
                  </label>
                  <select
                    className="mt-2 w-full rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                    onChange={(event) => setWalletChain(event.target.value)}
                    value={walletChain}
                  >
                    {walletChainOptions.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </div>

                {walletFormErrorMessage ? (
                  <p className="text-sm text-rose-300">{walletFormErrorMessage}</p>
                ) : null}

                <button
                  className="rounded-2xl bg-[#00F5C8] px-4 py-3 text-sm font-bold uppercase tracking-[0.3em] text-black transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
                  disabled={isCreatingWallet}
                  type="submit"
                >
                  {isCreatingWallet ? "Validating..." : "Add wallet"}
                </button>
              </form>
            </DashboardCard>

            <DashboardCard>
              <h2 className="text-lg font-semibold text-white">Wallet list</h2>
              <p className="mt-2 text-sm text-white/60">
                Click a wallet to load its snapshot history and capture a fresh Moralis-based
                balance.
              </p>

              {user.wallets.length === 0 ? (
                <p className="mt-8 text-white/70">No wallets have been added yet.</p>
              ) : (
                <div className="mt-6 grid gap-3">
                  {user.wallets.map((wallet) => {
                    const latestSnapshot = wallet.snapshots[0] ?? null;
                    const isSelected = wallet.id === selectedWalletId;

                    return (
                      <button
                        className={[
                          "rounded-3xl border px-5 py-4 text-left transition",
                          isSelected
                            ? "border-[#00F5C8]/60 bg-[#00F5C8]/10"
                            : "border-white/10 bg-white/[0.03] hover:border-white/20",
                        ].join(" ")}
                        key={wallet.id}
                        onClick={() => setSelectedWalletId(wallet.id)}
                        type="button"
                      >
                        <div className="flex items-start justify-between gap-4">
                          <div>
                            <div className="flex flex-wrap items-center gap-3">
                              <p className="text-lg font-semibold text-white">{wallet.name}</p>
                              <span className="rounded-full border border-[#00F5C8]/30 bg-[#00F5C8]/10 px-3 py-1 text-xs uppercase tracking-[0.25em] text-[#00F5C8]">
                                {wallet.chain}
                              </span>
                            </div>
                            <p className="mt-1 break-all text-sm text-white/55">
                              {wallet.address}
                            </p>
                          </div>

                          <div className="rounded-2xl border border-white/10 bg-[#020617]/70 px-3 py-2 text-right">
                            <p className="text-xs uppercase tracking-[0.25em] text-white/45">
                              Snapshots
                            </p>
                            <p className="mt-1 text-lg font-semibold text-white">
                              {wallet.snapshots.length}
                            </p>
                          </div>
                        </div>

                        <div className="mt-4 flex flex-wrap items-center justify-between gap-3 text-sm text-white/60">
                          <span>
                            Latest value{" "}
                            {latestSnapshot
                              ? formatCurrency(latestSnapshot.totalValue)
                              : "N/A"}
                          </span>
                          <span>
                            {latestSnapshot
                              ? formatDateTime(latestSnapshot.timestamp)
                              : "No history yet"}
                          </span>
                        </div>
                      </button>
                    );
                  })}
                </div>
              )}
            </DashboardCard>
          </div>

          {selectedWallet ? (
            <div className="grid gap-6 xl:grid-cols-[minmax(320px,0.8fr)_minmax(0,1.2fr)]">
              <DashboardCard>
                <div className="flex items-start justify-between gap-4">
                  <div>
                    <p className="text-xs uppercase tracking-[0.35em] text-[#00F5C8]/80">
                      Selected wallet
                    </p>
                    <h2 className="mt-3 text-2xl font-semibold text-white">
                      {selectedWallet.name}
                    </h2>
                    <div className="mt-3 flex flex-wrap items-center gap-3">
                      <span className="rounded-full border border-[#00F5C8]/30 bg-[#00F5C8]/10 px-3 py-1 text-xs uppercase tracking-[0.25em] text-[#00F5C8]">
                        {selectedWallet.chain}
                      </span>
                      <span className="break-all text-sm text-white/60">
                        {selectedWallet.address}
                      </span>
                    </div>
                  </div>

                  <button
                    className="rounded-2xl border border-rose-400/30 bg-rose-500/10 px-4 py-3 text-xs font-semibold uppercase tracking-[0.3em] text-rose-200 transition hover:bg-rose-500/15 disabled:cursor-not-allowed disabled:opacity-60"
                    disabled={isDeletingWalletId === selectedWallet.id}
                    onClick={() => handleDeleteWallet(selectedWallet)}
                    type="button"
                  >
                    {isDeletingWalletId === selectedWallet.id ? "Deleting..." : "Delete wallet"}
                  </button>
                </div>

                <form className="mt-8 grid gap-4" onSubmit={handleAddSnapshot}>
                  <div className="rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-4 text-sm text-white/70">
                    Snapshot capture calls Moralis for this wallet and chain, then prices the
                    returned assets in USD automatically.
                  </div>

                  <div>
                    <label className="text-xs uppercase tracking-[0.3em] text-white/45">
                      Notes
                    </label>
                    <textarea
                      className="mt-2 min-h-28 w-full rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                      maxLength={500}
                      onChange={(event) => setSnapshotNotes(event.target.value)}
                      placeholder="Month-end capture"
                      value={snapshotNotes}
                    />
                  </div>

                  {snapshotFormErrorMessage ? (
                    <p className="text-sm text-rose-300">{snapshotFormErrorMessage}</p>
                  ) : null}

                  <button
                    className="rounded-2xl bg-[#00F5C8] px-4 py-3 text-sm font-bold uppercase tracking-[0.3em] text-black transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
                    disabled={isAddingSnapshot}
                    type="submit"
                  >
                    {isAddingSnapshot ? "Capturing..." : "Capture snapshot"}
                  </button>
                </form>
              </DashboardCard>

              <DashboardCard>
                <div className="flex items-center justify-between gap-4">
                  <div>
                    <h2 className="text-lg font-semibold text-white">Snapshot history</h2>
                    <p className="mt-2 text-sm text-white/60">
                      Entries are ordered by timestamp descending for the selected wallet.
                    </p>
                  </div>

                  <div className="rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-3 text-right">
                    <p className="text-xs uppercase tracking-[0.3em] text-white/45">
                      Entries
                    </p>
                    <p className="mt-2 text-2xl font-semibold text-white">
                      {selectedWalletSnapshots.length}
                    </p>
                  </div>
                </div>

                {isSnapshotsLoading ? (
                  <p className="mt-8 text-white/70">Loading snapshots...</p>
                ) : selectedWalletSnapshots.length === 0 ? (
                  <p className="mt-8 text-white/70">
                    No snapshots exist for this wallet yet.
                  </p>
                ) : (
                  <div className="mt-6 grid gap-3">
                    {selectedWalletSnapshots.map((snapshot) => (
                      <div
                        className="rounded-3xl border border-white/10 bg-white/[0.03] px-5 py-4"
                        key={snapshot.id}
                      >
                        <div className="flex flex-wrap items-center justify-between gap-3">
                          <div>
                            <p className="text-lg font-semibold text-white">
                              {formatCurrency(snapshot.totalValue)}
                            </p>
                            <p className="mt-1 text-sm text-white/60">
                              {formatDateTime(snapshot.timestamp)}
                            </p>
                          </div>

                          <div className="text-right text-sm text-white/60">
                            <div>{snapshot.currency || "USD"}</div>
                            <div className="mt-1 uppercase tracking-[0.25em]">
                              {selectedWallet.chain}
                            </div>
                          </div>
                        </div>

                        {snapshot.notes ? (
                          <p className="mt-4 text-sm leading-6 text-white/70">
                            {snapshot.notes}
                          </p>
                        ) : null}
                      </div>
                    ))}
                  </div>
                )}
              </DashboardCard>
            </div>
          ) : (
            <DashboardCard>
              <p className="text-white/70">
                Add a wallet to start tracking snapshot history.
              </p>
            </DashboardCard>
          )}
        </div>
      )}
    </div>
  );
}
