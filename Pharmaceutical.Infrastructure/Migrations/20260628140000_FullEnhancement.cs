using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Metadata;
using Pharmaceutical.Infrastructure;

#nullable disable

namespace Pharmaceutical.Infrastructure.Migrations
{
    [DbContext(typeof(PharmaceuticalDbContext))]
    [Migration("20260628140000_FullEnhancement")]
    public partial class FullEnhancement : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "is_active",
            table: "drugs",
            type: "tinyint(1)",
            nullable: false,
            defaultValue: true);

        migrationBuilder.AlterColumn<string>(
            name: "transaction_type",
            table: "stock_transactions",
            type: "varchar(20)",
            maxLength: 20,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "varchar(10)",
            oldMaxLength: 10);

        migrationBuilder.CreateTable(
            name: "audit_logs",
            columns: table => new
            {
                audit_id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                entity_type = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                entity_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                user_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                old_values = table.Column<string>(type: "longtext", nullable: true),
                new_values = table.Column<string>(type: "longtext", nullable: true),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_audit_logs", x => x.audit_id));

        migrationBuilder.CreateTable(
            name: "drug_batches",
            columns: table => new
            {
                batch_id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                drug_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                batch_number = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                manufacture_date = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                expiry_date = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                quantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_drug_batches", x => x.batch_id);
                table.ForeignKey(
                    name: "FK_drug_batches_drugs_drug_id",
                    column: x => x.drug_id,
                    principalTable: "drugs",
                    principalColumn: "drug_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "purchase_orders",
            columns: table => new
            {
                order_id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                supplier_id = table.Column<int>(type: "int", nullable: false),
                status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false),
                created_by = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false),
                created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                approved_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_purchase_orders", x => x.order_id);
                table.ForeignKey(
                    name: "FK_purchase_orders_suppliers_supplier_id",
                    column: x => x.supplier_id,
                    principalTable: "suppliers",
                    principalColumn: "supplier_id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "purchase_order_lines",
            columns: table => new
            {
                line_id = table.Column<int>(type: "int", nullable: false)
                    .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                order_id = table.Column<int>(type: "int", nullable: false),
                drug_id = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false),
                quantity = table.Column<int>(type: "int", nullable: false),
                received_quantity = table.Column<int>(type: "int", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_purchase_order_lines", x => x.line_id);
                table.ForeignKey(
                    name: "FK_purchase_order_lines_drugs_drug_id",
                    column: x => x.drug_id,
                    principalTable: "drugs",
                    principalColumn: "drug_id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_purchase_order_lines_purchase_orders_order_id",
                    column: x => x.order_id,
                    principalTable: "purchase_orders",
                    principalColumn: "order_id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_drug_batches_drug_id_batch_number",
            table: "drug_batches",
            columns: new[] { "drug_id", "batch_number" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_purchase_order_lines_drug_id",
            table: "purchase_order_lines",
            column: "drug_id");

        migrationBuilder.CreateIndex(
            name: "IX_purchase_order_lines_order_id",
            table: "purchase_order_lines",
            column: "order_id");

        migrationBuilder.CreateIndex(
            name: "IX_purchase_orders_supplier_id",
            table: "purchase_orders",
            column: "supplier_id");

        migrationBuilder.AddForeignKey(
            name: "FK_drugs_suppliers_supplier_id",
            table: "drugs",
            column: "supplier_id",
            principalTable: "suppliers",
            principalColumn: "supplier_id",
            onDelete: ReferentialAction.Restrict);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropForeignKey(name: "FK_drugs_suppliers_supplier_id", table: "drugs");
        migrationBuilder.DropTable(name: "audit_logs");
        migrationBuilder.DropTable(name: "drug_batches");
        migrationBuilder.DropTable(name: "purchase_order_lines");
        migrationBuilder.DropTable(name: "purchase_orders");
        migrationBuilder.DropColumn(name: "is_active", table: "drugs");
    }
    }
}
