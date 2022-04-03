$.validator.setDefaults({
    highlight: function (element) {
        if (!$(element).hasClass('validate-ignore')) {
            $(element).addClass('is-invalid').removeClass('is-valid');
        }
    },
    unhighlight: function (element) {
        if (!$(element).hasClass('validate-ignore')) {
            $(element).removeClass('is-invalid').addClass('is-valid');
        }
    }
});

$.validator.methods.number = function (value, element) {
    return this.optional(element) || /-?(?:\d+|\d{1,3}(?:[\s\.,]\d{3})+)(?:[\.,]\d+)?$/.test(value);
}

// extend jquery range validator to work for required checkboxes
var defaultRangeValidator = $.validator.methods.range;
$.validator.methods.range = function (value, element, param) {
    if (element.type === 'checkbox') {
        // if it's a checkbox return true if it is checked
        return element.checked;
    } else {
        // otherwise run the default validation function
        return defaultRangeValidator.call(this, value, element, param);
    }
}