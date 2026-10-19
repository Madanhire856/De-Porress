(() => {
    'use strict';
    const form = document.querySelector('[data-trip-form]');
    if (!form) return;
    const departure = form.querySelector('[data-trip-departure]');
    const returning = form.querySelector('[data-trip-return]');
    const duration = form.querySelector('[data-trip-duration]');
    const update = () => {
        if (!departure.value || !returning.value || returning.value <= departure.value) {
            duration.textContent = 'All times are Zimbabwe time. Choose departure and return to calculate days and nights.';
            return;
        }
        const date = value => { const [year, month, day] = value.slice(0, 10).split('-').map(Number); return Date.UTC(year, month - 1, day); };
        const nights = Math.round((date(returning.value) - date(departure.value)) / 86400000);
        duration.textContent = `${nights + 1} calendar day(s) · ${nights} night(s). All times are Zimbabwe time.`;
    };
    departure.addEventListener('input', update); returning.addEventListener('input', update); update();
})();
