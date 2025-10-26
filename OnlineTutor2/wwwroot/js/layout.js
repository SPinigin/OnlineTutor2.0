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

// Динамическая корректировка отступов
function adjustBodyPadding() {
    const navbarHeight = $('.navbar-fixed').outerHeight() || 70;
    let footerHeight = $('.footer-fixed').outerHeight() || 120;

    // Для мобильных устройств
    if ($(window).width() <= 768) {
        footerHeight = Math.min(footerHeight, 70);
    }
    if ($(window).width() <= 576) {
        footerHeight = Math.min(footerHeight, 65);
    }

    const topPadding = navbarHeight + 5;
    const bottomPadding = footerHeight + 15; // фиксированный отступ

    $('body').css({
        'padding-top': topPadding + 'px',
        'padding-bottom': bottomPadding + 'px'
    });
}

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
