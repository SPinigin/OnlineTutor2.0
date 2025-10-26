using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineTutor2.Migrations
{
    /// <inheritdoc />
    public partial class AddOrthoeopyTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "ClassId",
                table: "Materials",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateTable(
                name: "OrthoeopyTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TestCategoryId = table.Column<int>(type: "int", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: true),
                    TimeLimit = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ShowHints = table.Column<bool>(type: "bit", nullable: false),
                    ShowCorrectAnswers = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoeopyTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrthoeopyTests_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrthoeopyTests_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_OrthoeopyTests_TestCategories_TestCategoryId",
                        column: x => x.TestCategoryId,
                        principalTable: "TestCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OrthoeopyQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrthoeopyTestId = table.Column<int>(type: "int", nullable: false),
                    Word = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StressPosition = table.Column<int>(type: "int", nullable: false),
                    WordWithStress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    WrongStressPositions = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoeopyQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrthoeopyQuestions_OrthoeopyTests_OrthoeopyTestId",
                        column: x => x.OrthoeopyTestId,
                        principalTable: "OrthoeopyTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrthoeopyTestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrthoeopyTestId = table.Column<int>(type: "int", nullable: false),
                    StudentId = table.Column<int>(type: "int", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Score = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<int>(type: "int", nullable: false),
                    Percentage = table.Column<double>(type: "float", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    AttemptNumber = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoeopyTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrthoeopyTestResults_OrthoeopyTests_OrthoeopyTestId",
                        column: x => x.OrthoeopyTestId,
                        principalTable: "OrthoeopyTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OrthoeopyTestResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrthoeopyAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrthoeopyTestResultId = table.Column<int>(type: "int", nullable: false),
                    OrthoeopyQuestionId = table.Column<int>(type: "int", nullable: false),
                    SelectedStressPosition = table.Column<int>(type: "int", nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrthoeopyAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrthoeopyAnswers_OrthoeopyQuestions_OrthoeopyQuestionId",
                        column: x => x.OrthoeopyQuestionId,
                        principalTable: "OrthoeopyQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OrthoeopyAnswers_OrthoeopyTestResults_OrthoeopyTestResultId",
                        column: x => x.OrthoeopyTestResultId,
                        principalTable: "OrthoeopyTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyAnswers_OrthoeopyQuestionId",
                table: "OrthoeopyAnswers",
                column: "OrthoeopyQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyAnswers_OrthoeopyTestResultId",
                table: "OrthoeopyAnswers",
                column: "OrthoeopyTestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyQuestions_OrthoeopyTestId",
                table: "OrthoeopyQuestions",
                column: "OrthoeopyTestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyTestResults_OrthoeopyTestId",
                table: "OrthoeopyTestResults",
                column: "OrthoeopyTestId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyTestResults_StudentId",
                table: "OrthoeopyTestResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyTests_ClassId",
                table: "OrthoeopyTests",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyTests_TeacherId",
                table: "OrthoeopyTests",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_OrthoeopyTests_TestCategoryId",
                table: "OrthoeopyTests",
                column: "TestCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrthoeopyAnswers");

            migrationBuilder.DropTable(
                name: "OrthoeopyQuestions");

            migrationBuilder.DropTable(
                name: "OrthoeopyTestResults");

            migrationBuilder.DropTable(
                name: "OrthoeopyTests");

            migrationBuilder.AlterColumn<int>(
                name: "ClassId",
                table: "Materials",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);
        }
    }
}
