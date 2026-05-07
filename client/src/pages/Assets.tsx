import { useEffect, useState } from "react";
import {
  Area,
  AreaChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from "recharts";

import { getApiErrorMessage } from "@/api/client";
import {
  getCryptoAssetHistory,
  getCryptoAssets,
  type CryptoAsset,
  type CryptoAssetPricePoint,
} from "@/api/crypto";
import DashboardCard from "@/components/dashboard-card";
import DashboardPageHeader from "@/components/dashboard-page-header";
import TimeRangeToggle, { type TimeRangeOption } from "@/components/time-range-toggle";
import { formatDateTime } from "@/lib/formatters";

export default function Assets() {
  const [assets, setAssets] = useState<CryptoAsset[]>([]);
  const [selectedAssetId, setSelectedAssetId] = useState("");
  const [priceHistory, setPriceHistory] = useState<CryptoAssetPricePoint[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [isHistoryLoading, setIsHistoryLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState("");
  const [historyErrorMessage, setHistoryErrorMessage] = useState("");
  const [historyRange, setHistoryRange] = useState<TimeRangeOption>("30d");

  useEffect(() => {
    let isMounted = true;

    const run = async () => {
      try {
        const assetsResult = await getCryptoAssets();

        if (!isMounted) {
          return;
        }

        setAssets(assetsResult);
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
    const hasSelectedAsset = assets.some((asset) => asset.assetId === selectedAssetId);

    if (!hasSelectedAsset) {
      setSelectedAssetId(assets[0]?.assetId ?? "");
    }
  }, [assets, selectedAssetId]);

  useEffect(() => {
    let isMounted = true;

    if (!selectedAssetId) {
      setPriceHistory([]);
      setHistoryErrorMessage("");
      setIsHistoryLoading(false);
      return;
    }

    setIsHistoryLoading(true);
    setHistoryErrorMessage("");

    const run = async () => {
      try {
        const historyResult = await getCryptoAssetHistory(selectedAssetId);

        if (!isMounted) {
          return;
        }

        setPriceHistory(historyResult);
      } catch (error) {
        if (!isMounted) {
          return;
        }

        setPriceHistory([]);
        setHistoryErrorMessage(getApiErrorMessage(error));
      } finally {
        if (isMounted) {
          setIsHistoryLoading(false);
        }
      }
    };

    run();

    return () => {
      isMounted = false;
    };
  }, [selectedAssetId]);

  const selectedAsset =
    assets.find((asset) => asset.assetId === selectedAssetId) ?? null;
  const thirtyDaysAgo = Date.now() - 30 * 24 * 60 * 60 * 1000;
  const visiblePriceHistory =
    historyRange === "30d"
      ? priceHistory.filter((point) => {
          return new Date(point.timestamp).getTime() >= thirtyDaysAgo;
        })
      : priceHistory;

  return (
    <div className="mx-auto max-w-7xl px-6">
      <DashboardPageHeader
        eyebrow="Assets"
        title="Supported cryptocurrencies with live prices"
        description="Current prices come from the latest stored backend snapshot, and the chart below now defaults to the last 30 days so recent movement is easier to read."
      />

      {isLoading ? (
        <DashboardCard>
          <p className="text-white/70">Loading live prices...</p>
        </DashboardCard>
      ) : errorMessage ? (
        <DashboardCard>
          <p className="text-sm text-rose-300">{errorMessage}</p>
        </DashboardCard>
      ) : assets.length === 0 ? (
        <DashboardCard>
          <p className="text-white/70">
            No supported crypto assets are available right now.
          </p>
        </DashboardCard>
      ) : (
        <div className="space-y-6">
          <DashboardCard>
            <div className="flex flex-wrap items-start justify-between gap-4">
              <div>
                <h2 className="text-lg font-semibold text-white">Stored price history</h2>
                <p className="mt-1 text-sm text-white/60">
                  The backend appends CoinGecko snapshots to the database on an interval,
                  and this chart reads those stored points.
                </p>
              </div>

              <label className="block text-sm text-white/65">
                <span className="text-xs uppercase tracking-[0.3em] text-white/45">Asset</span>
                <select
                  aria-label="Select asset price history"
                  className="mt-2 min-w-[220px] rounded-2xl border border-white/10 bg-[#020617]/80 px-4 py-3 text-white outline-none transition focus:border-[#00F5C8]"
                  onChange={(event) => setSelectedAssetId(event.target.value)}
                  value={selectedAssetId}
                >
                  {assets.map((asset) => (
                    <option key={asset.assetId} value={asset.assetId}>
                      {asset.symbol} · {asset.name}
                    </option>
                  ))}
                </select>
              </label>
            </div>

            {selectedAsset ? (
              <div className="mt-6 flex flex-wrap items-center justify-between gap-4">
                <div className="flex flex-wrap items-center gap-x-6 gap-y-2 text-sm text-white/60">
                  <span className="font-medium text-white">{selectedAsset.name}</span>
                  <span className="uppercase tracking-[0.25em] text-[#00F5C8]">
                    {selectedAsset.symbol}
                  </span>
                  <span>Latest stored price {formatAssetPrice(selectedAsset.currentPrice)}</span>
                </div>

                <TimeRangeToggle onChange={setHistoryRange} value={historyRange} />
              </div>
            ) : null}

            {isHistoryLoading ? (
              <div className="mt-8 text-white/70">Loading stored price history...</div>
            ) : historyErrorMessage ? (
              <div className="mt-8 text-sm text-rose-300">{historyErrorMessage}</div>
            ) : visiblePriceHistory.length === 0 ? (
              <div className="mt-8 text-sm text-white/70">
                {historyRange === "30d"
                  ? "No stored price history exists for this asset in the last 30 days yet."
                  : "No stored price history exists for this asset yet."}
              </div>
            ) : (
              <div className="mt-8 h-[320px] min-w-0">
                <ResponsiveContainer height="100%" minWidth={0} width="100%">
                  <AreaChart data={visiblePriceHistory}>
                    <defs>
                      <linearGradient id="assetPriceHistoryFill" x1="0" x2="0" y1="0" y2="1">
                        <stop offset="0%" stopColor="#00F5C8" stopOpacity={0.4} />
                        <stop offset="100%" stopColor="#00F5C8" stopOpacity={0.05} />
                      </linearGradient>
                    </defs>
                    <CartesianGrid stroke="rgba(255,255,255,0.08)" vertical={false} />
                    <XAxis
                      dataKey="timestamp"
                      minTickGap={36}
                      stroke="rgba(255,255,255,0.35)"
                      tickFormatter={formatHistoryTick}
                    />
                    <YAxis
                      stroke="rgba(255,255,255,0.35)"
                      tickFormatter={(value) => formatAssetPrice(Number(value))}
                    />
                    <Tooltip
                      contentStyle={{
                        background: "#020617",
                        border: "1px solid rgba(255,255,255,0.12)",
                        borderRadius: "18px",
                      }}
                      formatter={(value) => formatAssetPrice(Number(value))}
                      labelFormatter={(value) => formatDateTime(String(value))}
                    />
                    <Area
                      dataKey="currentPrice"
                      fill="url(#assetPriceHistoryFill)"
                      stroke="#00F5C8"
                      strokeWidth={2}
                      type="monotone"
                    />
                  </AreaChart>
                </ResponsiveContainer>
              </div>
            )}
          </DashboardCard>

          <DashboardCard className="overflow-hidden p-0">
            <div className="flex flex-wrap items-center justify-between gap-4 border-b border-white/10 px-6 py-5">
              <div>
                <h2 className="text-lg font-semibold text-white">Live market prices</h2>
                <p className="mt-1 text-sm text-white/60">
                  Latest database-backed snapshot for the supported asset set.
                </p>
              </div>
              <div className="rounded-full border border-white/10 bg-white/[0.03] px-4 py-2 text-xs uppercase tracking-[0.3em] text-white/55">
                {assets.length} tracked assets
              </div>
            </div>

            <div className="overflow-x-auto">
              <table className="min-w-full divide-y divide-white/10">
                <thead className="bg-white/[0.03]">
                  <tr className="text-left text-xs uppercase tracking-[0.3em] text-white/45">
                    <th className="px-6 py-4">Name</th>
                    <th className="px-6 py-4">Symbol</th>
                    <th className="px-6 py-4 text-right">Current price</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-white/10">
                  {assets.map((asset) => (
                    <tr
                      className="transition-colors duration-200 hover:bg-white/[0.03]"
                      key={asset.assetId}
                    >
                      <td className="px-6 py-5">
                        <div className="font-medium text-white">{asset.name}</div>
                      </td>
                      <td className="px-6 py-5 text-sm uppercase tracking-[0.25em] text-[#00F5C8]">
                        {asset.symbol}
                      </td>
                      <td className="px-6 py-5 text-right text-sm text-white/80">
                        {formatAssetPrice(asset.currentPrice)}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </DashboardCard>
        </div>
      )}
    </div>
  );
}

function formatHistoryTick(value: string): string {
  return new Date(value).toLocaleDateString("en-US", {
    day: "numeric",
    month: "short",
  });
}

function formatAssetPrice(value: number): string {
  const absoluteValue = Math.abs(value);
  const maximumFractionDigits =
    absoluteValue >= 1000 ? 2 : absoluteValue >= 1 ? 4 : 6;
  const minimumFractionDigits = absoluteValue >= 1 ? 2 : 4;

  return new Intl.NumberFormat("en-US", {
    currency: "USD",
    maximumFractionDigits,
    minimumFractionDigits,
    style: "currency",
  }).format(value);
}
