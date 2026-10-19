(() => {
    'use strict';
    const site = document.querySelector('.school-site');
    if (!site) return;
    const header = site.querySelector('.school-header');
    const toggle = site.querySelector('.school-menu-toggle');
    const navigation = site.querySelector('.school-navigation');
    const mobile = window.matchMedia('(max-width: 1100px)');
    if (header && toggle && navigation) {
        const setMenu = open => {
            header.classList.toggle('is-menu-open', open);
            toggle.setAttribute('aria-expanded', String(open));
            toggle.querySelector('.school-menu-label').textContent = open ? 'Close' : 'Menu';
        };
        site.classList.add('has-navigation');
        toggle.addEventListener('click', () => setMenu(toggle.getAttribute('aria-expanded') !== 'true'));
        navigation.addEventListener('click', event => { if (event.target.closest('a')) setMenu(false); });
        site.addEventListener('keydown', event => {
            if (event.key === 'Escape' && toggle.getAttribute('aria-expanded') === 'true') {
                setMenu(false);
                toggle.focus();
            }
        });
        document.addEventListener('pointerdown', event => {
            if (!header.contains(event.target) && toggle.getAttribute('aria-expanded') === 'true') setMenu(false);
        });
        mobile.addEventListener('change', () => setMenu(false));
    }

    if (!('IntersectionObserver' in window)) return;
    const motion = window.matchMedia('(prefers-reduced-motion: reduce)');
    const targets = site.querySelectorAll('.school-hero-copy, .school-front-photo, .school-values-strip > span, .school-section-heading, .school-about > div, .school-faith > div, .school-learning-grid article, .school-leadership-grid article, .school-gallery-card, .school-notice-card, .school-campus-photo, .school-bus-photo, .school-contact > div');
    const observer = new IntersectionObserver(entries => {
        for (const entry of entries) {
            if (!entry.isIntersecting) continue;
            entry.target.classList.add('school-reveal-visible');
            observer.unobserve(entry.target);
        }
    }, { root: site.closest('.public-wrapper'), threshold: 0.08, rootMargin: '0px 0px -16px 0px' });
    const setMotion = () => {
        observer.disconnect();
        for (const target of targets) {
            target.classList.remove('school-reveal', 'school-reveal-visible');
            if (!motion.matches) {
                target.classList.add('school-reveal');
                observer.observe(target);
            }
        }
    };
    setMotion();
    motion.addEventListener('change', setMotion);
})();
