using Fimple.Atm.Domain.Entities;
using Fimple.Atm.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Fimple.Atm.Infrastructure.PostgreSQL
{
    public class AppDbContext : DbContext
    {
        private const string TIMESTAMP_WITH_TIME_ZONE = "timestamp with time zone";
        private const int ENUM_MAX_LENGTH = 32;
        private const int NAME_MAX_LENGTH = 255;
        private const int CARD_HASH_MAX_LENGTH = 64;
        private const int MASKED_CARD_NUMBER_MAX_LENGTH = 32;
        private const int IDEMPOTENCY_KEY_MAX_LENGTH = 128;
        private const int USER_NAME_MAX_LENGTH = 128;
        private const int MONEY_PRECISION = 18;
        private const int MONEY_SCALE = 2;
        private const int RATE_SCALE = 6;

        private static readonly DateTime ExchangeRateSeedDate = new DateTime(2026, 10, 11, 0, 0, 0, DateTimeKind.Utc);

        public AppDbContext(DbContextOptions<AppDbContext> dbContextOptions)
            : base(dbContextOptions)
        {

        }

        public DbSet<AtmMachine> AtmMachines { get; set; }
        public DbSet<Cassette> Cassettes { get; set; }
        public DbSet<CassetteContent> CassetteContents { get; set; }
        public DbSet<CassetteMovement> CassetteMovements { get; set; }
        public DbSet<WithdrawalQuote> WithdrawalQuotes { get; set; }
        public DbSet<WithdrawalQuoteOption> WithdrawalQuoteOptions { get; set; }
        public DbSet<WithdrawalQuoteOptionItem> WithdrawalQuoteOptionItems { get; set; }
        public DbSet<DepositTransaction> DepositTransactions { get; set; }
        public DbSet<DepositNote> DepositNotes { get; set; }
        public DbSet<Reconciliation> Reconciliations { get; set; }
        public DbSet<ReconciliationLine> ReconciliationLines { get; set; }
        public DbSet<ExchangeRate> ExchangeRates { get; set; }
        public DbSet<OutboxEvent> OutboxEvents { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            ConfigureAtmEntity(modelBuilder);
            ConfigureCassetteEntity(modelBuilder);
            ConfigureCassetteContentEntity(modelBuilder);
            ConfigureCassetteMovementEntity(modelBuilder);
            ConfigureWithdrawalQuoteEntity(modelBuilder);
            ConfigureWithdrawalQuoteOptionEntity(modelBuilder);
            ConfigureWithdrawalQuoteOptionItemEntity(modelBuilder);
            ConfigureDepositTransactionEntity(modelBuilder);
            ConfigureDepositNoteEntity(modelBuilder);
            ConfigureReconciliationEntity(modelBuilder);
            ConfigureReconciliationLineEntity(modelBuilder);
            ConfigureExchangeRateEntity(modelBuilder);
            ConfigureOutboxEventEntity(modelBuilder);
        }

        private void ConfigureAtmEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<AtmMachine>();

            entity.ToTable(TableNames.ATMS);
            entity.HasKey(atm => atm.Id);
            entity.Property(atm => atm.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(atm => atm.Name)
                .IsRequired()
                .HasMaxLength(NAME_MAX_LENGTH)
                .HasColumnOrder(1);
            entity.Property(atm => atm.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(2);
            entity.Property(atm => atm.CreatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(3);
            entity.Property(atm => atm.UpdatedAt)
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(4);

            entity.HasIndex(atm => atm.Name).IsUnique();
        }

        private void ConfigureCassetteEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Cassette>();

            entity.ToTable(TableNames.CASSETTES);
            entity.HasKey(cassette => cassette.Id);
            entity.Property(cassette => cassette.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(cassette => cassette.AtmId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(cassette => cassette.Position)
                .IsRequired()
                .HasColumnOrder(2);
            entity.Property(cassette => cassette.Kind)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(3);
            entity.Property(cassette => cassette.Currency)
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(4);
            entity.Property(cassette => cassette.Denomination)
                .HasConversion<int>()
                .HasColumnOrder(5);
            entity.Property(cassette => cassette.Capacity)
                .IsRequired()
                .HasColumnOrder(6);
            entity.Property(cassette => cassette.CreatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(7);
            entity.Property(cassette => cassette.UpdatedAt)
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(8);

            entity.HasIndex(cassette => new { cassette.AtmId, cassette.Position }).IsUnique();

            entity.HasOne(cassette => cassette.Atm)
                .WithMany(atm => atm.Cassettes)
                .HasForeignKey(cassette => cassette.AtmId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureCassetteContentEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<CassetteContent>();

            entity.ToTable(TableNames.CASSETTE_CONTENTS, table =>
            {
                table.HasCheckConstraint("CK_" + TableNames.CASSETTE_CONTENTS + "_NoteCount", "\"NoteCount\" >= 0");
                table.HasCheckConstraint("CK_" + TableNames.CASSETTE_CONTENTS + "_ReservedCount", "\"ReservedCount\" >= 0 AND \"ReservedCount\" <= \"NoteCount\"");
            });
            entity.HasKey(content => content.Id);
            entity.Property(content => content.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(content => content.CassetteId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(content => content.Currency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(2);
            entity.Property(content => content.Denomination)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnOrder(3);
            entity.Property(content => content.NoteCount)
                .IsRequired()
                .HasColumnOrder(4);
            entity.Property(content => content.ReservedCount)
                .IsRequired()
                .HasColumnOrder(5);

            entity.HasIndex(content => new { content.CassetteId, content.Currency, content.Denomination }).IsUnique();

            entity.HasOne(content => content.Cassette)
                .WithMany(cassette => cassette.Contents)
                .HasForeignKey(content => content.CassetteId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureCassetteMovementEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<CassetteMovement>();

            entity.ToTable(TableNames.CASSETTE_MOVEMENTS);
            entity.HasKey(movement => movement.Id);
            entity.Property(movement => movement.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(movement => movement.CassetteId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(movement => movement.MovementType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(2);
            entity.Property(movement => movement.Currency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(3);
            entity.Property(movement => movement.Denomination)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnOrder(4);
            entity.Property(movement => movement.NoteCountDelta)
                .IsRequired()
                .HasColumnOrder(5);
            entity.Property(movement => movement.ReservedCountDelta)
                .IsRequired()
                .HasColumnOrder(6);
            entity.Property(movement => movement.ReferenceId)
                .HasColumnOrder(7);
            entity.Property(movement => movement.PerformedBy)
                .HasMaxLength(USER_NAME_MAX_LENGTH)
                .HasColumnOrder(8);
            entity.Property(movement => movement.CreatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(9);

            entity.HasIndex(movement => new { movement.CassetteId, movement.CreatedAt });
            entity.HasIndex(movement => movement.ReferenceId);

            entity.HasOne(movement => movement.Cassette)
                .WithMany()
                .HasForeignKey(movement => movement.CassetteId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureWithdrawalQuoteEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WithdrawalQuote>();

            entity.ToTable(TableNames.WITHDRAWAL_QUOTES);
            entity.HasKey(quote => quote.Id);
            entity.Property(quote => quote.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(quote => quote.AtmId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(quote => quote.CardHash)
                .IsRequired()
                .HasMaxLength(CARD_HASH_MAX_LENGTH)
                .HasColumnOrder(2);
            entity.Property(quote => quote.MaskedCardNumber)
                .IsRequired()
                .HasMaxLength(MASKED_CARD_NUMBER_MAX_LENGTH)
                .HasColumnOrder(3);
            entity.Property(quote => quote.Currency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(4);
            entity.Property(quote => quote.RequestedAmount)
                .IsRequired()
                .HasPrecision(MONEY_PRECISION, MONEY_SCALE)
                .HasColumnOrder(5);
            entity.Property(quote => quote.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(6);
            entity.Property(quote => quote.ConfirmedOptionId)
                .HasColumnOrder(7);
            entity.Property(quote => quote.ConfirmIdempotencyKey)
                .HasMaxLength(IDEMPOTENCY_KEY_MAX_LENGTH)
                .HasColumnOrder(8);
            entity.Property(quote => quote.CreatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(9);
            entity.Property(quote => quote.ExpiresAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(10);
            entity.Property(quote => quote.UpdatedAt)
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(11);
            entity.Property(quote => quote.LimitAmount)
                .IsRequired()
                .HasPrecision(MONEY_PRECISION, MONEY_SCALE)
                .HasColumnOrder(12);

            entity.HasIndex(quote => new { quote.AtmId, quote.Status });
            entity.HasIndex(quote => new { quote.CardHash, quote.Currency, quote.CreatedAt });
            entity.HasIndex(quote => new { quote.Status, quote.ExpiresAt });
            entity.HasIndex(quote => quote.ConfirmIdempotencyKey)
                .IsUnique()
                .HasFilter("\"ConfirmIdempotencyKey\" IS NOT NULL");

            entity.HasOne(quote => quote.Atm)
                .WithMany()
                .HasForeignKey(quote => quote.AtmId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureWithdrawalQuoteOptionEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WithdrawalQuoteOption>();

            entity.ToTable(TableNames.WITHDRAWAL_QUOTE_OPTIONS);
            entity.HasKey(option => option.Id);
            entity.Property(option => option.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(option => option.QuoteId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(option => option.OptionType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(2);
            entity.Property(option => option.Amount)
                .IsRequired()
                .HasPrecision(MONEY_PRECISION, MONEY_SCALE)
                .HasColumnOrder(3);

            entity.HasIndex(option => new { option.QuoteId, option.OptionType }).IsUnique();

            entity.HasOne(option => option.Quote)
                .WithMany(quote => quote.Options)
                .HasForeignKey(option => option.QuoteId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureWithdrawalQuoteOptionItemEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<WithdrawalQuoteOptionItem>();

            entity.ToTable(TableNames.WITHDRAWAL_QUOTE_OPTION_ITEMS);
            entity.HasKey(item => item.Id);
            entity.Property(item => item.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(item => item.OptionId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(item => item.CassetteId)
                .IsRequired()
                .HasColumnOrder(2);
            entity.Property(item => item.Denomination)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnOrder(3);
            entity.Property(item => item.NoteCount)
                .IsRequired()
                .HasColumnOrder(4);

            entity.HasIndex(item => item.OptionId);
            entity.HasIndex(item => item.CassetteId);

            entity.HasOne(item => item.Option)
                .WithMany(option => option.Items)
                .HasForeignKey(item => item.OptionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(item => item.Cassette)
                .WithMany()
                .HasForeignKey(item => item.CassetteId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureDepositTransactionEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DepositTransaction>();

            entity.ToTable(TableNames.DEPOSIT_TRANSACTIONS);
            entity.HasKey(deposit => deposit.Id);
            entity.Property(deposit => deposit.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(deposit => deposit.AtmId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(deposit => deposit.UnrecognisedNoteCount)
                .IsRequired()
                .HasColumnOrder(2);
            entity.Property(deposit => deposit.CreatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(3);

            entity.HasIndex(deposit => deposit.AtmId);

            entity.HasOne(deposit => deposit.Atm)
                .WithMany()
                .HasForeignKey(deposit => deposit.AtmId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureDepositNoteEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<DepositNote>();

            entity.ToTable(TableNames.DEPOSIT_NOTES);
            entity.HasKey(note => note.Id);
            entity.Property(note => note.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(note => note.DepositTransactionId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(note => note.Currency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(2);
            entity.Property(note => note.Denomination)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnOrder(3);
            entity.Property(note => note.NoteCount)
                .IsRequired()
                .HasColumnOrder(4);
            entity.Property(note => note.CassetteId)
                .HasColumnOrder(5);

            entity.HasIndex(note => note.DepositTransactionId);

            entity.HasOne(note => note.DepositTransaction)
                .WithMany(deposit => deposit.Notes)
                .HasForeignKey(note => note.DepositTransactionId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(note => note.Cassette)
                .WithMany()
                .HasForeignKey(note => note.CassetteId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureReconciliationEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<Reconciliation>();

            entity.ToTable(TableNames.RECONCILIATIONS);
            entity.HasKey(reconciliation => reconciliation.Id);
            entity.Property(reconciliation => reconciliation.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(reconciliation => reconciliation.AtmId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(reconciliation => reconciliation.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(2);
            entity.Property(reconciliation => reconciliation.TotalDifferenceInBaseCurrency)
                .IsRequired()
                .HasPrecision(MONEY_PRECISION, MONEY_SCALE)
                .HasColumnOrder(3);
            entity.Property(reconciliation => reconciliation.CreatedBy)
                .IsRequired()
                .HasMaxLength(USER_NAME_MAX_LENGTH)
                .HasColumnOrder(4);
            entity.Property(reconciliation => reconciliation.CreatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(5);
            entity.Property(reconciliation => reconciliation.ApprovedBy)
                .HasMaxLength(USER_NAME_MAX_LENGTH)
                .HasColumnOrder(6);
            entity.Property(reconciliation => reconciliation.ApprovedAt)
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(7);

            entity.HasIndex(reconciliation => new { reconciliation.AtmId, reconciliation.Status });

            entity.HasOne(reconciliation => reconciliation.Atm)
                .WithMany()
                .HasForeignKey(reconciliation => reconciliation.AtmId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureReconciliationLineEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ReconciliationLine>();

            entity.ToTable(TableNames.RECONCILIATION_LINES);
            entity.HasKey(line => line.Id);
            entity.Property(line => line.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(line => line.ReconciliationId)
                .IsRequired()
                .HasColumnOrder(1);
            entity.Property(line => line.CassetteId)
                .IsRequired()
                .HasColumnOrder(2);
            entity.Property(line => line.Currency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(3);
            entity.Property(line => line.Denomination)
                .IsRequired()
                .HasConversion<int>()
                .HasColumnOrder(4);
            entity.Property(line => line.SystemCount)
                .IsRequired()
                .HasColumnOrder(5);
            entity.Property(line => line.CountedCount)
                .IsRequired()
                .HasColumnOrder(6);
            entity.Property(line => line.Difference)
                .IsRequired()
                .HasColumnOrder(7);

            entity.HasIndex(line => line.ReconciliationId);

            entity.HasOne(line => line.Reconciliation)
                .WithMany(reconciliation => reconciliation.Lines)
                .HasForeignKey(line => line.ReconciliationId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(line => line.Cassette)
                .WithMany()
                .HasForeignKey(line => line.CassetteId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        private void ConfigureExchangeRateEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<ExchangeRate>();

            entity.ToTable(TableNames.EXCHANGE_RATES);
            entity.HasKey(exchangeRate => exchangeRate.Id);
            entity.Property(exchangeRate => exchangeRate.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(exchangeRate => exchangeRate.Currency)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(1);
            entity.Property(exchangeRate => exchangeRate.RateToBaseCurrency)
                .IsRequired()
                .HasPrecision(MONEY_PRECISION, RATE_SCALE)
                .HasColumnOrder(2);
            entity.Property(exchangeRate => exchangeRate.UpdatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(3);

            entity.HasIndex(exchangeRate => exchangeRate.Currency).IsUnique();

            entity.HasData(
                new ExchangeRate
                {
                    Id = new Guid("0f0a1b6e-3c11-4d6a-9a52-5b1d6a7c0001"),
                    Currency = Currency.Try,
                    RateToBaseCurrency = 1m,
                    UpdatedAt = ExchangeRateSeedDate
                },
                new ExchangeRate
                {
                    Id = new Guid("0f0a1b6e-3c11-4d6a-9a52-5b1d6a7c0002"),
                    Currency = Currency.Usd,
                    RateToBaseCurrency = 42m,
                    UpdatedAt = ExchangeRateSeedDate
                },
                new ExchangeRate
                {
                    Id = new Guid("0f0a1b6e-3c11-4d6a-9a52-5b1d6a7c0003"),
                    Currency = Currency.Eur,
                    RateToBaseCurrency = 49m,
                    UpdatedAt = ExchangeRateSeedDate
                });
        }

        private void ConfigureOutboxEventEntity(ModelBuilder modelBuilder)
        {
            var entity = modelBuilder.Entity<OutboxEvent>();

            entity.ToTable(TableNames.OUTBOX_EVENTS);
            entity.HasKey(outboxEvent => outboxEvent.Id);
            entity.Property(outboxEvent => outboxEvent.Id)
                .ValueGeneratedNever()
                .HasColumnOrder(0);
            entity.Property(outboxEvent => outboxEvent.EventType)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(1);
            entity.Property(outboxEvent => outboxEvent.Payload)
                .IsRequired()
                .HasColumnType("jsonb")
                .HasColumnOrder(2);
            entity.Property(outboxEvent => outboxEvent.Status)
                .IsRequired()
                .HasConversion<string>()
                .HasMaxLength(ENUM_MAX_LENGTH)
                .HasColumnOrder(3);
            entity.Property(outboxEvent => outboxEvent.CreatedAt)
                .IsRequired()
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(4);
            entity.Property(outboxEvent => outboxEvent.SentAt)
                .HasColumnType(TIMESTAMP_WITH_TIME_ZONE)
                .HasColumnOrder(5);

            entity.HasIndex(outboxEvent => new { outboxEvent.Status, outboxEvent.CreatedAt });
        }
    }
}
