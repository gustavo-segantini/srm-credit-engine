using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SrmCreditEngine.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "credit");

            migrationBuilder.CreateTable(
                name: "cedents",
                schema: "credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    cnpj = table.Column<string>(type: "character(14)", fixedLength: true, maxLength: 14, nullable: false),
                    contact_email = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cedents", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "currencies",
                schema: "credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    symbol = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    decimal_places = table.Column<int>(type: "integer", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_currencies", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "receivables",
                schema: "credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    cedent_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_number = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    type = table.Column<int>(type: "integer", nullable: false),
                    face_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    face_currency = table.Column<int>(type: "integer", nullable: false),
                    due_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    submitted_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_receivables", x => x.id);
                    table.ForeignKey(
                        name: "fk_receivables_cedents_cedent_id",
                        column: x => x.cedent_id,
                        principalSchema: "credit",
                        principalTable: "cedents",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "exchange_rates",
                schema: "credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    to_currency_id = table.Column<Guid>(type: "uuid", nullable: false),
                    rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    expires_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    source = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_exchange_rates", x => x.id);
                    table.ForeignKey(
                        name: "fk_exchange_rates_currencies_from_currency_id",
                        column: x => x.from_currency_id,
                        principalSchema: "credit",
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_exchange_rates_currencies_to_currency_id",
                        column: x => x.to_currency_id,
                        principalSchema: "credit",
                        principalTable: "currencies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "settlements",
                schema: "credit",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    receivable_id = table.Column<Guid>(type: "uuid", nullable: false),
                    face_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    face_currency = table.Column<int>(type: "integer", nullable: false),
                    base_rate = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    applied_spread = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    term_in_months = table.Column<int>(type: "integer", nullable: false),
                    present_value = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    discount = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    payment_currency = table.Column<int>(type: "integer", nullable: false),
                    net_disbursement = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    exchange_rate_applied = table.Column<decimal>(type: "numeric(18,8)", precision: 18, scale: 8, nullable: false),
                    status = table.Column<int>(type: "integer", nullable: false),
                    settled_at = table.Column<DateTime>(type: "timestamptz", nullable: true),
                    failure_reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamptz", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamptz", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_settlements", x => x.id);
                    table.ForeignKey(
                        name: "fk_settlements_receivables_receivable_id",
                        column: x => x.receivable_id,
                        principalSchema: "credit",
                        principalTable: "receivables",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                schema: "credit",
                table: "cedents",
                columns: new[] { "id", "cnpj", "contact_email", "created_at", "is_active", "name", "updated_at" },
                values: new object[] { new Guid("33333333-0000-0000-0000-000000000001"), "12345678000199", "ops@srmasset.com.br", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), true, "SRM Asset Management LTDA", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.InsertData(
                schema: "credit",
                table: "currencies",
                columns: new[] { "id", "code", "created_at", "decimal_places", "is_active", "name", "symbol", "updated_at" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000001"), 1, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "Real Brasileiro", "R$", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("11111111-0000-0000-0000-000000000002"), 2, new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), 2, true, "US Dollar", "$", new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });

            migrationBuilder.InsertData(
                schema: "credit",
                table: "exchange_rates",
                columns: new[] { "id", "created_at", "effective_date", "expires_at", "from_currency_id", "rate", "source", "to_currency_id", "updated_at" },
                values: new object[] { new Guid("22222222-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new Guid("11111111-0000-0000-0000-000000000002"), 5.75m, "SEED", new Guid("11111111-0000-0000-0000-000000000001"), new DateTime(2025, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.CreateIndex(
                name: "ix_cedents_cnpj",
                schema: "credit",
                table: "cedents",
                column: "cnpj",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_currencies_code",
                schema: "credit",
                table: "currencies",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_pair_date",
                schema: "credit",
                table: "exchange_rates",
                columns: new[] { "from_currency_id", "to_currency_id", "effective_date" });

            migrationBuilder.CreateIndex(
                name: "ix_exchange_rates_to_currency_id",
                schema: "credit",
                table: "exchange_rates",
                column: "to_currency_id");

            migrationBuilder.CreateIndex(
                name: "ix_receivables_cedent_id",
                schema: "credit",
                table: "receivables",
                column: "cedent_id");

            migrationBuilder.CreateIndex(
                name: "ix_receivables_doc_cedent",
                schema: "credit",
                table: "receivables",
                columns: new[] { "document_number", "cedent_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_receivables_due_date",
                schema: "credit",
                table: "receivables",
                column: "due_date");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_created_at",
                schema: "credit",
                table: "settlements",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_payment_currency",
                schema: "credit",
                table: "settlements",
                column: "payment_currency");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_receivable_id",
                schema: "credit",
                table: "settlements",
                column: "receivable_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_settlements_settled_at",
                schema: "credit",
                table: "settlements",
                column: "settled_at");

            migrationBuilder.CreateIndex(
                name: "ix_settlements_status",
                schema: "credit",
                table: "settlements",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "exchange_rates",
                schema: "credit");

            migrationBuilder.DropTable(
                name: "settlements",
                schema: "credit");

            migrationBuilder.DropTable(
                name: "currencies",
                schema: "credit");

            migrationBuilder.DropTable(
                name: "receivables",
                schema: "credit");

            migrationBuilder.DropTable(
                name: "cedents",
                schema: "credit");
        }
    }
}
