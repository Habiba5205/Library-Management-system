// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Preserve server-rendered table actions while containing overflow on small screens.
document.querySelectorAll('main table.table').forEach(table => {
    if (table.closest('.table-responsive')) return;
    const wrapper = document.createElement('div');
    wrapper.className = 'table-responsive';
    table.before(wrapper);
    wrapper.append(table);
});

document.querySelectorAll('.table-responsive').forEach(region => {
    region.tabIndex = 0;
    region.setAttribute('role', 'region');
    region.setAttribute('aria-label', region.closest('.panel')?.querySelector('h2')?.textContent || 'Records');
});

if (window.lucide) window.lucide.createIcons();

// Search and pagination apply to the already authorized records rendered by MVC.
document.querySelectorAll('body.action-index:not(.route-home) main table').forEach(table => {
    const rows = Array.from(table.tBodies[0]?.rows || []).filter(row => !row.querySelector('[colspan]'));
    table.querySelectorAll('tbody tr:has([colspan])').forEach(row => { row.hidden = true; });
    const wrapper = table.closest('.table-responsive');
    if (!wrapper) return;
    const toolbar = document.createElement('div');
    toolbar.className = 'records-toolbar';
    const search = document.createElement('input');
    search.type = 'search';
    search.className = 'form-control records-search';
    const heading = document.querySelector('main h1')?.textContent.trim() || 'records';
    search.placeholder = 'Search ' + heading.toLowerCase();
    search.setAttribute('aria-label', search.placeholder);
    toolbar.append(search);
    wrapper.before(toolbar);
    const footer = document.createElement('div');
    footer.className = 'records-pagination';
    const count = document.createElement('span');
    count.setAttribute('aria-live', 'polite');
    const nav = document.createElement('nav');
    nav.setAttribute('aria-label', heading + ' pages');
    const previous = document.createElement('button');
    previous.type = 'button';
    previous.className = 'icon-button';
    previous.title = 'Previous page';
    previous.setAttribute('aria-label', previous.title);
    previous.innerHTML = '<i data-lucide="chevron-left" aria-hidden="true"></i>';
    const next = document.createElement('button');
    next.type = 'button';
    next.className = 'icon-button';
    next.title = 'Next page';
    next.setAttribute('aria-label', next.title);
    next.innerHTML = '<i data-lucide="chevron-right" aria-hidden="true"></i>';
    const pageLabel = document.createElement('span');
    nav.append(previous, pageLabel, next);
    footer.append(count, nav);
    wrapper.after(footer);
    const empty = document.createElement('p');
    empty.className = 'empty-state';
    empty.textContent = 'No records found.';
    wrapper.after(empty);
    let page = 1;
    let pageSize = 10;
    const sizeLabel = document.createElement('label');
    sizeLabel.className = 'page-size-label';
    sizeLabel.append('Rows ');
    const sizeSelect = document.createElement('select');
    sizeSelect.className = 'form-select';
    [10, 25, 50].forEach(size => sizeSelect.add(new Option(String(size), String(size))));
    sizeLabel.append(sizeSelect);
    toolbar.append(sizeLabel);
    const render = () => {
        const query = search.value.trim().toLocaleLowerCase();
        const filtered = rows.filter(row => row.textContent.toLocaleLowerCase().includes(query));
        const pages = Math.max(1, Math.ceil(filtered.length / pageSize));
        page = Math.min(page, pages);
        rows.forEach(row => { row.hidden = true; });
        filtered.slice((page - 1) * pageSize, page * pageSize).forEach(row => { row.hidden = false; });
        empty.hidden = filtered.length > 0;
        count.textContent = filtered.length ? 'Showing ' + ((page - 1) * pageSize + 1) + '-' + Math.min(page * pageSize, filtered.length) + ' of ' + filtered.length : '0 records';
        pageLabel.textContent = page + ' / ' + pages;
        previous.disabled = page === 1;
        next.disabled = page === pages;
    };
    search.addEventListener('input', () => { page = 1; render(); });
    sizeSelect.addEventListener('change', () => { pageSize = Number(sizeSelect.value); page = 1; render(); });
    previous.addEventListener('click', () => { page--; render(); });
    next.addEventListener('click', () => { page++; render(); });
    render();
});

document.querySelectorAll('main table a.btn').forEach(link => {
    const label = link.textContent.trim();
    const icons = { Details: 'eye', Edit: 'pencil', Delete: 'trash-2' };
    if (!icons[label]) return;
    link.title = label;
    link.setAttribute('aria-label', label);
    link.className = 'row-icon ' + label.toLowerCase();
    link.replaceChildren();
    const icon = document.createElement('i');
    icon.dataset.lucide = icons[label];
    icon.setAttribute('aria-hidden', 'true');
    link.append(icon);
});

let recordDialogLoading = false;
// The dialog uses the real MVC form, including its antiforgery token and validation.
// Native submissions retain the controller's existing success/error behavior.
document.querySelectorAll('body.action-index main a').forEach(link => {
    const url = new URL(link.href, location.href);
    if (url.origin !== location.origin || !/^\/(Book|Author|Category|User)\/(Create|Edit|Delete)(\/\d+)?$/i.test(url.pathname)) return;
    link.addEventListener('click', async event => {
        if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
        event.preventDefault();
        if (recordDialogLoading || document.querySelector('.record-dialog[open]')) return;
        recordDialogLoading = true;
        link.setAttribute('aria-busy', 'true');
        try {
            const response = await fetch(url, { credentials: 'same-origin' });
            if (!response.ok || response.redirected) { location.assign(response.url || url.href); return; }
            const parsed = new DOMParser().parseFromString(await response.text(), 'text/html');
            const main = parsed.querySelector('main');
            if (!main?.querySelector('form')) { location.assign(url.href); return; }
            const dialog = document.createElement('dialog');
            dialog.className = 'record-dialog';
            dialog.setAttribute('aria-label', main.querySelector('h1')?.textContent || 'Edit record');
            const close = document.createElement('button');
            close.type = 'button';
            close.className = 'icon-button dialog-close';
            close.title = 'Close';
            close.setAttribute('aria-label', 'Close');
            close.innerHTML = '<i data-lucide="x" aria-hidden="true"></i>';
            const content = document.createElement('div');
            content.className = 'dialog-content';
            main.querySelectorAll('script').forEach(script => script.remove());
            content.append(...Array.from(main.childNodes));
            dialog.append(close, content);
            document.body.append(dialog);
            content.querySelectorAll('form').forEach(form => {
                form.action = new URL(form.getAttribute('action') || url.href, url).href;
            });
            close.addEventListener('click', () => dialog.close());
            content.querySelectorAll('a.btn-secondary').forEach(cancel => {
                cancel.addEventListener('click', e => { e.preventDefault(); dialog.close(); });
            });
            dialog.addEventListener('close', () => { dialog.remove(); link.focus(); });
            if (window.jQuery?.validator?.unobtrusive) window.jQuery.validator.unobtrusive.parse(content);
            window.lucide?.createIcons();
            dialog.showModal();
            window.initializeInteractions?.(dialog);
        } catch {
            location.assign(url.href);
        } finally {
            recordDialogLoading = false;
            link.removeAttribute('aria-busy');
        }
    });
});
window.lucide?.createIcons();
