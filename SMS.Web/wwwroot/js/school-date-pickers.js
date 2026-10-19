(() => {
    'use strict';
    const $ = window.jQuery;
    if (!$?.fn.datepicker) return;
    let nextId = 0;
    const forms = new Map();
    const isoPattern = /^\d{4}-\d{2}-\d{2}$/;
    const parseDate = value => {
        if (!isoPattern.test(value || '')) return null;
        try { return $.datepicker.parseDate('yy-mm-dd', value); } catch { return null; }
    };
    const labelFor = input => input.getAttribute('aria-label') || [...(input.labels || [])].map(label => label.textContent.trim()).join(' ') || 'Date';
    const emit = input => {
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    };
    function register(form, validate, inputs) {
        if (!form) return;
        if (!forms.has(form)) {
            forms.set(form, []);
            // Unobtrusive validation sets novalidate on some forms. Still enforce
            // the date/time constraints formerly enforced by native date inputs.
            form.addEventListener('submit', event => {
                for (const item of forms.get(form)) {
                    item.validate();
                    const invalid = item.inputs.find(input => !input.disabled && !input.checkValidity());
                    if (!invalid) continue;
                    event.preventDefault(); event.stopImmediatePropagation();
                    invalid.focus(); invalid.reportValidity(); return;
                }
            }, true);
        }
        forms.get(form).push({ validate, inputs });
    }
    function calendar(input, source, label, selected) {
        const wrapper = document.createElement('div'); wrapper.className = 'sms-date-field';
        input.before(wrapper); wrapper.append(input);
        const button = document.createElement('button');
        button.type = 'button'; button.className = 'sms-datepicker-toggle';
        button.setAttribute('aria-label', 'Choose ' + label.replace(/\s*\*$/, '').toLowerCase());
        button.title = 'Choose date'; button.innerHTML = '<i class="fas fa-calendar-alt" aria-hidden="true"></i>';
        button.disabled = input.disabled || input.readOnly; wrapper.append(button);
        input.classList.add('sms-datepicker-input');
        input.autocomplete = 'off'; input.inputMode = 'none'; input.placeholder = 'yyyy-mm-dd';
        input.setAttribute('pattern', '\\d{4}-\\d{2}-\\d{2}');
        $(input).datepicker({
            dateFormat: 'yy-mm-dd', changeMonth: true, changeYear: true,
            yearRange: 'c-100:c+20', showAnim: '', disabled: input.disabled,
            beforeShow: () => input.readOnly ? false : {
                minDate: parseDate((source.getAttribute('min') || '').slice(0, 10)),
                maxDate: parseDate((source.getAttribute('max') || '').slice(0, 10))
            },
            minDate: parseDate((source.getAttribute('min') || '').slice(0, 10)),
            maxDate: parseDate((source.getAttribute('max') || '').slice(0, 10)),
            onSelect: () => { selected(); emit(input); }
        });
        button.addEventListener('click', () => $(input).datepicker('show'));
    }
    function validateDate(input, source) {
        input.setCustomValidity('');
        if (input.disabled || input.readOnly || !input.value) return;
        input.value = input.value.trim();
        if (!parseDate(input.value)) { input.setCustomValidity('Choose a valid date in yyyy-mm-dd format.'); return; }
        const min = (source.getAttribute('min') || '').slice(0, 10), max = (source.getAttribute('max') || '').slice(0, 10);
        if (parseDate(min) && input.value < min) input.setCustomValidity('Choose a date on or after ' + min + '.');
        if (parseDate(max) && input.value > max) input.setCustomValidity('Choose a date on or before ' + max + '.');
    }
    function dateOnly(input) {
        const label = labelFor(input);
        input.type = 'text';
        const validate = () => validateDate(input, input);
        calendar(input, input, label, validate);
        input.addEventListener('input', validate); input.addEventListener('change', validate);
        validate(); register(input.form, validate, [input]);
    }
    function dateTime(original) {
        const label = labelFor(original), id = original.id || ('sms-date-' + (++nextId));
        const initial = original.value;
        const group = document.createElement('div'); group.className = 'sms-datetime-fields';
        const date = document.createElement('input'), time = document.createElement('input');
        date.type = 'text'; date.id = id; date.className = original.className;
        date.value = initial.slice(0, 10); date.setAttribute('aria-label', label + ' (date)');
        time.type = 'time'; time.id = id + '__time'; time.className = original.className;
        time.value = initial.slice(11); time.step = original.getAttribute('step') || (initial.length > 16 ? '1' : '60');
        time.setAttribute('aria-label', label + ' (time)');
        date.required = time.required = original.required;
        date.disabled = time.disabled = original.disabled;
        date.readOnly = time.readOnly = original.readOnly;
        original.before(group); group.append(date, time);
        original.type = 'hidden'; original.id = id + '__value';
        // The original field keeps its name and ISO value. Page scripts holding
        // its reference (for example trip duration) continue receiving events.
        const stamp = value => {
            const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2})(?:\.(\d{1,3}))?)?$/.exec(value);
            return match ? Date.UTC(+match[1], +match[2] - 1, +match[3], +match[4], +match[5], +(match[6] || 0), +(match[7] || '0').padEnd(3, '0')) : NaN;
        };
        const sync = () => {
            validateDate(date, original); time.setCustomValidity('');
            if (date.disabled || date.readOnly) return;
            if (!date.value && !time.value) { original.value = ''; return; }
            if (!date.value) date.setCustomValidity('Choose the date as well as the time.');
            if (!time.value) time.setCustomValidity('Enter the time as well as the date.');
            const combined = date.value + 'T' + time.value;
            const valid = parseDate(date.value) && /^([01]\d|2[0-3]):[0-5]\d(?::[0-5]\d(?:\.\d{1,3})?)?$/.test(time.value);
            if (valid) {
                const min = original.getAttribute('min'), max = original.getAttribute('max');
                if (min && stamp(combined) < stamp(min)) time.setCustomValidity('Choose a date and time on or after ' + min.replace('T', ' ') + '.');
                if (max && stamp(combined) > stamp(max)) time.setCustomValidity('Choose a date and time on or before ' + max.replace('T', ' ') + '.');
            }
            original.value = valid && date.checkValidity() && time.checkValidity() ? combined : '';
        };
        const changed = event => { sync(); original.dispatchEvent(new Event(event.type, { bubbles: true })); };
        calendar(date, original, label + ' date', sync);
        for (const input of [date, time]) {
            input.addEventListener('input', changed); input.addEventListener('change', changed);
        }
        sync(); register(original.form, sync, [date, time]);
    }
    $(function () {
        // Leave existing jQuery pickers and their page-specific rules intact.
        document.querySelectorAll('input[type="date"], input[type="datetime-local"]').forEach(input => {
            if (!input.id) input.id = 'sms-date-' + (++nextId);
            if ($(input).hasClass('hasDatepicker')) return;
            if (input.type === 'datetime-local') dateTime(input); else dateOnly(input);
        });
    });
})();
