using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Models
{
    // Связь SpellingTest <-> Class
    public class SpellingTestClass
    {
        public int Id { get; set; }
        public int SpellingTestId { get; set; }
        public int ClassId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public virtual SpellingTest SpellingTest { get; set; } = null!;
        public virtual Class Class { get; set; } = null!;
    }

    // Связь PunctuationTest <-> Class
    public class PunctuationTestClass
    {
        public int Id { get; set; }
        public int PunctuationTestId { get; set; }
        public int ClassId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public virtual PunctuationTest PunctuationTest { get; set; } = null!;
        public virtual Class Class { get; set; } = null!;
    }

    // Связь OrthoeopyTest <-> Class
    public class OrthoeopyTestClass
    {
        public int Id { get; set; }
        public int OrthoeopyTestId { get; set; }
        public int ClassId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public virtual OrthoeopyTest OrthoeopyTest { get; set; } = null!;
        public virtual Class Class { get; set; } = null!;
    }

    // Связь RegularTest <-> Class
    public class RegularTestClass
    {
        public int Id { get; set; }
        public int RegularTestId { get; set; }
        public int ClassId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.Now;

        public virtual RegularTest RegularTest { get; set; } = null!;
        public virtual Class Class { get; set; } = null!;
    }
}
