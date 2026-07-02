using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Services.Infrastructure.Json
{
    /// <summary>Serializes DateTimeOffset as "yyyy-MM-ddTHH:mm:sszzz" (no fractional seconds).</summary>
    public class Iso8601SecondsConverter : JsonConverter<DateTimeOffset>
    {
        private const string Format = "yyyy-MM-ddTHH:mm:sszzz";

        public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            DateTimeOffset.Parse(reader.GetString()!);

        public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value.ToString(Format));
    }
}
