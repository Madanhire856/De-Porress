(() => {
    document.querySelectorAll('form[data-auto-filter]').forEach(form => {
        const input = form.querySelector('input:not([type="hidden"])');
        if (!input) return;
        let timer;
        let composing = false;
        const submit = () => {
            clearTimeout(timer);
            if (!composing && form.checkValidity()) form.requestSubmit();
        };
        const schedule = () => {
            clearTimeout(timer);
            if (!composing) timer = setTimeout(submit, 400);
        };
        input.addEventListener('input', schedule);
        input.addEventListener('compositionstart', () => { composing = true; clearTimeout(timer); });
        input.addEventListener('compositionend', () => { composing = false; schedule(); });
        input.addEventListener('change', submit);
        form.querySelectorAll('select').forEach(select => select.addEventListener('change', submit));
        form.addEventListener('submit', () => clearTimeout(timer));

        // Match existing list pages: keep typing at the end after the filtered page reloads.
        if (input.type !== 'number' && input.value) {
            input.focus();
            input.setSelectionRange(input.value.length, input.value.length);
        }
    });
})();
