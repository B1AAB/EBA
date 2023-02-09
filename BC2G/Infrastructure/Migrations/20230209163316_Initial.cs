using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BC2G.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Utxo",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false),
                    Address = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<double>(type: "double precision", nullable: false),
                    ScriptType = table.Column<int>(type: "integer", nullable: false),
                    CreatedIn = table.Column<string>(type: "text", nullable: false),
                    CreatedInCount = table.Column<int>(type: "integer", nullable: false),
                    ReferencedIn = table.Column<string>(type: "text", nullable: false),
                    ReferencedInCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Utxo", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Utxo");
        }
    }
}
