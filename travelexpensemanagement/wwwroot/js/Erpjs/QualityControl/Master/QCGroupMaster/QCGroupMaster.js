let isReadOnly;
//=========Page Load===========
$(document).ready(function () {
    const urlParams = new URLSearchParams(location.search);
    const rowId = parseInt(urlParams.get('id'));
    isReadOnly = urlParams.get('readOnly') === 'true';

    $('#TxtQCGName').focus();

    if (!isNaN(rowId) && rowId > 0) {
        loadQCGroupById(rowId);

        if (isReadOnly) {
            setQCGroupFormReadOnly();
        }
    }

    // $('#cancelBtn').on('click', function () {
    //     window.location.href = '/QCGroupMasterList/Index';
    // });

    $('#ACTIVE').on('change', function () {
        let status = $(this).is(':checked') ? 'Active' : 'Inactive';
        $('#statusText').text(status);
    });
});

//========Save & Update=========
$('#btnSave').on('click', function (e) {
    e.preventDefault();

    const code = parseInt($('#CODE').val()) || 0;
    const name = $('#TxtQCGName').val().trim();
    const qcType = $('#ddlQCType').val();
    const isActive = $('#ACTIVE').is(':checked') ? 1 : 0;

    if (!validateRequiredField('#TxtQCGName', 'QCG Name')) return;
    if (!validateRequiredField('#ddlQCType', 'QC Type')) return;

    const payload = {
        CODE: code,
        NAME: name,
        QC_TYPE: qcType,
        ACTIVE: isActive,
        ACTION: code > 0 ? 'UPDATE' : 'INSERT',
    };

    $.ajax({
        url: '/QCGroupMaster/SaveQCGroup',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.success) {
                const msg = code > 0 ? "QC Group updated successfully!" : "QC Group saved successfully!";
                showToast(msg, { type: "success" });
                setTimeout(function () {
                    window.location.href = '/QCGroupMasterList/Index';
                }, 1000);
                $('#QCGroupMasterform')[0].reset();
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

//=========Get By Id=========
function loadQCGroupById(code) {
    $.ajax({
        url: '/QCGroupMasterList/GetQCGroupByCode',
        type: 'GET',
        data: { code },
        success: function (response) {
            const data = response.data;
            if (!data) {
                showToast("No data found.", { type: "warning" });
                return;
            }

            $('#CODE').val(data.code);
            $('#TxtQCGName').val(data.name);
            $('#ddlQCType').val(data.qC_TYPE);
            $('#ACTIVE').prop('checked', data.active === 1 || data.active === true);
            $('#statusText').text(data.active ? 'Active' : 'Inactive');
        },
        error: function (xhr) {
            console.error("Error loading QC Group by ID:", xhr);
            showToast('Error: ' + xhr.responseText, { type: "error" });
        }
    });
}

//=======Readonly============
function setQCGroupFormReadOnly() {
    const form = $('#QCGroupMasterform');
    form.find('input, select, textarea, button').prop('disabled', true);
    $('#ACTIVE').prop('disabled', true);
    $('#statusText').addClass('text-muted');

    $('#btnSave').hide();
    form.addClass('erppage-readonly');
}