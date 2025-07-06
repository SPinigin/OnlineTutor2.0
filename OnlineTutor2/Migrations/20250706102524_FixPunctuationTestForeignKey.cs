using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineTutor2.Migrations
{
    /// <inheritdoc />
    public partial class FixPunctuationTestForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PunctuationTests_TestCategories_TestCategoryId1",
                table: "PunctuationTests");

            migrationBuilder.DropIndex(
                name: "IX_PunctuationTests_TestCategoryId1",
                table: "PunctuationTests");

            migrationBuilder.DropColumn(
                name: "TestCategoryId1",
                table: "PunctuationTests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "TestCategoryId1",
                table: "PunctuationTests",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationTests_TestCategoryId1",
                table: "PunctuationTests",
                column: "TestCategoryId1");

            migrationBuilder.AddForeignKey(
                name: "FK_PunctuationTests_TestCategories_TestCategoryId1",
                table: "PunctuationTests",
                column: "TestCategoryId1",
                principalTable: "TestCategories",
                principalColumn: "Id");
        }
    }
}
