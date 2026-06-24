using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCardMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CardMappings",
                columns: table => new
                {
                    CardUid = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CardMappings", x => x.CardUid);
                    table.ForeignKey(
                        name: "FK_CardMappings_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CardMappings_BorrowerId",
                table: "CardMappings",
                column: "BorrowerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CardMappings");
        }
    }
}
