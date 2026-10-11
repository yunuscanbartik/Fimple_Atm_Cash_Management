namespace Fimple.Atm.Domain.Interfaces
{
    public interface ICardHasher
    {
        public string Hash(string cardNumber);
        public string Mask(string cardNumber);
    }
}
