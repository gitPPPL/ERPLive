using System.Globalization;
using System.Text.Json;
using travelexpensemanagement.Models.Test;
using travelexpensemanagement.Repositories.Interfaces.Test;

namespace travelexpensemanagement.Repositories.Implementations.Test
{
    public sealed class SqlValueConverter
    : ISqlValueConverter
    {
        public object Convert(
            JsonElement value,
            ColumnMetadataModel column)
        {
            if (value.ValueKind == JsonValueKind.Null)
            {
                return DBNull.Value;
            }

            return column.SqlDataType.ToLowerInvariant() switch
            {
                "int" =>
                    ToInt(value),

                "bigint" =>
                    ToLong(value),

                "smallint" =>
                    ToShort(value),

                "tinyint" =>
                    ToByte(value),

                "decimal" =>
                    ToDecimal(value),

                "numeric" =>
                    ToDecimal(value),

                "money" =>
                    ToDecimal(value),

                "smallmoney" =>
                    ToDecimal(value),

                "float" =>
                    ToDouble(value),

                "real" =>
                    ToFloat(value),

                "bit" =>
                    ToBoolean(value),

                "date" =>
                    ToDateTime(value),

                "datetime" =>
                    ToDateTime(value),

                "datetime2" =>
                    ToDateTime(value),

                "smalldatetime" =>
                    ToDateTime(value),

                "datetimeoffset" =>
                    ToDateTimeOffset(value),

                "uniqueidentifier" =>
                    ToGuid(value),

                "nvarchar" =>
                    ToStringValue(value),

                "varchar" =>
                    ToStringValue(value),

                "nchar" =>
                    ToStringValue(value),

                "char" =>
                    ToStringValue(value),

                "text" =>
                    ToStringValue(value),

                "ntext" =>
                    ToStringValue(value),

                "varbinary" =>
                    ToByteArray(value),

                "binary" =>
                    ToByteArray(value),

                _ => throw new NotSupportedException(
                    $"SQL datatype '{column.SqlDataType}' " +
                    $"is not supported.")
            };
        }

        private static int ToInt(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                int.TryParse(
                    value.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw InvalidValue(value, "integer");
        }

        private static long ToLong(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt64(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                long.TryParse(
                    value.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw InvalidValue(value, "long integer");
        }

        private static short ToShort(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt16(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                short.TryParse(
                    value.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw InvalidValue(value, "small integer");
        }

        private static byte ToByte(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetByte(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                byte.TryParse(
                    value.GetString(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw InvalidValue(value, "byte");
        }

        private static decimal ToDecimal(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDecimal(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(
                    value.GetString(),
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw InvalidValue(value, "decimal");
        }

        private static double ToDouble(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetDouble(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                double.TryParse(
                    value.GetString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw InvalidValue(value, "double");
        }

        private static float ToFloat(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetSingle(out var result))
            {
                return result;
            }

            if (value.ValueKind == JsonValueKind.String &&
                float.TryParse(
                    value.GetString(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out result))
            {
                return result;
            }

            throw InvalidValue(value, "float");
        }

        private static bool ToBoolean(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.True)
                return true;

            if (value.ValueKind == JsonValueKind.False)
                return false;

            if (value.ValueKind == JsonValueKind.Number &&
                value.TryGetInt32(out var number))
            {
                if (number == 1)
                    return true;

                if (number == 0)
                    return false;
            }

            if (value.ValueKind == JsonValueKind.String)
            {
                var text = value.GetString()?.Trim();

                if (bool.TryParse(text, out var result))
                    return result;

                if (text == "1")
                    return true;

                if (text == "0")
                    return false;

                if (text.Equals(
                        "yes",
                        StringComparison.OrdinalIgnoreCase))
                    return true;

                if (text.Equals(
                        "no",
                        StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            throw InvalidValue(value, "boolean");
        }

        private static DateTime ToDateTime(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(
                    value.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var result))
            {
                return result;
            }

            throw InvalidValue(value, "date/time");
        }

        private static DateTimeOffset ToDateTimeOffset(
            JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String &&
                DateTimeOffset.TryParse(
                    value.GetString(),
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces,
                    out var result))
            {
                return result;
            }

            throw InvalidValue(value, "date/time with offset");
        }

        private static Guid ToGuid(JsonElement value)
        {
            if (value.ValueKind == JsonValueKind.String &&
                Guid.TryParse(
                    value.GetString(),
                    out var result))
            {
                return result;
            }

            throw InvalidValue(value, "GUID");
        }

        private static string ToStringValue(JsonElement value)
        {
            return value.ValueKind switch
            {
                JsonValueKind.String =>
                    value.GetString() ?? string.Empty,

                JsonValueKind.Number =>
                    value.GetRawText(),

                JsonValueKind.True =>
                    "true",

                JsonValueKind.False =>
                    "false",

                _ =>
                    value.GetRawText()
            };
        }

        private static byte[] ToByteArray(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                throw InvalidValue(value, "base64 binary");
            }

            try
            {
                return System.Convert.FromBase64String(
                    value.GetString() ?? string.Empty);
            }
            catch
            {
                throw InvalidValue(value, "base64 binary");
            }
        }

        private static FormatException InvalidValue(
            JsonElement value,
            string expected)
        {
            return new FormatException(
                $"Value {value.GetRawText()} " +
                $"cannot be converted to {expected}.");
        }

    }
}
