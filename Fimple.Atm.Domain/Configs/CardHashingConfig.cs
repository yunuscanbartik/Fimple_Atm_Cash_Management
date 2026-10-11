namespace Fimple.Atm.Domain.Configs
{
    public class CardHashingConfig
    {
        public const string SECTION_NAME = "CardHashing";
        public const int MINIMUM_KEY_LENGTH = 32;

        public string Key { get; set; } = string.Empty;
    }
}
