using System.Dynamic;
using System.Text.Json;
using travelexpensemanagement.Models.Test;
using travelexpensemanagement.Repositories.Interfaces.Test;

namespace travelexpensemanagement.Repositories.Implementations.Test
{
    public sealed class DataConversionService
        : IDataConversionService
    {
        private readonly IDatabaseMetadataService
            _metadataService;

        private readonly ISqlValueConverter
            _sqlValueConverter;


        public DataConversionService(
            IDatabaseMetadataService metadataService,
            ISqlValueConverter sqlValueConverter)
        {
            _metadataService = metadataService;
            _sqlValueConverter = sqlValueConverter;
        }


        public async Task<dynamic> ConvertAsync(
            string tableName,
            JsonElement data)
        {
            var columns =
                await _metadataService
                    .GetColumnsAsync(tableName);


            // =========================================
            // Single object
            // =========================================

            if (data.ValueKind ==
                JsonValueKind.Object)
            {
                return ConvertObject(
                    data,
                    columns);
            }


            // =========================================
            // List
            // =========================================

            if (data.ValueKind ==
                JsonValueKind.Array)
            {
                var list =
                    new List<dynamic>();


                foreach (var item
                         in data.EnumerateArray())
                {
                    if (item.ValueKind !=
                        JsonValueKind.Object)
                    {
                        throw new FormatException(
                            $"Data for '{tableName}' " +
                            "must contain objects.");
                    }


                    list.Add(
                        ConvertObject(
                            item,
                            columns));
                }


                return list;
            }


            throw new FormatException(
                $"Data for '{tableName}' " +
                "must be an object or array.");
        }


        private ExpandoObject ConvertObject(
            JsonElement data,
            IReadOnlyDictionary<
                string,
                ColumnMetadataModel> columns)
        {
            IDictionary<string, object?> result =
                new ExpandoObject();


            foreach (var column in columns.Values)
            {
                // =====================================
                // Identity
                // =====================================

                if (column.IsIdentity)
                    continue;


                // =====================================
                // Property missing
                // =====================================

                if (!data.TryGetProperty(
                        column.ColumnName,
                        out var value))
                {
                    if (column.IsNullable)
                    {
                        result[column.ColumnName] =
                            DBNull.Value;
                    }

                    // If NOT NULL + DEFAULT:
                    // don't add property.
                    //
                    // SQL Server will use DEFAULT.

                    continue;
                }


                // =====================================
                // JSON null
                // =====================================

                if (value.ValueKind ==
                    JsonValueKind.Null)
                {
                    result[column.ColumnName] =
                        DBNull.Value;

                    continue;
                }


                // =====================================
                // Empty string
                // =====================================

                if (value.ValueKind ==
                        JsonValueKind.String &&
                    string.IsNullOrWhiteSpace(
                        value.GetString()))
                {
                    if (IsStringType(
                        column.SqlDataType))
                    {
                        result[column.ColumnName] =
                            value.GetString()
                            ?? string.Empty;
                    }
                    else
                    {
                        result[column.ColumnName] =
                            DBNull.Value;
                    }

                    continue;
                }


                // =====================================
                // SQL datatype conversion
                // =====================================

                result[column.ColumnName] =
                    _sqlValueConverter.Convert(
                        value,
                        column);
            }


            return (ExpandoObject)result;
        }


        private static bool IsStringType(
            string dataType)
        {
            return dataType.Equals(
                       "varchar",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   dataType.Equals(
                       "nvarchar",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   dataType.Equals(
                       "char",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   dataType.Equals(
                       "nchar",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   dataType.Equals(
                       "text",
                       StringComparison.OrdinalIgnoreCase)
                   ||
                   dataType.Equals(
                       "ntext",
                       StringComparison.OrdinalIgnoreCase);
        }
    }
}