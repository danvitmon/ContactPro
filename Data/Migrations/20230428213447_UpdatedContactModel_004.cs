using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContactPro.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedContactModel_004 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateOFBirth",
                table: "Contacts",
                newName: "DateOfBirth");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DateOfBirth",
                table: "Contacts",
                newName: "DateOFBirth");
        }
    }
}
