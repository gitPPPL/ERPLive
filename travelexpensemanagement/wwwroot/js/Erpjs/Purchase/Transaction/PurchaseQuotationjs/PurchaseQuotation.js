let rowsData = [];
let rowsAttachment = [];
let rowIndex = 0;

// Master Data
let freightTermMap = {
    "Ex-Work": "Ex-Work",
    "F.O.R-at out Plant": "F.O.R-at out Plant"
};

let itemMap = {};
let itemMakeMap = {};
let uomMap = {};
let taxCodeMap = {};

// Query Params
const urlParams = new URLSearchParams(location.search);
let rowId = parseInt(urlParams.get('id'));
let vType = urlParams.get('vType');
let isReadOnly = urlParams.get('readOnly') === 'true';

let isEditMode = rowId > 0;
let isDuplicateMode = false;
let isRateAutoCalculating = false;
let isPageLoading = false;

$(document).ready(async function () {

    initializePage();

    await DocTypeDDL();
    await LoadAllDropDowns();

    [itemMap, itemMakeMap, uomMap, taxCodeMap] = await Promise.all([
        loadItemList(),
        loadItemMakeList(),
        loadUOMList(),
        loadTaxCodeList()
    ]);

    if (!isEditMode) {
        setTimeout(() => {
            $('#ddlStatus').prop('disabled', true);
            $('#ddlStatus').val('1').trigger('change');
           
        }, 100);
    }

    if (rowId > 0) {

        await loadFullQuotationByVno(
            rowId,
            vType
        );

    } else {
      
        await GetVNo();
        addNewRowBelow();
        initSelect2($(document));

    }

    registerEvents();
});

$(document).on('change', 'input[id^="rate_"]', function () {
    console.log("Rate Changed");
    const rowId = this.id.split('_')[1];

    checkLastOrderRate(rowId);
});

//==== Import Rate -> Rate Calculation ==================
$(document).on('input', 'input[id^="importRate_"]', function () {
    console.log("RATE CHANGED BY:", this.id);
    if (isPageLoading) return;
    const rowId = this.id.split('_')[1];

    const exRate = parseFloat($('#NumExRate').val()) || 0;
    const importRate = parseFloat($(this).val()) || 0;

    if (isRateAutoCalculating) return;

    if (!isEditMode || isDuplicateMode) {

        if (exRate > 0) {

            isRateAutoCalculating = true;

            const newRate = (importRate * exRate).toFixed(2);

            $(`#rate_${rowId}`).val(newRate).data('auto', true);

            isRateAutoCalculating = false;
        }
    }
});

$(document).on('input', 'input[id^="importRate_"]', function () {
    console.log("RATE CHANGED BY:", this.id);
    if (isPageLoading) return;
    const rowId = this.id.split('_')[1];

    const exRate = parseFloat($('#NumExRate').val()) || 0;
    const importRate = parseFloat($(this).val()) || 0;

    if (isRateAutoCalculating) return;

    if (!isEditMode || isDuplicateMode) {

        if (exRate > 0) {

            isRateAutoCalculating = true;

            const newRate = (importRate * exRate).toFixed(2);

            $(`#rate_${rowId}`).val(newRate).data('auto', true);

            isRateAutoCalculating = false;
        }
    }
});

//=======For sync both fields(Rate ÷ Exchange Rate = Import Rate)=========
$(document).on('input', 'input[id^="rate_"]', function () {

    if (isPageLoading) return;
    if (isRateAutoCalculating) return;

    const rowId = this.id.split('_')[1];

    const exRate = parseFloat($('#NumExRate').val()) || 0;
    const rate = parseFloat($(this).val()) || 0;

    if (exRate > 0) {

        isRateAutoCalculating = true;

        const importRate = rate / exRate;

        $(`#importRate_${rowId}`).val(importRate.toFixed(4));

        isRateAutoCalculating = false;
    }
});

//==== Auto Fill Tax Percentage On Tax Selection ========
$(document).on('change', '.ddlTaxCode', function () {

    const $row = $(this).closest('tr');
    const rowId = this.id.split('_')[1];
    const taxCode = $(this).val();

    if (!taxCode) return;

    $.get('/PurchaseQuotation/GetTaxDetails',
        { taxCode: taxCode },
        function (res) {

            if (!res.success) return;

            $row.data('packOnBasic', res.data.packOnBasic);

            $row.find(`#cgstPerc_${rowId}`).val(res.data.cgstPer);
            $row.find(`#sgstPerc_${rowId}`).val(res.data.sgstPer);
            $row.find(`#igstPerc_${rowId}`).val(res.data.igstPer);
            $row.find(`#vatPerc_${rowId}`).val(res.data.vatPer);
            $row.find(`#cessPerc_${rowId}`).val(res.data.cessPer);

            recalculateRowWithTax($row, rowId);
        });
});

//======Duplicate Item validation(on Change)=========
$(document).on('change', 'select[id^="itemName_"]', function () {

    const selectedItem = $(this).val();

    if (!selectedItem) return;

    if (isDuplicateItem(selectedItem)) {

        showToast("This Item is already added.", {
            type: "warning"
        });

        $(this).val('').trigger('change');
        return;
    }
});

//==== Recalculate Rate When Exchange Rate Changes ======
$('#NumExRate').on('input change', function () {

    const exRate = parseFloat($(this).val()) || 0;
    if (exRate <= 0) return;

    $('#tblPurchaseQuotationListByVno tbody tr').each(function () {

        const $row = $(this);
        const id = $row.find('input[id^="importRate_"]').attr('id');

        if (!id) return;

        const rowId = id.split('_')[1];

        const importRate = parseFloat($(`#importRate_${rowId}`).val()) || 0;

        const rate = importRate * exRate;

        $(`#rate_${rowId}`)
            .val(rate.toFixed(2))
            .trigger('input'); 
    });
});

//=========preview Image on edit==============
$(document).on('click', '.btn-view-attachment', function () {
    
    const src = $(this).data('src');
    const type = $(this).data('type');

    $('#previewImage').hide();
    $('#previewPdf').hide();

    if (!src || !type) {
        return;
    }

    if (type.startsWith('image/')) {

        $('#previewImage')
            .attr('src', src)
            .show();
    }
    else if (type === 'application/pdf') {

        $('#previewPdf').attr('src', src).show();
    }
    else {

        window.open(src, '_blank');
        return;
    }

    const modal = new bootstrap.Modal(
        document.getElementById('imagePreviewModal')
    );

    modal.show();
});

//======Delete  Event for Image on Edit=========
$(document).on('click', '.btn-delete-attachment', function () {

    const row = $(this).closest('.erp-file-row');

    const fileName = row.attr('data-filename');

    rowsAttachment = rowsAttachment.filter(function (item) {
        return item.ATTACHMENT !== fileName;
    });

    row.remove();
});

//============Toggle Three Dot Menu===========
$(document).on("click", ".erppage-dropdownaction-btn", function (e) {
    e.stopPropagation();

    $(".erppage-dropdownaction-menu").remove();

    const $btn = $(this);
    const offset = $btn.offset();

    const menuWidth = 150; // approx width of dropdown
    const windowWidth = $(window).width();

    let leftPos = offset.left;

    // ? If going out of screen, open to LEFT
    if (offset.left + menuWidth > windowWidth) {
        leftPos = offset.left - menuWidth + $btn.outerWidth();
    }

    const row = $btn.closest('tr');

    const itemCode = row.find('select[id^="itemName_"]').val();

    const dropdown = $(`
        <div class="erppage-dropdownaction-menu" data-itemcode="${itemCode}">
            <a href="#" id="btn-itemWise-ExportToExcel"><i class="fa fa-file-excel-o"></i> Export To Excel</a>
            <a href="#" id="btn-itemWise-PurchaseHistory"><i class="fa fa-history"></i>Purchase History</a>
            <a href="#" id="btn-itemWise-PurchaseQuotationHistory"><i class="fa fa-history"></i>Quotation History</a>
            <a href="#" id="btn-itemWise-OrderHistory"><i class="fa fa-history"></i>Qrder History</a>
        </div>
    `);
    
    $("body").append(dropdown);

    dropdown.css({
        top: offset.top + $btn.outerHeight(),
        left: leftPos
    });
});

//===Close dropdown when clicking outside========
$(document).on("click", function () {
    $(".erppage-dropdownaction-menu").remove();
});

//=========Purchase History Event=========
$(document).on('click', '#btn-itemWise-PurchaseHistory', async function (e) {

    e.preventDefault();

    const itemCode = $(this).closest('.erppage-dropdownaction-menu').data('itemcode');

    if (!itemCode) {
        showToast("Please select item first.", { type: "warning" });
        return;
    }

    const data = await loadPurchaseHistory(itemCode);

    if (!data) return;

    bindPurchaseHistory(data);

    const modal = new bootstrap.Modal(
        document.getElementById('lastTenOrderHistoryModal')
    );

    modal.show();

    $(".erppage-dropdownaction-menu").remove();
});

//=========PurchaseQuotation History Event=========
$(document).on('click', '#btn-itemWise-PurchaseQuotationHistory', async function (e) {

    e.preventDefault();

    const itemCode = $(this).closest('.erppage-dropdownaction-menu').data('itemcode');

    if (!itemCode) {
        showToast("Please select item first.", { type: "warning" });
        return;
    }

    const data = await loadPurchaseQuotationHistory(itemCode);

    if (!data) return;

    bindPurchaseQuotationHistory(data);

    const modal = new bootstrap.Modal(
        document.getElementById('lastTenPurchaseHistoryQuotation')
    );

    modal.show();

    $(".erppage-dropdownaction-menu").remove();
});

//=========PurchaseQuotation History Event=========
$(document).on('click', '#btn-itemWise-OrderHistory', async function (e) {

    e.preventDefault();

    const itemCode = $(this).closest('.erppage-dropdownaction-menu').data('itemcode');
    const Vdate = $('#dtDocDate').val();

    if (!itemCode) {
        showToast("Please select item first.", { type: "warning" });
        return;
    }

    const data = await loadOrderHistory(itemCode, Vdate);

    if (!data) return;

    bindOrderHistory(data);

    const modal = new bootstrap.Modal(
        document.getElementById('lastTenOrderHistory')
    );

    modal.show();

    $(".erppage-dropdownaction-menu").remove();
});

$(document).on('click', '#btn-itemWise-ExportToExcel', function (e) {

    e.preventDefault();

    const vNo = $('#NumDocNo').val();
    const vType = $('#ddlDocType').val();

    window.location.href =
        `/PurchaseQuotation/ExportToExcel?vNo=${vNo}&vType=${encodeURIComponent(vType)}`;
});

//=======Init Page===========
function initializePage() {
    const today = new Date().toISOString().split('T')[0];

    $("#ddlDocType").focus();
    $("#dtDocDate").val(today);
    $("#dtQuotDate").val(today);
    $("#dtValidDate").val(today);
}

//========Register Events===================
function registerEvents() {

    $('#ddlDocType').off('change').on('change', function () {
        if (isEditMode) return;
        GetVNo();
    });

    $('#ddlDocType').off('blur').on('blur', function () {
        $(this).prop('disabled', true);
    });

    const fileInput = document.getElementById('fileInput');

    fileInput?.addEventListener('change', function () {
        const file = this.files?.[0];
        if (!file) return;

        const reader = new FileReader();

        reader.onload = function (e) {
            const base64 = e.target.result.split(',')[1];

            rowsAttachment.push({
                ATTACHMENT: file.name,
                ATTACHMENT_FILE: base64
            });

            console.log("Added to rowsAttachment:", rowsAttachment);
        };

        reader.readAsDataURL(file);
    });

    $('#ddlCurrency').on('change', function () {

        const currencyText = $('#ddlCurrency option:selected').text();

        if (currencyText) {
            $('#thImportRate').text(`Import Rate (${currencyText})`);
        } else {
            $('#thImportRate').text('Import Rate');
        }
    });

    $(document).on('input', `
    [id^="qty_"],
    [id^="rate_"],
    [id^="importRate_"],
    [id^="amount_"],
    [id^="packPerc_"],
    [id^="packAmount_"],
    [id^="discountPerc_"],
    [id^="discountAmount_"],
    [id^="freight_"],
    [id^="cgstPerc_"],
    [id^="cgstAmount_"],
    [id^="sgstPerc_"],
    [id^="sgstAmount_"],
    [id^="igstPerc_"],
    [id^="igstAmount_"],
    [id^="vatPerc_"],
    [id^="vatAmount_"],
    [id^="cessPerc_"],
    [id^="cessAmount_"],
    [id^="otherExpenses_"],
    [id^="ldRate_"],
    [id^="netAmount_"]
    [id^="bulkQty_"]
    [id^="bulkDiscountAmount_$"]
`, function () {

        let value = $(this).val();

        // numeric(15,4)
        if (!/^\d{0,11}(\.\d{0,4})?$/.test(value)) {
            $(this).val(value.slice(0, -1));
        }
    });

    $(document).on('input', `
    [id^="packPerc_"],
    [id^="discountPerc_"],
    [id^="cgstPerc_"],
    [id^="sgstPerc_"],
    [id^="igstPerc_"],
    [id^="vatPerc_"],
    [id^="cessPerc_"],
    [id^="bulkDiscountPerc_"]
    [id^="rateMonthly_"]
    [id^="rateQuarterly_"]
    [id^="rateAnnually_"]
`, function () {

        let value = $(this).val();

        // numeric(7,4)
        if (!/^\d{0,3}(\.\d{0,4})?$/.test(value)) {
            $(this).val(value.slice(0, -1));
        }
    });


}

//====Generate VNo========
async function GetVNo() {
    try {
        const vType = $('#ddlDocType').val();
        if (!vType) {
            console.warn("vType is empty");
            return;
        }
        const res = await fetch(`/PurchaseQuotation/GetNextV_NO?vType=${encodeURIComponent(vType)}`);

        if (!res.ok) {
            throw new Error("Network response was not ok");
        }
        const data = await res.json();
        if (data.v_NO) {
            $('#NumDocNo').val(data.v_NO);
            const docId = vType + data.v_NO;
            console.log("DOC_ID:", docId);
        } else {
            console.warn("V_NO not found in response");
        }

    } catch (e) {
        console.error("Error in GetVNo:", e);
    }
}

//=======Doc Type=======
function DocTypeDDL(callback) {
    return $.ajax({
        url: '/PurchaseQuotation/GetDocTypeList',
        type: 'GET',
        dataType: 'json',
        success: function (res) {

            const docType = $('#ddlDocType');
            docType.empty();

            $.each(res, function (index, item) {
                docType.append(
                    `<option value="${item.value}">${item.text}</option>`
                );
            });

            //set default
            if (rowId > 0 && vType) {

                docType.val(vType);

            } else if (res.length > 0) {

                docType.val(res[0].value);
                GetVNo();
            }

            if (typeof callback === "function") callback();
        },
        error: function (xhr) {
            showToast("Error While Loading DocType: " + xhr.responseText, { type: "error" });
        }
    });
}

//======Bind Dropdown(header)=====
async function LoadAllDropDowns() {

    await Promise.all([

        bindDropdown("PurchaseQuotation", "PARTY", "#txtPartyName", "-- Select Party --", null, null, false, null, true),

        bindDropdown("PurchaseQuotation", "PAYMENTTERM", "#ddlPaymentTerm", "-- Select Payment Terms --", null, null, false, null, true),

        bindDropdown("PurchaseQuotation", "STATUS", "#ddlStatus", "-- Select Status --"),

        bindDropdown("PurchaseQuotation", "Currency", "#ddlCurrency", "-- Select Currency --")
    ]);

}

function loadItemList() {
    return $.ajax({
        url: '/PurchaseQuotation/GetItemList',
        type: 'GET',
        dataType: 'json',
    }).then(function (data) {

        return data.map(item => ({
            value: item.value,
            text: item.text
        }));

    }).catch(function (xhr, status, error) {
        showToast("Error Loading Item List: " + error, { type: "error" });
        return [];
    });
}

function loadItemMakeList() {
    return $.ajax({
        url: '/PurchaseQuotation/GetItemMakeList',
        type: 'GET',
        dataType: 'json',
    }).then(function (data) {

        return data.map(item => ({
            value: item.value,
            text: item.text
        }));

    }).catch(function (xhr, status, error) {
        showToast("Error Loading Item Make List: " + error, { type: "error" });
        return [];
    });
}

$(document).on('change', 'select[id^="itemName_"]', function () {

    const rowId = this.id.split('_')[1];
    const selectedItemCode = $(this).val();

    const $makeDropdown = $(`#itemMake_${rowId}`);
    const $uomDropdown = $(`#uom_${rowId}`);

    // reset safely (Select2 safe)
    $makeDropdown.empty().append('<option value="">-- Select Item Make --</option>').trigger('change');

    if (!selectedItemCode) return;

    $.ajax({
        url: '/PurchaseQuotation/GetMakeItemsByItemCode',
        type: 'GET',
        data: { itemCode: selectedItemCode },

        success: function (response) {

            if (response.success && response.data.length > 0) {

                response.data.forEach(item => {
                    $makeDropdown.append(
                        `<option value="${item.makeCode}">${item.makeName}</option>`
                    );
                });

                $makeDropdown.trigger('change.select2');

                const unitCode = String(response.data?.[0]?.unitCode ?? "").trim();

                if (unitCode) {
                    $uomDropdown.val(unitCode).trigger('change.select2');
                }

                // pending edit fill
                const pendingMake = window.pendingMakeLoad?.[rowId];
                if (pendingMake) {
                    setTimeout(() => {
                        $makeDropdown.val(String(pendingMake)).trigger('change.select2');
                        delete window.pendingMakeLoad[rowId];
                    }, 0);
                }
            }
        }
    });
});

function loadUOMList() {
    return $.ajax({
        url: '/PurchaseQuotation/GetUOMList',
        type: 'GET',
        dataType: 'json',
    }).then(function (data) {

        return data.map(item => ({
            value: String(item.value).trim(),
            text: item.text
        }));

    }).catch(function (xhr, status, error) {
        showToast("Error Loading UOM List: " + error, { type: "error" });
        return [];
    });
}

function loadTaxCodeList() {
    return $.ajax({
        url: '/PurchaseQuotation/GetTextCodeList',
        type: 'GET',
        dataType: 'json'
    }).then(function (data) {

        return data.map(item => ({
            value: item.value,
            text: item.text
        }));

    }).catch(function (xhr, status, error) {
        showToast("Error Loading Tax Code: " + error, { type: "error" });
        return [];
    });
}

$('#ddlItemName').on('change', function () {
    var itemCode = $(this).val();
    $.ajax({
        url: '/PurchaseQuotation/GetMakeItemByItemCode',
        type: 'GET',
        data: { itemCode: itemCode },
        success: function (data) {
            $('#txtMake').val(data.itemMake);
        },
        error: function (xhr, status, error) {
            showToast("No Make code found For Select Item : " + error, { type: "error" });
        }
    });
});

$(document).on('change', '.ddlTaxCode', function () {
    const $row = $(this).closest('tr');
    const taxCode = $(this).val();
    const rowId = $(this).attr('id').split('_')[1];

    $.ajax({
        url: '/PurchaseQuotation/GetTextRelatedDetailsTaxCode',
        type: 'GET',
        data: { taxCode: taxCode },
        success: function (response) {
            const data = response.taxDetails;

            // Set tax % values in the row
            $row.find(`#cgstPerc_${rowId}`).val(data.cgstPer);
            $row.find(`#sgstPerc_${rowId}`).val(data.sgstPer);
            $row.find(`#igstPerc_${rowId}`).val(data.igstPer);
            $row.find(`#vatPerc_${rowId}`).val(data.vatPer);
            $row.find(`#otherExpenses_${rowId}`).val(data.othPer);

            // Now recalculate all amounts with new tax percentages
            recalculateRowWithTax($row, rowId);
            updateItemTotals();

        },
        error: function (xhr, status, error) {
            showToast("Error Loading Tax Details: " + error, { type: "error" });
        }
    });
});

//=====Load on Edit========
function loadFullQuotationByVno(vNo, vType) {
    $.ajax({
        url: '/PurchaseQuotation/GetFullQuotationByVno',
        type: 'GET',
        data: { vNo, vType },
        success: function (res) {
            console.log(res);
            if (!res.success || !res.header) {
                toastr.warning("Quotation not found.");
                return;
            }
            const header = res.header;
            const items = res.items || [];
            const attachments = res.attachments || [];

            console.log("Header:", header);
            console.log("Items:", items);
            console.log("Attachments:", attachments);
            isPageLoading = true;
            $('#TxtCode').val(header.DOC_ID);
            $('#ddlDocType').val(header.v_TYPE)
            $('#NumDocNo').val(header.v_NO);
            $('#dtDocDate').val(header.v_DATE?.substring(0, 10));
            $('#NumGroupNo').val(header.grouP_NO);
            $('#ddlStatus').val(header.status).trigger('change');
            $('#txtPartyName').val(header.partY_CODE).trigger('change');

            $('#txtQuotationNo').val(header.quotE_NO);
            $('#NumContactPerson').val(header.conT_PERSON);
            $('#dtQuotDate').val(header.quotE_DATE?.substring(0, 10));
            $('#dtValidDate').val(header.valiD_DATE?.substring(0, 10));
            $('#txtRemarks').val(header.remarks);

            $('#ddlCurrency').val(header.imporT_CURRENCY).trigger('change');
            $('#NumExRate').val(header.exrate);

            $('#ddlPaymentTerm').val(header.payterM_CODE).trigger('change');
            $('#txtPaymentTerm').val(header.paymenT_TERM);

            const freightTerm = header.freighT_TERM?.trim();
            $("#ddlFreightTerm").val(freightTerm).trigger("change");
            $('#txtDeliveryTerm').val(header.deliverY_TERM);

            // Item Total Tab Fields
            $('#numQuantityIT').val(header.qty);
            $('#numAmountIT').val(header.amount);
            $('#numPackAmtIT').val(header.pacK_AMT);
            $('#numDiscAmtIT').val(header.disC_AMT);
            $('#numBulkQtyIT').val(header.bulK_QTY);
            $('#numBulkDiscAmtIT').val(header.bulK_DISCAMT);
            $('#numFreightIT').val(header.freighT_AMT);
            $('#numCGSTAmtIT').val(header.cgsT_AMT);
            $('#numSGSTAmtIT').val(header.sgsT_AMT);
            $('#numIGSTAmtIT').val(header.igsT_AMT);
            $('#numCessAmtIT').val(header.cesS_AMT);
            $('#numVATAmtIT').val(header.vaT_AMT);
            $('#numOtherAmtIT').val(header.otH_AMT);
            $('#numNetAmtIT').val(header.neT_AMT);

            if (!Array.isArray(items) || items.length === 0) {
                addNewRowBelow();
            } else {

                Promise.all([
                    loadItemList(),
                    loadItemMakeList(),
                    loadTaxCodeList(),
                    loadUOMList()
                ])
                .then(([itemList, itemMakeList, taxCodeList, uomList]) => {

                    $('#tblPurchaseQuotationListByVno tbody').empty();

                    items.forEach((row, i) => {

                        rowIndex++;

                        const html = generateRowHtml(
                            row,
                            rowIndex,
                            itemList,
                            itemMakeList,
                            taxCodeList,
                            uomList,
                            i === items.length - 1
                        );

                        $('#tblPurchaseQuotationListByVno tbody').append(html);

                        // pending fill store
                        window.pendingMakeLoad = window.pendingMakeLoad || {};
                        window.pendingUomLoad = window.pendingUomLoad || {};

                        window.pendingMakeLoad[rowIndex] = row.makE_CODE;
                        window.pendingUomLoad[rowIndex] = row.uoM_CODE;
                    });

                    // IMPORTANT: init select2 AFTER DOM append
                    initSelect2($('#tblPurchaseQuotationListByVno tbody'));

                    //New Code
                    /*$('#tblPurchaseQuotationListByVno tbody .ddlTaxCode').trigger('change');*/

                    updateItemTotals();

                    if (isReadOnly) {
                        applyReadOnlyMode();
                    }
                });

            }

            // Loading for QUOTATION1/Attachment
            //const attachBody = $('#tblAttachmentPQ tbody');
            const attachBody = $('#fileList');
            attachBody.empty();

            if (attachments.length === 0) {
                attachBody.append('<tr><td colspan="3" class="text-center text-muted">No attachments found.</td></tr>');
            } else {
                attachments.forEach((att, idx) => {
                    //const fileName = att.attachment?.split('/').pop() || `Attachment_${idx + 1}`;
                    //const base64File = att.ATTACHMENT_FILE || att.attachmenT_FILE;
                    const fileName = att.ATTACHMENT || att.attachment || `Attachment_${idx + 1}`;
                    const base64File = att.ATTACHMENT_FILE || att.attachmenT_FILE || att.attachment_FILE;
                    const mimeType = "application/octet-stream";

                    // Guess mime type
                    const extension = fileName.split('.').pop()?.toLowerCase();
                    let guessedMime = mimeType;
                    if (['png', 'jpg', 'jpeg', 'gif', 'bmp', 'webp'].includes(extension)) {
                        guessedMime = `image/${extension === 'jpg' ? 'jpeg' : extension}`;
                    } else if (extension === 'pdf') {
                        guessedMime = 'application/pdf';
                    }

                    const fullBase64 = `data:${guessedMime};base64,${base64File}`;

                    // Push to rowsAttachment array (your goal)
                    rowsAttachment.push({
                        ATTACHMENT: fileName,
                        ATTACHMENT_FILE: base64File
                    });

                    // Build preview
                    let filePreview = '';
                    let tdStyle = '';

                    if (guessedMime.startsWith('image/')) {
                       // filePreview = `<img src="${fullBase64}" alt="${fileName}" style="height: 100%; width: 100%; border-radius: 50%; object-fit: cover;" />`;
                        filePreview = `<img src="${fullBase64}" alt="${fileName}" class="erp-file-thumbnail">`;
                        tdStyle = 'style="width: 100px; height: 100px; border-radius: 50%; border: 2px solid #ccc; overflow: hidden; text-align: center; vertical-align: middle;"';
                    } else if (guessedMime === 'application/pdf') {
                        filePreview = `<a href="${fullBase64}" target="_blank"><i class="fa fa-file-pdf-o" style="font-size:40px;color:#e53935;"></i></a>`;
                    } else {
                        filePreview = `<a href="${fullBase64}" download="${fileName}">Download File</a>`;
                    }

                    const card = `
                    <div class="erp-file-row" data-filename="${fileName}">
                    
                        <div class="erp-file-preview">
                            ${filePreview}
                        </div>
                    
                        <div class="erp-file-info">
                            <div class="erp-file-name" title="${fileName}">
                                ${fileName}
                            </div>
                    
                            <div class="erp-file-type">
                                ${extension.toUpperCase()} File
                            </div>
                        </div>
                    
                        <div class="erp-file-actions">

                            <button type="button" class="erp-btn view btn-view-attachment" data-src="${fullBase64}" data-type="${guessedMime}">
                                <i class="fa fa-eye"></i>
                            </button>
                            <button type="button" class="erp-btn delete btn-delete-attachment">
                                <i class="fa fa-trash"></i>
                            </button>

                        </div>
                    
                    </div>
                    `;

                    attachBody.append(card);
                });
            }
        },
        error: function (xhr) {
            showToast("Failed To load Data: " + xhr.responseText, { type: "error" });
        }
    });
}

//=====Add New Row==============
async function addNewRowBelow(skipInit = false) {
    rowIndex++;
    const currentRowId = rowIndex;
    const newRowData = {
        hidden: '',
        itemName: '',
        itemMake: '',
        purchaserRemarks: '',
        unit: '',
        qty: '',
        rate: '',
        amount: '',
        packPerc: '',
        packAmount: '',
        discountPerc: '',
        discountAmount: '',
        freight: '',
        taxCode: '',
        cgstPerc: '',
        cgstAmount: '',
        sgstPerc: '',
        sgstAmount: '',
        igstPerc: '',
        igstAmount: '',
        vatPerc: '',
        vatAmount: '',
        cessPerc: '',
        cessAmount: '',
        otherExpenses: '',
        ldRate: '',
        netAmount: '',
        warranty: '',
        leadTime: '',
        bulkQty: '',
        bulkRate: '',
        bulkDiscountPerc: '',
        bulkDiscountAmount: '',
        rateMonthly: '',
        rateQuarterly: '',
        rateAnnually: '',
        rateSpecial: '',
        requestType: '',
        requestNo: ''
    };
    rowsData.push(newRowData);

    const createRow = () => {
        return `
                <tr class="no-border-input">
                    <td style="display:none;"><input id="hidden_${currentRowId}" name="hidden[${currentRowId}]"></td>
                    <td style="display:none;"><label  id="yearCode_${currentRowId}" readonly>@ViewBag.YearCode</label></td>
                    <td style="display:none;"><label id="compCode_${currentRowId}" readonly>@ViewBag.CompCode</label></td>
                    <td style="display:none;"><label id="branchCode_${currentRowId}" readonly>@ViewBag.BranchCode</label></td>
                    <td style="display:none;"><input id="vNo_${currentRowId}" name="vNo[${currentRowId}]" value="${rowId}" readonly></td>
                    <td style="display:none;"><select id="vType_${currentRowId}" name="vType[${currentRowId}]" class="ddlDocType"></select></td>
                    <td><select id="itemName_${currentRowId}" name="itemName[${currentRowId}]" class="form-control"></select></td>
                    <td><select id="itemMake_${currentRowId}" name="itemMake[${currentRowId}]" class="form-control"></select></td>
                    <td><input type="text" id="purchaserRemarks_${currentRowId}" name="purchaserRemarks[${currentRowId}]" class="erppagetable-control"></td>
                    <td><select id="uom_${currentRowId}" name="uom[${currentRowId}]" class="erppagetable-control"></select></td>
                    <td><input type="number" id="qty_${currentRowId}" name="qty[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="rate_${currentRowId}" name="rate[${currentRowId}]" class="erppagetable-control" value=""></td>
                    
                    <td><input type="number" id="importRate_${currentRowId}" name="importRate[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="amount_${currentRowId}" name="amount[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="packPerc_${currentRowId}" name="packPerc[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="packAmount_${currentRowId}" name="packAmount[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="discountPerc_${currentRowId}" name="discountPerc[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="discountAmount_${currentRowId}" name="discountAmount[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="freight_${currentRowId}" name="freight[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td>
                       <select id="taxCode_${currentRowId}" name="taxCode[${currentRowId}]"  class="ddlTaxCode erppagetable-control"></select>
                    </td>
                    <td><input type="number" id="cgstPerc_${currentRowId}" name="cgstPerc[${currentRowId}]" class="form-control" value="" readonly></td>
                    <td><input type="number" id="cgstAmount_${currentRowId}" name="cgstAmount[${currentRowId}]" class="erppagetable-control" value=""></td>
                    <td><input type="number" id="sgstPerc_${currentRowId}" name="sgstPerc[${currentRowId}]" value="" class="form-control" readonly></td>
                    <td><input type="number" id="sgstAmount_${currentRowId}" name="sgstAmount[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="igstPerc_${currentRowId}" name="igstPerc[${currentRowId}]" value="" class="form-control" readonly></td>
                    <td><input type="number" id="igstAmount_${currentRowId}" name="igstAmount[${currentRowId}]" value="" class="form-control" readonly></td>
                    <td><input type="number" id="vatPerc_${currentRowId}" name="vatPerc[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="vatAmount_${currentRowId}" name="vatAmount[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="cessPerc_${currentRowId}" name="cessPerc[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="cessAmount_${currentRowId}" name="cessAmount[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="otherExpenses_${currentRowId}" name="otherExpenses[${currentRowId}]" value="" class="erppagetable-control" readonly></td>
                    <td><input type="number" id="ldRate_${currentRowId}" name="ldRate[${currentRowId}]" value="" readonly class="erppagetable-control"></td>
                    <td><input type="number" id="netAmount_${currentRowId}" name="netAmount[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="text" id="warranty_${currentRowId}" name="warranty[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="leadTime_${currentRowId}" name="leadTime[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="bulkQty_${currentRowId}" name="bulkQty[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="bulkRate_${currentRowId}" name="bulkRate[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="bulkDiscountPerc_${currentRowId}" name="bulkDiscountPerc[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="bulkDiscountAmount_${currentRowId}" name="bulkDiscountAmount[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="rateMonthly_${currentRowId}" name="rateMonthly[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="rateQuarterly_${currentRowId}" name="rateQuarterly[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="rateAnnually_${currentRowId}" name="rateAnnually[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td><input type="number" id="rateSpecial_${currentRowId}" name="rateSpecial[${currentRowId}]" value="" readonly class="erppagetable-control"></td>
                    <td><input id="requestType_${currentRowId}" name="requestType[${currentRowId}]" value="" maxlength="4" readonly class="erppagetable-control"></td>
                    <td><input id="requestNo_${currentRowId}" name="requestNo[${currentRowId}]" value="" class="erppagetable-control"></td>
                    <td class="action-col">
                        <div class="action-wrap">
                        <button class="act-btn add add-row-icon" onclick="addNewRowBelow()"><i class="fa fa-plus-circle"></i></button>
                        <button class="act-btn delete" onclick="deleteRow(this)"><i class="fa fa-trash"></i></button>
                            <button type="button" class="act-btn more erppage-dropdownaction-btn"><i class="fa fa-ellipsis-v"></i></button>
                        </div>
                    </td>
            </tr>`;
    };

    $('#tblPurchaseQuotationListByVno tbody').append(createRow());

    let itemList = [], itemMakeList = [], uomList = [], taxCodeList = [];

    if (!skipInit) {
        [itemList, itemMakeList, uomList, taxCodeList] = await Promise.all([
            loadItemList(),
            loadItemMakeList(),
            loadUOMList(),
            loadTaxCodeList()
        ]);
    }

    // ITEM
    const itemName = $(`#itemName_${currentRowId}`);
    itemName.empty().append('<option value="">-- Select Item --</option>');

    itemList.forEach(item => {
        itemName.append(`<option value="${item.value}">${item.text}</option>`);
    });

    // ITEM MAKE
    const itemMake = $(`#itemMake_${currentRowId}`);
    itemMake.empty().append('<option value="">-- Select Item Make --</option>');

    itemMakeList.forEach(item => {
        itemMake.append(`<option value="${item.value}">${item.text}</option>`);
    });
    // UOM
    const uom = $(`#uom_${currentRowId}`);
    uom.empty().append('<option value="">-- Select UOM --</option>');

    uomList.forEach(item => {
        uom.append(`<option value="${item.value}">${item.text}</option>`);
    });

    // TAX CODE
    const taxCode = $(`#taxCode_${currentRowId}`);
    taxCode.empty().append('<option value="">-- Select Tax Code --</option>');

    taxCodeList.forEach(item => {
        taxCode.append(`<option value="${item.value}">${item.text}</option>`);
    });

    $('#tblPurchaseQuotationListByVno tbody tr').each(function (index, tr) {
        $(tr).find('.add-row-icon').toggle(index === $('#tblPurchaseQuotationListByVno tbody tr').length - 1);
    });

    const $newRow = $('#tblPurchaseQuotationListByVno tbody tr:last');

    // init select2 only for new row (fast)
    initSelect2($newRow);
    return currentRowId;
}

function generateRowHtml(row, i, itemMap, itemMakeMap, taxCodeMap, uomMap, isLastRow) {

    // Helper function to return '0' if value is falsy (null, undefined, '', etc.)
    const getValue = value => (value === undefined || value === null || value === '') ? 0 : value;
    const itemCode = String(row.iteM_CODE ?? '');
    const makeCode = String(row.makE_CODE ?? '');
    const uomCode = String(row.uoM_CODE ?? '');
    const taxCode = String(row.taX_CODE ?? '');

    return `
             <tr>
                 <td style="display:none;">
                     <input id="hidden_${i}" name="hidden[${i}]" value="${row.hidden || ''}">
                 </td>
                     <td style="display:none;"><label id="yearCode_${rowIndex}" readonly>@ViewBag.YearCode</label></td>
                     <td style="display:none;"><label id="compCode_${rowIndex}" readonly>@ViewBag.CompCode</label></td>
                     <td style="display:none;"><label id="branchCode_${rowIndex}" readonly>@ViewBag.BranchCode</label></td>
                     <td style="display:none;"><input id="vNo_${rowIndex}" name="vNo[${rowIndex}]" value="${rowId}" readonly></td>
                     <td style="display:none;"><select id="vType_${rowIndex}" name="vType[${rowIndex}]"  ddlDocType"></select></td>

                 <td>
                     <select id="itemName_${i}" name="itemName[${i}]" class="form-control">
                       ${generateDropdownOptions(itemMap, itemCode)}
                    </select>
                 </td>
                 <td>
                     <select id="itemMake_${i}" name="itemMake[${i}]" class="form-control">
                        ${generateDropdownOptions(itemMakeMap, makeCode)}
                    </select>
                 </td>
                 <td><input type="text" id="purchaserRemarks_${i}" name="purchaserRemarks[${i}]" value="${row.purchaseR_REMARKS || ''}" class="erppagetable-control"></td>
                  <td>
                      <select id="uom_${i}" name="uom[${i}]" class="erppagetable-control">
                       ${generateDropdownOptions(uomMap, uomCode)}
                    </select>
                </td>
                 <td><input type="number" id="qty_${i}" name="qty[${i}]" class="erppagetable-control" value="${getValue(row.qty)}"></td>
                 <td><input type="number" id="rate_${i}" name="rate[${i}]" class="erppagetable-control" value="${getValue(row.rate)}"></td>
                 <td><input type="number" id="importRate_${i}" name="importRate[${i}]" class="erppagetable-control" value="${getValue(row.imporT_RATE)}"></td>
                 <td><input type="number" id="amount_${i}" name="amount[${i}]" class="erppagetable-control" value="${getValue(row.amount)}"></td>
                 <td><input type="number" id="packPerc_${i}" name="packPerc[${i}]" value="${getValue(row.pacK_PER)}" class="erppagetable-control"></td>
                 <td><input type="number" id="packAmount_${i}" name="packAmount[${i}]" value="${getValue(row.pacK_AMT)}" class="erppagetable-control"></td>
                 <td><input type="number" id="discountPerc_${i}" name="discountPerc[${i}]" value="${getValue(row.disC_PER)}" class="erppagetable-control"></td>
                 <td><input type="number" id="discountAmount_${i}" name="discountAmount[${i}]" value="${getValue(row.disC_AMT)}" class="erppagetable-control"></td>
                 <td><input type="number" id="freight_${i}" name="freight[${i}]" value="${getValue(row.freight)}" class="erppagetable-control"></td>
                 <td>
                    <select id="taxCode_${i}" name="taxCode[${i}]" class="ddlTaxCode erppagetable-control">
                        ${generateDropdownOptions(taxCodeMap, taxCode)}
                    </select>
                 </td>
                 <td><input type="number" id="cgstPerc_${i}" name="cgstPerc[${i}]" value="${getValue(row.cgsT_PER)}" readonly class="form-control"></td>
                 <td><input type="number" id="cgstAmount_${i}" name="cgstAmount[${i}]"  value="${getValue(row.cgsT_AMT)}" class="erppagetable-control"></td>
                 <td><input type="number" id="sgstPerc_${i}" name="sgstPerc[${i}]"  value="${getValue(row.sgsT_PER)}" readonly class="form-control"></td>
                 <td><input type="number" id="sgstAmount_${i}" name="sgstAmount[${i}]" value="${getValue(row.sgsT_AMT)}" class="erppagetable-control"></td>
                 <td><input type="number" id="igstPerc_${i}" name="igstPerc[${i}]" value="${getValue(row.igsT_PER)}" readonly class="form-control"></td>
                 <td><input type="number" id="igstAmount_${i}" name="igstAmount[${i}]" value="${getValue(row.igsT_AMT)}" readonly class="form-control"></td>
                 <td><input type="number" id="vatPerc_${i}" name="vatPerc[${i}]" value="${getValue(row.vaT_PER)}" class="erppagetable-control"></td>
                 <td><input type="number" id="vatAmount_${i}" name="vatAmount[${i}]" value="${getValue(row.vaT_AMT)}" class="erppagetable-control"></td>
                 <td><input type="number" id="cessPerc_${i}" name="cessPerc[${i}]" value="${getValue(row.cesS_PER)}" class="erppagetable-control"></td>
                 <td><input type="number" id="cessAmount_${i}" name="cessAmount[${i}]" value="${getValue(row.cesS_AMT)}" class="erppagetable-control"></td>
                 <td><input type="number" id="otherExpenses_${i}" name="otherExpenses[${i}]" value="${getValue(row.otH_EXPS)}" class="erppagetable-control"></td>
                 <td><input type="number" id="ldRate_${i}" name="ldRate[${i}]" value="${getValue(row.lD_RATE)}" readonly class="erppagetable-control"></td>
                 <td><input type="number" id="netAmount_${i}" name="netAmount[${i}]" value="${getValue(row.neT_AMT)}" class="erppagetable-control"></td>
                 <td><input type="text" id="warranty_${i}" name="warranty[${i}]" value="${row.warranty || ''}" class="erppagetable-control"></td>
                 <td><input type="number" id="leadTime_${i}" name="leadTime[${i}]" value="${getValue(row.leadtimE_DAYS)}" class="erppagetable-control"></td>
                 <td><input type="number" id="bulkQty_${i}" name="bulkQty[${i}]" value="${getValue(row.bulK_QTY)}" class="erppagetable-control"></td>
                 <td><input type="number" id="bulkRate_${i}" name="bulkRate[${i}]" value="${getValue(row.bulK_RATE)}" class="erppagetable-control"></td>
                 <td><input type="number" id="bulkDiscountPerc_${i}" name="bulkDiscountPerc[${i}]" value="${getValue(row.bulK_DISC_PER)}" class="erppagetable-control"></td>
                 <td><input type="number" id="bulkDiscountAmount_${i}" name="bulkDiscountAmount[${i}]" value="${getValue(row.bulK_DISC_AMT)}" class="erppagetable-control"></td>
                 <td><input type="number" id="rateMonthly_${i}" name="rateMonthly[${i}]" value="${getValue(row.ratE_MONTHLY)}" class="erppagetable-control"></td>
                 <td><input type="number" id="rateQuarterly_${i}" name="rateQuarterly[${i}]" value="${getValue(row.ratE_QUARTERLY)}" class="erppagetable-control"></td>
                 <td><input type="number" id="rateAnnually_${i}" name="rateAnnually[${i}]" value="${getValue(row.ratE_ANNUALY)}" class="erppagetable-control"></td>
                 <td><input type="number" id="rateSpecial_${i}" name="rateSpecial[${i}]" value="${getValue(row.ratE_SPECIAL)}" readonly class="erppagetable-control"></td>
                 <td><input id="requestType_${i}" name="requestType[${i}]" value="${row.reQ_TYPE || ''}" maxlength="4" readonly class="erppagetable-control"></td>
                 <td><input id="requestNo_${i}" name="requestNo[${i}]" value="${row.reQ_NO || ''}" class="erppagetable-control"></td>
                 <td class="action-col">
                    <div class="action-wrap">
                 ${isLastRow ? `
                        <button type="button" class="act-btn add add-row-icon" onclick="addNewRowBelow()">
                            <i class="fa fa-plus-circle"></i>
                        </button>
                    ` : ''}

                    <button type="button" class="act-btn delete" onclick="deleteRow(this)">
                        <i class="fa fa-trash"></i>
                    </button>
                          <button type="button" class="act-btn more erppage-dropdownaction-btn"><i class="fa fa-ellipsis-v"></i></button>
                    </div>
                </td>
             </tr>
         `;
}

function generateDropdownOptions(list, selectedValue) {

    const selectedStr = String(selectedValue ?? '');

    return list.map(item => {
        const value = String(item.value ?? '');
        const text = item.text ?? '';

        const selected = value === selectedStr ? 'selected' : '';

        return `<option value="${value}" ${selected}>${text}</option>`;
    }).join('');
}

$('#btnAddAttachment').on('click', function () {
    const fileName = $('#txtFileName').val().trim();
    const fileInput = $('#fileUpload')[0].files[0];

    if (!fileName || !fileInput) {
        showToast("Pkease Provide Both file name and file", { type: "error" });
        return;
    }

    const reader = new FileReader();
    reader.onload = function (e) {
        let fullBase64 = e.target.result;
        let base64File = fullBase64.split(',')[1];

        const attachment = {
            ATTACHMENT: fileName,
            ATTACHMENT_FILE: base64File
        };
        rowsAttachment.push(attachment);

        const isImage = fileInput.type.startsWith('image/');
        let previewHtml = '';
        let tdStyle = '';

        if (isImage) {
            tdStyle = 'style="width: 100px; height: 100px; border-radius: 50%; border: 2px solid #ccc; overflow: hidden; text-align: center; vertical-align: middle;"';
            previewHtml = `<img src="${fullBase64}" alt="${fileName}" style="height: 100%; width: 100%; border-radius: 50%; object-fit: cover;" />`;
        } else {
            previewHtml = `<a href="${fullBase64}" target="_blank">Preview</a>`;
        }

        //const row = `
        //        <tr data-filename="${fileName}" style="height: 100px;">
        //            <td style="vertical-align: middle;">${fileName}</td>
        //            <td ${tdStyle}>${previewHtml}</td>
        //            <td style="vertical-align: middle;">
        //                <i class="fa fa-trash text-danger cursor-pointer btn-delete-attachment"></i>
        //            </td>
        //        </tr>`;

        const extension = fileName.split('.').pop()?.toLowerCase();
        const card = `
        <div class="erp-file-row" data-filename="${fileName}">

            <div class="erp-file-preview">
                ${previewHtml}
            </div>

            <div class="erp-file-info">
                <div class="erp-file-name">${fileName}</div>
                <div class="erp-file-type">${extension.toUpperCase()} File</div>
            </div>

            <div class="erp-file-actions">

                <button type="button"
                        class="erp-btn view btn-view-attachment"
                        data-src="${fullBase64}"
                        data-type="${fileInput.type}">
                    <i class="fa fa-eye"></i>
                </button>

                <button type="button"
                        class="erp-btn delete btn-delete-attachment">
                    <i class="fa fa-trash"></i>
                </button>

            </div>

        </div>
        `;
        
        /* $('#tblAttachmentPQ tbody').append(row);*/
        $('#fileList').append(card);

        $('#txtFileName').val('');
        $('#fileUpload').val('');
    };

    reader.readAsDataURL(fileInput);
});

$('#tblAttachmentPQ').on('click', '.btn-delete-attachment', function () {
    const row = $(this).closest('tr');
    const fileName = row.data('filename');

    // Remove from array
    rowsAttachment = rowsAttachment.filter(item => item.ATTACHMENT !== fileName);

    row.remove();
});

//===========Save And Update==========
$('#btn-save').click(async function (e) {
    e.preventDefault();

    const isValidDate = await checkValidDate();
    if (!isValidDate) return;

    if (!validateHeader()) {
        return;
    }

    let rowsData = [];

    // 1. Gather data for Quotation1
    const headerData = {
        V_TYPE: $('#ddlDocType').val(),
        V_NO: parseInt($('#NumDocNo').val()) || null,
        V_DATE: $('#dtDocDate').val(),
        GROUP_NO: parseInt($('#NumGroupNo').val()) || null,
        STATUS: parseInt($('#ddlStatus').val()) || null,
        PARTY_CODE: parseInt($('#txtPartyName').val()) || null,
        QUOTE_NO: $('#txtQuotationNo').val(),
        CONT_PERSON: $('#NumContactPerson').val(),
        QUOTE_DATE: $('#dtQuotDate').val(),
        VALID_DATE: $('#dtValidDate').val(),
        REMARKS: $('#txtRemarks').val(),
        PAYTERM_CODE: parseInt($('#ddlPaymentTerm').val()) || null,
        PAYMENT_TERM: $('#txtPaymentTerm').val(),
        FREIGHT_TERM: $('#ddlFreightTerm').val(),
        DELIVERY_TERM: $('#txtDeliveryTerm').val(),
        IMPORT_CURRENCY: $('#ddlCurrency').val(),
        EXRATE: parseFloat($('#NumExRate').val()) || 0,

        AED: isDuplicateMode ? "D" : (isEditMode ? "E" : "A"),

        // Item Total Values
        QTY: parseFloat($('#numQuantityIT').val()) || 0,
        AMOUNT: parseFloat($('#numAmountIT').val()) || 0,
        PACK_AMT: parseFloat($('#numPackAmtIT').val()) || 0,
        DISC_AMT: parseFloat($('#numDiscAmtIT').val()) || 0,
        BULK_QTY: parseFloat($('#numBulkQtyIT').val()) || 0,
        BULK_DISCAMT: parseFloat($('#numBulkDiscAmtIT').val()) || 0,
        FREIGHT_AMT: parseFloat($('#numFreightIT').val()) || 0,
        CGST_AMT: parseFloat($('#numCGSTAmtIT').val()) || 0,
        SGST_AMT: parseFloat($('#numSGSTAmtIT').val()) || 0,
        IGST_AMT: parseFloat($('#numIGSTAmtIT').val()) || 0,
        CESS_AMT: parseFloat($('#numCessAmtIT').val()) || 0,
        VAT_AMT: parseFloat($('#numVATAmtIT').val()) || 0,
        OTH_AMT: parseFloat($('#numOtherAmtIT').val()) || 0,
        NET_AMT: parseFloat($('#numNetAmtIT').val()) || 0
    };

    // 2. Gather data for Quotation2
    $('#tblPurchaseQuotationListByVno tbody tr').each(function () {
        const row = $(this);
        rowsData.push({
            HIDDEN: row.find('input[id^="hidden_"]').val(),
            YEAR_CODE: parseInt(row.find('label[id^="yearCode_"]').text()) || null,
            COMP_CODE: parseInt(row.find('label[id^="compCode_"]').text()) || null,
            BRANCH_CODE: parseInt(row.find('label[id^="branchCode_"]').text()) || null,

            V_NO: headerData.V_NO,
            V_TYPE: headerData.V_TYPE,
            V_DATE: headerData.V_DATE,

            ITEM_CODE: parseInt(row.find('select[id^="itemName_"]').val()) || null,
            MAKE_CODE: parseInt(row.find('select[id^="itemMake_"]').val()) || null,
            PURCHASER_REMARKS: row.find('input[id^="purchaserRemarks_"]').val(),

            UOM_CODE: parseInt(row.find('select[id^="uom_"]').val()) || null,
            QTY: parseFloat(row.find('input[id^="qty_"]').val()) || null,

            IMPORT_RATE: parseFloat(row.find('input[id^="importRate_"]').val()) || 0,
            RATE: parseInt(row.find('input[id^="rate_"]').val()) || 0,
            AMOUNT: parseFloat(row.find('input[id^="amount_"]').val()) || 0,
            PACK_PER: parseFloat(row.find('input[id^="packPerc_"]').val()) || 0,

            PACK_AMT: parseFloat(row.find('input[id^="packAmount_"]').val()) || 0,
            DISC_PER: parseFloat(row.find('input[id^="discountPerc_"]').val()) || 0,
            DISC_AMT: parseFloat(row.find('input[id^="discountAmount_"]').val()) || 0,

            FREIGHT: parseFloat(row.find('input[id^="freight_"]').val()) || 0,
            TAX_CODE: parseInt(row.find('select[id^="taxCode_"]').val()) || null,
            CGST_PER: parseFloat(row.find('input[id^="cgstPerc_"]').val()) || 0,

            CGST_AMT: parseFloat(row.find('input[id^="cgstAmount_"]').val()) || 0,
            SGST_PER: parseFloat(row.find('input[id^="sgstPerc_"]').val()) || 0,
            SGST_AMT: parseFloat(row.find('input[id^="sgstAmount_"]').val()) || 0,

            IGST_PER: parseFloat(row.find('input[id^="igstPerc_"]').val()) || 0,
            IGST_AMT: parseFloat(row.find('input[id^="igstAmount_"]').val()) || 0,
            VAT_PER: parseFloat(row.find('input[id^="vatPerc_"]').val()) || 0,

            VAT_AMT: parseFloat(row.find('input[id^="vatAmount_"]').val()) || 0,
            CESS_PER: parseFloat(row.find('input[id^="cessPerc_"]').val()) || 0,
            CESS_AMT: parseFloat(row.find('input[id^="cessAmount_"]').val()) || 0,

            OTH_EXPS: parseFloat(row.find('input[id^="otherExpenses_"]').val()) || 0,
            LD_RATE: parseFloat(row.find('input[id^="ldRate_"]').val()) || 0,
            NET_AMT: parseFloat(row.find('input[id^="netAmount_"]').val()) || 0,

            WARRANTY: row.find('input[id^="warranty_"]').val(),
            LEADTIME_DAYS: parseInt(row.find('input[id^="leadTime_"]').val()) || null,
            BULK_QTY: parseFloat(row.find('input[id^="bulkQty_"]').val()) || 0,
            BULK_RATE: parseFloat(row.find('input[id^="bulkRate_"]').val()) || 0,

            BULK_DISC_PER: parseFloat(row.find('input[id^="bulkDiscountPerc_"]').val()) || 0,
            BULK_DISC_AMT: parseFloat(row.find('input[id^="bulkDiscountAmount_"]').val()) || 0,
            PREORITY_LEVEL: parseInt(row.find('input[id^="priorityLevel_"]').val()) || null,

            RATE_MONTHLY: parseFloat(row.find('input[id^="rateMonthly_"]').val()) || 0,
            RATE_QUARTERLY: parseFloat(row.find('input[id^="rateQuarterly_"]').val()) || 0,
            RATE_ANNUALY: parseFloat(row.find('input[id^="rateAnnually_"]').val()) || 0,
            RATE_SPECIAL: parseFloat(row.find('input[id^="rateSpecial_"]').val()) || 0,
            REQ_TYPE: row.find('input[id^="requestType_"]').val(),
            REQ_NO: parseInt(row.find('input[id^="requestNo_"]').val()) || null
        });
    });

    if (!validateGrid(rowsData, rowsAttachment)) {
        return;
    }

    const data = {
        header: headerData,
        lineRows: rowsData,
        Attachement: rowsAttachment
    };

    console.log("header data", headerData);
    console.log("line rows", rowsData);
     
    $.ajax({
        url: '/PurchaseQuotation/SaveQuotation',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (response) {
            if (response.success === true) {
                console.log("SAVE RESPONSE:", response);
                if (response.action === "INSERT") {
                    showToast("Data Saved Successfully", { type: "success" });
                }
                else if (response.action === "UPDATE") {
                    showToast("Data Update Successfully", { type: "success" });
                }
                isReadOnly = true;
                applyReadOnlyMode();
                
            } else {
                showToast(response.message, { type: "warning"});
            }
        },
        error: function (xhr, status, error) {
            showToast("Error While Saving Data: " + error, { type: "error" })
            
        }
    });
});

function deleteRow(iconElement) {
    // 1. Find the row
    const $row = $(iconElement).closest('tr');

    const idMatch = $row.find('input, select').first().attr('id').match(/_(\d+)$/);
    if (!idMatch) return;

    const rowIdx = parseInt(idMatch[1]);

    $row.remove();

    rowsData.splice(rowIdx, 1);

    $('#tblPurchaseQuotationListByVno tbody tr').each(function (i, tr) {
        $(tr).find('input, select').each(function () {
            const id = $(this).attr('id');
            const name = $(this).attr('name');
            if (id) {
                const baseId = id.substring(0, id.lastIndexOf('_'));
                $(this).attr('id', `${baseId}_${i}`);
            }
            if (name) {
                const baseName = name.substring(0, name.indexOf('['));
                $(this).attr('name', `${baseName}[${i}]`);
            }
        });
    });

    rowsData = rowsData.map((row, i) => ({ ...row }));

    $('#tblPurchaseQuotationListByVno tbody tr').each(function (index, tr) {
        $(tr).find('.add-row-icon').toggle(index === $('#tblPurchaseQuotationListByVno tbody tr').length - 1);
    });
}

//======Header Validation function===========
function validateHeader() {
    console.log("HEader Validation called")
    const vDate = $('#dtDocDate').val();
    const quoteDate = $('#dtQuotDate').val();
    const validDate = $('#dtValidDate').val();

    console.log("Quote:", quoteDate, new Date(quoteDate));
    console.log("Valid:", validDate, new Date(validDate));

    if (!validateRequiredField('#NumDocNo', 'Doc No')) return;
    if (!validateRequiredField('#dtDocDate', 'Doc Date')) return;
    if (!validateRequiredField('#txtPartyName', 'Party Name')) return;
    if (!validateRequiredField('#ddlStatus', 'Status')) return;
    if (!validateRequiredField('#ddlPaymentTerm', 'Payment Term')) return;
    if (!validateRequiredField('#ddlFreightTerm', 'Freight Term')) return;

    if (quoteDate && vDate && new Date(quoteDate) < new Date(vDate)) {
        showToast("Quotation Date must be greater than voucher date.", { type: "warning" });
        return;
    }

    // Valid Date >= Quotation Date
    if (validDate && quoteDate && new Date(validDate) < new Date(quoteDate)) {
        showToast("Valid date must be greater than or equal to quotation date.", { type: "warning" });
        return;
    }
    return true;
}

//========Validate Grid==========
function validateGrid(rowsData, rowsAttachment) {

    console.log("Validation Called !!:");

    if (!rowsData || rowsData.length === 0) {
        showToast("No Record in grid to save.", { type: "warning" });
        return false;
    }

    let validItemCount = 0;

    for (let i = 0; i < rowsData.length; i++) {

        const row = rowsData[i];

        if ((row.ITEM_CODE || 0) > 0) {
            validItemCount++;
        }

        if ((row.ITEM_CODE || 0) <= 0) {
            showToast(`Row ${i + 1}: Item Name is Required`, { type: "warning" });
            return false;
        }

        if ((row.MAKE_CODE || 0) <= 0) {
            showToast(`Row ${i + 1}: Make is Required.`, { type: "warning" });
            return false;
        }

        // Company 1 validation
        if ($("#hdnCompCode").val() == "1" &&
            (row.REQ_NO || 0) <= 0) {

            showToast(`Row ${i + 1}: Request not found for selected item.`, {
                type: "warning"
            });

            return false;
        }

        if ((row.AMOUNT || 0) > 0 &&(row.TAX_CODE || 0) <= 0) {

            showToast(`Row ${i + 1}: Tax Type not selected.`, {
                type: "warning"
            });

            return false;
        }
    }

    if (validItemCount === 0) {
        showToast("No Record in grid to save.", { type: "warning" });
        return false;
    }

    // Company 2 ko attachment mandatory nahi
    if ($("#hdnCompCode").val() != "2") {

        if (!rowsAttachment || rowsAttachment.length === 0) {

            showToast("At least one Quotation attachment required.", {
                type: "warning"
            });

            return false;
        }
    }

    return true;
}

$(document).on(
    'input change',
    `
    input[id^="qty_"],
    input[id^="rate_"],
    input[id^="packPerc_"],
    input[id^="discountPerc_"],
    input[id^="freight_"],
    input[id^="otherExpenses_"],
    input[id^="cgstPerc_"],
    input[id^="sgstPerc_"],
    input[id^="igstPerc_"],
    input[id^="vatPerc_"],
    input[id^="cessPerc_"]
    `,
    function () {


        const $row = $(this).closest('tr');
        const rowId = this.id.split('_')[1];

        const rate = $row.find(`#rate_${rowId}`).val();
        const qty = $row.find(`#qty_${rowId}`).val();
        const packPerc = $row.find(`#packPerc_${rowId}`).val();
        const discPerc = $row.find(`#discountPerc_${rowId}`).val();

        const freight = parseFloat($row.find(`#freight_${rowId}`).val()) || 0;
        const otherExpenses = parseFloat($row.find(`#otherExpenses_${rowId}`).val()) || 0;

        const cgstPer = parseFloat($row.find(`#cgstPerc_${rowId}`).val()) || 0;
        const sgstPer = parseFloat($row.find(`#sgstPerc_${rowId}`).val()) || 0;
        const igstPer = parseFloat($row.find(`#igstPerc_${rowId}`).val()) || 0;
        const vatPer = parseFloat($row.find(`#vatPerc_${rowId}`).val()) || 0;
        const cessPer = parseFloat($row.find(`#cessPerc_${rowId}`).val()) || 0;

        const result = calculateAmounts(
            rate,
            qty,
            packPerc,
            discPerc,
            freight,
            otherExpenses,
            cgstPer,
            sgstPer,
            igstPer,
            vatPer,
            cessPer
        );

        $row.find(`#amount_${rowId}`).val(result.amount);
        $row.find(`#packAmount_${rowId}`).val(result.packAmt);
        $row.find(`#discountAmount_${rowId}`).val(result.discAmt);

        $row.find(`#cgstAmount_${rowId}`).val(result.cgstAmt);
        $row.find(`#sgstAmount_${rowId}`).val(result.sgstAmt);
        $row.find(`#igstAmount_${rowId}`).val(result.igstAmt);
        $row.find(`#vatAmount_${rowId}`).val(result.vatAmt);
        $row.find(`#cessAmount_${rowId}`).val(result.cessAmt);

        $row.find(`#netAmount_${rowId}`).val(result.netAmount);
        $row.find(`#ldRate_${rowId}`).val(result.ldRate);

        updateItemTotals();
    }
);

$(document).on(
    'input change',
    `
    input[id^="bulkQty_"],
    input[id^="bulkDiscountAmount_"]
    `,
    function () {

        const rowId = this.id.split('_')[1];

        // Sirf footer totals update karo
        updateItemTotals();
    }
);

function updateItemTotals() {
    let totalQty = 0, totalAmount = 0, totalPackAmt = 0, totalDiscAmt = 0, totalBulkQty = 0, totalBulkDiscAmt = 0;
    let totalFreight = 0, totalCGST = 0, totalSGST = 0, totalIGST = 0, totalCess = 0, totalVAT = 0;
    let totalOtherAmt = 0, totalNetAmt = 0;

    $('#tblPurchaseQuotationListByVno tbody tr').each(function () {
        const $row = $(this);
        const rowId = $row.find('input[id^="qty_"], input[id^="rate_"]').first().attr('id')?.split('_')[1];
        if (!rowId) return;

        totalQty += parseFloat($row.find(`#qty_${rowId}`).val()) || 0;
        totalAmount += parseFloat($row.find(`#amount_${rowId}`).val()) || 0;
        totalPackAmt += parseFloat($row.find(`#packAmount_${rowId}`).val()) || 0;
        totalDiscAmt += parseFloat($row.find(`#discountAmount_${rowId}`).val()) || 0;
        totalBulkQty += parseFloat($row.find(`#bulkQty_${rowId}`).val()) || 0;
        totalBulkDiscAmt += parseFloat($row.find(`#bulkDiscountAmount_${rowId}`).val()) || 0;

        totalFreight += parseFloat($row.find(`#freight_${rowId}`).val()) || 0;
        totalCGST += parseFloat($row.find(`#cgstAmount_${rowId}`).val()) || 0;
        totalSGST += parseFloat($row.find(`#sgstAmount_${rowId}`).val()) || 0;
        totalIGST += parseFloat($row.find(`#igstAmount_${rowId}`).val()) || 0;
        totalCess += parseFloat($row.find(`#cessAmount_${rowId}`).val()) || 0;
        totalVAT += parseFloat($row.find(`#vatAmount_${rowId}`).val()) || 0;
        
        totalOtherAmt += parseFloat($row.find(`#otherExpenses_${rowId}`).val()) || 0;
        totalNetAmt += parseFloat($row.find(`#netAmount_${rowId}`).val()) || 0;

    });

    // Update the total fields
    $('#numQuantityIT').val(totalQty.toFixed(2));
    $('#numAmountIT').val(totalAmount.toFixed(2));
    $('#numPackAmtIT').val(totalPackAmt.toFixed(2));
    $('#numDiscAmtIT').val(totalDiscAmt.toFixed(2));
    $('#numBulkQtyIT').val(totalBulkQty.toFixed(2));
    $('#numBulkDiscAmtIT').val(totalBulkDiscAmt.toFixed(2));

    $('#numFreightIT').val(totalFreight.toFixed(2));
    $('#numCGSTAmtIT').val(totalCGST.toFixed(2));
    $('#numSGSTAmtIT').val(totalSGST.toFixed(2));
    $('#numIGSTAmtIT').val(totalIGST.toFixed(2));
    $('#numCessAmtIT').val(totalCess.toFixed(2));
    $('#numVATAmtIT').val(totalVAT.toFixed(2));

    $('#numOtherAmtIT').val(totalOtherAmt.toFixed(2));
    $('#numNetAmtIT').val(totalNetAmt.toFixed(2));
}

function recalculateRowWithTax($row, rowId) {

    const rate = $row.find(`#rate_${rowId}`).val();
    const qty = $row.find(`#qty_${rowId}`).val();
    const packPerc = $row.find(`#packPerc_${rowId}`).val();
    const discPerc = $row.find(`#discountPerc_${rowId}`).val();

    const freight =
        parseFloat($row.find(`#freight_${rowId}`).val()) || 0;

    const otherExpenses =
        parseFloat($row.find(`#otherExpenses_${rowId}`).val()) || 0;

    const cgstPer =
        parseFloat($row.find(`#cgstPerc_${rowId}`).val()) || 0;

    const sgstPer =
        parseFloat($row.find(`#sgstPerc_${rowId}`).val()) || 0;

    const igstPer =
        parseFloat($row.find(`#igstPerc_${rowId}`).val()) || 0;

    const vatPer =
        parseFloat($row.find(`#vatPerc_${rowId}`).val()) || 0;

    const cessPer =
        parseFloat($row.find(`#cessPerc_${rowId}`).val()) || 0;

    const packOnBasic =
        parseInt($row.data('packOnBasic')) || 0;

    const result = calculateAmounts(
        rate,
        qty,
        packPerc,
        discPerc,
        freight,
        otherExpenses,
        cgstPer,
        sgstPer,
        igstPer,
        vatPer,
        cessPer,
        packOnBasic
    );

    $row.find(`#amount_${rowId}`).val(result.amount);
    $row.find(`#packAmount_${rowId}`).val(result.packAmt);
    $row.find(`#discountAmount_${rowId}`).val(result.discAmt);

    $row.find(`#cgstAmount_${rowId}`).val(result.cgstAmt);
    $row.find(`#sgstAmount_${rowId}`).val(result.sgstAmt);
    $row.find(`#igstAmount_${rowId}`).val(result.igstAmt);
    $row.find(`#vatAmount_${rowId}`).val(result.vatAmt);
    $row.find(`#cessAmount_${rowId}`).val(result.cessAmt);

    $row.find(`#netAmount_${rowId}`).val(result.netAmount);
    $row.find(`#ldRate_${rowId}`).val(result.ldRate);
}

function calculateAmounts(
    rate,
    qty,
    packPerc,
    discPerc,
    freight,
    otherExpenses,
    cgstPer,
    sgstPer,
    igstPer,
    vatPer,
    cessPer,
    packOnBasic
) {

    rate = parseFloat(rate) || 0;
    qty = parseFloat(qty) || 0;
    packPerc = parseFloat(packPerc) || 0;
    discPerc = parseFloat(discPerc) || 0;
    freight = parseFloat(freight) || 0;
    otherExpenses = parseFloat(otherExpenses) || 0;

    // VB Cell(8)
    const amount = rate * qty;

    freight = amount > 0 ? freight : 0;

    // VB Cell(12)
    const discAmt = amount * discPerc / 100;

    // VB Cell(10)
    // const packAmt = (amount - discAmt) * packPerc / 100;

    let packAmt = 0;

    if (packOnBasic == 1) {
        packAmt = amount * packPerc / 100;
    }
    else {
        packAmt = (amount - discAmt) * packPerc / 100;
    }

    // VB Cell(26)
    const cessAmt = (amount + packAmt - discAmt) * cessPer / 100;

    // VB grossAmt
    const grossAmt =
        amount +
        packAmt -
        discAmt +
        freight;

    // VB calculateTax()
    const cgstAmt = grossAmt * cgstPer / 100;
    const sgstAmt = grossAmt * sgstPer / 100;
    const igstAmt = grossAmt * igstPer / 100;
    const vatAmt = grossAmt * vatPer / 100;

    // VB Cell(30)
    const netAmount =
        grossAmt +
        cgstAmt +
        sgstAmt +
        igstAmt +
        vatAmt +
        cessAmt +
        otherExpenses;

    // VB Cell(29)
    const ldRate =
        qty > 0 ? netAmount / qty : 0;

    return {
        amount: amount.toFixed(2),
        packAmt: packAmt.toFixed(2),
        discAmt: discAmt.toFixed(2),
        cgstAmt: cgstAmt.toFixed(2),
        sgstAmt: sgstAmt.toFixed(2),
        igstAmt: igstAmt.toFixed(2),
        vatAmt: vatAmt.toFixed(2),
        cessAmt: cessAmt.toFixed(2),
        netAmount: netAmount.toFixed(2),
        ldRate: ldRate.toFixed(2)
    };
}

//======Select 2 for Footer Table======
function initSelect2($context) {
    $context.find('select').not('#ddlDocType').each(function () {

        const $el = $(this);

        // 🔥 IMPORTANT: destroy if already initialized
        if ($el.hasClass("select2-hidden-accessible")) {
            $el.select2('destroy');
        }

        $el.select2({
            width: '100%',
            placeholder: "Select",
            allowClear: true
        });

        $el.off('select2:open').on('select2:open', function () {
            setTimeout(() => {
                let searchField = document.querySelector(
                    '.select2-container--open .select2-search__field'
                );
                if (searchField) searchField.focus();
            }, 50);
        });
    });
}

//==========Duplicate Function=============

$('#btn-duplicate').click(async function () {

    if (!rowId) {
        toastr.warning("Nothing to duplicate");
        return;
    }

    console.log("duplicate Clicked");

    await GetVNo();

    // reset state
    isEditMode = false;
    isDuplicateMode = true; 
    rowId = 0;

    // clear old doc reference
    $('#NumDocNo').val($('#NumDocNo').val());
    $('#TxtCode').val('');

    // remove id from URL
    const newUrl = window.location.pathname;
    window.history.replaceState({}, document.title, newUrl);

    showToast("Record duplicated successfully!", { type: "success" });
   
});

//=======Open Copy Modal===========   
function openCopyModal(actionType, modalId, tableId) {

    console.time("ajax-total");

    $.ajax({
        url: '/PurchaseQuotation/CopyData',
        type: 'GET',
        data: {
            actionType: actionType.trim(),
            vDate: $('#dtDocDate').val()
        },

        success: function (res) {

            console.timeEnd("ajax-total");

            console.time("tbody-build");

            let tbody = $(tableId + ' tbody');
            tbody.empty();

            $.each(res.data, function (i, item) {

                tbody.append(`
                    <tr>
                        <td><input type="checkbox"></td>
                        <td>${item.vNo ?? ''}</td>
                        <td>${item.vType ?? ''}</td>
                        <td>${item.vDate ?? ''}</td>
                        <td>${item.itemCode ?? ''}</td>
                        <td>${item.itemName ?? ''}</td>
                        <td>${item.make ?? ''}</td>
                        <td>${item.techDesc ?? ''}</td>
                        <td>${item.unit ?? ''}</td>
                        <td>${item.qty ?? ''}</td>
                        <td>${item.makeCode ?? ''}</td>
                        <td>${item.uCode ?? ''}</td>
                        <td>${item.taxCode ?? ''}</td>
                    </tr>
                `);
            });

            console.timeEnd("tbody-build");

            console.time("modal-show");

            $(modalId).modal('show');

            console.timeEnd("modal-show");
        }
    });
}

//========Common for both tables (PR & PQ)========
function wireSelectAll(selectAllId, tableId) {

    $(document).on('change', selectAllId, function () {

        const isChecked = $(this).prop('checked');

        $(`${tableId} tbody input[type="checkbox"]`)
            .prop('checked', isChecked);
    });

    $(document).on(
        'change',
        `${tableId} tbody input[type="checkbox"]`,
        function () {

            const total =
                $(`${tableId} tbody input[type="checkbox"]`).length;

            const checked =
                $(`${tableId} tbody input[type="checkbox"]:checked`).length;

            $(selectAllId).prop('checked', total === checked);
        }
    );
}

//========== Modal Table Search ==========
function wireTableSearch(searchBoxId, tableId) {

    $(document).on('keyup', searchBoxId, function () {

        const searchText = $(this).val().toLowerCase().trim();

        $(`${tableId} tbody tr`).each(function () {

            const rowText = $(this).text().toLowerCase();

            $(this).toggle(rowText.includes(searchText));
        });
    });
}

wireSelectAll('#selectAllPR', '#tblpurchaserequestmodal');
wireSelectAll('#selectAllPQ', '#tblpurchasequotationmodal');

wireTableSearch('#searchBoxPR', '#tblpurchasequotationmodal');
wireTableSearch('#searchBoxRS', '#tblpurchaserequestmodal');

async function getSelectedRows(tableId, modalId) {

    const rows = $('#tblPurchaseQuotationListByVno tbody tr');

    if (rows.length === 1) {
        const firstItem = rows.first()
            .find('select[id^="itemName_"]')
            .val();

        if (!firstItem) {
            rows.first().remove();
        }
    }

    const checkedRows = $(tableId + ' tbody tr').filter(function () {
        return $(this).find('input[type="checkbox"]').is(':checked');
    });

    for (const row of checkedRows) {

        const $row = $(row);

        const reqNo = $row.find('td:eq(1)').text().trim();
        const reqType = $row.find('td:eq(2)').text().trim();
        const itemCode = $row.find('td:eq(4)').text().trim();
        const remarks = $row.find('td:eq(7)').text().trim();
        const qty = $row.find('td:eq(9)').text().trim();
        const makeCode = $row.find('td:eq(10)').text().trim();
        const uomCode = $row.find('td:eq(11)').text().trim();
        const taxCode = $row.find('td:eq(12)').text().trim();

        const alreadyExists = $('select[id^="itemName_"]').filter(function () {
            return $(this).val() === itemCode;
        }).length > 0;

        if (alreadyExists) {
            showToast("Duplicate item found. Only one entry is allowed.", {
                type: "warning"
            });
            continue;
        }

        const newRowId = await addNewRowBelow();

        $(`#requestNo_${newRowId}`).val(reqNo);
        $(`#requestType_${newRowId}`).val(reqType);

        $(`#itemName_${newRowId}`).val(itemCode).trigger('change');

        await new Promise(resolve => setTimeout(resolve, 300));

        $(`#itemMake_${newRowId}`).val(makeCode).trigger('change');

        console.log(
            "Selected Value:",
            $(`#itemMake_${newRowId}`).val()
        );

        $(`#itemMake_${newRowId} option`).each(function () {
            console.log("Option:", `[${$(this).val()}]`);
        });

        $(`#uom_${newRowId}`).val(uomCode).trigger('change');

        $(`#taxCode_${newRowId}`).val(taxCode).trigger('change');

        $(`#purchaserRemarks_${newRowId}`).val(remarks);
        $(`#qty_${newRowId}`).val(qty);

    }

    $(modalId).modal('hide');
}

//=======Check Valid Date========
async function checkValidDate() {

    const data = {
        vdate: $("#dtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };

    try {

        const response = await fetch('/PurchaseQuotation/CheckValidDate', {
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

//======Read Only Mode========
function applyReadOnlyMode() {

    $('#PurchaseQuotationForm').find('input, select, textarea').prop('disabled', true);
    $('#btn-save').hide();
    $('#btn-duplicate').hide();
    $('#dtDocDate,#dtQuotDate,#dtValidDate').prop('disabled', true);

    // Copy From section hide
    $('.erppage-internalaction').hide();
    $('#CopyFrom').closest('.erppagedropdown').hide();
    $('#browseBtn').hide();
    $('#dropZone').css('pointer-events', 'none');

    // Print rakhna hai to enable kar do
    $('.erppage-btn-print').prop('disabled', false);

    // Back button enabled rahega
    $('.erppage-header-back').prop('disabled', false);


    $('#tblPurchaseQuotationListByVno').find('input, select, textarea').prop('disabled', true);
    $('#tblPurchaseQuotationListByVno').find('.act-btn').prop('disabled', true);
    $('.btn-delete-attachment').hide();
}

//=======Report 1========
function PendingQCReport() {

    var reportName = "QUOTATION1";
    // Crystal Report Formula
    var SelForMul =
        "{QUOTATION1.V_TYPE}='" + $("#ddlDocType").val() + "'" +
        " AND {QUOTATION1.V_NO}= " + $("#NumDocNo").val() +
        " AND {QUOTATION1.COMP_CODE}= " + window.globalVariables.compCode +
        " AND {QUOTATION1.BRANCH_CODE}= " + window.globalVariables.branchCode +
        " AND {QUOTATION1.YEAR_CODE}= " + window.globalVariables.yearCode;
    var formulaFields = {
        Reportname: reportName,
        selectionFormula: SelForMul,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "PURCHASE QUOTATION"
        }
    };

    // ===== DEBUG LOGS =====
    console.log("Company Name:", window.globalVariables.companyName);
    console.log("Company Add1:", window.globalVariables.add1);
    console.log("Company Add2:", window.globalVariables.add2);

    console.log("Comp Code:", window.globalVariables.compCode);
    console.log("Branch Code:", window.globalVariables.branchCode);
    console.log("Year Code:", window.globalVariables.yearCode);

    console.log("Database:", window.database.db);

    console.log("Selection Formula:", SelForMul);

    console.log("Formula Fields:", formulaFields);
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

//======Report 2=============
function PendingQCReport1() {

    var reportName = "QUOTATION2";
    // Crystal Report Formula
    var SelForMul =
        "{QUOTATION1.V_TYPE}='" + $("#ddlDocType").val() + "'" +
        " AND {QUOTATION1.V_NO}= " + $("#NumDocNo").val() +
        " AND {QUOTATION1.COMP_CODE}= " + window.globalVariables.compCode +
        " AND {QUOTATION1.BRANCH_CODE}= " + window.globalVariables.branchCode +
        " AND {QUOTATION1.YEAR_CODE}= " + window.globalVariables.yearCode;
    var formulaFields = {
        Reportname: reportName,
        selectionFormula: SelForMul,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "PURCHASE QUOTATION"
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

//=======Purchase History========
async function loadPurchaseHistory(itemCode) {
    try {
        const res = await $.ajax({
            url: '/PurchaseQuotation/GetPurchaseHistory',
            type: 'GET',
            data: {
                itemcode: itemCode
            }
        });

        if (!res.success) {
            showToast(res.message, { type: "error" });
            toastr.error(res.message);
            return null;
        }

        return res.data;

    } catch (err) {
        console.error(err);
        showToast("Error While Loading Load PurchaseHistory:" + err, { type: "error" });
    }
}

//=======Bind Purchase History========
function bindPurchaseHistory(data) {

    let tbody = '';

    if (!data || data.length === 0) {
        tbody = `
            <tr>
                <td colspan="17" class="text-center">
                    No Record Found
                </td>
            </tr>`;
    }
    else {

        data.forEach(item => {

            tbody += `
                <tr>
                    <td>${item.vNo ?? ''}</td>
                    <td>${item.date ?? ''}</td>
                    <td>${item.supplier ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.make ?? ''}</td>
                    <td>${item.unit ?? ''}</td>
                    <td class="text-end">${item.qty ?? 0}</td>
                    <td class="text-end">${item.rate ?? 0}</td>
                    <td class="text-end">${item.othAmt ?? 0}</td>
                    <td class="text-end">${item.cgstPer ?? 0}</td>
                    <td class="text-end">${item.sgstPer ?? 0}</td>
                    <td class="text-end">${item.igstPer ?? 0}</td>
                    <td class="text-end">${item.packPer ?? 0}</td>
                    <td class="text-end">${item.discPer ?? 0}</td>
                    <td class="text-end">${item.ldRate ?? 0}</td>
                    <td>${item.remarks ?? ''}</td>
                    <td>${item.status ?? ''}</td>
                </tr>`;
        });
    }

    $('#tblLastTenOrderHistory tbody').html(tbody);
}

//=======Purchase Quotation History========
async function loadPurchaseQuotationHistory(itemCode) {
    try {
        const res = await $.ajax({
            url: '/PurchaseQuotation/GetPurchaseQuotation',
            type: 'GET',
            data: {
                itemcode: itemCode
            }
        });

        if (!res.success) {
            showToast(res.message, { type: "error" });
            toastr.error(res.message);
            return null;
        }

        return res.data;

    } catch (err) {
        console.error(err);
        showToast("Error While Loading Load PurchaseQuotation History:" + err, { type: "error" });
    }
}

//=======Bind Purchase Quotation History========
function bindPurchaseQuotationHistory(data) {

    let tbody = '';

    if (!data || data.length === 0) {
        tbody = `
            <tr>
                <td colspan="17" class="text-center">
                    No Record Found
                </td>
            </tr>`;
    }
    else {

        data.forEach(item => {

            tbody += `
                <tr>
                    <td>${item.vNo ?? ''}</td>
                    <td>${item.date ?? ''}</td>
                    <td>${item.supplier ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.make ?? ''}</td>
                    <td>${item.unit ?? ''}</td>
                    <td>${item.groupNo ?? ''}</td>
                    <td class="text-end">${item.qty ?? 0}</td>
                    <td class="text-end">${item.rate ?? 0}</td>
                    <td class="text-end">${item.freight ?? 0}</td>
                    <td class="text-end">${item.cgstPer ?? 0}</td>
                    <td class="text-end">${item.sgstPer ?? 0}</td>
                    <td class="text-end">${item.igstPer ?? 0}</td>
                    <td class="text-end">${item.packPer ?? 0}</td>
                    <td class="text-end">${item.discPer ?? 0}</td>
                    <td class="text-end">${item.othExps ?? 0}</td>
                    <td class="text-end">${item.ldRate ?? 0}</td>
                    <td>${item.remarks ?? ''}</td>
                    <td>${item.status ?? ''}</td>
                </tr>`;
        });
    }

    $('#tblLastTenQuotationOrderHistory tbody').html(tbody);
}

//=======Order History========
async function loadOrderHistory(itemCode, Vdate) {
    try {
        const res = await $.ajax({
            url: '/PurchaseQuotation/OrderHistory',
            type: 'GET',
            data: {
                itemcode: itemCode,
                Vdate: Vdate
            }
        });

        if (!res.success) {
            showToast(res.message, { type: "error" });
            toastr.error(res.message);
            return null;
        }

        return res.data;

    } catch (err) {
        console.error(err);
        showToast("Error While Loading Order History:" + err, { type: "error" });
    }
}

//=======Bind Order History========
function bindOrderHistory(data) {

    let tbody = '';

    if (!data || data.length === 0) {
        tbody = `
            <tr>
                <td colspan="17" class="text-center">
                    No Record Found
                </td>
            </tr>`;
    }
    else {

        data.forEach(item => {

            tbody += `
                <tr>
                    <td>${item.vNo ?? ''}</td>
                    <td>${item.date ?? ''}</td>
                    <td>${item.supplier ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.make ?? ''}</td>
                    <td>${item.unit ?? ''}</td>
                    <td class="text-end">${item.qty ?? 0}</td>
                    <td class="text-end">${item.rate ?? 0}</td>
                    <td class="text-end">${item.cgstPer ?? 0}</td>
                    <td class="text-end">${item.sgstPer ?? 0}</td>
                    <td class="text-end">${item.igstPer ?? 0}</td>
                    <td class="text-end">${item.packPer ?? 0}</td>
                    <td class="text-end">${item.discPer ?? 0}</td>
                    <td class="text-end">${item.othExps ?? 0}</td>
                    <td class="text-end">${item.ldRate ?? 0}</td>
                    <td>${item.remarks ?? ''}</td>
                    <td>${item.status ?? ''}</td>
                </tr>`;
        });
    }

    $('#lastTenOrderQuotation tbody').html(tbody);
}

function isDuplicateItem(selectedItem) {

    let count = 0;

    $('select[id^="itemName_"]').each(function () {

        if ($(this).val() === selectedItem) {
            count++;
        }
    });

    return count > 1;
}

//=======Check Last Order Rate========
function checkLastOrderRate(rowId) {

    const itemCode = $(`#itemName_${rowId}`).val();
    const currentLdRate = parseFloat($(`#ldRate_${rowId}`).val()) || 0;
    const vDate = $('#dtDocDate').val();

    console.log({
        itemCode,
        currentLdRate,
        vDate
    });

    if (!itemCode || !vDate) return;

    $.ajax({
        url: '/PurchaseQuotation/GetLastOrderRate',
        type: 'GET',
        data: {
            itemCode: itemCode,
            vDate: vDate
        },
        success: function (res) {

            if (!res.success) return;

            const lastRate =
                parseFloat(res.lastRate) || 0;

            const $rateBox =
                $(`#rate_${rowId}`);

            $rateBox.css('background-color', '');

            if (lastRate <= 0) return;

            if (currentLdRate > lastRate) {

                $rateBox.css(
                    'background-color',
                    '#ffcdd2'
                );

                showToast(
                    `Current LD Rate (${currentLdRate.toFixed(2)}) is higher than Last Order Rate (${lastRate.toFixed(2)})`,
                    { type: "warning" }
                );

            } else if (currentLdRate < lastRate) {

                $rateBox.css(
                    'background-color',
                    '#c8e6c9'
                );

            }
        }
    });
}