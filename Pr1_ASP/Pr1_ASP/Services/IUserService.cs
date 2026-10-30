using Pr1_ASP.Dtos;
using Pr1_ASP.Models;

namespace Pr1_ASP.Services;

public interface IUserService
{
    Task<(List<UserDto> Items, int TotalCount)> GetAllAsync(int page, int pageSize, CancellationToken ct);
    Task<UserDto?> GetByIdAsync(int id, CancellationToken ct);
    Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct);
    Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct);
    Task<UserDto?> PatchAsync(int id, PatchUserDto dto, CancellationToken ct);
    Task<bool> DeleteAsync(int id, CancellationToken ct);

    Task<(User User, string RoleName)> RegisterAsync(RegisterDto dto, CancellationToken ct);
    Task<(User User, string RoleName)?> ValidateCredentialsAsync(LoginDto dto, CancellationToken ct);
}