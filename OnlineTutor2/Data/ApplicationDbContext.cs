using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using OnlineTutor2.Models;

namespace OnlineTutor2.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Test> Tests { get; set; }
        public DbSet<Question> Questions { get; set; }
        public DbSet<Answer> Answers { get; set; }
        public DbSet<TestResult> TestResults { get; set; }
        public DbSet<StudentAnswer> StudentAnswers { get; set; }
        public DbSet<Assignment> Assignments { get; set; } //???
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<TestCategory> TestCategories { get; set; }
        public DbSet<SpellingTest> SpellingTests { get; set; }
        public DbSet<SpellingQuestion> SpellingQuestions { get; set; }
        public DbSet<SpellingTestResult> SpellingTestResults { get; set; }
        public DbSet<SpellingAnswer> SpellingAnswers { get; set; }
        public DbSet<PunctuationTest> PunctuationTests { get; set; }
        public DbSet<PunctuationQuestion> PunctuationQuestions { get; set; }
        public DbSet<PunctuationTestResult> PunctuationTestResults { get; set; }
        public DbSet<PunctuationAnswer> PunctuationAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Настройка связей для Teacher
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.TeacherProfile)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для Student
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для Class
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Teacher)
                .WithMany(u => u.TeacherClasses)
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связей для Test
            modelBuilder.Entity<Test>()
                .HasOne(t => t.Teacher)
                .WithMany(u => u.CreatedTests)
                .HasForeignKey(t => t.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Test>()
                .HasOne(t => t.Class)
                .WithMany(c => c.Tests)
                .HasForeignKey(t => t.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связей для Question
            modelBuilder.Entity<Question>()
                .HasOne(q => q.Test)
                .WithMany(t => t.Questions)
                .HasForeignKey(q => q.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для Answer
            modelBuilder.Entity<Answer>()
                .HasOne(a => a.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для TestResult
            modelBuilder.Entity<TestResult>()
                .HasOne(tr => tr.Test)
                .WithMany(t => t.TestResults)
                .HasForeignKey(tr => tr.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<TestResult>()
                .HasOne(tr => tr.Student)
                .WithMany(s => s.TestResults)
                .HasForeignKey(tr => tr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для StudentAnswer
            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.TestResult)
                .WithMany(tr => tr.StudentAnswers)
                .HasForeignKey(sa => sa.TestResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany(q => q.StudentAnswers)
                .HasForeignKey(sa => sa.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<StudentAnswer>()
                .HasOne(sa => sa.Answer)
                .WithMany()
                .HasForeignKey(sa => sa.AnswerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связей для Assignment
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Class)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для Grade
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Teacher)
                .WithMany()
                .HasForeignKey(g => g.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Assignment)
                .WithMany(a => a.Grades)
                .HasForeignKey(g => g.AssignmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связей для Material
            modelBuilder.Entity<Material>()
                .HasOne(m => m.UploadedBy)
                .WithMany()
                .HasForeignKey(m => m.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Material>()
                .HasOne(m => m.Class)
                .WithMany(c => c.Materials)
                .HasForeignKey(m => m.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей Student -> Class
            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // Дополнительные настройки
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Настройка точности для decimal полей
            modelBuilder.Entity<Grade>()
                .Property(g => g.Percentage)
                .HasPrecision(5, 2);

            // Настройка связей для TestCategory
            modelBuilder.Entity<SpellingTest>()
                .HasOne(st => st.TestCategory)
                .WithMany(tc => tc.SpellingTests)
                .HasForeignKey(st => st.TestCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связей для SpellingTest
            modelBuilder.Entity<SpellingTest>()
                .HasOne(st => st.Teacher)
                .WithMany()
                .HasForeignKey(st => st.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SpellingTest>()
                .HasOne(st => st.Class)
                .WithMany()
                .HasForeignKey(st => st.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связей для SpellingQuestion
            modelBuilder.Entity<SpellingQuestion>()
                .HasOne(sq => sq.SpellingTest)
                .WithMany(st => st.Questions)
                .HasForeignKey(sq => sq.SpellingTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для SpellingTestResult
            modelBuilder.Entity<SpellingTestResult>()
                .HasOne(str => str.SpellingTest)
                .WithMany(st => st.TestResults)
                .HasForeignKey(str => str.SpellingTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingTestResult>()
                .HasOne(str => str.Student)
                .WithMany()
                .HasForeignKey(str => str.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для SpellingAnswer
            modelBuilder.Entity<SpellingAnswer>()
                .HasOne(sa => sa.TestResult)
                .WithMany(str => str.Answers)
                .HasForeignKey(sa => sa.SpellingTestResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingAnswer>()
                .HasOne(sa => sa.Question)
                .WithMany(sq => sq.StudentAnswers)
                .HasForeignKey(sa => sa.SpellingQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // Настройка связей для PunctuationTest
            modelBuilder.Entity<PunctuationTest>()
                .HasOne(pt => pt.TestCategory)
                .WithMany(tc => tc.PunctuationTests)
                .HasForeignKey(pt => pt.TestCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PunctuationTest>()
                .HasOne(pt => pt.Teacher)
                .WithMany()
                .HasForeignKey(pt => pt.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PunctuationTest>()
                .HasOne(pt => pt.Class)
                .WithMany()
                .HasForeignKey(pt => pt.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // Настройка связей для PunctuationQuestion
            modelBuilder.Entity<PunctuationQuestion>()
                .HasOne(pq => pq.PunctuationTest)
                .WithMany(pt => pt.Questions)
                .HasForeignKey(pq => pq.PunctuationTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для PunctuationTestResult
            modelBuilder.Entity<PunctuationTestResult>()
                .HasOne(ptr => ptr.PunctuationTest)
                .WithMany(pt => pt.TestResults)
                .HasForeignKey(ptr => ptr.PunctuationTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PunctuationTestResult>()
                .HasOne(ptr => ptr.Student)
                .WithMany()
                .HasForeignKey(ptr => ptr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связей для PunctuationAnswer
            modelBuilder.Entity<PunctuationAnswer>()
                .HasOne(pa => pa.TestResult)
                .WithMany(ptr => ptr.Answers)
                .HasForeignKey(pa => pa.PunctuationTestResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PunctuationAnswer>()
                .HasOne(pa => pa.Question)
                .WithMany(pq => pq.StudentAnswers)
                .HasForeignKey(pa => pa.PunctuationQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
