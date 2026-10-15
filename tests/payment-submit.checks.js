const assert = require('node:assert/strict');
const fs = require('node:fs');
const vm = require('node:vm');
const source = fs.readFileSync(require('node:path').join(__dirname, '../SMS.Web/wwwroot/js/payment-submit.js'), 'utf8');
function setup({ valid = true, jqueryValid = true } = {}) {
    const button = { disabled: false, textContent: 'Confirm' };
    const status = { textContent: '' };
    const listeners = {};
    const form = { action: '/Confirm', attributes: {}, addEventListener: (name, fn) => listeners[name] = fn,
        checkValidity: () => valid, setAttribute(name, value) { this.attributes[name] = value; },
        querySelectorAll: () => [button], querySelector: () => status };
    const jquery = () => ({valid: () => jqueryValid}); jquery.fn = {valid: true};
    const window = { jQuery: jquery, addEventListener() {} };
    vm.runInNewContext(source, { document: {querySelectorAll: () => [form]}, window, jQuery: jquery });
    const submit = ({prevented = false, action} = {}) => {
        const event = { defaultPrevented: prevented, submitter: action ? {hasAttribute: () => true, formAction: action} : null,
            preventDefault() { this.defaultPrevented = true; } };
        listeners.submit(event); return event;
    };
    return {form, button, status, submit};
}
let test = setup();
assert.equal(test.submit().defaultPrevented, false);
assert.equal(test.button.disabled, true);
assert.equal(test.form.attributes['aria-busy'], 'true');
assert.match(test.status.textContent, /Wait for the receipt/);
assert.equal(test.submit().defaultPrevented, true, 'A repeat submission is blocked');
test = setup({valid: false}); test.submit(); assert.equal(test.button.disabled, false, 'Invalid HTML input must remain editable');
test = setup({jqueryValid: false}); test.submit(); assert.equal(test.button.disabled, false, 'Unobtrusive validation failure must remain editable');
test = setup(); test.submit({prevented: true}); assert.equal(test.button.disabled, false, 'A cancelled event must not lock the form');
test = setup(); test.submit({action: '/Edit'}); assert.equal(test.form.action, '/Edit', 'Edit handler survives disabling the submitter');
console.log('Passed payment-submit checks: valid submission, repeat blocking, HTML validation, jQuery validation, cancelled submission, and edit action.');
