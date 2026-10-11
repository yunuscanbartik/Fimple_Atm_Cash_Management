using System.Text.Json.Serialization;

namespace Fimple.Atm.Domain.Models
{
    public abstract class UserRequestBase
    {
        [JsonIgnore]
        public string? UserName { get; private set; }

        public void SetUserName(string? userName)
        {
            UserName = userName;
        }
    }

    public abstract class AtmRequestBase : UserRequestBase
    {
        [JsonIgnore]
        public Guid AtmId { get; set; }
    }
}
