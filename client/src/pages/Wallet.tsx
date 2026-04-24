import { type FormEvent, useEffect, useState } from "react";

import { getApiErrorMessage } from "@/api/client";
import {
  connectWallet,
  createSnapshot,
  getWalletConnection,
  type WalletConnection,
} from "@/api/portfolio";
import DashboardCard from "@/components/dashboard-card";
import DashboardPageHeader from "@/components/dashboard-page-header";
import { formatCurrency, formatDateTime } from "@/lib/formatters";

const supportedChains = [
  { value: "eth", label: "Ethereum" },
  { value: "base", label: "Base" },
  { value: "arbitrum", label: "Arbitrum" },
  { value: "optimism", label: "Optimism" },
  { value: "polygon", label: "Polygon" },
  { value: "bsc", label: "BNB Chain" },
  { value: "avalanche", label: "Avalanche" },
];

export default function Wallet() {
  const [walletConnection, setWalletConnection] = useState<WalletConnection | null>(null);
  const [walletAddress, setWalletAddress] = useState("");
  const [chain, setChain] = useState("eth");
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [isSnapshotting, setIsSnapshotting] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [successMessage, setSuccessMessage] = useState("");

  useEffect(() => {
    let isMounted = true;

    const run = async () => {
      try {
        const result = await getWalletConnection();

        if (!isMounted) {
          return;
        }

        setWalletConnection(result);
        setWalletAddress(result.walletAddress ?? "");
        setChain(result.chain || "eth");
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

  async function handleConnectWallet(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setErrorMessage("");
    setSuccessMessage("");
    setIsSaving(true);

    try {
      const result = await connectWallet({
        walletAddress,
        chain,
      });

      setWalletConnection(result);
      setWalletAddress(result.walletAddress ?? walletAddress);
      setChain(result.chain);
      setSuccessMessage("Wallet connected and initial snapshot captured.");
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    } finally {
      setIsSaving(false);
    }
  }

  async function handleCreateSnapshot() {
    setErrorMessage("");
    setSuccessMessage("");
    setIsSnapshotting(true);

    try {
      const snapshot = await createSnapshot();

      setWalletConnection((currentState) =>
        currentState
          ? {
              ...currentState,
              lastSnapshotAtUtc: snapshot.timestamp,
              lastSnapshotValueUsd: snapshot.totalValueUsd,
            }
          : currentState,
      );

      setSuccessMessage("Snapshot stored successfully.");
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error));
    } finally {
      setIsSnapshotting(false);
    }
  }

  return (
    <div className="mx-auto max-w-7xl px-6">
      <DashboardPageHeader
        eyebrow="Wallet"
        title="Track a live wallet with stored snapshots"
        description="Connect one wallet address per account, let the backend hydrate holdings from Moralis, and persist immutable snapshots that power the rest of the dashboard from the database."
      />

      <div className="grid gap-6 lg:grid-cols-[minmax(0,420px)_minmax(0,1fr)]">
        <DashboardCard>
          <h2 className="text-lg font-semibold text-white">Wallet connection</h2>
          <p className="mt-2 text-sm text-white/60">
            The backend validates the address, stores the active chain, and captures
            an initial snapshot immediately after connection.
          </p>

          <form className="mt-6 space-y-4" onSubmit={handleConnectWallet}>
            <label className="block text-sm text-white/65">
              Wallet address
              <input
                className="mt-2 w-full rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                disabled={isLoading || isSaving}
                onChange={(event) => setWalletAddress(event.target.value)}
                placeholder="0x..."
                type="text"
                value={walletAddress}
              />
            </label>

            <label className="block text-sm text-white/65">
              Chain
              <select
                className="mt-2 w-full rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                disabled={isLoading || isSaving}
                onChange={(event) => setChain(event.target.value)}
                value={chain}
              >
                {supportedChains.map((supportedChain) => (
                  <option key={supportedChain.value} value={supportedChain.value}>
                    {supportedChain.label}
                  </option>
                ))}
              </select>
            </label>

            <button
              className="w-full rounded-2xl bg-[#00F5C8] px-4 py-3 font-bold uppercase tracking-[0.35em] text-black transition hover:brightness-110 disabled:cursor-not-allowed disabled:opacity-60"
              disabled={isLoading || isSaving || !walletAddress.trim()}
              type="submit"
            >
              {isSaving ? "Connecting..." : "Connect wallet"}
            </button>
          </form>

          {errorMessage ? (
            <p className="mt-4 text-sm text-rose-300">{errorMessage}</p>
          ) : null}

          {successMessage ? (
            <p className="mt-4 text-sm text-[#00F5C8]">{successMessage}</p>
          ) : null}
        </DashboardCard>

        <DashboardCard>
          <div className="flex flex-wrap items-start justify-between gap-4">
            <div>
              <h2 className="text-lg font-semibold text-white">Current tracking state</h2>
              <p className="mt-2 text-sm text-white/60">
                History and statistics continue to read from stored snapshots while
                the assets page now shows live CoinGecko pricing from the backend.
              </p>
            </div>

            <button
              className="rounded-full border border-white/10 bg-white/[0.05] px-5 py-3 text-xs font-semibold uppercase tracking-[0.3em] text-white transition hover:border-[#00F5C8]/40 hover:text-[#00F5C8] disabled:cursor-not-allowed disabled:opacity-60"
              disabled={!walletConnection?.isConnected || isSnapshotting}
              onClick={handleCreateSnapshot}
              type="button"
            >
              {isSnapshotting ? "Saving..." : "Create snapshot"}
            </button>
          </div>

          {isLoading ? (
            <div className="mt-10 text-white/70">Loading wallet state...</div>
          ) : !walletConnection?.isConnected ? (
            <div className="mt-10 rounded-3xl border border-dashed border-white/10 bg-white/[0.03] px-6 py-8 text-white/70">
              No wallet is connected yet. Add a valid EVM address to start snapshot-based
              tracking.
            </div>
          ) : (
            <div className="mt-8 grid gap-4 md:grid-cols-3">
              <StateCard
                label="Wallet"
                value={walletConnection.walletAddress ?? "Not connected"}
              />
              <StateCard label="Chain" value={walletConnection.chain.toUpperCase()} />
              <StateCard
                label="Last snapshot value"
                value={
                  walletConnection.lastSnapshotValueUsd === null
                    ? "No data"
                    : formatCurrency(walletConnection.lastSnapshotValueUsd)
                }
              />
              <StateCard
                label="Connected"
                value={
                  walletConnection.connectedAtUtc
                    ? formatDateTime(walletConnection.connectedAtUtc)
                    : "Unknown"
                }
              />
              <StateCard
                label="Latest snapshot"
                value={
                  walletConnection.lastSnapshotAtUtc
                    ? formatDateTime(walletConnection.lastSnapshotAtUtc)
                    : "Not captured"
                }
              />
              <StateCard label="Snapshot mode" value="Append-only history" />
            </div>
          )}
        </DashboardCard>
      </div>
    </div>
  );
}

interface StateCardProps {
  label: string;
  value: string;
}

function StateCard({ label, value }: StateCardProps) {
  return (
    <div className="rounded-3xl border border-white/10 bg-white/[0.03] p-5">
      <p className="text-xs uppercase tracking-[0.3em] text-white/45">{label}</p>
      <p className="mt-3 break-all text-sm font-medium text-white/85">{value}</p>
    </div>
  );
}
