using System.Security.Cryptography;
using System.Text;
using Fimple.Atm.Domain.Configs;
using Fimple.Atm.Domain.Constants;
using Fimple.Atm.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace Fimple.Atm.Application.Services
{
    public class CardHasher : ICardHasher
    {
        private const char MASK_CHARACTER = '*';

        private readonly byte[] _hashingKey;

        public CardHasher(IOptions<CardHashingConfig> cardHashingConfig)
        {
            _hashingKey = Encoding.UTF8.GetBytes(cardHashingConfig.Value.Key);
        }

        public string Hash(string cardNumber)
        {
            var cardNumberBytes = Encoding.UTF8.GetBytes(cardNumber);
            var hashBytes = HMACSHA256.HashData(_hashingKey, cardNumberBytes);
            return Convert.ToHexString(hashBytes).ToLowerInvariant();
        }

        public string Mask(string cardNumber)
        {
            var visibleDigits = cardNumber[^WithdrawalRules.MaskedCardVisibleDigitCount..];
            var maskedLength = cardNumber.Length - WithdrawalRules.MaskedCardVisibleDigitCount;
            return new string(MASK_CHARACTER, maskedLength) + visibleDigits;
        }
    }
}
