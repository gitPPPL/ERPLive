
const urlParams = new URLSearchParams(window.location.search);

const refNo = urlParams.get('refNo');
const refType = urlParams.get('refType') || 'SAUD';

$(document).ready(async function () {

    await wireEvents();

    await BindShipmentDropdown();
    await BindBankDDL();
    await BindPortDDL();

    if (refNo) {
        LoadSalesSaudaExportData();
        await LoadEditData();
    } else {
        await GetVNo();
    }

    $('#btn_save').on('click', async function (e) {
        e.preventDefault();
        await SaveExportDetails();
    });

});

async function wireEvents() {

    $('#ddlBankName').on('change', async function () {
        await GetBankDetails();
    });

    $('#TxtAttachment').on('change', function () {

        const file = this.files[0];

        if (!file) {
            return;
        }

        const fileUrl = URL.createObjectURL(file);
        const fileType = file.type || getMimeType(file.name);

        $('#attachmentFileName').text(file.name);

        $('#previewContainer').css('display', 'flex');

        $('#btnPreviewImage').data('file-url', fileUrl).data('file-type', fileType).show();

    });

    $('#btnPreviewImage').on('click', function () {

        const fileUrl = $(this).data('file-url');
        const fileType = $(this).data('file-type');

        if (!fileUrl) return;

        $('#previewImage, #previewPdf').hide();

        if (fileType === 'application/pdf') {

            $('#previewPdf').attr('src', fileUrl).show();

        } else if (fileType.startsWith('image/')) {

            $('#previewImage').attr('src', fileUrl).show();

        } else {
            showToast('Preview not supported for this file type', {type: 'warning'});
            return;
        }

        bootstrap.Modal.getOrCreateInstance(document.getElementById('imagePreviewModal')).show();
    });

    $('#btnDeleteAttachment').on('click', function () {

        $('#TxtAttachment').val('').show();
        $('#attachmentFileName').text('');
        $('#previewContainer').hide();
        $('#btnPreviewImage').removeData('file-url').removeData('file-type');
        $('#previewImage').attr('src', '').hide();
        $('#previewPdf').attr('src', '').hide();
    });
}

async function BindShipmentDropdown() {

    await Promise.all([
        
        bindDropdown('ExportDetails', 'NotifyTo', '#ddlNotify1', 'Select', null, null, false, null, true),
        bindDropdown('ExportDetails', 'NotifyTo', '#ddlNotify2', 'Select', null, null, false, null, true),
        bindDropdown('ExportDetails', 'CountryMast', '#ddlFinalCountry', 'Select Country', null, null, false, null, true),
        bindDropdown('ExportDetails', 'ComissionAgent', '#ddlCommissionAgent', 'Select', null, null, false, null, true),
      
    ]);
}

function LoadSalesSaudaExportData() {

    const storedData = sessionStorage.getItem('salesSaudaExportData');

    if (!storedData) return;

    const data = JSON.parse(storedData);

    console.log("data", data);

    $('#txtRefNo').val(data.refNo || '');
    $('#txtRefType').val(data.refType || '');

    $('#txtCustomerName').val(data.customerName || '');
    $('#txtAddress1').val(data.address1 || '');
    $('#txtAddress2').val(data.address2 || '');
    $('#txtAddress3').val(data.address3 || '');
    $('#txtContactNo').val(data.contactNo || '');
    $('#txtPartyStation').val(data.partyStation || '');
    $('#txtItemType').val(data.itemType || '');

    $('#txtItemType').val(data.itemType || '');
    $('#txtWeight').val(data.weight || '');
    $('#txtRate').val(data.rate || '');
    $('#txtCurrency').val(data.currency || '');
    $('#txtFreightTerm').val(data.freightTerm || '');
    $('#txtFreightRate').val(data.freightRate || '');

    //sessionStorage.removeItem('salesSaudaExportData');
}

async function GetVNo() {
    try {
        const response = await fetch('/ExportDetails/GenerateVNo');

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            return;
        }

        $('#txtVNo').val(data.v_NO);

        const docId = 'EXSD' + $('#txtVNo').val();
        console.log("docId", docId);

    } catch (error) {
        console.error('Error generating VNo:', error);
    }
}

async function BindBankDDL() {

    const vType = $('#txtRefType').val() || 'SAUD';

    try {
        const response = await fetch(
            `/ExportDetails/GetBankList?vType=${encodeURIComponent(vType)}`
        );

        if (!response.ok) throw new Error('Failed to load banks');

        const banks = await response.json();
        const ddl = $('#ddlBankName');

        ddl.empty().append('<option value="">Select Bank Name</option>');

        banks.forEach(bank => {
            ddl.append(new Option(bank.text, bank.value));
        });

        const hdfcBank = banks.find(bank =>
            (bank.text || '').trim().toUpperCase().includes('HDFC')
        );

        if (hdfcBank) {
            ddl.val(hdfcBank.value).trigger('change');
        }

    } catch (error) {
        console.error(error);
        showToast('Error loading banks', { type: 'error' });
    }
}

// ================================
// BANK CHANGE → AUTOFILL DETAILS
// ================================
async function GetBankDetails() {

    const bankCode = $('#ddlBankName').val();
    const vType = $('#txtRefType').val() || 'SAUD';

    const fields = ['#txtAccountNumber', '#txtBankIFSC', '#txtBankADCode', '#txtBankSwiftCode', '#txtBranch' ];

    fields.forEach(field => $(field).val(''));

    if (!bankCode) return;

    try {
        const response = await fetch(
            `/ExportDetails/GetBankDetails?bankCode=${encodeURIComponent(bankCode)}&vType=${encodeURIComponent(vType)}`
        );

        if (!response.ok) throw new Error('Failed to load bank details');

        const data = await response.json();

        if (!data.success) {
            showToast(data.message || 'Bank not found', { type: 'warning' });
            return;
        }

        $('#txtAccountNumber').val(data.accountNumber || '');
        $('#txtBankIFSC').val(data.ifscCode || '');
        $('#txtBankADCode').val(data.adCode || '');
        $('#txtBankSwiftCode').val(data.swiftCode || '');
        $('#txtBranch').val(data.branchName || '');

    } catch (error) {
        console.error(error);
        showToast('Error loading bank details', { type: 'error' });
    }
}

async function BindPortDDL() {
    try {
        const response = await $.ajax({
            url: '/ExportDetails/GetPortList',
            type: 'GET'
        });

        const portDropdowns = ['#txtPOL', '#txtPOD', '#txtFPOD', '#txtSeaPort'];

        portDropdowns.forEach(selector => {

            const $ddl = $(selector);

            $ddl.empty();

            $ddl.append($('<option>', { value: '', text: 'Select Port'}));

            response.forEach(item => {

                const displayText =`${item.code} | ${item.name} | ${item.state}`;

                $ddl.append(
                    $('<option>', {
                        value: item.code,
                        text: displayText
                    })
                );
            });

            $ddl.select2({
                width: '100%',
                placeholder: 'Select Port',
                allowClear: true
            });

            $ddl.on('select2:open', function () {
                setTimeout(function () {
                    document.querySelector('.select2-container--open .select2-search__field')?.focus();
                }, 0);
            });
        });

    } catch (error) {
        console.error('Error loading port list:', error);
        showToast("failed to load Port List: " + error, { type: "error" });
    }
}

async function SaveExportDetails() {

    const vNo = $('#txtVNo').val();
    const refNo = $('#txtRefNo').val();
    const refType = $('#txtRefType').val();

    if (!vNo || !refNo || !refType) {
        showToast('Document No and Reference details are required', {type: 'warning' });
        return;
    }

    const formData = new FormData();

    const fields = {
        V_TYPE: 'EXSD',
        V_NO: vNo,
        REF_TYPE: refType,
        REF_NO: refNo,

        INCO_TERM: $('#txtFreightTerm').val(),
        EX_RATE: $('#txtFreightRate').val(),

        POL_CODE: $('#txtPOL').val(),
        POD_CODE: $('#txtPOD').val(),
        FPOD: $('#txtFPOD').val(),
        FINAL_COUNTRY: $('#ddlFinalCountry').val(),

        BL_TYPE: $('#ddlBLType').val(),
        SEAPORT_CODE: $('#txtSeaPort').val(),

        NOTIFY_PERSON: $('#ddlNotify1').val(),
        NOTIFY_TO: $('#ddlNotify2').val(),

        COMMISSION_AGENT: $('#ddlCommissionAgent option:selected').text().trim(),

        LC_NUMBER: $('#txtLCNumber').val(),
        LC_DATE: $('#dtLCDate').val(),

        FRT_AMT: $('#txtOceanFreight').val(),
        INSU_AMT: $('#txtInsurance').val(),
        COMM_AMT: $('#txtCommission').val(),

        BANK_CODE: $('#ddlBankName').val(),
        BANK_BRANCH: $('#txtBranch').val(),
        ACT_NUMBER: $('#txtAccountNumber').val(),
        IFSC_CODE: $('#txtBankIFSC').val(),
        AD_CODE: $('#txtBankADCode').val(),
        SWIFT_CODE: $('#txtBankSwiftCode').val(),

        NATURE_PACKING: $('#txtPacking').val(),
        TECH_SPEC: $('#txtTechnicalSpecification').val(),
        SPL_INST: $('#txtSpecialInstruction').val(),

        COMM_NARR: $('#txt1Commission').val()
    };

    Object.entries(fields).forEach(([key, value]) => {
        formData.append(key, value ?? '');
    });

    const attachment = $('#TxtAttachment')[0]?.files[0];

    if (attachment) {
        formData.append('Attachment', attachment);
        formData.append('FILE_NAME', attachment.name);
    }

    const $saveButton = $('#btn_save');

    try {
        $saveButton.prop('disabled', true);

        const response = await $.ajax({
            url: '/ExportDetails/SaveAndUpdateData',
            type: 'POST',
            data: formData,
            processData: false,
            contentType: false
        });

        if (response.success) {
            showToast(response.message || 'Saved successfully', { type: 'success' });

            makeFormReadOnly();
        } else {
            showToast(response.message || 'Failed to save', { type: 'error' });
        }

    } catch (error) {
        console.error('Export Details save error:', error);
        showToast('Error saving Export Details', { type: 'error' });

    } finally {
        $saveButton.prop('disabled', false);
    }
}

async function LoadEditData() {

    if (!refNo) {
        return;
    }

    try {

        const response = await fetch(
            `/ExportDetails/LoadEditData?REF_NO=${encodeURIComponent(refNo)}&REF_TYPE=${encodeURIComponent(refType)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load Export Details');
        }

        const result = await response.json();

        console.log("LoadEditData result:", result);
        console.log("Export Details:", result.exportDetails);

        // ==========================================
        // NO EXISTING EXPORT DETAILS
        // ==========================================

        if (!result.exportDetails || Object.keys(result.exportDetails).length === 0) {

            $('#txtRefNo').val(refNo);
            $('#txtRefType').val(refType);

            await GetVNo();

            return;
        }
        const data = result.exportDetails;

        console.log("Binding Export Data:", data);

        // ==========================================
        // HEADER / REFERENCE
        // ==========================================

        $('#txtVNo').val(data.V_NO || '');
        $('#txtRefNo').val(data.REF_NO || '');
        $('#txtRefType').val(data.REF_TYPE || '');

        // ==========================================
        // EXPORT DETAILS
        // ==========================================

        $('#txtFreightTerm').val(data.INCO_TERM || '');
        $('#txtFreightRate').val(data.EX_RATE || '');

        $('#txtPOL').val(data.POL_CODE || '').trigger('change');
        $('#txtPOD').val(data.POD_CODE || '').trigger('change');
        $('#txtFPOD').val(data.FPOD || '').trigger('change');

        $('#ddlFinalCountry').val(data.FINAL_COUNTRY || '').trigger('change');

        $('#ddlBLType').val(data.BL_TYPE || '').trigger('change');

        $('#txtSeaPort').val(data.SEAPORT_CODE || '').trigger('change');

        $('#ddlNotify1').val(data.NOTIFY_PERSON || '').trigger('change');

        $('#ddlNotify2').val(data.NOTIFY_TO || '') .trigger('change');

        // $('#ddlCommissionAgent').val(data.COMMISSION_AGENT || '').trigger('change');
        const commissionAgent = (data.COMMISSION_AGENT || '').trim();
        const $commissionDDL = $('#ddlCommissionAgent');
        const matchedOption = $commissionDDL.find('option').filter(function () {
            return $(this).text().trim().toLowerCase() === commissionAgent.toLowerCase();
        }).first();

        if (matchedOption.length) {
            $commissionDDL.val(matchedOption.val()).trigger('change');
        } else {
            console.warn('Commission Agent not found:', commissionAgent);
        }

        // ==========================================
        // LC DETAILS
        // ==========================================

        $('#txtLCNumber').val(data.LC_NUMBER || '');

        if (data.LC_DATE) {
            $('#dtLCDate').val(data.LC_DATE.split('T')[0]);
        }

        // ==========================================
        // AMOUNT DETAILS
        // ==========================================

        $('#txtOceanFreight').val(data.FRT_AMT ?? '');
        $('#txtInsurance').val(data.INSU_AMT ?? '');
        $('#txtCommission').val(data.COMM_AMT ?? '');

        // ==========================================
        // BANK DETAILS
        // ==========================================

        $('#ddlBankName').val(data.BANK_CODE || '').trigger('change');

        $('#txtBranch').val(data.BANK_BRANCH || '');
        $('#txtAccountNumber').val(data.ACT_NUMBER || '');
        $('#txtBankIFSC').val(data.IFSC_CODE || '');
        $('#txtBankADCode').val(data.AD_CODE || '');
        $('#txtBankSwiftCode').val(data.SWIFT_CODE || '');

        // ==========================================
        // OTHER DETAILS
        // ==========================================

        $('#txtPacking').val(data.NATURE_PACKING || '');
        $('#txtTechnicalSpecification').val(data.TECH_SPEC || '');
        $('#txtSpecialInstruction').val(data.SPL_INST || '');

        $('#txt1Commission').val(data.COMM_NARR || '');

        // ================================
        // LOAD ATTACHMENT
        // ================================

        const images = result.images || [];

        if (images.length > 0) {

            const attachment = images[0];

            console.log("Attachment Data:", attachment);

            const base64 = attachment.IMG_FILE;
            const fileName = attachment.FILE_NAME;

            if (base64 && fileName) {

                const fileType = attachment.FILE_TYPE || getMimeType(fileName);

                const fileUrl = `data:${fileType};base64,${base64}`;

                $('#TxtAttachment').hide();

                $('#attachmentFileName').text(fileName);

                $('#previewContainer').css('display', 'flex');

                $('#btnPreviewImage').data('file-url', fileUrl).data('file-type', fileType).show();
            }
        }

    } catch (error) {

        console.error('Error loading Export Details:', error);

        showToast(
            'Error loading Export Details',
            { type: 'error' }
        );
    }
}

function getMimeType(fileName) {

    const ext = fileName?.split('.').pop()?.toLowerCase();

    switch (ext) {

        case 'jpg':
        case 'jpeg':
            return 'image/jpeg';

        case 'png':
            return 'image/png';

        case 'gif':
            return 'image/gif';

        case 'bmp':
            return 'image/bmp';

        case 'webp':
            return 'image/webp';

        case 'pdf':
            return 'application/pdf';

        default:
            return 'application/octet-stream';
    }
}

function makeFormReadOnly() {

    $('#ExportSalesForm').find('input:not(#TxtAttachment), textarea').prop('readonly', true);
    $('#ExportSalesForm').find('select').prop('disabled', true);
    $('#TxtAttachment').prop('disabled', true);
    $('#btn_save').prop('disabled', true);
    $('#btnPreviewImage').prop('disabled', false).show();
    $('#btnDeleteAttachment').prop('disabled', true).hide();
}

$('#btnDelete').on('click', async function () {

    const refNoValue = $('#txtRefNo').val();
    const refTypeValue = $('#txtRefType').val();
    const vNoValue = $('#txtVNo').val();

    if (!refNoValue || !refTypeValue || !vNoValue) {
        showToast('Delete details not found', { type: 'warning' });
        return;
    }

    const result = await Swal.fire({
        title: 'Are you sure?',
        text: 'This Export Sales Entry will be deleted.',
        icon: 'warning',
        showCancelButton: true,
        confirmButtonText: 'Yes, Delete',
        cancelButtonText: 'Cancel'
    });

    if (!result.isConfirmed) {
        return;
    }

    try {

        const response = await $.ajax({
            url: '/ExportDetails/DeleteData',
            type: 'POST',
            data: {
                refNo: refNoValue,
                refType: refTypeValue,
                vNo: vNoValue
            }
        });

        if (response.success) {

            await Swal.fire({
                title: 'Deleted Successfully',
                text: response.message || 'Data deleted successfully.',
                icon: 'success',
                confirmButtonText: 'OK'
            });
            sessionStorage.removeItem('salesSaudaExportData');
            window.location.reload();

        } else {

            await Swal.fire({
                title: 'Delete Failed',
                text: response.message || 'Failed to delete data.',
                icon: 'error',
                confirmButtonText: 'OK'
            });
        }

    } catch (error) {

        console.error('Delete Export Details error:', error);
        showToast('Error deleting Export Details', { type: 'error' });
    }
});

