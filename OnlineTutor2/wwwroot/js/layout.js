$(document).ready(function () {
    initializeLayout();
});

function initializeLayout() {
    handleNavbarScroll();
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

// Инициализация тултипов Bootstrap
function initializeTooltips() {
    const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
    const tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
        return new bootstrap.Tooltip(tooltipTriggerEl);
    });
}

// Автоматическое скрытие уведомлений через 5 секунд
setTimeout(function () {
    $('.alert').each(function () {
        if ($(this).hasClass('alert-dismissible')) {
            $(this).fadeTo(500, 0).slideUp(500, function () {
                $(this).alert('close');
            });
        }
    });
}, 5000);
