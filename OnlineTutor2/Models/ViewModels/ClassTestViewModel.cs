using OnlineTutor2.Models;

namespace OnlineTutor2.ViewModels
{
    public class ClassTestViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public string TestType { get; set; } // "Spelling", "Regular", "Grammar", другие
        public string TypeDisplayName { get; set; }
        public string IconClass { get; set; }
        public string ColorClass { get; set; }
        public string ControllerName { get; set; }

        // Специфичные данные в зависимости от типа теста
        public int QuestionsCount { get; set; }
        public int TimeLimit { get; set; }
        public int ResultsCount { get; set; }
        public int MaxAttempts { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        // Фабричные методы для создания из разных типов тестов
        public static ClassTestViewModel FromSpellingTest(SpellingTest spellingTest)
        {
            return new ClassTestViewModel
            {
                Id = spellingTest.Id,
                Title = spellingTest.Title,
                Description = spellingTest.Description,
                CreatedAt = spellingTest.CreatedAt,
                IsActive = spellingTest.IsActive,
                TestType = "Spelling",
                TypeDisplayName = "Орфография",
                IconClass = "fas fa-spell-check",
                ColorClass = "primary",
                ControllerName = "SpellingTest",
                QuestionsCount = spellingTest.Questions?.Count ?? 0,
                TimeLimit = spellingTest.TimeLimit,
                ResultsCount = spellingTest.TestResults?.Count ?? 0,
                MaxAttempts = spellingTest.MaxAttempts,
                StartDate = spellingTest.StartDate,
                EndDate = spellingTest.EndDate
            };
        }

        public static ClassTestViewModel FromRegularTest(Test test)
        {
            return new ClassTestViewModel
            {
                Id = test.Id,
                Title = test.Title,
                Description = test.Description,
                CreatedAt = test.CreatedAt,
                IsActive = test.IsActive,
                TestType = "Regular",
                TypeDisplayName = "Обычный тест",
                IconClass = "fas fa-tasks",
                ColorClass = "info",
                ControllerName = "Test",
                QuestionsCount = test.Questions?.Count ?? 0,
                TimeLimit = test.TimeLimit,
                ResultsCount = test.TestResults?.Count ?? 0,
                MaxAttempts = test.MaxAttempts,
                StartDate = test.StartDate,
                EndDate = test.EndDate
            };
        }

        // добавить:
        // public static ClassTestViewModel FromGrammarTest(GrammarTest grammarTest) { ... }
        // public static ClassTestViewModel FromEssayTest(EssayTest essayTest) { ... }
    }
}
