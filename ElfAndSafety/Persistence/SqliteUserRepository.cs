using Dapper;
using ElfAndSafety.Models;
using Microsoft.Data.Sqlite;

namespace ElfAndSafety.Persistence;

public class SqliteUserRepository : IUserRepository
{
    private readonly string _connectionString;
    private readonly ElfAndSafety.Persistence.Cqrs.IEventBus? _eventBus;

    public SqliteUserRepository(string connectionString, ElfAndSafety.Persistence.Cqrs.IEventBus? eventBus = null)
    {
        _connectionString = connectionString;
        _eventBus = eventBus;
    }

    private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

    public async Task<IEnumerable<User>> GetUsersAsync(bool? showDeleted = null)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sql = "SELECT Id, FirstName, Surname, EmailAddress, Username, Deleted, DateCreated, DateLastModified FROM Users";
        if (showDeleted.HasValue)
        {
            sql += " WHERE Deleted = @Deleted";
            return await conn.QueryAsync<User>(sql, new { Deleted = showDeleted.Value ? 1 : 0 });
        }

        return await conn.QueryAsync<User>(sql);
    }

    public async Task<User?> GetUserByIdAsync(int id)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        var sql = "SELECT Id, FirstName, Surname, EmailAddress, Username, Deleted, DateCreated, DateLastModified FROM Users WHERE Id = @Id";
        return await conn.QueryFirstOrDefaultAsync<User>(sql, new { Id = id });
    }

    public async Task<User> CreateUserAsync(User user)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sql = @"INSERT INTO Users (FirstName, Surname, EmailAddress, Username, Deleted, DateCreated, DateLastModified)
                    VALUES (@FirstName, @Surname, @EmailAddress, @Username, @Deleted, @DateCreated, @DateLastModified);
                    SELECT last_insert_rowid();";

        var id = await conn.ExecuteScalarAsync<long>(sql, new
        {
            user.FirstName,
            user.Surname,
            user.EmailAddress,
            user.Username,
            Deleted = user.Deleted ? 1 : 0,
            DateCreated = user.DateCreated,
            DateLastModified = user.DateLastModified
        });

        user.Id = (int)id;

        _eventBus?.Publish(new ElfAndSafety.Persistence.Cqrs.UserChangedEvent { Type = ElfAndSafety.Persistence.Cqrs.UserEventType.Created, User = user, UserId = user.Id });
        return user;
    }

    public async Task<User> UpdateUserAsync(User user)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sql = @"UPDATE Users SET FirstName = @FirstName, Surname = @Surname, EmailAddress = @EmailAddress,
                    Username = @Username, Deleted = @Deleted, DateLastModified = @DateLastModified WHERE Id = @Id";

        await conn.ExecuteAsync(sql, new
        {
            user.FirstName,
            user.Surname,
            user.EmailAddress,
            user.Username,
            Deleted = user.Deleted ? 1 : 0,
            DateLastModified = user.DateLastModified,
            user.Id
        });

        _eventBus?.Publish(new ElfAndSafety.Persistence.Cqrs.UserChangedEvent { Type = ElfAndSafety.Persistence.Cqrs.UserEventType.Updated, User = user, UserId = user.Id });
        return user;
    }

    public async Task<bool> DeleteUserAsync(int id)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sql = "UPDATE Users SET Deleted = 1, DateLastModified = @DateLastModified WHERE Id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { DateLastModified = DateTime.UtcNow, Id = id });
        var success = rows > 0;
        if (success)
        {
            _eventBus?.Publish(new ElfAndSafety.Persistence.Cqrs.UserChangedEvent { Type = ElfAndSafety.Persistence.Cqrs.UserEventType.Deleted, UserId = id });
        }

        return success;
    }

    public async Task<bool> RestoreUserAsync(int id)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();

        var sql = "UPDATE Users SET Deleted = 0, DateLastModified = @DateLastModified WHERE Id = @Id";
        var rows = await conn.ExecuteAsync(sql, new { DateLastModified = DateTime.UtcNow, Id = id });
        var success = rows > 0;
        if (success)
        {
            _eventBus?.Publish(new ElfAndSafety.Persistence.Cqrs.UserChangedEvent { Type = ElfAndSafety.Persistence.Cqrs.UserEventType.Restored, UserId = id });
        }

        return success;
    }
}
