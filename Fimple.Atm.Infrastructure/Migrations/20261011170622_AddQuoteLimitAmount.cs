using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fimple.Atm.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddQuoteLimitAmount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "LimitAmount",
                table: "t_WithdrawalQuotes",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m)
                .Annotation("Relational:ColumnOrder", 12);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LimitAmount",
                table: "t_WithdrawalQuotes");
        }
    }
}
