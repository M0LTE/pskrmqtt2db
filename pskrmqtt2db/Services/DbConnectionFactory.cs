using Microsoft.Extensions.Options;
using MySqlConnector;
using pskrmqtt2db.Models;
using System.Data;
using Dapper;

namespace pskrmqtt2db.Services;
public class DbConnectionFactory
{
    private readonly DbConfig _dbConfig;

    public DbConnectionFactory(IOptions<DbConfig> dbConfig)
    {
        _dbConfig = dbConfig.Value;
    }

    internal async Task<IDbConnection> GetWriteConnection()
    {
        if (string.IsNullOrWhiteSpace(_dbConfig?.Server))
        {
            throw new Exception("DB server not specified");
        }

        var parts = _dbConfig?.Server?.Split(':');
        var server = parts!.FirstOrDefault();
        int port = parts!.Length == 2 && int.TryParse(parts[1], out var p) ? p : 3306;

        var conn = new MySqlConnection($"Server={server};Port={port};Database={_dbConfig?.Database};User ID={_dbConfig?.Username};Password={_dbConfig?.Password};Default Command Timeout=30");
        await conn.OpenAsync();
        await conn.ExecuteAsync(@"SET time_zone = '+00:00';");
        return conn;
    }

    internal Task<IDbConnection> GetReadConnection() => GetWriteConnection();
}