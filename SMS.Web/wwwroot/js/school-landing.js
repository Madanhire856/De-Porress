(() => {
    'use strict';
    const site = document.querySelector('.school-site');
    if (!site || !('IntersectionObserver' in window)) return;
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    if (motion.matches) return;
    const targets = site.querySelectorAll('.school-section-heading, .school-about > div, .school-faith > div, .school-learning-grid article, .school-leadership-grid article, .school-bus-photo, .school-contact > div');
    const observer = new IntersectionObserver(entries => {
        for (const entry of entries) {
            entry.target.classList.toggle('school-reveal-visible', entry.isIntersecting);
        }
    }, { root: site.closest('.public-wrapper'), threshold: 0.08 });
    for (const target of targets) {
        target.classList.add('school-reveal');
        observer.observe(target);
    }
    motion.addEventListener('change', event => {
        if (!event.matches) return;
        observer.disconnect();
        for (const target of targets) target.classList.remove('school-reveal', 'school-reveal-visible');
    });
})();
