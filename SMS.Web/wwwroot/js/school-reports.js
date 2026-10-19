(() => {
    'use strict';
    if (document.querySelector('[data-report-print="true"]')) window.addEventListener('load', () => window.print(), { once: true });
})();
