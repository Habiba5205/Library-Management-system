const { test } = require('node:test');
const assert = require('node:assert/strict');
const { readFileSync } = require('node:fs');
const vm = require('node:vm');
const path = require('node:path');

function fixture(valid = true) {
    const listeners = {};
    const windowListeners = {};
    const attributes = new Map();
    const classes = new Set();
    const button = {
        disabled: false, name: 'decision', value: 'confirm',
        getAttribute: key => attributes.get(key) ?? null,
        setAttribute: (key, value) => attributes.set(key, value),
        removeAttribute: key => attributes.delete(key),
        classList: { add: value => classes.add(value), remove: value => classes.delete(value) }
    };
    let focused = false;
    class Form {
        constructor() { this.method = 'post'; this.dataset = {}; this.attrs = new Map(); }
        checkValidity() { return valid; }
        querySelectorAll() { return [button]; }
        querySelector() { return { focus() { focused = true; } }; }
        setAttribute(key, value) { this.attrs.set(key, value); }
        removeAttribute(key) { this.attrs.delete(key); }
    }
    const document = {
        querySelectorAll: () => [],
        addEventListener: (name, handler) => { listeners[name] = handler; }
    };
    const window = { addEventListener: (name, handler) => { windowListeners[name] = handler; } };
    const form = new Form();
    button.form = form;
    vm.runInNewContext(readFileSync(path.join(__dirname, '../../wwwroot/js/interactions.js'), 'utf8'),
        { document, window, HTMLFormElement: Form, HTMLDialogElement: class {} });
    function submit(defaultPrevented = false) {
        const event = { target: form, submitter: button, defaultPrevented,
            preventDefault() { this.defaultPrevented = true; } };
        listeners.submit(event);
        return event;
    }
    return { form, button, submit, windowListeners, classes, attributes, focused: () => focused };
}

test('valid POST stays serializable and duplicate submissions are blocked', () => {
    const f = fixture();
    assert.equal(f.submit().defaultPrevented, false);
    assert.equal(f.button.disabled, false);
    assert.equal(f.button.name, 'decision');
    assert.equal(f.button.value, 'confirm');
    assert.equal(f.attributes.get('aria-disabled'), 'true');
    assert.equal(f.classes.has('is-submitting'), true);
    assert.equal(f.submit().defaultPrevented, true);
});

test('invalid forms focus an error without entering the busy state', () => {
    const f = fixture(false);
    assert.equal(f.submit().defaultPrevented, true);
    assert.equal(f.focused(), true);
    assert.equal(f.form.attrs.has('aria-busy'), false);
});

test('back/forward page restoration clears the submission lock', () => {
    const f = fixture();
    f.submit();
    f.windowListeners.pageshow();
    assert.equal(f.form.attrs.has('aria-busy'), false);
    assert.equal(f.attributes.has('aria-disabled'), false);
    assert.equal(f.classes.has('is-submitting'), false);
    assert.equal(f.submit().defaultPrevented, false);
});

test('cancelled validation events and GET filters are not locked', () => {
    const f = fixture();
    f.submit(true);
    assert.equal(f.form.attrs.has('aria-busy'), false);
    f.form.method = 'get';
    assert.equal(f.submit().defaultPrevented, false);
    assert.equal(f.form.attrs.has('aria-busy'), false);
});
