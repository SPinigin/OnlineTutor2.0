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

        // Users and Profiles
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }

        // Classes and Education
        public DbSet<Class> Classes { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }

        // Test Categories
        public DbSet<TestCategory> TestCategories { get; set; }

        // Regular Tests
        public DbSet<RegularTest> RegularTests { get; set; }
        public DbSet<RegularQuestion> RegularQuestions { get; set; }
        public DbSet<RegularTestResult> RegularTestResults { get; set; }
        public DbSet<RegularAnswer> RegularAnswers { get; set; }

        // Spelling Tests
        public DbSet<SpellingTest> SpellingTests { get; set; }
        public DbSet<SpellingQuestion> SpellingQuestions { get; set; }
        public DbSet<SpellingTestResult> SpellingTestResults { get; set; }
        public DbSet<SpellingAnswer> SpellingAnswers { get; set; }

        // Punctuation Tests
        public DbSet<PunctuationTest> PunctuationTests { get; set; }
        public DbSet<PunctuationQuestion> PunctuationQuestions { get; set; }
        public DbSet<PunctuationTestResult> PunctuationTestResults { get; set; }
        public DbSet<PunctuationAnswer> PunctuationAnswers { get; set; }

        // Orthoeopy Tests
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
                .OnDelete(DeleteBehavior.Restrict);

            // Student
            modelBuilder.Entity<Student>()
                .HasOne(s => s.User)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<Student>(s => s.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Student>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Students)
                .HasForeignKey(s => s.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Class
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Teacher)
                .WithMany(u => u.TeacherClasses)
                .HasForeignKey(c => c.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // CalendarEvent
            modelBuilder.Entity<CalendarEvent>()
                .HasOne(ce => ce.Teacher)
                .WithMany()
                .HasForeignKey(ce => ce.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(ce => ce.Class)
                .WithMany()
                .HasForeignKey(ce => ce.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<CalendarEvent>()
                .HasOne(ce => ce.Student)
                .WithMany()
                .HasForeignKey(ce => ce.StudentId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // Assignment
            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Teacher)
                .WithMany()
                .HasForeignKey(a => a.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Assignment>()
                .HasOne(a => a.Class)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.ClassId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Grade
            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Student)
                .WithMany(s => s.Grades)
                .HasForeignKey(g => g.StudentId)
                .IsRequired()
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
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Grade>()
                .HasOne(g => g.Test)
                .WithMany()
                .HasForeignKey(g => g.TestId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<Grade>()
                .Property(g => g.Percentage)
                .HasPrecision(5, 2);

            // Material
            modelBuilder.Entity<Material>()
                .HasOne(m => m.UploadedBy)
                .WithMany()
                .HasForeignKey(m => m.UploadedById)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Material>()
                .HasOne(m => m.Class)
                .WithMany(c => c.Materials)
                .HasForeignKey(m => m.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // RegularTest
            modelBuilder.Entity<RegularTest>()
                .HasOne(rt => rt.Teacher)
                .WithMany(u => u.CreatedTests)
                .HasForeignKey(rt => rt.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegularTest>()
                .HasOne(rt => rt.TestCategory)
                .WithMany(tc => tc.RegularTests)
                .HasForeignKey(rt => rt.TestCategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RegularTest>()
                .HasOne(rt => rt.Class)
                .WithMany(c => c.RegularTests)
                .HasForeignKey(rt => rt.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // RegularQuestion
            modelBuilder.Entity<RegularQuestion>()
                .HasOne(q => q.RegularTest)
                .WithMany(t => t.RegularQuestions)
                .HasForeignKey(q => q.TestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // RegularTestResult
            modelBuilder.Entity<RegularTestResult>()
                .HasOne(tr => tr.RegularTest)
                .WithMany(t => t.RegularTestResults)
                .HasForeignKey(tr => tr.RegularTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegularTestResult>()
                .HasOne(tr => tr.Student)
                .WithMany(s => s.RegularTestResults)
                .HasForeignKey(tr => tr.StudentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // RegularAnswer
            modelBuilder.Entity<RegularAnswer>()
                .HasOne(a => a.RegularTestResult)
                .WithMany(tr => tr.RegularAnswers)
                .HasForeignKey(a => a.TestResultId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegularAnswer>()
                .HasOne(a => a.RegularQuestion)
                .WithMany(q => q.RegularAnswers)
                .HasForeignKey(a => a.QuestionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // SpellingTest
            modelBuilder.Entity<SpellingTest>()
                .HasOne(st => st.Teacher)
                .WithMany()
                .HasForeignKey(st => st.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SpellingTest>()
                .HasOne(st => st.TestCategory)
                .WithMany(tc => tc.SpellingTests)
                .HasForeignKey(st => st.TestCategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<SpellingTest>()
                .HasOne(st => st.Class)
                .WithMany(c => c.SpellingTests)
                .HasForeignKey(st => st.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // SpellingQuestion
            modelBuilder.Entity<SpellingQuestion>()
                .HasOne(sq => sq.SpellingTest)
                .WithMany(st => st.SpellingQuestions)
                .HasForeignKey(sq => sq.SpellingTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // SpellingTestResult
            modelBuilder.Entity<SpellingTestResult>()
                .HasOne(str => str.SpellingTest)
                .WithMany(st => st.SpellingTestResults)
                .HasForeignKey(str => str.SpellingTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingTestResult>()
                .HasOne(str => str.Student)
                .WithMany(s => s.SpellingTestResults)
                .HasForeignKey(str => str.StudentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // SpellingAnswer
            modelBuilder.Entity<SpellingAnswer>()
                .HasOne(sa => sa.SpellingTestResult)
                .WithMany(str => str.SpellingAnswers)
                .HasForeignKey(sa => sa.SpellingTestResultId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingAnswer>()
                .HasOne(sa => sa.SpellingQuestion)
                .WithMany(sq => sq.StudentAnswers)
                .HasForeignKey(sa => sa.SpellingQuestionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // PunctuationTest
            modelBuilder.Entity<PunctuationTest>()
                .HasOne(pt => pt.Teacher)
                .WithMany()
                .HasForeignKey(pt => pt.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PunctuationTest>()
                .HasOne(pt => pt.TestCategory)
                .WithMany(tc => tc.PunctuationTests)
                .HasForeignKey(pt => pt.TestCategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<PunctuationTest>()
                .HasOne(pt => pt.Class)
                .WithMany(c => c.PunctuationTests)
                .HasForeignKey(pt => pt.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // PunctuationQuestion
            modelBuilder.Entity<PunctuationQuestion>()
                .HasOne(pq => pq.PunctuationTest)
                .WithMany(pt => pt.PunctuationQuestions)
                .HasForeignKey(pq => pq.PunctuationTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // PunctuationTestResult
            modelBuilder.Entity<PunctuationTestResult>()
                .HasOne(ptr => ptr.PunctuationTest)
                .WithMany(pt => pt.PunctuationTestResults)
                .HasForeignKey(ptr => ptr.PunctuationTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PunctuationTestResult>()
                .HasOne(ptr => ptr.Student)
                .WithMany(s => s.PunctuationTestResults)
                .HasForeignKey(ptr => ptr.StudentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // PunctuationAnswer
            modelBuilder.Entity<PunctuationAnswer>()
                .HasOne(pa => pa.PunctuationTestResult)
                .WithMany(ptr => ptr.PunctuationAnswers)
                .HasForeignKey(pa => pa.PunctuationTestResultId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PunctuationAnswer>()
                .HasOne(pa => pa.PunctuationQuestion)
                .WithMany(pq => pq.StudentAnswers)
                .HasForeignKey(pa => pa.PunctuationQuestionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            // OrthoeopyTest
            modelBuilder.Entity<OrthoeopyTest>()
                .HasOne(ot => ot.Teacher)
                .WithMany()
                .HasForeignKey(ot => ot.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrthoeopyTest>()
                .HasOne(ot => ot.TestCategory)
                .WithMany(tc => tc.OrthoeopyTests)
                .HasForeignKey(ot => ot.TestCategoryId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrthoeopyTest>()
                .HasOne(ot => ot.Class)
                .WithMany(c => c.OrthoeopyTests)
                .HasForeignKey(ot => ot.ClassId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);

            // OrthoeopyQuestion
            modelBuilder.Entity<OrthoeopyQuestion>()
                .HasOne(oq => oq.OrthoeopyTest)
                .WithMany(ot => ot.OrthoeopyQuestions)
                .HasForeignKey(oq => oq.OrthoeopyTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // OrthoeopyTestResult
            modelBuilder.Entity<OrthoeopyTestResult>()
                .HasOne(otr => otr.OrthoeopyTest)
                .WithMany(ot => ot.OrthoeopyTestResults)
                .HasForeignKey(otr => otr.OrthoeopyTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrthoeopyTestResult>()
                .HasOne(otr => otr.Student)
                .WithMany(s => s.OrthoeopyTestResults)
                .HasForeignKey(otr => otr.StudentId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // OrthoeopyAnswer
            modelBuilder.Entity<OrthoeopyAnswer>()
                .HasOne(oa => oa.OrthoeopyTestResult)
                .WithMany(otr => otr.OrthoeopyAnswers)
                .HasForeignKey(oa => oa.OrthoeopyTestResultId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrthoeopyAnswer>()
                .HasOne(oa => oa.OrthoeopyQuestion)
                .WithMany(oq => oq.OrthoeopyAnswers)
                .HasForeignKey(oa => oa.OrthoeopyQuestionId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
