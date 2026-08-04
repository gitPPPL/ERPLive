let uploadedFiles = [];
let EPRUploadedFiles = [];

let changeItem = false;
let itemVsBillHSNCodeDiff = false;

let userLevel = "";
let compCode = "";
let pubDefPOInMRN = "";
let dataSource = "";

let EPRFlg = 0;
var EPRAttachmentList = [];

let isLoadForEdit = false;

const dropdownCache = {};
const dropdownPromiseCache = {};

let itemOptionsHtml = "";
let taxOptionsHtml = "";
let deptOptionsHtml = "";

const rowsData = [];

const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const rowIdVType = urlParams.get('vType');
const isReadOnly = urlParams.get('readOnly') === 'true';

$(document).ready(async function () {
    $('#ddlDocType').focus();
    getGlobalValues();
    toggleDate();

    // Initially disable file picker
    $("#erpFile").prop("disabled", true);
    $(".erppage-filepicker-btn").addClass("disabled");

    try {

        const currentDate = getCurrentDateYMD();
        $('#DtDocDate, #DtBillDate, #DtGRDate, #DtBillDateLD, #DtHoldDateCRDRNote, #DtBLDate, #Dtsysdate, #DtChDate, #DtPlDate, #DtHoldDate')
            .val(currentDate);
        await loadInitialDropdowns();
        wireEvents();
        loadCopyFromMenu();

        // Load once and cache HTML
        await Promise.all([
            loadItemList($("<select>")),
            loadTaxTypeList($("<select>")),
            loadDepartmentList($("<select>"))
        ]);

        if (!isNaN(rowId) && rowId > 0) {
            isLoadForEdit = true;
            await loadFullQuotationByVno(rowId, rowIdVType);
            setTimeout(() => {
                isLoadForEdit = false;
            }, 1000);
        } else {
            addNewRowBelow();
        }


    } catch (err) {
        console.error("Error during page initialization:", err);
        toastr.error("Failed to initialize page.");
    }

});

//=====================EVENTS=============================
function wireEvents() {
    bindHeaderEvents();
    bindAddressEvents();
    bindSaveEvents();
    bindAttachmentEvents();
    bindGridEvents();
    bindFreightEvents();
    bindTotalsEvents();
    bindTransportEvents();
    bindBankEvents();
    EPRAttachmentEvent();
    bindButtonsAndModalsEvent();
}

function bindHeaderEvents() {
    //------------ VType Change --------
    $('#ddlDocType').on('change', function () {
        if (isLoadForEdit) return;
        const vType = $(this).val();
        if (!rowId) {
            GetVNo(vType);
        }
        loadMRNList(vType);
        $('#TxtMRNNo1').val('');
        SetLatestDebitAccount(vType);
        loadCopyFromMenu(vType);
    });

    //--------------- MRN Change ------------
    $('#TxtMRNNo2').on('change', function () {
        if (isLoadForEdit) return;
        const mrnTypeNo = $(this).find(':selected').text().trim();
        const mrnType = $(this).find(':selected').data('vtype');
        if (!mrnTypeNo)
            return;
        $.ajax({
            url: "/PurchaseBillPassEntry/ValidateMRN",
            type: "POST",
            data: {
                mrnTypeNo: mrnTypeNo,
                vType: $("#ddlDocType").val(),
                vNo: $("#NumDocNo").val()
            },
            dataType: "json",
            success: function (response) {
                if (!response.success) {
                    showToast(response.message, { type: "warning" });
                    clearPurchaseBillFields();
                    $("#TxtMRNNo2").val("");
                    $("#TxtMRNNo1").val("");
                    calculateItemTotals();
                    $("#TxtMRNNo2").focus();
                    return;
                }
                $("#TxtMRNNo1").val(mrnType);
                LoadMRNData(mrnType, response.mrnNo);
            },
            error: function () {
                showToast("Error occurred.", { type: "error" });
            }
        });
    });

    //---------- Freight Credit Account --------
    $('#ddlFreightCreditAC').on('change', function () {
        if (isLoadForEdit) return;
        loadTranGSTByFrtCrAc($(this).val());
    });

    //---------- Bill To Gst Change --------
    $('#TxtGSTNo').on('change', async function () {
        const result = await validatePartyGst("BillTo", $("#ddlBillFrom").val(), $("#TxtGSTNo").val());
        if (result && !result.isValid) {
            showToast(result.message, { type: "warning" });
            $("#TxtGSTNo").focus();
        }
    })

    //---------- Ship To Gst Change --------
    $('#TxtGSTNoSF').on('change', async function () {
        const result = await validatePartyGst("ShipTo", $("#ddlShipFrom1").val(), $("#TxtGSTNoSF").val());

        if (result && !result.isValid) {
            showToast(result.message, { type: "warning" });
            $("#TxtGSTNoSF").focus();
        }
    })
}

function bindAddressEvents() {
    //---------- Ship List Change ------
    $('#ddlShipFrom1').on('change', function () {
        if (isLoadForEdit) return;
        const shipFromCode = $(this).val();
        loadAddList(shipFromCode, '#ddlShipFromAddress');
    });

    //--------- Bill From List Change --------
    $('#ddlBillFrom').on('change', function () {
        if (isLoadForEdit) return;
        const billFromCode = $(this).val();
        loadAddList(billFromCode, '#ddlBillFromAddress');
    });

    //---------- Ship Address Change -----------
    $('#ddlShipFromAddress').on('change', function () {
        if (isLoadForEdit) return;
        const selectedVal = $(this).val();
        const code = $('#ddlShipFrom1').val();
        $.ajax({
            url: '/PurchaseBillPassEntry/GetAddressByBillToParty',
            type: 'GET',
            data: {
                code: code,
                addressId: selectedVal
            },
            success: function (response) {
                const res = response.addressDetails;
                $('#TxtAdd1SF').val(res.add1);
                $('#TxtAdd2SF').val(res.add2);
                $('#TxtAdd3SF').val(res.add3);
                $('#TxtGSTNoSF').val(res.gstin);
                $('#TxtPincodeSF').val(res.pincode);
                loadCityList('#ddlCitySF', res.cityCode);
            },
            error: function (xhr, status, error) {
                toastr.error('Error loading ship address: ' + error);
            }
        });
    });

    //---------- Bill Address Change ----------
    $('#ddlBillFromAddress').on('change', function () {
        if (isLoadForEdit) return;
        const selectedVal = $(this).val();
        const code = $('#ddlBillFrom').val();
        $.ajax({
            url: '/PurchaseBillPassEntry/GetAddressByBillToParty',
            type: 'GET',
            data: {
                code: code,
                addressId: selectedVal
            },
            success: function (response) {
                const res = response.addressDetails;
                $('#TxtAdd1PD').val(res.add1);
                $('#TxtAdd2PD').val(res.add2);
                $('#TxtAdd3PD').val(res.add3);
                $('#TxtGSTNo').val(res.gstin);
                $('#NumPincodeBL').val(res.pincode);
                loadCityList('#ddlCityPD', res.cityCode);
            },
            error: function (xhr, status, error) {
                toastr.error('Error loading bill address: ' + error);
            }
        });
    });

    //--------- Ship City Change ----------
    $('#ddlCitySF').on('change', function () {
        if (isLoadForEdit) return;
        const cCode = parseInt($(this).val()) || 0;
        loadStateList('#ddlStateSF', cCode);
    });

    //-------- Bill City Change ----------
    $('#ddlCityPD').on('change', function () {
        if (isLoadForEdit) return;
        const cCode = parseInt($(this).val()) || 0;
        loadStateList('#ddlStatePD', cCode);
    });
}

function bindSaveEvents() {
    //--------- Save Click --------
    $('#btn-save').on('click', async function (e) {
        e.preventDefault();
        try {
            const isvalid = await Validate();
            if (!isvalid) {
                return;
            }
            await saveUpdateData();
        }
        catch (error) {
            console.error(error);
        }
    });
}

function bindAttachmentEvents() {
    ////======Delete  Event for Attachment =========
    $(document).on('click', '.erppageattachmentsectiondelete', function () {
        const $fileItem = $(this).closest('.erppageattachmentsectionfileitem');

        const fileName = $fileItem.find('.erppageattachmentsectionfilename').text().trim();

        const index = uploadedFiles.findIndex(item => item && item.FILE_NAME === fileName);

        if (index !== -1) {
            uploadedFiles.splice(index, 1);
        }

        $fileItem.remove();
    });
}

function bindGridEvents() {
    //--------- Item Change ---------
    $(document).on("change", ".item-name", function () {
        if (isLoadForEdit) return;
        const $row = $(this).closest("tr");
        const selectedOption = $(this).find("option:selected");
        const uomCode = selectedOption.data("ucode");
        const uomName = selectedOption.data("unit");
        $row.find(".uom-code").val(uomCode || "");
        $row.find(".uom-name").val(uomName || "");

    });

    //--------- Row Calculation ---------
    $(document).on(
        "change",
        ".usd-rate,.exch-rate,.rate,.bill-qty,.recd-qty,.pack-per,.disc-per,.cess-per,.oth-amt,.pack-amt,.disc-amt,.cgst-amt,.sgst-amt,.igst-amt,.cess-amt,.vat-per,.vat-amt",
        async function () {
            if (isLoadForEdit) return;
            const $row = $(this).closest("tr");
            await processRow($row, {
                calculateAmount: true,
                calculateTaxes: true
            });
        }
    );

    //--------- Amount Change ---------
    $(document).on("change", ".amount", async function () {
        if (isLoadForEdit) return;
        const $row = $(this).closest("tr");
        const qty = parseFloat($row.find(".bill-qty").val()) || 0;
        const amount = parseFloat($row.find(".amount").val()) || 0;

        const rate = qty > 0
            ? amount / qty
            : 0;

        $row.find(".rate").val(rate.toFixed(6));
        await processRow($row, {
            calculateAmount: true,
            calculateTaxes: true
        });
    });

    //--------- GST Percentage Change ---------
    $(document).on(
        "change",
        ".cgst-per,.sgst-per,.igst-per",
        async function () {
            if (isLoadForEdit) return;
            const $row = $(this).closest("tr");
            await processRow($row, {
                calculateTaxes: true
            });
            toggleTaxAmountFields($row);
        }
    );

    //--------- Tax Type Change ---------
    $(document).on("change", ".tax-code", async function () {
        if (isLoadForEdit) return;
        const $ddl = $(this);
        const $row = $ddl.closest("tr");

        const itemCode =
            parseInt($row.find(".item-name").val()) || 0;

        const selected = $ddl.find("option:selected");

        // Fill tax percentages
        $row.find(".cgst-per").val(selected.data("cgst") || 0);
        $row.find(".sgst-per").val(selected.data("sgst") || 0);
        $row.find(".igst-per").val(selected.data("igst") || 0);
        $row.find(".vat-per").val(selected.data("vat") || 0);

        await calculateAmt($row, itemCode);
        calculateTax($row, itemCode);
        calculateItemTotals();
        calculateLandAmount($row, itemCode);
        await CalcDrCrNote();
        toggleTaxAmountFields($row);
        // Move focus to Pack %
        $row.find(".pack-per").focus();
    });

}

function bindFreightEvents() {
    //----------- Freight Pay Change ---------
    $('#NumFreightPay').on('change', async function () {
        if (isLoadForEdit) return;
        const request = GetCrDrNoteRequest(false);
        await CalcFreightAndCrDr(request);
    });

    //----------- Freight Tax Amount Change ---------
    $('#NumFrtTax2').on('change', async function () {
        if (isLoadForEdit) return;
        const request = GetCrDrNoteRequest(true);
        await CalcFreightAndCrDr(request);
    });

    //----------- Freight Tax % Change ---------
    $('#NumFrtTax1').on('change', async function () {
        if (isLoadForEdit) return;
        calculateLandAmount();
        const request = GetCrDrNoteRequest(false);
        await CalcFreightAndCrDr(request);
    });

    //----------- Freight TDS % Change ---------
    $('#NumTDSonFRT1').on('change', function () {
        if (isLoadForEdit) return;
        const freightPay = parseFloat($('#NumFreightPay').val()) || 0;
        const freightTdsPer = parseFloat($(this).val()) || 0;
        const freightTds = (freightPay * freightTdsPer / 100).toFixed(2);
        $('#NumTDSonFRT2').val(freightTds);
    });

    //----------- Unloading TDS % Change ----------
    $('#NumUnloadTDS1').on('change', function () {
        if (isLoadForEdit) return;
        const unloadingAmt = parseFloat($('#NumUnloadAmt').val()) || 0;
        const unloadingTdsPer = parseFloat($(this).val()) || 0;
        const unloadingTds = (unloadingAmt * unloadingTdsPer * 0.01).toFixed(2);
        $('#NumUnloadTDS2').val(unloadingTds);
    });

    //----------- Item Total TDS % Change ----------
    $('#TxtTds1').on('change', async function () {
        if (isLoadForEdit) return;
        await calculateTDSAmount('#TxtTds1', '#TxtTds2');
    });

    //----------- TDS 194Q % Change ----------
    $('#TxtTds194q1').on('change', async function () {
        if (isLoadForEdit) return;
        await calculateTDSAmount('#TxtTds194q1', '#TxtTds194q2');
    });

}

function bindTotalsEvents() {
    $('#NumTcs1').on('change', function () {
        if (isLoadForEdit) return;
        const grossAmount = CalculateGrossAmount();
        const tcs = Math.ceil(grossAmount * (parseFloat($(this).val()) || 0) / 100);
        $('#NumTcs2').val(tcs);
        const roundOff = parseFloat($('#NumRoundOff').val()) || 0;
        $('#NumNetAmount').val(grossAmount + tcs + roundOff);
        calculateItemTotals();
    });

    $('#NumTcs2').on('change', function () {
        if (isLoadForEdit) return;
        calculateItemTotals();
    });
    $('#NumRoundOff').on('change', function () {
        if (isLoadForEdit) return;
        const subTotal = parseFloat($('#NumSubTotal').val()) || 0;
        const roundOff = parseFloat($(this).val()) || 0;
        const netAmount = subTotal + roundOff;
        $('#NumNetAmount').val(netAmount.toFixed(2));
        $('#TxtNetAmount').val(netAmount.toFixed(2));
    });
    bindDistributionChange('#NumPacking', '.pack-amt', '.pack-per');
    bindDistributionChange('#NumDiscount', '.disc-amt', '.disc-per');
    bindDistributionChange('#NumOthAmt', '.oth-amt');
}

function bindTransportEvents() {
    //----------- Transport Change ----------
    $('#ddlTransportName').on('change', function () {
        if (isLoadForEdit) return;
        const transportCode = $(this).val();
        if (!transportCode)
            return;
        getFrtCrAcCodeByTransCode(transportCode);
    });

    //----------- Exchange Rate Change ----------
    $('#NumExRate').on('change', async function () {
        if (isLoadForEdit) return;
        await onExchangeRateChanged();
    });
}

function bindBankEvents() {
    //---------- Bank Rate Change ----------
    $('#TxtBankRate2').on('change', async function () {
        if (isLoadForEdit) return;
        const bankAmt = parseFloat($(this).val()) || 0;
        const netAmount = parseFloat($('#NumNetAmount').val()) || 0;

        // Difference Amount
        $('#TxtDiffAmt').val((bankAmt - netAmount).toFixed(2));

        // Generate PL No if required
        if (
            bankAmt > 0 &&
            (!$('#NumPlNo').val() || parseInt($('#NumPlNo').val()) === 0)
        ) {
            const response = await $.ajax({
                url: '/PurchaseBillPassEntry/GetNextPLNo',
                type: 'GET'
            });
            if (response.success) {
                $('#NumPlNo').val(response.plNo);
            }
        }
    });
}

function EPRAttachmentEvent() {
    // Document Type Change
    $("#ddlDocumentType").change(function () {

        if ($(this).val() == "") {

            // Disable file selection
            $("#erpFile").val("").prop("disabled", true);
            $(".erppage-filepicker-name").val("");
            $(".erppage-filepicker-btn").addClass("disabled");

        } else {

            // Enable file selection
            $("#erpFile").prop("disabled", false);
            $(".erppage-filepicker-btn").removeClass("disabled");
        }

    });

    // File selected
    $("#erpFile").change(function () {

        if (this.files.length > 0) {
            $(".erppage-filepicker-name").val(this.files[0].name);
        } else {
            $(".erppage-filepicker-name").val("");
        }

    });

    // Remove selected file
    $(".erppage-filepicker-remove").click(function () {

        $("#erpFile").val("");
        $(".erppage-filepicker-name").val("");

    });

    // Add selected file details in grid
    $("#SaveButtonDT").click(function () {

        var docType = $("#ddlDocumentType").val();
        var fileInput = $("#erpFile")[0];

        if (docType == "") {
            showToast("Please select Document Type.", {type:"warning"});
            return;
        }

        if (fileInput.files.length == 0) {
            showToast("Please select a file.", {type:"warning"});
            return;
        }

        // Duplicate Document Type Check
        var isExists = EPRAttachmentList.some(function (item) {
            return item.DocumentType === docType;
        });

        if (isExists) {
            showToast("This Document Type has already been added.", { type: "warning" });
            return;
        }

        var attachment = {
            DocumentType: docType,
            OriginalFileName: fileInput.files[0].name,
            FileName: getEPRAttachmentFileName(docType, fileInput.files[0].name),
            File: fileInput.files[0]   // agar future me upload karna ho
        };

        AddEPRAttachmentRow(attachment);

        $("#erpFile").val("");
        $(".erppage-filepicker-name").val("");
        $("#ddlDocumentType").val("");
    });

    // Delete row
    $(document).on("click", ".btnEPRDelete", function () {

        var row = $(this).closest("tr");
        var index = row.data("index");

        // Remove from array
        EPRAttachmentList.splice(index, 1);

        // Remove from grid
        row.remove();

        // Update indexes of remaining rows
        $("#tblAttachmentEPR tbody tr").each(function (i) {
            $(this).attr("data-index", i);
        });

    });

    // Preview File row
    $(document).on("click", ".btnEPRPreview", function () {

        var index = $(this).closest("tr").data("index");
        var attachment = EPRAttachmentList[index];

        if (!attachment)
            return;

        var file = attachment.File;
        var url = URL.createObjectURL(file);

        $("#previewImage").hide();
        $("#previewPdf").hide();

        // Image Preview
        if (file.type.startsWith("image/")) {

            $("#previewImage")
                .attr("src", url)
                .show();

        }
        // PDF Preview
        else if (file.type === "application/pdf") {

            $("#previewPdf")
                .attr("src", url)
                .show();

        }
        else {

            window.open(url, '_blank');
            return;
        }

        $("#imagePreviewModal").modal("show");
    });
}

function bindButtonsAndModalsEvent() {
    //TDS Calc
    $('#btnTDSCalc').on('click', function () {
        OnTDSBtnClick();
    })

    //Copy From
    $(document).on("click", ".copy-from-item", function (e) {
        e.preventDefault();

        const code = $(this).data("doctype");
        //const modal = $(this).data("modal");

        getCopyFromData(code);
    })

    //Select All
    $(document).on("change", "#selectAllPR", function () {

        const isChecked = $(this).is(":checked");

        $("#tblpurchaseordermodal .copyfrom-check").prop("checked", isChecked);

    });

    //Select Individual
    $(document).on("change", "#tblpurchaseordermodal .copyfrom-check", function () {

        const total = $("#tblpurchaseordermodal .copyfrom-check").length;
        const checked = $("#tblpurchaseordermodal .copyfrom-check:checked").length;

        $("#selectAllPR").prop("checked", total > 0 && total === checked);

    });
}

//------------- GENERATE VNO -----------------
async function GetVNo(vType) {
    try {
        const res = await fetch(`/PurchaseBillPassEntry/GetVNo?vType=${encodeURIComponent(vType)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();

        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//=====================DROPDOWNS=============================

//--------- DOCTYPE -----------
function loadDocTypeList(selectedValue = null) {
    docTypeMap = {};
    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetDocTypeList",
        selector: "#ddlDocType",
        responsePath: null,
        valueField: "value",
        textField: "text",
        defaultOption: "-- Select Doc Type --",
        selectedValue,
        beforeBind(list) {
            docTypeMap = {};
            list.forEach(x => {
                docTypeMap[x.value] = x.text;
            });
        }
    });
}
//------------- Latest Debit Account By VType -----------------
function SetLatestDebitAccount(vType) {

    $.ajax({
        url: '/PurchaseBillPassEntry/GetLatestDebitAccount',
        type: 'GET',
        data: {
            vType: vType
        },
        success: function (response) {
            if (response.success) {
                loadDrAcListByVtype(vType, response.debitAc);
            }
            else {
                showToast(response.message, { type: "error" });
            }
        },
        error: function (xhr) {
            console.error(xhr.responseText);
        }
    });
}

//--------- MRN -----------
function loadMRNList(vType, selectedValue = null) {
    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetMrnNoList",
        selector: "#TxtMRNNo2",
        data: { vType },
        defaultOption: "--Select MRN No--",
        selectedValue,
        isInitSelect2: true,
        optionBuilder: item => `
            <option
                value="${item.Value}"
                data-vtype="${item.vType}">
                ${item.Text}
            </option>
        `
    });
}

//--------- SUPPLIER PARTY -----------
function loadPartyListNatureSupplier(selector, isSelect2 = false, selectedValue = null) {
    return loadDropdown({
        cacheKey: "Supplier",
        url: "/PurchaseBillPassEntry/GetPartyListNatureSupplier",
        selector,
        defaultOption: "-- Select --",
        isInitSelect2: isSelect2,
        selectedValue
    });
}
//--------- DR AC BY VTYPE-----------
function loadDrAcListByVtype(vType, selectedValue = null) {
    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetDrAcListByVtype",
        selector: "#ddlDebitAC",
        data: { vType },
        defaultOption: "-- Select --",
        selectedValue
    });
}

//--------- PARTY DR CR -----------
function loadPartyDrCrAcList(selector, selectedValue = null) {

    return loadDropdown({
        cacheKey: "PartyDrCr",
        url: "/PurchaseBillPassEntry/GetPartyDrCrAcList",
        selector,
        defaultOption: "-- Select --",
        isInitSelect2: true,
        selectedValue
    });

}

//--------- TRANSPORT GST -----------
function loadTranGSTByFrtCrAc(code, selectedValue = null) {
    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetTranGSTByFrtCrAc",
        selector: "#ddlTransportGSTNo",
        data: { frtCrAcCode: code },
        defaultOption: "-- Select GST--",
        isInitSelect2: false,
        selectedValue: selectedValue,
        afterBind(list, ddl) {
            if (list.length) {
                ddl.val(list[0].Value).trigger("change");
            }
        }
    });
}

//--------- ITEM -----------
function loadItemList(selector, selectedValue = null) {
    return loadDropdown({
        cacheKey: "Item",
        url: "/PurchaseBillPassEntry/GetItemList",
        selector,
        defaultOption: "-- Select --",
        selectedValue,
        optionBuilder: item => `
            <option
                value="${item.Value}"
                data-unit="${item.unit}"
                data-ucode="${item.ucode}">
                ${item.Text}
            </option>
        `
    });
}

//--------- ADDRESS -----------
function loadAddList(shipFromCode, selector) {
    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetAddList",
        selector,
        data: { shipFromCode },
        defaultOption: "-- Select Address --"
    });
}

//--------- CITY -----------
function loadCityList(selector, selectedValue = null) {
    return loadDropdown({
        cacheKey: "City",
        url: "/PurchaseBillPassEntry/GetCityList",
        selector,
        defaultOption: "-- Select City --",
        selectedValue
    });
}

//--------- STATE -----------
function loadStateList(selector, cCode) {
    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetStateList",
        selector,
        data: { cCode },
        defaultOption: null,
        optionBuilder: item => `<option value="${item.Value ?? ''}">${item.Text ?? ''}</option>`
    });
}

//--------- CURRENCY -----------
function loadCurrencyList(selectedValue = null) {
    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetCurrencyList",
        selector: "#ddlCurrency",
        defaultOption: "-- Select Currency --",
        selectedValue: selectedValue,
        afterBind(list, ddl) {
            if (list.length) {
                ddl.val(list[0].Value).trigger("change");
            }
        }
    });
}

//--------- TAX -----------
function loadTaxTypeList(selector, selectedValue = null) {
    return loadDropdown({
        cacheKey: "Tax",
        url: "/PurchaseBillPassEntry/GetTaxList",
        selector,
        defaultOption: "-- Select --",
        selectedValue,
        optionBuilder(item) {
            return `
            <option
                value="${item.Value}"
                data-cgst="${item.CGST_PER}"
                data-sgst="${item.SGST_PER}"
                data-igst="${item.IGST_PER}"
                data-vat="${item.VAT_PER}"
                data-tds="${item.TDS_PER}"
                data-tcs="${item.TCS_PER}"
                data-oth="${item.OTH_PER}"
                data-oth2="${item.OTH_PER2}">
                ${item.Text}
            </option>`;
        }
    });
}

//--------- STATUS -----------
function loadStatusList() {

    statusMap = {};

    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetStatusList",
        selector: "#ddlStatus",
        responsePath: null,
        valueField: "value",
        textField: "text",
        defaultOption: "-- Select Status --",
        beforeBind(list) {
            statusMap = {};
            list.forEach(x => statusMap[x.value] = x.text);

        },
        afterBind(list, ddl) {
            if (list.length) {
                ddl.val(list[0].value).trigger("change");
            }
        }
    });

}

//--------- TRANSPORT -----------
function loadTransportList() {

    return loadDropdown({
        url: "/PurchaseBillPassEntry/GetTransportList",
        selector: "#ddlTransportName",
        responsePath: null,
        valueField: "value",
        textField: "text",
        defaultOption: "-- Select Transport --",
        isInitSelect2: true
    });

}

//--------- DEPARTMENT -----------
function loadDepartmentList(selector, selectedValue = null) {

    return loadDropdown({
        cacheKey: "Department",
        url: "/PurchaseBillPassEntry/GetDepartmentList",
        selector,
        defaultOption: "-- Select --",
        isInitSelect2: true,
        selectedValue
    });

}

//--------- Generic ddl -------
async function loadDropdown({

    cacheKey = null,
    url,
    selector,
    data = {},
    responsePath = "data",

    valueField = "Value",
    textField = "Text",

    defaultOption = "-- Select --",
    selectedValue = null,

    isInitSelect2 = false,

    beforeBind = null,
    afterBind = null,

    optionBuilder = null

}) {

    const ddl = $(selector);

    let list = [];
    let html = "";

    //==========================
    // Already Cached
    //==========================
    if (cacheKey && dropdownCache[cacheKey]) {

        ({ list, html } = dropdownCache[cacheKey]);

    }

    //==========================
    // Request Already Running
    //==========================
    else if (cacheKey && dropdownPromiseCache[cacheKey]) {

        ({ list, html } = await dropdownPromiseCache[cacheKey]);

    }

    //==========================
    // Make New Request
    //==========================
    else {

        const promise = (async () => {

            const start = performance.now();

            const response = await $.ajax({
                url,
                type: "GET",
                data,
                dataType: "json"
            });

            const dataList = responsePath
                ? response[responsePath] || []
                : response || [];

            if (beforeBind) {
                beforeBind(dataList);
            }

            let optionHtml = "";

            if (defaultOption !== null) {
                optionHtml += `<option value="">${defaultOption}</option>`;
            }

            if (optionBuilder) {

                optionHtml += dataList.map(optionBuilder).join("");

            }
            else {

                optionHtml += dataList.map(item => `
                    <option value="${item[valueField]}">
                        ${item[textField]}
                    </option>
                `).join("");

            }

            return {
                list: dataList,
                html: optionHtml
            };

        })();

        if (cacheKey) {
            dropdownPromiseCache[cacheKey] = promise;
        }

        const result = await promise;

        list = result.list;
        html = result.html;

        if (cacheKey) {

            dropdownCache[cacheKey] = result;

            delete dropdownPromiseCache[cacheKey];

        }
    }

    //==========================
    // beforeBind on Cached Data
    //==========================
    if (beforeBind && cacheKey && dropdownCache[cacheKey]) {
        beforeBind(list);
    }

    //==========================
    // Bind
    //==========================
    ddl.html(html);

    //==========================
    // Select2
    //==========================
    if (isInitSelect2) {
        initSelect2(ddl);
    }

    //==========================
    // Selected Value
    //==========================
    if (selectedValue !== null && selectedValue !== "") {
        ddl.val(selectedValue).trigger("change");
    }

    //==========================
    // afterBind
    //==========================
    if (afterBind) {
        afterBind(list, ddl);
    }

    return list;
}

//---------- Load All Initially on Page Load -------------
async function loadInitialDropdowns() {

    await Promise.all([

        loadStatusList(),
        loadDocTypeList(),

        loadPartyListNatureSupplier('#ddlBillFrom'),
        loadPartyListNatureSupplier('#ddlShipFrom1', true),

        loadCityList('#ddlDispCity'),
        loadCityList('#ddlCityPD'),
        loadCityList('#ddlCitySF'),

        loadTransportList(),

        loadPartyDrCrAcList('#ddlCreditAC'),
        loadPartyDrCrAcList('#ddlFreightDebitAC'),
        loadPartyDrCrAcList('#ddlFreightCreditAC'),
        loadPartyDrCrAcList('#ddlWBDebitAC'),
        loadPartyDrCrAcList('#ddlWBCreditAC'),
        loadPartyDrCrAcList('#ddlUnloadDebitAC'),
        loadPartyDrCrAcList('#ddlUnloadCreditAC'),
        loadPartyDrCrAcList('#ddlTdsAccount'),

        loadCurrencyList()

    ]);

    //// Depends on Freight Credit Account
    //const frtCrAcCode = $('#ddlFreightCreditAC').val();
    //await loadTranGSTByFrtCrAc(frtCrAcCode);
}
//=========== DROPDOWN END ============

function convertToDateInputFormat(dateTimeStr) {
    if (!dateTimeStr) return '';

    var datePart = dateTimeStr.split(' ')[0];
    var parts = datePart.split('/');

    if (parts.length !== 3) return '';

    var day = parts[0].padStart(2, '0');
    var month = parts[1].padStart(2, '0');
    var year = parts[2];

    return `${year}-${month}-${day}`;
}

//LOAD DATA by V_No
async function loadFullQuotationByVno(vNo, vType) {

    try {

        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetFullQuotationByVno",
            type: "GET",
            dataType: "json",
            data: { vNo: vNo, vType: vType }
        });

        console.log("Quotation :", response);

        if (!response.success || !response.header) {
            showToast("Quotation not found.", { type: "warning" });
            return;
        }

        const header = response.header;
        const items = response.items || [];
        const attachments = response.attachments || [];
        const eprAttachments = response.eprAttachments || [];

        //==========================
        // Document Details
        //==========================

        await loadDocTypeList(header.v_TYPE);
        $("#DtDocDate").val(formatDateYMD(header.v_DATE));
        $("#NumDocNo").val(header.v_NO);


        const vTypeToBind = $("#ddlDocType").val();
        await loadMRNList(vTypeToBind, header.reF_NO);
        $("#TxtMRNNo1").val(header.reF_TYPE || "");
        //==========================
        // Bill From
        //==========================

        await loadPartyListNatureSupplier("#ddlBillFrom", false, header.partY_CODE);
        $("#TxtAdd1PD").val(header.bilL_ADD1 || "");
        $("#TxtAdd2PD").val(header.bilL_ADD2 || "");
        $("#TxtAdd3PD").val(header.bilL_ADD3 || "");
        await loadCityList("#ddlCityPD", header.bilL_CITY);
        await loadStateList("#ddlStatePD", header.bilL_CITY);
        $("#NumPincodeBL").val(header.bilL_PINCODE || "");
        $("#TxtGSTNo").val(header.bilL_GST || "");
        $("#TxtDispFromAdd").val(header.disP_ADDRESS || "");
        await loadCityList("#ddlDispCity", header.disP_CITY);

        //==========================
        // Ship From
        //==========================

        await loadPartyListNatureSupplier("#ddlShipFrom1", true, header.shiP_CODE);
        $("#TxtAdd1SF").val(header.shiP_ADD1 || "");
        $("#TxtAdd2SF").val(header.shiP_ADD2 || "");
        $("#TxtAdd3SF").val(header.shiP_ADD3 || "");
        await loadCityList("#ddlCitySF", header.shiP_CITY);
        await loadStateList("#ddlStateSF", header.shiP_CITY);
        $("#TxtPincodeSF").val(header.shiP_PINCODE || "");
        $("#TxtGSTNoSF").val(header.shiP_GST || "");

        //==========================
        // Bill Details
        //==========================

        $("#TxtBillNo").val(header.bilL_NO || "");
        setDateControl(header.bilL_DATE, "#DtBillDate", "#chkBillDate");
        console.log(header.bilL_DATE)

        $("#TxtChallanNo").val(header.chalL_NO || "");
        setDateControl(header.chalL_DATE, "#DtChDate", "#chkChDate");

        $("#TxtBLNo").val(header.bL_NO || "");
        setDateControl(header.bL_DT, "#DtBLDate", "#chkBLDate");

        $("#TxtWaybillNo").val(header.waybilL_NO || "");
        $("#TxtWayBillInvNo").val(header.ewB_INVNO || "");
        $("#DtWaybillDate").val(formatDateYMD(header.ewB_DATE));
        $("#DtWaybillExpiry").val(formatDateYMD(header.ewB_EXPDATE));

        //==========================
        // Accounts
        //==========================

        await loadPartyDrCrAcList("#ddlDebitAC", header.debiT_AC);
        await loadPartyDrCrAcList("#ddlCreditAC", header.crediT_AC);

        $("#txtRemarks").val(header.remarks || "");
        //==========================
        // General
        //==========================

        $("#ddlInputType").val(header.inpuT_TYPE || "");
        $("#NumExRate").val(header.excH_RATE || 0);
        $("#txtRemarks").val(header.remarks || "");
        $("#NumNetAmount").val(header.namount || 0);
        $("#TxtNetAmount").val(header.namount || 0);
        $("#ddlStatus").val(header.status || "");
        $("#ddlCurrency").val(header.currencY || "");
        $("#NumReceivedQty").val(header.recD_QTY || 0);
        $("#NumBillQty").val(header.bilL_QTY || 0);
        $("#NumAmount").val(header.amount || 0);
        $("#NumPacking").val(header.pacK_AMT || 0);
        $("#NumDiscount").val(header.disC_AMT || 0);
        $("#NumCgst").val(header.cgsT_AMT || 0);
        $("#NumSgst").val(header.sgsT_AMT || 0);
        $("#NumIgst").val(header.igsT_AMT || 0);
        $("#NumCess").val(header.cesS_AMT || 0);
        $("#NumVat").val(header.vaT_AMT || 0);
        $("#NumOtherAmt").val(header.otH_AMT || 0);
        $("#NumTcs1").val(header.tcS_PER || 0);
        $("#NumTcs2").val(header.tcS_AMT || 0);
        $("#NumRoundOff").val(header.rounD_OFF || 0);
        $("#TxtTds1").val(header.tdS_PER || 0);
        $("#TxtTds2").val(header.tdS_AMT || 0);
        $("#TxtTds194q1").val(header.tds_per194Q || 0);
        $("#TxtTds194q2").val(header.tds_amt194Q || 0);
        $("#TxtBankRate1").val(header.banK_RATE || 0);
        $("#TxtBankRate2").val(header.banK_AMT || 0);
        $("#TxtDiffAmt").val(header.difF_AMT || 0);
        $("#NumPlNo").val(header.pL_NO || "");

        loadCurrencyList(header.currency);

        setDateControl(header.pL_DATE, "#DtPlDate", "#chkPlDate");

        //==========================
        // Transport Details
        //==========================

        const transportCode = parseInt(header.transporT_CODE) || 0;
        const transportName = (header.transporT_NAME || "").trim();
        if (transportCode === 0 && transportName === "") {
            $("#ddlTransportName").val(null).trigger("change");
        }
        else if (transportCode === 0) {
            if ($("#ddlTransportName option[value='" + transportName + "']").length === 0) {
                $("#ddlTransportName").append(
                    $("<option>", {
                        value: transportName,
                        text: transportName
                    })
                );
            }
            $("#ddlTransportName").val(transportName).trigger("change");
        }
        else {
            $("#ddlTransportName").val(transportCode.toString()).trigger("change");
            getFrtCrAcCodeByTransCode(transportCode);
        }

        $("#ddlTransportGSTNo").val(header.trP_GSTNO || "");
        $("#ddlTaxType").val(header.trP_TAXTYPE || "");
        $("#txtVehicleNo").val(header.trucK_NO || "");
        $("#txtContainerNo").val(header.containeR_NO || "");
        $("#txtGRNo").val(header.gR_NO || "");
        setDateControl(header.gR_DATE, "#DtGRDate", "#chkGRDate");

        $("#ChkSealedVehicle").prop("checked", (header.sealeD_VEHICLE || 0) == 1);

        //==========================
        // Freight
        //==========================

        $("#NumFreightPay").val(header.frtpaY_AMT || 0);
        $("#NumFrtTax1").val(header.frtpaY_TAXPER || 0);
        $("#NumFrtTax2").val(header.frtpaY_TAX || 0);
        $("#TxtFrtPayNarration").val(header.frtpaY_NAR || "");
        await loadPartyDrCrAcList("#ddlFreightDebitAC", header.frtpaY_DRAC);
        await loadPartyDrCrAcList("#ddlFreightCreditAC", header.frtpaY_CRAC);
        $("#NumTDSonFRT1").val(header.frT_TDSPER || 0);
        $("#NumTDSonFRT2").val(header.frT_TDS || 0);
        $("#TxtBillNoLD").val(header.trP_BILLNO || "");
        setDateControl(header.frT_BILLDT, "#DtBillDateLD", "#chkBillDateLD");
        await loadTranGSTByFrtCrAc(header.frtpaY_CRAC, header.trP_GSTNO)

        //==========================
        // Weigh Bridge
        //==========================

        $("#NumWBAmount").val(header.wB_AMT || 0);
        $("#NumWBTDS1").val(header.wB_TDSPER || 0);
        $("#NumWBTDS2").val(header.wB_TDS || 0);
        $("#TxtWBNarration").val(header.wB_NARR || "");
        await loadPartyDrCrAcList("#ddlWBDebitAC", header.wB_DRACT);
        await loadPartyDrCrAcList("#ddlWBCreditAC", header.wB_CRACT);

        //==========================
        // Unloading
        //==========================

        $("#NumUnloadAmt").val(header.uL_AMT || 0);
        $("#NumUnloadTDS1").val(header.uL_TDSPER || 0);
        $("#NumUnloadTDS2").val(header.uL_TDS || 0);
        $("#TxtUnloadNarration").val(header.uL_NARR || "");
        await loadPartyDrCrAcList("#ddlUnloadDebitAC", header.uL_DRACT);
        await loadPartyDrCrAcList("#ddlUnloadCreditAC", header.uL_CRACT);

        //==========================
        // Hold Details
        //==========================

        $("#ddlPayment").val(header.holD_PAY || "");
        $("#TxtReason").val(header.holD_REASON || "");
        setDateControl(header.holD_DATE, "#DtHoldDate", "#chkHoldDate");

        if ((header.holD_PAY || "").toUpperCase() === "HOLD") {
            $("#ddlPayment").prop("disabled", true);
            $("#DtHoldDate").prop("disabled", true);
            $("#chkHoldDate").prop("disabled", true);
        }

        //==========================
        // Debit / Credit Notes
        //==========================

        $("#ChkDebitFromTransporter").prop(
            "checked",
            (header.dR_FROM_TPT || "").toUpperCase() === "YES"
        );
        $("#TxtQualityDiffDebitAmt").val(header.qlT_DR_AMT || 0);
        $("#TxtQualityDiffDebitTax").val(header.qlT_DR_TAX || 0);
        $("#TxtQualityDiffDebitNarration").val(header.qlT_DR_NAR || "");
        $("#TxtRateDiffDebitAmt").val(header.rdF_DR_AMT || 0);
        $("#TxtRateDiffDebitTax").val(header.rdF_DR_TAX || 0);
        $("#TxtRateDiffDebitNarration").val(header.rdF_DR_NAR || "");
        $("#TxtWeightDebitAmt").val(header.qtY_DR_AMT || 0);
        $("#TxtWeightDebitTax").val(header.qtY_DR_TAX || 0);
        $("#TxtWeightDebitNarration").val(header.qtY_DR_NAR || "");
        $("#TxtQCDebitNoteAmt").val(header.qC_DR_AMT || 0);
        $("#TxtQCDebitNoteTax").val(header.qC_DR_TAX || 0);
        $("#TxtQCDebitNarration").val(header.qC_DR_NAR || "");
        $("#TxtOtherDebitAmt").val(header.otH_DR_AMT || 0);
        $("#TxtOtherDebitTax").val(header.otH_DR_TAX || 0);
        $("#TxtOtherDebitNarration").val(header.otH_DR_NAR || "");

        //==========================
        // Item Grid
        //==========================

        const tbody = $("#tblItemRecordPBPE tbody");

        tbody.empty();

        if (items.length === 0) {
            await addNewRowBelow();
        }
        else {
            for (const item of items) {
                await addNewRowBelow(item);
            }
        }

        //==========================
        // Attachment
        //==========================

        if (attachments.length) {

            const files = attachments.map(x =>
                base64ToFile(x.filE_DATA, x.filE_NAME)
            );

            renderFiles(files);
        }

        //==========================
        // EPR Attachment
        //==========================
        eprAttachments.forEach(function (item) {
            var fileName = item.filE_NAME || item.FILE_NAME;

            // Split only at the first "_"
            var index = fileName.indexOf("_");

            var documentTypeCode = fileName.substring(0, index);
            var originalFileName = fileName.substring(index + 1);

            // Convert code to display text
            var documentType = getDocumentType(documentTypeCode);

            var file = base64ToFile(item.filE_DATA, item.filE_NAME);

            AddEPRAttachmentRow({
                DocumentType: documentType,
                OriginalFileName: originalFileName,
                FileName: fileName,
                File: file
            });

        });
    }
    catch (error) {
        console.error(error);
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

function parseNullableDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    return isNaN(date.getTime()) ? null : date.toISOString();
}

//------------------ ADD ROW ---------------
function createRowHtml(data = {}) {

    return `
        <tr>

            <td class="freeze-item"><select class="form-control form-control-sm item-name" disabled></select></td>

            <td><input class="form-control form-control-sm hsn-code" type="text" value="${data.hsN_CODE || ''}"/></td>
            <td>
                <input class="form-control form-control-sm uom-code" type="hidden" value="${data.uoM_CODE || ''}" disabled/>
                <input class="form-control form-control-sm uom-name" type="text" value="${data.unit || ''}" disabled/>
            </td>

            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos || ''}"/></td>
            <td><input class="form-control form-control-sm recd-qty" type="number" value="${data.recD_QTY || ''}" disabled/></td>
            <td><input class="form-control form-control-sm bill-qty" type="number" value="${data.bilL_QTY || ''}"/></td>

            <td><input class="form-control form-control-sm usd-rate" type="number" value="${data.usD_RATE || ''}"/></td>
            <td><input class="form-control form-control-sm exch-rate" type="number" value="${data.excH_RATE || ''}"/></td>
            <td><input class="form-control form-control-sm rate" type="number" value="${data.rate || ''}"/></td>
            <td><input class="form-control form-control-sm amount" type="number" value="${data.amount || ''}"/></td>

            <td>
                <select class="form-control form-control-sm rcm-yn">
                    <option value="">-- Select --</option>
                    <option value="YES" ${(data.rcM_YN || '').toUpperCase() === 'YES' ? 'selected' : ''}>YES</option>
                    <option value="NO" ${(data.rcM_YN || '').toUpperCase() === 'NO' ? 'selected' : ''}>NO</option>
                </select>
            </td>

            <td>
                <select class="form-control form-control-sm input-yn">
                    <option value="">-- Select --</option>
                    <option value="YES" ${(data.inpuT_YN || '').toUpperCase() === 'YES' ? 'selected' : ''}>YES</option>
                    <option value="NO" ${(data.inpuT_YN || '').toUpperCase() === 'NO' ? 'selected' : ''}>NO</option>
                </select>
            </td>

            <td><select class="form-control form-control-sm tax-code"></select></td>

            <td><input class="form-control form-control-sm pack-per" type="number" value="${data.pacK_PER || ''}"/></td>
            <td><input class="form-control form-control-sm pack-amt" type="number" value="${data.pacK_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm disc-per" type="number" value="${data.disC_PER || ''}"/></td>
            <td><input class="form-control form-control-sm disc-amt" type="number" value="${data.disC_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm cgst-per" type="number" value="${data.cgsT_PER || ''}" disabled/></td>
            <td><input class="form-control form-control-sm cgst-amt" type="number" value="${data.cgsT_AMT || ''}" ${data.cgsT_PER ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm sgst-per" type="number" value="${data.sgsT_PER || ''}" disabled/></td>
            <td><input class="form-control form-control-sm sgst-amt" type="number" value="${data.sgsT_AMT || ''}" ${data.sgsT_PER ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm igst-per" type="number" value="${data.igsT_PER || ''}" disabled/></td>
            <td><input class="form-control form-control-sm igst-amt" type="number" value="${data.igsT_AMT || ''}" ${data.igsT_PER ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm cess-per" type="number" value="${data.cesS_PER || ''}"/></td>
            <td><input class="form-control form-control-sm cess-amt" type="number" value="${data.cesS_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm vat-per" type="number" value="${data.vaT_PER || ''}"/></td>
            <td><input class="form-control form-control-sm vat-amt" type="number" value="${data.vaT_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm oth-amt" type="number" value="${data.otH_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm net-amt" type="number" value="${data.neT_AMT || ''}" disabled/></td>

            <td>
                <input class="form-control form-control-sm make-code" type="hidden" value="${data.makE_CODE || ''}"/>
                <input class="form-control form-control-sm make-name" type="text" value="${data.make || ''}" disabled/>
            </td>
            <td>
                <select class="form-control form-control-sm dept-code" disabled></select>
            </td>

            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || ''}"/></td>

            <td><input class="form-control form-control-sm land-rate" type="number" value="${data.lanD_RATE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm land-amt" type="number" value="${data.lanD_AMT || ''}" disabled/></td>

            <td><input class="form-control form-control-sm poland-rate" type="number" value="${data.polanD_RATE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm po-rate" type="number" value="${data.pO_RATE || ''}" disabled/></td>

            <td><input class="form-control form-control-sm po-type" type="text" value="${data.pO_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm po-no" type="number" value="${data.pO_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm kanta-type" type="text" value="${data.kantA_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm kanta-no" type="number" value="${data.kantA_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm req-type" type="text" value="${data.reQ_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm req-no" type="number" value="${data.reQ_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm ref-type" type="text" value="${data.reF_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm ref-no" type="number" value="${data.reF_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm dr-note-amt" type="number" value="${data.dr_notE_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm cr-note-amt" type="number" value="${data.cr_notE_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm qlty-diff-dr-amt" type="number" value="${data.qlty_diff_dR_AMT || ''}" disabled/></td>
            <td><input class="form-control form-control-sm rate-diff-dr-amt" type="number" value="${data.rate_diff_dR_AMT || ''}" disabled/></td>
            <td><input class="form-control form-control-sm qc-diff-dr-amt" type="number" value="${data.qc_diff_dR_AMT || ''}" disabled/></td>
            <td><input class="form-control form-control-sm qty-diff-dr-amt" type="number" value="${data.qty_diff_dR_AMT || ''}" disabled/></td>
            <td><input class="form-control form-control-sm other-dr-amt" type="number" value="${data.other_dR_AMT || ''}" disabled/></td>

            <td class="action-col">
                <i class="fas fa-trash text-danger delete-row"></i>
                <i class="fas fa-plus-circle text-success add-row"></i>
            </td>

        </tr>
    `;
}

async function addNewRowBelow(data = null) {

    data = data || {};

    let rowHtml = createRowHtml(data);

    $("#tblItemRecordPBPE tbody").append(rowHtml);

    const $lastRow = $("#tblItemRecordPBPE tbody tr:last");

    //await loadItemList($lastRow.find(".item-name"), data.iteM_CODE || null);

    //await loadTaxTypeList($lastRow.find(".tax-code"), data.taX_CODE || null);

    //await loadDepartmentList($lastRow.find(".dept-code"), data.depT_CODE || null);
    $lastRow.find(".item-name").html(dropdownCache.Item.html);
    $lastRow.find(".tax-code").html(dropdownCache.Tax.html);
    $lastRow.find(".dept-code").html(dropdownCache.Department.html);

    $lastRow.find(".item-name").val(data.iteM_CODE || "");
    $lastRow.find(".tax-code").val(data.taX_CODE || "");
    $lastRow.find(".dept-code").val(data.depT_CODE || "");
}

//-------------- DELETE ROW -----------
function deleteRow(el) {
    // Remove row
    $(el).closest('tr').remove();

    // Remove existing add buttons from all rows
    $('#tblItemRecordPBPE tbody tr .add-row-icon').remove();

    // Add add-icon to the last row only (if any rows left)
    const lastRow = $('#tblItemRecordPBPE tbody tr:last');
    if (lastRow.length) {
        const actionCell = lastRow.find('td:last');
        actionCell.append(`
                <i class="fas fa-plus-circle ms-2 text-success add-row-icon" onclick="addNewRowBelow()" style="cursor:pointer;"></i>
            `);
    }
}

//-------- SELECT2 HELPER -------------
function initSelect2($ddl) {
    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true
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

//-------- DATE WITH CHK HELPER -------------
function toggleDate() {

    $('.erppage-checkbox-input').each(function () {

        const chk = $(this);
        const dateInput = chk.closest('.erppage-datebox').find('input[type="date"]');

        if (!dateInput.length) return;

        // Initial state
        dateInput.prop('disabled', !chk.is(':checked'));

        // Toggle on change
        chk.on('change', function () {
            dateInput.prop('disabled', !this.checked);
        });

    });

}

function setDateControl(dateValue, dateInputId, checkBoxId) {
    if (!dateValue || dateValue === "") {
        const currentDate = getCurrentDateYMD();
        $(dateInputId).val(currentDate);
        $(dateInputId).prop('disabled', true);
        $(checkBoxId).prop('checked', false);
    } else {
        $(dateInputId).val(formatDateYMD(dateValue));
        $(dateInputId).prop('disabled', false);
        $(checkBoxId).prop('checked', true);
    }
}

//============================================ MRN No Change=============================
function clearPurchaseBillFields() {

    // Item Grid
    $('#tblItemRecordPBPE tbody').empty();

    // Bill Details
    $('#TxtBillNo').val('');
    $('#DtBillDate').val('');
    $('#chkBillDate').prop('checked', false);

    $('#TxtChallanNo').val('');
    $('#DtChDate').val('');
    $('#chkChDate').prop('checked', false);

    $('#TxtWaybillNo').val('');
    $('#DtWaybillDate').val('');

    $('#TxtWayBillInvNo').val('');
    $('#DtWaybillExpiry').val('');
    $('#TxtBLNo').val('');
    $('#DtBLDate').val('');
    $('#chkBLDate').prop('checked', false);

    // Bill From
    $('#ddlBillFrom').val('').trigger('change');
    $('#ddlBillFromAddress').val('').trigger('change');
    $('#TxtAdd1PD').val('');
    $('#TxtAdd2PD').val('');
    $('#TxtAdd3PD').val('');
    $('#ddlCityPD').val('').trigger('change');
    $('#ddlStatePD').val('').trigger('change');
    $('#NumPincodeBL').val('');
    $('#TxtGSTNo').val('');
    $('#TxtDispFromAdd').val('');
    $('#ddlDispCity').val('').trigger('change');


    // Ship To
    $('#ddlShipFrom1').val('').trigger('change');
    $('#ddlShipFromAddress').val('').trigger('change');
    $('#TxtAdd1SF').val('');
    $('#TxtAdd2SF').val('');
    $('#TxtAdd3SF').val('');
    $('#ddlCitySF').val('').trigger('change');
    $('#ddlStateSF').val('').trigger('change');
    $('#TxtPincodeSF').val('');
    $('#TxtGSTNoSF').val('');

    // Remarks
    $('#TxtRemarks').val('');
    $('#ddlInputType').val('');

    // Logistic Details
    $('#ddlTransportName').val('').trigger('change');
    $('#txtVehicleNo').val('');
    $('#txtContainerNo').val('');
    $('#txtGRNo').val('');
    $('#DtGRDate').val('');
    $('#chkGRDate').prop('checked', false);
    $('#ChkSealedVehicle').prop('checked', false);


    // Freight
    $('#NumFreightPay').val('');
    $('#NumFrtTax1').val('');
    $('#NumFrtTax2').val('');
    $('#TxtFrtPayNarration').val('');

    // Reference
    $('#TxtReason').val('');

    // Payment
    $('#ddlPayment').val('').trigger('change');

    // Hold Date
    $('#DtHoldDate').val('');
    $('#chkHoldDate').prop('checked', false);
}

//----------------------- HEADER DATA BY MRN NO ---------------
function LoadMRNData(vType, vNo) {
    $.ajax({
        url: '/PurchaseBillPassEntry/GetPurchaseDetailsByMRN',
        type: 'GET',
        data: { vType: vType, vNo: vNo },
        dataType: 'json',
        success: function (response) {
            if (response.success) {
                const data = response.data;
                const currentDate = getCurrentDateYMD();

                $('#TxtBillNo').val(data.bilL_NO || '');
                setDateControl(data.bilL_DATE, '#DtBillDate', '#chkBillDate');

                $('#TxtChallanNo').val(data.chalL_NO || '');
                setDateControl(data.chalL_DATE, '#DtChDate', '#chkChDate');
                //==============WayBill Details
                $('#TxtWaybillNo').val(data.waybilL_NO || '');
                $('#TxtWayBillInvNo').val(data.ewB_INVNO || '');
                if (data.ewB_DATE || data.ewB_DATE !== "") {
                    $('#DtWaybillDate').val(formatDateYMD(data.ewB_DATE));
                }

                if (data.ewB_EXPDATE || data.ewB_EXPDATE !== "") {
                    $('#DtWaybillExpiry').val(formatDateYMD(data.ewB_EXPDATE));
                }


                $('#NumExRate').val(data.excH_RATE || 0);

                loadPartyDrCrAcList('#ddlCreditAC', data.partY_CODE);
                //================Bill Details
                loadPartyListNatureSupplier('#ddlBillFrom', false, data.partY_CODE);
                $('#TxtAdd1PD').val(data.bilL_ADD1 || '');
                $('#TxtAdd2PD').val(data.bilL_ADD2 || '');
                $('#TxtAdd3PD').val(data.bilL_ADD3 || '');
                loadCityList('#ddlCityPD', data.bilL_CITY);
                $('#NumPincodeBL').val(data.bilL_PINCODE || '');
                $('#TxtGSTNo').val(data.bilL_GST || '');
                loadStateList('#ddlStateSF', data.bilL_STATE);

                //===============Ship Details
                loadPartyListNatureSupplier('#ddlShipFrom1', true, data.shiP_CODE);
                $('#TxtAdd1SF').val(data.shiP_ADD1 || '');
                $('#TxtAdd2SF').val(data.shiP_ADD2 || '');
                $('#TxtAdd3SF').val(data.shiP_ADD3 || '');
                loadCityList('#ddlCitySF', data.shiP_CITY);
                $('#TxtPincodeSF').val(data.shiP_PINCODE || '');
                $('#TxtGSTNoSF').val(data.shiP_GST || '');
                loadStateList('#ddlStatePD', data.shiP_STATE)

                $('#txtRemarks').val(data.remarks || '');

                //===================Transport===========
                const transportCode = parseInt(data.transporT_CODE) || 0;
                const transportName = (data.transporT_NAME || "").trim();

                if (transportCode === 0 && transportName === "") {

                    // Clear selection
                    $('#ddlTransportName').val(null).trigger('change');

                }
                else if (transportCode === 0) {

                    // Add temporary option if it doesn't already exist
                    if ($('#ddlTransportName option[value="' + transportName + '"]').length === 0) {
                        $('#ddlTransportName').append(
                            $('<option>', {
                                value: transportName,
                                text: transportName
                            })
                        );
                    }

                    // Select the temporary option
                    $('#ddlTransportName')
                        .val(transportName)
                        .trigger('change');

                }
                else {

                    // Select by Transport Code
                    $('#ddlTransportName')
                        .val(transportCode.toString())
                        .trigger('change');

                }

                //==============Logistic Details=======
                $('#txtVehicleNo').val(data.trucK_NO || '');
                $('#txtContainerNo').val(data.containeR_NO || '');
                $('#txtGRNo').val(data.gR_NO || '');
                setDateControl(data.gR_DATE, '#DtGRDate', '#chkGRDate');

                $('#NumFreightPay').val(data.frtpaY_AMT || 0);
                $('#NumFrtTax1').val(data.frtpaY_TAXPER || 0);
                $('#NumFrtTax2').val(data.frtpaY_TAX || 0);
                $('#TxtFrtPayNarration').val(data.frtpaY_NAR || '');

                $('#ddlPayment').val(data.holD_PAY || '');
                $('#TxtReason').val(data.holD_REASON || '');
                setDateControl(data.holD_DATE, '#DtHoldDate', '#chkHoldDate');

                const paymentVal = $('#ddlPayment').val() || '';
                if (paymentVal.toUpperCase() === "HOLD") {
                    $('#ddlPayment').prop('disabled', true);
                    $('#DtHoldDate').prop('disabled', true);
                    $('#chkHoldDate').prop('disabled', true);
                } else {

                    $('#ddlPayment').prop('disabled', false);
                    $('#DtHoldDate').prop('disabled', false);
                    $('#chkHoldDate').prop('disabled', false);

                }


                $('#ddlInputType').val("GST NA");

                $('#NumTcs1').val(data.tcS_PER || 0);
                $('#NumTcs2').val(data.tcS_AMT || 0);

                GetPurchaseItemsByMRN(vType, vNo);
            }
            else {
                showToast(response.message, { type: "error" });
            }
        },
        error: function (error) {
            showToast(error, { type: "error" });
        }
    });
}

//----------------------- ITEM DETAILS BY MRN NO ---------------
async function GetPurchaseItemsByMRN(vType, vNo) {

    try {
        const response = await $.ajax({
            url: '/PurchaseBillPassEntry/GetPurchaseItemsByMRN',
            type: 'GET',
            dataType: 'json',
            data: {
                vType,
                vNo
            }
        });

        if (!response.success) {
            showToast(response.message, { type: "error" });
            return;
        }
        console.log("MRN Items: ", response.data);
        $("#tblItemRecordPBPE tbody").empty();

        for (const item of response.data) {

            item.rcM_YN = "NO";
            item.inpuT_YN = "YES";

            try {
                // Get PO Rates
                const rateData = await GetItemOrderRatesByPO(
                    item.pO_TYPE,
                    item.pO_NO,
                    item.iteM_CODE
                );

                item.polanD_RATE = rateData.landRate;
                item.pO_RATE = rateData.rate;

                // Add Row
                //const $row = await addNewRowBelow(item);
                await addNewRowBelow(item);

                let $row = $("#tblItemRecordPBPE tbody tr:last");

                // Calculations
                await calculateAmt($row, item.iteM_CODE);

                calculateTax($row, item.iteM_CODE);

                calculateLandAmount($row, item.iteM_CODE);

                // HSN & Qty Check
                const result = await GetHsnCodeAndQty(
                    item.iteM_CODE,
                    item.pO_TYPE,
                    item.pO_NO
                );

                if (result.hsnCode !== item.hsN_CODE) {
                    $row.find(".hsn-code")
                        .css("background-color", "#f8d7da");
                }

                const recdQty =
                    parseFloat($row.find(".recd-qty").val()) || 0;

                if (parseFloat(result.qty) !== recdQty) {
                    $row.find(".recd-qty")
                        .css("background-color", "#f8d7da");
                }

            }
            catch (err) {

                console.error(err);
                showToast(err.message || err, { type: "error" });

            }
        }

        // Calculate once after all rows are loaded
        calculateItemTotals();

        await CalcDrCrNote();

    }
    catch (xhr) {

        console.error(xhr);

        showToast(
            xhr.responseJSON?.message ||
            xhr.responseText ||
            "Unable to load Purchase Items.",
            { type: "error" }
        );
    }
}

function GetItemOrderRatesByPO(poType, poNo, itemCode) {

    return $.ajax({
        url: '/PurchaseBillPassEntry/GetItemOrderRatesByPO',
        type: 'GET',
        dataType: 'json',
        data: {
            poType: poType,
            poNo: poNo,
            itemCode: itemCode
        }
    }).then(function (res) {

        if (res.success) {
            return {
                landRate: res.landRate,
                rate: res.rate,
                exists: res.exists
            };
        } else {
            throw new Error(res.message || "No data found");
        }
    });
}

//--------------------- ITEM AMOUNTS CALCULATIONS ----------------
async function calculateAmt($row, itemCode) {
    //=====================
    //if (isReadOnly) {
    //    return;
    //}

    // ---------- INPUTS ----------
    let usdRate = parseFloat($row.find('.usd-rate').val()) || 0;

    let billQty = parseFloat($row.find('.bill-qty').val()) || 0;
    let rate = parseFloat($row.find('.rate').val()) || 0;
    let exRate = parseFloat($row.find('.exch-rate').val()) || 0;

    let pack = parseFloat($row.find('.pack-amt').val()) || 0;
    let packPer = parseFloat($row.find('.pack-per').val()) || 0;
    let disc = parseFloat($row.find('.disc-amt').val()) || 0;
    let discPer = parseFloat($row.find('.disc-per').val()) || 0;
    let cess = parseFloat($row.find('.cess-amt').val()) || 0;
    let cessPer = parseFloat($row.find('.cess-per').val()) || 0;
    let vat = parseFloat($row.find('.vat-amt').val()) || 0;
    let vatPer = parseFloat($row.find('.vat-per').val()) || 0;

    let taxCode = $row.find('.tax-code').val() || 0;

    let cgst = parseFloat($row.find('.cgst-amt').val()) || 0;
    let sgst = parseFloat($row.find('.sgst-amt').val()) || 0;
    let igst = parseFloat($row.find('.igst-amt').val()) || 0;
    let otherAmt = parseFloat($row.find('.oth-amt').val()) || 0;

    //let itemCode = $row.find('.item-name').val() ;
    let pob = 0;
    let packAmt = 0;
    let discount = 0;
    let cessAmt = 0;
    let vatAmt = 0;
    let net = 0;
    let basicAmt = 0;

    if (itemCode > 0) {
        // ------------RATE --------------
        if (exRate > 0) {
            rate = usdRate * exRate;
            $row.find('.rate').val(rate.toFixed(4));
        }
        // ---------- BASIC ----------
        basicAmt = billQty * rate;

        // ---------- DISCOUNT ----------
        discount = (discPer > 0) ? (basicAmt * discPer / 100) : disc;

        // ---------- PACKING ----------
        if (taxCode > 0) {
            let res = await GetPackOnBasic(taxCode);
            pob = res.success ? res.data : 0;
        }
        if (pob === 1) {
            packAmt = (packPer > 0) ? (basicAmt * packPer / 100) : pack;
        }
        else {
            packAmt = (packPer > 0) ? ((basicAmt - discount) * packPer / 100) : pack;
        }

        // ---------- TAXABLE VALUE ----------
        let grossAmt = basicAmt + packAmt - discount;

        // ---------- CESS/VAT ----------
        cessAmt = (cessPer > 0) ? grossAmt * cessPer / 100 : cess;

        vatAmt = (vatPer > 0) ? grossAmt * vatPer / 100 : vat;

        // ---------- NET AMOUNT ----------
        net = grossAmt + cgst + sgst + igst + cessAmt + vatAmt + otherAmt;

    }
    // ---------- UPDATE UI ----------
    $row.find('.amount').val(basicAmt.toFixed(2));
    $row.find('.pack-amt').val(packAmt.toFixed(4));
    $row.find('.disc-amt').val(discount.toFixed(4));

    $row.find('.cess-amt').val(cessAmt.toFixed(4));
    $row.find('.vat-amt').val(vatAmt.toFixed(4));

    $row.find('.net-amt').val(net.toFixed(4));
}

function GetPackOnBasic(code) {

    return $.ajax({
        url: '/PurchaseBillPassEntry/GetPackOnBasic',
        type: 'GET',
        dataType: 'json',
        data: {
            code: code
        }
    });
}

//--------------------- ITEM TAX CALCULATIONS ----------------
function calculateTax($row, itemCode) {

    //If specialusercontrol.lblAction.Tag = 2 Then Return
    //        If recal = False Then Return

    const amount = parseFloat($row.find(".amount").val()) || 0;
    const packAmt = parseFloat($row.find(".pack-amt").val()) || 0;
    const discAmt = parseFloat($row.find(".disc-amt").val()) || 0;

    const cgstPer = parseFloat($row.find(".cgst-per").val()) || 0;
    const sgstPer = parseFloat($row.find(".sgst-per").val()) || 0;
    const igstPer = parseFloat($row.find(".igst-per").val()) || 0;

    const cessAmt = parseFloat($row.find(".cess-amt").val()) || 0;
    const vatAmt = parseFloat($row.find(".vat-amt").val()) || 0;
    const otherAmt = parseFloat($row.find(".oth-amt").val()) || 0;

    let grossAmt = 0;

    let cgst = parseFloat($row.find(".cgst-amt").val()) || 0;
    let sgst = parseFloat($row.find(".sgst-amt").val()) || 0;
    let igst = parseFloat($row.find(".igst-amt").val()) || 0;

    if (itemCode > 0) {
        grossAmt = amount + packAmt - discAmt;
    }
    // Recalculate only if user is not editing these fields
    //if (currentField !== "cgst-amt" && currentField !== "sgst-amt") {
    cgst = grossAmt * cgstPer / 100;
    sgst = grossAmt * sgstPer / 100;

    $row.find(".cgst-amt").val(cgst.toFixed(4));
    $row.find(".sgst-amt").val(sgst.toFixed(4));
    //}

    //if (currentField !== "igst-amt") {
    igst = grossAmt * igstPer / 100;
    $row.find(".igst-amt").val(igst.toFixed(4));
    //}

    const netAmt =
        grossAmt +
        cgst +
        sgst +
        igst +
        cessAmt +
        vatAmt +
        otherAmt;

    $row.find(".net-amt").val(netAmt.toFixed(4));

    // Equivalent of ReadOnly property
    if (cgst > 0) {
        $row.find(".cgst-amt").prop("readonly", false);
        $row.find(".sgst-amt").prop("readonly", false);
    } else {
        $row.find(".cgst-amt").prop("readonly", true);
        $row.find(".sgst-amt").prop("readonly", true);
    }

    if (igst > 0) {
        $row.find(".igst-amt").prop("readonly", false);
    } else {
        $row.find(".igst-amt").prop("readonly", true);
    }
}

function GetHsnCodeAndQty(itemCode, poType, poNo) {

    return new Promise(function (resolve, reject) {

        $.ajax({
            url: '/PurchaseBillPassEntry/GetHsnCodeAndQty',
            type: 'GET',
            dataType: 'json',
            data: {
                itemCode: itemCode,
                poType: poType,
                poNo: poNo
            },
            success: function (response) {

                if (response.success) {
                    resolve(response.data);
                }
                else {
                    reject(response.message);
                }
            },
            error: function (xhr, status, error) {
                reject(error || xhr.responseText);
            }
        });

    });

}

//--------------------- ITEM TOTAL CALCULATIONS ----------------
function calculateItemTotals() {

    let totals = {
        recQty: 0,
        billQty: 0,
        amount: 0,
        packing: 0,
        discount: 0,
        cgst: 0,
        sgst: 0,
        igst: 0,
        cess: 0,
        vat: 0,
        other: 0,
        netAmt: 0
    };

    $("#tblItemRecordPBPE tbody tr").each(function () {

        const row = $(this);

        totals.recQty += parseFloat(row.find(".recd-qty").val()) || 0;
        totals.billQty += parseFloat(row.find(".bill-qty").val()) || 0;
        totals.amount += parseFloat(row.find(".amount").val()) || 0;
        totals.packing += parseFloat(row.find(".pack-amt").val()) || 0;
        totals.discount += parseFloat(row.find(".disc-amt").val()) || 0;
        totals.cgst += parseFloat(row.find(".cgst-amt").val()) || 0;
        totals.sgst += parseFloat(row.find(".sgst-amt").val()) || 0;
        totals.igst += parseFloat(row.find(".igst-amt").val()) || 0;
        totals.cess += parseFloat(row.find(".cess-amt").val()) || 0;
        totals.vat += parseFloat(row.find(".vat-amt").val()) || 0;
        totals.other += parseFloat(row.find(".oth-amt").val()) || 0;
        totals.netAmt += parseFloat(row.find(".net-amt").val()) || 0;

    });

    //Display Totals
    $("#NumReceivedQty").val(totals.recQty.toFixed(2));
    $("#NumBillQty").val(totals.billQty.toFixed(2));
    $("#NumAmount").val(totals.amount.toFixed(2));
    $("#NumPacking").val(totals.packing.toFixed(2));
    $("#NumDiscount").val(totals.discount.toFixed(2));

    $("#NumCgst").val(totals.cgst.toFixed(2));
    $("#NumSgst").val(totals.sgst.toFixed(2));
    $("#NumIgst").val(totals.igst.toFixed(2));
    $("#NumVat").val(totals.vat.toFixed(2));
    $("#NumCess").val(totals.cess.toFixed(2));

    $("#NumOtherAmt").val(totals.other.toFixed(2));
    //TCS Amount
    const tcsAmt = parseFloat($("#NumTcs2").val()) || 0;

    //Sub Total
    const subTotal = totals.netAmt + tcsAmt;

    $("#NumSubTotal").val(subTotal.toFixed(2));

    //Round Off
    const rounded = Math.round(subTotal);
    const roundOff = rounded - subTotal;

    $("#NumRoundOff").val(roundOff.toFixed(2));
    $("#TxtNetAmount").val(rounded.toFixed(2));
    $("#NumNetAmount").val(rounded.toFixed(2));

    //TDS 194Q
    const tdsPer = parseFloat($("#TxtTds194q1").val()) || 0;

    const tds194Q = roundAwayFromZero(
        ((totals.amount + totals.packing - totals.discount) * tdsPer) / 100
    );

    $("#TxtTds194q2").val(tds194Q);
}

//--------------------- ITEM LAND AMOUnT CALCULATIONS ----------------
function calculateLandAmount($row, itemCode) {

    //$("#tblItemRecordPBPE tbody tr").each(function () {

    //const $row = $(this);

    //const itemCode = $row.find(".item-name").val() || 0;
    const billQty = parseFloat($row.find(".bill-qty").val()) || 0;
    const rate = parseFloat($row.find(".rate").val()) || 0;

    const packAmt = parseFloat($row.find(".pack-amt").val()) || 0;
    const discAmt = parseFloat($row.find(".disc-amt").val()) || 0;

    const cgst = parseFloat($row.find(".cgst-amt").val()) || 0;
    const sgst = parseFloat($row.find(".sgst-amt").val()) || 0;
    const igst = parseFloat($row.find(".igst-amt").val()) || 0;
    const cess = parseFloat($row.find(".cess-amt").val()) || 0;

    let packRate = 0;
    let discRate = 0;
    let taxRate = 0;

    if (itemCode > 0) {
        if (billQty > 0) {
            packRate = packAmt / billQty;
            discRate = discAmt / billQty;
            taxRate = (cgst + sgst + igst + cess) / billQty;
        }
    }

    const landRate = rate + packRate - discRate + taxRate;
    const landAmt = billQty * landRate;


    $row.find(".land-rate").val(landRate.toFixed(2));
    $row.find(".land-amt").val(landAmt.toFixed(2));

    //});

}

//------------------ DR/CR NOTE Request --------------

function GetCrDrNoteRequest(isFreightTaxChanged = false) {
    const billFrom = $("#ddlBillFrom");
    const billFromEl = billFrom[0];

    const request = {
        vType: $("#ddlDocType").val(),
        vNo: $("#NumDocNo").val(),
        vDate: $("#DtDocDate").val(),
        billToPartyCode: parseInt(billFromEl.value) || 0,
        billToPartyName: billFromEl.options[billFromEl.selectedIndex]?.text.trim() || "",
        txtQualityDiffDebitAmt: parseFloat($("#TxtQualityDiffDebitAmt").val()) || 0,
        txtQualityDiffDebitTax: parseFloat($("#TxtQualityDiffDebitTax").val()) || 0,
        items: [],

        totalRcvdQty: parseFloat($('#NumReceivedQty').val()) || 0,
        totalBillQty: parseFloat($('#NumBillQty').val()) || 0,
        totalNetAmt: parseFloat($('#NumNetAmount').val()) || 0,
        totalTCSAmt: parseFloat($('#NumTcs2').val()) || 0,
        totalPackingAmt: parseFloat($('#NumPacking').val()) || 0,
        isSealedVehicle: $('#ChkSealedVehicle').is(':checked'),

        mrnType: $("#TxtMRNNo1").val() || "",
        mrnNo: $("#TxtMRNNo2").val() || 0,

        inputType: $("#ddlInputType").val() || "",
        FreightAmountPay: parseFloat($('#NumFreightPay').val()) || 0,
        FreightTax: parseFloat($('#NumFrtTax2').val()) || 0,
        FreightTaxPercent: parseFloat($('#NumFrtTax1').val()) || 0,
        isFreightTaxChanged: isFreightTaxChanged,
    };


    $("#tblItemRecordPBPE tbody tr").each(function () {

        const row = this;

        const item = row.querySelector(".item-name");
        const unit = row.querySelector(".uom-name");
        const amount = row.querySelector(".amount");
        const recdQty = row.querySelector(".recd-qty");
        const billQty = row.querySelector(".bill-qty");

        const cgst = row.querySelector(".cgst-per");
        const sgst = row.querySelector(".sgst-per");
        const igst = row.querySelector(".igst-per");

        const poType = row.querySelector(".po-type");
        const poNo = row.querySelector(".po-no");

        const landRate = row.querySelector(".land-rate");
        const poRate = row.querySelector(".po-rate");
        const poLandRate = row.querySelector(".poland-rate");

        request.items.push({
            itemCode: parseInt(item.value) || 0,
            itemName: item.options[item.selectedIndex]?.text || "",
            unit: unit.value,

            amount: parseFloat(amount.value) || 0,

            recdQty: parseFloat(recdQty.value) || 0,
            billQty: parseFloat(billQty.value) || 0,

            cgstPer: parseFloat(cgst.value) || 0,
            sgstPer: parseFloat(sgst.value) || 0,
            igstPer: parseFloat(igst.value) || 0,

            poType: poType.value,
            poNo: parseInt(poNo.value) || 0,

            landRate: parseFloat(landRate.value) || 0,
            poRate: parseFloat(poRate.value) || 0,
            poLandRate: parseFloat(poLandRate.value) || 0
        });

    });

    return request;
}

//----------------- BIND CR/DR NOTE RESPONSE TO UI -------------
function BindDebitNoteResponse(result) {
    //------------ Rate Debit --------------
    $("#TxtRateDiffDebitAmt").val((result.rateDiffDebitAmt || 0).toFixed(2));
    $("#TxtRateDiffDebitTax").val((result.rateDiffDebitTax || 0).toFixed(2));
    $("#TxtRateDiffDebitNarration").val(result.rateDiffDebitNarration);

    //------------ Quality Debit -------------
    $("#TxtQualityDiffDebitAmt").val((result.qualityDiffDebitAmt || 0).toFixed(2));
    $("#TxtQualityDiffDebitTax").val((result.qualityDiffDebitTax || 0).toFixed(2));
    $("#TxtQualityDiffDebitNarration").val(result.qualityDiffDebitNarration);

    //-------------- Weight Debit ---------------
    $("#TxtWeightDebitAmt").val((result.weightDiffDebitAmt || 0).toFixed(2));
    $("#TxtWeightDebitTax").val((result.weightDiffDebitTax || 0).toFixed(2));
    $("#TxtWeightDebitNarration").val(result.weightDiffDebitNarration);

    //-------------- QC Debit -------------
    $("#TxtQCDebitNoteAmt").val((result.qcDebitAmt || 0).toFixed(2));
    $("#TxtQCDebitNoteTax").val((result.qcDebitTax || 0).toFixed(2));
    $("#TxtQCDebitNarration").val(result.qcDebitNarration);
}

//------------- CALCULATE CR/DR NOTE -------------
async function CalcDrCrNote() {

    const request = GetCrDrNoteRequest();

    try {
        const result = await $.ajax({
            url: "/PurchaseBillPassEntry/CalculateDebitNote",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(request)
        });

        if (result.warnings && result.warnings.length > 0) {
            result.warnings.forEach(function (message) {
                showToast(message, { type: "warning" });
            });
        }

        BindDebitNoteResponse(result);
    }
    catch (ex) {

        console.error(ex);

        showToast(
            "Unable to calculate Debit Note.",
            { type: "error" });
    }
}

//------------ CALCULATE FREIGHT -------------
async function CalcFreightAndCrDr(request) {

    //const request = GetCrDrNoteRequest();

    try {

        const result = await $.ajax({
            url: "/PurchaseBillPassEntry/CalculateFrieght",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify(request)
        });

        if (result.warnings && result.warnings.length > 0) {
            result.warnings.forEach(function (message) {
                showToast(message, { type: "warning" });
            });
        }

        BindDebitNoteResponse(result);

        $('#NumFrtTax2').val(result.txtFrtTaxVal);
        loadPartyDrCrAcList('#ddlFreightDebitAC', result.frtDrAcCode || 0);

    }
    catch (ex) {

        console.error(ex);

        showToast(
            "Unable to calculate Debit Note.",
            { type: "error" });
    }
}

//----------- ROW PROCESSING HELPER FOR EVENTS ----------
async function processRow($row, { calculateAmount = false, calculateTaxes = false } = {}) {

    const itemCode = Number($row.find(".item-name").val()) || 0;

    if (calculateAmount) {
        await calculateAmt($row, itemCode);
    }

    if (calculateTaxes) {
        calculateTax($row, itemCode);
    }

    calculateItemTotals();
    calculateLandAmount($row, itemCode);
    await CalcDrCrNote();
}

//----------- CALCULATE GROSS AMOUNT HELPER FOR EVENTS ----------
function CalculateGrossAmount() {
    let gross = 0;

    $('#tblItemRecordPBPE tbody tr').each(function () {

        const $row = $(this);

        gross +=
            (parseFloat($row.find('.amount').val()) || 0) +
            (parseFloat($row.find('.pack-amt').val()) || 0) +
            (parseFloat($row.find('.cgst-amt').val()) || 0) +
            (parseFloat($row.find('.sgst-amt').val()) || 0) +
            (parseFloat($row.find('.igst-amt').val()) || 0) +
            (parseFloat($row.find('.oth-amt').val()) || 0) -
            (parseFloat($row.find('.disc-amt').val()) || 0);

    });

    return gross;
}

//----------- TOTAL AMOUNT EVENTS HELPER ----------
function distributeAmount(totalAmt, totalBaseAmt, amountClass, percentClass = null) {

    let cumulativeAmt = 0;

    const $rows = $('#tblItemRecordPBPE tbody tr');

    // Reset
    $rows.each(function () {

        if (percentClass) {
            $(this).find(percentClass).val(0);
        }

        $(this).find(amountClass).val(0);
    });

    // Distribute
    $rows.each(function (index) {

        const $row = $(this);

        const itemCode = parseInt($row.find('.item-name').val()) || 0;

        if (itemCode <= 0)
            return;

        let rowAmt = 0;

        if (index === $rows.length - 1) {

            rowAmt = +(totalAmt - cumulativeAmt).toFixed(2);

        } else {

            const amount = parseFloat($row.find('.amount').val()) || 0;

            rowAmt = +((totalAmt / totalBaseAmt) * amount).toFixed(2);

            cumulativeAmt += rowAmt;
        }

        $row.find(amountClass).val(rowAmt.toFixed(2));

        calculateAmt($row, itemCode);
        calculateTax($row, itemCode);

    });

    calculateItemTotals();
    calculateLandAmount();
    CalcDrCrNote();
}

//----------- TOTALS CHANGE HELPER --------
function bindDistributionChange(inputSelector, amountColumn, perColumn = null) {

    $(inputSelector).on('change', function () {
        if (isLoadForEdit) return;
        const total = parseFloat($(this).val()) || 0;
        const totalAmount = parseFloat($('#NumAmount').val()) || 0;

        distributeAmount(
            total,
            totalAmount,
            amountColumn,
            perColumn
        );

        $(this).val(total.toFixed(2));

    });

}
//----------- GET EXISTING TDS ----------
async function checkExistingTDS() {

    const billNo = `${$('.po-type').first().val() || ''}${parseInt($('.po-no').first().val()) || 0}`;
    const drCode = parseInt($('#ddlBillFrom').val()) || 0;

    return await $.ajax({
        url: '/PurchaseBillPassEntry/CheckExistingTDS',
        type: 'POST',
        data: {
            billNo: billNo,
            drCode: drCode
        }
    });
}

//----------- CALCULATE TDS AMOUNT --------
async function calculateTDSAmount(sourceTextbox, targetTextbox) {

    const response = await checkExistingTDS();

    if (response.totTDS > 0) {
        const message = `TDS already deducted of Rs. ${response.totTDS}`;
        showToast(message, { type: "warning" });
    }

    const totalAmt = parseFloat($('#NumAmount').val()) || 0;
    const totalPacking = parseFloat($('#NumPacking').val()) || 0;
    const totalDisc = parseFloat($('#NumDiscount').val()) || 0;

    const tdsPer = parseFloat($(sourceTextbox).val()) || 0;

    const tdsAmt = Math.round(
        (totalAmt + totalPacking - totalDisc) * tdsPer / 100
    );

    $(targetTextbox).val(tdsAmt);
}

//----------------- Get Freight Cr Ac Code -----------
function getFrtCrAcCodeByTransCode(transportCode) {

    $.ajax({
        url: '/PurchaseBillPassEntry/GetFrtCrAcByTransCode',
        type: 'GET',
        data: {
            transportCode: transportCode
        },
        success: function (response) {

            if (!response.success)
                return;

            // Freight Credit A/C
            $('#ddlFreightCreditAC')
                .val(response.partyCode)
                .trigger('change');
        },
        error: function () {
            showToast("Unable to load transport details.", { type: "error" });
        }
    });

}

//--------------- Exchange Rate change ---------------
async function onExchangeRateChanged() {

    const exRate = parseFloat($('#NumExRate').val()) || 0;
    if (exRate <= 0)
        return;

    const rows = $('#tblItemRecordPBPE tbody tr');

    for (const row of rows) {

        const $row = $(row);

        const itemCode = parseInt($row.find('.item-name').val()) || 0;

        if (itemCode > 0) {

            $row.find('.exch-rate').val(exRate.toFixed(2));

            await calculateAmt($row, itemCode);
            calculateTax($row, itemCode);
        }
    }

    calculateItemTotals();
    calculateLandAmount($row, itemCode);
    await CalcDrCrNote();
}

//-------------- COLLECT DATA FOR SAVE & UPDATE ----------
async function collectPurchaseBillData() {

    //Header Details
    const headerData = {
        V_TYPE: $('#ddlDocType').val() || "",
        V_DATE: parseNullableDate($('#DtDocDate').val()) || null,
        V_NO: parseInt($('#NumDocNo').val()) || 0,

        REF_TYPE: $('#TxtMRNNo1').val() || "",
        REF_NO: parseInt($('#TxtMRNNo2').val()) || 0,

        //------------ Bill Details -----------
        PARTY_CODE: parseInt($('#ddlBillFrom').val()) || 0,
        BILL_ADDRESSID: parseInt($('#ddlBillFromAddress').val()) || 0,
        BILL_ADD1: $('#TxtAdd1PD').val() || "",
        BILL_ADD2: $('#TxtAdd2PD').val() || "",
        BILL_ADD3: $('#TxtAdd3PD').val() || "",
        BILL_CITY: parseInt($('#ddlCityPD').val()) || 0,
        BILL_GST: $('#TxtGSTNo').val() || "",
        BILL_PINCODE: $('#NumPincodeBL').val() || "",

        DISP_ADDRESS: $('#TxtDispFromAdd').val() || "",
        DISP_CITY: parseInt($('#ddlDispCity').val()) || 0,

        CURRENCY: parseInt($('#ddlCurrency').val()) || 0,

        //------------ Ship Details -----------
        SHIP_CODE: parseInt($('#ddlShipFrom1').val()) || 0,
        SHIP_ADDRESSID: parseInt($('#ddlShipFromAddress').val()) || 0,
        SHIP_ADD1: $('#TxtAdd1SF').val() || "",
        SHIP_ADD2: $('#TxtAdd2SF').val() || "",
        SHIP_ADD3: $('#TxtAdd3SF').val() || "",
        SHIP_CITY: parseInt($('#ddlCitySF').val()) || 0,
        SHIP_GST: $('#TxtGSTNoSF').val() || "",
        SHIP_PINCODE: $('#TxtPincodeSF').val() || "",

        //------------ Document Details -----------
        BILL_NO: $('#TxtBillNo').val() || "",
        //BILL_DATE: parseNullableDate($('#DtBillDate').val()) || null,
        BILL_DATE: getOptionalDate('#chkBillDate', '#DtBillDate'),

        CHALL_NO: $('#TxtChallanNo').val() || "",
        //CHALL_DATE: parseNullableDate($('#DtChDate').val()) || null,
        CHALL_DATE: getOptionalDate('#chkChDate', '#DtChDate'),

        BL_NO: $('#TxtBLNo').val() || "",
        //BL_DT: parseNullableDate($('#DtBLDate').val()) || null,
        BL_DT: getOptionalDate('#chkBLDate', '#DtBLDate'),

        WAYBILL_NO: $('#TxtWaybillNo').val() || "",
        EWB_DATE: $('#DtWaybillDate').val() || null,
        EWB_INVNO: $('#TxtWayBillInvNo').val() || "",
        EWB_EXPDATE: $('#DtWaybillExpiry').val() || null,

        DEBIT_AC: parseInt($('#ddlDebitAC').val()) || 0,
        CREDIT_AC: parseInt($('#ddlCreditAC').val()) || 0,

        INPUT_TYPE: $('#ddlInputType').val().trim() || '',
        STATUS: parseInt($('#ddlStatus').val()) || 0,
        EXCH_RATE: parseFloat($('#NumExRate').val()) || 0,
        REMARKS: $('#txtRemarks').val() || "",
        NAMOUNT: parseFloat($('#TxtNetAmount').val()) || 0,

        //------------ Item Total -----------
        RECD_QTY: parseFloat($('#NumReceivedQty').val()) || 0,
        BILL_QTY: parseFloat($('#NumBillQty').val()) || 0,
        AMOUNT: parseFloat($('#NumAmount').val()) || 0,
        DISC_AMT: parseFloat($('#NumDiscount').val()) || 0,
        PACK_AMT: parseFloat($('#NumPacking').val()) || 0,
        CGST_AMT: parseFloat($('#NumCgst').val()) || 0,
        SGST_AMT: parseFloat($('#NumSgst').val()) || 0,
        IGST_AMT: parseFloat($('#NumIgst').val()) || 0,
        CESS_AMT: parseFloat($('#NumCess').val()) || 0,
        VAT_AMT: parseFloat($('#NumVat').val()) || 0,
        OTH_AMT: parseFloat($('#NumOtherAmt').val()) || 0,
        TCS_PER: parseFloat($('#NumTcs1').val()) || 0,
        TCS_AMT: parseFloat($('#NumTcs2').val()) || 0,
        ROUND_OFF: parseFloat($('#NumRoundOff').val()) || 0,

        TDS_ACT: parseInt($('#ddlTdsAccount').val()) || 0,
        TDS_PER: parseFloat($('#TxtTds1').val()) || 0,
        TDS_AMT: parseFloat($('#TxtTds2').val()) || 0,

        TDS_PER194Q: parseFloat($('#TxtTds194q1').val()) || 0,
        TDS_AMT194Q: parseFloat($('#TxtTds194q2').val()) || 0,

        BANK_RATE: parseFloat($('#TxtBankRate1').val()) || 0,
        BANK_AMT: parseFloat($('#TxtBankRate2').val()) || 0,
        DIFF_AMT: parseFloat($('#TxtDiffAmt').val()) || 0,
        PL_NO: parseInt($('#NumPlNo').val()) || 0,
        //PL_DATE: parseNullableDate($('#DtPlDate').val()) || null,
        PL_DATE: getOptionalDate('#chkPlDate', '#DtPlDate'),
        BILLAMT_USD: parseFloat($('#TxtPartyUsd').val()) || 0,

        //------------- Logistic Details ----------
        TRANSPORT_CODE: parseInt($('#ddlTransportName').val()) || 0,
        TRANSPORT_NAME: $('#ddlTransportName option:selected').text() || "",

        TRUCK_NO: $('#txtVehicleNo').val() || "",
        CONTAINER_NO: $('#txtContainerNo').val() || "",

        GR_NO: $('#txtGRNo').val() || "",
        //GR_DATE: parseNullableDate($('#DtGRDate').val()) || null,
        GR_DATE: getOptionalDate('#chkGRDate', '#DtGRDate'),

        SEALED_VEHICLE: $('#ChkSealedVehicle').is(':checked') ? 1 : 0,

        // Freight
        FRTPAY_AMT: parseFloat($('#NumFreightPay').val()) || 0,
        FRTPAY_TAXPER: parseFloat($('#NumFrtTax1').val()) || 0,
        FRTPAY_TAX: parseFloat($('#NumFrtTax2').val()) || 0,
        FRTPAY_DRAC: parseInt($('#ddlFreightDebitAC').val()) || 0,
        FRTPAY_CRAC: parseInt($('#ddlFreightCreditAC').val()) || 0,
        FRTPAY_NAR: $('#TxtFrtPayNarration').val(),

        FRT_TDSPER: parseFloat($('#NumTDSonFRT1').val()) || 0,
        FRT_TDS: parseFloat($('#NumTDSonFRT2').val()) || 0,

        // Transport GST
        TRP_GSTNO: $('#ddlTransportGSTNo').val() || "",
        TRP_TAXTYPE: $('#ddlTaxType').val() || "",
        TRP_BILLNO: $('#TxtBillNoLD').val() || "",
        //TRP_BILLDATE: parseNullableDate($('#DtBillDateLD').val()) || null,
        FRT_BILLDT: getOptionalDate('#chkBillDateLD', '#DtBillDateLD'),

        // Weigh Bridge
        WB_AMT: parseFloat($('#NumWBAmount').val()) || 0,
        WB_TDSPER: parseFloat($('#NumWBTDS1').val()) || 0,
        WB_TDS: parseFloat($('#NumWBTDS2').val()) || 0,
        WB_DRACT: parseInt($('#ddlWBDebitAC').val()) || 0,
        WB_CRACT: parseInt($('#ddlWBCreditAC').val()) || 0,
        WB_NARR: $('#TxtWBNarration').val(),

        // Unloading
        UL_AMT: parseFloat($('#NumUnloadAmt').val()) || 0,
        UL_TDSPER: parseFloat($('#NumUnloadTDS1').val()) || 0,
        UL_TDS: parseFloat($('#NumUnloadTDS2').val()) || 0,
        UL_DRACT: parseInt($('#ddlUnloadDebitAC').val()) || 0,
        UL_CRACT: parseInt($('#ddlUnloadCreditAC').val()) || 0,
        UL_NARR: $('#TxtUnloadNarration').val() || "",

        //------------- CR/DR Note Details ----------
        DR_FROM_TPT: $('#ChkDebitFromTransporter').is(':checked') ? "YES" : "NO",

        QLT_DR_AMT: parseFloat($('#TxtQualityDiffDebitAmt').val()) || 0,
        QLT_DR_TAX: parseFloat($('#TxtQualityDiffDebitTax').val()) || 0,
        QLT_DR_NAR: $('#TxtQualityDiffDebitNarration').val() || '',

        QLT_CR_AMT: parseFloat($('#TxtQualityCreditNoteAmt').val()) || 0,
        QLT_CR_TAX: parseFloat($('#TxtQualityCreditNoteVal').val()) || 0,
        QLT_CR_NAR: $('#TxtQualityCreditNarration').val() || '',

        RDF_DR_AMT: parseFloat($('#TxtRateDiffDebitAmt').val()) || 0,
        RDF_DR_TAX: parseFloat($('#TxtRateDiffDebitTax').val()) || 0,
        RDF_DR_NAR: $('#TxtRateDiffDebitNarration').val() || '',

        RDF_CR_AMT: parseFloat($('#TxtRateDiffCreditNoteAmt').val()) || 0,
        RDF_CR_TAX: parseFloat($('#TxtRateDiffCreditNoteVal').val()) || 0,
        RDF_CR_NAR: $('#TxtRateDiffCreditNarration').val() || '',

        QTY_DR_AMT: parseFloat($('#TxtWeightDebitAmt').val()) || 0,
        QTY_DR_TAX: parseFloat($('#TxtWeightDebitTax').val()) || 0,
        QTY_DR_NAR: $('#TxtWeightDebitNarration').val() || '',

        QTY_CR_AMT: parseFloat($('#TxtWeightCreditNoteAmt').val()) || 0,
        QTY_CR_TAX: parseFloat($('#TxtWeightCreditNoteVal').val()) || 0,
        QTY_CR_NAR: $('#TxtWeightCreditNarration').val() || '',

        QC_DR_AMT: parseFloat($('#TxtQCDebitNoteAmt').val()) || 0,
        QC_DR_TAX: parseFloat($('#TxtQCDebitNoteTax').val()) || 0,
        QC_DR_NAR: $('#TxtQCDebitNarration').val() || '',

        QC_CR_AMT: parseFloat($('#TxtQCCreditNoteAmt').val()) || 0,
        QC_CR_TAX: parseFloat($('#TxtQCCreditNoteVal').val()) || 0,
        QC_CR_NAR: $('#TxtQCCreditNarration').val() || '',

        OTH_DR_AMT: parseFloat($('#TxtOtherDebitAmt').val()) || 0,
        OTH_DR_TAX: parseFloat($('#TxtOtherDebitTax').val()) || 0,
        OTH_DR_NAR: $('#TxtOtherDebitNarration').val() || '',

        HOLD_PAY: $('#ddlPayment').val() || '',
        HOLD_REASON: $('#TxtReason').val() || '',
        //HOLD_DATE: parseNullableDate($('#DtHoldDate').val()) || null,
        HOLD_DATE: getOptionalDate('#chkHoldDate', '#DtHoldDate'),

        ACTION: rowId ? "UPDATE" : "INSERT",
    };

    //Item Details
    const rowsData = [];

    $('#tblItemRecordPBPE tbody tr').each(function () {

        const row = $(this);
        // Cache controls once
        const item = row.find('.item-name');
        const hsn = row.find('.hsn-code');
        const uomCode = row.find('.uom-code');
        const uomName = row.find('.uom-name');
        const nos = row.find('.nos');
        const recdQty = row.find('.recd-qty');
        const billQty = row.find('.bill-qty');
        const usdRate = row.find('.usd-rate');
        const exchRate = row.find('.exch-rate');
        const rate = row.find('.rate');
        const amount = row.find('.amount');
        const rcm = row.find('.rcm-yn');
        const input = row.find('.input-yn');
        const tax = row.find('.tax-code');
        const packPer = row.find('.pack-per');
        const packAmt = row.find('.pack-amt');
        const discPer = row.find('.disc-per');
        const discAmt = row.find('.disc-amt');
        const cgstPer = row.find('.cgst-per');
        const cgstAmt = row.find('.cgst-amt');
        const sgstPer = row.find('.sgst-per');
        const sgstAmt = row.find('.sgst-amt');
        const igstPer = row.find('.igst-per');
        const igstAmt = row.find('.igst-amt');
        const cessPer = row.find('.cess-per');
        const cessAmt = row.find('.cess-amt');
        const vatPer = row.find('.vat-per');
        const vatAmt = row.find('.vat-amt');
        const othAmt = row.find('.oth-amt');
        const netAmt = row.find('.net-amt');
        const make = row.find('.make-code');
        const dept = row.find('.dept-code');
        const remarks = row.find('.remarks');
        const landRate = row.find('.land-rate');
        const landAmt = row.find('.land-amt');
        const poLandRate = row.find('.poland-rate');
        const poRate = row.find('.po-rate');
        const poType = row.find('.po-type');
        const poNo = row.find('.po-no');
        const kantaType = row.find('.kanta-type');
        const kantaNo = row.find('.kanta-no');
        const reqType = row.find('.req-type');
        const reqNo = row.find('.req-no');
        const refType = row.find('.ref-type');
        const refNo = row.find('.ref-no');
        const drNoteAmt = row.find('.dr-note-amt');
        const crNoteAmt = row.find('.cr-note-amt');
        const qltyDiffDrAmt = row.find('.qlty-diff-dr-amt');
        const rateDiffDrAmt = row.find('.rate-diff-dr-amt');
        const qcDiffDrAmt = row.find('.qc-diff-dr-amt');
        const qtyDiffDrAmt = row.find('.qty-diff-dr-amt');
        const otherDrAmt = row.find('.other-dr-amt');
        rowsData.push({
            ITEM_CODE: parseInt(item.val()) || 0,
            ITEM_NAME: item[0].selectedIndex >= 0
                ? item[0].options[item[0].selectedIndex].text.trim()
                : "",

            HSN_CODE: hsn.val() || "",
            UOM_CODE: parseInt(uomCode.val()) || 0,
            UOM_NAME: uomName.val() || "",

            NOS: parseInt(nos.val()) || 0,
            RECD_QTY: parseFloat(recdQty.val()) || 0,
            BILL_QTY: parseFloat(billQty.val()) || 0,

            USD_RATE: parseFloat(usdRate.val()) || 0,
            EXCH_RATE: parseFloat(exchRate.val()) || 0,
            RATE: parseFloat(rate.val()) || 0,
            AMOUNT: parseFloat(amount.val()) || 0,

            RCM_YN: rcm.val() || "",
            INPUT_YN: input.val() || "",

            TAX_CODE: parseInt(tax.val()) || 0,

            PACK_PER: parseFloat(packPer.val()) || 0,
            PACK_AMT: parseFloat(packAmt.val()) || 0,

            DISC_PER: parseFloat(discPer.val()) || 0,
            DISC_AMT: parseFloat(discAmt.val()) || 0,

            CGST_PER: parseFloat(cgstPer.val()) || 0,
            CGST_AMT: parseFloat(cgstAmt.val()) || 0,

            SGST_PER: parseFloat(sgstPer.val()) || 0,
            SGST_AMT: parseFloat(sgstAmt.val()) || 0,

            IGST_PER: parseFloat(igstPer.val()) || 0,
            IGST_AMT: parseFloat(igstAmt.val()) || 0,

            CESS_PER: parseFloat(cessPer.val()) || 0,
            CESS_AMT: parseFloat(cessAmt.val()) || 0,

            VAT_PER: parseFloat(vatPer.val()) || 0,
            VAT_AMT: parseFloat(vatAmt.val()) || 0,

            OTH_AMT: parseFloat(othAmt.val()) || 0,
            NET_AMT: parseFloat(netAmt.val()) || 0,

            MAKE_CODE: parseInt(make.val()) || 0,
            DEPT_CODE: parseInt(dept.val()) || 0,

            REMARKS: remarks.val() || "",

            LAND_RATE: parseFloat(landRate.val()) || 0,
            LAND_AMT: parseFloat(landAmt.val()) || 0,

            POLAND_RATE: parseFloat(poLandRate.val()) || 0,
            PO_RATE: parseFloat(poRate.val()) || 0,

            PO_TYPE: poType.val() || "",
            PO_NO: parseInt(poNo.val()) || 0,

            KANTA_TYPE: kantaType.val() || "",
            KANTA_NO: parseInt(kantaNo.val()) || 0,

            REQ_TYPE: reqType.val() || "",
            REQ_NO: parseInt(reqNo.val()) || 0,

            REF_TYPE: refType.val() || "",
            REF_NO: parseInt(refNo.val()) || 0,

            //=================Missing in PURCHASE2================
            //DRNOTE_AMT: parseFloat(drNoteAmt.val()) || 0,
            //CRNOTE_AMT: parseFloat(crNoteAmt.val()) || 0,
            //QLTDIFF_DRAMT: parseFloat(qltyDiffDrAmt.val()) || 0,
            //RDIFF_DRAMT: parseFloat(rateDiffDrAmt.val()) || 0,
            //QCDIFF_DRAMT: parseFloat(qcDiffDrAmt.val()) || 0,
            //QTYDIFF_DRAMT: parseFloat(qtyDiffDrAmt.val()) || 0,
            //OTH_DRAMT: parseFloat(otherDrAmt.val()) || 0
        });
    });

    //Attachments
    const Attachement = getUploadedFiles();

    //EPR Attachments
    var EPRAttachments = await collectEPRAttachmentFile();

    console.log("headerData: ", headerData)
    console.log("rowsData: ", rowsData)
    console.log("Attachement: ", Attachement)
    console.log("EPRAttachments: ", EPRAttachments)

    return {
        headerData,
        rowsData,
        Attachement,
        EPRAttachments
    };
}

async function saveUpdateData() {
    console.time("Collect");
    const rowsData = await collectPurchaseBillData();
    console.timeEnd("Collect");

    if (rowsData.length === 0) {
        toastr.warning("Please add at least one row before saving.");
        return;
    }

    const data = {
        header: rowsData.headerData,
        lineRows: rowsData.rowsData,
        Attachement: rowsData.Attachement,
        EPRAttachments: rowsData.EPRAttachments
    };

    console.time("Ajax");
    //3. AJAX Save
    $.ajax({
        url: '/PurchaseBillPassEntry/SavePurchaseBillPassEntry',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (response) {
            if (response.success) {
                toastr.success('Saved successfully!');
                //setTimeout(() => {
                //    window.location.href = '/PurchaseBillPassEntryList/Index';
                //}, 1000);
            } else {
                toastr.error('Error: ' + response.message);
            }
        },
        error: function (xhr, status, error) {
            toastr.error('AJAX error: ' + error);
        }
    });
    console.timeEnd("Ajax");
}

function getOptionalDate(checkboxSelector, dateSelector) {
    return $(checkboxSelector).is(':checked')
        ? (parseNullableDate($(dateSelector).val()) || null)
        : null;
}

function toggleTaxAmountFields(row) {
    const cgstPer = parseFloat(row.find('.cgst-per').val()) || 0;
    const sgstPer = parseFloat(row.find('.sgst-per').val()) || 0;
    const igstPer = parseFloat(row.find('.igst-per').val()) || 0;

    row.find('.cgst-amt').prop('disabled', cgstPer <= 0);
    row.find('.sgst-amt').prop('disabled', sgstPer <= 0);
    row.find('.igst-amt').prop('disabled', igstPer <= 0);
}

//--------------- VALIDATIONS ---------------
async function Validate() {
    let isValid = true;

    const VDateEl = $('#DtDocDate');
    const VTypeEl = $('#ddlDocType');
    const VNoEl = $('#NumDocNo');
    const chkBillDateLDEl = $('#chkBillDateLD');
    const DtBillDateLDEl = $('#DtBillDateLD');
    const inputTypeEl = $('#ddlInputType');
    const billGSTEl = $('#TxtGSTNo');
    const billFromEl = $('#ddlBillFrom');
    const mrnTypeEl = $('#TxtMRNNo1');
    const mrnNoDEl = $('#TxtMRNNo2');
    const tdsPerEl = $("#TxtTds1");
    const tdsAmtEl = $("#TxtTds2");
    const tds194QPerEl = $("#TxtTds194q1");
    const tds194QAmtEl = $("#TxtTds194q2");
    const totalNetAmtEl = $("#NumNetAmount");

    if (!validateRequiredField(VTypeEl, 'Document Type') || !validateRequiredField(VNoEl, 'Document Number')) {
        isValid = false;
        return false;
    }
    //------------- Validate VDate ------------
    const isValidVDate = await checkValidDate();
    if (!isValidVDate) {
        isValid = false;
        return false;
    }

    //------------- Validate Transport Bill Date ----------
    if (chkBillDateLDEl.is(':checked')) {
        const billDate = DtBillDateLDEl.val();
        if (billDate) {
            const selectedDate = new Date(billDate);
            selectedDate.setHours(0, 0, 0, 0);
            const today = new Date();
            today.setHours(0, 0, 0, 0);
            if (selectedDate > today) {
                setInvalid(DtBillDateLDEl, "Transport Bill Date can not be greater than today. Check in Logistic details.");
                isValid = false;
                return false;
            }
        }
    }

    //------------- Validate MRN Date ----------
    const MRNDate = await getPurchaseDate(mrnTypeEl.val(), mrnNoDEl.val());

    const mrnDate = MRNDate ? new Date(MRNDate) : null;
    const invoiceDate = new Date(VDateEl.val());

    mrnDate?.setHours(0, 0, 0, 0);
    invoiceDate.setHours(0, 0, 0, 0);


    if (mrnDate && mrnDate > invoiceDate) {
        showToast(`Invoice Date ${formatDateddmmyyyy(VDateEl.val())} can not be less than MRN Date ${formatDateddmmyyyy(MRNDate)}.`, { type: "warning" });
        isValid = false;
        return false;
    }

    //------------- Validate Input Type != Import ---------
    if (inputTypeEl.val() !== "Import") {
        // Party GSTIN validation (35 Lakh)
        if (billGSTEl.val().trim().length === 0) {
            const totalAmount = await getPartyPurchaseAmount(billFromEl.val(), VTypeEl.val());

            if (parseFloat(totalAmount) || 0 > 3500000) {
                showToast("Invoice cannot be saved because the party does not have a GSTIN yet and the total transaction has reached 35 Lakh in this financial year.", { type: "warning" });
                isValid = false;
                return false;
            }
        }
        // TDS validation (50 Lakh)
        const tdsAmt = parseFloat(tdsAmtEl.val()) || 0;
        const tds194QAmt = parseFloat(tds194QAmtEl.val()) || 0;
        if ((tdsAmt + tds194QAmt) <= 0) {

            const totalTransaction = await getPartyPurchaseAmount(billFromEl.val(), VTypeEl.val(), VNoEl.val(), parseFloat(totalNetAmtEl.val()) || 0);

            if (parseFloat(totalTransaction) > 5000000) {
                showToast(`Invoice cannot be saved because TDS has not been deducted for this party and the total transaction has reached 
                50 Lakh in this financial year.`, { type: "warning" });
                isValid = false;
                return false;
            }
        }
    }

    //------------- Validate TDS 206AB section ---------
    const tdsPer = parseFloat(tdsPerEl.val()) || 0;
    const tds194QPer = parseFloat(tds194QPerEl.val()) || 0;
    const tds206Apply = await getTDS206Apply(billFromEl.val());

    if ((tdsPer + tds194QPer) < 5 && tds206Apply === "Yes") {
        showToast("This party falls under TDS 206AB section, so please deduct TDS above 5%.", { type: "warning" });
        isValid = false;
        return false;
    }

    //----------------- Validate Input Type Existence in Posting Master ---------------
    //--------- Confusion in this validation -----------
    //const postingExists = await isPostingExist(VTypeEl.val());

    //if (!postingExists) {
    //    showToast(`${inputTypeEl.val()} not found in Posting Master.`, { type: "warning" });
    //    inputTypeEl.focus();
    //    isValid = false;
    //    return false;
    //}

    //----------------- Validate Bill No and Challan No ---------------
    const billNo = $('#TxtBillNo').val();
    const challanNo = $('#TxtChallanNo').val();
    if (!billNo && !challanNo) {
        showToast(`Bill No or Challan No is blank!`, { type: "warning" });
        isValid = false;
        return false;
    }

    //----------------- Validate Bill No and Bill Date ---------------
    if (billNo !== "" && !$('#chkBillDate').is(":checked")) {
        setInvalid($('#DtBillDate'), "Bill date is required.");
        isValid = false;
        return false;
    }

    //----------------- Validate Bill Date ---------------
    if ($('#chkBillDate').is(":checked")) {

        const billDateValue = new Date($('#DtBillDate').val());
        const voucherDateValue = new Date(VDateEl.val());

        billDateValue.setHours(0, 0, 0, 0);
        voucherDateValue.setHours(0, 0, 0, 0);

        if (billDateValue > voucherDateValue) {
            setInvalid($('#DtBillDate'), "Bill date cannot be greater than Voucher date.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Challan No and Challan Date ---------------
    if (challanNo !== "" && !$('#chkChDate').is(":checked")) {
        setInvalid($('#DtChDate'), "Challan date is required.");
        isValid = false;
        return false;
    }

    //----------------- Validate Challan Date ---------------
    if ($('#chkChDate').is(":checked")) {

        const chDateValue = new Date($('#DtChDate').val());
        const voucherDateValue = new Date(VDateEl.val());

        chDateValue.setHours(0, 0, 0, 0);
        voucherDateValue.setHours(0, 0, 0, 0);

        if (chDateValue > voucherDateValue) {
            setInvalid($('#DtChDate'), "Challan date cannot be greater than Voucher date.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Debit Account And Credit Account ---------------
    if (!validateRequiredField('#ddlDebitAC', 'Debit Account') || !validateRequiredField('#ddlCreditAC', 'Credit Account')) {
        isValid = false;
        return false;
    }

    //----------------- Validate Debit and Credit Account ---------------
    if ($("#ddlDebitAC").val() !== "" && $("#ddlCreditAC").val() !== "" && $("#ddlDebitAC").val() === $("#ddlCreditAC").val()) {
        setInvalid($("#ddlCreditAC"), "Debit A/c and Credit A/c must be different.");
        isValid = false;
        return false;
    }

    //----------------- Validate Quality Difference Amount ---------------
    if (Number($("#TxtQualityDiffDebitAmt").val()) < Number($("#TxtQualityCreditNoteAmt").val())) {
        setInvalid($("#TxtQualityCreditNoteAmt"), "Quality Diff Credit Amount cannot be more than Quality Diff Debit Amount.");
        isValid = false;
        return false;
    }

    //----------------- Validate Quality Difference Tax ---------------
    if (Number($("#TxtQualityDiffDebitTax").val()) < Number($("#TxtQualityCreditNoteVal").val())) {
        setInvalid($("#TxtQualityCreditNoteVal"), "Quality Diff Credit Tax cannot be more than Quality Diff Debit Tax.");
        isValid = false;
        return false;
    }

    //----------------- Validate QC Difference Amount ---------------
    if (Number($("#TxtQCDebitNoteAmt").val()) < Number($("#TxtQCCreditNoteAmt").val())) {
        setInvalid($("#TxtQCCreditNoteAmt"), "QC Diff Credit Amount cannot be more than QC Diff Debit Amount.");
        isValid = false;
        return false;
    }

    //----------------- Validate QC Difference Tax ---------------
    if (Number($("#TxtQCDebitNoteTax").val()) < Number($("#TxtQCCreditNoteVal").val())) {
        setInvalid($("#TxtQCCreditNoteVal"), "QC Diff Credit Tax cannot be more than QC Diff Debit Tax.");
        isValid = false;
        return false;
    }

    //----------------- Validate Rate Difference Amount ---------------
    if (Number($("#TxtRateDiffDebitAmt").val()) < Number($("#TxtRateDiffCreditNoteAmt").val())) {
        setInvalid($("#TxtRateDiffCreditNoteAmt"), "Rate Diff Credit Amount cannot be more than Rate Diff Debit Amount.");
        isValid = false;
        return false;
    }

    //----------------- Validate Rate Difference Tax ---------------
    if (Number($("#TxtRateDiffDebitTax").val()) < Number($("#TxtRateDiffCreditNoteVal").val())) {
        setInvalid($("#TxtRateDiffCreditNoteVal"), "Rate Diff Credit Tax cannot be more than Rate Diff Debit Tax.");
        isValid = false;
        return false;
    }

    //----------------- Validate Weight Difference Amount ---------------
    if (Number($("#TxtWeightDebitAmt").val()) < Number($("#TxtWeightCreditNoteAmt").val())) {
        setInvalid($("#TxtWeightCreditNoteAmt"), "Weight Diff Credit Amount cannot be more than Weight Diff Debit Amount.");
        isValid = false;
        return false;
    }

    //----------------- Validate Weight Difference Tax ---------------
    if (Number($("#TxtWeightDebitTax").val()) < Number($("#TxtWeightCreditNoteVal").val())) {
        setInvalid($("#TxtWeightCreditNoteVal"), "Weight Diff Credit Tax cannot be more than Weight Diff Debit Tax.");
        isValid = false;
        return false;
    }

    //----------------- Validate TDS Account ---------------
    if (Number($("#TxtTds2").val()) > 0 && !$("#ddlTdsAccount").val()) {
        setInvalid($("#ddlTdsAccount"), "TDS A/c must be selected if TDS amount is greater than 0.");
        isValid = false;
        return false;
    }

    //----------------- Validation For VType RIMP ---------------
    if (VTypeEl.val() === "RIMP") {
        if (Number($('#TxtBankRate2').val()) > 0) {
            // PL Date
            if (!$('#chkPlDate').is(":checked")) {
                setInvalid($('#DtPlDate'), "PL Date is required!");
                isValid = false;
                return false;
            }
            // PL No
            if ($('#chkPlDate').is(":checked") && Number($('#NumPlNo').val()) === 0) {
                setInvalid($('#NumPlNo'), "PL No is required!");
                isValid = false;
                return false;
            }
            // PL No Existence
            if (Number($('#NumPlNo').val()) > 0) {
                const pldocid = await getPLDocId(Number(VNoEl.val()), Number($('#NumPlNo').val()));
                if (pldocid !== "") {
                    setInvalid($('#NumPlNo'), `PL No alredy exist in Doc No: ${pldocid}`);
                    isValid = false;
                    return false;
                }
            }
        }
    }

    //----------------- Validate Frieght Debit A/c ---------------
    if (Number($('#NumFreightPay').val()) > 0 && Number($('#ddlFreightDebitAC').val()) === 0) {
        setInvalid($('#ddlFreightDebitAC'), "Freight Debit A/c Required.");
        isValid = false;
        return false;
    }

    //------------------ Validate Debit A/c With PO Type ---------------
    const DrAcType = await getDebitAcType(Number($("#ddlDebitAC").val()));
    const firstRow = document.querySelector("#MainGrid tbody tr");

    if (firstRow && getRowControls(firstRow).item.value && $("#cmbVtype").val() === "STPB") {
        const poType = await getPOType(getRowControls(firstRow).poType.value, Number(getRowControls(firstRow).poNo.value));
        if ((DrAcType || "").toString().toUpperCase() === "ASSET" && (poType || "").toString().toUpperCase() !== "CAPITAL") {
            setInvalid($('#ddlFreightDebitAC'), "The Debit A/c and the PO Type (Capital/Others) must belong to the same group or have the same nature");

            if (userLevel !== "1") {
                isValid = false;
                return false;
            }
        }
    }

    //------------------ Validate Debit A/c With Frieght Debit A/c ---------------
    if (Number($('#NumFreightPay').val()) > 0 && Number($('#ddlFreightDebitAC').val()) !== 0) {
        const FrtDrAcType = await getDebitAcType(Number($("#ddlFreightDebitAC").val()));
        if (DrAcType.toString() !== FrtDrAcType.toString()) {
            setInvalid($('#ddlFreightDebitAC'), "The Debit Account and the Freight Debit Account must belong to the same group or have the same nature.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Frieght Credit A/c ---------------
    if (Number($('#NumFreightPay').val()) > 0 && Number($('#ddlFreightCreditAC').val()) === 0) {
        setInvalid($('#ddlFreightCreditAC'), "Freight Credit A/c Required.");
        isValid = false;
        return false;
    }

    //----------------- Validate Frieght Debit And Credit A/c ---------------
    if (Number($('#NumFreightPay').val()) > 0 && Number($('#ddlFreightDebitAC').val()) !== 0 && Number($('#ddlFreightCreditAC').val()) !== 0 &&
        (Number($('#ddlFreightDebitAC').val()) === Number($('#ddlFreightCreditAC').val()))) {
        setInvalid($('#ddlFreightCreditAC'), "Freight Debit A/c and Freight Credit A/c must be different.");
        isValid = false;
        return false;
    }

    //----------------- Validate Frieght Tax ---------------
    if (Number($('#NumFreightPay').val()) === 0 && Number($('#NumFrtTax2').val()) > 0) {
        setInvalid($('#NumFrtTax2'), "Freight Tax not apply if Freight Amount is 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate Frieght TDS ---------------
    if (Number($('#NumTDSonFRT2').val()) > 0 && Number($('#NumFreightPay').val()) === 0) {
        setInvalid($('#NumFreightPay'), "Freight TDS not apply if Freight Amount is 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate Transport Details if Freight Tax > 0 ---------------
    if (Number($('#NumFrtTax2').val()) > 0) {

        // Transport GST No
        if (!$('#ddlTransportGSTNo').val()) {
            setInvalid($('#ddlTransportGSTNo'), "Transport GST No. is required if Freight Tax is greater than 0.");
            isValid = false;
            return false;
        }

        // Transport GST Type
        if (!$('#ddlTaxType').val()) {
            setInvalid($('#ddlTaxType'), "Transport GST Type is required if Freight Tax is greater than 0.");
            isValid = false;
            return false;
        }

        // Transport Bill No
        if (!$('#TxtBillNoLD').val().trim()) {
            setInvalid($('#TxtBillNoLD'), "Transport Bill No. is required if Freight Tax is greater than 0.");
            isValid = false;
            return false;
        }

        // Transport Bill Date
        if (!$('#chkBillDateLD').is(':checked')) {
            setInvalid($('#DtBillDateLD'), "Transport Bill Date is required if Freight Tax is greater than 0.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Transport Name ---------------
    if (Number($('#NumFreightPay').val()) > 0 && !$('#ddlTransportName').val()) {
        setInvalid($('#ddlTransportName'), "Transport Name must be selected if Freight Amount is greater than 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate GR Number ---------------
    if (Number($('#NumFreightPay').val()) > 0 && $('#chkGRDate').is(':checked') && (!$('#txtGRNo').val().trim() || $('#txtGRNo').val().trim() === "0")) {
        setInvalid($('#txtGRNo'), "GR Number is required if Freight Amount is greater than 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate GR Date ---------------
    if (Number($('#NumFreightPay').val()) > 0 && !$('#chkGRDate').is(':checked') && $('#txtGRNo').val().trim() !== "" && $('#txtGRNo').val().trim() !== "0") {
        setInvalid($('#DtGRDate'), "GR Date required with GR Number.");
        isValid = false;
        return false;
    }

    //----------------- Validate Duplicate Transport + GR No ---------------
    if ($('#ddlTransportName').val() && $('#txtGRNo').val()) {
        const docId = `${VTypeEl.val()}${VNoEl.val()}`;
        const transportName = $('#ddlTransportName')[0].options[$('#ddlTransportName')[0].selectedIndex]?.text.trim() || "";

        const purchaseDocId = await getPurchaseVoucherNo(transportName, $('#txtGRNo').val().trim(), docId, "PURCHASE");

        if (purchaseDocId) {
            showToast(
                `Transport Name '${transportName}' with GR No '${$('#txtGRNo').val().trim()}' already exists in Purchase Bill/Direct Exps/JW/Imported Exps/Return No: ${purchaseDocId}.`,
                { type: "warning" }
            );
            isValid = false;
            return false;
        }

        const saleDocId = await getPurchaseVoucherNo(transportName, $('#txtGRNo').val().trim(), docId, "SALE");

        if (saleDocId) {
            showToast(
                `Transport Name '${transportName}' with GR No '${$('#txtGRNo').val().trim()}' already exists in Sale/JW Issue/Sale Return Invoice No: ${saleDocId}.`,
                { type: "warning" }
            );
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Weighbridge Debit A/c ---------------
    if ((Number($('#NumWBAmount').val()) > 0 || Number($('#NumWBTDS2').val()) > 0) && Number($('#ddlWBDebitAC').val()) === 0) {
        setInvalid($('#ddlWBDebitAC'), "Weighbridge Debit A/c must be selected if Weighbridge Amount or Weighbridge Tax Amount is greater than 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate Debit A/c with Weighbridge Debit A/c ---------------
    if ((Number($('#NumWBAmount').val()) > 0 || Number($('#NumWBTDS2').val()) > 0) && Number($('#ddlWBDebitAC').val()) !== 0) {
        const wbDrAcType = await getDebitAcType(Number($('#ddlWBDebitAC').val()));

        if ((wbDrAcType ?? "") !== (DrAcType ?? "")) {
            setInvalid($('#ddlWBDebitAC'), "The Debit Account and the Weighbridge Debit Account must belong to the same group or have the same nature.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Weighbridge Credit A/c ---------------
    if ((Number($('#NumWBAmount').val()) > 0 || Number($('#NumWBTDS2').val()) > 0) && Number($('#ddlWBCreditAC').val()) === 0) {
        setInvalid($('#ddlWBCreditAC'), "Weighbridge Credit A/c must be selected if Weighbridge Amount or Weighbridge Tax Amount is greater than 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate Weighbridge Debit & Credit A/c ---------------
    if (Number($('#NumWBAmount').val()) > 0 && Number($('#ddlWBDebitAC').val()) !== 0 && Number($('#ddlWBCreditAC').val()) !== 0 &&
        Number($('#ddlWBDebitAC').val()) === Number($('#ddlWBCreditAC').val())) {
        setInvalid($('#ddlWBCreditAC'), "Weighbridge Debit a/c and Weighbridge Credit A/c must be different.");
        isValid = false;
        return false;
    }

    //----------------- Validate Unloading Debit A/c ---------------
    if ((Number($('#NumUnloadAmt').val()) > 0 || Number($('#NumUnloadTDS2').val()) > 0) && Number($('#ddlUnloadDebitAC').val()) === 0) {
        setInvalid($('#ddlUnloadDebitAC'), "Unloading Debit A/c must be selected if Unloading Amount or Unloading Tax Amount is greater than 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate Debit A/c with Unloading Debit A/c ---------------
    if ((Number($('#NumUnloadAmt').val()) > 0 || Number($('#NumUnloadTDS2').val()) > 0) && Number($('#ddlUnloadDebitAC').val()) !== 0) {
        const ulDrAcType = await getDebitAcType(Number($('#ddlUnloadDebitAC').val()));

        if ((ulDrAcType ?? "") !== (DrAcType ?? "")) {
            setInvalid($('#ddlUnloadDebitAC'), "The Debit Account and the Unloading Debit Account must belong to the same group or have the same nature.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Unloading Credit A/c ---------------
    if ((Number($('#NumUnloadAmt').val()) > 0 || Number($('#NumUnloadTDS2').val()) > 0) && Number($('#ddlUnloadCreditAC').val()) === 0) {
        setInvalid($('#ddlUnloadCreditAC'), "Unloading Credit A/c must be selected if Unloading Amount or Unloading Tax Amount is greater than 0.");
        isValid = false;
        return false;
    }

    //----------------- Validate Unloading Debit & Credit A/c ---------------
    if (Number($('#NumUnloadAmt').val()) > 0 && Number($('#ddlUnloadDebitAC').val()) !== 0 && Number($('#ddlUnloadCreditAC').val()) !== 0 &&
        Number($('#ddlUnloadDebitAC').val()) === Number($('#ddlUnloadCreditAC').val())) {
        setInvalid($('#ddlUnloadCreditAC'), "Unloading Debit A/c and Unloading Credit A/c must be different.");
        isValid = false;
        return false;
    }

    //----------------- Validate GR Date ---------------
    if ($('#chkGRDate').is(':checked')) {

        const grDate = new Date($('#DtGRDate').val());
        const voucherDate = new Date(VDateEl.val());

        grDate.setHours(0, 0, 0, 0);
        voucherDate.setHours(0, 0, 0, 0);

        if (grDate > voucherDate) {
            setInvalid($('#DtGRDate'), "GR Date cannot be greater than Voucher Date.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Payment Hold Reason ---------------
    if (($('#ddlPayment').val() || "").toUpperCase() === "HOLD" && !$('#TxtReason').val().trim()) {
        setInvalid($('#TxtReason'), "Reason is required for Payment HOLD.");
        isValid = false;
        return false;
    }

    //----------------- Validate Hold Date ---------------
    if ($('#chkHoldDate').is(':checked')) {

        const holdDate = new Date($('#DtHoldDate').val());
        const voucherDate = new Date(VDateEl.val());

        holdDate.setHours(0, 0, 0, 0);
        voucherDate.setHours(0, 0, 0, 0);

        if (holdDate < voucherDate) {
            setInvalid($('#DtHoldDate'), "Hold Date must be greater than or equal to Voucher Date.");
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Payment Done ---------------
    const paymentExists = await checkPaymentExists(VTypeEl.val(), Number(VNoEl.val()));
    if (paymentExists) {
        showToast(`Payment done of document no ${VTypeEl.val()}${VNoEl.val()}. Please check ledger. Edit not allowed.`, { type: "warning" });
        if (userLevel !== "1") {
            isValid = false;
            return false;
        }
    }

    //----------------- Validate Purchase Qty Excess than Sauda Qty (For RM Only) ---------------
    if (VTypeEl.val() === "RMPB") {
        const result = await getPurchaseQtyExcess(Number(VNoEl.val()), Number($('#NumReceivedQty').val()) || 0);
        if (result) {
            if (result.isExcess) {
                showToast(`Total Purchase Qty (${result.totalPurchaseQty}) > Sauda+Tolarnce Qty(${result.allowedQty}), Give Reason for Excess Qty.\nGive Reason in Other Debit note field/Narration.`, { type: "warning" });
                isvalid = false;
                return false;
            }
        }
    }

    //----------------- Validate Duplicate Bill No ---------------
    if ($('#TxtBillNo').val().trim()) {
        const duplicateBill = await checkDuplicateBill(Number($('#ddlBillFrom').val()), $('#TxtBillNo').val().trim(), Number(VNoEl.val()));
        if (duplicateBill && duplicateBill.exists) {
            showToast(`Bill No ${$('#TxtBillNo').val().trim()} already exists in Purchase Bill, Serial No: ${duplicateBill.docId} dated: ${duplicateBill.vDate}`,
                { type: "warning" });
            isvalid = false;
            return false;
        }
    }

    //----------------- Validate Bill From & Ship From ---------------
    if (!validateRequiredField($('#ddlBillFrom'), 'Bill From') || !validateRequiredField($('#ddlShipFrom1'), 'Ship From')) {
        isValid = false;
        return false;
    }

    //----------------- Validate Party State Tax ---------------
    if (Number($('#ddlBillFrom').val()) > 0) {

        if ($('#ddlCityPD').prop('selectedIndex') === -1 || !$('#ddlCityPD').val()) {
            setInvalid($('#ddlCityPD'), "City not selected.");
            isValid = false;
            return false;
        }

        const taxValidation = await validateTaxType(
            Number($('#ddlCityPD').val()),
            Number($('#NumIgst').val()),
            Number($('#NumCgst').val()),
            Number($('#NumSgst').val())
        );

        if (taxValidation && !taxValidation.isValid) {
            showToast(taxValidation.message, { type: "warning" });
            isValid = false;
            return false;
        }
    }

    //-----------Grid Validation--------
    const isValidGrid = await validateItemGrid();
    if (!isValidGrid) {
        isvalid = false;
        return false;
    }

    //------------EPR Attachment Validation----------
    //const isEPRValidate = await validateEPRAttachment();  // Uncomment after testing
    //if (!isEPRValidate) {
    //    isvalid = false;
    //    return false;
    //}

    //------------Cr/Dr Amount Validation----------
    const isCrDrValidate = await validateDrCrNoteAmount();
    if (!isCrDrValidate) {
        isvalid = false;
        return false;
    }

    return isValid;

}

async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/PurchaseBillPassEntry/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();
        if (result.status === false) {
            // toastr.warning(result.message);
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;
    } catch (error) {
        console.error("Error:", error);
        return false;
    }
}

async function validateItemGrid() {
    const vType = $('#ddlDocType').val();
    const mrnType = $('#TxtMRNNo1').val();
    const freightAmount = $('#NumFreightPay').val();
    let validItemCount = 0;

    const rows = document.querySelectorAll("#tblItemRecordPBPE tbody tr");

    for (const row of rows) {

        const c = getRowControls(row);

        if (Number(c.item.value) > 0)
            validItemCount++;

        //Validate Item
        if (!validateItem(c)) return false;

        const itemName = c.item.options[c.item.selectedIndex].text;

        if (Number(c.item.value) > 0) {
            //Validate Freight, HSN and QC
            const result = await validatePurchaseRow(vType, c.item.value, itemName, c.hsn.value, c.recdQty.value, freightAmount,
                c.poType.value, c.poNo.value, c.refType.value, c.refNo.value);

            if (result) {
                // Freight warning
                if (result.freightWarning) {
                    showToast(result.freightMessage, { type: "warning" });
                }
                // HSN mismatch
                if (result.hsnMismatch) {
                    showToast(result.hsnMessage, { type: "warning" });
                    itemVsBillHSNCodeDiff = result.item_vs_Bill_HSNCodeDiff;
                }
                // QC pending (stop further processing)
                if (!result.isValid && result.qcPending) {
                    showToast(result.qcMessage, { type: "warning" });
                    return false;
                }
            }

            //Validate Reference
            if (Number(c.refNo.value) === 0) {
                setInvalid(c.refNo, `Ref type and Ref Number is missing of ${itemName}`);
                return false;
            }

            //Validate Received Quantity
            if (Number(c.item.value) > 0 && Number(c.recdQty.value) === 0) {
                setInvalid(c.refNo, "Received Qty is 0.");
                //return true;
            }

            //Validate Amount
            if (Number(c.amount.value) === 0) {
                setInvalid(c.amount, "Amount must not be 0.");
                //return false;
            }

            //Validate Make
            if (vType !== "STDP" && mrnType !== "RCPT") {
                if (Number(c.makeCode.value) === 0) {
                    setInvalid(c.makeCode, "Make is empty.");
                    return false;
                }
                if ((pubDefPOInMRN || "").toUpperCase() === "YES" && Number(c.poNo.value) === 0) {
                    setInvalid(c.makeCode, `PO Number is Required/Compulsary of Item ${itemName}`);
                    return false;
                };
            }

            //Validate PO/Sauda Approval
            const appResult = await validatePoSaudaApproval(c.item.value, itemName, c.poType.value, c.poNo.value);

            if (appResult && !appResult.isValid) {
                showToast(appResult.message, { type: "warning" });
                return false;
            }

            //Validate Tax
            if (Number(c.taxCode.value) === 0) {
                setInvalid(c.taxCode, "Tax Type not selected.");
                return false;
            }

            //Validate EPR Item
            const hsn = c.hsn.value.trim();
            if (hsn.startsWith("3915")) {
                EPRFlg = 1;
            }
        }
    }

    if (validItemCount === 0) {
        showToast('No Record in grid to save.', { type: "warning" });
        return false;
    }
    return true;
}

function validateItem(c) {
    const itemCode = Number(c.item.value);
    if (c.item.value && itemCode === 0) {
        setInvalid(c.item, "Item code not valid.");
        return false;
    }
    return true;
}

async function validatePurchaseRow(vType, itemCode, itemName, billHsnCode,
    qty, freightAmount, poType, poNo, mrnType, mrnNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/ValidatePurchaseRow",
            type: "GET",
            dataType: "json",
            data: {
                vType: vType,
                itemCode: itemCode,
                itemName: itemName,
                billHsnCode: billHsnCode,
                qty: qty,
                freightAmount: freightAmount,
                poType: poType,
                poNo: poNo,
                mrnType: mrnType,
                mrnNo: mrnNo
            }
        });

        if (response.success) {
            return response.result;
        } else {
            showToast(response.message || "Failed to validate purchase row.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error validating purchase row:", error);
        showToast("An error occurred while validating purchase row.", { type: "error" });
        return null;
    }
}

async function validatePoSaudaApproval(itemCode, itemName, poType, poNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/ValidatePoSaudaApproval",
            type: "GET",
            dataType: "json",
            data: {
                itemCode: itemCode,
                itemName: itemName,
                poType: poType,
                poNo: poNo
            }
        });

        if (response.success) {
            return response.result;
        } else {
            showToast(response.message || "Failed to validate PO/Sauda approval.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error validating PO/Sauda approval:", error);
        showToast("An error occurred while validating PO/Sauda approval.", { type: "error" });
        return null;
    }
}

async function validateEPRAttachment() {
    const pubCompCode = Number(compCode);
    const billToName = document.querySelector("#ddlBillFrom").selectedOptions[0].text;
    if ((pubCompCode == 2 || pubCompCode == 5) && billToName && billToName.length >= 19 &&
        billToName.toUpperCase().includes("PASHUPATI EXCRUSION")) {
        EPRFlg = 1;
    }
    //if (EPRFlg === 1 && [1, 4, 7].includes(pubCompCode)) {
    if (EPRFlg === 1 && dataSource !== "192.168.1.217") {
        if ($("#ddlDocType").val() === "RMPB") {
            const billToShipTo = $("#ddlBillFrom").val() !== $("#ddlShipFrom1").val();

            let requiredDocs = [
                "Company WB Slip Copy",
                "Party WB Slip Copy",
                "Party Invoice Copy",
                "Party GR Copy",
                "Party EWaybill Copy"
            ];

            if (billToShipTo)
                requiredDocs.push("Other Copy Bill To/Ship To");

            const uploadedDocs = [];

            $("#tblAttachmentEPR tbody tr").each(function () {
                const docType = $(this).find("td:eq(0)").text().trim();
                if (docType)
                    uploadedDocs.push(docType);
            });

            const missing = requiredDocs.filter(x => !uploadedDocs.includes(x));

            if (missing.length > 0) {
                const message =
                    "EPR Attachment is required in case of RAW Material Purchase with Items having HSN Code 3915 or Recycle Material.<br><br>" +
                    missing.map(doc => `Missing: ${doc}`).join("<br>");

                showToast(message, { type: "warning" });
                return false;
            }
        }
    }


    return true;
}

function validateDrCrNoteAmount() {
    let totDrAmtDetail = 0;
    let totCrAmtDetail = 0;
    let totDrAmtHeader = 0;
    let totCrAmtHeader = 0;

    if ($("#ddlInputType").val() === "GST Input") {
        totDrAmtHeader =
            parseFloat($("#TxtQualityDiffDebitAmt").val() || 0) +
            parseFloat($("#TxtRateDiffDebitAmt").val() || 0) +
            parseFloat($("#TxtQCDebitNoteAmt").val() || 0) +
            parseFloat($("#TxtWeightDebitAmt").val() || 0) +
            parseFloat($("#TxtOtherDebitAmt").val() || 0);

        totCrAmtHeader =
            parseFloat($("#TxtQualityCreditNoteAmt").val() || 0) +
            parseFloat($("#TxtRateDiffCreditNoteAmt").val() || 0) +
            parseFloat($("#TxtQCCreditNoteAmt").val() || 0) +
            parseFloat($("#TxtWeightCreditNoteAmt").val() || 0);
    } else {
        totDrAmtHeader =
            parseFloat($("#TxtQualityDiffDebitAmt").val() || 0) +
            parseFloat($("#TxtRateDiffDebitAmt").val() || 0) +
            parseFloat($("#TxtQCDebitNoteAmt").val() || 0) +
            parseFloat($("#TxtWeightDebitAmt").val() || 0) +
            parseFloat($("#TxtOtherDebitAmt").val() || 0) +
            parseFloat($("#TxtQualityDiffDebitTax").val() || 0) +
            parseFloat($("#TxtRateDiffDebitAmt").val() || 0) +
            parseFloat($("#TxtQCDebitNoteTax").val() || 0) +
            parseFloat($("#TxtWeightDebitTax").val() || 0) +
            parseFloat($("#TxtOtherDebitTax").val() || 0);

        // NOTE: This line is intentionally kept the same as the original VB code.
        // It overwrites totDrAmtHeader instead of assigning to totCrAmtHeader.
        //totDrAmtHeader = // I have changed to totCrAmtHeader
        totCrAmtHeader =
            parseFloat($("#TxtQualityCreditNoteAmt").val() || 0) +
            parseFloat($("#TxtRateDiffCreditNoteAmt").val() || 0) +
            parseFloat($("#TxtQCCreditNoteAmt").val() || 0) +
            parseFloat($("#TxtWeightCreditNoteAmt").val() || 0) +
            parseFloat($("#TxtQualityCreditNoteVal").val() || 0) +
            parseFloat($("#TxtRateDiffCreditNoteVal").val() || 0) +
            parseFloat($("#TxtQCCreditNoteVal").val() || 0) +
            parseFloat($("#TxtWeightCreditNoteVal").val() || 0);
    }

    document.querySelectorAll("#tblItemRecordPBPE tbody tr").forEach(row => {
        const controls = getRowControls(row);

        if (controls.item && controls.item.value) {
            totDrAmtDetail += parseFloat(controls.drNoteAmt?.value || 0);
            totCrAmtDetail += parseFloat(controls.crNoteAmt?.value || 0);
        }
    });

    totDrAmtDetail = Math.round(totDrAmtDetail);
    totCrAmtDetail = Math.round(totCrAmtDetail);
    totDrAmtHeader = Math.round(totDrAmtHeader);
    totCrAmtHeader = Math.round(totCrAmtHeader);

    if (Math.abs(totDrAmtDetail) !== totDrAmtHeader) {
        showToast(
            `Sum of Debit note Amount of Grid (${Math.abs(totDrAmtDetail)}) and Sum of Debit note Amt at Header (${totDrAmtHeader}) not matched.`,
            { type: "warning" }
        );
        return false;
    }

    if (Math.abs(totCrAmtDetail) !== totCrAmtHeader) {
        showToast(
            `Sum of Credit note Amount of Grid (${Math.abs(totCrAmtDetail)}) and Sum of Credit note Amt at Header (${totCrAmtHeader}) not matched.`,
            { type: "warning" }
        );
        return false;
    }

    return true;
}

async function getPurchaseDate(vType, vNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetPurchaseDate",
            type: "GET",
            dataType: "json",
            data: {
                vType: vType,
                vNo: vNo
            }
        });

        if (response.success) {
            console.log("Purchase Date:", response.purchaseDate);
            return response.purchaseDate;
        } else {
            showToast(response.message || "Failed to get purchase date.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error fetching purchase date:", error);
        showToast("An error occurred while fetching the purchase date.", { type: "error" });
        return null;
    }
}

async function getPartyPurchaseAmount(partyCode, vType, vNo, currentAmount) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetPartyPurchaseAmount",
            type: "GET",
            dataType: "json",
            data: {
                partyCode: partyCode,
                vType: vType,
                vNo: vNo,
                currentAmount: currentAmount
            }
        });

        if (response.success) {
            console.log("Total Purchase Amount:", response.totalAmount);
            return parseFloat(response.totalAmount) || 0;
        } else {
            showToast(response.message || "Failed to get purchase amount.", { type: "error" });
            return 0;
        }
    } catch (error) {
        console.error("Error fetching purchase amount:", error);
        showToast("An error occurred while fetching the purchase amount.", { type: "error" });
        return 0;
    }
}

async function getTDS206Apply(partyCode) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetTDS206Apply",
            type: "GET",
            dataType: "json",
            data: {
                partyCode: partyCode
            }
        });

        if (response.success) {
            console.log("TDS 206 Apply:", response.tds206Apply);
            return response.tds206Apply || "";
        } else {
            showToast(response.message || "Failed to fetch TDS 206 Apply.", { type: "error" });
            return "";
        }
    } catch (error) {
        console.error("Error fetching TDS 206 Apply:", error);
        showToast("An error occurred while fetching TDS 206 Apply.", { type: "error" });
        return "";
    }
}

async function isPostingExist(vType) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/IsPostingExist",
            type: "GET",
            dataType: "json",
            data: {
                vType: vType
            }
        });

        if (response.success) {
            return response.isPostingExist;
        } else {
            showToast(response.message || "Failed to check posting.", { type: "error" });
            return false;
        }
    } catch (error) {
        console.error("Error checking posting:", error);
        showToast("An error occurred while checking posting.", { type: "error" });
        return false;
    }
}

async function getPLDocId(vNo, plNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetPLDocId",
            type: "GET",
            dataType: "json",
            data: {
                vNo: vNo,
                plNo: plNo
            }
        });

        if (response.success) {
            return response.docId;
        } else {
            showToast(response.message || "Failed to get PL document ID.", { type: "error" });
            return 0;
        }
    } catch (error) {
        console.error("Error getting PL document ID:", error);
        showToast("An error occurred while fetching the PL document ID.", { type: "error" });
        return 0;
    }
}

async function getDebitAcType(code) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetDebitAcType",
            type: "GET",
            dataType: "json",
            data: {
                code: code
            }
        });

        if (response.success) {
            return response.type;
        } else {
            showToast(response.message || "Failed to get debit A/c type.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error getting debit A/c type:", error);
        showToast("An error occurred while getting debit A/c type.", { type: "error" });
        return null;
    }
}

async function getPOType(poType, poNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetPOType",
            type: "GET",
            dataType: "json",
            data: {
                poType: poType,
                poNo: poNo
            }
        });

        if (response.success) {
            return response.type;
        } else {
            showToast(response.message || "Failed to get PO type.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error getting PO type:", error);
        showToast("An error occurred while getting PO type.", { type: "error" });
        return null;
    }
}

async function getPurchaseVoucherNo(transportName, grNo, currentVoucher, purchaseOrSale) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetPurchaseOrSaleVoucherNo",
            type: "GET",
            dataType: "json",
            data: {
                transportName: transportName,
                grNo: grNo,
                currentVoucher: currentVoucher,
                purchaseOrSale: purchaseOrSale
            }
        });

        if (response.success) {
            return response.voucherNo;
        } else {
            showToast(response.message || "Failed to get purchase voucher number.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error getting purchase voucher number:", error);
        showToast("An error occurred while getting purchase voucher number.", { type: "error" });
        return null;
    }
}

async function checkPaymentExists(docType, docNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/CheckPaymentExists",
            type: "GET",
            dataType: "json",
            data: {
                docType: docType,
                docNo: docNo
            }
        });

        if (response.success) {
            return response.exists;
        } else {
            showToast(response.message || "Failed to check payment existence.", { type: "error" });
            return false;
        }
    } catch (error) {
        console.error("Error checking payment existence:", error);
        showToast("An error occurred while checking payment existence.", { type: "error" });
        return false;
    }
}

async function getPurchaseQtyExcess(vNo, currentRecQty) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetPurchaseQtyExcess",
            type: "GET",
            dataType: "json",
            data: {
                vNo: vNo,
                currentRecQty: currentRecQty
            }
        });

        if (response.success) {
            return response.result;
        } else {
            showToast(response.message || "Failed to validate purchase quantity.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error validating purchase quantity:", error);
        showToast("An error occurred while validating purchase quantity.", { type: "error" });
        return null;
    }
}

async function checkDuplicateBill(partyCode, billNo, currentVNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/CheckDuplicateBill",
            type: "GET",
            dataType: "json",
            data: {
                partyCode: partyCode,
                billNo: billNo,
                currentVNo: currentVNo
            }
        });

        if (response.success) {
            return {
                exists: response.exists,
                docId: response.docId,
                vDate: response.vDate
            };
        } else {
            showToast(response.message || "Failed to check duplicate bill.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error checking duplicate bill:", error);
        showToast("An error occurred while checking duplicate bill.", { type: "error" });
        return null;
    }
}

async function validateTaxType(cityCode, totalIGST, totalCGST, totalSGST) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/ValidateTaxType",
            type: "GET",
            dataType: "json",
            data: {
                cityCode: cityCode,
                totalIGST: totalIGST,
                totalCGST: totalCGST,
                totalSGST: totalSGST
            }
        });

        if (response.success) {
            return {
                isValid: response.isValid,
                message: response.message
            };
        } else {
            showToast(response.message || "Failed to validate tax type.", { type: "error" });
            return null;
        }
    }
    catch (error) {
        console.error("Error validating tax type:", error);
        showToast("An error occurred while validating tax type.", { type: "error" });
        return null;
    }
}

async function validatePartyGst(gstType, partyCode, gstNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/ValidatePartyGst",
            type: "GET",
            dataType: "json",
            data: {
                gstType: gstType,
                partyCode: partyCode,
                gstNo: gstNo
            }
        });

        if (response.success) {
            return response.result;
        } else {
            showToast(response.message || "Failed to validate GST No.", { type: "error" });
            return null;
        }
    } catch (error) {
        console.error("Error validating GST No:", error);
        showToast("An error occurred while validating GST No.", { type: "error" });
        return null;
    }
}

function getRowControls(row) {
    return {
        item: row.querySelector(".item-name"),
        hsn: row.querySelector(".hsn-code"),

        uomCode: row.querySelector(".uom-code"),
        uomName: row.querySelector(".uom-name"),

        nos: row.querySelector(".nos"),
        recdQty: row.querySelector(".recd-qty"),
        billQty: row.querySelector(".bill-qty"),

        usdRate: row.querySelector(".usd-rate"),
        exchRate: row.querySelector(".exch-rate"),
        rate: row.querySelector(".rate"),
        amount: row.querySelector(".amount"),

        rcmYN: row.querySelector(".rcm-yn"),
        inputYN: row.querySelector(".input-yn"),

        taxCode: row.querySelector(".tax-code"),

        packPer: row.querySelector(".pack-per"),
        packAmt: row.querySelector(".pack-amt"),

        discPer: row.querySelector(".disc-per"),
        discAmt: row.querySelector(".disc-amt"),

        cgstPer: row.querySelector(".cgst-per"),
        cgstAmt: row.querySelector(".cgst-amt"),

        sgstPer: row.querySelector(".sgst-per"),
        sgstAmt: row.querySelector(".sgst-amt"),

        igstPer: row.querySelector(".igst-per"),
        igstAmt: row.querySelector(".igst-amt"),

        cessPer: row.querySelector(".cess-per"),
        cessAmt: row.querySelector(".cess-amt"),

        vatPer: row.querySelector(".vat-per"),
        vatAmt: row.querySelector(".vat-amt"),

        othAmt: row.querySelector(".oth-amt"),
        netAmt: row.querySelector(".net-amt"),

        makeCode: row.querySelector(".make-code"),
        makeName: row.querySelector(".make-name"),

        deptCode: row.querySelector(".dept-code"),

        remarks: row.querySelector(".remarks"),

        landRate: row.querySelector(".land-rate"),
        landAmt: row.querySelector(".land-amt"),

        polandRate: row.querySelector(".poland-rate"),
        poRate: row.querySelector(".po-rate"),

        poType: row.querySelector(".po-type"),
        poNo: row.querySelector(".po-no"),

        kantaType: row.querySelector(".kanta-type"),
        kantaNo: row.querySelector(".kanta-no"),

        reqType: row.querySelector(".req-type"),
        reqNo: row.querySelector(".req-no"),

        refType: row.querySelector(".ref-type"),
        refNo: row.querySelector(".ref-no"),

        drNoteAmt: row.querySelector(".dr-note-amt"),
        crNoteAmt: row.querySelector(".cr-note-amt")
    };
}

async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntry/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            console.log(d);
            pubDefPOInMRN = d.pubDefPOInMRN;
            compCode = d.compCode;
            dataSource = d.dataSource;
            userLevel = d.userLevel;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}

//-----------------------EPR Attachment---------------------
function AddEPRAttachmentRow(attachment) {
    var index = EPRAttachmentList.length;
    EPRAttachmentList.push(attachment);
    console.log("EPRAttachmentList: ", EPRAttachmentList);
    var row = `
        <tr data-index="${index}">
            <td>${attachment.DocumentType}</td>
            <td>${attachment.OriginalFileName}</td>
            <td class="action-col">
                <button type="button" class="btnEPRPreview">
                    <i class="fa fa-eye"></i>
                </button>
                <button type="button" class="btnEPRDelete">
                    <i class="fa fa-trash"></i>
                </button>
            </td>
        </tr>`;

    $("#tblAttachmentEPR tbody").append(row);
}

function getEPRAttachmentFileName(documentType, originalFileName) {

    // Remove spaces from document type
    var docType = documentType.replace(/\s+/g, "");

    return docType + "_" + originalFileName;
}

function collectEPRAttachmentFile() {

    return Promise.all(EPRAttachmentList.map(function (item) {

        return new Promise(function (resolve, reject) {

            const reader = new FileReader();

            reader.onload = function (e) {

                resolve({
                    FILE_NAME: item.FileName,
                    FILE_DATA: e.target.result.split(',')[1] // Base64
                });

            };

            reader.onerror = reject;

            reader.readAsDataURL(item.File);

        });

    }));
}

const documentTypeMap = {
    "CompanyWBSlipCopy": "Company WB Slip Copy",
    "PartyWBSlipCopy": "Party WB Slip Copy",
    "PartyInvoiceCopy": "Party Invoice Copy",
    "PartyGRCopy": "Party GR Copy",
    "PartyEWaybillCopy": "Party EWaybill Copy",
    "OtherCopy-1": "Other Copy-1",
    "OtherCopy-2": "Other Copy-2",
    "OtherCopy-3": "Other Copy-3",
    "OtherCopy-4": "Other Copy-4",
    "OtherCopy-5": "Other Copy-5"
};

function getDocumentType(code) {
    return documentTypeMap[code] || code;
}
//----------------Attachment--------------
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
function roundAwayFromZero(value) {
    return value >= 0
        ? Math.floor(value + 0.5)
        : Math.ceil(value - 0.5);
}

//---------------TDS Calculation Button Click-------------
function OnTDSBtnClick() {

    var firstRow = $("#tblPurchase tbody tr:first");

    var poType = firstRow.find(".po-type").val() || "";
    var poNo = parseInt(firstRow.find(".po-no").val()) || 0;

    var model = {
        V_TYPE: $("#ddlDocType").val(),
        V_NO: parseInt($("#NumDocNo").val()) || 0,
        REF_TYPE: poType,
        REF_NO: poNo,
        PARTY_CODE: parseInt($("#ddlBillFrom").val()) || 0,

        AMOUNT: parseFloat($("#NumAmount").val()) || 0,

        QTY_DR_AMT: parseFloat($("#TxtWeightDebitAmt").val()) || 0,
        RDF_DR_AMT: parseFloat($("#TxtRateDiffDebitAmt").val()) || 0,
        QC_DR_AMT: parseFloat($("#TxtQCDebitNoteAmt").val()) || 0,
        QLT_DR_AMT: parseFloat($("#TxtQualityDiffDebitAmt").val()) || 0,
        OTH_DR_AMT: parseFloat($("#TxtOtherDebitAmt").val()) || 0,

        QTY_CR_AMT: parseFloat($("#TxtWeightCreditNoteAmt").val()) || 0,
        RDF_CR_AMT: parseFloat($("#TxtRateDiffCreditNoteAmt").val()) || 0,
        QC_CR_AMT: parseFloat($("#TxtQCCreditNoteAmt").val()) || 0,
        QLT_CR_AMT: parseFloat($("#TxtQualityCreditNoteAmt").val()) || 0
    };

    $.ajax({
        url: '/PurchaseBillPassEntry/CalculateTDS',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(model),
        success: function (response) {

            if (response.success) {

                $("#NumAdvTDS").val(response.data.advTds.toFixed(2));
                $("#NumNetAmt").val(response.data.netAmt.toFixed(2));
                $("#NumDrNote").val(response.data.drNote.toFixed(2));
                $("#NumCRNote").val(response.data.crNote.toFixed(2));
                $("#NumTDSAmt").val(response.data.tds194Q.toFixed(2));
            }
            else {
                showToast(response.message, {type:"warning"});
            }
        },
        error: function (xhr) {
            showToast(xhr.responseJSON?.message || "Error while calculating TDS.", {type:"error"});
        }
    });
}

//-------------Copy From--------------
function loadCopyFromMenu(docType) {

    $.ajax({
        url: '/PurchaseBillPassEntry/GetCopyFromMenu',
        type: 'GET',
        data: { docType: docType },
        success: function (response) {

            if (!response.success) {
                showToast(response.message, {type:"warning"});
                return;
            }

            let menu = $("#copyFromMenu");
            menu.empty();

            $.each(response.data, function (i, item) {

                menu.append(`
                    <li>
                        <a class="dropdown-item erppagedropdown-item copy-from-item" href="#" data-doctype="${item.code}"">
                            ${item.name}
                        </a>
                    </li>
                `);
            });
        },
        error: function (xhr) {
            showToast("An error occurred while loading Copy From options.", {type:"warning"});
            console.error(xhr);
        }
    });
}

function getCopyFromData(code) {
    const billTo = Number($("#ddlBillFrom").val());
    if (!billTo || billTo <= 0) {
        setInvalid($("#ddlBillFrom"), "Please select Bill From.");
        return;
    }

    const billNo = $("#TxtBillNo").val();
    if ((billNo || "").trim() === "") {
        setInvalid($("#TxtBillNo"), "Please select Bill No.");
        return;
    }

    const request = {
        vType: $("#ddlDocType").val(),
        billTo: billTo,
        billNo: billNo,
        vNo: $("#NumDocNo").val() || 0,
        currentVType: code
    };

    $.ajax({
        url: '/PurchaseBillPassEntry/GetCopyFromData',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),

        success: function (response) {

            if (!response.success) {
                showToast(response.message, {type:"warning"});
                return;
            }

            //console.log(response.data);
            bindCopyFromGrid(response.data);
            $('#purchaseorderModal').modal("show");
        },

        error: function (xhr) {
            showToast("Unable to load Copy From data.", {type:"error"});
            console.log(xhr);
        }
    });
}

function bindCopyFromGrid(data) {

    const table = $("#tblpurchaseordermodal");
    const thead = table.find("thead");
    const tbody = table.find("tbody");

    table.find("colgroup").remove();
    thead.empty();
    tbody.empty();

    if (!data || data.length === 0) {
        thead.html("");
        tbody.html(`<tr><td colspan="100%" class="text-center">No Record Found</td></tr>`);
        return;
    }

    // ---------- Create ColGroup ----------
    let colgroup = "<colgroup>";

    // Checkbox column
    colgroup += `<col style="width:50px;">`;

    Object.keys(data[0]).forEach(col => {

        let maxLength = col.length;

        // Find longest value in this column
        data.forEach(row => {
            const value = row[col] == null ? "" : row[col].toString();
            if (value.length > maxLength)
                maxLength = value.length;
        });

        let width;

        // Fixed width for numeric columns
        if ([
            "Nos",
            "Qty",
            "BalQty",
            "Rate",
            "PackPer",
            "DiscPer",
            "CGSTPer",
            "SGSTPer",
            "IGSTPer",
            "OthAmt",
            "RECDQTY",
            "BILLQTY"
        ].includes(col)) {

            width = 90;
        }
        else {

            // Approximate width: 9px per character
            width = maxLength * 9;

            // Minimum width
            width = Math.max(width, 80);

            // Maximum width
            width = Math.min(width, 300);
        }

        colgroup += `<col style="width:${width}px;">`;

    });

    colgroup += "</colgroup>";

    table.prepend(colgroup);

    // ---------- Header ----------
    let header = "<tr>";

    header += `<th style="text-align:center;"><input type="checkbox" id="selectAllPR"></th>`;

    Object.keys(data[0]).forEach(col => {
        header += `<th>${col}</th>`;
    });

    header += "</tr>";
    thead.html(header);

    // ---------- Body ----------
    let rows = "";
    data.forEach((row, index) => {
        rows += "<tr>";
        rows += `<td style="text-align:center;"> <input type="checkbox" class="copyfrom-check" data-index="${index}"></td>`;

        Object.keys(row).forEach(col => {
            const value = row[col] ?? "";
            rows += `<td title="${value}">${value}</td>`;
        });

        rows += "</tr>";
    });

    tbody.html(rows);
    makeColumnsResizable("#tblpurchaseordermodal");
}

