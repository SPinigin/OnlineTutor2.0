using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Attributes
{
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _dependentProperty;
        private readonly object _targetValue;

        public RequiredIfAttribute(string dependentProperty, object targetValue)
        {
            _dependentProperty = dependentProperty;
            _targetValue = targetValue;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var propertyInfo = validationContext.ObjectType.GetProperty(_dependentProperty);
            if (propertyInfo == null)
            {
                return new ValidationResult($"Свойство {_dependentProperty} не найдено");
            }

            var dependentValue = propertyInfo.GetValue(validationContext.ObjectInstance);

            if (dependentValue != null && dependentValue.Equals(_targetValue))
            {
                if (value == null || string.IsNullOrWhiteSpace(value.ToString()))
                {
                    return new ValidationResult(ErrorMessage ?? $"Поле обязательно");
                }
            }

            return ValidationResult.Success;
        }
    }
}
