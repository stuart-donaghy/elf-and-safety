using Microsoft.Data.Sqlite;

namespace ElfAndSafety.Persistence;

public static class SqliteDbInitializer
{
    public static void Initialize(string connectionString)
    {
        // Ensure database file and table exist
        using var conn = new SqliteConnection(connectionString);
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Users (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FirstName TEXT NOT NULL,
                Surname TEXT NOT NULL,
                EmailAddress TEXT NOT NULL,
                Username TEXT NOT NULL,
                Deleted INTEGER NOT NULL DEFAULT 0,
                DateCreated TEXT NOT NULL,
                DateLastModified TEXT NOT NULL
            );
        ";
        cmd.ExecuteNonQuery();

        // Seed data if empty
        cmd.CommandText = "SELECT COUNT(1) FROM Users";
        var count = Convert.ToInt32(cmd.ExecuteScalar());
        if (count == 0)
        {
            cmd.CommandText = @"
                INSERT INTO Users (FirstName, Surname, EmailAddress, Username, Deleted, DateCreated, DateLastModified)
                VALUES
                ('John', 'Doe', 'john.doe@example.com', 'johndoe', 0, @now, @now),
                ('Jane', 'Smith', 'jane.smith@example.com', 'janesmith', 0, @now, @now),
                ('Bob', 'Johnson', 'bob.johnson@example.com', 'bobjohnson', 1, @now, @now);
            ";
            cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }
    }
}
