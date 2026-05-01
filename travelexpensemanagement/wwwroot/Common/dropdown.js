function bindDropdown(controller, type, dropdownId, placeholder, selectedValue = null, callback = null, skipPlaceholder = false) {
    return $.ajax({
        url: `/${controller}/GetDropdown`,
        type: 'GET',
        data: { type: type },
        success: function (data) {

            const ddl = $(dropdownId);
            ddl.empty();

            // Placeholder control
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
                ddl.val(data[0].value);   //auto select first
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