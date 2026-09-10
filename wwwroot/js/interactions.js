(() => {
    const busyForms = new Map();
    const approvedForms = new WeakSet();

    function initialize(root = document) {
        if (window.bootstrap?.Tooltip) {
            root.querySelectorAll('.row-icon[title], .icon-button[title]').forEach(button => {
                bootstrap.Tooltip.getOrCreateInstance(button, {
                    trigger: 'hover focus',
                    container: button.closest('dialog') || document.body
                });
            });
        }
        root.querySelectorAll('.alert-success, .alert-danger, .alert-warning').forEach(alert => {
            if (!alert.hasAttribute('role')) {
                alert.setAttribute('role', alert.classList.contains('alert-danger') ? 'alert' : 'status');
            }
        });
    }

    function resetBusyForms() {
        busyForms.forEach((buttons, form) => {
            form.removeAttribute('aria-busy');
            buttons.forEach(([button, disabled]) => {
                if (disabled === null) button.removeAttribute('aria-disabled');
                else button.setAttribute('aria-disabled', disabled);
                button.classList.remove('is-submitting');
            });
        });
        busyForms.clear();
    }

    function confirmSubmission(form, submitter) {
        if (form.dataset.confirmOpen) return;
        form.dataset.confirmOpen = 'true';
        const dialog = document.createElement('dialog');
        dialog.className = 'record-dialog confirm-dialog';
        const content = document.createElement('div');
        content.className = 'dialog-content';
        const title = document.createElement('h2');
        title.id = 'confirm-action-title';
        title.textContent = 'Confirm action';
        dialog.setAttribute('aria-labelledby', title.id);
        const message = document.createElement('p');
        message.textContent = form.dataset.confirm;
        const actions = document.createElement('div');
        actions.className = 'confirmation-actions';
        const cancel = document.createElement('button');
        cancel.type = 'button';
        cancel.className = 'btn btn-secondary';
        cancel.textContent = 'Cancel';
        const confirm = document.createElement('button');
        confirm.type = 'button';
        confirm.className = 'btn btn-primary';
        confirm.textContent = 'Confirm';
        actions.append(cancel, confirm);
        content.append(title, message, actions);
        dialog.append(content);
        document.body.append(dialog);
        cancel.addEventListener('click', () => dialog.close());
        confirm.addEventListener('click', () => {
            approvedForms.add(form);
            dialog.close();
            form.requestSubmit(submitter || undefined);
        });
        dialog.addEventListener('close', () => {
            delete form.dataset.confirmOpen;
            dialog.remove();
            submitter?.focus();
        });
        dialog.showModal();
        cancel.focus();
    }

    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || event.defaultPrevented || form.method.toLowerCase() !== 'post') return;
        if (busyForms.has(form)) { event.preventDefault(); return; }
        const valid = form.checkValidity() &&
            (!window.jQuery?.validator || !jQuery(form).data('validator') || jQuery(form).valid());
        if (!valid) {
            event.preventDefault();
            form.querySelector('.input-validation-error, :invalid')?.focus();
            return;
        }
        if (form.dataset.confirm && !approvedForms.has(form)) {
            event.preventDefault();
            confirmSubmission(form, event.submitter);
            return;
        }
        approvedForms.delete(form);
        // Keep controls enabled for native serialization, including submitter name/value.
        const buttons = Array.from(form.querySelectorAll('button[type="submit"], button:not([type]), input[type="submit"]'));
        busyForms.set(form, buttons.map(button => [button, button.getAttribute('aria-disabled')]));
        form.setAttribute('aria-busy', 'true');
        buttons.forEach(button => button.setAttribute('aria-disabled', 'true'));
        event.submitter?.classList.add('is-submitting');
    });

    document.addEventListener('click', event => {
        const control = event.target.closest('button, input[type="submit"]');
        if (control?.form && busyForms.has(control.form)) event.preventDefault();
    }, true);

    document.addEventListener('invalid', event => {
        if (!event.target.form) return;
        event.target.form.classList.add('was-validated');
    }, true);
    document.addEventListener('close', event => {
        if (!(event.target instanceof HTMLDialogElement)) return;
        event.target.querySelectorAll('[data-bs-original-title]').forEach(button => {
            window.bootstrap?.Tooltip.getInstance(button)?.dispose();
        });
    }, true);
    window.addEventListener('pageshow', resetBusyForms);
    window.initializeInteractions = initialize;
    initialize();
})();
