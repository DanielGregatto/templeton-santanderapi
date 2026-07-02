using Services.Infrastructure.Json;
using System;
using System.Text.Json.Serialization;

namespace Services.Contracts.Results
{
    public record StoryResult(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("uri")] string Uri,
        [property: JsonPropertyName("postedBy")] string PostedBy,
        [property: JsonPropertyName("time"), JsonConverter(typeof(Iso8601SecondsConverter))] DateTimeOffset Time,
        [property: JsonPropertyName("score")] int Score,
        [property: JsonPropertyName("commentCount")] int CommentCount);
}
