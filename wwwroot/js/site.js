// Общая обработка всех форм на сайте
document.addEventListener('submit', function (event) {
    const form = event.target;
    const submitter = event.submitter;

    // 1. Подтверждение: <form data-confirm="..."> или <button data-confirm="...">
    const confirmText = (submitter && submitter.dataset.confirm) || form.dataset.confirm;
    if (confirmText && !window.confirm(confirmText)) {
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

    // Кнопки блокируем чуть позже, когда браузер уже начал отправку
    setTimeout(function () {
        form.querySelectorAll('button[type="submit"]').forEach(function (button) {
            button.disabled = true;
        });
    }, 0);
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