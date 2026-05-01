function bindDropdown(controller, type, dropdownId, placeholder, selectedValue = null, callback = null) {
    $.ajax({
        url: `/${controller}/GetDropdown`,
        type: 'GET',
        data: { type: type },
        success: function (data) {

            const ddl = $(dropdownId);
            ddl.empty().append(`<option value="">${placeholder}</option>`);

            $.each(data, function (i, item) {
                ddl.append(`<option value="${item.value}">${item.text}</option>`);
            });

            if (selectedValue) {
                ddl.val(selectedValue);
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




