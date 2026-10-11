using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Fimple.Atm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "t_Atms",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Atms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_ExchangeRates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RateToBaseCurrency = table.Column<decimal>(type: "numeric(18,6)", precision: 18, scale: 6, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_ExchangeRates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_OutboxEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Payload = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_OutboxEvents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "t_Cassettes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    Kind = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Denomination = table.Column<int>(type: "integer", nullable: true),
                    Capacity = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Cassettes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_Cassettes_t_Atms_AtmId",
                        column: x => x.AtmId,
                        principalTable: "t_Atms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_DepositTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtmId = table.Column<Guid>(type: "uuid", nullable: false),
                    UnrecognisedNoteCount = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_DepositTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_DepositTransactions_t_Atms_AtmId",
                        column: x => x.AtmId,
                        principalTable: "t_Atms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_Reconciliations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtmId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    TotalDifferenceInBaseCurrency = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    CreatedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_Reconciliations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_Reconciliations_t_Atms_AtmId",
                        column: x => x.AtmId,
                        principalTable: "t_Atms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_WithdrawalQuotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AtmId = table.Column<Guid>(type: "uuid", nullable: false),
                    CardHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    MaskedCardNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RequestedAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ConfirmedOptionId = table.Column<Guid>(type: "uuid", nullable: true),
                    ConfirmIdempotencyKey = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_WithdrawalQuotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_WithdrawalQuotes_t_Atms_AtmId",
                        column: x => x.AtmId,
                        principalTable: "t_Atms",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_CassetteContents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CassetteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Denomination = table.Column<int>(type: "integer", nullable: false),
                    NoteCount = table.Column<int>(type: "integer", nullable: false),
                    ReservedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_CassetteContents", x => x.Id);
                    table.CheckConstraint("CK_t_CassetteContents_NoteCount", "\"NoteCount\" >= 0");
                    table.CheckConstraint("CK_t_CassetteContents_ReservedCount", "\"ReservedCount\" >= 0 AND \"ReservedCount\" <= \"NoteCount\"");
                    table.ForeignKey(
                        name: "FK_t_CassetteContents_t_Cassettes_CassetteId",
                        column: x => x.CassetteId,
                        principalTable: "t_Cassettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_CassetteMovements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CassetteId = table.Column<Guid>(type: "uuid", nullable: false),
                    MovementType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Denomination = table.Column<int>(type: "integer", nullable: false),
                    NoteCountDelta = table.Column<int>(type: "integer", nullable: false),
                    ReservedCountDelta = table.Column<int>(type: "integer", nullable: false),
                    ReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    PerformedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_CassetteMovements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_CassetteMovements_t_Cassettes_CassetteId",
                        column: x => x.CassetteId,
                        principalTable: "t_Cassettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_DepositNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DepositTransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Denomination = table.Column<int>(type: "integer", nullable: false),
                    NoteCount = table.Column<int>(type: "integer", nullable: false),
                    CassetteId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_DepositNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_DepositNotes_t_Cassettes_CassetteId",
                        column: x => x.CassetteId,
                        principalTable: "t_Cassettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_DepositNotes_t_DepositTransactions_DepositTransactionId",
                        column: x => x.DepositTransactionId,
                        principalTable: "t_DepositTransactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_ReconciliationLines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReconciliationId = table.Column<Guid>(type: "uuid", nullable: false),
                    CassetteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Currency = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Denomination = table.Column<int>(type: "integer", nullable: false),
                    SystemCount = table.Column<int>(type: "integer", nullable: false),
                    CountedCount = table.Column<int>(type: "integer", nullable: false),
                    Difference = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_ReconciliationLines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_ReconciliationLines_t_Cassettes_CassetteId",
                        column: x => x.CassetteId,
                        principalTable: "t_Cassettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_ReconciliationLines_t_Reconciliations_ReconciliationId",
                        column: x => x.ReconciliationId,
                        principalTable: "t_Reconciliations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_WithdrawalQuoteOptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    QuoteId = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionType = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_WithdrawalQuoteOptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_WithdrawalQuoteOptions_t_WithdrawalQuotes_QuoteId",
                        column: x => x.QuoteId,
                        principalTable: "t_WithdrawalQuotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "t_WithdrawalQuoteOptionItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    CassetteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Denomination = table.Column<int>(type: "integer", nullable: false),
                    NoteCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_t_WithdrawalQuoteOptionItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_t_WithdrawalQuoteOptionItems_t_Cassettes_CassetteId",
                        column: x => x.CassetteId,
                        principalTable: "t_Cassettes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_t_WithdrawalQuoteOptionItems_t_WithdrawalQuoteOptions_Optio~",
                        column: x => x.OptionId,
                        principalTable: "t_WithdrawalQuoteOptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "t_ExchangeRates",
                columns: new[] { "Id", "Currency", "RateToBaseCurrency", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("0f0a1b6e-3c11-4d6a-9a52-5b1d6a7c0001"), "Try", 1m, new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0f0a1b6e-3c11-4d6a-9a52-5b1d6a7c0002"), "Usd", 42m, new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("0f0a1b6e-3c11-4d6a-9a52-5b1d6a7c0003"), "Eur", 49m, new DateTime(2026, 10, 11, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.CreateIndex(
                name: "IX_t_Atms_Name",
                table: "t_Atms",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_CassetteContents_CassetteId_Currency_Denomination",
                table: "t_CassetteContents",
                columns: new[] { "CassetteId", "Currency", "Denomination" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_CassetteMovements_CassetteId_CreatedAt",
                table: "t_CassetteMovements",
                columns: new[] { "CassetteId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_t_CassetteMovements_ReferenceId",
                table: "t_CassetteMovements",
                column: "ReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_t_Cassettes_AtmId_Position",
                table: "t_Cassettes",
                columns: new[] { "AtmId", "Position" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_DepositNotes_CassetteId",
                table: "t_DepositNotes",
                column: "CassetteId");

            migrationBuilder.CreateIndex(
                name: "IX_t_DepositNotes_DepositTransactionId",
                table: "t_DepositNotes",
                column: "DepositTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_t_DepositTransactions_AtmId",
                table: "t_DepositTransactions",
                column: "AtmId");

            migrationBuilder.CreateIndex(
                name: "IX_t_ExchangeRates_Currency",
                table: "t_ExchangeRates",
                column: "Currency",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_OutboxEvents_Status_CreatedAt",
                table: "t_OutboxEvents",
                columns: new[] { "Status", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_t_ReconciliationLines_CassetteId",
                table: "t_ReconciliationLines",
                column: "CassetteId");

            migrationBuilder.CreateIndex(
                name: "IX_t_ReconciliationLines_ReconciliationId",
                table: "t_ReconciliationLines",
                column: "ReconciliationId");

            migrationBuilder.CreateIndex(
                name: "IX_t_Reconciliations_AtmId_Status",
                table: "t_Reconciliations",
                columns: new[] { "AtmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_t_WithdrawalQuoteOptionItems_CassetteId",
                table: "t_WithdrawalQuoteOptionItems",
                column: "CassetteId");

            migrationBuilder.CreateIndex(
                name: "IX_t_WithdrawalQuoteOptionItems_OptionId",
                table: "t_WithdrawalQuoteOptionItems",
                column: "OptionId");

            migrationBuilder.CreateIndex(
                name: "IX_t_WithdrawalQuoteOptions_QuoteId_OptionType",
                table: "t_WithdrawalQuoteOptions",
                columns: new[] { "QuoteId", "OptionType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_t_WithdrawalQuotes_AtmId_Status",
                table: "t_WithdrawalQuotes",
                columns: new[] { "AtmId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_t_WithdrawalQuotes_CardHash_Currency_CreatedAt",
                table: "t_WithdrawalQuotes",
                columns: new[] { "CardHash", "Currency", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_t_WithdrawalQuotes_ConfirmIdempotencyKey",
                table: "t_WithdrawalQuotes",
                column: "ConfirmIdempotencyKey",
                unique: true,
                filter: "\"ConfirmIdempotencyKey\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_t_WithdrawalQuotes_Status_ExpiresAt",
                table: "t_WithdrawalQuotes",
                columns: new[] { "Status", "ExpiresAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "t_CassetteContents");

            migrationBuilder.DropTable(
                name: "t_CassetteMovements");

            migrationBuilder.DropTable(
                name: "t_DepositNotes");

            migrationBuilder.DropTable(
                name: "t_ExchangeRates");

            migrationBuilder.DropTable(
                name: "t_OutboxEvents");

            migrationBuilder.DropTable(
                name: "t_ReconciliationLines");

            migrationBuilder.DropTable(
                name: "t_WithdrawalQuoteOptionItems");

            migrationBuilder.DropTable(
                name: "t_DepositTransactions");

            migrationBuilder.DropTable(
                name: "t_Reconciliations");

            migrationBuilder.DropTable(
                name: "t_Cassettes");

            migrationBuilder.DropTable(
                name: "t_WithdrawalQuoteOptions");

            migrationBuilder.DropTable(
                name: "t_WithdrawalQuotes");

            migrationBuilder.DropTable(
                name: "t_Atms");
        }
    }
}
