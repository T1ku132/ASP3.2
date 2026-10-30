using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Pr1_ASP.Dtos;
using Pr1_ASP.Models;
using Pr1_ASP.Repositories;

namespace Pr1_ASP.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _repository;
    private readonly IRoleRepository _roles;
    private readonly IMapper _mapper;
    private readonly IPasswordHasher<User> _passwordHasher;

    public UserService(
        IUserRepository repository,
        IRoleRepository roles,
        IMapper mapper,
        IPasswordHasher<User> passwordHasher)
    {
        _repository = repository;
        _roles = roles;
        _mapper = mapper;
        _passwordHasher = passwordHasher;
    }

    public async Task<(List<UserDto> Items, int TotalCount)> GetAllAsync(
        int page, int pageSize, CancellationToken ct)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var entities = await _repository.GetPageAsync(page, pageSize, ct);
        var total = await _repository.CountAsync(ct);
        return (_mapper.Map<List<UserDto>>(entities), total);
    }

    public async Task<UserDto?> GetByIdAsync(int id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct, track: false);
        return entity is null ? null : _mapper.Map<UserDto>(entity);
    }

    public async Task<UserDto> CreateAsync(CreateUserDto dto, CancellationToken ct)
    {
        var entity = _mapper.Map<User>(dto);
        entity.PasswordHash = _passwordHasher.HashPassword(entity, dto.Password);
        entity.CreatedAt = DateTime.UtcNow;

        await ValidateAsync(entity, ct);

        await _repository.AddAsync(entity, ct);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
        {
            throw new ConflictException($"Пользователь с логином '{entity.Username}' или email '{entity.Email}' уже существует.");
        }

        return await LoadAsync(entity.UserId, ct);
    }

    public async Task<UserDto?> UpdateAsync(int id, UpdateUserDto dto, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is null) return null;

        _mapper.Map(dto, entity);
        entity.PasswordHash = _passwordHasher.HashPassword(entity, dto.Password);

        await ValidateAsync(entity, ct);

        _repository.Update(entity);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
        {
            throw new ConflictException($"Пользователь с логином '{entity.Username}' или email '{entity.Email}' уже существует.");
        }

        return await LoadAsync(id, ct);
    }

    public async Task<UserDto?> PatchAsync(int id, PatchUserDto dto, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is null) return null;

        _mapper.Map(dto, entity);

        if (dto.Password is not null)
        {
            entity.PasswordHash = _passwordHasher.HashPassword(entity, dto.Password);
        }

        await ValidateAsync(entity, ct);

        _repository.Update(entity);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
        {
            throw new ConflictException($"Пользователь с логином '{entity.Username}' или email '{entity.Email}' уже существует.");
        }

        return await LoadAsync(id, ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity is null) return false;

        if (await _repository.HasDependentsAsync(id, ct))
            throw new ConflictException("Нельзя удалить пользователя: есть корзина, заказы или резервирования.");

        _repository.Remove(entity);
        await _repository.SaveChangesAsync(ct);
        return true;
    }

    public async Task<(User User, string RoleName)> RegisterAsync(RegisterDto dto, CancellationToken ct)
    {
        if (await _repository.UsernameExistsAsync(dto.Username, 0, ct))
            throw new ConflictException($"Имя пользователя '{dto.Username}' уже занято.");

        if (await _repository.EmailExistsAsync(dto.Email, 0, ct))
            throw new ConflictException($"Email '{dto.Email}' уже используется.");

        var defaultRoleId = 11;
        var role = await _roles.GetByIdAsync(defaultRoleId, ct);

        if (role is null)
            throw new InvalidOperationException($"Роль по умолчанию (ID={defaultRoleId}) не найдена в базе данных.");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            RoleId = defaultRoleId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, dto.Password);

        await _repository.AddAsync(user, ct);

        try
        {
            await _repository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqlException sqlEx && (sqlEx.Number == 2627 || sqlEx.Number == 2601))
        {
            throw new ConflictException($"Пользователь с логином '{dto.Username}' или email '{dto.Email}' уже существует.");
        }

        return (user, role.RoleName);
    }

    public async Task<(User User, string RoleName)?> ValidateCredentialsAsync(LoginDto dto, CancellationToken ct)
    {
        var allUsers = await _repository.GetPageAsync(1, 100000, ct);
        var user = allUsers.FirstOrDefault(u => u.Username == dto.Username);

        if (user == null || user.IsActive != true)
            return null;

        var result = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

        if (result == PasswordVerificationResult.Failed)
            return null;

        var role = await _roles.GetByIdAsync(user.RoleId, ct);
        var roleName = role?.RoleName ?? "User";

        return (user, roleName);
    }

    private async Task<UserDto> LoadAsync(int id, CancellationToken ct)
    {
        var saved = await _repository.GetByIdAsync(id, ct, track: false)
            ?? throw new InvalidOperationException("Не удалось загрузить сохранённую запись.");
        return _mapper.Map<UserDto>(saved);
    }

    private async Task ValidateAsync(User entity, CancellationToken ct)
    {
        if (!await _roles.ExistsAsync(entity.RoleId, ct))
            throw new InvalidOperationException($"Роль с Id {entity.RoleId} не найдена.");

        if (await _repository.UsernameExistsAsync(entity.Username, entity.UserId, ct))
            throw new ConflictException($"Имя пользователя '{entity.Username}' уже занято.");

        if (await _repository.EmailExistsAsync(entity.Email, entity.UserId, ct))
            throw new ConflictException($"Email '{entity.Email}' уже используется.");
    }
}