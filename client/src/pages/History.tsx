import { useEffect, useState } from "react";

import { refresh } from "@/api/auth";
import { getApiErrorMessage } from "@/api/client";
import {
  getUser,
  getWalletSnapshots,
  type UserView,
  type WalletSnapshotItemView,
  type WalletView,
} from "@/api/wallets";
import DashboardCard from "@/components/dashboard-card";
import DashboardPageHeader from "@/components/dashboard-page-header";
import TimeRangeToggle, { type TimeRangeOption } from "@/components/time-range-toggle";
import { formatCurrency, formatDateTime, formatPercent } from "@/lib/formatters";

const thirtyDaysInMilliseconds = 30 * 24 * 60 * 60 * 1000;

export default function History() {
  const [user, setUser] = useState<UserView | null>(null);
  const [selectedWalletId, setSelectedWalletId] = useState<number | null>(null);
  const [snapshots, setSnapshots] = useState<WalletSnapshotItemView[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isSnapshotsLoading, setIsSnapshotsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [range, setRange] = useState<TimeRangeOption>("30d");

  const selectedWallet =
    user?.wallets.find((wallet) => wallet.id === selectedWalletId) ?? null;
  const visibleSnapshots =
    range === "30d"
      ? snapshots.filter((snapshot) => {
          return (
            Date.now() - new Date(snapshot.timestamp).getTime() <=
            thirtyDaysInMilliseconds
          );
        })
      : snapshots;
  const latestSnapshot = visibleSnapshots[0] ?? null;
  const oldestSnapshot = visibleSnapshots[visibleSnapshots.length - 1] ?? null;
  const rangeChangeValue =
    latestSnapshot && oldestSnapshot
      ? latestSnapshot.totalValue - oldestSnapshot.totalValue
      : 0;
  const rangeChangePercentage =
    oldestSnapshot && oldestSnapshot.totalValue > 0
      ? (rangeChangeValue / oldestSnapshot.totalValue) * 100
      : 0;

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
          setUser(null);
          setSelectedWalletId(null);
          setSnapshots([]);
          setErrorMessage("Your session has expired. Please log in again.");
          return;
        }

        const nextUser = await getUser(session.id);

        if (!isMounted) {
          return;
        }

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
      setSnapshots([]);
      setIsSnapshotsLoading(false);
      return;
    }

    const run = async () => {
      setIsSnapshotsLoading(true);
      setErrorMessage("");

      try {
        const nextSnapshots = await getWalletSnapshots(selectedWalletId);

        if (!isMounted) {
          return;
        }

        setSnapshots(nextSnapshots);
      } catch (error) {
        if (!isMounted) {
          return;
        }

        setErrorMessage(getApiErrorMessage(error));
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

  return (
    <div className="mx-auto max-w-7xl px-6">
      <DashboardPageHeader
        eyebrow="History"
        title="Wallet snapshots with a 30-day default lens"
        description="Select one of your wallets and inspect its stored value history. The timeline starts with the last 30 days, while the full archive stays one toggle away."
      />

      {isLoading ? (
        <DashboardCard>
          <p className="text-white/70">Loading snapshot history...</p>
        </DashboardCard>
      ) : errorMessage ? (
        <DashboardCard>
          <p className="text-sm text-rose-300">{errorMessage}</p>
        </DashboardCard>
      ) : !user || user.wallets.length === 0 ? (
        <DashboardCard>
          <p className="text-white/70">
            Add a wallet on the Wallet page to start building history.
          </p>
        </DashboardCard>
      ) : (
        <div className="space-y-6">
          <DashboardCard>
            <div className="flex flex-wrap items-start justify-between gap-5">
              <div>
                <h2 className="text-lg font-semibold text-white">Timeline window</h2>
                <p className="mt-2 max-w-2xl text-sm text-white/60">
                  Pick a wallet and focus on the last month first or switch to the
                  full stored archive.
                </p>
              </div>

              <TimeRangeToggle onChange={setRange} value={range} />
            </div>

            <div className="mt-6 grid gap-3 md:grid-cols-3">
              {user.wallets.map((wallet) => (
                <WalletHistorySelector
                  isSelected={wallet.id === selectedWalletId}
                  key={wallet.id}
                  onClick={() => setSelectedWalletId(wallet.id)}
                  wallet={wallet}
                />
              ))}
            </div>

            {selectedWallet ? (
              <div className="mt-6 grid gap-4 md:grid-cols-3">
                <HistoryMetricCard
                  label="Snapshots in range"
                  value={String(visibleSnapshots.length)}
                  secondary={range === "30d" ? "Last 30 days" : "All stored history"}
                />
                <HistoryMetricCard
                  label="Latest value"
                  value={latestSnapshot ? formatCurrency(latestSnapshot.totalValue) : "N/A"}
                  secondary={
                    latestSnapshot
                      ? formatDateTime(latestSnapshot.timestamp)
                      : "No snapshots yet"
                  }
                />
                <HistoryMetricCard
                  label="Range change"
                  value={formatCurrency(rangeChangeValue)}
                  secondary={formatPercent(rangeChangePercentage)}
                  tone={rangeChangeValue}
                />
              </div>
            ) : null}
          </DashboardCard>

          {!selectedWallet ? (
            <DashboardCard>
              <p className="text-white/70">Select a wallet to inspect its history.</p>
            </DashboardCard>
          ) : isSnapshotsLoading ? (
            <DashboardCard>
              <p className="text-white/70">Loading wallet snapshots...</p>
            </DashboardCard>
          ) : visibleSnapshots.length === 0 ? (
            <DashboardCard>
              <p className="text-white/70">
                {range === "30d"
                  ? "No snapshots were stored for this wallet in the last 30 days."
                  : "No snapshots exist yet for this wallet."}
              </p>
            </DashboardCard>
          ) : (
            visibleSnapshots.map((snapshot) => (
              <DashboardCard key={snapshot.id}>
                <div className="flex flex-wrap items-start justify-between gap-4">
                  <div>
                    <h2 className="text-lg font-semibold text-white">
                      {formatDateTime(snapshot.timestamp)}
                    </h2>
                    <p className="mt-1 text-sm text-white/60">
                      {selectedWallet.name} · {selectedWallet.chain} · {selectedWallet.address}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-xs uppercase tracking-[0.3em] text-white/45">
                      Total value
                    </p>
                    <p className="mt-2 text-lg font-semibold text-[#00F5C8]">
                      {formatCurrency(snapshot.totalValue)}
                    </p>
                    <p className="mt-1 text-sm text-white/60">
                      {snapshot.currency || "No currency label"}
                    </p>
                  </div>
                </div>

                {snapshot.notes ? (
                  <p className="mt-4 rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-4 text-sm leading-7 text-white/70">
                    {snapshot.notes}
                  </p>
                ) : null}
              </DashboardCard>
            ))
          )}
        </div>
      )}
    </div>
  );
}

interface WalletHistorySelectorProps {
  isSelected: boolean;
  onClick: () => void;
  wallet: WalletView;
}

function WalletHistorySelector({
  isSelected,
  onClick,
  wallet,
}: WalletHistorySelectorProps) {
  return (
    <button
      className={[
        "rounded-3xl border px-5 py-4 text-left transition",
        isSelected
          ? "border-[#00F5C8]/60 bg-[#00F5C8]/10"
          : "border-white/10 bg-white/[0.03] hover:border-white/20",
      ].join(" ")}
      onClick={onClick}
      type="button"
    >
      <p className="text-lg font-semibold text-white">{wallet.name}</p>
      <p className="mt-2 text-xs uppercase tracking-[0.25em] text-[#00F5C8]">
        {wallet.chain}
      </p>
      <p className="mt-1 break-all text-sm text-white/55">{wallet.address}</p>
      <p className="mt-4 text-xs uppercase tracking-[0.25em] text-white/45">
        {wallet.snapshots.length} stored snapshots
      </p>
    </button>
  );
}

interface HistoryMetricCardProps {
  label: string;
  value: string;
  secondary: string;
  tone?: number;
}

function HistoryMetricCard({
  label,
  value,
  secondary,
  tone = 0,
}: HistoryMetricCardProps) {
  const toneClassName =
    tone > 0 ? "text-emerald-300" : tone < 0 ? "text-rose-300" : "text-white";

  return (
    <div className="rounded-3xl border border-white/10 bg-white/[0.03] p-5">
      <p className="text-xs uppercase tracking-[0.35em] text-white/45">{label}</p>
      <p className={`mt-4 text-2xl font-semibold ${toneClassName}`}>{value}</p>
      <p className="mt-2 text-sm text-white/60">{secondary}</p>
    </div>
  );
}
