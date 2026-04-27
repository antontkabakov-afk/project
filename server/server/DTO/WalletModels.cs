using System.ComponentModel.DataAnnotations;

namespace server.DTO;

public record UserView(
    int Id,
    string Username,
    string Email,
    IReadOnlyList<WalletView> Wallets);

public record WalletView(
    int Id,
    string Name,
    string Address,
    string Chain,
    int UserId,
    IReadOnlyList<WalletSnapshotItemView> Snapshots);

public record WalletSnapshotItemView(
    int Id,
    int WalletId,
    DateTime Timestamp,
    decimal TotalValue,
    string? Currency,
    string? Notes);

public record CreateWalletRequest(
    [Required, MaxLength(100)] string Name,
    [Required, MaxLength(255)] string Address,
    [Required, MaxLength(32)] string Chain);

public record CreateWalletSnapshotRequest(
    [MaxLength(500)] string? Notes);
