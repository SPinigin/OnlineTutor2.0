$(document).ready(function () {
    initializeRegistrationForm();
});

function initializeRegistrationForm() {
    initializeRoleSelection();
    initializePhoneMask();
    initializeFormValidation();
    initializeAnimations();
}

// Инициализация выбора роли
function initializeRoleSelection() {
    // Обработка изменения радио-кнопок роли
    $('.role-radio').change(function () {
        const selectedRole = $(this).val();
        handleRoleChange(selectedRole);
    });

    // Обработка клика по карточке роли
    $('.role-card').click(function () {
        const radio = $(this).find('input[type="radio"]');
        radio.prop('checked', true).trigger('change');
    });

    // Проверка предвыбранной роли при загрузке
    const checkedRole = $('.role-radio:checked').val();
    if (checkedRole) {
        handleRoleChange(checkedRole);
    }
}

// Обработка смены роли
function handleRoleChange(selectedRole) {
    // Скрыть все специфичные поля с анимацией
    $('.role-specific-fields').slideUp(300, function () {
        $(this).hide();
    });

    // Убрать выделение со всех карточек
    $('.role-card').removeClass('selected-student selected-teacher');

    // Показать поля для выбранной роли
    setTimeout(() => {
        if (selectedRole === 'Student') {
            showStudentFields();
        } else if (selectedRole === 'Teacher') {
            showTeacherFields();
        }
    }, 300);
}

// Показать поля для студента
function showStudentFields() {
    $('#studentFields')
        .removeClass('teacher-fields')
        .addClass('student-fields')
        .slideDown(400);

    $('.role-card').has('#roleStudent').addClass('selected-student');

    // Обновить алерт
    updateRoleAlert('student');
}

// Показать поля для учителя
function showTeacherFields() {
    $('#teacherFields')
        .removeClass('student-fields')
        .addClass('teacher-fields')
        .slideDown(400);

    $('.role-card').has('#roleTeacher').addClass('selected-teacher');

    // Обновить алерт
    updateRoleAlert('teacher');
}

// Обновление алерта в зависимости от роли
function updateRoleAlert(role) {
    const studentAlert = $('#studentFields .alert-role-info');
    const teacherAlert = $('#teacherFields .alert-role-info');

    if (role === 'student') {
        studentAlert.removeClass('teacher-alert').addClass('student-alert');
    } else if (role === 'teacher') {
        teacherAlert.removeClass('student-alert').addClass('teacher-alert');
    }
}

// Инициализация маски для телефона
function initializePhoneMask() {
    $('#PhoneNumber').on('input', function () {
        formatPhoneNumber(this);
    });

    // Добавление контейнера для флага
    const phoneInput = $('#PhoneNumber');
    if (!phoneInput.parent().hasClass('phone-input-container')) {
        phoneInput.wrap('<div class="phone-input-container"></div>');
    }
}

// Форматирование номера телефона
function formatPhoneNumber(input) {
    let value = input.value.replace(/\D/g, '');
    let formattedValue = '';

    if (value.length > 0) {
        // Убираем первую цифру если это 8 или 7
        if (value.startsWith('8') || value.startsWith('7')) {
            value = value.substring(1);
        }

        if (value.length <= 3) {
            formattedValue = '+7 (' + value;
        } else if (value.length <= 6) {
            formattedValue = '+7 (' + value.substring(0, 3) + ') ' + value.substring(3);
        } else if (value.length <= 8) {
            formattedValue = '+7 (' + value.substring(0, 3) + ') ' + value.substring(3, 6) + '-' + value.substring(6);
        } else {
            formattedValue = '+7 (' + value.substring(0, 3) + ') ' + value.substring(3, 6) + '-' + value.substring(6, 8) + '-' + value.substring(8, 10);
        }
    }

    input.value = formattedValue;
}

// Инициализация валидации формы
function initializeFormValidation() {
    const form = $('#registrationForm');

    // Валидация при отправке формы
    form.on('submit', function (e) {
        if (!validateForm()) {
            e.preventDefault();
            showValidationErrors();
        }
    });

    // Валидация полей в реальном времени
    $('input, select, textarea').on('blur', function () {
        validateField($(this));
    });

    // Удаление ошибок при вводе
    $('input, select, textarea').on('input', function () {
        clearFieldError($(this));
    });
}

// Валидация всей формы
function validateForm() {
    let isValid = true;
    const requiredFields = $('input[required], select[required]');

    requiredFields.each(function () {
        if (!validateField($(this))) {
            isValid = false;
        }
    });

    // Дополнительные проверки
    if (!validatePasswordMatch()) {
        isValid = false;
    }

    if (!validateAge()) {
        isValid = false;
    }

    return isValid;
}

// Валидация отдельного поля
function validateField(field) {
    const value = field.val().trim();
    const fieldType = field.attr('type');
    let isValid = true;

    // Проверка обязательных полей
    if (field.attr('required') && !value) {
        showFieldError(field, 'Это поле обязательно для заполнения');
        isValid = false;
    }

    // Специфичные проверки
    if (value) {
        switch (fieldType) {
            case 'email':
                if (!isValidEmail(value)) {
                    showFieldError(field, 'Введите корректный email');
                    isValid = false;
                }
                break;
            case 'tel':
                if (!isValidPhone(value)) {
                    showFieldError(field, 'Введите корректный номер телефона');
                    isValid = false;
                }
                break;
            case 'date':
                if (!isValidDate(field)) {
                    showFieldError(field, 'Введите корректную дату');
                    isValid = false;
                }
                break;
        }
    }

    if (isValid) {
        clearFieldError(field);
        field.addClass('is-valid');
    }

    return isValid;
}

// Проверка совпадения паролей
function validatePasswordMatch() {
    const password = $('#Password').val();
    const confirmPassword = $('#ConfirmPassword').val();

    if (password && confirmPassword && password !== confirmPassword) {
        showFieldError($('#ConfirmPassword'), 'Пароли не совпадают');
        return false;
    }

    return true;
}

// Проверка возраста
function validateAge() {
    const dateOfBirth = new Date($('#DateOfBirth').val());
    const today = new Date();
    const age = today.getFullYear() - dateOfBirth.getFullYear();

    if (age < 5 || age > 100) {
        showFieldError($('#DateOfBirth'), 'Проверьте правильность даты рождения');
        return false;
    }

    return true;
}

// Показать ошибку поля
function showFieldError(field, message) {
    field.removeClass('is-valid').addClass('is-invalid');

    let errorElement = field.siblings('.field-error');
    if (errorElement.length === 0) {
        errorElement = $('<div class="field-error text-danger small mt-1"></div>');
        field.after(errorElement);
    }

    errorElement.text(message).show();
}

// Очистить ошибку поля
function clearFieldError(field) {
    field.removeClass('is-invalid');
    field.siblings('.field-error').hide();
}

// Показать общие ошибки валидации
function showValidationErrors() {
    $('html, body').animate({
        scrollTop: $('.is-invalid').first().offset().top - 100
    }, 500);

    // Показать уведомление
    showNotification('Пожалуйста, исправьте ошибки в форме', 'error');
}

// Вспомогательные функции валидации
function isValidEmail(email) {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
}

function isValidPhone(phone) {
    const cleanPhone = phone.replace(/\D/g, '');
    return cleanPhone.length === 11;
}

function isValidDate(field) {
    const date = new Date(field.val());
    return date instanceof Date && !isNaN(date);
}

// Инициализация анимаций
function initializeAnimations() {
    // Анимация появления карточек при загрузке
    $('.role-card').each(function (index) {
        $(this).delay(index * 100).animate({
            opacity: 1,
            transform: 'translateY(0)'
        }, 300);
    });

    // Анимация фокуса на полях
    $('input, select, textarea').on('focus', function () {
        $(this).parent().addClass('focused');
    }).on('blur', function () {
        $(this).parent().removeClass('focused');
    });
}

// Показать уведомление
function showNotification(message, type = 'info') {
    const alertClass = type === 'error' ? 'alert-danger' : 'alert-info';
    const icon = type === 'error' ? 'fas fa-exclamation-triangle' : 'fas fa-info-circle';

    const notification = $(`
        <div class="alert ${alertClass} alert-dismissible fade show notification-alert" role="alert">
            <i class="${icon}"></i> ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `);

    $('.container').first().prepend(notification);

    // Автоматическое скрытие через 5 секунд
    setTimeout(() => {
        notification.alert('close');
    }, 5000);
}

// Дополнительные утилиты
function resetForm() {
    $('#registrationForm')[0].reset();
    $('.role-specific-fields').hide();
    $('.role-card').removeClass('selected-student selected-teacher');
    $('input, select, textarea').removeClass('is-valid is-invalid');
    $('.field-error').hide();
}

// Экспорт функций для использования в других скриптах
window.RegistrationForm = {
    reset: resetForm,
    validate: validateForm,
    showNotification: showNotification
};
