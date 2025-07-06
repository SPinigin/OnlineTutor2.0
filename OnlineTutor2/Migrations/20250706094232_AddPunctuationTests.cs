using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OnlineTutor2.Migrations
{
    /// <inheritdoc />
    public partial class AddPunctuationTests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PunctuationTests",
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
                    ShowCorrectAnswers = table.Column<bool>(type: "bit", nullable: false),
                    TestCategoryId1 = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PunctuationTests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PunctuationTests_AspNetUsers_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PunctuationTests_Classes_ClassId",
                        column: x => x.ClassId,
                        principalTable: "Classes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PunctuationTests_TestCategories_TestCategoryId",
                        column: x => x.TestCategoryId,
                        principalTable: "TestCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PunctuationTests_TestCategories_TestCategoryId1",
                        column: x => x.TestCategoryId1,
                        principalTable: "TestCategories",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PunctuationQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PunctuationTestId = table.Column<int>(type: "int", nullable: false),
                    SentenceWithNumbers = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    CorrectPositions = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    PlainSentence = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Hint = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PunctuationQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PunctuationQuestions_PunctuationTests_PunctuationTestId",
                        column: x => x.PunctuationTestId,
                        principalTable: "PunctuationTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PunctuationTestResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PunctuationTestId = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("PK_PunctuationTestResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PunctuationTestResults_PunctuationTests_PunctuationTestId",
                        column: x => x.PunctuationTestId,
                        principalTable: "PunctuationTests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PunctuationTestResults_Students_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Students",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PunctuationAnswers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PunctuationTestResultId = table.Column<int>(type: "int", nullable: false),
                    PunctuationQuestionId = table.Column<int>(type: "int", nullable: false),
                    StudentAnswer = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IsCorrect = table.Column<bool>(type: "bit", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PunctuationAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PunctuationAnswers_PunctuationQuestions_PunctuationQuestionId",
                        column: x => x.PunctuationQuestionId,
                        principalTable: "PunctuationQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PunctuationAnswers_PunctuationTestResults_PunctuationTestResultId",
                        column: x => x.PunctuationTestResultId,
                        principalTable: "PunctuationTestResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationAnswers_PunctuationQuestionId",
                table: "PunctuationAnswers",
                column: "PunctuationQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationAnswers_PunctuationTestResultId",
                table: "PunctuationAnswers",
                column: "PunctuationTestResultId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationQuestions_PunctuationTestId",
                table: "PunctuationQuestions",
                column: "PunctuationTestId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationTestResults_PunctuationTestId",
                table: "PunctuationTestResults",
                column: "PunctuationTestId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationTestResults_StudentId",
                table: "PunctuationTestResults",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationTests_ClassId",
                table: "PunctuationTests",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationTests_TeacherId",
                table: "PunctuationTests",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationTests_TestCategoryId",
                table: "PunctuationTests",
                column: "TestCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_PunctuationTests_TestCategoryId1",
                table: "PunctuationTests",
                column: "TestCategoryId1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PunctuationAnswers");

            migrationBuilder.DropTable(
                name: "PunctuationQuestions");

            migrationBuilder.DropTable(
                name: "PunctuationTestResults");

            migrationBuilder.DropTable(
                name: "PunctuationTests");
        }
    }
}
