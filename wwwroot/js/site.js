// Общая обработка всех форм на сайте
document.addEventListener('submit', function (event) {
    const form = event.target;

    // 1. Подтверждение удаления: <form data-confirm="Текст вопроса">
    if (form.dataset.confirm && !window.confirm(form.dataset.confirm)) {
        event.preventDefault();
        return;
    }

    // Если проверка полей на странице не пропустила форму, ничего не блокируем
    if (event.defaultPrevented) {
        return;
    }

    // 2. Защита от двойной отправки: повторный клик игнорируется
    if (form.dataset.submitted === 'true') {
        event.preventDefault();
        return;
    }

    form.dataset.submitted = 'true';
    form.querySelectorAll('button[type="submit"]').forEach(function (button) {
        button.disabled = true;
    });
});

// Если пользователь вернулся кнопкой "Назад", форма снова должна работать
window.addEventListener('pageshow', function (event) {
    if (event.persisted) {
        document.querySelectorAll('form[data-submitted]').forEach(function (form) {
            delete form.dataset.submitted;
            form.querySelectorAll('button[type="submit"]').forEach(function (button) {
                button.disabled = false;
            });
        });
    }
});