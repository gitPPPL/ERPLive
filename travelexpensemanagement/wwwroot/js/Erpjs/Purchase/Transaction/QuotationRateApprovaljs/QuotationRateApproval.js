var fieldsId = ['ddlDocType', 'dtDocDate', 'ddlItem', 'dtFrom', 'chkFromDate', 'dtTo', 'chkToDate', 'ddlVendor',
                'txtGroupNo', 'ddlSortedBy','btnFill', 'btnComparison', 'btnShowQcDetail'];

$('#tblItemDetailsQR tbody tr').each(function () {
    $(this).find('td:eq(0)').addClass('sticky-col');
});

let rowsAttachment = [];
let docId = "";
let readOnly;
const selectedItemSet = new Set();
const urlParams = new URLSearchParams(window.location.search);
let isReadOnly = urlParams.get('readOnly') === 'true';

$(async function () {
    try {
        await GetDocTypeAsync();
        $('#ddlDocType').prop('selectedIndex', 0);
        handleDocLoad();
    } catch (error) {
        toastr.error('Failed to load document types: ' + error);
    }
});

$(document).ready(function () {
    $("#ddlDocType").focus();
    GetVendorList();
    GetItemList();

    $(document).on('click', '#QuotRtDetail', '#attach-del', function(){
        $(this).closest('tr').remove();
    });

    $(document).on('click', '#attach-Row', function () {
        addAttachmentRow();
    });

    $('#fileInput').on('change', function (e) {

        const files = e.target.files;

        $('#fileList').empty();
        rowsAttachment = [];

        Array.from(files).forEach(file => {

            const reader = new FileReader();

            reader.onload = function (ev) {

                const base64 = ev.target.result.split(',')[1];
                const mime = file.type || 'application/octet-stream';

                // store
                rowsAttachment.push({
                    FileName: file.name,
                    FileContentBase64: base64,
                    FileType: mime
                });

                // UI
                const card = `
                    <div class="erp-file-row" data-filename="${file.name}">

                        <div class="erp-file-preview">
                            ${mime.startsWith('image/')
                                ? `<img src="${ev.target.result}" class="erp-file-thumbnail">`
                                : mime === 'application/pdf'
                                    ? `<i class="fa fa-file-pdf-o" style="font-size:40px;color:red;"></i>`
                                    : `<i class="fa fa-file"></i>`
                            }
                        </div>

                        <div class="erp-file-info">
                            <div class="erp-file-name">${file.name}</div>
                            <div class="erp-file-type">${file.type || 'FILE'}</div>
                        </div>

                        <div class="erp-file-actions">

                                <button type="button"
                                    class="erp-btn view btn-view-attachment"
                                    data-src="${ev.target.result}"
                                    data-type="${mime}">
                                    <i class="fa fa-eye"></i>
                                </button>

                            <button type="button"
                                    class="erp-btn delete btn-delete-attachment">
                                <i class="fa fa-trash"></i>
                            </button>

                        </div>

                    </div>
                `;

                $('#fileList').append(card);
            };

            reader.readAsDataURL(file);
        });
    });
        
    $(document).on('click', '.btn-delete-attachment', function () {

        const row = $(this).closest('.erp-file-row');
        const fileName = row.data('filename');

        // remove from array
        rowsAttachment = rowsAttachment.filter(x => x.FileName !== fileName);

        row.remove();
    });

    $(document).on('click', '.btn-view-attachment', function (e) {
        e.preventDefault();

        const src = $(this).data('src');  
        const type = $(this).data('type');getFileType

        const img = document.getElementById('previewImage');
        const pdf = document.getElementById('previewPdf');
               
        img.style.display = "none";
        pdf.style.display = "none";

        const modalEl = document.getElementById('imagePreviewModal');
        const modal = bootstrap.Modal.getOrCreateInstance(modalEl);

        if (!src || !type) return;

        if (type.startsWith('image/')) {
            img.src = src;   
            img.style.display = "block";
        }
        else if (type === 'application/pdf') {
            pdf.src = src;
            pdf.style.display = "block";
        }

        modal.show();
    });

    $('#ddlSortedBy').on('change', function () {

        if ($('#tblItemDetailsQR tbody tr').length > 0) {

            $('#tblItemDetailsQR tbody').empty();

            FilterDataList();
        }

    });

    $('#chkFromDate').on('change', function () {

        const checked = $(this).is(':checked');

        $(this).val(checked ? '1' : '0');

        $('#dtFrom')
            .prop('disabled', !checked)
            .val(checked ? new Date().toISOString().split('T')[0] : '');
    });

    $('#chkToDate').on('change', function () {

        const checked = $(this).is(':checked');

        $(this).val(checked ? '1' : '0');

        $('#dtTo')
            .prop('disabled', !checked)
            .val(checked ? new Date().toISOString().split('T')[0] : '');
    });

    $('#ddlItem').on('change', function () {

    
        let code = $(this).val();
        let name = $('#ddlItem option:selected').text();

        if (!code) return;

        if (selectedItemSet.has(code)) {
            
            return;
        }

        addItemToTable(code, name);

        requestAnimationFrame(() => {
            flushItemTable();
            
        });

        $(this).val(null).trigger('change');

    });

    function addItem(selectedItemCode, selectedItemName) {

        if (selectedItemSet.has(selectedItemCode)) return;

        selectedItemSet.add(selectedItemCode);

        const rowHtml = `
            <tr data-code="${selectedItemCode}">
                <td>${selectedItemName}</td>
                <td class="action-col">
                    <div class="action-wrap">
                        <button type="button" class="act-btn delete btn-itemRemove">
                            <i class="fa fa-trash btn-delete"></i>
                        </button>
                    </div>
                </td>
            </tr>
        `;
        setTimeout(() => {
            $('#itemTable tbody')[0].insertAdjacentHTML('beforeend', rowHtml);
        }, 0);

        $('#ddlItem').val(null).trigger('change');
    }

    let itemBuffer = [];

    function addItemToTable(code, name) {

        if (selectedItemSet.has(code)) return;

        selectedItemSet.add(code);
       

        itemBuffer.push(`
            <tr data-code="${code}">
                <td>${name}</td>
                <td>
                    <button type="button" class="act-btn delete btn-itemRemove">
                            <i class="fa fa-trash btn-delete"></i>
                    </button>
                </td>
            </tr>
        `);
    }

    function flushItemTable() {

        if (itemBuffer.length === 0) return;

        $('#itemTable tbody')[0].insertAdjacentHTML(
            'beforeend',
            itemBuffer.join('')
        );

        itemBuffer = [];
    }

    function isItemAlreadyAdded(itemCode) {
        return $('#itemTable tbody tr').filter(function () {
            return $(this).data('code') == itemCode;
        }).length > 0;
    }

    $(document).on('click', '.btn-itemRemove', function () {

        const code = $(this).closest('tr').data('code');

        selectedItemSet.delete(code);

        $(this).closest('tr').remove();
    });

    $('#ddlVendor').on('change', function () {
            let selectedvendorName = $('#ddlVendor option:selected').text();
            let selectedvendorId = $('#ddlVendor').val();
        if (selectedvendorId && !isVendorAlreadyAdded(selectedvendorId)) {
            $('#vendorTable tbody').append(`
            <tr data-code="${selectedvendorId}">
                <td>${selectedvendorName}</td>
                <td class="action-col">
                    <div class="action-wrap">
                        <button type="button" class="act-btn delete btn-vendeorRemove"> <i class="fa fa-trash"></i></button>
                    </div>
                </td>
                </tr>
        `);
        $('#ddlVendor').val("");
        }
    });

    $(document).on('click', '#vendorTable tbody .btn-vendeorRemove', function () {
        $(this).closest('tr').remove();
    });

    $(document).on('click', '.attach-row', function () {
        addAttachmentRow();
    });

    function isVendorAlreadyAdded(item) {
        let exists = false;
        $('#vendorTable tbody tr').each(function () {
            if ($(this).find('td:first').text() === item) {
                exists = true;
            }
        });
        return exists;
    }

    $('#dtFrom, #dtTo').on('change', function () {
        validateDateRange();
    });

    $('#btnFill').on('click', function () {
        FilterDataList();    
    });

    $('#btn-save').on('click', async function (e) {
        e.preventDefault();

        const isValidDate = await checkValidDate();
        if (!isValidDate) return;

        if (!validateData()) {
            return;
        }

        try {
            const tableData = await collectFormData();

            if (docId) {
                UpdateData(tableData);
            } else {
                SaveData(tableData);
            }
        } catch (error) {
            showToast("An Error occured While Saving the Data", { type: "error" });
        }

    });

    $('#tblItemDetailsQR tbody').on('contextmenu', 'tr', function (e) {
        e.preventDefault();
        const $row = $(this);
        const rowIndex = $row.data('row-index');
        var ITEM_CODE= $row.find(`#row-${rowIndex}-ITEM_CODE`).attr('value');
        selectedItemCode = ITEM_CODE;

        $('#customContextMenu').css({ top: e.pageY + 'px', left: e.pageX + 'px' }).show();

    });

    $(document).on('click', '#purchaseReceiptLink', function (e) {
        e.preventDefault();
        if (selectedItemCode) {
            fillPurchaseDataDetail(selectedItemCode);
        }
        $('#customContextMenu').hide();
    });

    $(document).on('click', '#QuotationAprvalLink', function (e) {
        e.preventDefault();
        if (selectedItemCode) {
            fillPurchaseApprovalData(selectedItemCode);
        }
        $('#customContextMenu').hide();
    });

    $(document).on('click', '#PurchaseOrderLink', function (e) {
        e.preventDefault();
        if (selectedItemCode) {
            fillPurchaseOrderData(selectedItemCode);
        }
        $('#customContextMenu').hide();
    });

    $(document).on('click', function () {
        $('#customContextMenu').hide();
    });

});

function GetDocTypeAsync(selectedValue) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url:'/QuotationRateApproval/GetDocType',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    const $dropdown = $('#ddlDocType');
                    $dropdown.empty();
                    $.each(response.data, function (index, item) {
                        $dropdown.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                    });

                    if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
                        $dropdown.val(selectedValue).trigger('change');
                    } else {
                        $dropdown.prop('selectedIndex', 0);
                    }

                    resolve();
                } else {
                    reject("Invalid response status.");
                }
            },
            error: function (xhr, status, error) {
                showToast("Document Type Load Failed", { type: "error" });
                reject(error);
            }
        });
    });
}

async function handleDocLoad() {

    docId = urlParams.get('id');   
    readOnly = isReadOnly;         

    if (docId) {

        if (readOnly) {
            $('#ddlDocType').prop('disabled', true); 
        }

        await GetDocData(docId, readOnly);

    } else {

        const Vtype = $('#ddlDocType').val();
        if (Vtype) {
            GetDocid(Vtype);
        }

        const today = new Date();
        const todayDate = today.getFullYear() + '-' +
            (today.getMonth() + 1).toString().padStart(2, '0') + '-' +
            today.getDate().toString().padStart(2, '0');

        $('#dtDocDate').val(todayDate);

        $('#chkFromDate').prop('checked', false).val('0');
        $('#chkToDate').prop('checked', false).val('0');

        $('#dtFrom').val('').prop('disabled', true);
        $('#dtTo').val('').prop('disabled', true);

        //$('#dtFrom').val(todayDate);
        //$('#dtTo').val(todayDate);
    }
}

function fillPurchaseDataDetail(selectedItemCode)
{
    $.ajax({
        url:'/QuotationRateApprovalList/GetpurchaseReceiptHistory',
        type: 'GET',
        dataType: 'JSON',
        data:{itemcode : selectedItemCode},
        success: function (response) {
            if (response.status) {
                console.log("Purchase Receipt History", response);
                console.log(response.data);
                let $tableId = $('#tblpurchasereceipthistory tbody'); 
                $tableId.empty();
                $.each(response.data, function (index, item) {
                    var rowDt = `
                    <tr>
                        <td>${item.VNo}</td>
                        <td>${item.Date}</td>
                        <td>${item.Supplier}</td>
                        <td>${item.ItemName}</td>
                        <td>${item.Make}</td>
                        <td>${item.Unit}</td>
                        <td>${item.Qty}</td>
                        <td>${item.Rate}</td>
                        <td>${item.OthAmt}</td>
                        <td>${item.CGSTPer}</td>
                        <td>${item.SGSTPer}</td>
                        <td>${item.IGSTPer}</td>
                        <td>${item.PackPer}</td>
                        <td>${item.DiscPer}</td>
                        <td>${item.LDRate}</td>
                        <td>${item.Remarks}</td>
                        <td>${item.Status}</td>
                    </tr>
                    `;
                    $tableId.append(rowDt);
                });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error While filling Data" + error,  { type: "error" });
            console.error('AJAX Error:', status, error);
        }
    });
}

function fillPurchaseApprovalData(selectedItemCode)
{
    $.ajax({
        url:'/QuotationRateApprovalList/GetQuotationApprovalData',
        type: 'GET',
        dataType: 'JSON',
        data:{itemcode : selectedItemCode},
        success: function (response) {
            if (response.status) {
                console.log("Approve Data", response)
                let $tableId = $('#tblquotationapprovalhistory tbody'); 
                $tableId.empty();

                $.each(response.data, function (index, item) {
                    var rowDt = `
                    <tr>
                        <td>${item.V_NO}</td>
                        <td>${item.V_DATE}</td>
                        <td>${item.party}</td>
                        <td>${item.ITEM_NAME}</td>
                        <td>${item.MAKE_CODE}</td>
                        <td>${item.UOM_CODE}</td>
                        <td>${item.RECD_QTY}</td>
                        <td>${item.RATE}</td>
                        <td></td>
                        <td>${item.CGST_PER}</td>
                        <td>${item.SGST_PER}</td>
                        <td>${item.IGST_PER}</td>
                        <td>${item.PACK_PER}</td>
                        <td>${item.DISC_PER}</td>
                        <td></td>
                        <td></td>
                        <td>${item.OTH_EXPS}</td>
                        <td>${item.LD_RATE}</td>
                        <td>${item.PURCHASER_REMARKS}</td>
                        <td>${item.STATUS}</td>
                    </tr>
                    `;
                    $tableId.append(rowDt);
                });
            }
        },
        error: function (xhr, status, error) {
            console.error('AJAX Error:', error);
           
        }
    });
}

function fillPurchaseOrderData(selectedItemCode)
{
    $.ajax({
        url:'/QuotationRateApprovalList/GetPurchaseOrderData',
        type: 'GET',
        dataType: 'JSON',
        data:{itemcode : selectedItemCode},
        success: function (response) {
            if (response.status) {
                let $tableId = $('#tblorderhistory tbody'); 
                $tableId.empty();
               
                $.each(response.data, function (index, item) {
                    var rowDt = `
                    <tr>
                        <td>${item.V_NO}</td>
                        <td>${item.V_DATE}</td>
                        <td>${item.Party}</td>
                        <td>${item.ITEM_NAME}</td>
                        <td>${item.MAKE_CODE}</td>
                        <td>${item.UOM_CODE}</td>
                        <td>${item.QTY}</td>
                        <td>${item.RATE}</td>
                        <td>${item.CGST_PER}</td>
                        <td>${item.SGST_PER}</td>
                        <td>${item.IGST_PER}</td>
                        <td>${item.PACK_PER}</td>
                        <td>${item.DISC_PER}</td>
                        <td>${item.OTH_AMT}</td>
                        <td>${item.LAND_RATE}</td>
                        <td>${item.REMARKS}</td>
                        <td>${item.STATUS}</td>
                    </tr>
                    `;
                    $tableId.append(rowDt);
                });
            }
        },
        error: function (xhr, status, error) {
            console.error('AJAX Error:', status, error);
        }
    });
}

function setTableReadonly(tableId, isReadonly) {
    const table = document.getElementById(tableId);
    if (!table) return;

    const inputs = table.querySelectorAll('input, textarea, select');

    inputs.forEach(elem => {
        if (elem.tagName === 'SELECT') {
            elem.disabled = isReadonly;
        } else if (elem.type === 'checkbox' || elem.type === 'radio') {
            elem.disabled = isReadonly;
        } else {
            elem.readOnly = isReadonly;
        }
    });
}

function applyReadOnlyMode() {

    $('#ddlDocType,#ddlStatus,#dtDocDate,#txtGroupNo,#ddlSortedBy , #ddlItem, #ddlVendor').prop('disabled', true);
    $('#dtFrom,#dtTo') .prop('disabled', true);
    $('#chkFromDate,#chkToDate').prop('disabled', true);
    $('#btn-save').hide();
    $('#btnFill,#btnShowQcDetail').prop('disabled', true);
    $('#browseBtn').hide();
    $('#fileInput').prop('disabled', true);
    $('.btn-delete-attachment').hide();
    $('#tblItemDetailsQR tbody select').prop('disabled', true);

}

function FilterDataList() {
    const isFromSelected = $('#chkFromDate').is(':checked');
    const isToSelected = $('#chkToDate').is(':checked');
    const fromDate = isFromSelected ? $('#dtFrom').val() : null;
    const toDate = isToSelected ? $('#dtTo').val() : null;
    const GroupId = $('#txtGroupNo').val().trim();
    const sortType = $('#ddlSortedBy').val();
    
    // Collect vendor IDs
    const vendorList = [];
    $('#vendorTable tbody tr').each(function () {
        const id = $(this).data('code');
        if (id) vendorList.push(id.toString());
    });

    const itemList = Array.from(selectedItemSet);
    const payload = {
        VDate: toNullableDate($('#dtDocDate').val()),
        FromDt: toNullableDate(fromDate),
        ToDt: toNullableDate(toDate),
        groupCode: toNullableInt(GroupId),
        SortBy: sortType,
        VendorList: vendorList,
        ItemList: itemList
    };

    $.ajax({
        url: '/QuotationRateApproval/GetFilterItemdetails',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (response) {
            if (
                response.status === true &&
                Array.isArray(response.data) &&
                response.data.length > 0
            ) {
                applyOldVBPriority(response.data);
                //fillItemDetailsTableForFill(response.data);
                fillItemDetailsTableForFill(response.data, 1, false);
                //selectedItemSet.clear();
                //$('#itemTable tbody').empty();
                //$('#ddlItem').val(null).trigger('change');
            } else {
                showToast("No Data Found", { type: "warning" });
            }
        },
        error: function (xhr) {
            showToast("Error " + xhr.responseText, { type: "error" });
        }
    });
}

function validateDateRange() {
    const fromDate = $('#dtFrom').val();
    const toDate = $('#dtTo').val();

    if (fromDate && toDate && new Date(fromDate) > new Date(toDate)) {
        showToast("From Date Cannot Be greater Than To Date ", { type: "warning" });
        $('#dtTo').val('');
    }
}

function GetDocData(MasterTblId, readOnly) {
    selectedItemSet.clear();
    $.ajax({
        url: '/QuotationRateApproval/GetQuotRtApvrlDetailsById',
        type: 'GET',
        data: { id: MasterTblId },
        success: function (res) {
            if (res.status) {
                fillFormFields(res.detail[0]);
                fillItemDetailsTable(res.detail);
                loadAttachmentsForEdit(res.attachment);
                if (readOnly) {
                    applyReadOnlyMode();
                }
            }
        },
        error: function () {
            showToast("Failed To Load Data", { type: "error" });
        }
    });
}

function getFileType(fileName) {

    const ext = fileName?.split('.').pop()?.toLowerCase();

    switch (ext) {
        case 'jpg':
        case 'jpeg':
        case 'png':
        case 'gif':
        case 'webp':
            return 'image/jpeg';

        case 'pdf':
            return 'application/pdf';

        default:
            return '';
    }
}

function loadAttachmentsForEdit(attachments) {

    const $list = $('#fileList');
    $list.empty();
    rowsAttachment = [];

    if (!attachments || attachments.length === 0) return;

    attachments.forEach(att => {

        const fileName = att.FileName || att.ATTACHMENT || att.filE_NAME;
        const base64 = att.FileContentBase64 || att.ATTACHMENT_FILE || att.filE_BASE64;
        const type = att.filE_TYPE || att.FileType || getFileType(fileName);

        rowsAttachment.push({
            FileName: fileName,
            FileContentBase64: base64,
            FileType: type
        });

        const dataUrl = normalizeBase64(base64, type);

        const html = `
            <div class="erp-file-row" data-filename="${fileName}">

                    <div class="erp-file-preview">
                        ${type && type.startsWith('image/')
                            ? `<img src="${dataUrl}" class="erp-file-thumbnail">`
                            : `<i class="fa fa-file-pdf-o"></i>`
                        }
                    </div>

                <div class="erp-file-info">
                    <div class="erp-file-name">${fileName}</div>
                    <div class="erp-file-type">${type}</div>
                </div>

                <div class="erp-file-actions">

                    <button type="button"
                            class="erp-btn view btn-view-attachment"
                            data-src="${dataUrl}"
                            data-type="${type}">
                        <i class="fa fa-eye"></i>
                    </button>

                    <button type="button"
                            class="erp-btn delete btn-delete-attachment">
                        <i class="fa fa-trash"></i>
                    </button>

                </div>

            </div>
        `;

        $list.append(html);
    });
}

function SaveData(saveDt) {
    $.ajax({
        url:'/QuotationRateApproval/SaveOrUpdateQuotRateApproval',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(saveDt),
        success: function (response) {
            if (response?.status) {
                showToast("Data Saved Successfully", { type: "success" });
                isReadOnly = true;
                applyReadOnlyMode();
            } else {
                showToast(response?.message || "Save failed. Please try again.", { type: "error" });
            }
        },
        error: function () {
            showToast("Error Occured while saving", { type: "error" });
        }
    });
};

function UpdateData(UpdateDt) {
    $.ajax({
        url:'/QuotationRateApproval/SaveOrUpdateQuotRateApproval',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(UpdateDt),
        dataType: 'json',
        success: function (response) {
            if (response?.status) {
                showToast("Data Update Successfully", { type: "success" });
                isReadOnly = true;
                applyReadOnlyMode();
            } else {
                showToast("Update Failed", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error Occured while Updating" +error, { type: "error" });
        }
    });
}

function fillFormFields(data) {
    $('#txtDocNo').val(data.V_NO || '');
    $('#TxtDocid').val(data.DOC_ID || '');
    let VDate = (data.V_DATE).substring(0, 10);
    $('#dtDocDate').val(VDate || '');
    $('#ddlDocType').val(data.V_TYPE || '');
    $('#ddlStatus').val(data.STATUS || '');
    $('#txtGroupNo').val(data.groupCd || '');
    $('#ddlSortedBy').val(data.sortType || '');
}

async function collectFormData() {
    const id = toNullableInt(docId);
    const attachments = await getAttachmentDetails();

    const data = {
        V_NO: toNullableInt($('#txtDocNo').val()),
        V_DOCID: toNullableString($('#TxtDocid').val()),
        V_DATE: toNullableString($('#dtDocDate').val()),
        V_type: toNullableString($('#ddlDocType').val()),
        status: toNullableString($('#ddlStatus').val()),
        groupCd: toNullableInt($('#txtGroupNo').val()),
        sortType: toNullableString($('#ddlSortedBy').val()),
        SaveOrUpdate: (!id || id === 0) ? 'Save' : 'Update',
        quotationRateApprovalDetail: getItemDetailsFromTable(),
        quotatRateApprovalAttachment: attachments
    };
    return data;
}

function getItemDetailsFromTable() {
    const details = [];
    //const index = 0;
    //const selectElem = document.querySelector(`#row-${index}-FAPROV_STATUS select`);
    //const selectedValue = selectElem ? selectElem.value : "";

    $('#tblItemDetailsQR tbody tr').each(function () {
        const $row = $(this);
        const rowIndex = $row.data('row-index');
        const getText = (field) => $(`#row-${rowIndex}-${field}`).text().trim();
        const getVal = (field) => $(`#row-${rowIndex}-${field}`).attr('value');
        const getSelect = (field) => $(`#row-${rowIndex}-${field}`).val();

        const detail = {
            PARTY_CODE: parseIntSafe(getText('PARTY_CODE')),
            ITEM_CODE: parseIntSafe(getVal('ITEM_CODE')),
            MAKE_CODE: parseIntSafe(getVal('MAKE_CODE')),
            TECH_DESC: getText('TECH_DESC'),
            UOM_CODE: parseIntSafe(getVal('UOM_CODE')),
            REF_NO: parseIntSafe(getVal('REF_NO')),
            REF_DATE: parseDate(getVal('REF_DATE')),
            REF_TYPE: getStringOrNull(getText('REF_TYPE')),
            REF_DOCID: getStringOrNull(getText('REF_DOCID')),
            QTY: parseFloatSafe(getText('QTY')),
            RATE: parseFloatSafe(getText('RATE')),
            AMOUNT: getDecimalOrZero(getText('AMOUNT')),
            PACK_PER: parseFloatSafe(getText('PACK_PER')),
            PACK_AMT: parseFloatSafe(getText('PACK_AMT')),
            DISC_PER: parseFloatSafe(getText('DISC_PER')),
            DISC_AMT: parseFloatSafe(getText('DISC_AMT')),
            FREIGHT: parseFloatSafe(getText('FREIGHT')),
            TAX_CODE: parseIntSafe(getText('TAX_CODE')),
            CGST_PER: parseFloatSafe(getText('CGST_PER')),
            CGST_AMT: getDecimalOrZero(getText('CGST_AMT')),
            SGST_PER: parseFloatSafe(getText('SGST_PER')),
            SGST_AMT: getDecimalOrZero(getText('SGST_AMT')),
            IGST_PER: parseFloatSafe(getText('IGST_PER')),
            IGST_AMT: getDecimalOrZero(getText('IGST_AMT')),
            VAT_PER: parseFloatSafe(getText('VAT_PER')),
            VAT_AMT: getDecimalOrZero(getText('VAT_AMT')),
            CESS_PER: parseFloatSafe(getText('CESS_PER')),
            CESS_AMT: getDecimalOrZero(getText('CESS_AMT')),
            OTH_EXPS: getDecimalOrZero(getText('OTH_EXPS')),
            LD_RATE: parseFloatSafe(getText('LD_RATE')),
            NET_AMT: getDecimalOrZero(getText('NET_AMT')),
            BULK_QTY: parseFloatSafe(getText('BULK_QTY')),
            BULK_RATE: getDecimalOrZero(getText('BULK_RATE')),
            BULK_DISC_PER: parseFloatSafe(getText('BULK_DISC_PER')),
            BULK_DISC_AMT: getDecimalOrZero(getText('BULK_DISC_AMT')),
            WARRANTY: getStringOrNull(getText('WARRANTY')),
            LEADTIME_DAYS: parseIntSafe(getText('LEADTIME_DAYS')),
            PURCHASER_REMARKS: getStringOrNull(getText('PURCHASER_REMARKS')),
            PREORITY_LEVEL:  parseIntSafe(getSelect('APPROVAL_LEVEL')),
            RATE_MONTHLY: parseFloatSafe(getText('RATE_MONTHLY')),
            RATE_QUARTERLY: parseFloatSafe(getText('RATE_QUARTERLY')),
            RATE_ANNUALY: parseFloatSafe(getText('RATE_ANNUALY')),
            RATE_SPECIAL: parseFloatSafe(getText('RATE_SPECIAL')),
            REQ_TYPE: getStringOrNull(getText('REQ_TYPE')),
            REQ_NO: parseIntSafe(getText('REQ_NO')),
            STATUS: parseIntSafe(getText('STATUS')),
            APROV_CODE: 0, 
            APROV_STATUS: getStringOrNull(getText('APROV_STATUS')),
            APROV_REMARKS: getStringOrNull(getText('APROV_REMARKS')),
            FAPROV_STATUS: getStringOrNull(
                $(`#row-${rowIndex}-FAPROV_STATUS select`).val()
            ),
            FAPROV_REMARKS: getStringOrNull(
                $(`#row-${rowIndex}-FAPROV_REMARKS`).val()
            ),
            PACK_UR: getStringOrNull(getText('PACK_UR')),
            DISC_UR: getStringOrNull(getText('DISC_UR')),
            FREIGHT_UR: getStringOrNull(getText('FREIGHT_UR')),
            CGST_UR: getStringOrNull(getText('CGST_UR')),
            SGST_UR: getStringOrNull(getText('SGST_UR')),
            IGST_UR: getStringOrNull(getText('IGST_UR')),
            OTHEXP_UR: getStringOrNull(getText('OTHEXP_UR')),
            BULKDISC_UR: getStringOrNull(getText('BULKDISC_UR')),
            DOC_ID: getText('DOC_ID')
        };

        details.push(detail);
    });
    return details;
}
   
async function getAttachmentDetails() {

    return rowsAttachment.map(x => ({
        FileName: x.FileName,
        FileContentBase64: x.FileContentBase64,
        FileType: x.FileType
    }));

}

function toBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = () => resolve(reader.result.split(',')[1]);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

function GetDocid(VType) {
    $.ajax({
        url:'/QuotationRateApproval/GetMaxVNo',
        type: 'GET',
        data:{V_type:VType},
        success: function (response) {
            if (response.status === true && response.data) {
                $('#txtDocNo').val(response.data.vNo || '');
                $('#TxtDocid').val(response.data.docId || '');
            } else {
                $('#txtDocNo').val('');
                $('#TxtDocid').val('');
            }
        },
        error: function (xhr, status, error) {
            showToast("Error Fetching Doc ID: " + error, { type: "error" });
        }
    });
}

function GetVendorList(selectedValue = null) {
    $.ajax({
        url:'/QuotationRateApproval/GetVendorName',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.status) {
                const $dropdown = $('#ddlVendor');
                $dropdown.empty();
                $dropdown.append('<option selected disabled>- Select party -</option>');

                $.each(response.data, function (index, item) {
                    $dropdown.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                });

                $dropdown.select2({
                    placeholder: "- Select Party -",
                    allowClear: true
                });

                $dropdown.on('select2:open', function () {
                    setTimeout(function () {
                        let searchField = document.querySelector('.select2-container--open .select2-search__field');
                        if (searchField) {
                            searchField.focus();
                            searchField.click();
                        }
                    }, 100);
                });

                if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
                    $dropdown.val(selectedValue).trigger('change');
                } else {
                    $dropdown.prop('selectedIndex', 0);
                }
            } else {
                showToast("Vendor Load failed: ", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Vendor Load failed: " + error, { type: "error" });
        }
    });
}

function GetItemList() {

    $('#ddlItem').select2({
        placeholder: "Search Item",
        allowClear: true,
        minimumInputLength: 0,

        ajax: {
            url: '/QuotationRateApproval/GetItemName',
            dataType: 'json',
            delay: 250,
            global: false,

            data: function (params) {
                return {
                    search: params.term || '',
                    page: params.page || 1
                };
            },

            processResults: function (response, params) {

                params.page = params.page || 1;

                return {
                    results: response.data.map(x => ({
                        id: x.value,
                        text: x.text
                    })),
                    pagination: {
                        more: response.pagination.more
                    }
                };
            },

            cache: true
        }
    });

    $('#ddlItem').on('select2:open', function () {
        document.querySelector('.select2-search__field').focus();
    });
}

function parseIntSafe(value) {
    const parsed = parseInt(value, 10);
    return isNaN(parsed) ? null : parsed;
}

function parseFloatSafe(value) {
    const parsed = parseFloat(value);
    return isNaN(parsed) ? null : parsed;
}

function getStringOrNull(val) {
    const str = String(val).trim();

    return (
        str === '' ||
        str === 'undefined' ||
        str === 'null'
    ) ? null : str;
}

function getDecimalOrZero(val) {
    const num = parseFloat(val);
    return isNaN(num) ? 0.000 : num;
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

function fillItemDetailsTable(data, ApprvlStage = 1, append = false) {  
    const $tbody = $('#tblItemDetailsQR tbody');
    //$tbody.empty();
    let startIndex = 0;
    if (!append) {
        $tbody.empty();
    } else {
        startIndex = $tbody.find('tr').length;
    }

    data.forEach((item, i) => {
        if (item.ITEM_CODE) {
            selectedItemSet.add(item.ITEM_CODE.toString());
        }
            const index = startIndex + i;
          Object.keys(item).forEach(key => {
            if (item[key] === null || item[key] === undefined) {
            item[key] = '';
          }
        });

        let finalApprovalStatusTd = '';
        if (ApprvlStage == 1) {
            finalApprovalStatusTd = `
            <td id="row-${index}-FAPROV_STATUS">
                <select class="form-control">
                <option value="">-select final status-</option>
                <option value="Approved" ${item.FAPROV_STATUS === 'Approved' ? 'selected' : ''}>Approved</option>
                <option value="Rejected" ${item.FAPROV_STATUS === 'Reject' ? 'selected' : ''}>Rejected</option>
                <option value="Hold" ${item.FAPROV_STATUS === 'Hold' ? 'selected' : ''}>Hold</option>
                </select>
            </td>
            `;
        }


      const row = `
        <tr data-row-index="${index}">
        <td id="row-${index}-V_NO" style="display:none;">${item.V_NO}</td>
        <td id="row-${index}-V_TYPE" style="display:none;">${item.V_TYPE}</td>
        <td id="row-${index}-V_DATE" style="display:none;">${item.V_DATE}</td>
        <td style="display:none;" id="row-${index}-PARTY_CODE">${item.PARTY_CODE}</td>
        <td id="row-${index}-ITEM_CODE" class="freeze-item" value="${item.ITEM_CODE}">${item.ItemName}</td>
        <td id="row-${index}-MAKE_CODE" value="${item.MAKE_CODE}">${item.make}</td>
        <td id="row-${index}-UOM_CODE" value="${item.UOM_CODE}">${item.Unit}</td>
        <td id="row-${index}-UOM_CODE" value="${item.V_NO}">${item.V_NO}</td>
        <td id="row-${index}-V_NO_DUP" style="display:none;">${item.V_NO}</td>
        <td id="row-${index}-QTY">${item.QTY}</td>
        <td id="row-${index}-RATE">${item.RATE}</td>
        <td id="row-${index}-AMOUNT" style="display:none;">${item.AMOUNT}</td>
        <td id="row-${index}-PACK_PER">${item.PACK_PER}</td>
        <td id="row-${index}-PACK_AMT">${item.PACK_AMT}</td>
        <td id="row-${index}-DISC_PER">${item.DISC_PER}</td>
        <td id="row-${index}-DISC_AMT">${item.DISC_AMT}</td>
        <td id="row-${index}-FREIGHT">${item.FREIGHT}</td>
        <td id="row-${index}-TAX_CODE" style="display:none;">${item.TAX_CODE}</td>
        <td id="row-${index}-CGST_PER">${item.CGST_PER}</td>
        <td id="row-${index}-CGST_AMT" style="display:none;">${item.CGST_AMT}</td>
        <td id="row-${index}-SGST_PER">${item.SGST_PER}</td>
        <td id="row-${index}-SGST_AMT" style="display:none;">${item.SGST_AMT}</td>
        <td id="row-${index}-IGST_PER">${item.IGST_PER}</td>
        <td id="row-${index}-IGST_AMT" style="display:none;">${item.IGST_AMT}</td>
        <td id="row-${index}-VAT_PER">${item.VAT_PER}</td>
        <td id="row-${index}-VAT_AMT" style="display:none;">${item.VAT_AMT}</td>
        <td id="row-${index}-CESS_PER">${item.CESS_PER}</td>
        <td id="row-${index}-CESS_AMT" style="display:none;">${item.CESS_AMT}</td>
        <td id="row-${index}-OTH_EXPS" style="display:none;">${item.OTH_EXPS}</td>
        <td id="row-${index}-LD_RATE">${item.LD_RATE}</td>
        <td id="row-${index}-PARTY_NAME">${item.Partyname}</td>
        <td id="row-${index}-PURCHASER_REMARKS">${item.TECH_DESC}</td>
        <td id="row-${index}-APPROVAL">
            <select class="form-control" id="row-${index}-APPROVAL_LEVEL">
            <option value="">-select Priority level-</option>
            ${[1, 2, 3, 4, 5, 6].map(level => `
                <option value="${level}" ${item.PREORITY_LEVEL == level ? 'selected' : ''}>${level}</option>
            `).join('')}
            </select>
        </td>
        <td id="row-${index}-RATE_MONTHLY">${item.RATE_MONTHLY}</td>
        <td id="row-${index}-RATE_QUARTERLY">${item.RATE_QUARTERLY}</td>
        <td id="row-${index}-RATE_ANNUALY">${item.RATE_ANNUALY}</td>
        <td id="row-${index}-RATE_SPECIAL">${item.RATE_SPECIAL}</td>
        <td id="row-${index}-REQ_TYPE">${item.REQ_TYPE}</td>
        <td id="row-${index}-REQ_NO">${item.REQ_NO}</td>
        <td style="display:none;" id="row-${index}-TECH_DESC">${item.TECH_DESC}</td>
        <td style="display:none;" id="row-${index}-UOM_CODE_H">${item.UOM_CODE}</td>
        <td style="display:none;" id="row-${index}-REF_NO">${item.V_NO}</td>
        <td style="display:none;" id="row-${index}-REF_DATE">${item.V_DATE}</td>
        <td style="display:none;" id="row-${index}-REF_TYPE">${item.Ref_Type || ''}</td>
        <td style="display:none;" id="row-${index}-REF_DOCID">${item.ref_docid}</td>
        <td style="display:none;" id="row-${index}-NET_AMT">${item.NET_AMT}</td>
        <td style="display:none;" id="row-${index}-BULK_QTY">${item.BULK_QTY}</td>
        <td style="display:none;" id="row-${index}-BULK_RATE">${item.BULK_RATE}</td>
        <td style="display:none;" id="row-${index}-BULK_DISC_PER">${item.BULK_DISC_PER}</td>
        <td style="display:none;" id="row-${index}-BULK_DISC_AMT">${item.BULK_DISC_AMT}</td>
        <td style="display:none;" id="row-${index}-WARRANTY">${item.WARRANTY}</td>
        <td style="display:none;" id="row-${index}-LEADTIME_DAYS">${item.LEADTIME_DAYS}</td>
        <td style="display:none;" id="row-${index}-STATUS">${item.STATUS}</td>
        <td style="display:none;" id="row-${index}-APROV_CODE">${item.APROV_CODE}</td>
        <td style="display:none;" id="row-${index}-APROV_STATUS">${item.APROV_STATUS}</td>
        <td style="display:none;" id="row-${index}-APROV_REMARKS">${item.APROV_REMARKS}</td>
        ${finalApprovalStatusTd}
        <td>
           <input type="text" class="form-control" id="row-${index}-FAPROV_REMARKS" value="${item.FAPROV_REMARKS || ''}">
        </td>
        <td style="display:none;" id="row-${index}-PACK_UR">${item.PACK_UR}</td>
        <td style="display:none;" id="row-${index}-DISC_UR">${item.DISC_UR}</td>
        <td style="display:none;" id="row-${index}-FREIGHT_UR">${item.FREIGHT_UR}</td>
        <td style="display:none;" id="row-${index}-CGST_UR">${item.CGST_UR}</td>
        <td style="display:none;" id="row-${index}-SGST_UR">${item.SGST_UR}</td>
        <td style="display:none;" id="row-${index}-IGST_UR">${item.IGST_UR}</td>
        <td style="display:none;" id="row-${index}-OTHEXP_UR">${item.OTHEXP_UR}</td>
        <td style="display:none;" id="row-${index}-BULKDISC_UR">${item.BULKDISC_UR}</td>
        <td style="display:none;" id="row-${index}-AUTOPO_FLG">${item.AUTOPO_FLG}</td>
        <td>
            <i class="fa fa-trash btn-delete-action" id="QuotRtDetail" title="Delete Row"></i>
        </td>
        </tr>
    `;
         
      $tbody.append(row);
    });

}

function fillItemDetailsTableForFill(data, ApprvlStage = 1, append = false) {

    const $tbody = $('#tblItemDetailsQR tbody');
    let startIndex = 0;

    if (!append) {
        $tbody.empty();
    } else {
        startIndex = $tbody.find('tr').length;
    }

    data.forEach((item, i) => {
        const index = startIndex + i;

        Object.keys(item).forEach(key => {
            if (item[key] == null) {
                item[key] = '';
            }
        });

        let finalApprovalStatusTd = '';

        if (ApprvlStage == 1) {
            finalApprovalStatusTd = `
                <td id="row-${index}-FAPROV_STATUS">
                    <select class="form-control">
                        <option value="">-select final status-</option>
                        <option value="Approved">Approved</option>
                        <option value="Rejected">Rejected</option>
                        <option value="Hold">Hold</option>
                    </select>
                </td>`;
        }

        const row = `
        <tr data-row-index="${index}">

            <td style="display:none;" id="row-${index}-V_NO">${item.V_NO}</td>
            <td style="display:none;" id="row-${index}-V_DATE">${item.V_DATE}</td>
            <td style="display:none;" id="row-${index}-PARTY_CODE">${item.PARTY_CODE}</td>

            <td id="row-${index}-ITEM_CODE" class="freeze-item" value="${item.ITEM_CODE}">
                ${item.IName}
            </td>

            <td id="row-${index}-MAKE_CODE" value="${item.MAKE_CODE}">
                ${item.Make}
            </td>

            <td id="row-${index}-UOM_CODE" value="${item.UOM_CODE}">
                ${item.Unit}
            </td>

            <td>${item.V_NO}</td>

            <td id="row-${index}-QTY">${item.QTY}</td>
            <td id="row-${index}-RATE">${item.RATE}</td>
            <td id="row-${index}-PACK_PER">${item.PACK_PER}</td>
            <td id="row-${index}-PACK_AMT">${item.PACK_AMT}</td>
            <td id="row-${index}-DISC_PER">${item.DISC_PER}</td>
            <td id="row-${index}-DISC_AMT">${item.DISC_AMT}</td>
            <td id="row-${index}-FREIGHT">${item.FREIGHT}</td>
            <td id="row-${index}-CGST_PER">${item.CGST_PER}</td>
            <td id="row-${index}-SGST_PER">${item.SGST_PER}</td>
            <td id="row-${index}-IGST_PER">${item.IGST_PER}</td>
            <td id="row-${index}-VAT_PER">${item.VAT_PER}</td>
            <td id="row-${index}-CESS_PER">${item.CESS_PER}</td>
            <td id="row-${index}-LD_RATE">${item.LD_RATE}</td>
            <td id="row-${index}-PARTY_NAME">
                ${item.Vendor}
            </td>
            <td id="row-${index}-PURCHASER_REMARKS">
                ${item.TECH_DESC}
            </td>

           <td id="row-${index}-APPROVAL">
                <select class="form-control" id="row-${index}-APPROVAL_LEVEL">
                    <option value="">-select Priority level-</option>
                    <option value="1" ${item.APPROVAL_LEVEL == 1 ? 'selected' : ''}>1</option>
                    <option value="2" ${item.APPROVAL_LEVEL == 2 ? 'selected' : ''}>2</option>
                    <option value="3" ${item.APPROVAL_LEVEL == 3 ? 'selected' : ''}>3</option>
                    <option value="4" ${item.APPROVAL_LEVEL == 4 ? 'selected' : ''}>4</option>
                    <option value="5" ${item.APPROVAL_LEVEL == 5 ? 'selected' : ''}>5</option>
                    <option value="6" ${item.APPROVAL_LEVEL == 6 ? 'selected' : ''}>6</option>
                </select>
            </td>

            <td id="row-${index}-RATE_MONTHLY">${item.RATE_MONTHLY}</td>
            <td id="row-${index}-RATE_QUARTERLY">${item.RATE_QUARTERLY}</td>
            <td id="row-${index}-RATE_ANNUALY">${item.RATE_ANNUALY}</td>
            <td id="row-${index}-RATE_SPECIAL">${item.RATE_SPECIAL}</td>

            <td style="display:none;" id="row-${index}-REF_NO" value="${item.V_NO}"></td>
            <td style="display:none;" id="row-${index}-REF_DATE" value="${item.V_DATE}"></td>

            <td style="display:none;" id="row-${index}-REF_TYPE">${item.Ref_Type}</td>
            <td style="display:none;" id="row-${index}-REF_DOCID">${item.ref_docid}</td>

            <td style="display:none;" id="row-${index}-ITEM_CODE_H">${item.ITEM_CODE}</td>
            <td style="display:none;" id="row-${index}-MAKE_CODE_H">${item.MAKE_CODE}</td>
            <td style="display:none;" id="row-${index}-UOM_CODE_H">${item.UOM_CODE}</td>
            <td style="display:none;" id="row-${index}-TAX_CODE">${item.TAX_CODE}</td>
            <td id="row-${index}-REQ_TYPE">${item.REQ_TYPE}</td>
            <td id="row-${index}-REQ_NO">${item.REQ_NO}</td>
            ${finalApprovalStatusTd}
             <td>
               <input type="text" class="form-control" id="row-${index}-FAPROV_REMARKS" value="${item.FAPROV_REMARKS || ''}">
            </td>
        </tr>`;
        $tbody.append(row);
        $(`#row-${index}-APPROVAL_LEVEL`).val(item.APPROVAL_LEVEL);
    });
}

function toggleColumnByHeader(tableId, headerText, show) {
    const $table = $('#' + tableId);

    // Find column index by matching header text (trimmed)
    let colIndex = -1;
    $table.find('thead th').each(function(i) {
        if ($(this).text().trim() === headerText) {
            colIndex = i;
            return false; // break loop
        }
    });

    if (colIndex === -1) {
        console.warn(`Header "${headerText}" not found.`);
        return;
    }

    // Show/hide the header cell
    $table.find('thead th').eq(colIndex).toggle(show);

    // Show/hide each corresponding td in tbody rows
    $table.find('tbody tr').each(function() {
      $(this).find('td').eq(colIndex).toggle(show);
    });
}

function openPreview(fileType, base64) {

    const modal = new bootstrap.Modal(document.getElementById('imagePreviewModal'));

    const img = document.getElementById('previewImage');
    const pdf = document.getElementById('previewPdf');

    img.style.display = "none";
    pdf.style.display = "none";

    if (fileType?.startsWith('image')) {

        img.src = `data:${fileType};base64,${base64}`;
        img.style.display = "block";
        pdf.style.display = "none";

    } else if (fileType === "application/pdf") {

        pdf.src = `data:${fileType};base64,${base64}`;
        pdf.style.display = "block";
        img.style.display = "none";

    } else {
        showToast("Preview Not Supported: " + err, { type: "error" });
        return;
    }

    modal.show();
}

function renderAttachmentList(attachments) {

    const $list = $('#fileList');
    $list.empty();

    if (!attachments || attachments.length === 0) return;

    attachments.forEach(att => {

        const fileType = getFileType(att.filE_NAME);

        const item = $(`
            <div class="attachment-item"
                style="display:flex;gap:10px;align-items:center;margin-bottom:8px;">

                <i class="fa fa-file"></i>

                <span style="flex:1;">${att.filE_NAME}</span>

                <button type="button" class="btn btn-sm btn-primary btn-view">
                    View
                </button>

            </div>
        `);

        item.find('.btn-view').on('click', function () {
            openPreview(fileType, att.filE_BASE64);
        });

        $list.append(item);
    });
}

function normalizeBase64(base64, type) {
    if (!base64) return null;

    if (base64.startsWith("data:")) return base64;

    return `data:${type || 'application/octet-stream'};base64,${base64}`;
}

function getFileType(fileName) {

    const ext = fileName?.split('.').pop()?.toLowerCase();

    switch (ext) {
        case 'jpg':
        case 'jpeg':
            return 'image/jpeg';

        case 'png':
            return 'image/png';

        case 'gif':
            return 'image/gif';

        case 'webp':
            return 'image/webp';

        case 'pdf':
            return 'application/pdf';

        default:
            return '';
    }
}

function validateData() {

    const fromChecked = $('#chkFromDate').is(':checked');
    const toChecked = $('#chkToDate').is(':checked');

    if (!validateRequiredField('#ddlDocType', 'Doc Type')) return false;
    if (!validateRequiredField('#txtDocNo', 'Doc No')) return false;
    if (!validateRequiredField('#dtDocDate', 'Doc Date')) return false;

    // Date Validation
    if (fromChecked && toChecked) {

        const fromDate = new Date($('#dtFrom').val());
        const toDate = new Date($('#dtTo').val());

        if (toDate < fromDate) {
            showToast("To Date Should Be Greater Than Or Equal To From Date", { type: "warning" });
            $('#dtTo').focus();
            return false;
        }
    }

    // ==========================
    // Grid Validation
    // ==========================

    const $rows = $('#tblItemDetailsQR tbody tr');

    if ($rows.length === 0) {
        showToast("No Record in grid to save", { type: "error" });
        return false;
    }

    let validRowFound = false;

    for (let i = 0; i < $rows.length; i++) {

        const $row = $($rows[i]);
        const rowIndex = $row.data('row-index');

        const itemName =
            $(`#row-${rowIndex}-ITEM_CODE`).text().trim();

        const priority =
            $(`#row-${rowIndex}-APPROVAL_LEVEL`).val();

        const finalStatus =
            $(`#row-${rowIndex}-FAPROV_STATUS select`).val();

        const remarks =
            $(`#row-${rowIndex}-FAPROV_REMARKS`).val().trim();

        // At least one valid row
        validRowFound = true;

        // Final Status Required
        //if (!finalStatus) {
        //    toastr.error(`Final Status is required for item ${itemName}`);
        //    $(`#row-${rowIndex}-FAPROV_STATUS select`).focus();
        //    return false;
        //}

        //if (
        //    (finalStatus === 'Hold' ||
        //        finalStatus === 'Rejected' ||
        //        finalStatus === 'Reject')
        //    &&
        //    !remarks
        //) {
        //    toastr.error(`Reason is required for HOLD/REJECT of item ${itemName}`);
        //    return false;
        //}

        // Optional: Priority Required
        
    }

    if (!validRowFound) {
        showToast("No record in grid to save", { type: "error" });
        return false;
    }

    // ==========================
    // Attachment Validation
    // ==========================

    if (rowsAttachment.length === 0) {
        showToast("At least one Attachment is required for rate approval", { type: "warning" });
        return false;
    }

    return true;
}

//=======Report========
function PendingQCReport() {

    var reportName = "INDENT5";
    // Crystal Report Formula
    var SelForMul =
        "{Quotation2.V_TYPE}='" + $("#ddlDocType").val() + "'" +
        " AND {Quotation2.V_NO}= " + $("#txtDocNo").val() +
        " AND {Quotation2.COMP_CODE}= " + window.globalVariables.compCode +
        " AND {Quotation2.BRANCH_CODE}= " + window.globalVariables.branchCode +
        " AND {Quotation2.YEAR_CODE}= " + window.globalVariables.yearCode;
    var formulaFields = {
        Reportname: reportName,
        selectionFormula: SelForMul,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "Rate Approval Comparision"
        }
    };

    var now = new Date();
    var day = String(now.getDate()).padStart(2, '0');
    var month = String(now.getMonth() + 1).padStart(2, '0');
    var year = String(now.getFullYear()).slice(-2);
    var hours = String(now.getHours()).padStart(2, '0');
    var minutes = String(now.getMinutes()).padStart(2, '0');
    var seconds = String(now.getSeconds()).padStart(2, '0');
    var timestamp = `${day}${month}${year}_${hours}${minutes}${seconds}`;

    $.ajax({
        url: 'http://localhost:34089/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;

            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },
        error: function (xhr, status, error) {
            console.error('Error generating report:', error);
        }
    });
}

//=======Check Valid Date========
async function checkValidDate() {

    const data = {
        vdate: $("#dtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#txtDocNo").val()
    };

    try {

        const response = await fetch('/QuotationRateApproval/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();

        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;

    } catch (error) {
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

function applyOldVBPriority(data) {
    if (!data || data.length === 0) return;

    let icod = data[0].ITEM_CODE;
    let irat = parseFloat(data[0].LD_RATE || 0);

    let irow = 1;

    data[0].APPROVAL_LEVEL = 1;

    for (let i = 1; i < data.length; i++) {

        if (data[i].ITEM_CODE != null) {

            if (data[i].ITEM_CODE == icod) {

                if (parseFloat(data[i].LD_RATE || 0) > irat) {
                    irow++;
                }

                data[i].APPROVAL_LEVEL = irow;
            }
            else {

                icod = data[i].ITEM_CODE;
                irat = parseFloat(data[i].LD_RATE || 0);

                irow = 1;

                data[i].APPROVAL_LEVEL = 1;
            }
        }
    }
}

