const urlParams = new URLSearchParams(window.location.search);
const saudaNo = urlParams.get('saudaNo');
const vNo = urlParams.get('id');
const vType = urlParams.get('vtype');
const isReadOnly = urlParams.get('readOnly') === "true";

let existingVNo = null;
let isLoadingEntry = false;
let transportOptions = [];
let ExpSupplierOptions = [];
let ExpTypeOptions = [];
let skipDocChecks = false;
let compCode = "";
// ============= INITIALIZE ALL ATTACHMENTS =============

const attachments = [
    {
        type: 'PI', fileInput: "FileSupplierPI", chip: "chipPI", fileName: "TxtPIFileName", viewBtn: "BtnViewPI", removeBtn: "BtnRemovePI",
        requiredField: "TxtSupplierPINo"
    },
    {
        type: 'INV', fileInput: "FileSupplierInv", chip: "chipInv", fileName: "TxtInvFileName", viewBtn: "BtnViewInv", removeBtn: "BtnRemoveInv",
        requiredField: "TxtSupplierInvNo"
    },
    { type: 'BL', fileInput: "FileBL", chip: "chipBL", fileName: "TxtBLFileName", viewBtn: "BtnViewBL", removeBtn: "BtnRemoveBL", requiredField: "TxtBLNo" },
    { type: 'BE', fileInput: "FileBE", chip: "chipBE", fileName: "TxtBEFileName", viewBtn: "BtnViewBE", removeBtn: "BtnRemoveBE", requiredField: "TxtBENo" },
    {
        type: 'DP', fileInput: "FilePaymentRef", chip: "chipPaymentRef", fileName: "TxtPaymentRefFileName", viewBtn: "BtnViewPaymentRef",
        removeBtn: "BtnRemovePaymentRef", requiredField: "TxtPaymentRefNo"
    },
    { type: 'LC', fileInput: "FileLC", chip: "chipLC", fileName: "TxtLCFileName", viewBtn: "BtnViewLC", removeBtn: "BtnRemoveLC", requiredField: "TxtLCNo"},
    {
        type: 'SBLC', fileInput: "FileSBLC", chip: "chipSBLC", fileName: "TxtSBLCFileName", viewBtn: "BtnViewSBLC", removeBtn: "BtnRemoveSBLC",
        requiredField: "TxtSBLCNo"
    },

    // Other Attachment
    { type: 'Pic1', fileInput: "FileOtherAttachment1", nameBox: "TxtOtherAttachment1" },
    { type: 'Pic2', fileInput: "FileOtherAttachment2", nameBox: "TxtOtherAttachment2" }
];

const fileState = {};          
const MAX_FILE_SIZE = 307200;    // 300 KB
const ALLOWED_EXT = ['pdf', 'jpg', 'jpeg', 'png', 'bmp', 'xls', 'xlsx', 'doc', 'docx'];

$(document).ready(async function () {
    await wireEvents();
    attachments.forEach(config => initAttachment(config));
    if (saudaNo != null && saudaNo.trim() !== '') {
        await checkEntryBySauda(saudaNo, 'EXIM');
    }

    await getGlobalValues();

    await BindDropdowns();
    addContainerRow();
    addExpenseRow();
    applyPaymentMode();

    const currentDate = getCurrentDateYMD();
    $("#DtDocDate, #DtSaudaDate").val(currentDate);

    if (vNo && vType) {
        existingVNo = vNo;
        $('#NumDocNo').val(vNo);
        await loadEntry(vType, vNo);
    } else if (saudaNo != null && saudaNo.trim() !== '') {
        if (existingVNo) {
            $('#NumDocNo').val(existingVNo);
            await loadEntry('EXIM', existingVNo);
        } else {
            setDropdown('#ddlSaudaNo', saudaNo);
        }
    }

});

//=============CHECK EXISTING ENTRY=================
async function checkEntryBySauda(saudaNo, vType) {
    try {
        const res = await fetch(`/ImportTrackingEntry/GetEntryBySauda?saudaNo=${encodeURIComponent(saudaNo)}&vType=${encodeURIComponent(vType)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.success) throw new Error(data.message);
        existingVNo = data.exists ? data.vNo : null;
    }
    catch (e) {
        showToast('Error checking existing entry: ' + e.message, { type: "warning" });
    }
}


//=============DROPDOWNS=================
async function BindDropdowns() {
    await Promise.all([
        bindDropdown("ImportTrackingEntry", "doctype", '#ddlDocType', '', 'EXIM', null, true, null, false),
        bindDropdown("ImportTrackingEntry", "supplier", '#ddlSupplier', 'Select Supplier', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "agent", '#ddlAgent', 'Select Agent', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "item", '#ddlItemName', 'Select Item', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "delivery", '#ddlDeliveryAt', 'Select Delivery At', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "payterm", '#ddlPaymentTerms', 'Select Payment Term', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "paybank", '#ddlPaymentBank', 'Select Payment Bank', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "country", '#ddlCountryOfOrigin', 'Select Country', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "port", '#ddlPOL', 'Select POL', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "port", '#ddlPOD', 'Select POD', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "currency", '#ddlCurrency', 'Select Currency', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "cha", '#ddlCHA', 'Select CHA', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "cha", '#ddlShippingLine', 'Select Shipping Line', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "bank", '#ddlBankName', 'Select Bank', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "supplier", '#ddlLCBeneficiary', 'Select LC Beneficiary', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "supplier", '#ddlLCBeneficiary', 'Select LC Beneficiary', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "bank", '#ddlLCIssueBank', 'Select LC Issue Bank', null, null, false, null, true),
        bindDropdown("ImportTrackingEntry", "sauda", '#ddlSaudaNo', 'Select Sauda No', null, null, false, null, true),
        loadTransportOptions(),
        loadExpenseSupplierOptions(),
        loadExpenseTypeOptions()
    ]);
    $('#ddlDocType').trigger('change');
}
function setDropdown(selector, value) {
    $(selector).val(value).trigger('change');
}
async function loadTransportOptions() {
    const res = await $.ajax({
        url: '/ImportTrackingEntry/GetDropdown',
        type: 'GET',
        dataType: 'json',
        data: { type: 'transport' }
    });
    transportOptions = res || [];
}
async function loadExpenseSupplierOptions() {
    const res = await $.ajax({
        url: '/ImportTrackingEntry/GetDropdown',
        type: 'GET',
        dataType: 'json',
        data: { type: 'expsupplier' }
    });
    ExpSupplierOptions = res || [];
}
async function loadExpenseTypeOptions() {
    const res = await $.ajax({
        url: '/ImportTrackingEntry/GetDropdown',
        type: 'GET',
        dataType: 'json',
        data: { type: 'expensetype' }
    });
    ExpTypeOptions = res || [];
}
function bindOptions($select, list, placeholder) {
    $select.empty().append(`<option value="">${placeholder}</option>`);
    list.forEach(o => $select.append($('<option>').val(o.value).text(o.text)));
}
function initSelect2($ddl) {
    if (!$ddl.length) return;

    // Already initialized
    if ($ddl.hasClass('select2-hidden-accessible')) return;

    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true
    });

    $ddl.on('select2:open', function () {
        setTimeout(function () {
            const searchBox = document.querySelector('.select2-container--open .select2-search__field');
            if (searchBox) searchBox.focus();
        }, 0);
    });
}


//=============GENERATE VNO=================
async function GetVNo(vtype) {
    try {
        const res = await fetch(`/ImportTrackingEntry/GetVNo/?vType=${encodeURIComponent(vtype)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}


//=============EVENTS=================
async function wireEvents() {
    //----------DocType Change--------
    $("#ddlDocType").on('change', async function () {
        var vType = $(this).val();
        if (!vType || existingVNo || isLoadingEntry || vNo) return;

        try {
            await GetVNo(vType);
        } catch (err) {
            console.error("Error loading Document No:", err);
        }
    });

    //----------Sauda No Change--------
    $(document).on('change', '#ddlSaudaNo', function () {
        if (isLoadingEntry) return;
        loadSaudaDetails($(this).val());
    });

    //---------- Delete Expense Row Button Click -----------
    $('#tblImportExpenses').on('click', '.del-expense-row', function () {
        const $tbody = $('#tblImportExpenses tbody');

        if ($tbody.find('tr').length === 1) return;

        const $row = $(this).closest('tr');
        $row.remove();

        const $lastRow = $tbody.find('tr:last');

        if ($lastRow.length) {
            if ($lastRow.find('.add-expense-row').length === 0) {
                $lastRow.find('.action-wrap').append(`
                <button type="button" class="act-btn add btn-add-action add-expense-row" title="Add Row">
                    <i class="fa fa-plus-circle"></i>
                </button>
            `);
            }
        }
    });

    //---------- Add Expense Row Button Click -----------
    $('#tblImportExpenses').on('click', '.add-expense-row', async function () {
        //const currentSelect = $(this).closest('tr').find('.item-name')[0];
        ////Duplicate
        //if (checkDuplicateItems(currentSelect)) return;

        await addExpenseRow();
    });

    ///---------- Delete Container Row Button Click -----------
    $('#tblContainerDetails').on('click', '.del-container-row', function () {
        const $tbody = $('#tblContainerDetails tbody');

        if ($tbody.find('tr').length === 1) return;

        const $row = $(this).closest('tr');
        $row.remove();

        const $lastRow = $tbody.find('tr:last');

        if ($lastRow.length) {
            if ($lastRow.find('.add-container-row').length === 0) {
                $lastRow.find('.action-wrap').append(`
                <button type="button" class="act-btn add btn-add-action add-container-row" title="Add Row">
                    <i class="fa fa-plus-circle"></i>
                </button>
            `);
            }
        }
    });

    //---------- Add Container Row Button Click -----------
    $('#tblContainerDetails').on('click', '.add-container-row', async function () {
        //const currentSelect = $(this).closest('tr').find('.item-name')[0];
        ////Duplicate
        //if (checkDuplicateItems(currentSelect)) return;

        await addContainerRow();
    });

    //---------- Save button click -----------
    $('#btn-save').on('click', async function (e) {
        e.preventDefault();
        await saveEntry();
    });

    //---------- Payment Type Radio Button -----------
    $('input[name=RdoPaymentMode]').on('change', applyPaymentMode);

    //---------- INR value -----------
    $('#NumSupInvValue, #NumExchRate').on('blur', calcInvValueINR);
    $('#ddlCurrency').on('change', function () {
        if (isLoadingEntry) return;
        calcInvValueINR();
    });
}


//=============SAUDA DETAILS=================
function loadSaudaDetails(saudaNo) {
    if (!saudaNo) return;

    $.ajax({
        url: '/ImportTrackingEntry/GetSaudaDetails',
        type: 'GET',
        dataType: 'json',
        data: { saudaNo: saudaNo },
        success: function (res) {
            console.log(res);
            if (res && res.success) {
                fillSaudaDetails(res.data);
            }
        },
        error: function () {
            console.error('Failed to load Sauda details.');
        }
    });
}
function fillSaudaDetails(data) {
    console.log(data);
    $('#DtSaudaDate').val(data.vDate?.split('T')[0] ?? '');
    setDropdown('#ddlSupplier', data.partyCode);
    setDropdown('#ddlItemName', data.itemCode);
    setDropdown('#ddlPaymentTerms', data.payTermCode);
    
    $('#ddlCurrency option').filter(function () {
        return $(this).text() === data.currency;
    }).prop('selected', true).trigger('change');
}


//=============Fill Details By VNo===============
async function loadEntry(vType, vNo) {
    try {
        const res = await $.ajax({
            url: '/ImportTrackingEntry/GetEntry',
            type: 'GET',
            dataType: 'json',
            data: { vType: vType, vNo: vNo }
        });

        if (!res || !res.success) {
            showToast(res?.message || 'Unable to load entry.', { type: 'warning' });
            return;
        }

        isLoadingEntry = true;
        try {
            fillEntry(res);   
        } finally {
            isLoadingEntry = false;
            if (isReadOnly) {
                setFormReadonly();
            }
        }
    }
    catch (e) {
        console.error('Failed to load entry.', e);
        showToast('Error loading entry.', { type: 'warning' });
    }
}
const dt = v => v?.split('T')[0] ?? '';
function setByText(selector, text) {
    const t = $.trim(text);
    const $opt = $(selector + ' option').filter(function () {
        return $.trim($(this).text()) === t;
    }).first();
    $(selector).val($opt.length ? $opt.val() : '').trigger('change');
}
function fillEntry(res) {
    const d = res.data;

    console.log("Edit Data: ", res.data)

    existingVNo = d.V_NO;   // stops the doc type change from generating a new number

    // ---- Basic information ----
    setDropdown('#ddlDocType', d.V_TYPE);
    $('#NumDocNo').val(d.V_NO);
    $('#DtDocDate').val(dt(d.V_DATE));
    setDropdown('#ddlSaudaNo', d.SAUDA_TYPE + d.SAUDA_NO);
    $('#DtSaudaDate').val(dt(d.SAUDA_DATE));
    setDropdown('#ddlSupplier', d.SUPPLIER);
    setDropdown('#ddlAgent', d.AGENT);
    setDropdown('#ddlItemName', d.ITEM_CODE);
    $('#TxtSizeDesp').val(d.SIZE);
    setByText('#ddlDeliveryAt', d.DEL_LOCATION);
    setDropdown('#ddlPaymentTerms', d.PAYMENT_TERMS);
    setDropdown('#ddlPaymentBank', d.PAYMENT_BANK);

    // ---- Invoice related ----
    $('#TxtSupplierOfferNo').val(d.OFFER_NO);
    $('#TxtSupplierPINo').val(d.PI_NO);
    $('#DtPIDate').val(dt(d.PI_DATE));
    $('#TxtSupplierInvNo').val(d.SUPPLIER_INVNO);
    $('#DtInvDate').val(dt(d.SUPPLIER_INVDATE));
    $('#NumSupInvValue').val(d.SUPPLIER_INVAMT);
    setByText('#ddlCurrency', d.CURRENCY);
    $('#NumExchRate').val(d.EXCH_RATE);
    $('#NumSupInvValueINR').val(d.INV_AMTINR);
    $('#DtDocRecdVerify').val(dt(d.SCANDOC_VERIFYDATE));
    $('#DtDocHOToBank').val(dt(d.DOC_HANDOVERDTBANK));
    $('#DtDocHOToCHA').val(dt(d.CHAHANDOVER_DATE));
    setDropdown('#ddlCHA', d.CHA_NAME);

    // ---- BL movement ----
    setDropdown('#ddlShippingLine', d.SHIPLINE_NAME);
    $('#TxtBLNo').val(d.BL_NO);
    $('#DtBLDate').val(dt(d.BL_DATE));
    $('#TxtVSLName').val(d.VSL_NAME);
    $('#NumFreeDays').val(d.FREE_DAYS);
    setDropdown('#ddlCountryOfOrigin', d.ORIGIN_COUNTRY);
    setDropdown('#ddlPOL', d.POL);
    setDropdown('#ddlPOD', d.POD);
    $('#DtETDDate').val(dt(d.ETD));
    $('#DtETADate').val(dt(d.ETA));
    $('#DtRailDate').val(dt(d.RAIL_DATE));
    $('#DtICDDate').val(dt(d.ICD_DATE));
    $('#DtMovDate').val(dt(d.MOVE_DATE));
    $('#DtTentContHORetDate').val(dt(d.TENCONT_HORETDATE));
    $('#DtMRNDate').val(dt(d.RECEIPT_DATE));
    $('#DtActualContHODate').val(dt(d.ACTUAL_CONTHODATE));

    // ---- Bills of entry ----
    $('#DtCustomOOCDate').val(dt(d.CUSTOM_OOCDATE));
    $('#TxtBENo').val(d.BE_NO);
    $('#DtBOESubmitDate').val(dt(d.BOE_SUBMITDATE));
    $('#DtBEDate').val(dt(d.BE_DATE));
    $('#DtChecklistApprovalDate').val(dt(d.CHKLIST_APPROVDATE));
    setDropdown('#ddlSampleCollected', d.SAMPLE_COLLECTED);
    $('#DtSampleTestReportDate').val(dt(d.SAMPLE_TESTREPORTDATE));
    $('#DtSampleCollDate').val(dt(d.SAMPLE_COLDATE));
    setDropdown('#ddlAssessmentType', d.ASSESMENT_TYPE);
    $('#DtAssDate').val(dt(d.ASSESMENT_DATE));
    $('#NumPassingRate').val(d.PASSING_RATE);
    setDropdown('#ddlUnderProtest', d.UNDER_PROTEST);
    $('#TxtAdvanceLicNo').val(d.ADVANCE_LICNO);

    // ---- Duty related ----
    $('#NumAccessValue').val(d.ACCESSIBLE_VALUE);
    $('#NumBCD').val(d.BCD);
    $('#NumSWS').val(d.SWS);
    $('#NumIGST').val(d.GST);
    $('#NumTotalDutyAmt').val(d.DUTY_AMT);
    $('#DtDutyPaymentDate').val(dt(d.DUTY_PAYDATE));
    setDropdown('#ddlDutyPaymentMode', d.DUTY_PAYMODE);

    // ---- Shipping line related ----
    $('#NumShippingLineCharges').val(d.HOLLAGE_CHARGES);
    $('#NumSecurityAmt').val(d.SECURITY_AMT);
    $('#DtSecDate').val(dt(d.SEC_DATE));
    $('#NumDetentionAmt').val(d.DETENTION_AMT);
    $('#DtDetnDate').val(dt(d.DETENTION_DATE));
    $('#TxtDetnReason').val(d.DETENTION_REASON);
    $('#TxtReasonAppBy').val(d.DETREASON_APPROVBY);
    $('#DtShipLinePayDate').val(dt(d.SHIPLINE_PAYDATE));
    $('#TxtCHAChargeDetails').val(d.CHA_CHARGEDETAIL);
    $('#NumIntRate').val(d.INT_RATE);
    setDropdown('#ddlCHAChargeType', d.CHA_CHARGETYPE);
    $('#NumCONCORExps').val(d.CONCOR_EXPS);
    $('#NumCHAAmt').val(d.CHA_AMT);
    setDropdown('#ddlCFSCharges', d.CFS_CHARGES);
    $('#DtCFSDStuffDate').val(dt(d.CFS_DSTUFFDATE));
    $('#NumCFSAmt').val(d.CFS_AMT);
    $('#DtCFSPayDate').val(dt(d.CFS_PAYDATE));

    // ---- Supplier payment ----
    $(d.PAY_TYPE === 'LC' ? '#RdoLC' : d.PAY_TYPE === 'SBLC' ? '#RdoSBLC' : '#RdoDirect')
        .prop('checked', true).trigger('change');
    setDropdown('#ddlBankName', d.BANK_NAME);
    $('#NumPaymentAmt').val(d.PAY_AMT);
    $('#DtPaymentDate').val(dt(d.PAY_DATE));
    $('#TxtPaymentRefNo').val(d.PAY_REFNO);

    // ---- LC payment ----
    $('#TxtLCNo').val(d.LC_NO);
    $('#DtLCDate').val(dt(d.LC_DATE));
    setDropdown('#ddlLCBeneficiary', d.LC_BENIFICIARY);
    setDropdown('#ddlLCIssueBank', d.LC_ISSUEBANK);
    $('#DtLCExpDate').val(dt(d.LC_EXPIRYDATE));
    $('#NumLCAmt').val(d.LC_AMT);
    $('#DtBLRecdDate').val(dt(d.BL_RECDDATE));
    $('#DtLCOrigDocReceiptBank').val(dt(d.LC_RCPTDATEBANK));
    $('#DtBLHandoverCHA').val(dt(d.BL_HANDOVERDATE));
    $('#DtAcceptedDocReceiptFactory').val(dt(d.LC_ACCPTDATEFACTORY));

    // ---- SBLC payment ----
    $('#TxtSBLCNo').val(d.SBLC_NO);
    $('#DtSBLCPayDate').val(dt(d.SBLC_PAYDATE));
    $('#DtSBLCApplDate').val(dt(d.SBLC_APPLDATE));
    $('#NumSBLCDiffDays').val(d.DIFF_DAYS);
    $('#DtSBLCDueDate').val(dt(d.SBLC_DUEDATE));
    $('#NumSBLCAvlAmt').val(d.SBLC_AVLAMT);
    $('#NumSBLCDisbAmt').val(d.SBLC_DISBAMT);
    $('#DtSBLCDisbDate').val(dt(d.SBLC_DISBDATE));
    $('#NumSBLCOthCh').val(d.OTH_CHARGES);
    $('#NumSBLCIntRate').val(d.SBLC_INTRATESOFR);

    // ---- Other information ----
    $('#NumRefundAmt').val(d.REFUND_AMT);
    $('#NumExpectedLoading').val(d.EXPECTED_LOADING);
    $('#NumQltyClaim').val(d.QLTY_CLAIMAMT);
    $('#DtTentHODate').val(dt(d.TENTATIVE_HODATE));
    $('#DtTentFactArrDate').val(dt(d.FACTORY_DATE));
    $('#DtETABEDate').val(dt(d.ETA_BEDATE));
    $('#DtRefdRecdDate').val(dt(d.REFUND_RECDATE));
    $('#NumQCAmt').val(d.QC_AMT);
    setDropdown('#ddlQCAmtRecd', d.QC_AMTRECD);
    $('#DtLatestShipDate').val(dt(d.LATEST_SHIPDATE));
    $('#DtIGMDate').val(dt(d.IGM_DATE));
    setDropdown('#ddlEnhanceRate', d.ENHANCE_RATE);
    $('#DtDODate').val(dt(d.DO_DATE));
    $('#DtDOValidDate').val(dt(d.DO_VALIDDATE));

    $('#TxtTestMemoNo').val(d.TEST_MEMONO);
    $('#DtTestMemoDate').val(dt(d.TEST_MEMODATE));

    fillContainers(res.containers);
    fillExpenses(res.expenses);
    fillFiles(res.files);
}


//=============GRID ROWS=================
function addContainerRow(c = {}) {
    // Remove delete button from the current last row
    const $previousLastRow = $("#tblContainerDetails tbody tr:last");
    $previousLastRow.find(".add-container-row").remove();

    const $tr = $(`<tr class="no-border-input">
        <td><input type="text" class="form-control c-no"></td>
        <td><input type="number" class="form-control c-wt"></td>
        <td><input type="number" class="form-control c-pkgs"></td>
        <td>
            <select class="form-control c-size">
                <option value="">Select Size</option>
                <option value="40 HQ">40 HQ</option>
                <option value="20 SD">20 SD</option>
                <option value="LCL">LCL</option>
            </select>
        </td>
        <td><select class="form-control c-transport"></select></td>
        <td><input type="number" class="form-control c-freight"></td>
        <td class="action-col">
            <div class="action-wrap">    
                <button type="button" class="act-btn delete btn-del-row del-container-row" title="Delete">
                    <i class="fa fa-trash"></i>
                </button>
                <button type="button" class="act-btn add add-container-row" title="Add Row">
                    <i class="fa fa-plus-circle"></i>
                </button>
            </div>
        </td>
    </tr>`);

    bindOptions($tr.find('.c-transport'), transportOptions, 'Select Transport');
    $tr.find('.c-transport').val(c.TRANSPORT_CODE ?? '');
    $tr.find('.c-size').val($.trim(c.CONT_SIZE));

    $tr.find('.c-no').val(c.CONTAINER_NO);
    $tr.find('.c-wt').val(c.GRS_WEIGHT);
    $tr.find('.c-pkgs').val(c.PKGS);
    $tr.find('.c-freight').val(c.FREIGHT);

    $('#tblContainerDetails tbody').append($tr);
    initSelect2($tr.find('.c-transport'));
}
function addExpenseRow(x = {}) {
    // Remove delete button from the current last row
    const $previousLastRow = $("#tblImportExpenses tbody tr:last");
    $previousLastRow.find(".add-expense-row").remove();

    const $tr = $(`<tr class="no-border-input">
        <td><select class="form-control x-type"></select></td>
        <td><select class="form-control x-party"></select></td>
        <td><input type="text" class="form-control x-invno"></td>
        <td><input type="date" class="form-control x-invdate"></td>
        <td><input type="number" class="form-control x-amt"></td>
        <td><input type="text" class="form-control x-remarks"></td>
        <td><input type="text" class="form-control x-reason"></td>
        <td><input type="text" class="form-control x-approved"></td>
        <td><input type="date" class="form-control x-paydate"></td>
        <td><input type="text" class="form-control x-payref"></td>
        <td class="action-col">
            <div class="action-wrap">  
                <button type="button" class="act-btn delete btn-del-row del-expense-row" title="Delete">
                    <i class="fa fa-trash"></i>
                </button>
                <button type="button" class="act-btn add btn-add-action add-expense-row" title="Add Row">
                    <i class="fa fa-plus-circle"></i>
                </button>
            </div>
        </td>
    </tr>`);

    bindOptions($tr.find('.x-party'), ExpSupplierOptions, 'Select Supplier');
    bindOptions($tr.find('.x-type'), ExpTypeOptions, 'Select Expense Type');
    $tr.find('.x-party').val(x.PARTY_CODE ?? '');
    $tr.find('.x-type').val(x.EXPS_TYPE ?? '');

    $tr.find('.x-invno').val(x.INV_NO);
    $tr.find('.x-invdate').val(dt(x.INV_DATE));
    $tr.find('.x-amt').val(x.INV_AMT);
    $tr.find('.x-remarks').val(x.REMARKS);
    $tr.find('.x-reason').val(x.REASON);
    $tr.find('.x-approved').val(x.APPROVED_BY);
    $tr.find('.x-paydate').val(dt(x.PAY_DATE));
    $tr.find('.x-payref').val(x.PAY_REFNO);

    $('#tblImportExpenses tbody').append($tr);
    initSelect2($tr.find('.x-party, .x-type'))
}
function fillContainers(list) {
    $('#tblContainerDetails tbody').empty();
    (list || []).forEach(c => addContainerRow(c));
    if (!list || list.length === 0) addContainerRow();
}
function fillExpenses(list) {
    $('#tblImportExpenses tbody').empty();
    (list || []).forEach(x => addExpenseRow(x));
    if (!list || list.length === 0) addExpenseRow();
}
function fillFiles(list) {
    Object.keys(fileState).forEach(k => delete fileState[k]);

    (list || []).forEach(f => {
        if (!f.IMG_FILE) return;
        fileState[f.FILE_TYPE] = { name: f.FILE_NAME, file: base64ToFile(f.IMG_FILE, f.FILE_NAME) };
    });

    attachments.forEach(renderAttachment);
}


//=============ATTACHMENTS=================
function hasAttachment(type) {
    return !!fileState[type];
}
function showAttachmentPreview(fileUrl, fileName = '') {
    const image = document.getElementById('previewImage');
    const pdf = document.getElementById('previewPdf');
    const modalElement = document.getElementById('imagePreviewModal');

    if (!image || !pdf || !modalElement || !fileUrl) return;

    // Reset previous preview
    image.src = '';
    image.style.display = 'none';
    pdf.src = '';
    pdf.style.display = 'none';

    const extension = fileName.split('.').pop().toLowerCase();

    if (extension === 'pdf') {
        pdf.src = fileUrl;
        pdf.style.display = 'block';
    } else if (['jpg', 'jpeg', 'png', 'gif', 'webp', 'bmp'].includes(extension)) {
        image.src = fileUrl;
        image.style.display = 'block';
    } else {
        window.open(fileUrl, '_blank');
        return;
    }

    bootstrap.Modal.getOrCreateInstance(modalElement).show();
}
function initAttachment(config) {
    const input = document.getElementById(config.fileInput);
    if (!input) { console.error("Attachment input not found:", config); return; }

    input.addEventListener('click', function (e) {
        if (!config.requiredField) return;

        const field = document.getElementById(config.requiredField);

        if (!field || !field.value.trim()) {
            e.preventDefault();
            showToast(`${config.type} number not entered.`, { type: 'warning' });
            field?.focus();
        }
    });

    const picker = input.closest('.erppage-filepicker');
    const viewBtn = config.viewBtn ? document.getElementById(config.viewBtn) : picker.querySelector('button[title="View"]');
    const removeBtn = config.removeBtn ? document.getElementById(config.removeBtn) : picker.querySelector('.erppage-filepicker-remove');

    input.setAttribute('accept', '.pdf,.jpg,.jpeg,.png,.bmp,.xls,.xlsx,.doc,.docx');

    input.addEventListener('change', function () {
        const file = this.files?.[0];
        if (!file) return;

        const ext = file.name.split('.').pop().toLowerCase();

        if (!ALLOWED_EXT.includes(ext)) {
            showToast('Only PDF, image and Office files are allowed.', { type: 'warning' });
            this.value = '';
            return;
        }

        if (file.size > MAX_FILE_SIZE) {
            showToast('Max. 300 KB File Size support.', { type: 'warning' });
            this.value = '';
            return;
        }

        const vType = $('#ddlDocType').val();
        const vNo = $('#NumDocNo').val();

        const prefix = `${compCode}_${vType}${vNo}_${config.type}_`;
        const fileName = file.name.startsWith(prefix)
            ? file.name
            : prefix + file.name;

        fileState[config.type] = {
            name: fileName,
            file: new File([file], fileName, { type: file.type })
        };

        this.value = '';
        renderAttachment(config);
    });

    viewBtn?.addEventListener('click', () => viewAttachment(config.type));

    removeBtn?.addEventListener('click', async function (e) {
        e.preventDefault();

        if (!fileState[config.type]) return;

        const result = await Swal.fire({
            title: 'Are you sure?',
            text: 'Do you want to remove this attachment?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, remove it!',
            cancelButtonText: 'No, keep it',
            confirmButtonColor: '#d33',
            cancelButtonColor: '#3085d6'
        });

        if (!result.isConfirmed) return;

        delete fileState[config.type];
        renderAttachment(config);

        Swal.fire({
            title: 'Removed!',
            text: 'Attachment has been removed.',
            icon: 'success',
            timer: 1500,
            showConfirmButton: false
        });
    });

    renderAttachment(config);
}
function renderAttachment(config) {
    const name = fileState[config.type]?.name || '';

    if (config.chip) {
        const chip = document.getElementById(config.chip);
        const label = document.getElementById(config.fileName);
        chip.hidden = !name;
        label.textContent = name;
        label.title = name;
    } else {
        document.getElementById(config.nameBox).value = name;
    }
}

function viewAttachment(type) {
    const s = fileState[type];
    if (!s) return;
    showAttachmentPreview(URL.createObjectURL(s.file), s.name);
}
function base64ToFile(base64, fileName) {
    const ext = fileName.split('.').pop().toLowerCase();
    const mime = {
        pdf: 'application/pdf', png: 'image/png', gif: 'image/gif', bmp: 'image/bmp',
        jpg: 'image/jpeg', jpeg: 'image/jpeg'
    }[ext] || 'application/octet-stream';

    const bytes = Uint8Array.from(atob(base64), c => c.charCodeAt(0));
    return new File([bytes], fileName, { type: mime });
}


//=============VALIDATION=================
async function loadValidationFlags() {
    try {
        const res = await $.ajax({
            url: '/ImportTrackingEntry/GetValidationFlags',
            type: 'GET',
            dataType: 'json'
        });
        skipDocChecks = !!res.skipDocChecks;
    } catch (e) {
        console.error(e);
    }
}
function invalid(tab, selector, message) {
    $(`.erppage-tab[data-tab="${tab}"]`).trigger('click');
    setInvalid($(selector), message);
    return false;
}
function validateData() {
    // Doc Type and Doc No
    if (!$('#ddlDocType').val())
        return invalid('generalinfo', '#ddlDocType', 'Doc Type is required.');
    if (!$.trim($('#NumDocNo').val()))
        return invalid('generalinfo', '#NumDocNo', 'Doc No is required.');

    // [tab, number box, date box, chip, label]
    const docs = [
        ['generalinfo', '#TxtSupplierInvNo', '#DtInvDate', 'INV', 'Supplier Invoice'],
        ['generalinfo', '#TxtSupplierPINo', '#DtPIDate', 'PI', 'PI'],
        ['generalinfo', '#TxtBENo', '#DtBEDate', 'BE', 'BE'],
        ['generalinfo', '#TxtBLNo', '#DtBLDate', 'BL', 'BL'],
        ['paymentinfo', '#TxtLCNo', '#DtLCDate', 'LC', 'LC']
    ];

    for (const [tab, noBox, dateBox, type, label] of docs) {
        if ($.trim($(noBox).val()) === '') continue;

        if (!$(dateBox).val() && !skipDocChecks)
            return invalid(tab, dateBox, `${label} date is required if ${label} No. is given.`);

        if (!hasAttachment(type) && !skipDocChecks)
            return invalid(tab, noBox, `${label} attachment is required if ${label} No. is given.`);
    }

    // At least one record in container grid
    const containerCount = $('#tblContainerDetails tbody .c-no')
        .filter(function () { return $.trim($(this).val()) !== ''; }).length;

    if (containerCount === 0)
        return invalid('generalinfo', '#tblContainerDetails tbody tr:first .c-no', 'No record for Container Detail in grid.');

    return true;
}


//=============SAVE & UPDATE=================
const sv = s => $.trim($(s).val());                                           // text
const nv = s => parseFloat($(s).val()) || 0;                                  // number
const dv = s => $(s).val() || null;                                           // date
const iv = s => parseInt($(s).val()) || 0;                                    // ddl code
const tv = s => $(s).val() ? $.trim($(s + ' option:selected').text()) : '';   // ddl text

function collectHeader() {
    // Sauda dropdown: value = SAUDA_TYPE + SAUDA_NO, text = SAUDA_NO
    const saudaVal = $('#ddlSaudaNo').val() || '';
    const saudaNo = $.trim($('#ddlSaudaNo option:selected').text());
    const saudaType = saudaVal ? saudaVal.substring(0, saudaVal.length - saudaNo.length) : '';

    return {
        // text
        SAUDA_TYPE: saudaType,
        ITEM_NAME: tv('#ddlItemName'),
        SIZE: sv('#TxtSizeDesp'),
        DEL_LOCATION: tv('#ddlDeliveryAt'),
        SUPPLIER_INVNO: sv('#TxtSupplierInvNo'),
        CURRENCY: tv('#ddlCurrency'),
        BL_NO: sv('#TxtBLNo'),
        CHA_CHARGEDETAIL: sv('#TxtCHAChargeDetails'),
        BE_NO: sv('#TxtBENo'),
        SAMPLE_COLLECTED: sv('#ddlSampleCollected'),
        ASSESMENT_TYPE: sv('#ddlAssessmentType'),
        DUTY_PAYMODE: sv('#ddlDutyPaymentMode'),
        ADVANCE_LICNO: sv('#TxtAdvanceLicNo'),
        SBLC_NO: sv('#TxtSBLCNo'),
        VSL_NAME: sv('#TxtVSLName'),
        POD: sv('#ddlPOD'), POL: sv('#ddlPOL'),
        DETENTION_REASON: sv('#TxtDetnReason'),
        DETREASON_APPROVBY: sv('#TxtReasonAppBy'),
        LC_NO: sv('#TxtLCNo'),
        ENHANCE_RATE: sv('#ddlEnhanceRate'),
        UNDER_PROTEST: tv('#ddlUnderProtest'),
        CHA_CHARGETYPE: sv('#ddlCHAChargeType'),
        CFS_CHARGES: sv('#ddlCFSCharges'),
        QC_AMTRECD: sv('#ddlQCAmtRecd'),
        PI_NO: sv('#TxtSupplierPINo'),
        OFFER_NO: sv('#TxtSupplierOfferNo'),
        PAY_TYPE: ($('input[name=RdoPaymentMode]:checked').val() || '').toUpperCase(),
        PAY_REFNO: sv('#TxtPaymentRefNo'),

        // codes
        SAUDA_NO: parseInt(saudaNo) || 0,
        ITEM_CODE: iv('#ddlItemName'),
        SUPPLIER: iv('#ddlSupplier'),
        AGENT: iv('#ddlAgent'),
        PAYMENT_TERMS: iv('#ddlPaymentTerms'),
        PAYMENT_BANK: iv('#ddlPaymentBank'),
        CHA_NAME: iv('#ddlCHA'),
        BANK_NAME: iv('#ddlBankName'),
        SHIPLINE_NAME: iv('#ddlShippingLine'),
        LC_BENIFICIARY: iv('#ddlLCBeneficiary'),
        LC_ISSUEBANK: iv('#ddlLCIssueBank'),
        ORIGIN_COUNTRY: iv('#ddlCountryOfOrigin'),

        // amounts / numbers
        EXPECTED_LOADING: nv('#NumExpectedLoading'),
        SUPPLIER_INVAMT: nv('#NumSupInvValue'),
        EXCH_RATE: nv('#NumExchRate'),
        INV_AMTINR: nv('#NumSupInvValueINR'),
        DUTY_AMT: nv('#NumTotalDutyAmt'),
        HOLLAGE_CHARGES: nv('#NumShippingLineCharges'),
        SECURITY_AMT: nv('#NumSecurityAmt'),
        REFUND_AMT: nv('#NumRefundAmt'),
        FREE_DAYS: nv('#NumFreeDays'),
        SBLC_AVLAMT: nv('#NumSBLCAvlAmt'),
        SBLC_DISBAMT: nv('#NumSBLCDisbAmt'),
        SBLC_INTRATESOFR: nv('#NumSBLCIntRate'),
        OTH_CHARGES: nv('#NumSBLCOthCh'),
        DIFF_DAYS: nv('#NumSBLCDiffDays'),
        INT_RATE: nv('#NumIntRate'),
        DETENTION_AMT: nv('#NumDetentionAmt'),
        LC_AMT: nv('#NumLCAmt'),
        QLTY_CLAIMAMT: nv('#NumQltyClaim'),
        QC_AMT: nv('#NumQCAmt'),
        PASSING_RATE: nv('#NumPassingRate'),
        CONCOR_EXPS: nv('#NumCONCORExps'),
        CHA_AMT: nv('#NumCHAAmt'),
        CFS_AMT: nv('#NumCFSAmt'),
        ACCESSIBLE_VALUE: nv('#NumAccessValue'),
        BCD: nv('#NumBCD'),
        SWS: nv('#NumSWS'),
        GST: nv('#NumIGST'),
        PAY_AMT: nv('#NumPaymentAmt'),

        // dates
        SAUDA_DATE: dv('#DtSaudaDate'),
        ETD: dv('#DtETDDate'),
        ETA: dv('#DtETADate'),
        SUPPLIER_INVDATE: dv('#DtInvDate'),
        SCANDOC_VERIFYDATE: dv('#DtDocRecdVerify'),
        CHAHANDOVER_DATE: dv('#DtDocHOToCHA'),
        BL_DATE: dv('#DtBLDate'),
        BL_RECDDATE: dv('#DtBLRecdDate'),
        BL_HANDOVERDATE: dv('#DtBLHandoverCHA'),
        CHKLIST_APPROVDATE: dv('#DtChecklistApprovalDate'),
        BE_DATE: dv('#DtBEDate'),
        ETA_BEDATE: dv('#DtETABEDate'),
        IGM_DATE: dv('#DtIGMDate'),
        SAMPLE_COLDATE: dv('#DtSampleCollDate'),
        SAMPLE_TESTREPORTDATE: dv('#DtSampleTestReportDate'),
        ASSESMENT_DATE: dv('#DtAssDate'),
        DUTY_PAYDATE: dv('#DtDutyPaymentDate'),
        DOC_HANDOVERDTBANK: dv('#DtDocHOToBank'),
        SHIPLINE_PAYDATE: dv('#DtShipLinePayDate'),
        DO_DATE: dv('#DtDODate'),
        RECEIPT_DATE: dv('#DtMRNDate'),
        TENTATIVE_HODATE: dv('#DtTentHODate'),
        SBLC_APPLDATE: dv('#DtSBLCApplDate'),
        SBLC_PAYDATE: dv('#DtSBLCPayDate'),
        SBLC_DUEDATE: dv('#DtSBLCDueDate'),
        SBLC_DISBDATE: dv('#DtSBLCDisbDate'),
        RAIL_DATE: dv('#DtRailDate'),
        ICD_DATE: dv('#DtICDDate'),
        DO_VALIDDATE: dv('#DtDOValidDate'),
        SEC_DATE: dv('#DtSecDate'),
        REFUND_RECDATE: dv('#DtRefdRecdDate'),
        DETENTION_DATE: dv('#DtDetnDate'),
        CUSTOM_OOCDATE: dv('#DtCustomOOCDate'),
        FACTORY_DATE: dv('#DtTentFactArrDate'),
        LC_DATE: dv('#DtLCDate'),
        LC_EXPIRYDATE: dv('#DtLCExpDate'),
        TENTATIVE_HORETDATE: null,
        MOVE_DATE: dv('#DtMovDate'),
        TENCONT_HORETDATE: dv('#DtTentContHORetDate'),
        LATEST_SHIPDATE: dv('#DtLatestShipDate'),
        BOE_SUBMITDATE: dv('#DtBOESubmitDate'),
        CFS_DSTUFFDATE: dv('#DtCFSDStuffDate'),
        CFS_PAYDATE: dv('#DtCFSPayDate'),
        ACTUAL_CONTHODATE: dv('#DtActualContHODate'),
        PI_DATE: dv('#DtPIDate'),
        LC_RCPTDATEBANK: dv('#DtLCOrigDocReceiptBank'),
        LC_ACCPTDATEFACTORY: dv('#DtAcceptedDocReceiptFactory'),
        PAY_DATE: dv('#DtPaymentDate'),

        TEST_MEMONO: sv('#TxtTestMemoNo'),
        TEST_MEMODATE: dv('#DtTestMemoDate')
    };
}
function collectContainers() {
    return $('#tblContainerDetails tbody tr').map(function () {
        const r = $(this);
        return {
            CONTAINER_NO: $.trim(r.find('.c-no').val()),
            GRS_WEIGHT: parseFloat(r.find('.c-wt').val()) || 0,
            PKGS: parseFloat(r.find('.c-pkgs').val()) || 0,
            CONT_SIZE: r.find('.c-size').val() || '',
            TRANSPORT_CODE: r.find('.c-transport').val() || 0,
            FREIGHT: r.find('.c-freight').val() || 0
        };
    }).get().filter(c => c.CONTAINER_NO !== '');
}
function collectExpenses() {
    return $('#tblImportExpenses tbody tr').map(function () {
        const r = $(this);
        return {
            EXPS_TYPE: r.find('.x-type').val() || '',
            PARTY_CODE: parseInt(r.find('.x-party').val()) || 0,
            INV_NO: $.trim(r.find('.x-invno').val()),
            INV_DATE: r.find('.x-invdate').val() || null,
            INV_AMT: parseFloat(r.find('.x-amt').val()) || 0,
            REMARKS: $.trim(r.find('.x-remarks').val()),
            REASON: $.trim(r.find('.x-reason').val()),
            APPROVED_BY: $.trim(r.find('.x-approved').val()),
            PAY_DATE: r.find('.x-paydate').val() || null,
            PAY_REFNO: $.trim(r.find('.x-payref').val())
        };
    }).get().filter(x => x.EXPS_TYPE !== '');
}
function collectAttachments() {
    const readBase64 = file => new Promise((resolve, reject) => {
        const r = new FileReader();
        r.onload = e => resolve(e.target.result.split(',')[1]);
        r.onerror = reject;
        r.readAsDataURL(file);
    });

    return Promise.all(
        Object.entries(fileState).map(async ([type, s]) => ({
            FILE_TYPE: type,
            FILE_NAME: s.name,
            FILE_DATA: await readBase64(s.file)
        }))
    );
}
async function saveEntry() {
    if (!validateData()) return;

    const files = await collectAttachments();

    const payload = {
        ACTION: (existingVNo && !isNaN(existingVNo) && existingVNo > 0) ? "UPDATE" : "INSERT",
        V_TYPE: $('#ddlDocType').val(),
        V_NO: parseInt($('#NumDocNo').val()) || 0,
        V_DATE: $('#DtDocDate').val(),
        header: collectHeader(),
        containers: collectContainers(),
        expenses: collectExpenses(),
        newFiles: files
    };

    console.log("PayLoad", payload);

    try {
        const res = await $.ajax({
            url: '/ImportTrackingEntry/Save',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(payload)
        });

        if (!res.success) {
            showToast(res.message, { type: 'warning' });
            return;
        }

        showToast(res.message, { type: 'success' });
    } catch (e) {
        console.error(e);
        showToast('Error while saving.', { type: 'error' });
    }
}


//=============Global Values===============
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/ImportTrackingEntry/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            compCode = d.compCode;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}


// ============= PAYMENT MODE (Direct / LC / SBLC) =============
function setEnabled(selector, enabled) {
    const $sec = $(selector);
    // normal controls
    $sec.find('input, select, textarea, button').prop('disabled', !enabled);
    // paperclip label
    $sec.find('label.erppage-filepicker-btn').toggleClass('disabled', !enabled);
}
function applyPaymentMode() {
    const mode = $('input[name=RdoPaymentMode]:checked').val();   // Direct / LC / SBLC

    setEnabled('#fsLCPayment', false);
    setEnabled('#fsSBLCPayment', false);

    $('#ddlBankName, #NumPaymentAmt, #DtPaymentDate').prop('disabled', true);

    if (mode === 'Direct') {
        $('#ddlBankName, #NumPaymentAmt, #DtPaymentDate').prop('disabled', false);
        $('#ddlBankName').focus();
    }
    else if (mode === 'LC') {
        setEnabled('#fsLCPayment', true);
        $('#TxtLCNo').focus();
    }
    else if (mode === 'SBLC') {
        setEnabled('#fsSBLCPayment', true);
        $('#TxtSBLCNo').focus();
    }

    // SBLC Diff. days always calculated
    $('#NumSBLCDiffDays').prop('disabled', true);
}


// ============= INVOICE VALUE (INR) =============
function calcInvValueINR() {
    const value = parseFloat($('#NumSupInvValue').val()) || 0;
    const rate = parseFloat($('#NumExchRate').val()) || 0;
    const currencyCode = parseInt($('#ddlCurrency').val()) || 0;

    // VB: currency code > 1 ho to value * rate, warna value jaisi hai
    const inr = currencyCode > 1 ? (value * rate).toFixed(2) : value;

    $('#NumSupInvValueINR').val(inr);
}


// ============= READONLY MODE =============
function setFormReadonly() {
    const form = $('#ImportTrackingForm');
    $('#btn-save').hide();
    form.addClass('erppage-readonly');

    $('#tblContainerDetails .del-container-row, #tblContainerDetails .add-container-row, #tblImportExpenses .del-expense-row, #tblImportExpenses .add-expense-row')
        .prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });

    // Disable Browse/Attach buttons only
    form.find('.erppage-filepicker-btn[for], .erppage-filepicker-remove, .erppage-file-icon.delete-icon')
        .prop('disabled', true)
        .addClass('disabled')
        .css({ 'pointer-events': 'none', 'cursor': 'not-allowed', 'opacity': '0.5' });

    // Prevent selecting files
    form.find('input[type="file"], input[type="radio"]').prop('disabled', true);
}