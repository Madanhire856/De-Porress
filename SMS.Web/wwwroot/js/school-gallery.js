(() => {
    'use strict';
    for (const gallery of document.querySelectorAll('[data-school-gallery]')) {
        const links = [...gallery.querySelectorAll('[data-gallery-open]')];
        const dialog = gallery.querySelector('[data-gallery-dialog]');
        if (!dialog || typeof dialog.showModal !== 'function') continue;
        const image = dialog.querySelector('[data-gallery-large]');
        const caption = dialog.querySelector('[data-gallery-caption]');
        const position = dialog.querySelector('[data-gallery-position]');
        let index = 0;
        let opener;
        const show = next => {
            index = (next + links.length) % links.length;
            const link = links[index];
            image.src = link.href;
            image.alt = link.dataset.caption || '';
            caption.textContent = link.dataset.caption || '';
            position.textContent = `${link.dataset.album || ''} · ${index + 1} / ${links.length}`;
        };
        links.forEach((link, photoIndex) => link.addEventListener('click', event => {
            if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
            event.preventDefault();
            opener = link;
            show(photoIndex);
            dialog.showModal();
        }));
        dialog.querySelector('[data-gallery-close]').addEventListener('click', () => dialog.close());
        dialog.querySelector('[data-gallery-previous]').addEventListener('click', () => show(index - 1));
        dialog.querySelector('[data-gallery-next]').addEventListener('click', () => show(index + 1));
        dialog.addEventListener('keydown', event => {
            if (event.key === 'ArrowLeft' || event.key === 'ArrowRight') {
                event.preventDefault();
                show(index + (event.key === 'ArrowRight' ? 1 : -1));
            }
        });
        dialog.addEventListener('click', event => { if (event.target === dialog) dialog.close(); });
        dialog.addEventListener('close', () => {
            image.removeAttribute('src');
            opener?.focus();
        });
        if (links.length < 2) {
            dialog.querySelector('[data-gallery-previous]').hidden = true;
            dialog.querySelector('[data-gallery-next]').hidden = true;
        }
    }
})();
