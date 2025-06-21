using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineTutor2.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IconClass = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ColorClass = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SpellingTests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TestCategoryId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    TeacherId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClassId = table.Column<int>(type: "int", nullable: true),
                    TimeLimit = table.Column<int>(type: "int", nullable: false),
                    MaxAttempts = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    ShowHints = table.Column<bool>(type: "bit", nullable: false),
                    ShowCorrectAnswers = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpellingTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpellingTests_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpellingTests_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_SpellingTests_TestCategories_TestCategoryId",
                        column: x => x.TestCategoryId,
                        principalTable: "TestCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SpellingQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpellingTestId = table.Column<int>(type: "int", nullable: false),
                    WordWithGap = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CorrectLetter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    FullWord = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Hint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpellingQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpellingQuestions_SpellingTests_SpellingTestId",
                        column: x => x.SpellingTestId,
                        principalTable: "SpellingTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpellingTestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpellingTestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_SpellingTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpellingTestResults_SpellingTests_SpellingTestId",
                        column: x => x.SpellingTestId,
                        principalTable: "SpellingTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_SpellingTestResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SpellingAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpellingTestResultId = table.Column<int>(type: "int", nullable: false),
                    SpellingQuestionId = table.Column<int>(type: "int", nullable: false),
                    StudentAnswer = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SpellingAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SpellingAnswers_SpellingQuestions_SpellingQuestionId",
                        column: x => x.SpellingQuestionId,
                        principalTable: "SpellingQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SpellingAnswers_SpellingTestResults_SpellingTestResultId",
                        column: x => x.SpellingTestResultId,
                        principalTable: "SpellingTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SpellingAnswers_SpellingQuestionId",
                table: "SpellingAnswers",
                column: "SpellingQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellingAnswers_SpellingTestResultId",
                table: "SpellingAnswers",
                column: "SpellingTestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellingQuestions_SpellingTestId",
                table: "SpellingQuestions",
                column: "SpellingTestId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellingTestResults_SpellingTestId",
                table: "SpellingTestResults",
                column: "SpellingTestId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellingTestResults_StudentId",
                table: "SpellingTestResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellingTests_ClassId",
                table: "SpellingTests",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellingTests_TeacherId",
                table: "SpellingTests",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_SpellingTests_TestCategoryId",
                table: "SpellingTests",
                column: "TestCategoryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SpellingAnswers");

            migrationBuilder.DropTable(
                name: "SpellingQuestions");

            migrationBuilder.DropTable(
                name: "SpellingTestResults");

            migrationBuilder.DropTable(
                name: "SpellingTests");

            migrationBuilder.DropTable(
                name: "TestCategories");
        }
    }
}
