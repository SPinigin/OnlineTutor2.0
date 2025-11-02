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

        // DbSets
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }

        // Test Categories
        public DbSet<TestCategory> TestCategories { get; set; }

        // RegularTests
        public DbSet<RegularTest> RegularTests { get; set; }
        public DbSet<RegularQuestion> RegularQuestions { get; set; }
        public DbSet<RegularTestResult> RegularTestResults { get; set; }
        public DbSet<RegularAnswer> RegularAnswers { get; set; }

        // SpellingTests
        public DbSet<SpellingTest> SpellingTests { get; set; }
        public DbSet<SpellingQuestion> SpellingQuestions { get; set; }
        public DbSet<SpellingTestResult> SpellingTestResults { get; set; }
        public DbSet<SpellingAnswer> SpellingAnswers { get; set; }

        // PunctuationTests
        public DbSet<PunctuationTest> PunctuationTests { get; set; }
        public DbSet<PunctuationQuestion> PunctuationQuestions { get; set; }
        public DbSet<PunctuationTestResult> PunctuationTestResults { get; set; }
        public DbSet<PunctuationAnswer> PunctuationAnswers { get; set; }

        // OrthoeopyTests
        public DbSet<OrthoeopyTest> OrthoeopyTests { get; set; }
        public DbSet<OrthoeopyQuestion> OrthoeopyQuestions { get; set; }
        public DbSet<OrthoeopyTestResult> OrthoeopyTestResults { get; set; }
        public DbSet<OrthoeopyAnswer> OrthoeopyAnswers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ApplicationUser
            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Teacher
            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.User)
                .WithOne(u => u.TeacherProfile)
                .HasForeignKey<Teacher>(t => t.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Student
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // Class
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Teacher)
                .WithMany(u => u.TeacherClasses)
                .HasForeignKey(c => c.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            // CalendarEvent
            modelBuilder.Entity<CalendarEvent>()
                .HasOne(ce => ce.Teacher)
                .WithMany()
                .HasForeignKey(ce => ce.TeacherId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(ce => ce.Class)
                .WithMany()
                .HasForeignKey(ce => ce.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(ce => ce.Student)
                .WithMany()
                .HasForeignKey(ce => ce.StudentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Assignment
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

            // Grade
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

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Test)
                .WithMany()
                .HasForeignKey(g => g.TestId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Grade>()
                .Property(g => g.Percentage)
                .HasPrecision(5, 2);

            // Material
            modelBuilder.Entity<Material>()
                .HasOne(m => m.UploadedBy)
                .WithMany()
                .HasForeignKey(m => m.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Material>()
                .HasOne(m => m.Class)
                .WithMany(c => c.Materials)
                .HasForeignKey(m => m.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // RegularTest
            modelBuilder.Entity<RegularTest>()
                .HasOne(t => t.Teacher)
                .WithMany(u => u.CreatedTests)
                .HasForeignKey(t => t.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegularTest>()
                .HasOne(t => t.Class)
                .WithMany(c => c.Tests)
                .HasForeignKey(t => t.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // RegularQuestion
            modelBuilder.Entity<RegularQuestion>()
                .HasOne(q => q.RegularTest)
                .WithMany(t => t.RegularQuestions)
                .HasForeignKey(q => q.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            // RegularTestResult
            modelBuilder.Entity<RegularTestResult>()
                .HasOne(tr => tr.RegularTest)
                .WithMany(t => t.RegularTestResults)
                .HasForeignKey(tr => tr.TestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegularTestResult>()
                .HasOne(tr => tr.Student)
                .WithMany(s => s.TestResults)
                .HasForeignKey(tr => tr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // RegularAnswer
            modelBuilder.Entity<RegularAnswer>()
                .HasOne(a => a.RegularTestResult)
                .WithMany(tr => tr.RegularAnswers)
                .HasForeignKey(a => a.TestResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegularAnswer>()
                .HasOne(a => a.RegularQuestion)
                .WithMany(q => q.RegularAnswers)
                .HasForeignKey(a => a.QuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // SpellingTest
            modelBuilder.Entity<SpellingTest>()
                .HasOne(st => st.TestCategory)
                .WithMany(tc => tc.SpellingTests)
                .HasForeignKey(st => st.TestCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // SpellingQuestion
            modelBuilder.Entity<SpellingQuestion>()
                .HasOne(sq => sq.SpellingTest)
                .WithMany(st => st.SpellingQuestions)
                .HasForeignKey(sq => sq.SpellingTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // SpellingTestResult
            modelBuilder.Entity<SpellingTestResult>()
                .HasOne(str => str.SpellingTest)
                .WithMany(st => st.SpellingTestResults)
                .HasForeignKey(str => str.SpellingTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingTestResult>()
                .HasOne(str => str.Student)
                .WithMany()
                .HasForeignKey(str => str.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // SpellingAnswer
            modelBuilder.Entity<SpellingAnswer>()
                .HasOne(sa => sa.SpellingTestResult)
                .WithMany(str => str.SpellingAnswers)
                .HasForeignKey(sa => sa.SpellingTestResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingAnswer>()
                .HasOne(sa => sa.SpellingQuestion)
                .WithMany(sq => sq.StudentAnswers)
                .HasForeignKey(sa => sa.SpellingQuestionId)
                .OnDelete(DeleteBehavior.Restrict);

            // PunctuationTest
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

            // PunctuationQuestion
            modelBuilder.Entity<PunctuationQuestion>()
                .HasOne(pq => pq.PunctuationTest)
                .WithMany(pt => pt.Questions)
                .HasForeignKey(pq => pq.PunctuationTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // PunctuationTestResult
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

            // PunctuationAnswer
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

            // OrthoeopyTest
            modelBuilder.Entity<OrthoeopyTest>()
                .HasOne(ot => ot.TestCategory)
                .WithMany(tc => tc.OrthoeopyTests)
                .HasForeignKey(ot => ot.TestCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrthoeopyTest>()
                .HasOne(ot => ot.Teacher)
                .WithMany()
                .HasForeignKey(ot => ot.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrthoeopyTest>()
                .HasOne(ot => ot.Class)
                .WithMany()
                .HasForeignKey(ot => ot.ClassId)
                .OnDelete(DeleteBehavior.SetNull);

            // OrthoeopyQuestion
            modelBuilder.Entity<OrthoeopyQuestion>()
                .HasOne(oq => oq.OrthoeopyTest)
                .WithMany(ot => ot.OrthoeopyQuestions)
                .HasForeignKey(oq => oq.OrthoeopyTestId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrthoeopyTestResult
            modelBuilder.Entity<OrthoeopyTestResult>()
                .HasOne(otr => otr.OrthoeopyTest)
                .WithMany(ot => ot.OrthoeopyTestResults)
                .HasForeignKey(otr => otr.OrthoeopyTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrthoeopyTestResult>()
                .HasOne(otr => otr.Student)
                .WithMany()
                .HasForeignKey(otr => otr.StudentId)
                .OnDelete(DeleteBehavior.Cascade);

            // OrthoeopyAnswer
            modelBuilder.Entity<OrthoeopyAnswer>()
                .HasOne(oa => oa.OrthoeopyTestResult)
                .WithMany(otr => otr.OrthoeopyAnswers)
                .HasForeignKey(oa => oa.OrthoeopyTestResultId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrthoeopyAnswer>()
                .HasOne(oa => oa.OrthoeopyQuestion)
                .WithMany(oq => oq.OrthoeopyAnswers)
                .HasForeignKey(oa => oa.OrthoeopyQuestionId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
