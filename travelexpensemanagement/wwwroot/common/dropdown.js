function bindDropdown(controller, type, dropdownId, placeholder, selectedValue = null, callback = null, skipPlaceholder = false, extraData = null, useSelect2 = false) {

    let requestData = { type: type };
    if (extraData) {
        requestData.data = extraData;
    }
    return $.ajax({
        url: `/${controller}/GetDropdown`,
        type: 'GET',
        data: requestData,
        success: function (data) {

            const ddl = $(dropdownId);
            ddl.empty();

            if (!skipPlaceholder) {
                ddl.append(`<option value="">${placeholder}</option>`);
            }

            // Fill data
            $.each(data, function (i, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });

            if (selectedValue) {
                ddl.val(selectedValue);
            }
            else if (skipPlaceholder && data.length > 0) {
                ddl.val(data[0].value);  
            }
            //Initialize select2
            //if (useSelect2) {
            //    ddl.select2({
            //        placeholder: placeholder,
            //        allowClear: true
            //    });
            //}
            // Initialize Select2
            if (useSelect2) {

                // Destroy existing Select2 if already initialized
                if (ddl.hasClass("select2-hidden-accessible")) {
                    ddl.select2("destroy");
                }

                ddl.select2({
                    placeholder: placeholder,
                    allowClear: true,
                    width: '100%'
                });

                // Auto focus search box when dropdown opens
                ddl.on('select2:open', function () {
                    setTimeout(function () {
                        document.querySelector('.select2-container--open .select2-search__field')?.focus();
                    }, 0);
                });
            }


            if (typeof callback === "function") {
                callback();
            }
        },
        error: function (xhr) {
            console.error("Dropdown error:", xhr.responseText);
        }
    });
}

function ensureOption($dropdown, code, name) {
    if (code && $dropdown.find(`option[value="${code}"]`).length === 0) {
        $dropdown.append(`<option value="${code}">${name}</option>`);
    }
}