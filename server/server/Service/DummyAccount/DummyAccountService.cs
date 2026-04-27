/*
using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using server.Date;
using server.Models;

namespace server.Service.DummyAccount;

public class DummyAccountService : IDummyAccountService
{
    private static readonly Regex WalletAddressRegex = new(
        "^0x[0-9a-fA-F]{40}$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] DefaultChainNames = ["Ethereum", "Polygon", "BSC"];
    private static readonly string[] UsernamePrefixes =
    [
        "amber",
        "brisk",
        "cinder",
        "delta",
        "ember",
        "frost",
        "gold",
        "hyper",
        "ion",
        "lunar",
        "nova",
        "onyx"
    ];
    private static readonly string[] UsernameSuffixes =
    [
        "atlas",
        "badger",
        "comet",
        "falcon",
        "harbor",
        "matrix",
        "otter",
        "quartz",
        "rocket",
        "signal",
        "vector",
        "voyager"
    ];
    private static readonly IReadOnlyDictionary<string, string> KnownChainAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["ethereum"] = "Ethereum",
            ["eth"] = "Ethereum",
            ["polygon"] = "Polygon",
            ["matic"] = "Polygon",
            ["bsc"] = "BSC",
            ["binance smart chain"] = "BSC"
        };

    private readonly AppDbContext _db;
    private readonly ILogger<DummyAccountService> _logger;

    public DummyAccountService(
        AppDbContext db,
        ILogger<DummyAccountService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DummyAccountResponse> CreateDummyAccountAsync(
        CreateDummyAccountRequest? request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        DummyAccountResponse? response = null;
        var strategy = _db.Database.CreateExecutionStrategy();

        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);

            var selectedChains = await ResolveChainsAsync(request, cancellationToken);
            var account = new DummyAccount
            {
                Username = await GenerateUniqueUsernameAsync(cancellationToken),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var chain in selectedChains)
            {
                account.Wallets.Add(new AccountWallet
                {
                    Chain = chain,
                    WalletAddress = GenerateWalletAddress()
                });
            }

            _db.Accounts.Add(account);
            await _db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            response = MapAccount(account);
        });

        _logger.LogInformation(
            "Created dummy account {AccountId} with {WalletCount} chain wallets.",
            response!.Id,
            response.Chains.Count);

        return response!;
    }

    public async Task<DummyAccountResponse?> GetAccountAsync(
        int accountId,
        CancellationToken cancellationToken = default)
    {
        var account = await _db.Accounts
            .AsNoTracking()
            .Include(x => x.Wallets)
            .ThenInclude(x => x.Chain)
            .FirstOrDefaultAsync(x => x.Id == accountId, cancellationToken);

        return account is null ? null : MapAccount(account);
    }

    private static void ValidateRequest(CreateDummyAccountRequest? request)
    {
        if (request?.ChainCount is <= 0)
        {
            throw new ArgumentException("chain_count must be greater than zero.", nameof(request));
        }
    }

    private async Task<List<Chain>> ResolveChainsAsync(
        CreateDummyAccountRequest? request,
        CancellationToken cancellationToken)
    {
        var requestedChainNames = NormalizeRequestedChains(request?.Chains);

        if (requestedChainNames.Count > 0)
        {
            return await GetOrCreateChainsAsync(requestedChainNames, cancellationToken);
        }

        var availableChains = await _db.Chains
            .OrderBy(chain => chain.Id)
            .ToListAsync(cancellationToken);

        if (availableChains.Count == 0)
        {
            availableChains = DefaultChainNames
                .Select(name => new Chain { Name = name })
                .ToList();

            _db.Chains.AddRange(availableChains);
        }

        var targetChainCount = request?.ChainCount ?? Random.Shared.Next(1, availableChains.Count + 1);

        if (targetChainCount > availableChains.Count)
        {
            throw new ArgumentException(
                $"chain_count cannot exceed the {availableChains.Count} registered chains when no explicit chain list is provided.",
                nameof(request));
        }

        return availableChains
            .OrderBy(_ => Random.Shared.Next())
            .Take(targetChainCount)
            .ToList();
    }

    private async Task<List<Chain>> GetOrCreateChainsAsync(
        IReadOnlyList<string> requestedChainNames,
        CancellationToken cancellationToken)
    {
        var existingChains = await _db.Chains
            .Where(chain => requestedChainNames.Contains(chain.Name))
            .ToListAsync(cancellationToken);

        var chainsByName = existingChains.ToDictionary(chain => chain.Name, StringComparer.OrdinalIgnoreCase);
        var missingChains = requestedChainNames
            .Where(name => !chainsByName.ContainsKey(name))
            .Select(name => new Chain { Name = name })
            .ToList();

        if (missingChains.Count > 0)
        {
            _db.Chains.AddRange(missingChains);

            foreach (var chain in missingChains)
            {
                chainsByName[chain.Name] = chain;
            }
        }

        return requestedChainNames
            .Select(name => chainsByName[name])
            .ToList();
    }

    private async Task<string> GenerateUniqueUsernameAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 32; attempt += 1)
        {
            var candidate = GenerateUsername();
            var exists = await _db.Accounts
                .AsNoTracking()
                .AnyAsync(account => account.Username == candidate, cancellationToken);

            if (!exists)
            {
                return candidate;
            }
        }

        return $"dummy-{Guid.NewGuid():N}"[..18];
    }

    private static string GenerateUsername()
    {
        var prefix = UsernamePrefixes[Random.Shared.Next(UsernamePrefixes.Length)];
        var suffix = UsernameSuffixes[Random.Shared.Next(UsernameSuffixes.Length)];
        var numericPart = Random.Shared.Next(1000, 99999);

        return $"{prefix}-{suffix}-{numericPart}";
    }

    private static string GenerateWalletAddress()
    {
        Span<byte> buffer = stackalloc byte[20];

        while (true)
        {
            RandomNumberGenerator.Fill(buffer);
            var candidate = $"0x{Convert.ToHexString(buffer).ToLowerInvariant()}";

            if (WalletAddressRegex.IsMatch(candidate))
            {
                return candidate;
            }
        }
    }

    private static IReadOnlyList<string> NormalizeRequestedChains(IReadOnlyList<string>? chainNames)
    {
        if (chainNames is null || chainNames.Count == 0)
        {
            return [];
        }

        var normalizedNames = new List<string>(chainNames.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawName in chainNames)
        {
            var normalizedName = NormalizeChainName(rawName);

            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                continue;
            }

            if (seen.Add(normalizedName))
            {
                normalizedNames.Add(normalizedName);
            }
        }

        if (normalizedNames.Count == 0)
        {
            throw new ArgumentException("At least one non-empty chain name is required when chains are provided.", nameof(chainNames));
        }

        return normalizedNames;
    }

    private static string NormalizeChainName(string? chainName)
    {
        var trimmedName = chainName?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            return string.Empty;
        }

        if (KnownChainAliases.TryGetValue(trimmedName, out var canonicalName))
        {
            return canonicalName;
        }

        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(trimmedName.ToLowerInvariant());
    }

    private static DummyAccountResponse MapAccount(DummyAccount account)
    {
        var chains = account.Wallets
            .OrderBy(wallet => wallet.Chain.Name)
            .Select(wallet => new DummyAccountChainResponse(
                wallet.ChainId,
                wallet.Chain.Name,
                wallet.WalletAddress))
            .ToArray();

        return new DummyAccountResponse(
            account.Id,
            account.Username,
            account.CreatedAt,
            chains);
    }
}
*/
