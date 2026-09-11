(() => {
    const previews = new Map();
    function refresh(field) {
        const region = field.closest('.cover-upload');
        const preview = region.querySelector('.cover-preview');
        const error = region.querySelector('.cover-client-error');
        const remove = region.querySelector('input[name="RemoveCover"]');
        if (previews.has(field)) URL.revokeObjectURL(previews.get(field));
        previews.delete(field);
        field.setCustomValidity('');
        error.textContent = '';
        const file = field.files?.[0];
        if (file && (file.size > 5 * 1024 * 1024 || !/\.(jpe?g|png|webp)$/i.test(file.name))) {
            error.textContent = 'Choose a JPG, PNG, or WebP image no larger than 5 MB.';
            field.setCustomValidity(error.textContent);
        }
        if (file && !error.textContent) {
            const url = URL.createObjectURL(file);
            previews.set(field, url);
            preview.src = url;
            preview.hidden = false;
            if (remove) remove.checked = false;
        } else {
            preview.hidden = !preview.dataset.original || !!remove?.checked;
            if (preview.dataset.original) preview.src = preview.dataset.original;
            else preview.removeAttribute('src');
        }
    }
    document.addEventListener('change', event => {
        const region = event.target.closest('.cover-upload');
        if (!region) return;
        const field = region.querySelector('input[type="file"]');
        if (event.target.name === 'RemoveCover' && event.target.checked) field.value = '';
        refresh(field);
    });
    document.addEventListener('close', () => {
        previews.forEach((url, field) => {
            if (field.closest('dialog')?.open !== true) {
                URL.revokeObjectURL(url);
                previews.delete(field);
            }
        });
    }, true);
    window.addEventListener('pagehide', () => { previews.forEach(url => URL.revokeObjectURL(url)); previews.clear(); });
})();
