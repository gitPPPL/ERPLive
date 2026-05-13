//function bindDropdown(controller, type, dropdownId, placeholder, selectedValue = null, callback = null) {
//    $.ajax({
//        url: `/${controller}/GetDropdown`,
//        type: 'GET',
//        data: { type: type },
//        success: function (data) {

//            const ddl = $(dropdownId);
//            ddl.empty().append(`<option value="">${placeholder}</option>`);

//            $.each(data, function (i, item) {
//                ddl.append(`<option value="${item.value}">${item.text}</option>`);
//            });

//            if (selectedValue) {
//                ddl.val(selectedValue);
//            }

//            if (typeof callback === "function") {
//                callback();
//            }
//        },
//        error: function (xhr) {
//            console.error("Dropdown error:", xhr.responseText);
//        }
//    });
//}
function bindDropdown(controller, type, dropdownId, placeholder, selectedValue = null, callback = null, skipPlaceholder = false, extraData = null, useSelect2 = false) {

    let requestData = { type: type };

    // Add extraData to the requestData if provided
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

            //Initialize select2
            if (useSelect2) {
                ddl.select2({
                    placeholder: placeholder,  // Optionally set a placeholder for Select2
                    allowClear: true            // Optionally allow clearing the selection
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