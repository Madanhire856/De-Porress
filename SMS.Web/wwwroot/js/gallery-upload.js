(() => {
    'use strict';
    const form = document.querySelector('[data-gallery-upload]');
    const input = form?.querySelector('[data-gallery-file]');
    if (!input || !window.DataTransfer || !HTMLCanvasElement.prototype.toBlob) return;
    const thumbnailInput = form.querySelector('[data-gallery-thumbnail]');
    const preview = form.querySelector('[data-gallery-preview]');
    const status = form.querySelector('[data-gallery-upload-status]');
    const submit = form.querySelector('button[type="submit"]');
    let generation = 0;
    let preparing = false;
    let previewUrl;
    const render = (image, maxSize, quality) => new Promise((resolve, reject) => {
        const scale = Math.min(1, maxSize / Math.max(image.naturalWidth, image.naturalHeight));
        const canvas = document.createElement('canvas');
        canvas.width = Math.max(1, Math.round(image.naturalWidth * scale));
        canvas.height = Math.max(1, Math.round(image.naturalHeight * scale));
        const context = canvas.getContext('2d');
        context.fillStyle = '#fff'; context.fillRect(0, 0, canvas.width, canvas.height);
        context.drawImage(image, 0, 0, canvas.width, canvas.height);
        canvas.toBlob(blob => blob ? resolve(blob) : reject(new Error('Image conversion failed')), 'image/jpeg', quality);
    });
    const setFile = (field, file) => { const transfer = new DataTransfer(); transfer.items.add(file); field.files = transfer.files; };
    input.addEventListener('change', async () => {
        const current = ++generation;
        const file = input.files[0];
        thumbnailInput.value = '';
        if (previewUrl) URL.revokeObjectURL(previewUrl);
        preview.hidden = true;
        if (!file) { preparing = false; submit.disabled = false; status.textContent = ''; return; }
        preparing = true; submit.disabled = true;
        status.classList.remove('text-danger');
        status.textContent = 'Preparing your photo…';
        const source = URL.createObjectURL(file);
        try {
            if (!['image/jpeg', 'image/png'].includes(file.type) || file.size > 20 * 1024 * 1024) throw new Error('Unsupported photo');
            const image = new Image(); image.src = source; await image.decode();
            const full = await render(image, 1600, .85);
            const thumb = await render(image, 480, .8);
            if (current !== generation) return;
            if (full.size > 2 * 1024 * 1024 || thumb.size > 256 * 1024) throw new Error('Photo too large');
            setFile(input, new File([full], 'school-photo.jpg', { type: 'image/jpeg' }));
            setFile(thumbnailInput, new File([thumb], 'school-photo-thumbnail.jpg', { type: 'image/jpeg' }));
            previewUrl = URL.createObjectURL(full); preview.src = previewUrl; preview.hidden = false;
            status.textContent = `Photo ready · ${Math.ceil(full.size / 1024)} KB`;
        } catch {
            if (current !== generation) return;
            input.value = ''; thumbnailInput.value = '';
            status.classList.add('text-danger');
            status.textContent = 'This photo could not be prepared. Choose a smaller JPEG or PNG photo.';
        } finally {
            URL.revokeObjectURL(source);
            if (current === generation) { preparing = false; submit.disabled = false; }
        }
    });
    form.addEventListener('submit', event => { if (preparing) event.preventDefault(); });
})();
