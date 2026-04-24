export const supportedChains = [
  { value: "eth", label: "Ethereum" },
  { value: "base", label: "Base" },
  { value: "arbitrum", label: "Arbitrum" },
  { value: "optimism", label: "Optimism" },
  { value: "polygon", label: "Polygon" },
  { value: "bsc", label: "BNB Chain" },
  { value: "avalanche", label: "Avalanche" },
] as const;

const chainLabelMap = new Map<string, string>(
  supportedChains.map((supportedChain) => [supportedChain.value, supportedChain.label]),
);

export function getChainLabel(chain: string | null | undefined): string {
  if (!chain) {
    return "Unknown chain";
  }

  return chainLabelMap.get(chain.toLowerCase()) ?? chain.toUpperCase();
}
