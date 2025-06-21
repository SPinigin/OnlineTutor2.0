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

// динамическая корректировка отступов
function adjustbodypadding() {
    const navbarheight = $('.navbar-fixed').outerheight();
    const footerheight = $('.footer-fixed').outerheight();

    $('body').css({
        'padding-top': navbarheight + 'px',
        'padding-bottom': footerheight + 'px'
    });

    $('.main-content').css({
        'min-height': 'calc(100vh - ' + (navbarheight + footerheight + 40) + 'px)'
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

// Добавление эффекта прозрачности для navbar при прокрутке
$(window).scroll(function () {
    if ($(this).scrollTop() > 50) {
        $('.navbar-fixed').addClass('scrolled');
    } else {
        $('.navbar-fixed').removeClass('scrolled');
    }
});
