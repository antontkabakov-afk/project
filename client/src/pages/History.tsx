import { useEffect, useState } from "react";

import { getApiErrorMessage } from "@/api/client";
import DashboardCard from "@/components/dashboard-card";
import DashboardPageHeader from "@/components/dashboard-page-header";
import TimeRangeToggle, { type TimeRangeOption } from "@/components/time-range-toggle";
import {
  getSnapshotHistoryForRange,
  getWalletConnection,
  type WalletConnection,
  type WalletSnapshot,
} from "@/api/portfolio";
import {
  formatAmount,
  formatCurrency,
  formatDateTime,
  formatPercent,
} from "@/lib/formatters";

export default function History() {
  const [walletConnection, setWalletConnection] = useState<WalletConnection | null>(null);
  const [snapshots, setSnapshots] = useState<WalletSnapshot[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [range, setRange] = useState<TimeRangeOption>("30d");

  const selectedRangeDays = range === "30d" ? 30 : undefined;
  const latestSnapshot = snapshots[0] ?? null;
  const oldestSnapshot = snapshots[snapshots.length - 1] ?? null;
  const rangeChangeValue =
    latestSnapshot && oldestSnapshot
      ? latestSnapshot.totalValueUsd - oldestSnapshot.totalValueUsd
      : 0;
  const rangeChangePercentage =
    oldestSnapshot && oldestSnapshot.totalValueUsd > 0
      ? (rangeChangeValue / oldestSnapshot.totalValueUsd) * 100
      : 0;

  useEffect(() => {
    let isMounted = true;

    const run = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const [walletResult, snapshotsResult] = await Promise.all([
          getWalletConnection(),
          getSnapshotHistoryForRange(selectedRangeDays),
        ]);

        if (!isMounted) {
          return;
        }

        setWalletConnection(walletResult);
        setSnapshots(snapshotsResult);
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
  }, [selectedRangeDays]);

  return (
    <div className="mx-auto max-w-7xl px-6">
      <DashboardPageHeader
        eyebrow="History"
        title="Snapshot timeline with a 30-day default lens"
        description="Every entry is a stored wallet state from the database. The view now starts with the last month so the timeline is easier to scan, and you can still switch back to the full archive."
      />

      {isLoading ? (
        <DashboardCard>
          <p className="text-white/70">Loading snapshot history...</p>
        </DashboardCard>
      ) : errorMessage ? (
        <DashboardCard>
          <p className="text-sm text-rose-300">{errorMessage}</p>
        </DashboardCard>
      ) : !walletConnection?.isConnected ? (
        <DashboardCard>
          <p className="text-white/70">
            Connect a wallet to start building a snapshot timeline.
          </p>
        </DashboardCard>
      ) : (
        <div className="space-y-6">
          <DashboardCard>
            <div className="flex flex-wrap items-start justify-between gap-5">
              <div>
                <h2 className="text-lg font-semibold text-white">Timeline window</h2>
                <p className="mt-2 max-w-2xl text-sm text-white/60">
                  Focus the snapshot feed on the last month or open the full archive
                  when you want the entire portfolio record.
                </p>
              </div>

              <TimeRangeToggle onChange={setRange} value={range} />
            </div>

            <div className="mt-6 grid gap-4 md:grid-cols-3">
              <HistoryMetricCard
                label="Snapshots in range"
                value={String(snapshots.length)}
                secondary={range === "30d" ? "Last 30 days" : "All stored history"}
              />
              <HistoryMetricCard
                label="Latest value"
                value={latestSnapshot ? formatCurrency(latestSnapshot.totalValueUsd) : "N/A"}
                secondary={
                  latestSnapshot ? formatDateTime(latestSnapshot.timestamp) : "No snapshots yet"
                }
              />
              <HistoryMetricCard
                label="Range change"
                value={formatCurrency(rangeChangeValue)}
                secondary={formatPercent(rangeChangePercentage)}
                tone={rangeChangeValue}
              />
            </div>
          </DashboardCard>

          {snapshots.length === 0 ? (
            <DashboardCard>
              <p className="text-white/70">
                {range === "30d"
                  ? "No snapshots were captured in the last 30 days. Switch to All time or create a fresh snapshot."
                  : "No snapshots exist yet for the connected wallet."}
              </p>
            </DashboardCard>
          ) : (
            snapshots.map((snapshot) => (
              <DashboardCard className="overflow-hidden p-0" key={snapshot.id}>
                <div className="flex flex-wrap items-center justify-between gap-4 border-b border-white/10 px-6 py-5">
                  <div>
                    <h2 className="text-lg font-semibold text-white">
                      {formatDateTime(snapshot.timestamp)}
                    </h2>
                    <p className="mt-1 text-sm text-white/60">
                      {snapshot.walletAddress} on {snapshot.chain.toUpperCase()}
                    </p>
                  </div>
                  <div className="text-right">
                    <p className="text-xs uppercase tracking-[0.3em] text-white/45">
                      Portfolio value
                    </p>
                    <p className="mt-2 text-lg font-semibold text-[#00F5C8]">
                      {formatCurrency(snapshot.totalValueUsd)}
                    </p>
                  </div>
                </div>

                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-white/10">
                    <thead className="bg-white/[0.03]">
                      <tr className="text-left text-xs uppercase tracking-[0.3em] text-white/45">
                        <th className="px-6 py-4">Asset</th>
                        <th className="px-6 py-4 text-right">Balance</th>
                        <th className="px-6 py-4 text-right">Price</th>
                        <th className="px-6 py-4 text-right">Value</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-white/10">
                      {snapshot.assets.map((asset) => (
                        <tr
                          className="transition-colors duration-200 hover:bg-white/[0.03]"
                          key={asset.assetId}
                        >
                          <td className="px-6 py-5">
                            <div className="font-medium text-white">{asset.assetName}</div>
                            <div className="mt-1 text-xs uppercase tracking-[0.25em] text-[#00F5C8]">
                              {asset.assetSymbol}
                            </div>
                          </td>
                          <td className="px-6 py-5 text-right text-sm text-white/80">
                            {formatAmount(asset.amountHeld)}
                          </td>
                          <td className="px-6 py-5 text-right text-sm text-white/80">
                            {formatCurrency(asset.priceUsd)}
                          </td>
                          <td className="px-6 py-5 text-right text-sm text-white">
                            {formatCurrency(asset.currentValue)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              </DashboardCard>
            ))
          )}
        </div>
      )}
    </div>
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
