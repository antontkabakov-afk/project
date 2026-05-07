import { useEffect, useState } from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  Cell,
  Pie,
  PieChart,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import { refresh } from "@/api/auth";
import { getApiErrorMessage } from "@/api/client";
import DashboardCard from "@/components/dashboard-card";
import DashboardPageHeader from "@/components/dashboard-page-header";
import TimeRangeToggle, { type TimeRangeOption } from "@/components/time-range-toggle";
import {
  getPortfolioStatistics,
  type AssetPerformance,
  type PortfolioStatistics,
} from "@/api/portfolio";
import { getUser, type UserView } from "@/api/wallets";
import { formatAmount, formatCurrency, formatPercent } from "@/lib/formatters";

const chartColors = [
  "#00F5C8",
  "#38BDF8",
  "#F59E0B",
  "#F97316",
  "#A3E635",
  "#FB7185",
  "#818CF8",
  "#2DD4BF",
];

export default function Statistics() {
  const [user, setUser] = useState<UserView | null>(null);
  const [statistics, setStatistics] = useState<PortfolioStatistics | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState("");
  const [range, setRange] = useState<TimeRangeOption>("30d");
  const [selectedWalletScope, setSelectedWalletScope] = useState("all");

  const selectedRangeDays = range === "30d" ? 30 : undefined;
  const selectedWalletId =
    selectedWalletScope === "all" ? undefined : Number(selectedWalletScope);
  const selectedWallet =
    user?.wallets.find((wallet) => wallet.id === selectedWalletId) ?? null;
  const performance = statistics?.performance;

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
          setStatistics(null);
          setErrorMessage("Your session has expired. Please log in again.");
          return;
        }

        const nextUser = await getUser(session.id);

        if (!isMounted) {
          return;
        }

        setUser(nextUser);
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

    if (!user) {
      setStatistics(null);
      return;
    }

    const run = async () => {
      setIsLoading(true);
      setErrorMessage("");

      try {
        const result = await getPortfolioStatistics(selectedRangeDays, selectedWalletId);

        if (!isMounted) {
          return;
        }

        setStatistics(result);
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
  }, [selectedRangeDays, selectedWalletId, user]);

  return (
    <div className="mx-auto max-w-7xl px-6">
      <DashboardPageHeader
        eyebrow="Statistics"
        title="Compare the full account or one wallet at a time"
        description="Switch between all wallets and an individual chain-specific wallet, then inspect stored performance, distribution, and movers for the selected window."
      />

      {isLoading ? (
        <DashboardCard>
          <p className="text-white/70">Calculating portfolio statistics...</p>
        </DashboardCard>
      ) : errorMessage ? (
        <DashboardCard>
          <p className="text-sm text-rose-300">{errorMessage}</p>
        </DashboardCard>
      ) : !user || user.wallets.length === 0 ? (
        <DashboardCard>
          <p className="text-white/70">Add a wallet and capture snapshots to unlock statistics.</p>
        </DashboardCard>
      ) : !statistics || !performance ? (
        <DashboardCard>
          <p className="text-white/70">No statistics are available yet.</p>
        </DashboardCard>
      ) : (
        <div className="space-y-6">
          <DashboardCard>
            <div className="flex flex-wrap items-start justify-between gap-5">
              <div>
                <h2 className="text-lg font-semibold text-white">Statistics window</h2>
                <p className="mt-2 max-w-2xl text-sm text-white/60">
                  Scope the data to the entire account or a single wallet. The 30-day view
                  stays the default lens for faster recent comparisons.
                </p>
              </div>

              <div className="flex flex-wrap items-end gap-3">
                <label className="block text-sm text-white/65">
                  <span className="text-xs uppercase tracking-[0.3em] text-white/45">
                    Scope
                  </span>
                  <select
                    className="mt-2 min-w-[240px] rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                    onChange={(event) => setSelectedWalletScope(event.target.value)}
                    value={selectedWalletScope}
                  >
                    <option value="all">All wallets</option>
                    {user.wallets.map((wallet) => (
                      <option key={wallet.id} value={String(wallet.id)}>
                        {wallet.name} · {wallet.chain}
                      </option>
                    ))}
                  </select>
                </label>

                <TimeRangeToggle onChange={setRange} value={range} />
              </div>
            </div>
          </DashboardCard>

          <div className="grid gap-6 md:grid-cols-3">
            <MetricCard
              label={range === "30d" ? "30-day change" : "Change since first snapshot"}
              tone={performance.changeValueUsd}
              value={formatCurrency(performance.changeValueUsd)}
              secondary={selectedWallet ? selectedWallet.name : "All wallets"}
            />
            <MetricCard
              label="Current value"
              tone={performance.currentValueUsd}
              value={formatCurrency(performance.currentValueUsd)}
              secondary={selectedWallet ? `${selectedWallet.name} · ${selectedWallet.chain}` : "Latest account snapshot"}
            />
            <MetricCard
              label="Starting value"
              tone={performance.startingValueUsd}
              value={formatCurrency(performance.startingValueUsd)}
              secondary={
                range === "30d" ? "Oldest visible point in range" : "First stored snapshot"
              }
            />
          </div>

          <DashboardCard>
            <div className="flex flex-wrap items-center justify-between gap-4">
              <div>
                <h2 className="text-lg font-semibold text-white">
                  {selectedWallet ? `${selectedWallet.name} history` : "Portfolio history"}
                </h2>
                <p className="mt-2 text-sm text-white/60">
                  The chart uses stored wallet snapshots and aggregates them into the selected scope.
                </p>
              </div>
              <div className="text-right text-sm text-white/60">
                <div>
                  Started:{" "}
                  {performance.startTimestampUtc
                    ? new Date(performance.startTimestampUtc).toLocaleString()
                    : "N/A"}
                </div>
                <div className="mt-1">
                  Latest:{" "}
                  {performance.endTimestampUtc
                    ? new Date(performance.endTimestampUtc).toLocaleString()
                    : "N/A"}
                </div>
              </div>
            </div>

            {statistics.history.length === 0 ? (
              <p className="mt-10 text-sm text-white/70">
                {range === "30d"
                  ? "No snapshots were captured in the last 30 days yet."
                  : "Capture snapshots to populate the historical performance chart."}
              </p>
            ) : (
              <div className="mt-8 h-[320px] min-w-0">
                <ResponsiveContainer height="100%" minWidth={0} width="100%">
                  <AreaChart data={statistics.history}>
                    <defs>
                      <linearGradient id="portfolioHistoryFill" x1="0" x2="0" y1="0" y2="1">
                        <stop offset="0%" stopColor="#00F5C8" stopOpacity={0.45} />
                        <stop offset="100%" stopColor="#00F5C8" stopOpacity={0.05} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid stroke="rgba(255,255,255,0.08)" vertical={false} />
                    <XAxis
                      dataKey="timestamp"
                      minTickGap={36}
                      stroke="rgba(255,255,255,0.35)"
                      tickFormatter={(value) =>
                        new Date(value).toLocaleDateString("en-US", {
                          month: "short",
                          day: "numeric",
                        })
                      }
                    />
                    <YAxis
                      stroke="rgba(255,255,255,0.35)"
                      tickFormatter={(value) => formatCurrency(Number(value))}
                    />
                    <Tooltip
                      contentStyle={{
                        background: "#020617",
                        border: "1px solid rgba(255,255,255,0.12)",
                        borderRadius: "18px",
                      }}
                      formatter={(value) => formatCurrency(Number(value))}
                      labelFormatter={(value) => new Date(value).toLocaleString()}
                    />
                    <Area
                      dataKey="totalValueUsd"
                      fill="url(#portfolioHistoryFill)"
                      stroke="#00F5C8"
                      strokeWidth={2}
                      type="monotone"
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            )}
          </DashboardCard>

          <div className="grid gap-6 xl:grid-cols-[minmax(0,1.35fr)_minmax(320px,0.65fr)]">
            <DashboardCard className="grid gap-6 lg:grid-cols-[minmax(0,320px)_minmax(0,1fr)]">
              <div>
                <h2 className="text-lg font-semibold text-white">Asset distribution</h2>
                <p className="mt-2 text-sm text-white/60">
                  Allocation comes from the latest snapshot inside the selected scope and time window.
                </p>

                {statistics.distribution.length === 0 ? (
                  <p className="mt-10 text-sm text-white/70">
                    No active holdings are available in the latest snapshot.
                  </p>
                ) : (
                  <div className="mt-8 h-[280px] min-w-0">
                    <ResponsiveContainer height="100%" minWidth={0} width="100%">
                      <PieChart>
                        <Pie
                          data={statistics.distribution}
                          dataKey="currentValue"
                          innerRadius={70}
                          nameKey="assetSymbol"
                          outerRadius={105}
                          paddingAngle={4}
                        >
                          {statistics.distribution.map((asset, index) => (
                            <Cell
                              fill={chartColors[index % chartColors.length]}
                              key={`${asset.assetSymbol}-${index}`}
                            />
                          ))}
                        </Pie>
                        <Tooltip
                          formatter={(value) =>
                            formatCurrency(
                              Number(Array.isArray(value) ? value[0] : value ?? 0),
                            )
                          }
                        />
                      </PieChart>
                    </ResponsiveContainer>
                  </div>
                )}
              </div>

              <div className="space-y-3">
                {statistics.distribution.map((asset, index) => (
                  <div
                    className="flex items-center justify-between rounded-2xl border border-white/10 bg-white/[0.03] px-4 py-4"
                    key={`${asset.assetSymbol}-${index}`}
                  >
                    <div className="flex items-center gap-3">
                      <span
                        className="h-3 w-3 rounded-full"
                        style={{
                          backgroundColor: chartColors[index % chartColors.length],
                        }}
                      />
                      <div>
                        <p className="font-medium text-white">{asset.assetName}</p>
                        <p className="mt-1 text-xs uppercase tracking-[0.25em] text-white/45">
                          {asset.assetSymbol}
                        </p>
                      </div>
                    </div>

                    <div className="text-right">
                      <p className="font-medium text-white">
                        {formatPercent(asset.percentage)}
                      </p>
                      <p className="mt-1 text-sm text-white/60">
                        {formatCurrency(asset.currentValue)}
                      </p>
                    </div>
                  </div>
                ))}
              </div>
            </DashboardCard>

            <div className="grid gap-6">
              <PerformanceCard
                asset={statistics.bestAsset}
                label={range === "30d" ? "Best asset in 30 days" : "Best asset over time"}
                emptyLabel="No asset history yet."
              />
              <PerformanceCard
                asset={statistics.worstAsset}
                label={range === "30d" ? "Worst asset in 30 days" : "Worst asset over time"}
                emptyLabel="No asset history yet."
              />
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

interface MetricCardProps {
  label: string;
  value: string;
  secondary: string;
  tone: number;
}

function MetricCard({ label, value, secondary, tone }: MetricCardProps) {
  const toneClassName =
    tone > 0
      ? "text-emerald-300"
      : tone < 0
        ? "text-rose-300"
        : "text-white";

  return (
    <DashboardCard>
      <p className="text-xs uppercase tracking-[0.35em] text-white/45">{label}</p>
      <p className={`mt-4 text-3xl font-semibold ${toneClassName}`}>{value}</p>
      <p className="mt-2 text-sm text-white/60">{secondary}</p>
    </DashboardCard>
  );
}

interface PerformanceCardProps {
  asset: AssetPerformance | null;
  label: string;
  emptyLabel: string;
}

function PerformanceCard({ asset, label, emptyLabel }: PerformanceCardProps) {
  return (
    <DashboardCard>
      <p className="text-xs uppercase tracking-[0.35em] text-white/45">{label}</p>

      {asset ? (
        <div className="mt-4 space-y-4">
          <div>
            <h3 className="text-2xl font-semibold text-white">{asset.assetName}</h3>
            <p className="mt-1 text-xs uppercase tracking-[0.25em] text-[#00F5C8]">
              {asset.assetSymbol}
            </p>
          </div>

          <div className="grid gap-3 rounded-2xl border border-white/10 bg-white/[0.03] p-4">
            <div className="flex items-center justify-between text-sm text-white/70">
              <span>Change</span>
              <span
                className={
                  asset.changeValueUsd >= 0 ? "text-emerald-300" : "text-rose-300"
                }
              >
                {formatCurrency(asset.changeValueUsd)}
              </span>
            </div>
            <div className="flex items-center justify-between text-sm text-white/70">
              <span>Performance</span>
              <span className="text-white">{formatPercent(asset.changePercentage)}</span>
            </div>
            <div className="flex items-center justify-between text-sm text-white/70">
              <span>Holdings</span>
              <span className="text-white">{formatAmount(asset.amountHeld)}</span>
            </div>
            <div className="flex items-center justify-between text-sm text-white/70">
              <span>Current value</span>
              <span className="text-white">{formatCurrency(asset.currentValue)}</span>
            </div>
          </div>
        </div>
      ) : (
        <p className="mt-4 text-sm text-white/70">{emptyLabel}</p>
      )}
    </DashboardCard>
  );
}
