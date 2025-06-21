namespace OnlineTutor2.Models
{
    public static class ApplicationRoles
    {
        public const string Admin = "Admin";
        public const string Teacher = "Teacher";
        public const string Student = "Student";

        public static readonly string[] AllRoles = { Admin, Teacher, Student };
    }
}
