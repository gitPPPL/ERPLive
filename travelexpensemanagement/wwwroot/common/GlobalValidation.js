let permission = null;
let Entrypermission = null;
function validateForm(formSelector) {
    console.log("Validating form...");
    let isValid = true;
    $(formSelector).find('.error-message').remove();
    $(formSelector).find('[data-required="true"]').css("border", "");

    const requiredFields = $(formSelector).find('[data-required="true"]');
    for (let i = 0; i < requiredFields.length; i++) {
        const field = $(requiredFields[i]);
        let value = field.val();
        if (typeof value === "string") {
            value = value.trim();
        }

        const errorMsg = field.data("error") || "This field is required";

        if (!value) {
            field.focus().css("border", "1px solid red");
            showError(field, errorMsg);
            isValid = false;
            break;
        }
    }

    $(formSelector).find('[data-validate="email"]').each(function () {
        const email = $(this).val().trim();
        if (email && !validateEmail(email)) {
            isValid = false;
            showError($(this), $(this).data("error") || "Invalid email address");
        }
    });

    $(formSelector).find('[data-validate="phone"]').each(function () {
        const phone = $(this).val().trim();
        if (phone && !validatePhone(phone)) {
            isValid = false;
            showError($(this), $(this).data("error") || "Invalid phone number");
        }
    });

    return isValid;
}
function showError(element, message) {
    element.after(`<span class="error-message" style="color:red;font-size:12px;">${message}</span>`);
}
function validateEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}
function validatePhone(phone) {
    const regex = /^\d{10}$/;
    return regex.test(phone);
}
function validateRequiredField(selector, fieldName) {
    const $field = $(selector);
    const value = $field.val()?.trim();

    if (!value) {
        //toastr.warning(`${fieldName} is required`);
        //$field.focus();
        setInvalid($(selector), `${fieldName} is required`)
        return false;
    }
    return true;
}
const MessageStore = {
    insert: '✅ Data inserted successfully!',
    update: '✅ Data updated successfully!',
    delete: '🗑️ Data deleted successfully!',
    error: '❌ An error occurred. Please try again.',
    warning: '⚠️ Please check your inputs.',
    info: 'ℹ️ Just so you know...'
  };
function showFlashMessageByKey(key, type = 'success') {
    const message = MessageStore[key];
    if (message) {
        localStorage.setItem('flashMessage', JSON.stringify({ message, type }));
    }
  }
$(document).ready(function () {
        const stored = localStorage.getItem('flashMessage');
        if (stored) {
          const {message, type} = JSON.parse(stored);
        if (message && type && toastr[type]) {
            toastr[type](message);
          } else {
            toastr.info(message);
          }
        localStorage.removeItem('flashMessage');
        }
});
function confirmAction(message = "Are you sure you want to delete this item group?", yesText = "Yes", noText = "No") {
    return Swal.fire({
        title: '<strong>Confirm Delete</strong>',
        html: `
            <div style="margin-bottom: 10px;">
                <div style="background: #ffe5e5; display: inline-block; border-radius: 10px; padding: 10px;">
                    <i class="fa fa-trash" aria-hidden="true" style="color:red;"></i>
                </div>
            </div>
            <div style="color: #555; font-size: 14px;">
                ${message}
            </div>
        `,
        showCancelButton: true,
        confirmButtonText: yesText,
        cancelButtonText: noText,
        confirmButtonColor: '#d33',
        cancelButtonColor: '#aaa',
        buttonsStyling: false,
        reverseButtons: true,    
        focusCancel: true,      
        customClass: {
            popup: 'custom-swal-spacing',
            confirmButton: 'btn btn-secondary',
            cancelButton: 'btn btn-danger'
        }
    }).then(result => result.isConfirmed);
}

$('.nav-link').on('click dblclick', function (e) {
    e.stopPropagation();
});
function showDocumentPopupjQuery(data, docCode) {
    console.log(data);
    if (!data || !Array.isArray(data) || data.length === 0) {
        toastr.error("No data to show");
        return;
    }

    const existingModal = document.getElementById("dynamicDocModal");
    if (existingModal) existingModal.remove();

    const tableRows = data.map(row => `
        <tr>
            <td>${row.code || row.doC_CODE || ''}</td>
            <td>${row.uUser || ''}</td>
            <td>${row.udate ? new Date(row.udate).toLocaleString() : ''}</td>
            <td>${row.euser || ''}</td>
            <td>${row.edate ? new Date(row.edate).toLocaleString() : ''}</td>
            <td></td>
            <td>Approved By</td>
            <td>Approved On</td>
            <td>${row.wsid || ''}</td>
            <td>${row.lip || ''}</td>
            <td>${row.lid || ''}</td>
        </tr>
    `).join('');

    const modalHTML = `
        <div class="modal fade" id="dynamicDocModal" tabindex="-1" aria-labelledby="docModalLabel" aria-hidden="true">
    <div class="modal-dialog modal-lg erppagesmodal">
        <div class="modal-content erppagesmodal-content">
            <div class="erppagesmodal-header">
                <div class="erppagesmodal-header-left">
                    <div class="erppagesmodal-header-icon">
                        <i class="fa fa-file-signature"></i>
                    </div>
                    <h5 class="erppagesmodal-title" id="docModalLabel">
                        Document Details for ${data[0]?.uUser || 'Unknown User'}
                    </h5>
                </div>
                <button type="button" class="erppagesmodal-close" data-bs-dismiss="modal" aria-label="Close">
                    <i class="fa fa-times"></i>
                </button>
            </div>
            <div class="erppagesmodal-body">
                    <div class="erppagelist-container">
                        <div class="excel-wrapper fixed-grid-wrapper">
                            <table id="tblApprovalStageMaster" class="excel-table fixed-grid-table">
                                <colgroup>
                                    <col style="display:none;" /> <!-- FIXED -->
                                    <col style="width: 120px;" />  <!-- Doc ID -->
                                    <col style="width: 120px;" />  <!-- Created By -->
                                    <col style="width: 120px;" />  <!-- Created On -->
                                    <col style="width: 140px;" />  <!-- Last Modified By -->
                                    <col style="width: 140px;" />  <!-- Last Modified On -->
                                    <col style="width: 100px;" />  <!-- Status -->
                                    <col style="width: 120px;" />  <!-- Approved By -->
                                    <col style="width: 120px;" />  <!-- Approved On -->
                                    <col style="width: 140px;" />  <!-- Last PC Name -->
                                    <col style="width: 140px;" />  <!-- Last N-Compid -->
                                    <col style="width: 140px;" />  <!-- Last Login By -->
                                </colgroup>
                                <thead>
                                    <tr>
                                        <th style="display:none;">Code</th>
                                        <th>Doc id</th>
                                        <th>Created By</th>
                                        <th>Created On</th>
                                        <th>Last Modified By</th>
                                        <th>Last Modified On</th>
                                        <th>Status</th>
                                        <th>Approved By</th>
                                        <th>Approved On</th>
                                        <th>Last PC Name</th>
                                        <th>Last N-Compid</th>
                                        <th>Last Login By</th>
                                    </tr>
                                </thead>
                                <tbody>${tableRows}</tbody>
                            </table>
                        </div>
                    </div>
            </div>
        </div>
    </div>
</div>`;

    // Inject modal into the DOM
    document.body.insertAdjacentHTML('beforeend', modalHTML);

    // Show the modal using Bootstrap's API
    const modalElement = document.getElementById('dynamicDocModal');
    const modal = new bootstrap.Modal(modalElement);
    modal.show();

    // Wait until modal is shown, THEN make it draggable
    $(modalElement).on('shown.bs.modal', function () {
        $(this).find('.modal-dialog').draggable({
            handle: '.popup-header',
            containment: 'window' // Optional: keep inside screen
        });
    });
}

// MaxLength Validation for Input Fields
function enforceMaxlength(selector) {
    $(document).on('input', selector, function () {
        const maxLength = $(this).data('maxlength');
        const value = $(this).val();

        if (maxLength && value.length > maxLength) {
            $(this).val(value.slice(0, maxLength));
        }
    });
}
// Usage: apply to all number inputs with data-maxlength
$(document).ready(function () {
    enforceMaxlength('input[type="number"][data-maxlength]');
});
//How to call
//<input type="number" class="form-control" id="FLAG_A" name="FLAG_A" data-maxlength="5">

function handleBack(redirectUrl, isReadOnly = false) {

    if (isReadOnly) {
        window.location.href = redirectUrl;
        return;
    }

    Swal.fire({
        title: 'Are you sure?',
        text: "Unsaved data will be lost.",
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, exit',
        cancelButtonText: 'Stay',
        confirmButtonColor: '#3085d6',
        cancelButtonColor: '#d33'
    }).then((result) => {
        if (result.isConfirmed) {
            window.location.href = redirectUrl;
        }
    });
}

//================Yesterday Date===========
function getYesterdayYMD() {
    const today = new Date();
    today.setDate(today.getDate() - 1); // subtract 1 day

    const dd = String(today.getDate()).padStart(2, '0');
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const yyyy = today.getFullYear();

    return `${yyyy}-${mm}-${dd}`;
}
//=====Format Date yyyy-mm-dd========
function formatDateYMD(dateStr) {
    if (!dateStr) return '';
    let parts = dateStr.split('T');
    let newDate = parts[0];
    return newDate;
}
function parseIntSafe(value) {
    const parsed = parseInt(value, 10);
    return isNaN(parsed) ? null : parsed;
}
function parseFloatSafe(value) {
    const parsed = parseFloat(value);
    return isNaN(parsed) ? null : parsed;
}
function parseDate(dateStr) {
    if (!dateStr) return null;
    const parts = dateStr.split(/[-\/]/);
    if (parts.length === 3) {
        let [day, month, year] = parts.map(p => parseInt(p, 10));
        if (year < 1000) year += 2000;
        return new Date(year, month - 1, day);
    }
    return null;
}
function parseDecimalSafe(val) {
    const num = parseFloat(val);
    return isNaN(num) ? null : num;
}
function toNullableInt(val) {
    const parsed = parseInt(val);
    return isNaN(parsed) ? null : parsed;
}

function toNullableDate(val) {
    const date = new Date(val);
    return isNaN(date.getTime()) ? null : val;
}

function toNullableString(val) {
    return val?.trim() || null;
}

function allowOnlyNumbers(input) {
    input.value = input.value
        .replace(/[^0-9.]/g, '')
        .replace(/(\..*)\./g, '$1');
}

function getCurrentFormattedDateTime() {
    const now = new Date();
    const yyyy = now.getFullYear();
    const mm = String(now.getMonth() + 1).padStart(2, '0');
    const dd = String(now.getDate()).padStart(2, '0');
    const hh = String(now.getHours()).padStart(2, '0');
    const min = String(now.getMinutes()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}T${hh}:${min}`;
}
//============Current Date========
function getCurrentDateYMD() {
    const today = new Date();
    const dd = String(today.getDate()).padStart(2, '0');
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const yyyy = today.getFullYear();
    return `${yyyy}-${mm}-${dd}`;
};

function checkPermission(controllerName, callback) {

    $.ajax({
        url: '/Permission/GetCurrentMenuPermission',
        type: 'GET',
        data: {
            controllerName: controllerName
        },
        success: function (res) {

            if (!res.success)
                return;

            permission = res;

            $("#button_add").toggle(res.add);
            $("#button_edit").toggle(res.edit);
            $("#button_delete").toggle(res.delete);
            $("#button_print").toggle(res.print);
            $("#button_export").toggle(res.export);
            $("#button_mail").toggle(res.mail);
            $("#button_approval").toggle(res.approval);
            $("#button_document").toggle(res.docdetail);

            applyGridPermission();

            if (callback)
                callback();
        }
    });
}
function applyGridPermission() {

    if (!permission)
        return;

    $(".permission-edit").toggle(permission.edit);
    $(".permission-delete").toggle(permission.delete);
}

function checkModificationDays(options) {
    const {
        controller,
        action = 'checkModificationDays',
        vDate,
        rowId = null,
        vType = null,
        onAllowed = null,
        url = `/${controller}/${action}`
    } = options;

    $.ajax({
        url: url,
        type: 'GET',
        dataType: 'json',
        data: { vDate: vDate },

        success: function (response) {

            if (response.success) {

                if (response.isAllowed === 0) {
                    showToast(response.message, { type: "warning" });
                }
                else {

                    // Dynamic callback
                    if (typeof onAllowed === "function") {
                        //onAllowed(rowId);
                        if (vType !== null) {
                            onAllowed(rowId, vType);
                        } else {
                            onAllowed(rowId);
                        }
                    }

                }

            } else {
                showToast(response.message, { type: "error" });
            }
        },

        error: function () {
            showToast("An error occurred!", { type: "error" });
        }
    });
}

function checkPermissionForEntryPage(controllerName) {

    $.ajax({
        url: '/Permission/GetCurrentEntryPagePermission',
        type: 'GET',
        data: {
            controllerName: controllerName
        },
        success: function (res) {
            console.log(res);
            if (!res.success)
                return;
            Entrypermission = res;

            $("#button_add").toggle(res.add);
            $("#button_edit").toggle(res.edit);
            $("#button_delete").toggle(res.delete);
            $("#button_print").toggle(res.print);
            $("#button_export").toggle(res.export);
            $("#button_mail").toggle(res.mail);
            $("#button_approval").toggle(res.approval);
            $("#button_document").toggle(res.docdetail);

            $(".permission-edit").toggle(res.edit);
            $(".permission-delete").toggle(res.delete);

            applyGridPermissionforEntryPage();
        }
    });
}

function applyGridPermissionforEntryPage() {

    if (!permission)
        return;

    $(".permission-edit").toggle(permission.edit);
    $(".permission-delete").toggle(permission.delete);
}

function SetFYDate(inputId, loginDate) {
    var $input = $('#' + inputId);
    var d = new Date(loginDate);
    var fyStartYear = d.getMonth() >= 3 ? d.getFullYear() : d.getFullYear() - 1;
    var minDate = fyStartYear + '-04-01';
    var maxDate = loginDate;
    $input.attr('min', minDate).attr('max', maxDate).val(maxDate);

    $input.on('change', function () {
        var selectedDate = new Date(this.value);
        var min = new Date(minDate);
        var max = new Date(maxDate);

        if (selectedDate < min || selectedDate > max) {
            toastr.info('Please select a date within the Financial Year and not greater than Login Date.');
            this.value = maxDate;
        }
    });
}