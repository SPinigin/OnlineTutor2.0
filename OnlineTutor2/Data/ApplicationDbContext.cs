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

        #region DbSets

        // Users and Profiles
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Student> Students { get; set; }

        // Classes and Education
        public DbSet<Class> Classes { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Grade> Grades { get; set; }
        public DbSet<Material> Materials { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<CalendarEvent> CalendarEvents { get; set; }

        // Test Categories
        public DbSet<TestCategory> TestCategories { get; set; }

        // Regular Tests
        public DbSet<RegularTest> RegularTests { get; set; }
        public DbSet<RegularQuestion> RegularQuestions { get; set; }
        public DbSet<RegularTestResult> RegularTestResults { get; set; }
        public DbSet<RegularAnswer> RegularAnswers { get; set; }
        public DbSet<RegularQuestionOption> RegularQuestionOptions { get; set; }

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

        // Many-to-Many junction tables
        public DbSet<SpellingTestClass> SpellingTestClasses { get; set; }
        public DbSet<PunctuationTestClass> PunctuationTestClasses { get; set; }
        public DbSet<OrthoeopyTestClass> OrthoeopyTestClasses { get; set; }
        public DbSet<RegularTestClass> RegularTestClasses { get; set; }

        #endregion

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            #region ApplicationUser

            modelBuilder.Entity<ApplicationUser>()
                .HasIndex(u => u.Email)
                .IsUnique();

            #endregion

            #region Teacher and Student

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

            modelBuilder.Entity<Student>()
                .Property(s => s.StudentNumber)
                .HasMaxLength(50)
                .IsRequired(false);

            #endregion

            #region Class

            modelBuilder.Entity<Class>()
                .HasOne(c => c.Teacher)
                .WithMany(u => u.TeacherClasses)
                .HasForeignKey(c => c.TeacherId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Restrict);

            #endregion

            #region CalendarEvent

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

            #endregion

            #region Assignment

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

            #endregion

            #region Grade

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

            #endregion

            #region Material

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

            #endregion

            #region RegularTest

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

            #endregion

            #region RegularQuestion

            modelBuilder.Entity<RegularQuestion>()
                .HasOne(q => q.RegularTest)
                .WithMany(t => t.RegularQuestions)
                .HasForeignKey(q => q.TestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region RegularTestResult

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

            #endregion

            #region RegularAnswer

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

            #endregion

            #region RegularQuestionOption

            modelBuilder.Entity<RegularQuestionOption>()
                .HasOne(rqo => rqo.Question)
                .WithMany(rq => rq.Options)
                .HasForeignKey(rqo => rqo.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegularQuestionOption>()
                .HasIndex(rqo => new { rqo.QuestionId, rqo.OrderIndex });

            #endregion

            #region SpellingTest

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

            #endregion

            #region SpellingQuestion

            modelBuilder.Entity<SpellingQuestion>()
                .HasOne(sq => sq.SpellingTest)
                .WithMany(st => st.SpellingQuestions)
                .HasForeignKey(sq => sq.SpellingTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region SpellingTestResult

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

            #endregion

            #region SpellingAnswer

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

            #endregion

            #region PunctuationTest

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


            #endregion

            #region PunctuationQuestion

            modelBuilder.Entity<PunctuationQuestion>()
                .HasOne(pq => pq.PunctuationTest)
                .WithMany(pt => pt.PunctuationQuestions)
                .HasForeignKey(pq => pq.PunctuationTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region PunctuationTestResult

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

            #endregion

            #region PunctuationAnswer

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

            #endregion

            #region OrthoeopyTest

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

            #endregion

            #region OrthoeopyQuestion

            modelBuilder.Entity<OrthoeopyQuestion>()
                .HasOne(oq => oq.OrthoeopyTest)
                .WithMany(ot => ot.OrthoeopyQuestions)
                .HasForeignKey(oq => oq.OrthoeopyTestId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            #endregion

            #region OrthoeopyTestResult

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

            #endregion

            #region OrthoeopyAnswer

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

            #endregion

            #region Many-to-Many Relationships (Test <-> Class)

            // SpellingTest <-> Class (Many-to-Many)
            modelBuilder.Entity<SpellingTestClass>()
                .HasKey(stc => new { stc.SpellingTestId, stc.ClassId });

            modelBuilder.Entity<SpellingTestClass>()
                .HasOne(stc => stc.SpellingTest)
                .WithMany(st => st.TestClasses)
                .HasForeignKey(stc => stc.SpellingTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingTestClass>()
                .HasOne(stc => stc.Class)
                .WithMany(c => c.SpellingTests)
                .HasForeignKey(stc => stc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SpellingTestClass>()
                .HasIndex(stc => stc.SpellingTestId);

            modelBuilder.Entity<SpellingTestClass>()
                .HasIndex(stc => stc.ClassId);

            // PunctuationTest <-> Class (Many-to-Many)
            modelBuilder.Entity<PunctuationTestClass>()
                .HasKey(ptc => new { ptc.PunctuationTestId, ptc.ClassId });

            modelBuilder.Entity<PunctuationTestClass>()
                .HasOne(ptc => ptc.PunctuationTest)
                .WithMany(pt => pt.TestClasses)
                .HasForeignKey(ptc => ptc.PunctuationTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PunctuationTestClass>()
                .HasOne(ptc => ptc.Class)
                .WithMany(c => c.PunctuationTests)
                .HasForeignKey(ptc => ptc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<PunctuationTestClass>()
                .HasIndex(ptc => ptc.PunctuationTestId);

            modelBuilder.Entity<PunctuationTestClass>()
                .HasIndex(ptc => ptc.ClassId);

            // OrthoeopyTest <-> Class (Many-to-Many)
            modelBuilder.Entity<OrthoeopyTestClass>()
                .HasKey(otc => new { otc.OrthoeopyTestId, otc.ClassId });

            modelBuilder.Entity<OrthoeopyTestClass>()
                .HasOne(otc => otc.OrthoeopyTest)
                .WithMany(ot => ot.TestClasses)
                .HasForeignKey(otc => otc.OrthoeopyTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrthoeopyTestClass>()
                .HasOne(otc => otc.Class)
                .WithMany(c => c.OrthoeopyTests)
                .HasForeignKey(otc => otc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrthoeopyTestClass>()
                .HasIndex(otc => otc.OrthoeopyTestId);

            modelBuilder.Entity<OrthoeopyTestClass>()
                .HasIndex(otc => otc.ClassId);

            // RegularTest <-> Class (Many-to-Many)
            modelBuilder.Entity<RegularTestClass>()
                .HasKey(rtc => new { rtc.RegularTestId, rtc.ClassId });

            modelBuilder.Entity<RegularTestClass>()
                .HasOne(rtc => rtc.RegularTest)
                .WithMany(rt => rt.TestClasses)
                .HasForeignKey(rtc => rtc.RegularTestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegularTestClass>()
                .HasOne(rtc => rtc.Class)
                .WithMany(c => c.RegularTests)
                .HasForeignKey(rtc => rtc.ClassId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RegularTestClass>()
                .HasIndex(rtc => rtc.RegularTestId);

            modelBuilder.Entity<RegularTestClass>()
                .HasIndex(rtc => rtc.ClassId);

            #endregion

            #region AuditLog

            modelBuilder.Entity<AuditLog>()
                .HasOne(al => al.User)
                .WithMany()
                .HasForeignKey(al => al.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.Action);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.EntityType);

            modelBuilder.Entity<AuditLog>()
                .HasIndex(al => al.CreatedAt);

            #endregion
        }
    }
}
