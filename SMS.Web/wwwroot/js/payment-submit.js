(() => {
    'use strict';
    document.querySelectorAll('.payment-submit-form').forEach(form => {
        let submitting = false;
        form.addEventListener('submit', event => {
            if (submitting) { event.preventDefault(); return; }
            if (event.defaultPrevented || !form.checkValidity()
                || (window.jQuery && jQuery.fn.valid && !jQuery(form).valid())) return;
            submitting = true;
            if (event.submitter?.hasAttribute('formaction')) form.action = event.submitter.formAction;
            form.setAttribute('aria-busy', 'true');
            form.querySelectorAll('button[type="submit"]').forEach(button => {
                button.disabled = true;
                button.textContent = 'Please wait…';
            });
            const status = form.querySelector('.payment-submit-status');
            if (status) status.textContent = 'Recording payment. Wait for the receipt before retrying.';
        });
        window.addEventListener('pageshow', event => {
            if (event.persisted) window.location.reload();
        });
    });
})();
