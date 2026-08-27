using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace travelexpensemanagement.Common.ModelBinding
{
    public class SafePrimitiveConverterFactory : JsonConverterFactory
    {
        public override bool CanConvert(Type typeToConvert)
        {
            Type type = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

            return type == typeof(int)
                || type == typeof(long)
                || type == typeof(short)
                || type == typeof(byte)
                || type == typeof(decimal)
                || type == typeof(double)
                || type == typeof(float)
                || type == typeof(bool)
                || type == typeof(string);
        }

        public override JsonConverter CreateConverter(
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            Type converterType =
                typeof(SafePrimitiveConverter<>)
                .MakeGenericType(typeToConvert);

            return (JsonConverter)Activator.CreateInstance(converterType)!;
        }
    }


    public class SafePrimitiveConverter<T> : JsonConverter<T>
    {
        private static readonly Type TargetType =
            Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

        public override T Read(
            ref Utf8JsonReader reader,
            Type typeToConvert,
            JsonSerializerOptions options)
        {
            try
            {
                // JSON null
                if (reader.TokenType == JsonTokenType.Null)
                {
                    return GetDefaultValue();
                }

                // String
                if (reader.TokenType == JsonTokenType.String)
                {
                    string? value = reader.GetString();

                    if (string.IsNullOrWhiteSpace(value))
                        return GetDefaultValue();

                    return ConvertValue(value);
                }

                // Number
                if (reader.TokenType == JsonTokenType.Number)
                {
                    using JsonDocument doc = JsonDocument.ParseValue(ref reader);

                    string value = doc.RootElement.GetRawText();

                    return ConvertValue(value);
                }

                // Boolean
                if (reader.TokenType == JsonTokenType.True)
                {
                    if (TargetType == typeof(bool))
                        return (T)(object)true;

                    return GetDefaultValue();
                }

                if (reader.TokenType == JsonTokenType.False)
                {
                    if (TargetType == typeof(bool))
                        return (T)(object)false;

                    return GetDefaultValue();
                }

                // Anything else
                return GetDefaultValue();
            }
            catch
            {
                // Important:
                // Never allow one bad property to destroy
                // the complete model.
                return GetDefaultValue();
            }
        }


        public override void Write(
            Utf8JsonWriter writer,
            T value,
            JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            if (TargetType == typeof(string))
            {
                writer.WriteStringValue((string?)(object?)value);
                return;
            }

            if (TargetType == typeof(int))
            {
                writer.WriteNumberValue(Convert.ToInt32(value));
                return;
            }

            if (TargetType == typeof(long))
            {
                writer.WriteNumberValue(Convert.ToInt64(value));
                return;
            }

            if (TargetType == typeof(short))
            {
                writer.WriteNumberValue(Convert.ToInt16(value));
                return;
            }

            if (TargetType == typeof(byte))
            {
                writer.WriteNumberValue(Convert.ToByte(value));
                return;
            }

            if (TargetType == typeof(decimal))
            {
                writer.WriteNumberValue(Convert.ToDecimal(value));
                return;
            }

            if (TargetType == typeof(double))
            {
                writer.WriteNumberValue(Convert.ToDouble(value));
                return;
            }

            if (TargetType == typeof(float))
            {
                writer.WriteNumberValue(Convert.ToSingle(value));
                return;
            }

            if (TargetType == typeof(bool))
            {
                writer.WriteBooleanValue(Convert.ToBoolean(value));
                return;
            }

            writer.WriteNullValue();
        }


        private static T GetDefaultValue()
        {
            // string → ""
            if (TargetType == typeof(string))
            {
                return (T)(object)string.Empty;
            }

            // int, decimal, double, bool, etc. → default(T)
            return default!;
        }


        private static T ConvertValue(string value)
        {
            try
            {
                if (TargetType == typeof(string))
                {
                    return (T)(object)value;
                }


                if (TargetType == typeof(int))
                {
                    if (int.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out int result))
                    {
                        return (T)(object)result;
                    }

                    return GetDefaultValue();
                }


                if (TargetType == typeof(long))
                {
                    if (long.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out long result))
                    {
                        return (T)(object)result;
                    }

                    return GetDefaultValue();
                }


                if (TargetType == typeof(short))
                {
                    if (short.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out short result))
                    {
                        return (T)(object)result;
                    }

                    return GetDefaultValue();
                }


                if (TargetType == typeof(byte))
                {
                    if (byte.TryParse(
                        value,
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out byte result))
                    {
                        return (T)(object)result;
                    }

                    return GetDefaultValue();
                }


                if (TargetType == typeof(decimal))
                {
                    if (decimal.TryParse(
                        value,
                        NumberStyles.Number,
                        CultureInfo.InvariantCulture,
                        out decimal result))
                    {
                        return (T)(object)result;
                    }

                    return GetDefaultValue();
                }


                if (TargetType == typeof(double))
                {
                    if (double.TryParse(
                        value,
                        NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        out double result))
                    {
                        return (T)(object)result;
                    }

                    return GetDefaultValue();
                }


                if (TargetType == typeof(float))
                {
                    if (float.TryParse(
                        value,
                        NumberStyles.Float | NumberStyles.AllowThousands,
                        CultureInfo.InvariantCulture,
                        out float result))
                    {
                        return (T)(object)result;
                    }

                    return GetDefaultValue();
                }


                if (TargetType == typeof(bool))
                {
                    if (bool.TryParse(value, out bool result))
                    {
                        return (T)(object)result;
                    }

                    // Also support 1 / 0
                    if (value == "1")
                        return (T)(object)true;

                    if (value == "0")
                        return (T)(object)false;

                    return GetDefaultValue();
                }


                return GetDefaultValue();
            }
            catch
            {
                return GetDefaultValue();
            }
        }
    }

}
