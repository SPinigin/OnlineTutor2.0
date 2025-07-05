//проверить и выпилить, так как уже не должен использоваться нигде


$(document).ready(function () {
    initializePasswordFields();
});

function initializePasswordFields() {
    // Инициализируем все поля паролей
    $('input[type="password"]').each(function () {
        if (!$(this).hasClass('password-initialized')) {
            setupPasswordField($(this));
        }
    });
}

function setupPasswordField(passwordField) {
    const fieldId = passwordField.attr('id') || 'password_' + Math.random().toString(36).substr(2, 9);
    passwordField.attr('id', fieldId);
    passwordField.addClass('password-initialized password-input-with-toggle');

    // Создаем контейнер если его нет
    if (!passwordField.parent().hasClass('password-input-group')) {
        passwordField.wrap('<div class="password-input-group"></div>');
    }

    const container = passwordField.parent();

    // Добавляем кнопку копирования если это поле с генерацией
    if (passwordField.hasClass('generated-password') && container.find('.password-copy-btn').length === 0) {
        const copyBtn = $(`
            <button type="button" class="password-copy-btn" data-target="${fieldId}" title="Копировать пароль">
                <i class="fas fa-copy"></i>
            </button>
        `);
        container.append(copyBtn);
        passwordField.addClass('password-input-with-copy');
        passwordField.css('padding-right', '80px');
    }

    // Добавляем индикатор силы пароля
    if (passwordField.hasClass('check-strength') && container.find('.password-strength-indicator').length === 0) {
        const strengthIndicator = $('<div class="password-strength-indicator"></div>');
        container.append(strengthIndicator);
    }
}

// Обработчик копирования пароля
$(document).on('click', '.password-copy-btn', function () {
    const targetId = $(this).data('target');
    const passwordField = $(`#${targetId}`);

    if (passwordField.val()) {
        copyToClipboard(passwordField.val());
        showPasswordNotification('Пароль скопирован в буфер обмена', 'success');
    }
});

// Проверка силы пароля
$(document).on('input', 'input[type="password"].check-strength, input[type="text"].check-strength', function () {
    const password = $(this).val();
    const container = $(this).parent();
    const strengthIndicator = container.find('.password-strength-indicator');

    if (strengthIndicator.length > 0) {
        updatePasswordStrength(password, strengthIndicator);
    }
});

function updatePasswordStrength(password, indicator) {
    const strength = calculatePasswordStrength(password);

    indicator.removeClass('password-strength-weak password-strength-medium password-strength-strong');

    if (password.length === 0) {
        indicator.hide();
        return;
    }

    indicator.show();

    if (strength < 3) {
        indicator.addClass('password-strength-weak');
    } else if (strength < 5) {
        indicator.addClass('password-strength-medium');
    } else {
        indicator.addClass('password-strength-strong');
    }
}

function calculatePasswordStrength(password) {
    let strength = 0;

    if (password.length >= 6) strength++;
    if (password.length >= 8) strength++;
    if (/[a-z]/.test(password)) strength++;
    if (/[A-Z]/.test(password)) strength++;
    if (/[0-9]/.test(password)) strength++;
    if (/[^A-Za-z0-9]/.test(password)) strength++;

    return strength;
}

// Генерация пароля
function generateSecurePassword(length = 12) {
    const lowercase = 'abcdefghijklmnopqrstuvwxyz';
    const uppercase = 'ABCDEFGHIJKLMNOPQRSTUVWXYZ';
    const numbers = '0123456789';
    const symbols = '!@#$%^&*()_+-=[]{}|;:,.<>?';

    const allChars = lowercase + uppercase + numbers + symbols;

    let password = '';

    // Гарантируем хотя бы один символ каждого типа
    password += lowercase[Math.floor(Math.random() * lowercase.length)];
    password += uppercase[Math.floor(Math.random() * uppercase.length)];
    password += numbers[Math.floor(Math.random() * numbers.length)];
    password += symbols[Math.floor(Math.random() * symbols.length)];

    // Заполняем остальную длину
    for (let i = 4; i < length; i++) {
        password += allChars[Math.floor(Math.random() * allChars.length)];
    }

    // Перемешиваем символы
    return password.split('').sort(() => Math.random() - 0.5).join('');
}

// Копирование в буфер обмена
function copyToClipboard(text) {
    if (navigator.clipboard && window.isSecureContext) {
        return navigator.clipboard.writeText(text);
    } else {
        // Fallback для старых браузеров
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        textArea.style.top = '-999999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();
        document.execCommand('copy');
        textArea.remove();
    }
}

// Уведомления
function showPasswordNotification(message, type = 'info') {
    const alertClass = type === 'success' ? 'alert-success' : 'alert-info';
    const icon = type === 'success' ? 'fas fa-check-circle' : 'fas fa-info-circle';

    const notification = $(`
        <div class="alert ${alertClass} alert-dismissible fade show password-notification" 
             style="position: fixed; top: 20px; right: 20px; z-index: 9999; min-width: 300px;">
            <i class="${icon} me-2"></i>${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        </div>
    `);

    $('body').append(notification);

    setTimeout(() => {
        notification.alert('close');
    }, 3000);
}

// Функция для добавления генератора пароля к полю
function addPasswordGenerator(passwordFieldId, buttonText = 'Сгенерировать пароль') {
    const passwordField = $(`#${passwordFieldId}`);
    if (passwordField.length === 0) return;

    passwordField.addClass('generated-password check-strength');
    setupPasswordField(passwordField);

    const container = passwordField.closest('.mb-3, .form-group');
    if (container.find('.password-generate-btn').length === 0) {
        const generateBtn = $(`
            <button type="button" class="btn btn-outline-secondary btn-sm password-generate-btn" 
                    onclick="generateAndSetPassword('${passwordFieldId}')">
                <i class="fas fa-random me-1"></i>${buttonText}
            </button>
        `);
        container.append(generateBtn);
    }
}

// Генерация и установка пароля
function generateAndSetPassword(fieldId) {
    const password = generateSecurePassword();
    const passwordField = $(`#${fieldId}`);

    passwordField.val(password);
    passwordField.trigger('input'); // Для проверки силы пароля

    // Показываем пароль на 3 секунды
    const toggleBtn = passwordField.parent().find('.password-toggle-btn');
    if (toggleBtn.length > 0) {
        toggleBtn.click(); // Показываем пароль

        setTimeout(() => {
            if (passwordField.attr('type') === 'text') {
                toggleBtn.click(); // Скрываем обратно
            }
        }, 3000);
    }

    showPasswordNotification('Пароль сгенерирован и установлен', 'success');
}

// Экспорт функций для глобального использования
window.PasswordUtils = {
    initializePasswordFields,
    setupPasswordField,
    addPasswordGenerator,
    generateAndSetPassword,
    generateSecurePassword
};
