using System.Text.Json.Serialization;

namespace Fimple.Atm.Domain.Models
{
    public abstract class AtmRequestBase : UserRequestBase
    {
        [JsonIgnore]
        public Guid AtmId { get; set; }
    }
}
