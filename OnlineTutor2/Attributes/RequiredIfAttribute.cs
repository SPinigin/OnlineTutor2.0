using System.ComponentModel.DataAnnotations;

namespace OnlineTutor2.Attributes
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class RequiredIfAttribute : ValidationAttribute
    {
        private readonly string _dependentProperty;
        private readonly object _targetValue;

        public RequiredIfAttribute(string dependentProperty, object targetValue)
        {
            _dependentProperty = dependentProperty;
            _targetValue = targetValue;
            ErrorMessage = ErrorMessage ?? "Это поле обязательно";
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            var propertyInfo = validationContext.ObjectType.GetProperty(_dependentProperty);

            if (propertyInfo == null)
            {
                return ValidationResult.Success; // Если свойство не найдено, пропускаем валидацию
            }

            var dependentValue = propertyInfo.GetValue(validationContext.ObjectInstance);

            // Проверяем, соответствует ли зависимое значение целевому
            bool shouldValidate = false;

            if (_targetValue is bool targetBool && dependentValue is bool dependentBool)
            {
                shouldValidate = targetBool == dependentBool;
            }
            else
            {
                shouldValidate = Equals(dependentValue, _targetValue);
            }

            if (shouldValidate)
            {
                // Если условие выполнено, проверяем значение
                if (value == null || (value is string str && string.IsNullOrWhiteSpace(str)))
                {
                    return new ValidationResult(ErrorMessage);
                }
            }

            return ValidationResult.Success;
        }
    }
}
