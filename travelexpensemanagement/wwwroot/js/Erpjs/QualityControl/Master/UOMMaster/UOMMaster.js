let isReadOnly;

//=========Page Load=======
$(document).ready(function () {
    const urlParams = new URLSearchParams(location.search);
    const rowId = parseInt(urlParams.get('id'));
    isReadOnly = urlParams.get('readOnly') === 'true';

    $('#TxtQCUnitName').focus();


    if (!isNaN(rowId) && rowId > 0) {
        loadUOMById(rowId);

        if (isReadOnly) {
            setUOMFormReadOnly();
        }
    }
    //commented by sumesh
    // $('#cancelBtn').on('click', function () {
    //     window.location.href = '/UOMMasterList/Index';
    // });

    $('#ACTIVE').on('change', function () {
        let status = $(this).is(':checked') ? 'Active' : 'Inactive';
        $('#statusText').text(status);
    });
});
//==========Save & Update=======
$('#btnSave').on('click', function (e) {
    e.preventDefault();

    const code = parseInt($('#CODE').val()) || 0;
    const name = $('#TxtQCUnitName').val().trim();
    const shortName = $('#TxtShortName').val().trim();
    const isActive = $('#ACTIVE').is(':checked') ? 1 : 0;

    // if (!name) {
    //     toastr.warning('Please enter QC Unit Name.');
    //     $('#TxtQCUnitName').focus();
    //     return;
    // }

    // if (!shortName) {
    //     toastr.warning('Please enter Short Name.');
    //     $('#TxtShortName').focus();
    //     return;
    // }
    if (!validateRequiredField('#TxtQCUnitName', 'Unit Name')) return;

    const payload = {
        CODE: code,
        NAME: name,
        SHORTNAME: shortName,
        ACTIVE: isActive,
        ACTION: code > 0 ? 'UPDATE' : 'INSERT',
    };

    $.ajax({
        url: '/UOMMaster/SaveUOM',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.success) {
                showToast("UOM saved successfully!", { type: "success" });
                setTimeout(function () {
                    window.location.href = '/UOMMasterList/Index';
                }, 1000);
                $('#UOMMasterForm')[0].reset();
            } else {
                showToast(response.message || "Something went wrong while saving.", { type: "warning" });
            }
        },
        error: function (xhr) {
            const errorMsg = xhr.responseJSON?.message || xhr.responseText || "An error occurred.";
            showToast("Error: " + errorMsg, { type: "error" });
        }
    });
});

//==========Get By Id=========
function loadUOMById(code) {
    $.ajax({
        url: '/UOMMasterList/GetUOMByCode',
        type: 'GET',
        data: { code },
        success: function (response) {
            const data = response.data;
            if (!data) {
                showToast("No data found.", { type: "warning" });
                return;
            }

            $('#CODE').val(data.code);
            $('#TxtQCUnitName').val(data.name);
            $('#TxtShortName').val(data.shortname);
            $('#ACTIVE').prop('checked', data.active === 1 || data.active === true);
            $('#statusText').text(data.active ? 'Active' : 'Inactive');
        },
        error: function (xhr) {
            console.error("Error loading UOM by ID:", xhr);
            showToast('Error: ' + xhr.responseText, { type: "error" });
        }
    });
}

//=======Set Readonly====
function setUOMFormReadOnly() {
    const form = $('#UOMMasterForm');
    form.find('input, select, textarea, button').prop('disabled', true);
    $('#ACTIVE').prop('disabled', true);
    $('#statusText').addClass('text-muted');
    $('#btnSave').hide();

    form.addClass('erppage-readonly');
}

