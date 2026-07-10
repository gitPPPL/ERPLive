
const $tbody = $('#tblItemDetailsPR tbody');
const $attachmentTbody = $('#tblAttachmentPR tbody');
const urlParams = new URLSearchParams(location.search);
const rowId = urlParams.get('id');
const mode = urlParams.get('mode');
let isReadOnly = mode === 'view';
const vType = 'STPI';
const DBTableName = 'PREQUEST1';
let itemCache = {};
let IsApprovalBody = false;
let IsFinalApprovalBody = false;
let userLevel = window.userLevel;
let compCode = window.compCode;
let isInitialLoad = false;
let isMonthlyRequirementLoad = false;
let isCopyFromOrMonthly = false;
let uploadedFiles = [];
let isEdit = false;
let isSaved = true;

$(document).ready(async function () {
    await InitPage(rowId);

    if (mode === "view") {
        setFormReadOnly();
        //$('#PurchaseRequestForm').after(
        //    '<span class="badge bg-secondary ms-2">Read‑Only Mode</span>'
        //);
    }
});

//==============Page Load Functions==========
async function InitPage(rowId) {
    await CheckIsApprovalBody();
    await CheckIsFinalApprovalBody();
    ApprovalFields();
    $('#DtDocDate').focus();
    await WireAllEvents();
    if (!rowId) {
        GetVNo();
        const date = new Date();
        document.getElementById('DtDocDate').valueAsDate = date;
        date.setDate(date.getDate() + 7);
        document.getElementById('DtRequiredDate').valueAsDate = date;

        document.getElementById("ddlStatus").disabled = true;
        await addRow();
        ApprovalFields();
        calculateTotalAmount();
    }
    else {
        if (!isReadOnly) {
            isEdit = true;
        }
        await LoadFormByID(rowId);
        checkApprovalStatus(vType, rowId, DBTableName);
    }
    if (!isInitialLoad) {
        await bindHeaderDropDowns();
        fetchPlanList();
        fetchRequesterList();
    }

    //addAttachmentRow({ index: 0 });

}

//============Check IsApprovalBody=============
async function CheckIsApprovalBody() {
    try {
        const response = await $.ajax({
            url: '/PurchaseRequest/CheckIsApprovalBody',
            type: 'GET',
            dataType: 'json'
        });

        if (response.exists) {
            IsApprovalBody = true;
        } else {
            IsApprovalBody = false;
        }
    } catch (error) {
        console.error('Error checking approval stage:', error);
        showToast('An error occurred while checking the approval stage.', { type: "error" });
    }
}

async function CheckIsFinalApprovalBody() {
    $.ajax({
        url: '/PurchaseRequest/CheckIsFinalApprovalBody',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response.success === false) {
                showToast("An error occurred in checking Final Approval Body!", { type: "error" });
                return;
            }
            if (response.exists) {
                IsFinalApprovalBody = true;
            } else {
                IsFinalApprovalBody = false;
            }
        },
        error: function (xhr, status, error) {
            console.error("AJAX Error:", error);
            showToast("An error occurred in checking Final Approval Body!", { type: "error" });
        }
    });
}

function ApprovalFields() {
    if (IsApprovalBody || userLevel === 1) {
        $('#item-approx-rate, #itemCol-approx-rate, #item-OpenReqQty, #itemCol-OpenReqQty, #btn-lastTenPurchaseOrder, #btn-lastTenPurchases, #div-ApproxTotalAmt').show();
        $tbody.find('tr').each(function () {
            $(this).find('.td-approxRate, .td-open-req-qty').show();
            $(this).find('.approvalStatus').prop('disabled', false);
            $(this).find('.approval-remarks').prop('disabled', false);
            $(this).find('.td-status').show();
        });
        $('#btn-monthlyrequirement').show();
        $('#btn-copyFromDiv').show();
        $('#item-status, #itemCol-status').show();
    }
    else {
        $('#item-approx-rate, #itemCol-approx-rate, #item-OpenReqQty, #itemCol-OpenReqQty, #btn-lastTenPurchaseOrder, #btn-lastTenPurchases, #div-ApproxTotalAmt').hide();
        $tbody.find('tr').each(function () {
            $(this).find('.td-approxRate, .td-open-req-qty').hide();
            $(this).find('.approvalStatus').prop('disabled', true);
            $(this).find('.approval-remarks').prop('disabled', true);
            $(this).find('.td-status').hide();
        });
        $('#btn-monthlyrequirement').hide();
        $('#btn-copyFromDiv').hide();
        $('#item-status, #itemCol-status').hide();
    }
}

//==============Wire Events=============
async function WireAllEvents() {

    //==========Department change=========
    $('#ddlDepartment').on('change', async function () {
        const selectedValue = this.value;
        const docDate = $('#DtDocDate').val();
        if (!docDate) {
            showToast('Please select a valid Doc Date before proceeding.', { type: "warning" });
            $("#DtDocDate").focus();
            return;
        }
        await fetchDDlItems($('.ddlItem'), selectedValue, null);
        if (isMonthlyRequirementLoad) {
            $tbody.empty();
            await addRow();
            ApprovalFields();

            calculateTotalAmount();
        }
    });

    //=============Item change==========
    $(document).on('change', 'select.ddlItem', async function () {

        const selectedId = $(this).val();

        if (!isMonthlyRequirementLoad) {
            //Duplicate Items
            if (checkDuplicateItems(this)) {
                return;
            }
        }
        if (!isCopyFromOrMonthly && !isReadOnly && !IsApprovalBody && !isInitialLoad) {
            if (selectedId && selectedId.length > 0) {
                const isMonthly = await CheckMonthlyReq(selectedId);
                if (isMonthly) {
                    setInvalid($(this), 'This Item is a Monthly Requirement Item, So You Can not Make a Requistion');
                    return;
                }
            }
        }

        const $row = $(this).closest('tr');

        const unitName = $(this).find(':selected').data('uname') || '';
        const unitCode = $(this).find(':selected').data('ucode') || '';

        //  Assign units to fields
        $row.find('.unit-code').val(unitCode);
        $row.find('.unit-name').val(unitName);

        if (selectedId && selectedId.length > 0) {
            if (!isCopyFromOrMonthly && !isInitialLoad) {
                await loadDropdown({
                    type: 'Make',
                    selectElem: $row.find('.ddlMake'),
                    defaultText: "- Select Make-",
                    selectedValue: null,
                    extraData: selectedId
                });
            }
            try {
                if (!isInitialLoad) {
                    await fetchItemAllDetails(selectedId, $row);
                }
            } catch (err) {
                console.error('Error in fetch chain:', err);
            }
        }
    });

    //================Save=============
    $("#btnSaves").click(async function (e) {
        e.preventDefault();

        if (!await Validate()) return;

        const payload = await CollectFormData();
        if (!payload && payload.length < 0) {
            return;
        }
        $("#btnSaves").prop("disabled", true);

        saveUpdate(payload);

    });

    //=============Add Row Button Click==========
    $(document).on('click', '.btn-add-action', async function () {
        const currentSelect = $(this)
            .closest('tr')
            .find('.ddlItem')[0];

        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            return;
        }
        isInitialLoad = false;
        await addRow();
        ApprovalFields();

        calculateTotalAmount();
        $tbody.find('tr:last .ddlItem').focus();
    });

    //=============Delete Row Button Click==========
    $(document).on('click', '.btn-delete-action', function () {
        // Prevent deleting if only one row exists
        if ($tbody.find('tr').length === 1) {
            return;
        }
        const $row = $(this).closest('tr');
        const isLastRow = $row.is(':last-child');
        $row.remove();
        if (isLastRow) {
            const $lastRow = $tbody.find('tr:last');
            if ($lastRow.length > 0 && $lastRow.find('.btn-add-action').length === 0) {
                const $wrap = $lastRow.find('.action-wrap');

                //$lastRow.find('td:last').prepend(
                //    `<button type="button" class="act-btn add btn-add-action" title="Add Row"><i class="fa fa-plus-circle"></i></button>`
                //);
                $wrap.prepend(`
                <button type="button"
                        class="act-btn add btn-add-action"
                        title="Add Row">
                    <i class="fa fa-plus-circle"></i>
                </button>
            `);
            }
        }

        calculateTotalAmount();
    });

    ////======Delete  Event for Image on Edit=========
    $(document).on('click', '.erppageattachmentsectiondelete', function () {
        const $fileItem = $(this).closest('.erppageattachmentsectionfileitem');

        const fileName = $fileItem.find('.erppageattachmentsectionfilename').text().trim();

        const index = uploadedFiles.findIndex(item => item && item.FILE_NAME === fileName);

        if (index !== -1) {
            uploadedFiles.splice(index, 1);
        }

        $fileItem.remove();
    });


    $('#selectAllPR').on('change', function () {
        const isChecked = $(this).is(':checked');
        $('#tblpurchasequotationmodal tbody input[type="checkbox"]').prop('checked', isChecked);
    });

    $(document).on('change', '#tblpurchasequotationmodal tbody input[type="checkbox"]', function () {
        const totalCheckboxes = $('#tblpurchasequotationmodal tbody input[type="checkbox"]').length;
        const checkedCheckboxes = $('#tblpurchasequotationmodal tbody input[type="checkbox"]:checked').length;

        $('#selectAllPR').prop('checked', totalCheckboxes === checkedCheckboxes);
    });

    //////////// Searching Data using dropdown Dropdown in purchase Quatation Table
    let selectedColumn = parseInt($('#columnSelect').val());
    $('#searchLabel').html(`Search in: <strong>${$('#columnSelect option:selected').text()}</strong>`);

    $('#columnSelect').on('change', function () {
        selectedColumn = parseInt($(this).val());
        $('#searchLabel').html(`Search in: <strong>${$(this).find('option:selected').text()}</strong>`);
        $('#searchBoxPR').trigger('keyup');
    });

    $('#searchBoxPR').on('keyup', function () {
        const searchTerm = $(this).val().toLowerCase();
        $('#tblpurchasequotationmodal tbody tr').each(function () {
            const cell = $(this).find('td').eq(selectedColumn);
            const text = cell.text().toLowerCase();
            $(this).toggle(text.includes(searchTerm));
        });
    });

    //=============Req Qty Change Event==========
    $(document).on('change', '.required-qty', function () {
        const $row = $(this).closest('tr');
        const reqQty = parseFloat($(this).val()) || 0;
        if (!IsApprovalBody && reqQty > 0) {
            $row.find('.user-qty').val(reqQty);
        }
    });

    //=============Last 10 Purchase Request Click==========
    $('#btn-lastTenPurchaseRequest').on("click", function (e) {
        e.preventDefault();
        GetLastTenPurchaseRequest();
    });
    //=============Last 10 Consumption Click==========
    $('#btn-lastTenConsumption').on("click", function (e) {
        e.preventDefault();
        GetLastTenConsumptionDetails();
    });
    //=============Last 10 Purchase History Click==========
    $('#btn-lastTenPurchases').on("click", function (e) {
        e.preventDefault();
        GetLastTenPurchaseHistory();
    });
    //=============Last 10 Order History Click==========
    $('#btn-lastTenPurchaseOrder').on("click", function (e) {
        e.preventDefault();
        GetLastTenOrderHistory();
    });
    //============Item Wise Purchase Request==========
    $(document).on('click', '#btn-itemWise-PurchaseRequest', function (e) {
        e.preventDefault();
        let itemCode = $(this).closest(".erppage-dropdownaction-menu").data("itemcode");
        getItemWisePurchaseRequest(itemCode);
    });
    //============Item Wise Consumption History==========
    $(document).on('click', '#btn-itemWise-ConsumptionHistory', function (e) {
        e.preventDefault();
        let itemCode = $(this).closest(".erppage-dropdownaction-menu").data("itemcode");
        getItemWiseConsumptionHistory(itemCode);
    });
    //============Item Wise Purchase Order History==========
    $(document).on('click', '#btn-itemWise-PurchaseOrder', function (e) {
        e.preventDefault();
        let itemCode = $(this).closest(".erppage-dropdownaction-menu").data("itemcode");
        getItemWisePurchaseOrder(itemCode);
    });
    //============Item Wise Purchase Quotation History==========
    $(document).on('click', '#btn-itemWise-PurchaseQuotation', function (e) {
        e.preventDefault();
        let itemCode = $(this).closest(".erppage-dropdownaction-menu").data("itemcode");
        getItemWisePurchaseQuotation(itemCode);
    });
    //============Item Wise Purchase Receipt History==========
    $(document).on('click', '#btn-itemWise-PurchaseReceiptHistory', function (e) {
        e.preventDefault();
        let itemCode = $(this).closest(".erppage-dropdownaction-menu").data("itemcode");
        getItemWisePurchaseReceiptHistory(itemCode);
    });
    //============Item Wise Purchase Receipt History==========
    $(document).on('click', '#btn-itemWise-PurchaseHistory', function (e) {
        e.preventDefault();
        let itemCode = $(this).closest(".erppage-dropdownaction-menu").data("itemcode");
        getItemWisePurchaseHistory(itemCode);
    });

    //==============Total Approx Rate Claculation on field chnage
    $(document).on('change', '.required-qty, .approx-rate', function (e) {
        e.preventDefault();
        calculateTotalAmount();
    });

    //====================Remove the red border from required instruction======
    $(document).on('change', '#DtRequiredDate', function () {
        clearInvalid($('#txtRequiredDate'));
    });
    //=============== Enter Key Focus =============
    $(document).on('keydown', 'input, textarea, select', function (e) {
        if (e.key !== "Enter") return;

        e.preventDefault();
        moveToNext(this);
    });
    
    //===========Track the save status for sending approval==========
    $(document).on('change, input', '#PurchaseRequestForm input, #PurchaseRequestForm select, #tblItemDetailsPR input, #tblItemDetailsPR select', function (e) {
        if (!e.originalEvent) return;
        isSaved = false;
    });
}
function moveToNext(current) {

    const $focusable = $('input:visible, select:visible, textarea:visible')
        .filter(':enabled');

    const index = $focusable.index(current);

    if (index > -1 && index + 1 < $focusable.length) {
        $focusable.eq(index + 1).focus();
    }
}
//===================Collect Form Data===========
async function CollectFormData() {
    await new Promise(resolve => setTimeout(resolve, 500));
    let approvStatus = '';
    let approvRemarks = '';
    if (IsFinalApprovalBody) {
        approvStatus = 'Approved';
        approvRemarks = 'Document Approved.';
    }

    const Code = $.trim($('#TxtCode').val());
    const rowsData = collectRowsData();
    if (!rowsData || rowsData.length === 0) {
        return null;
    }
    //const AttachmentsRowsData = collectPurchaseDocumentsData();
    const AttachmentsRowsData = getUploadedFiles();
    const Header = {
        DOC_ID: $.trim($('#TxtCode').val()),
        V_NO: parseFloat($.trim($('#NumDocNo').val())) || 0,
        V_DATE: formatDate($("#DtDocDate").val()),
        DEPT_CODE: parseInt($('#ddlDepartment').val()) || 0,
        TARGET_DATE: formatDate($("#DtRequiredDate").val()),
        REASON: $.trim($('#txtRequiredDate').val()),
        PLACE_CODE: parseInt($('#ddlPlace').val()) || 0,
        URGENT_REQUEST: $('#chkUrgentRequest').prop('checked') ? 1 : 0,
        STATUS: parseInt($('#ddlStatus').val()) || 0,
        OWNER_CODE: parseInt($.trim($('#ddlRequester').val())) || 0,
        OWNER_NAME: $.trim($('#ddlRequester option:selected').data("name")) || "",
        PLAN_NO: parseInt($.trim($('#ddlPlanComplain').val())) || 0,
        PLAN_TYPE: $('#ddlPlanComplain option:selected').data('plantype') || "",
        REMARKS: $.trim($('#txtRemarks').val()),
        FAPROV_STATUS: approvStatus,
        FAPROV_REMARKS: approvRemarks,
        ACTION: (!Code || Code.trim() === '') ? 'INSERT' : 'UPDATE'
    };
    const payload = {
        Header,
        itamDetails: rowsData,
        purchaseDocuments: AttachmentsRowsData
    };

    return payload;
}

function collectRowsData() {
    const rows = [];
    let hasError = false;
    const $allRows = $('#tblItemDetailsPR tbody tr');


    function isRowEmpty($row) {
        const itemVal = $row.find('.ddlItem').val();
        return !itemVal || itemVal === '0' || itemVal.trim() === '';
    }

    $allRows.each(function (index) {

        const $row = $(this);

        if (isRowEmpty($row)) {
            return; // skip blank row
        }

        const rowLabel = `row ${index + 1}`;

        //if (!validateItemRow($row, rowLabel)) {
        //    hasError = true;
        //    return false; // exit .each loop
        //}

        // Build and push rowData as before...
        const $ddlItem = $row.find('.ddlItem');
        const $ddlPlaceUse = $row.find('.ddlplaceuse');
        const $priorityType = $row.find('.priority-type');
        const $scrapType = $row.find('.scrap-type');
        const $workType = $row.find('.work-type');
        const $status = $row.find('.status');
        const $approvalStatus = $row.find('.approvalStatus');

        let ITEM_CODE = $row.find('td:first').text().trim();
        if (!ITEM_CODE) {
            ITEM_CODE = parseInt($ddlItem.val());
            $row.find('td:first').text(ITEM_CODE);
        }
        let status = 1
        if (isEdit) {
            if (IsApprovalBody) {
                if ($status.val() == 1 || $status.val() == 2 || $status.val() == 3) { }
                else {
                    setInvalid($status, "Status should be 1/2/3 for Open/Cancel/Close.");
                    hasError = true;
                    return false;
                }
            }
            if ($('#ddlStatus').val() === 1) {
                status = $status.val() === 0 ? 1 : $status.val();
            } else {
                status = $('#ddlStatus').val();
            }

        }

        const rowData = {
            ITEM_CODE,
            ITEM_TEXT: $ddlItem.find('option:selected').text().trim(),
            MAKE_CODE: parseInt($row.find('.ddlMake').val()) || 0,
            MAKE_TEXT: $row.find('.ddlMake option:selected').text().trim(),
            UOM_CODE: parseInt($row.find('.unit-code').val()) || 0,
            UOM_NAME: $row.find('.unit-name').val() || "",
            TECH_DESC: $row.find('.tech-desc').val()?.trim() || "",
            APROX_RATE: parseFloat($row.find('.approx-rate').val()) || 0,

            APROV_CODE: parseInt($approvalStatus.val()) || 0,
            APROV_STATUS: $approvalStatus.val() === ""
                ? ""
                : $approvalStatus.find("option:selected").text().trim(),

            APROV_REMARKS: $row.find('.approval-remarks').val()?.trim() || "",
            STD_REQ: parseFloat($row.find('.std-req').val()) || 0,
            CUR_STK: parseFloat($row.find('.current-stock').val()) || 0,
            AVG_CONS: parseFloat($row.find('.avg-consumption').val()) || 0,
            OPEN_POQTY: parseFloat($row.find('.pending-po-qty').val()) || 0,
            OPEN_RQQTY: parseFloat($row.find('.open-req-qty').val()) || 0,
            USER_QTY: parseFloat($row.find('.user-qty').val()) || 0,
            REQ_QTY: parseFloat($row.find('.required-qty').val()) || 0,
            REQ_REASON: $row.find('.req-reason').val()?.trim() || "",
            PLACE_Code: parseInt($ddlPlaceUse.val()) || 0,
            PLACE_USE: $ddlPlaceUse.find('option:selected').text().trim() || "",

            PRIORITY_CODE: parseInt($priorityType.val()) || 0,
            PRIORITY_TYPE: $priorityType.find("option:selected").text().trim() || "",

            SCRAP_TYPE: $scrapType.val() === "Reu" ? "Reusable" :
                $scrapType.val() === "Scr" ? "Scrap" :
                    $scrapType.val() || "",

            WORK_TYPECODE: parseInt($workType.val()) || 0,
            WORK_TYPE: $workType.find('option:selected').text().trim() || "",

            REMARKS: $row.find('.remarks').val()?.trim() || "",
            //STATUS: parseInt($status.val()) || 0,
            STATUS: status,
            MONTHLY: $row.find('.monthly').val()
        };

        rows.push(rowData);
    });

    return hasError ? [] : rows;
}

function collectFile(file) {

    const reader = new FileReader();

    reader.onload = function (e) {

        uploadedFiles.push({
            FILE_NAME: file.name,
            FILE_DATA: e.target.result.split(',')[1] // base64 only
        });
    };

    reader.readAsDataURL(file);
}
function getUploadedFiles() {
    return uploadedFiles;
}
//=================Save================
function saveUpdate(payload) {
    $.ajax({
        url: '/PurchaseRequest/SavedData',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(payload),
        success: function (response) {
            if (response.success) {
                isSaved = true;
                showToast("Saved successfully!", { type: "success" });
                setFormReadOnly();
                isReadOnly = true;
                setTimeout(() => window.location.href = '/PurchaseRequest/Index?id=' + encodeURIComponent($('#NumDocNo').val()) + '&mode=view', 1000);
                if (isReadOnly) {
                    const vNo = $('#NumDocNo').val();
                    checkApprovalStatus(vType, vNo, DBTableName);
                }
            } else {
                showToast(response.message || "Save failed.", { type: "error" });
            }
        },
        error: function (xhr, status, error) {
            let errorMessage = "Something went wrong.";
            if (xhr.status === 400) {
                errorMessage = "Bad Request: " + xhr.responseText;
            } else if (xhr.status === 500) {
                errorMessage = "Server error: " + xhr.responseText;
            } else {
                errorMessage = "Unexpected error: " + xhr.statusText;
            }

            showToast("Error: ", errorMessage, { type: "error" });

        },
        complete: function () {
            $("#btnSaves").prop("disabled", false);
        }
    });
}

//===============Readonly============
function setFormReadOnly() {
    const form = $('#PurchaseRequestForm');
    form.addClass('erppage-readonly');
    form.find('input, textarea, select').prop('disabled', true);
    $('#btnSaves, #btn-monthlyrequirement, #btn-copyFromDiv, .erppageattachmentsectiondelete').hide();
    $('.btn-add-action, .btn-delete-action').addClass('disabled').css('pointer-events', 'none');
    $('#dropZone')
        .css({
            'pointer-events': 'none',
            //'opacity': '0.65',      
            'cursor': 'not-allowed'
        });
}

//============VNO==================
async function GetVNo() {
    try {
        const res = await fetch('/PurchaseRequest/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();

        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//================Get By Id===============
async function LoadFormByID(id) {
    try {
        const res = await $.ajax({
            url: '/PurchaseRequestList/GetDataByCode',
            method: 'GET',
            data: { code: id }
        });

        if (!res.success) {
            showToast(res.message || "Failed to load data.", { type: "error" });
            return;
        }

        const header = res.data.header;
        const details = res.data.itamDetails;
        const attachments = res.data.purchaseDocuments;
        await bindHeaderData(header);
        await ItemTableData(details);
        await AttachmentTableData(attachments);

    } catch (err) {
        showToast("Something went wrong while loading the form.", { type: "error" });
        //console.error(err);
    }
}

async function bindHeaderData(header) {
    $('#TxtCode').val(header.doC_ID || '');
    $('#NumDocNo').val(header.v_NO || '');
    $('#DtDocDate').val(formatDate(header.v_DATE));
    $('#DtRequiredDate').val(formatDate(header.targeT_DATE));
    $('#txtRequiredDate').val(header.reason || '');
    $('#chkUrgentRequest').prop('checked', header.urgenT_REQUEST === 1);
    $('#txtRemarks').val(header.remarks || '');

    await bindHeaderDropDowns({
        dept: header.depT_CODE ?? 0,
        status: header.status ?? 0,
        place: header.placE_CODE ?? 0,
        //plan: header.plaN_NO ?? 0,
        //requester: header.owneR_CODE ?? "",
    });
    fetchPlanList(header.plaN_NO)
    fetchRequesterList(header.owneR_CODE)
}
async function ItemTableData(details) {
    const $tbody = $('#tblItemDetailsPR tbody');
    $tbody.empty();
    if (details && details.length > 0) {
        console.log("details", details);
        isInitialLoad = true;
        for (const item of details) {
            await addRow(item);
            ApprovalFields();

            calculateTotalAmount();
        }
    }
    else {
        isInitialLoad = false;
        await addRow();
        ApprovalFields();

        calculateTotalAmount();
    }
}
async function AttachmentTableData(attachments) {
    //const $attachmentBody = $('#tblAttachmentPR tbody');
    //$attachmentBody.empty();

    //for (const attach of attachments) {
    //    const attachRowData = {
    //        index: attach.index || Date.now(),
    //        fileName: attach.filE_NAME || 'No file',
    //        filePath: attach.filE_Path || ''
    //    };
    //    addAttachmentRow(attachRowData);
    //}
    //const attachments = res.data.purchaseDocuments || [];

    if (attachments.length) {

        const files = attachments.map(x =>
            base64ToFile(x.filE_DATA, x.filE_NAME)
        );

        renderFiles(files);
    }
}
function base64ToFile(base64, fileName) {

    const ext = fileName.split('.').pop().toLowerCase();

    let mimeType = 'application/octet-stream';

    if (['jpg', 'jpeg'].includes(ext))
        mimeType = 'image/jpeg';
    else if (ext === 'png')
        mimeType = 'image/png';
    else if (ext === 'gif')
        mimeType = 'image/gif';
    else if (ext === 'pdf')
        mimeType = 'application/pdf';

    const byteString = atob(base64);
    const arrayBuffer = new ArrayBuffer(byteString.length);
    const intArray = new Uint8Array(arrayBuffer);

    for (let i = 0; i < byteString.length; i++) {
        intArray[i] = byteString.charCodeAt(i);
    }

    return new File(
        [intArray],
        fileName,
        { type: mimeType }
    );
}

function formatDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    if (isNaN(date)) return null;
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

//================DROPDOWNS============
async function bindHeaderDropDowns(data = {}) {
    const viewMode = isReadOnly ? 0 : 1;
    await Promise.all([
        bindDropdown('PurchaseRequest', 'Department', '#ddlDepartment', '-- Select Department --', data.dept || null, null, false, viewMode, true),
        bindDropdown('PurchaseRequest', 'DocStatus', '#ddlStatus', '-- Select Status --', data.status || null, null, true, null, true),
        bindDropdown('PurchaseRequest', 'Place', '#ddlPlace', '-- Select Place --', data.place || null, null, false, null, true),
        //bindDropdown('PurchaseRequest', 'Requester', '#ddlRequester', '-- Select Requester --', data.requester || null, null, false, null, true)
    ]);
}

function fetchRequesterList(selectedValue = null) {
    $.ajax({
        url: '/PurchaseRequest/GetRequesterList',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response) {
                console.log("response: ", response);
                const $ddl = $('#ddlRequester');
                let html = '<option value="">-- Select Requester --</option>';
                $.each(response.data, function (i, item) {
                    html += `<option 
                                value="${item.Code}" 
                                data-name="${item.Name}" 
                                data-designation="${item.Designation}">
                                ${item.Name} | ${item.Code} | ${item.Designation}
                            </option>`;
                });
                $ddl.html(html);
                $ddl.select2({
                    placeholder: '-- Select Requester --',
                    allowClear: true,
                    templateSelection: function (option) {
                        if (!option.id) return option.text;

                        const name = $(option.element).data('name');
                        return `${name}`;
                    }
                });
                $ddl.on('select2:open', function () {
                    setTimeout(function () {
                        let searchBox = document.querySelector('.select2-container--open .select2-search__field');

                        if (searchBox) {
                            searchBox.focus();
                        }
                    }, 0);
                });
                $ddl.val(selectedValue || '').trigger('change');
            }
        },
        error: function (xhr, status, error) {
            console.error("Error loading Requester:", error);
        }
    });
}
function fetchPlanList(selectedValue = null) {
    $.ajax({
        url: '/PurchaseRequest/GetPlanList',
        type: 'GET',
        dataType: 'json',
        success: function (response) {
            if (response) {
                const $ddl = $('#ddlPlanComplain');
                let html = '<option value="">-- Select Plan/Complain --</option>';
                $.each(response.data, function (i, item) {
                    html += `<option value="${item.PlanNo}" data-plantype="${item.PlanType}">${item.PlanType} | ${item.PlanNo} | ${item.deptName} | ${item.FaltName} | ${item.MachName} | ${item.ComplainDate}</option>`;
                });
                $ddl.html(html);
                $ddl.select2({
                    placeholder: '-- Select Plan/Complain --',
                    allowClear: true,
                    templateSelection: function (option) {
                        if (!option.id) return option.text;

                        return option.id;
                    }
                });
                $ddl.on('select2:open', function () {
                    setTimeout(function () {
                        let searchBox = document.querySelector('.select2-container--open .select2-search__field');

                        if (searchBox) {
                            searchBox.focus();
                        }
                    }, 0);
                });
                $ddl.val(selectedValue || '').trigger('change');
            }
        },
        error: function (xhr, status, error) {
            console.error("Error loading Plan/Complain:", error);
        }
    });
}

async function loadDropdown({ type, selectElem, defaultText = "- Select -", selectedValue = null, extraData = null }) {
    await bindDropdown('PurchaseRequest', type, selectElem, defaultText, selectedValue, null, false, extraData, true);
}

function initSelect2($ddl) {
    $ddl.select2({
        placeholder: '-- Select Item --',
        allowClear: true,
    });
    $ddl.on('select2:open', function () {
        setTimeout(function () {
            let searchBox = document.querySelector('.select2-container--open .select2-search__field');

            if (searchBox) {
                searchBox.focus();
            }
        }, 0);
    });
}
let prevDept = ''
function fetchDDlItems(selector, deptId, selectedValue = null) {
    const $ddl = $(selector);

    // ✅ If cached for this department
    if (itemCache[deptId]) {

        $ddl.html(itemCache[deptId]);

        if (!$ddl.hasClass('select2-hidden-accessible')) {
            initSelect2($ddl);
        }

        $ddl.val(selectedValue || '').trigger('change');
        return Promise.resolve();
    }

    return $.ajax({
        url: '/PurchaseRequest/GetddlItems',
        type: 'GET',
        dataType: 'json',
        data: { deptid: deptId },
        success: function (response) {
            if (response) {
                let html = '<option value="">-- Select Item --</option>';

                $.each(response.data, function (i, item) {
                    html += `<option value="${item.CODE}" data-ucode="${item.UCode}" data-uname="${item.Unit}">${item.name}</option>`;
                });

                // ✅ store per dept
                itemCache[deptId] = html;

                $ddl.html(html);

                if (!$ddl.hasClass('select2-hidden-accessible')) {
                    initSelect2($ddl);
                }

                $ddl.val(selectedValue || '').trigger('change');
            }
        },
        error: function (xhr, status, error) {
            console.error("Error loading Items:", error);
        }
    });
}


//==========================Add Item Rows=====================
async function addRow(data = {}) {
    $tbody.find('.btn-add-action').remove();
    const row = `
                <tr class="no-border-input">
                        <td style="display:none;">${data.iteM_CODE || ''}</td>
                        <td class="freeze-item"><select class="erppagetable-control ddlItem"></select></td>
                        <td><select class="erppagetable-control ddlMake"></select></td>
                        <td>
                            <input type="text" class="erppagetable-control unit-name" value="${data.uniT_NAME || ''}" disabled />
                            <input type="hidden" class="unit-code" value="${data.uniT_CODE || ''}" />
                        </td>
                        <td><input type="text" class="erppagetable-control tech-desc" value="${data.tecH_DESC || ''}"/></td>
                        <td class="td-approxRate">
                            <input type="text" class="erppagetable-control approx-rate"
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 18, 2);"
                            value="${data.aproX_RATE || ''}" />
                        </td>
                       <td>
                            <select class="erppagetable-control approvalStatus">
                                <option value="">--Select--</option>
                                <option value="Yes" ${(data.aproV_STATUS || '').toUpperCase() === "APPROVED" ? "selected" : ""}>Approved</option>
                                <option value="No" ${(data.aproV_STATUS || '').toUpperCase() === "HOLD" ? "selected" : ""}>Hold</option>
                                <option value="No" ${(data.aproV_STATUS || '').toUpperCase() === "REJECT" ? "selected" : ""}>Reject</option>
                            </select>
                        </td>
                        <td><input type="text" class="erppagetable-control approval-remarks" value="${data.aproV_REMARKS || ''}"/></td>
                        <td  class="td-open-req-qty">
                            <input type="text" class="erppagetable-control open-req-qty"
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 15, 4);"
                            value="${data.opeN_RQQTY || 0}" disabled/>
                        </td>
                        <td>
                            <input type="text" class="erppagetable-control user-qty"
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 15, 4);"
                            value="${data.useR_QTY || ''}" disabled/>
                        </td>
                        <td>
                            <input type="text" class="erppagetable-control required-qty"
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 15, 4);"
                            value="${data.reQ_QTY || ''}"/>
                        </td>
                        <td><input type="text" class="erppagetable-control req-reason" value="${data.reQ_REASON || ''}" maxlength="200"/></td>
                        <td><select class="erppagetable-control ddlplaceuse" style="width:300px"></select></td>
                        <td>
                            <select class="erppagetable-control priority-type">
                                <option value="">--Select--</option>
                                <option value="Yes" ${(data.prioritY_TYPE || '').toUpperCase() === "LOW" ? "selected" : ""}>Low</option>
                                <option value="No" ${(data.prioritY_TYPE || '').toUpperCase() === "MEDIUM" ? "selected" : ""}>Medium</option>
                                <option value="No" ${(data.prioritY_TYPE || '').toUpperCase() === "HIGH" ? "selected" : ""}>High</option>
                            </select>
                        </td>
                        <td>
                            <select class="erppagetable-control scrap-type">
                                <option value="">--Select--</option>
                                <option value="Yes" ${data.scraP_TYPE === "Yes" ? "selected" : ""}>Yes</option>
                                <option value="No" ${data.scraP_TYPE === "No" ? "selected" : ""}>No</option>
                            </select>
                        </td>
                        <td>
                            <select class="erppagetable-control work-type">
                                <option value="">--Select--</option>
                                <option value="Yes" ${(data.worK_TYPE || '').toUpperCase() === "MODIFICATION" ? "selected" : ""}>Modification</option>
                                <option value="No" ${(data.worK_TYPE || '').toUpperCase() === "NEW" ? "selected" : ""}>New</option>
                                <option value="No" ${(data.worK_TYPE || '').toUpperCase() === "OTHER" ? "selected" : ""}>Other</option>
                                <option value="No" ${(data.worK_TYPE || '').toUpperCase() === "REPAIR" ? "selected" : ""}>Repair</option>
                                <option value="No" ${(data.worK_TYPE || '').toUpperCase() === "REPLACEMENT" ? "selected" : ""}>Replacement</option>
                            </select>
                        </td>
                        <td><input type="text" class="erppagetable-control remarks" value="${data.remarks || ''}"/></td>
                        <td>
                            <input type="text" class="erppagetable-control std-req"
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 15, 4);"
                            value="${data.stD_REQ || ''}" disabled/>
                        </td>
                        <td>
                            <input type="text" class="erppagetable-control current-stock"
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 15, 4);"
                            value="${data.cuR_STK || ''}" disabled/>
                        </td>
                        <td class="td-avgCons">
                            <input type="text" class="erppagetable-control avg-consumption" 
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 15, 4);"
                            value="${data.avG_CONS || ''}" disabled/>
                        </td>
                        <td>
                            <input type="text" class="erppagetable-control pending-po-qty"
                            oninput="allowOnlyNumbers(this); SetMaxlength(this, 15, 4);"
                            value="${data.opeN_POQTY || ''}" disabled/>
                        </td>
                        <td class="td-status"><input type="text" class="erppagetable-control status" value="${data.status || ''}"/></td>
                        <td style="display:none;"><input type="text" class="erppagetable-control monthly" value="${data.isMonthly ? 'MONTHLY' : ''}"/></td>
                    <td class="action-col">
                       <div class="action-wrap">
                        <button type="button" class="act-btn add btn-add-action" title="Add Row"><i class="fa fa-plus-circle"></i></button>
                        <button type="button" class="act-btn delete btn-delete-action" title="Delete Row"><i class="fa fa-trash"></i></button>
                        <button type="button" class="act-btn more erppage-dropdownaction-btn"><i class="fa fa-ellipsis-v"></i></button>
                       </div>
                   </td>
            </tr>
        `;

    const $row = $(row);
    $tbody.append($row);

    // 🔥 IMPORTANT: bind item after row is added
    const deptId = $('#ddlDepartment').val();

    await fetchDDlItems(
        $row.find('.ddlItem'),
        deptId,
        data.iteM_CODE || null
    );

    const itemId = data.iteM_CODE || $row.find('.ddlItem').val();

    await Promise.all([
        loadDropdown({
            type: 'Make',
            selectElem: $row.find('.ddlMake'),
            defaultText: "- Select Make-",
            selectedValue: data.makE_CODE || null,
            extraData: itemId
        }),
        loadDropdown({
            type: 'PlaceUse',
            selectElem: $row.find('.ddlplaceuse'),
            defaultText: "- Select Place of Use-",
            selectedValue: data.placE_Code || null,
        }),
        //loadDropdown({
        //    type: 'Priority',
        //    selectElem: $row.find('.priority-type'),
        //    defaultText: "- Select Priority -",
        //    selectedValue: data.prioritY_CODE || null,
        //}),
        //loadDropdown({
        //    type: 'WorkType',
        //    selectElem: $row.find('.work-type'),
        //    defaultText: "- Select Work Type -",
        //    selectedValue: data.worK_TYPECODE || null,
        //}),
        //loadDropdown({
        //    type: 'ApprovalStatus',
        //    selectElem: $row.find('.approvalStatus'),
        //    defaultText: "- Select Status -",
        //    selectedValue: data.aproV_CODE || null,
        //})
    ]);
    if (isReadOnly) {
        if (data.isPOGenerated === true) {
            $row.addClass('po-generated-row');
        }
        if (data.isOrderRcvd === true) {
            $row.addClass('order-received-row');
        }
    }
}

//================Fetch Records on behalf of item=============
function fetchItemAllDetails(itemCode, $row) {

    var docDate = document.getElementById("DtDocDate").value;

    return $.ajax({
        url: '/PurchaseRequest/GetItemAllDetails',
        type: 'GET',
        data: {
            itemCode: itemCode,
            vDate: docDate
        },
        success: function (res) {

            const d = res.data;
            console.log(d);
            $row.find('input.approx-rate').val(d.rate || 0);
            $row.find('input.pending-po-qty').val(d.pendingQty || 0);
            $row.find('input.open-req-qty').val(d.total_Qty || 0);
            $row.find('input.current-stock').val(d.currentStocklist || 0);
            $row.find('input.avg-consumption').val(d.avgConsumption || 0);
            $row.find('input.tech-desc').val(d.tecH_DESC);
        }
    });
}

//====================VALIDATION==============================
async function Validate() {
    let isValid = true;
    if ((!validateRequiredField('#NumDocNo', 'Document Number')) ||
        (!validateRequiredField('#DtDocDate', ' Doc Date.')) ||
        (!validateRequiredField('#ddlDepartment', 'Department Name')) ||
        (!validateRequiredField('#ddlPlace', 'Place Type')) ||
        (!validateRequiredField('#DtRequiredDate', 'Required Date')) ||
        (!validateRequiredField('#ddlRequester', 'Requester Name')) ||
        (!validateRequiredField('#ddlStatus', 'Status.'))) {
        isValid = false;
        return false;
    }

    let docDate = new Date($('#DtDocDate').val());
    let requiredDate = new Date($('#DtRequiredDate').val());

    let vDate = $('#DtDocDate').val();
    let docNo = $('#NumDocNo').val();

    if (!(await checkValidDate())) {
        return false;
    }

    if (rowId && !isReadOnly && $('#ddlStatus').val() > 1) {
        return true;
    }

    //1. Required Date Validation
    if (requiredDate < docDate) {
        setInvalid($('#DtRequiredDate'), "Required Date should be greater than Document date.");
        isValid = false;
        return false;
    }
    // 2. Urgent requirement validation (DocDate + 5 days rule)
    let docDatePlus5 = new Date(docDate);
    docDatePlus5.setDate(docDatePlus5.getDate() + 5);

    if (requiredDate < docDatePlus5 && $('#txtRequiredDate').val().trim().length === 0) {
        setInvalid($('#txtRequiredDate'), "Please specify the Reason for urgent requirement less than 5 days.");
        isValid = false;
        return false;
    }

    //3. Maximum two requests
    const isWithinMaxRequest = await checkMaxRequestCount(docNo, vDate)
    if (!IsApprovalBody && !isWithinMaxRequest) {
        showToast("Max. Limit Exceeds.", { type: "warning" });
        isValid = false;
        return false;
    }

    //4. No record
    let hasItemRecord = false;
    let $rows = $('#tblItemDetailsPR tbody tr');

    $rows.each(function () {
        const $row = $(this);
        const itemCode = $row.find('.ddlItem').val();

        if (itemCode && itemCode.trim() !== '') {
            hasItemRecord = true;
            return false; // break only the .each loop
        }

        const currentSelect = $row.find('.ddlItem')[0];
        // Duplicate Check
        if (checkDuplicateItems(currentSelect)) {
            isValid = false;
            return false;
        }
    });

    if (!hasItemRecord) {
        showToast('No Item Record to save.', { type: 'warning' });
        $rows.find('.ddlItem').focus();
        return false;
    }

    let departName = $('#ddlDepartment').find("option:selected").text().trim();
    for (const row of $rows) {

        const $row = $(row);

        const itemInput = $row.find('.ddlItem');
        const itemCode = itemInput.val();
        const itemtext = itemInput.find("option:selected").text().trim();

        const makeInput = $row.find('.ddlMake');
        const makeCode = makeInput.val();

        const reqQtyInput = $row.find('.required-qty');
        const reqQtyVal = reqQtyInput.val();

        const reqReasonInput = $row.find('.req-reason');

        const stdReq = $row.find('.std-req').val();
        const avgConsumption = $row.find('.avg-consumption').val();
        const currentStock = $row.find('.current-stock').val();
        const pendingPOQty = $row.find('.pending-po-qty').val();

        const approvalStatusInput = $row.find('.approvalStatus');
        const approvalStatus = approvalStatusInput.find("option:selected").text().toLocaleUpperCase() || '';

        const approxRateInput = $row.find('.approx-rate');

        const approvalRemarksInput = $row.find('.approval-remarks');
        const approvalRemarks = approvalRemarksInput.val()?.trim();


        if (itemCode && itemCode.length > 0) {

            //5. Check Already Sent Request
            const requestNo = await GetRequestNo(itemCode);

            if (requestNo && requestNo.length > 0) {

                setInvalid(
                    itemInput,
                    `Request already sent for ${itemtext} in ${departName} (Req No: ${requestNo})`
                );
                isValid = false;
                return false;
            }

            //6. Required Make Code
            if (!validateRequiredField(makeInput, `Make For ${itemtext}`)) {
                isValid = false;
                return false;
            }

            //7. Valid Make Code
            if (makeCode && makeCode.length > 0) {
                const isMakeExist = await GetItemMake(itemCode, makeCode);
                if (!isMakeExist) {
                    setInvalid(makeInput, `Incorrect Make of ${itemtext}`);
                    isValid = false;
                    return false;
                }
            }

            //8. Monthly requirements
            const isMonthly = await CheckMonthlyReq(itemCode);
            if (!IsApprovalBody && isMonthly) {
                setInvalid(itemInput, 'This Item is a Monthly Requirement Item, So You Can not Make a Requistion');
                isValid = false;
                return false;
            }

            //9. Request Qty > 0
            if (reqQtyVal == null || reqQtyVal.trim() === "" || Number(reqQtyVal) === 0) {
                setInvalid(reqQtyInput, `Request Qty of ${itemtext} must be greater than 0.`);
                isValid = false;
                return false;
            }

            //10. Request Reason
            if (!validateRequiredField(reqReasonInput, `Reason for request of ${itemtext}`)) {
                isValid = false;
                return false;
            }

            //11. Required Qty > Average Consumption
            const reqQty = Number(reqQtyVal);

            if (stdReq > 0 && reqQty > avgConsumption) {
                setInvalid(
                    reqQtyInput,
                    `Required quantity of ${itemtext} is greater than average consumption.`
                );

                isValid = false;
                return false;
            }

            //12. Sufficient stock
            if ((currentStock + pendingPOQty) >= reqQty) {
                setInvalid(
                    reqQtyInput,
                    `Sufficient Stock (Stock + Pending Order) available for '${itemtext}'. Remove the request.`
                );
                if (compCode !== "2") {
                    isValid = false;
                    return false;
                }
            }

            //13. Required row fields
            if ((!validateRequiredField($row.find('.ddlplaceuse'), 'Place of Use')) ||
                (!validateRequiredField($row.find('.priority-type'), 'Priority Type')) ||
                (!validateRequiredField($row.find('.scrap-type'), 'Scrap Type')) ||
                (!validateRequiredField($row.find('.work-type'), 'Work Type'))) {
                isValid = false;
                return false;
            }

            //For Approval Body
            if (IsApprovalBody) {
                //14. Approval Status Required
                if (!validateRequiredField(approvalStatusInput, `Status of ${itemtext}`)) {
                    isValid = false;
                    return false;
                }

                //15. Approx Rate Required
                if (!validateRequiredField(approxRateInput, `Approx. rate of ${itemtext}`)) {
                    isValid = false;
                    return false;
                }
            }

            //16. Approval Status
            if ((approvalStatus === "HOLD" || approvalStatus === "REJECT") && !approvalRemarks) {
                setInvalid(
                    approvalRemarksInput,
                    `Reason for ${approvalStatus.toUpperCase()} of ${itemtext} is required.`
                );

                isValid = false;
                return false;
            }
        }
    }


    return isValid;
}

async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/PurchaseRequest/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        console.log("result: ", result);
        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;
    } catch (error) {
        console.error("Error:", error);
        return false;
    }
}
//======================Validation Helper Methods==============
//========Request Number=========
async function GetRequestNo(itemCode) {
    const deptCode = $('#ddlDepartment').val();
    const vNo = $('#NumDocNo').val();

    return $.ajax({
        url: '/PurchaseRequest/GetPurchaseRequests',
        type: 'GET',
        data: {
            itemCode: itemCode,
            deptCode: deptCode,
            vNo: vNo
        }
    }).then(function (res) {

        if (res.success) {
            return res.data;   // ✅ THIS is returned
        } else {
            return null;
        }
    }).catch(function (err) {
        console.error("AJAX Error:", err);
        return null;
    });
}

//========Duplicate=========
function checkDuplicateItems(currentSelect, silent = false) {

    const value = currentSelect.value;
    if (!value) return false;

    let duplicate = false;

    document.querySelectorAll('#tblItemDetailsPR .ddlItem').forEach(el => {
        if (el === currentSelect) return;

        if (el.value === value) {
            duplicate = true;
        }
    });

    if (duplicate) {

        $(currentSelect).addClass("is-invalid");
        if (!silent) {
            showToast("Duplicate item found!", { type: "warning" });
        }
    } else {
        $(currentSelect).removeClass("is-invalid");
    }

    return duplicate;
}
//========Item Make Code=======
async function GetItemMake(itemCode, makeCode) {

    return $.ajax({
        url: '/PurchaseRequest/GetItemMake',
        type: 'GET',
        data: {
            itemCode: itemCode,
            makeCode: makeCode
        }
    }).then(function (res) {

        if (res.success) {
            return res.exists;
        } else {
            console.error(res.message);
            return false;
        }

    }).catch(function (err) {
        console.error("AJAX Error:", err);
        return false;
    });
}

//========Monthly Req=========
async function CheckMonthlyReq(itemCode) {

    return $.ajax({
        url: '/PurchaseRequest/CheckMonthlyReq',
        type: 'GET',
        data: {
            itemCode: itemCode
        }
    }).then(function (res) {

        if (res.success) {
            return res.exists;
        } else {
            console.error(res.message);
            return false;
        }

    }).catch(function (err) {
        console.error("AJAX Error:", err);
        return false;
    });
}

//========Max Request Limit=====
async function checkMaxRequestCount(vNo, vDate) {
    try {
        const response = await $.ajax({
            url: '/PurchaseRequest/GetMaxRequestCount',
            type: 'GET',
            data: {
                vNo: vNo,
                vDate: vDate
            },
            dataType: 'json'
        });

        if (response.success) {
            return response.isWithinLimit; // Return the entire response object
        } else {
            showToast(response.message || "Failed to check request count.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error checking max request count:", error);
        showToast("An error occurred while checking the request count.", { type: "error" });
        return null;
    }
}

//=====================Modals===================

//=============Purchase Request History===========
function GetLastTenPurchaseRequest() {
    var itemCodes = [];

    $('#tblItemDetailsPR tbody tr').each(function () {

        var itemCode = $(this).find('.ddlItem').val();

        if (itemCode && itemCode !== '') {
            itemCodes.push(parseInt(itemCode));
        }
    });

    // Remove duplicates if needed
    itemCodes = [...new Set(itemCodes)];

    if (itemCodes.length > 4) {
        showToast("There are more than 5 item in Grid, Please select Rowwise History View!.", { type: "warning" });
        return;
    }
    if (itemCodes.length === 0) {
        showToast("No Items Available!.", { type: "warning" });
        return;
    }

    $.ajax({
        url: '/PurchaseRequest/GetLastTenPurchaseRequest',
        type: 'GET',
        traditional: true,
        data: {
            itemCodes: itemCodes
        },
        dataType: 'JSON',
        success: function (response) {

            if (response.success) {

                bindLastTenPurchaseRequestGrid(response.data);

                $("#lastTenPurchaseRequestModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        }
    });
}
function bindLastTenPurchaseRequestGrid(data) {

    var tbody = $("#tblLastTenPurchaseRequest tbody");

    tbody.empty();

    if (!data || data.length === 0) {

        tbody.append(`
            <tr>
                <td colspan="12" class="text-center">
                    No Records Found
                </td>
            </tr>
        `);

        return;
    }

    $.each(data, function (i, item) {

        tbody.append(`
            <tr>
                <td>${item.itemCode}</td>
                <td>${item.itemName}</td>
                <td>${item.vNo}</td>
                <td>${item.vDate}</td>
                <td>${item.department}</td>
                <td>${item.makeName}</td>
                <td>${item.unit}</td>
                <td>${parseFloat(item.qty || 0).toFixed(4)}</td>
                <td>${item.placeofUse}</td>
                <td>${item.techDesc}</td>
                <td>${item.remarks || ''}</td>
                <td>${item.status}</td>
            </tr>
        `);
    });
}

//=============Consumption History===========
function GetLastTenConsumptionDetails() {

    var itemCodes = [];

    $('#tblItemDetailsPR tbody tr').each(function () {

        var itemCode = $(this).find('.ddlItem').val();

        if (itemCode && itemCode !== '') {
            itemCodes.push(parseInt(itemCode));
        }
    });

    // Remove duplicate item codes
    itemCodes = [...new Set(itemCodes)];

    if (itemCodes.length > 4) {
        showToast("There are more than 5 item in Grid, Please select Rowwise History View!.", { type: "warning" });
        return;
    }

    if (itemCodes.length === 0) {
        showToast("No Items Available!.", { type: "warning" });
        return;
    }

    $.ajax({
        url: '/PurchaseRequest/GetLastTenConsumptionDetails',
        type: 'GET',
        traditional: true,
        data: {
            itemCodes: itemCodes
        },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenConsumptionGrid(response.data);

                $("#lastTenConsumptionModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            showToast("Error while loading consumption history.", { type: "error" });
        }
    });
}
function bindLastTenConsumptionGrid(data) {

    var tbody = $("#tblLastTenConsumption tbody");

    tbody.empty();

    if (!data || data.length === 0) {

        tbody.append(`
            <tr>
                <td colspan="11" class="text-center">
                    No Records Found
                </td>
            </tr>
        `);

        return;
    }

    $.each(data, function (i, item) {

        tbody.append(`
            <tr>
                <td>${item.itemCode}</td>
                <td>${item.vNo || ''}</td>
                <td>${item.date || ''}</td>
                <td>${item.itemName || ''}</td>
                <td>${item.make || ''}</td>
                <td>${item.unit || ''}</td>
                <td>${parseFloat(item.qty || 0).toFixed(4)}</td>
                <td>${parseFloat(item.rate || 0).toFixed(4)}</td>
                <td>${item.department || ''}</td>
                <td>${item.machine || ''}</td>
                <td>${item.remarks || ''}</td>
                <td>${item.status || ''}</td>
            </tr>
        `);
    });
}

//=============Purchase History===========
function GetLastTenPurchaseHistory() {

    var itemCodes = [];

    $('#tblItemDetailsPR tbody tr').each(function () {

        var itemCode = $(this).find('.ddlItem').val();

        if (itemCode && itemCode !== '') {
            itemCodes.push(parseInt(itemCode));
        }
    });

    itemCodes = [...new Set(itemCodes)];

    if (itemCodes.length > 4) {
        showToast("There are more than 5 item in Grid, Please select Rowwise History View!.", { type: "warning" });
        return;
    }

    if (itemCodes.length === 0) {
        showToast("No Items Available!.", { type: "warning" });
        return;
    }

    $.ajax({
        url: '/PurchaseRequest/GetLastTenPurchaseHistory',
        type: 'GET',
        traditional: true,
        data: {
            itemCodes: itemCodes
        },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenPurchaseHistoryGrid(response.data);

                $("#lastTenPurchaseHistoryModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            showToast("Error while loading purchase history.", { type: "error" });
        }
    });
}
function bindLastTenPurchaseHistoryGrid(data) {

    var tbody = $("#tblLastTenPurchaseHistory tbody");

    tbody.empty();

    if (!data || data.length === 0) {

        tbody.append(`
            <tr>
                <td colspan="18" class="text-center">
                    No Records Found
                </td>
            </tr>
        `);

        return;
    }

    $.each(data, function (i, item) {

        tbody.append(`
            <tr>
                <td>${item.itemCode}</td>
                <td>${item.vNo || ''}</td>
                <td>${item.date || ''}</td>
                <td>${item.supplier || ''}</td>
                <td>${item.itemName || ''}</td>
                <td>${item.make || ''}</td>
                <td>${item.unit || ''}</td>
                <td>${parseFloat(item.qty || 0).toFixed(4)}</td>
                <td>${parseFloat(item.rate || 0).toFixed(4)}</td>
                <td>${parseFloat(item.othAmt || 0).toFixed(4)}</td>
                <td>${parseFloat(item.cgstPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.sgstPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.igstPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.packPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.discPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.ldRate || 0).toFixed(4)}</td>
                <td>${item.remarks || ''}</td>
                <td>${item.status || ''}</td>
            </tr>
        `);
    });
}

//=============Order History===========
function GetLastTenOrderHistory() {

    var itemCodes = [];

    $('#tblItemDetailsPR tbody tr').each(function () {

        var itemCode = $(this).find('.ddlItem').val();

        if (itemCode && itemCode !== '') {
            itemCodes.push(parseInt(itemCode));
        }
    });

    itemCodes = [...new Set(itemCodes)];

    if (itemCodes.length > 4) {
        showToast("There are more than 5 item in Grid, Please select Rowwise History View!.", { type: "warning" });
        return;
    }

    if (itemCodes.length === 0) {
        showToast("No Items Available!.", { type: "warning" });
        return;
    }

    $.ajax({
        url: '/PurchaseRequest/GetLastTenOrderHistory',
        type: 'GET',
        traditional: true,
        data: {
            itemCodes: itemCodes
        },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenOrderGrid(response.data);

                $("#lastTenOrderHistoryModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function () {
            showToast("Error while loading purchase history.", { type: "error" });
        }
    });
}
function bindLastTenOrderGrid(data) {

    var tbody = $("#tblLastTenOrderHistory tbody");

    tbody.empty();

    if (!data || data.length === 0) {

        tbody.append(`
            <tr>
                <td colspan="18" class="text-center">
                    No Records Found
                </td>
            </tr>
        `);

        return;
    }

    $.each(data, function (i, item) {

        tbody.append(`
            <tr>
                <td>${item.itemCode}</td>
                <td>${item.vNo || ''}</td>
                <td>${item.date || ''}</td>
                <td>${item.supplier || ''}</td>
                <td>${item.itemName || ''}</td>
                <td>${item.make || ''}</td>
                <td>${item.unit || ''}</td>
                <td>${parseFloat(item.qty || 0).toFixed(4)}</td>
                <td>${parseFloat(item.rate || 0).toFixed(4)}</td>
                <td>${parseFloat(item.othAmt || 0).toFixed(4)}</td>
                <td>${parseFloat(item.cgstPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.sgstPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.igstPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.packPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.discPer || 0).toFixed(4)}</td>
                <td>${parseFloat(item.ldRate || 0).toFixed(4)}</td>
                <td>${item.remarks || ''}</td>
                <td>${item.status || ''}</td>
            </tr>
        `);
    });
}

//===========Load Monthly Data========
async function LoadMonthlyRequirementData() {

    const DEPT_CODE = parseInt($('#ddlDepartment').val()) || 0;

    try {
        const res = await $.ajax({
            url: '/PurchaseRequestList/GetDataMonthlyRequirement',
            method: 'GET',
            data: { deptId: DEPT_CODE }
        });

        if (res.success) {
            isMonthlyRequirementLoad = true;

            $tbody.empty();
            isCopyFromOrMonthly = true;

            //await Promise.all(res.data.map(r => addRow(r)));
            await Promise.all(res.data.map(r => addRow({ ...r, isMonthly: true })));
            ApprovalFields();

            calculateTotalAmount();
            let hasDuplicate = false;
            const seen = new Map();

            document.querySelectorAll('#tblItemDetailsPR .ddlItem').forEach(el => {
                const value = $(el).val();

                if (!value) return;

                if (seen.has(value)) {
                    hasDuplicate = true;

                    $(el).addClass('is-invalid');

                } else {
                    seen.set(value, el);
                }
            });

            // single toastr
            if (hasDuplicate) {
                showToast("Duplicate items found and marked. Please remove them!", { type: "warning" });
            }

        } else {
            console.error("Error from server:", res.message);
            showToast(res.message || "Failed to load data.", { type: "error" });
        }

    } catch (err) {
        console.error("Failed to load data", err);
        showToast("Something went wrong while loading the form.", { type: "error" });
    }
}

//==========Item Wise History==========
function getItemWisePurchaseRequest(itemCode) {

    $.ajax({
        url: '/PurchaseRequest/GetItemWisePurchaseRequest',
        type: 'GET',
        data: { itemCode: itemCode },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenPurchaseRequestGrid(response.data);

                $("#lastTenPurchaseRequestModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error while fetching data." + error, { type: "error" });
        }
    });
}
function getItemWiseConsumptionHistory(itemCode) {

    $.ajax({
        url: '/PurchaseRequest/GetItemWiseConsumptionHistory',
        type: 'GET',
        data: { itemCode: itemCode },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenConsumptionGrid(response.data);

                $("#lastTenConsumptionModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error while fetching data." + error, { type: "error" });
        }
    });
}
function getItemWisePurchaseOrder(itemCode) {

    $.ajax({
        url: '/PurchaseRequest/GetItemWisePurchaseOrderHistory',
        type: 'GET',
        data: { itemCode: itemCode },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenOrderGrid(response.data);

                $("#lastTenOrderHistoryModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error while fetching data." + error, { type: "error" });
        }
    });
}
function getItemWisePurchaseQuotation(itemCode) {

    $.ajax({
        url: '/PurchaseRequest/GetItemWisePurchaseQuotationHistory',
        type: 'GET',
        data: { itemCode: itemCode },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindPurchaseQuotationHistoryGrid(response.data);

                $("#purchaseQuotationHistoryModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error while fetching quotation history." + error, { type: "error" });
        }
    });
}
function bindPurchaseQuotationHistoryGrid(data) {

    var tbody = $("#tblPurchaseQuotationHistory tbody");
    tbody.empty();
    if (!data || data.length === 0) {

        tbody.append(`
            <tr>
                <td colspan="19" class="text-center">
                    No Records Found
                </td>
            </tr>
        `);

        return;
    }
    $.each(data, function (i, item) {

        var row = `<tr>
                    <td>${item.itemCode}</td>
                    <td>${item.vNo}</td>
                    <td>${item.date}</td>
                    <td>${item.supplier}</td>
                    <td>${item.itemName}</td>
                    <td>${item.make}</td>
                    <td>${item.unit}</td>
                    <td>${item.groupNo}</td>
                    <td>${parseFloat(item.qty || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.rate || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.freight || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.cgstPer || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.sgstPer || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.igstPer || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.packPer || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.discPer || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.othExps || 0).toFixed(4)}</td>
                    <td>${parseFloat(item.ldRate || 0).toFixed(4)}</td>
                    <td>${item.remarks}</td>
                    <td>${item.status}</td>
                  </tr>`;

        tbody.append(row);
    });
}
function getItemWisePurchaseReceiptHistory(itemCode) {

    $.ajax({
        url: '/PurchaseRequest/GetItemWisePurchaseReceiptHistory',
        type: 'GET',
        data: { itemCode: itemCode },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenPurchaseHistoryGrid(response.data);

                $("#lastTenPurchaseHistoryLabel").text("Purchase Receipt History");

                $("#lastTenPurchaseHistoryModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error while fetching material receipt history." + error, { type: "error" });
        }
    });
}
function getItemWisePurchaseHistory(itemCode) {

    $.ajax({
        url: '/PurchaseRequest/GetItemWisePurchaseHistory',
        type: 'GET',
        data: { itemCode: itemCode },
        dataType: 'json',
        success: function (response) {

            if (response.success) {

                bindLastTenPurchaseHistoryGrid(response.data);

                $("#lastTenPurchaseHistoryLabel").text("Purchase History");

                $("#lastTenPurchaseHistoryModal").modal("show");
            }
            else {
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr, status, error) {
            showToast("Error while fetching purchase invoice history." + error, { type: "error" });
        }
    });
}
//======================Copy From================
function LoadCopyForm() {
    return $.ajax({
        url: '/PurchaseRequestList/GetDataCopyForm',
        method: 'GET',
        data: {},
        success: function (res) {
            if (res.success) {

                $('#tblpurchasequotationmodal tbody').empty();
                res.data.forEach(function (item) {
                    var row = '<tr>';
                    row += '<td><input type="checkbox" class="copyRowCheckbox" /></td>';
                    row += '<td>' + item.v_NO + '</td>';
                    row += '<td>' + item.v_TYPE + '</td>';
                    row += '<td>' + formatDate(item.v_DATE) + '</td>';
                    row += '<td>' + item.iteM_CODE + '</td>';
                    row += '<td>' + item.itemName + '</td>';
                    row += '<td>' + item.make + '</td>';
                    row += '<td>' + item.techDesc + '</td>';
                    row += '<td>' + item.unit + '</td>';
                    row += '<td>' + item.qty + '</td>';
                    row += '<td>' + item.makeCode + '</td>';
                    row += '<td>' + item.uCode + '</td>';
                    row += '<td>' + item.taxCode + '</td>';
                    row += '<td style="display:none;">Action</td>';
                    row += '</tr>';

                    $('#tblpurchasequotationmodal tbody').append(row);
                });


            } else {
                console.error("Error from server:", res.message);
                showToast(res.message || "Failed to load data.", { type: "error" });
            }
        },
        error: function (err) {
            console.error("Failed to load data", err);
            showToast("Something went wrong while loading the form.", { type: "error" });
        }
    });
}
function getSelectedRows() {
    var selectedRowsData = [];
    $('#tblpurchasequotationmodal tbody tr').each(function () {
        var checkbox = $(this).find('input[type="checkbox"]');
        if (checkbox.prop('checked')) {
            var rowData = {
                vno: parseInt($(this).find('td').eq(1).text()) || 0,
                vtype: $(this).find('td').eq(2).text(),
                vdate: $(this).find('td').eq(3).text(),
                iteM_CODE: parseInt($(this).find('td').eq(4).text()) || 0,
                tecH_DESC: $(this).find('td').eq(7).text(),
                uniT_NAME: $(this).find('td').eq(8).text(),
                useR_QTY: $(this).find('td').eq(9).text(),
                reQ_QTY: $(this).find('td').eq(9).text(),
                makE_CODE: $(this).find('td').eq(10).text(),
                uniT_CODE: $(this).find('td').eq(11).text(),
            };

            selectedRowsData.push(rowData);
        }
    });

    if (selectedRowsData.length > 0) {
        addRowsToMainTable(selectedRowsData);
        showToast(selectedRowsData.length + " row(s) copied successfully.", { type: "success" });
    } else {
        showToast("No rows selected.", { type: "warning" });
    }
}

async function addRowsToMainTable(selectedRowsData) {

    if ($tbody.find('tr').length === 1) {
        const firstRowInputs = $tbody.find('tr:first input, tr:first select');
        let isBlank = true;

        firstRowInputs.each(function () {
            //if ($(this).val().trim() !== '') {
            //    isBlank = false;
            //    return false;
            //}
            const val = $.trim($(this).val() || '');

            if (val !== '' && val !== '0') {
                isBlank = false;
                return false;
            }
        });

        if (isBlank) {
            $tbody.find('tr:first').remove();
        }
    }

    isCopyFromOrMonthly = true;

    for (const row of selectedRowsData) {

        const isMonthly = row.iteM_CODE && row.iteM_CODE > 0
            ? await CheckMonthlyReq(row.iteM_CODE)
            : false;

        await addRow(row);
        ApprovalFields();

        calculateTotalAmount();

        if (isMonthly) {

            const $lastRow = $tbody.find('tr:last');
            const $itemInput = $lastRow.find('.ddlItem');

            setInvalid(
                $itemInput,
                'This Item is a Monthly Requirement Item, So You Can not Make a Requisition'
            );

            // 🔥 IMPORTANT: wait for dropdown to be ready
            setTimeout(() => {
                $itemInput.trigger('focus');
            }, 50);

            return;
        }
    }
}

//===========Approx Total Amt Calculation==========
function calculateTotalAmount() {
    let totalAmt = 0;
    $tbody.find('tr').each(function () {
        const approxRate = parseFloat($(this).find('.approx-rate').val()) || 0;
        const reqQty = parseFloat($(this).find('.required-qty').val()) || 0;
        totalAmt += approxRate * reqQty;
    });
    $('#lbl-totalApproxAmount').text(
        totalAmt.toFixed(2)
    );
}

//============Toggle Three Dot Menu===========
$(document).on("click", ".erppage-dropdownaction-btn", function (e) {
    e.stopPropagation();

    $(".erppage-dropdownaction-menu").remove();

    const $btn = $(this);
    const offset = $btn.offset();

    const menuWidth = 150;
    const windowWidth = $(window).width();

    let leftPos = offset.left;

    if (offset.left + menuWidth > windowWidth) {
        leftPos = offset.left - menuWidth + $btn.outerWidth();
    }

    const itemCode = $btn.closest('tr').find('.ddlItem').val();

    // Show Purchase Order option only for approval users
    const purchaseOrderMenu =
        (IsApprovalBody || userLevel === 1)
            ? `<a href="#" id="btn-itemWise-PurchaseOrder"><i class="fa fa-history"></i> Purchase Order History</a>
               <a href="#" id="btn-itemWise-PurchaseQuotation"><i class="fa fa-history"></i> Purchase Quotation History</a>
               <a href="#" id="btn-itemWise-PurchaseReceiptHistory"><i class="fa fa-history"></i> Purchase Receipt History</a>
               <a href="#" id="btn-itemWise-PurchaseHistory"><i class="fa fa-history"></i> Purchase History</a>`
            : '';

    const dropdown = $(`
        <div class="erppage-dropdownaction-menu" data-itemcode="${itemCode}">
            <a href="#" id="btn-itemWise-PurchaseRequest"><i class="fa fa-history"></i> Purchase Request History</a>
            <a href="#" id="btn-itemWise-ConsumptionHistory"><i class="fa fa-history"></i> Consumption History</a>
            ${purchaseOrderMenu}
        </div>
    `);

    $("body").append(dropdown);

    dropdown.css({
        top: offset.top + $btn.outerHeight(),
        left: leftPos
    });
});

// Close dropdown when clicking outside
$(document).on("click", function () {
    $(".erppage-dropdownaction-menu").remove();
});

//========Helper========
function SetMaxlength(selector, numeric, decimal) {
    console.log(selector);

    let value = $(selector).val();

    let regex = new RegExp(`^\\d{0,${numeric}}(\\.\\d{0,${decimal}})?$`);

    if (!regex.test(value)) {
        $(selector).val(value.slice(0, -1));
    }
}

//====================Print Report===========
async function PrintRequest() {

    let items = [];

    $('#tblItemDetailsPR tbody tr').each(function () {
        let itemCode = $(this).find('.ddlItem').val();

        if (itemCode) {
            items.push({
                ItemCode: parseInt(itemCode)
            });
        }
    });

    let model = {
        VNo: $('#NumDocNo').val(),
        Items: items
    };

    //$.ajax({
    //    url: '/PurchaseRequest/PrintRequest',
    //    type: 'POST',
    //    contentType: 'application/json',
    //    data: JSON.stringify(model),
    //    success: function (res) {
    //        if (res.success) {
    //            alert("Saved Successfully");
    //            console.log(res.approvedBy);
    //        } else {
    //            alert(res.message);
    //        }
    //    }
    //});
    try {
        const res = await $.ajax({
            url: '/PurchaseRequest/PrintRequest',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(model)
        });

        if (res.success) {
            return res.approvedBy;
        }

        throw new Error(res.message);

    } catch (err) {
        console.error(err);
        return null;
    }
}

async function PRReport() {

    var reportName = "storereqslip";
    var vNo = $('#NumDocNo').val();
    var approvedBy = await PrintRequest();
    console.log(approvedBy);
    // Crystal Report Formula
    var formula =
        "{prequest1.COMP_CODE} = " + window.globalVariables.compCode +
        " and {prequest1.BRANCH_CODE} = " + window.globalVariables.branchCode +
        " and {prequest1.YEAR_CODE} = " + window.globalVariables.yearCode +
        " and {prequest1.V_TYPE} = 'STPI' " +
        " and {prequest1.V_NO} = " + vNo;

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: window.database.db,
        Parameters: {
            comp_name: window.globalVariables.companyName,
            comp_add1: window.globalVariables.add1,
            comp_add2: window.globalVariables.add2,
            RPTNAME: "Requistion Slip",
            approvedBy: approvedBy
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
        url: 'http://localhost:34088/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(formulaFields),
        contentType: "application/json",
        xhrFields: {
            responseType: 'blob'
        },
        success: function (response) {
            console.log('PDF response:', response);
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

//===================Approval===================
$(document).on('click', '#btn_Sendapproval', function () {
    var FromName = window.location.pathname.split('/')[1];
    $.ajax({
        url: '/Approval/CheckPendingUser',
        type: 'POST',
        data: {
            vNo: $('#NumDocNo').val(),
            vType: vType
        },
        success: function (response) {
            console.log('Response:', response);
            // Pending with another user
            if (response.success === false) {
                showToast(`Pending With Another User : ${response.fullName} (${response.userCode})`,
                    { type: "warning" });
                return;
            }
            if (!SendApproveValidation()) return;
            if (!CheckSaved()) return;
            // Approval_Code = 5
            if (response.approvalCode8 === true) {
                OpenApprovalModal({
                    DocType: vType,
                    DocNo: $('#NumDocNo').val(),
                    TableName: DBTableName
                });
                return;
            }
            // Approval_Code != 8
            OpenSendForApprovalModal({
                DocType: vType,
                DocNo: $('#NumDocNo').val(),
                UserCode: null,
                UserName: null,
                DocDate: null,
                TableName: DBTableName,
                FromName, FromName
            });

        },
        error: function (xhr, status, error) {
            console.log(error);
            alert('Error while checking approval status.');
        }
    });

});
$(document).on('click', '#btn_Approved', function () {
    if (!SendApproveValidation()) return;
    if (!CheckSaved()) return;
    OpenApprovalModal({
        DocType: vType,
        DocNo: $('#NumDocNo').val(),
        TableName: DBTableName
    });
});

function SendApproveValidation() {
    if (IsApprovalBody) {
        let isValid = true;

        $('#tblItemDetailsPR tbody tr').each(function () {

            const $row = $(this);

            const itemCode = $row.find('.ddlItem').val();
            const itemName = $row.find('.ddlItem option:selected').text();

            const approxRate = $row.find('.approx-rate').val().trim();
            const approvalStatus = $row.find('.approvalStatus').val();

            if (itemCode && !approvalStatus) {

                setInvalid($row.find('.approvalStatus'), `Please select Approval Status of Item => ${itemName}`);
                isValid = false;
                return false;   // break each loop
            }

            if (itemCode && approxRate === "") {

                setInvalid($row.find('.approx-rate'), `Approx. Rate required for ${itemName}.`);
                isValid = false;
                return false;
            }

        });
        return isValid;
    }
    else {
        return true;
    }
}
function CheckSaved() {

    if (!isSaved) {
        showToast("Please save the record before sending for approval.", {
            type: "warning"
        });
        return false;
    }

    return true;
}
