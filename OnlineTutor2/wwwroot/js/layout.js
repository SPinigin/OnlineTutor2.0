$(document).ready(function () {
    initializeLayout();
});

function initializeLayout() {
    handleNavbarScroll();
    adjustBodyPadding();
    initializeTooltips();
}

// Обработка прокрутки для navbar
function handleNavbarScroll() {
    $(window).scroll(function () {
        const scrollTop = $(this).scrollTop();
        const navbar = $('.navbar-fixed');

        if (scrollTop > 50) {
            navbar.addClass('scrolled');
        } else {
            navbar.removeClass('scrolled');
        }
    });
}

// Динамическая корректировка отступов для компактного footer
function adjustBodyPadding() {
    const navbarHeight = $('.navbar-fixed').outerHeight() || 70;
    let footerHeight = $('.footer-fixed').outerHeight() || 120;

    // Для мобильных устройств используем значительно меньшую высоту footer
    if ($(window).width() <= 768) {
        footerHeight = Math.min(footerHeight, 60);
    }

    if ($(window).width() <= 576) {
        footerHeight = Math.min(footerHeight, 55);
    }

    const topPadding = navbarHeight + 5;
    const bottomPadding = footerHeight + 10;

    $('body').css({
        'padding-top': topPadding + 'px',
        'padding-bottom': bottomPadding + 'px'
    });

    $('.main-content').css({
        'padding-bottom': '10px'
    });
}

// Функция для проверки и исправления перекрытия
function checkContentOverlap() {
    const footerTop = $('.footer-fixed').offset().top;
    const contentBottom = $('.main-content').offset().top + $('.main-content').outerHeight();

    if (contentBottom > footerTop) {
        const additionalPadding = (contentBottom - footerTop) + 20;
        $('body').css('padding-bottom', (parseInt($('body').css('padding-bottom')) + additionalPadding) + 'px');
    }
}

// Обновленная инициализация
function initializeLayout() {
    handleNavbarScroll();
    adjustBodyPadding();

    // Проверяем перекрытие после загрузки контента
    setTimeout(checkContentOverlap, 100);

    initializeTooltips();
}

// Проверяем при изменении размера окна
$(window).resize(function () {
    adjustBodyPadding();
    setTimeout(checkContentOverlap, 100);
});

// Проверяем при скролле (для динамического контента)
$(window).scroll(function () {
    if ($(this).scrollTop() > 50) {
        $('.navbar-fixed').addClass('scrolled');
    } else {
        $('.navbar-fixed').removeClass('scrolled');
    }
});

// Инициализация тултипов Bootstrap
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Обработка изменения размера окна
$(window).resize(function () {
    adjustBodyPadding();
});

// Автоматическое скрытие уведомлений через 5 секунд
setTimeout(function () {
    $('.alert').alert('close');
}, 5000);

// Добавление эффекта прозрачности для navbar при прокрутке
$(window).scroll(function () {
    if ($(this).scrollTop() > 50) {
        $('.navbar-fixed').addClass('scrolled');
    } else {
        $('.navbar-fixed').removeClass('scrolled');
    }
});
