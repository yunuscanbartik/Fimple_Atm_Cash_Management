using System.Text.Json;
using System.Text.Json.Serialization;

namespace Fimple.Atm.Application.Services
{
    internal static class OutboxJson
    {
        public static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper) }
        };
    }
}
