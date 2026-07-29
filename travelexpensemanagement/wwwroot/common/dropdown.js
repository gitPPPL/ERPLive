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
            if (useSelect2) {
                ddl.select2({
                    placeholder: placeholder,
                    allowClear: true
                });

                ddl.on('select2:open', function () {
                    setTimeout(function () {
                        let searchBox = document.querySelector(
                            '.select2-container--open .select2-search__field'
                        );

                        if (searchBox) {
                            searchBox.focus();
                        }
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

function bindDropdownNew(controller, type, controlId, placeholder,
    selectedValue = null,
    callback = null,
    skipPlaceholder = false,
    extraData = null,
    useSelect2 = false) {

    let requestData = { type: type };

    if (extraData) {
        requestData.data = extraData;
    }

    return $.ajax({
        url: `/${controller}/GetDropdown`,
        type: 'GET',
        data: requestData,
        global: false,

        success: function (data) {

            const control = $(controlId);
            // INPUT -> Autocomplete
            if (control.is("input")) {

                if (control.data("ui-autocomplete")) {
                    control.autocomplete("destroy");
                }
                control.autocomplete({

                    minLength: 0,
                    delay: 200,
                    source: function (request, response) {

                        $.ajax({
                            url: "/" + controller + "/Search" + type,
                            type: "GET",
                            data: {
                                term: request.term
                            },
                            success: function (data) {

                                response($.map(data, function (item) {
                                    return {
                                        label: item.text,
                                        value: item.text,
                                        code: item.value
                                    };
                                }));

                            }
                        });

                    },
                    select: function (event, ui) {
                        debugger

                        control.val(ui.item.value);

                        switch (type) {
                            case "Party":
                                $("#hdnPartyCode").val(ui.item.code);
                                break;

                            case "City":
                                $("#hdnCity").val(ui.item.code);
                                break;

                            case "Courier":
                                $("#hdnCourier").val(ui.item.code);
                                break;

                            case "Purpose":
                                $("#hdnPurpose").val(ui.item.code);
                                break;

                            case "printParty":
                                $("#hdnprintPartyCode").val(ui.item.code);
                                break;

                            case "TransportName":
                                $("#hdnTransport").val(ui.item.code);
                                break;
                        }

                        return false;
                    },

                    open: function () {

                        $(".ui-autocomplete").css({
                            "max-height": "300px",
                            "overflow-y": "auto",
                            "overflow-x": "hidden"
                        });

                    }

                });

                // Focus par Top 100 records load
                control.off("focus").on("focus", function () {
                    $(this).autocomplete("search", "");
                });

                // Edit Mode value
                if (selectedValue) {
                    control.val(selectedValue);
                }

                if (typeof callback === "function") {
                    callback();
                }

                return;
            }
            control.empty();

            if (!skipPlaceholder) {
                control.append(`<option value="">${placeholder}</option>`);
            }

            $.each(data, function (i, item) {

                control.append(
                    `<option value="${item.value}">${item.text}</option>`
                );

            });

            if (selectedValue) {
                control.val(selectedValue);
            }
            else if (skipPlaceholder && data.length > 0) {
                control.val(data[0].value);
            }

            if (useSelect2) {

                control.select2({
                    placeholder: placeholder,
                    allowClear: true
                });

                control.on('select2:open', function () {

                    setTimeout(function () {

                        let searchBox = document.querySelector(
                            '.select2-container--open .select2-search__field'
                        );

                        if (searchBox) {
                            searchBox.focus();
                        }

                    }, 0);

                });

            }

            if (typeof callback === "function") {
                callback();
            }

        },

        error: function (xhr) {

            console.log(xhr.responseText);

        }

    });

}
function ensureOption($dropdown, code, name) {
    if (code && $dropdown.find(`option[value="${code}"]`).length === 0) {
        $dropdown.append(`<option value="${code}">${name}</option>`);
    }
}

