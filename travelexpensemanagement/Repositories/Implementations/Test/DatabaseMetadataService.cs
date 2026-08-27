using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Caching.Memory;
using travelexpensemanagement.Dbconnection;
using travelexpensemanagement.Models.Test;
using travelexpensemanagement.Repositories.Interfaces.Test;

namespace travelexpensemanagement.Repositories.Implementations.Test
{
    public sealed class DatabaseMetadataService : IDatabaseMetadataService
    {
        private readonly DataBaseConnection _dbConnection;
        private readonly IMemoryCache _cache;

        public DatabaseMetadataService(DataBaseConnection dbConnection, IMemoryCache cache)
        {
            _dbConnection = dbConnection;
            _cache = cache;
        }


        public async Task<IReadOnlyDictionary<string, ColumnMetadataModel>> GetColumnsAsync(string tableName)
        {
            // =========================================
            // Cache Key
            // =========================================

            string cacheKey = $"db-schema:{tableName.ToLowerInvariant()}";

            // =========================================
            // Check Cache
            // =========================================

            if (_cache.TryGetValue(cacheKey, out IReadOnlyDictionary<string, ColumnMetadataModel>? cachedMetadata))
            {
                return cachedMetadata!;
            }


            // =========================================
            // Database
            // =========================================

            const string sql = """
                SELECT
                    c.COLUMN_NAME,
                    c.DATA_TYPE,
                    c.CHARACTER_MAXIMUM_LENGTH,
                    c.NUMERIC_PRECISION,
                    c.NUMERIC_SCALE,
                    c.IS_NULLABLE,
                    c.COLUMN_DEFAULT,
                    COLUMNPROPERTY(
                        OBJECT_ID(
                            c.TABLE_SCHEMA + '.' + c.TABLE_NAME),
                        c.COLUMN_NAME,
                        'IsIdentity'
                    ) AS IsIdentity
                FROM INFORMATION_SCHEMA.COLUMNS c
                WHERE c.TABLE_SCHEMA = 'dbo'
                  AND c.TABLE_NAME = @TableName
                ORDER BY c.ORDINAL_POSITION;
                """;


            var result = new Dictionary<string, ColumnMetadataModel>(StringComparer.OrdinalIgnoreCase);
            await using var connection = _dbConnection.GetErpConnection();
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add(new SqlParameter("@TableName", System.Data.SqlDbType.NVarChar, 128)
            {
                Value = tableName
            });

            await connection.OpenAsync();
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                var metadata = new ColumnMetadataModel
                {
                    ColumnName = reader.GetString(0),
                    SqlDataType = reader.GetString(1),
                    MaxLength = reader.IsDBNull(2) ? null : reader.GetInt32(2),
                    Precision = reader.IsDBNull(3) ? null : Convert.ToByte(reader.GetValue(3)),
                    Scale = reader.IsDBNull(4) ? null : Convert.ToByte(reader.GetValue(4)),
                    IsNullable = string.Equals(reader.GetString(5), "YES", StringComparison.OrdinalIgnoreCase),
                    DefaultValue = reader.IsDBNull(6) ? null : reader.GetString(6),
                    IsIdentity = !reader.IsDBNull(7) && Convert.ToInt32(reader.GetValue(7)) == 1
                };

                result[metadata.ColumnName] = metadata;
            }


            // =========================================
            // Table not found
            // =========================================

            if (result.Count == 0)
            {
                throw new InvalidOperationException($"Table '{tableName}' was not found.");
            }

            // =========================================
            // Store in Cache
            // =========================================

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30),
                SlidingExpiration = TimeSpan.FromMinutes(10),
                Priority = CacheItemPriority.High
            };

            _cache.Set(cacheKey, result, cacheOptions);
            return result;
        }
    }
}