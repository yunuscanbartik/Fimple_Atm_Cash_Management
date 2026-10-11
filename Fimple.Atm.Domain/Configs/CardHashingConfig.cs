namespace Fimple.Atm.Domain.Configs
{
    public class CardHashingConfig
    {
        public const string SectionName = "CardHashing";
        public const int MinimumKeyLength = 32;

        public string Key { get; set; } = string.Empty;
    }
}
