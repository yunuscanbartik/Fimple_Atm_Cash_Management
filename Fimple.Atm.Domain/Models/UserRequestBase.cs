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
}
