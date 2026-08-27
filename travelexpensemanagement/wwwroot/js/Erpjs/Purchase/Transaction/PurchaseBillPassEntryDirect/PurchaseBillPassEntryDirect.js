let uploadedFiles = [];
let copyFromRows = [];

let changeItem = false;
let itemVsBillHSNCodeDiff = false;

let userLevel = "";
let compCode = "";
let branchCode = "";
let yearCode = "";
let pubDefPOInMRN = "";
let dataSource = "";
let companyName = "";
let companyGst = "";
let add1 = "";
let add2 = "";
let db = "";

let isLoadForEdit = false;

const dropdownCache = {};
const dropdownPromiseCache = {};

let itemOptionsHtml = "";
let taxOptionsHtml = "";
let deptOptionsHtml = "";

const rowsData = [];

const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const rowIdVType = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';
const DBTableName = "PURCHASE1";

let isMRNChange = false;

$(document).ready(async function () {
    $('#ddlDocType').focus();
    getGlobalValues();
    toggleDate();
    // Initially hide all
    $('#BtnDrNotePrint').hide();
    $('#BtnCrNotePrint').hide();
    try {

        const currentDate = getCurrentDateYMD();
        $('#DtDocDate, #DtBillDate, #DtGRDate, #DtBillDateLD, #DtHoldDateCRDRNote, #DtBLDate, #Dtsysdate, #DtChDate, #DtPlDate, #DtHoldDate')
            .val(currentDate);
        wireEvents();
        await loadInitialDropdowns();
        
        // Load once and cache HTML
        await Promise.all([
            loadDropdown("item", $("<select>")),
            loadDropdown("tax", $("<select>")),
            loadDropdown("department", $("<select>"))
        ]);

        if (!isNaN(rowId) && rowId > 0) {
            isLoadForEdit = true;
            await loadFullQuotationByVno(rowId, rowIdVType, isReadOnly);
            setTimeout(() => {
                isLoadForEdit = false;
            }, 1000);
            checkApprovalStatus(rowIdVType, rowId, DBTableName);
        } else {
            addNewRowBelow();
        }
        if (isReadOnly) {
            setFormReadonly();
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
}

function bindHeaderEvents() {
    //------------ VType Change --------
    $('#ddlDocType').on('change', function () {
        if (isLoadForEdit) return;
        const vType = $(this).val();
        if (!rowId) {
            GetVNo(vType);
        }
        $(this).prop('disabled', true);
   });

    //--------------- MRN Change ------------
    $('#TxtMRNNo2').on('change', function () {
        if (isLoadForEdit) return;
        const mrnNo = $(this).val();
        if (!mrnNo) {
            return;
        }
        const mrnTypeNo = $(this).find(':selected').text().trim();
        const mrnType = $(this).find(':selected').data('vtype');
        $.ajax({
            url: "/PurchaseBillPassEntryDirect/ValidateMRN",
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
                    calculateItemTotals();
                    $("#TxtMRNNo2").focus();
                    return;
                }
                isMRNChange = true;
                LoadMRNData(mrnType, response.mrnNo);
            },
            error: function () {
                showToast("Error occurred.", { type: "error" });
            }
        });
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

    //=============Delete Row Button Click==========
    $(document).on('click', '.btn-delete-action', function () {
        // Prevent deleting if only one row exists
        const $tbody = $('#tblItemRecordPBPE tbody');
        if ($tbody.find('tr').length === 1) {
            return;
        }
        const $row = $(this).closest('tr');
        $row.remove();
    });

    //=============Add Row Button Click==========
    $(document).on('click', '.btn-add-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            return;
        }
        await addNewRowBelow();
    });
}

function bindAddressEvents() {
    //---------- Ship List Change ------
    $('#ddlShipFrom1').on('change', function () {
        if (isLoadForEdit || isMRNChange) return;
        const shipFromCode = $(this).val();
        $('#TxtAdd1SF').val('');
        $('#TxtAdd2SF').val('');
        $('#TxtAdd3SF').val('');
        $('#ddlCitySF').val('');
        $('#TxtPincodeSF').val('');
        $('#TxtGSTNoSF').val('');
        loadDropdown("address", "#ddlShipFromAddress", { shipFromCode: shipFromCode }).then(function () {
            const $addressDropdown = $('#ddlShipFromAddress');

            // Get first actual address, ignoring "-- Select Address --"
            const $firstAddress = $addressDropdown.find('option[value!=""]').first();
            if ($firstAddress.length) {
                $addressDropdown.val($firstAddress.val()).trigger('change');
            }
        });
    });

    //--------- Bill From List Change --------
    $('#ddlBillFrom').on('change', function () {
        if (isLoadForEdit || isMRNChange) return;
        const billFromCode = $(this).val();
        $('#TxtAdd1PD').val('');
        $('#TxtAdd2PD').val('');
        $('#TxtAdd3PD').val('');
        $('#ddlCityPD').val('');
        $('#NumPincodeBL').val('');
        $('#TxtGSTNo').val('');
        loadDropdown("address", "#ddlBillFromAddress", { billFromCode: billFromCode }).then(function () {
            const $addressDropdown = $('#ddlBillFromAddress');

            // Get first actual address, ignoring "-- Select Address --"
            const $firstAddress = $addressDropdown.find('option[value!=""]').first();
            if ($firstAddress.length) {
                $addressDropdown.val($firstAddress.val()).trigger('change');
            }
        });
    });

    //---------- Ship Address Change -----------
    bindAddressChange({
        addressSelector: '#ddlShipFromAddress',
        partySelector: '#ddlShipFrom1',
        add1Selector: '#TxtAdd1SF',
        add2Selector: '#TxtAdd2SF',
        add3Selector: '#TxtAdd3SF',
        gstSelector: '#TxtGSTNoSF',
        pincodeSelector: '#TxtPincodeSF',
        citySelector: '#ddlCitySF'
    });
    //---------- Bill Address Change ----------
    bindAddressChange({
        addressSelector: '#ddlBillFromAddress',
        partySelector: '#ddlBillFrom',
        add1Selector: '#TxtAdd1PD',
        add2Selector: '#TxtAdd2PD',
        add3Selector: '#TxtAdd3PD',
        gstSelector: '#TxtGSTNo',
        pincodeSelector: '#NumPincodeBL',
        citySelector: '#ddlCityPD'
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
        //if (isLoadForEdit) return;
        //const $row = $(this).closest("tr");
        //const selectedOption = $(this).find("option:selected");
        //const uomCode = selectedOption.data("ucode");
        //const uomName = selectedOption.data("unit");
        //$row.find(".uom-code").val(uomCode || "");
        //$row.find(".uom-name").val(uomName || "");
        itemNameChanged(this);
    });

    //--------- Row Calculation ---------
    $(document).on("change",
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
    $(document).on("change", ".cgst-per,.sgst-per,.igst-per", async function () {
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

    //----------- Item Total TDS % & TDS 194Q % Change ----------
    bindTDSChange('#TxtTds1', '#TxtTds2');
    bindTDSChange('#TxtTds194q1', '#TxtTds194q2');
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

    $("#TxtQCCreditNoteAmt, #TxtQualityCreditNoteAmt, #TxtWeightCreditNoteAmt, #TxtRateDiffCreditNoteAmt, #TxtOtherDebitAmt").on("blur", function () {
        calDrCrGrid();
    });
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
                url: '/PurchaseBillPassEntryDirect/GetNextPLNo',
                type: 'GET'
            });
            if (response.success) {
                $('#NumPlNo').val(response.plNo);
            }
        }
    });
}

function bindAddressChange({ addressSelector, partySelector, add1Selector, add2Selector, add3Selector, gstSelector, pincodeSelector, citySelector }) {
    $(addressSelector).on('change', async function () {

        if (isLoadForEdit) return;

        const $address = $(this);
        let addressId = $address.val();

        if (!addressId) {
            $address.prop('selectedIndex', 0);
            addressId = $address.val();
        }

        const code = $(partySelector).val();

        try {
            const response = await $.ajax({
                url: '/PurchaseBillPassEntryDirect/GetAddressByBillToParty',
                type: 'GET',
                data: {
                    code,
                    addressId
                }
            });

            const address = response.addressDetails;

            $(add1Selector).val(address.add1);
            $(add2Selector).val(address.add2);
            $(add3Selector).val(address.add3);
            $(gstSelector).val(address.gstin);
            $(pincodeSelector).val(address.pincode);
            await loadDropdown("city", citySelector, {}, address.cityCode);

        } catch (error) {
            toastr.error('Error loading address');
        }
    });
}

function bindTDSChange(source, target) {
    $(source).on('change', async function () {
        if (isLoadForEdit) return;

        await calculateTDSAmount(source, target);
    });
}
//------------- GENERATE VNO -----------------
async function GetVNo(vType) {
    try {
        const res = await fetch(`/PurchaseBillPassEntryDirect/GetVNo?vType=${encodeURIComponent(vType)}`);
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

//--------- MRN -----------
function loadMRNList(selectedValue = null) {

    return $.ajax({
        url: "/PurchaseBillPassEntryDirect/GetMrnNoList",
        type: "GET",
        dataType: "json"
    }).then(response => {

        const ddl = $("#TxtMRNNo2");

        let html = `<option value="">-- Select MRN No --</option>`;

        html += (response.data || []).map(item => `
            <option
                value="${item.Value}"
                data-vtype="${item.vType}">
                ${item.Text}
            </option>
        `).join("");

        ddl.html(html);

        initSelect2(ddl);

        if (selectedValue !== null && selectedValue !== "") {
            ddl.val(selectedValue).trigger("change");
        }

        return response.data || [];
    });
}


//--------- Generic ddl -------
async function loadDropdown(type, selector, data = {}, selectedValue = null) {

    const ddl = $(selector);
    const typeKey = type.toLowerCase();

    const defaultOptions = {
        //doctype: "-- Select Doc Type --",
        doctype: null,
        party: "-- Select --",
        drcr: "-- Select --",
        drcrbyvtype: "-- Select --",
        item: "-- Select --",
        address: "-- Select Address --",
        department: "-- Select --",
        city: "-- Select City --",
        currency: "-- Select Currency --",
        tax: "-- Select --",
        status: "-- Select Status --",
        transport: null,
        mrn: "-- Select MRN No --",
        transportgst: "-- Select GST --"
    };

    const defaultOption =
        Object.prototype.hasOwnProperty.call(defaultOptions, typeKey)
            ? defaultOptions[typeKey]
            : "-- Select --";
    const cacheTypes = [
        "doctype",
        "party",
        "drcr",
        "item",
        "department",
        "city",
        "currency",
        "tax"
    ];

    const useCache = cacheTypes.includes(typeKey);

    // --------------------------------------------------------
    // CACHE
    // --------------------------------------------------------
    if (useCache && dropdownCache[typeKey]) {
        const { list, html } = dropdownCache[typeKey];
        bindPBDdl(ddl, list, html, typeKey, selectedValue);
        return list;
    }

    // --------------------------------------------------------
    // EXISTING REQUEST
    // --------------------------------------------------------
    if (useCache && dropdownPromiseCache[typeKey]) {
        const { list, html } = await dropdownPromiseCache[typeKey];
        bindPBDdl(ddl, list, html, typeKey, selectedValue);
        return list;
    }

    // --------------------------------------------------------
    // REQUEST
    // --------------------------------------------------------
    const promise = $.ajax({
        url: "/PurchaseBillPassEntryDirect/GetList",
        type: "GET",
        data: {
            type,
            ...data
        },
        dataType: "json"

    }).then(response => {

        if (!response.success) {
            throw new Error(
                response.message || "Unable to load list"
            );
        }

        const list = response.data || [];
        let html = "";

        // Default option
        if (defaultOption !== null) {
            html += `<option value="">${defaultOption}</option>
            `;
        }

        // Build options
        html += buildOptions(typeKey, list);

        return { list, html };
    });

    // --------------------------------------------------------
    // SAVE REQUEST
    // --------------------------------------------------------
    if (useCache) {
        dropdownPromiseCache[typeKey] = promise;
    }

    try {

        const result = await promise;
        // Save cache
        if (useCache) {
            dropdownCache[typeKey] = result;
            delete dropdownPromiseCache[typeKey];
        }

        // Bind
        bindPBDdl(ddl, result.list, result.html, typeKey, selectedValue);
        return result.list;
    }
    catch (error) {

        if (useCache) {
            delete dropdownPromiseCache[typeKey];
        }
        console.error(`Error loading dropdown: ${type}`, error);
        throw error;
    }
}
function buildOptions(type, list) {

    switch (type) {

        case "item":
            return list.map(item => `
                <option
                    value="${item.Value}"
                    data-unit="${item.unit || ""}"
                    data-ucode="${item.ucode || ""}">
                    ${item.Text || ""}
                </option>
            `).join("");


        case "tax":
            return list.map(item => `
                <option
                    value="${item.Value}"
                    data-cgst="${item.CGST_PER || 0}"
                    data-sgst="${item.SGST_PER || 0}"
                    data-igst="${item.IGST_PER || 0}"
                    data-vat="${item.VAT_PER || 0}"
                    data-tds="${item.TDS_PER || 0}"
                    data-tcs="${item.TCS_PER || 0}"
                    data-oth="${item.OTH_PER || 0}"
                    data-oth2="${item.OTH_PER2 || 0}">
                    ${item.Text || ""}
                </option>
            `).join("");


        case "mrn":
            return list.map(item => `
                <option
                    value="${item.Value}"
                    data-vtype="${item.vType || ""}">
                    ${item.Text || ""}
                </option>
            `).join("");

        default:
            return list.map(item => `
                <option value="${item.Value}">
                    ${item.Text}
                </option>
            `).join("");
    }
}

function bindPBDdl(ddl, list, html, type, selectedValue = null) {

    ddl.html(html);

    // Select2
    if (
        [
            "party",
            "drcr",
            "department",
            "transport",
            "mrn"
        ].includes(type)
    ) {
        initSelect2(ddl);
    }

    // Auto-select first value
    if (
        [
            "status",
            "currency",
            "transportgst"
        ].includes(type)
        && list.length
    ) {
        ddl.val(list[0].Value)
            .trigger("change");
    }

    if (type === "doctype" && list.length === 1) {
        ddl.val(String(list[0].Value))
            .trigger("change");
    }

    if (selectedValue !== null && selectedValue !== undefined && selectedValue !== "") {
        ddl.val(String(selectedValue)).trigger("change");
    }
}
//---------- Load All Initially on Page Load -------------
async function loadInitialDropdowns() {

    await Promise.all([

        loadDropdown("status", "#ddlStatus"),
        loadDropdown("doctype", "#ddlDocType"),

        loadMRNList(),
        loadDropdown("drcrbyvtype", "#ddlDebitAC"),

        loadDropdown("party", "#ddlBillFrom"),
        loadDropdown("party", "#ddlShipFrom1"),

        loadDropdown("city", "#ddlCityPD"),
        loadDropdown("city", "#ddlCitySF"),

        loadDropdown("transport", "#ddlTransportName"),

        loadDropdown("drcr", "#ddlCreditAC"),
        loadDropdown("drcr", "#ddlFreightDebitAC"),
        loadDropdown("drcr", "#ddlFreightCreditAC"),
        loadDropdown("drcr", "#ddlWBDebitAC"),
        loadDropdown("drcr", "#ddlWBCreditAC"),
        loadDropdown("drcr", "#ddlUnloadDebitAC"),
        loadDropdown("drcr", "#ddlUnloadCreditAC"),
        loadDropdown("drcr", "#ddlTdsAccount"),
    ]);
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
            url: "/PurchaseBillPassEntryDirect/GetFullQuotationByVno",
            type: "GET",
            dataType: "json",
            data: { vNo: vNo, vtype: vType }
        });

        console.log("Quotation :", response);

        if (!response.success || !response.header) {
            showToast("Quotation not found.", { type: "warning" });
            return;
        }

        const header = response.header;
        const items = response.items || [];
        const attachments = response.attachments || [];
        
        //==========================
        // Document Details
        //==========================

        await loadDropdown("doctype", "#ddlDocType", {}, header.v_TYPE);
        $("#DtDocDate").val(formatDateYMD(header.v_DATE));
        $("#NumDocNo").val(header.v_NO);


        const vTypeToBind = $("#ddlDocType").val();
        await loadMRNList(header.reF_NO);
        //==========================
        // Bill From
        //==========================

        await loadDropdown("party", "#ddlBillFrom", {}, header.partY_CODE);
        $("#TxtAdd1PD").val(header.bilL_ADD1 || "");
        $("#TxtAdd2PD").val(header.bilL_ADD2 || "");
        $("#TxtAdd3PD").val(header.bilL_ADD3 || "");
        await loadDropdown("city", "#ddlCityPD", {}, header.bilL_CITY);
        $("#TxtGSTNo").val(header.bilL_GST || "");
        
        //==========================
        // Ship From
        //==========================

        await loadDropdown("party", "#ddlShipFrom1", {}, header.shiP_CODE);

        $("#TxtAdd1SF").val(header.shiP_ADD1 || "");
        $("#TxtAdd2SF").val(header.shiP_ADD2 || "");
        $("#TxtAdd3SF").val(header.shiP_ADD3 || "");
        await loadDropdown("city", "#ddlCitySF", {}, header.shiP_CITY);
        $("#TxtGSTNoSF").val(header.shiP_GST || "");

        //==========================
        // Bill Details
        //==========================

        $("#TxtBillNo").val(header.bilL_NO || "");
        setDateControl(header.bilL_DATE, "#DtBillDate", "#chkBillDate");

        $("#TxtChallanNo").val(header.chalL_NO || "");
        setDateControl(header.chalL_DATE, "#DtChDate", "#chkChDate");


        $("#TxtWaybillNo").val(header.waybilL_NO || "");

        //==========================
        // Accounts
        //==========================
        await loadDropdown("drcr", "#ddlDebitAC", {}, header.debiT_AC);
        await loadDropdown("drcr", "#ddlCreditAC", {}, header.crediT_AC);
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
        await loadDropdown("drcr", "#ddlFreightDebitAC", {}, header.frtpaY_DRAC);
        await loadDropdown("drcr", "#ddlFreightCreditAC", {}, header.frtpaY_CRAC);

        $("#NumTDSonFRT1").val(header.frT_TDSPER || 0);
        $("#NumTDSonFRT2").val(header.frT_TDS || 0);
        
        //==========================
        // Weigh Bridge
        //==========================

        $("#NumWBAmount").val(header.wB_AMT || 0);
        $("#NumWBTDS1").val(header.wB_TDSPER || 0);
        $("#NumWBTDS2").val(header.wB_TDS || 0);
        $("#TxtWBNarration").val(header.wB_NARR || "");
        await loadDropdown("drcr", "#ddlWBDebitAC", {}, header.wB_DRACT);
        await loadDropdown("drcr", "#ddlWBCreditAC", {}, header.wB_CRACT);

        //==========================
        // Unloading
        //==========================

        $("#NumUnloadAmt").val(header.uL_AMT || 0);
        $("#NumUnloadTDS1").val(header.uL_TDSPER || 0);
        $("#NumUnloadTDS2").val(header.uL_TDS || 0);
        $("#TxtUnloadNarration").val(header.uL_NARR || "");
        await loadDropdown("drcr", "#ddlUnloadDebitAC", {}, header.uL_DRACT);
        await loadDropdown("drcr", "#ddlUnloadCreditAC", {}, header.uL_CRACT);

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

        //Cr/Dr Btn visibility
        checkDrCrNoteVisibility();
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

            <td class="freeze-item"><select class="form-control form-control-sm item-name"></select></td>

            <td><input class="form-control form-control-sm hsn-code" type="text" value="${data.hsN_CODE || data.HSN_CODE || ''}"/></td>
            <td>
                <input class="form-control form-control-sm uom-code" type="hidden" value="${data.uoM_CODE || data.UOM_CODE || ''}" disabled/>
                <input class="form-control form-control-sm uom-name" type="text" value="${data.unit || data.UNIT || ''}" disabled/>
            </td>

            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos ?? data.NOS ?? ''}"/></td>
            <td><input class="form-control form-control-sm recd-qty" type="number" value="${data.recD_QTY ?? data.RECD_QTY ?? ''}" /></td>
            <td><input class="form-control form-control-sm bill-qty" type="number" value="${data.bilL_QTY ?? data.BILL_QTY ?? ''}"/></td>

            <td><input class="form-control form-control-sm usd-rate" type="number" value="${data.usD_RATE ?? data.USD_RATE ?? ''}"/></td>
            <td><input class="form-control form-control-sm exch-rate" type="number" value="${data.excH_RATE ?? data.EXCH_RATE ?? ''}"/></td>
            <td><input class="form-control form-control-sm rate" type="number" value="${data.rate ?? data.RATE ?? ''}"/></td>
            <td><input class="form-control form-control-sm amount" type="number" value="${data.amount ?? data.AMOUNT ?? ''}"/></td>

            <td>
                <select class="form-control form-control-sm rcm-yn">
                    <option value="">-- Select --</option>
                    <option value="YES" ${(data.rcM_YN || data.RCM_YN || '').toUpperCase() === 'YES' ? 'selected' : ''}>YES</option>
                    <option value="NO" ${(data.rcM_YN || data.RCM_YN || '').toUpperCase() === 'NO' ? 'selected' : ''}>NO</option>
                </select>
            </td>

            <td>
                <select class="form-control form-control-sm input-yn">
                    <option value="">-- Select --</option>
                    <option value="YES" ${(data.inpuT_YN || data.INPUT_YN || '').toUpperCase() === 'YES' ? 'selected' : ''}>YES</option>
                    <option value="NO" ${(data.inpuT_YN || data.INPUT_YN || '').toUpperCase() === 'NO' ? 'selected' : ''}>NO</option>
                </select>
            </td>

            <td><select class="form-control form-control-sm tax-code"></select></td>

            <td><input class="form-control form-control-sm pack-per" type="number" value="${data.pacK_PER ?? data.PACK_PER ?? ''}"/></td>
            <td><input class="form-control form-control-sm pack-amt" type="number" value="${data.pacK_AMT ?? data.PACK_AMT ?? ''}"/></td>

            <td><input class="form-control form-control-sm disc-per" type="number" value="${data.disC_PER ?? data.DISC_PER ?? ''}"/></td>
            <td><input class="form-control form-control-sm disc-amt" type="number" value="${data.disC_AMT ?? data.DISC_AMT ?? ''}"/></td>

            <td><input class="form-control form-control-sm cgst-per" type="number" value="${data.cgsT_PER ?? data.CGST_PER ?? ''}" disabled/></td>
            <td><input class="form-control form-control-sm cgst-amt" type="number" value="${data.cgsT_AMT ?? data.CGST_AMT ?? ''}" ${(data.cgsT_PER || data.CGST_PER) ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm sgst-per" type="number" value="${data.sgsT_PER ?? data.SGST_PER ?? ''}" disabled/></td>
            <td><input class="form-control form-control-sm sgst-amt" type="number" value="${data.sgsT_AMT ?? data.SGST_AMT ?? ''}" ${(data.sgsT_PER || data.SGST_PER) ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm igst-per" type="number" value="${data.igsT_PER ?? data.IGST_PER ?? ''}" disabled/></td>
            <td><input class="form-control form-control-sm igst-amt" type="number" value="${data.igsT_AMT ?? data.IGST_AMT ?? ''}" ${(data.igsT_PER || data.IGST_PER) ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm cess-per" type="number" value="${data.cesS_PER ?? data.CESS_PER ?? ''}"/></td>
            <td><input class="form-control form-control-sm cess-amt" type="number" value="${data.cesS_AMT ?? data.CESS_AMT ?? ''}"/></td>

            <td><input class="form-control form-control-sm vat-per" type="number" value="${data.vaT_PER ?? data.VAT_PER ?? ''}"/></td>
            <td><input class="form-control form-control-sm vat-amt" type="number" value="${data.vaT_AMT ?? data.VAT_AMT ?? ''}"/></td>

            <td><input class="form-control form-control-sm oth-amt" type="number" value="${data.otH_AMT ?? data.OTH_AMT ?? ''}"/></td>
            <td><input class="form-control form-control-sm net-amt" type="number" value="${data.neT_AMT ?? data.NET_AMT ?? ''}" disabled/></td>

            <td>
                <input class="form-control form-control-sm make-code" type="hidden" value="${data.makE_CODE ?? data.MAKE_CODE ?? ''}"/>
                <input class="form-control form-control-sm make-name" type="text" value="${data.make || data.MAKE || ''}" disabled/>
            </td>
            <td>
                <select class="form-control form-control-sm dept-code" disabled></select>
            </td>

            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || data.REMARKS || ''}"/></td>

            <td><input class="form-control form-control-sm land-rate" type="number" value="${data.lanD_RATE ?? data.LAND_RATE ?? ''}" /></td>
            <td><input class="form-control form-control-sm land-amt" type="number" value="${data.lanD_AMT ?? data.LAND_AMT ?? ''}" /></td>

            <td><input class="form-control form-control-sm poland-rate" type="number" value="${data.polanD_RATE ?? data.POLAND_RATE ?? ''}" /></td>
            <td><input class="form-control form-control-sm po-rate" type="number" value="${data.pO_RATE ?? data.PO_RATE ?? ''}" /></td>

            <td><input class="form-control form-control-sm po-type" type="text" value="${data.pO_TYPE || data.PO_TYPE || ''}" /></td>
            <td><input class="form-control form-control-sm po-no" type="number" value="${data.pO_NO || data.PO_NO || ''}" /></td>

            <td><input class="form-control form-control-sm kanta-type" type="text" value="${data.kantA_TYPE || data.KANTA_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm kanta-no" type="number" value="${data.kantA_NO || data.KANTA_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm req-type" type="text" value="${data.reQ_TYPE || data.REQ_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm req-no" type="number" value="${data.reQ_NO || data.REQ_NO || ''}" disabled/></td>

            <td><input class="form-control form-control-sm ref-type" type="text" value="${data.reF_TYPE || data.REF_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm ref-no" type="number" value="${data.reF_NO || data.REF_NO || ''}" disabled/></td>

            <td class="action-col">
                <div class="action-wrap">
                    <button type="button" class="act-btn delete btn-delete-action" title="Delete Row"><i class="fa fa-trash"></i></button>
                    <button type="button" class="act-btn add btn-add-action" title="Add Row"><i class="fa fa-plus-circle"></i></button>
                </div>
            </td>

        </tr>
    `;
}

async function addNewRowBelow(data = null) {

    data = data || {};

    let rowHtml = createRowHtml(data);

    $("#tblItemRecordPBPE tbody").append(rowHtml);

    const $lastRow = $("#tblItemRecordPBPE tbody tr:last");

    $lastRow.find(".item-name").html(dropdownCache.item.html);
    $lastRow.find(".tax-code").html(dropdownCache.tax.html);
    $lastRow.find(".dept-code").html(dropdownCache.department.html);

    //$lastRow.find(".item-name").val(data.iteM_CODE || data.ITEM_CODE || "").trigger("change");
    const itemName = $lastRow[0].querySelector(".item-name");
    itemName.value = data.iteM_CODE || data.ITEM_CODE || "";
    //itemName.dispatchEvent(new Event("change", { bubbles: true }));
    itemNameChanged(itemName);

    $lastRow.find(".tax-code").val(data.taX_CODE || data.TAX_CODE || "");
    $lastRow.find(".dept-code").val(data.depT_CODE || data.DEPT_CODE || "");
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
    
    // Bill From
    $('#ddlBillFrom').val('').trigger('change');
    $('#ddlBillFromAddress').val('').trigger('change');
    $('#TxtAdd1PD').val('');
    $('#TxtAdd2PD').val('');
    $('#TxtAdd3PD').val('');
    $('#ddlCityPD').val('').trigger('change');
    $('#TxtGSTNo').val('');
    

    // Ship To
    $('#ddlShipFrom1').val('').trigger('change');
    $('#ddlShipFromAddress').val('').trigger('change');
    $('#TxtAdd1SF').val('');
    $('#TxtAdd2SF').val('');
    $('#TxtAdd3SF').val('');
    $('#ddlCitySF').val('').trigger('change');
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
async function LoadMRNData(vType, vNo) {
    $.ajax({
        url: '/PurchaseBillPassEntryDirect/GetPurchaseDetailsByMRN',
        type: 'GET',
        data: { vType: vType, vNo: vNo },
        dataType: 'json',
        success: async function (response) {
            if (response.success) {
                const data = response.data;
                console.log("MRN Header data: ", data);
                
                $('#NumExRate').val(data.excH_RATE || 0);

                loadDropdown("drcr", "#ddlCreditAC", {}, data.partY_CODE);
                //================Bill Details
                loadDropdown("party", "#ddlBillFrom", {}, data.partY_CODE);

                $('#TxtAdd1PD').val(data.bilL_ADD1 || '');
                $('#TxtAdd2PD').val(data.bilL_ADD2 || '');
                $('#TxtAdd3PD').val(data.bilL_ADD3 || '');
                loadDropdown("city", "#ddlCityPD", {}, data.bilL_CITY);
                $('#TxtGSTNo').val(data.bilL_GST || '');
                
                //===============Ship Details
                loadDropdown("party", "#ddlShipFrom1", {}, data.shiP_CODE);

                $('#TxtAdd1SF').val(data.shiP_ADD1 || '');
                $('#TxtAdd2SF').val(data.shiP_ADD2 || '');
                $('#TxtAdd3SF').val(data.shiP_ADD3 || '');
                loadDropdown("city", "#ddlCitySF", {}, data.shiP_CITY);
                $('#TxtGSTNoSF').val(data.shiP_GST || '');
                $('#txtRemarks').val(data.remarks || '');

                
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
            url: '/PurchaseBillPassEntryDirect/GetPurchaseItemsByMRN',
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
                await addNewRowBelow(item);

                let $row = $("#tblItemRecordPBPE tbody tr:last");

                // Calculations
                await calculateAmt($row, item.iteM_CODE);
                calculateTax($row, item.iteM_CODE);
                calculateLandAmount($row, item.iteM_CODE);

            }
            catch (err) {

                console.error(err);
                showToast(err.message || err, { type: "error" });

            }
        }

        // Calculate once after all rows are loaded
        calculateItemTotals();
    }
    catch (xhr) {
        console.error(xhr);
        showToast(xhr.responseJSON?.message || xhr.responseText || "Unable to load Purchase Items.", { type: "error" });
    }
}

function GetItemOrderRatesByPO(poType, poNo, itemCode) {

    return $.ajax({
        url: '/PurchaseBillPassEntryDirect/GetItemOrderRatesByPO',
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
        url: '/PurchaseBillPassEntryDirect/GetPackOnBasic',
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

    const landRate = Number(
        (rate + packRate - discRate + taxRate).toFixed(2)
    );
    const landAmt = Number(
        (billQty * landRate).toFixed(2)
    );


    $row.find(".land-rate").val(landRate.toFixed(2));
    $row.find(".land-amt").val(landAmt.toFixed(2));

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

        //mrnType: $("#TxtMRNNo1").val() || "",
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
            url: "/PurchaseBillPassEntryDirect/CalculateDebitNote",
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
        showToast("Unable to calculate Debit Note.", { type: "error" });
    }
}

//------------ CALCULATE FREIGHT -------------
async function CalcFreightAndCrDr(request) {
    try {

        const result = await $.ajax({
            url: "/PurchaseBillPassEntryDirect/CalculateFrieght",
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
        loadDropdown("drcr", "#ddlFreightDebitAC", {}, result.frtDrAcCode || 0);

    }
    catch (ex) {
        console.error(ex);
        showToast("Unable to calculate Debit Note.", { type: "error" });
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

        distributeAmount(total, totalAmount, amountColumn, perColumn);
        $(this).val(total.toFixed(2));
    });

}
//----------- GET EXISTING TDS ----------
async function checkExistingTDS() {

    const billNo = `${$('.po-type').first().val() || ''}${parseInt($('.po-no').first().val()) || 0}`;
    const drCode = parseInt($('#ddlBillFrom').val()) || 0;

    return await $.ajax({
        url: '/PurchaseBillPassEntryDirect/CheckExistingTDS',
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
        url: '/PurchaseBillPassEntryDirect/GetFrtCrAcByTransCode',
        type: 'GET',
        data: {
            transportCode: transportCode
        },
        success: function (response) {

            if (!response.success)
                return;

            // Freight Credit A/C
            $('#ddlFreightCreditAC').val(response.partyCode).trigger('change');
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
        
        REF_TYPE: $('#TxtMRNNo2').find(':selected').data('vtype') || "",
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
        BILL_DATE: getOptionalDate('#chkBillDate', '#DtBillDate'),

        CHALL_NO: $('#TxtChallanNo').val() || "",
        CHALL_DATE: getOptionalDate('#chkChDate', '#DtChDate'),

        BL_NO: $('#TxtBLNo').val() || "",
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
        PL_DATE: getOptionalDate('#chkPlDate', '#DtPlDate'),
        BILLAMT_USD: parseFloat($('#TxtPartyUsd').val()) || 0,

        //------------- Logistic Details ----------
        TRANSPORT_CODE: parseInt($('#ddlTransportName').val()) || 0,
        TRANSPORT_NAME: $('#ddlTransportName option:selected').text() || "",

        TRUCK_NO: $('#txtVehicleNo').val() || "",
        CONTAINER_NO: $('#txtContainerNo').val() || "",

        GR_NO: $('#txtGRNo').val() || "",
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
        });
    });

    //Attachments
    const Attachement = getUploadedFiles();

    console.log("headerData: ", headerData)
    console.log("rowsData: ", rowsData)
    console.log("Attachement: ", Attachement)
    
    return {
        headerData,
        rowsData,
        Attachement
    };
}

async function saveUpdateData() {
    const rowsData = await collectPurchaseBillData();

    if (rowsData.length === 0) {
        toastr.warning("Please add at least one row before saving.");
        return;
    }

    const data = {
        header: rowsData.headerData,
        lineRows: rowsData.rowsData,
        Attachement: rowsData.Attachement
    };

    //3. AJAX Save
    $.ajax({
        url: '/PurchaseBillPassEntryDirect/SavePurchaseBillPassEntry',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (response) {
            if (response.success) {
                showToast('Saved successfully!', { type: "success" });
                setFormReadonly();
                isReadOnly = true;
                setTimeout(() => window.location.href = '/PurchaseBillPassEntryDirect/Index?id=' + encodeURIComponent($('#NumDocNo').val()) + '&vtype=' + encodeURIComponent($('#ddlDocType').val()) + '&readOnly=true', 1000);
                if (isReadOnly) {
                    const vNo = $('#NumDocNo').val();
                    checkApprovalStatus(vType, vNo, DBTableName);
                }
            } else {
                toastr.error('Error: ' + response.message);
            }
        },
        error: function (xhr, status, error) {
            toastr.error('AJAX error: ' + error);
        }
    });
}

function getOptionalDate(checkboxSelector, dateSelector) {
    return $(checkboxSelector).is(':checked') ? (parseNullableDate($(dateSelector).val()) || null) : null;
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

    //----------------- Validate Frieght Debit A/c ---------------
    if (Number($('#NumFreightPay').val()) > 0 && Number($('#ddlFreightDebitAC').val()) === 0) {
        setInvalid($('#ddlFreightDebitAC'), "Freight Debit A/c Required.");
        isValid = false;
        return false;
    }

    //----------------- Validate Frieght Credit A/c ---------------
    if (Number($('#NumFreightPay').val()) > 0 && Number($('#ddlFreightCreditAC').val()) === 0) {
        setInvalid($('#ddlFreightCreditAC'), "Freight Credit A/c Required.");
        isValid = false;
        return false;
    }

    //----------------- Validate Freight Debit And Credit A/c ---------------
    if (Number($('#NumFreightPay').val()) > 0 &&
        (!$('#ddlFreightDebitAC').val() || Number($('#ddlFreightDebitAC').val()) === 0) &&
        (!$('#ddlFreightCreditAC').val() || Number($('#ddlFreightCreditAC').val()) === 0)) {

        setInvalid($('#ddlFreightDebitAC'), "Freight Debit A/c and Credit A/c must be selected if Freight amount > 0.");
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

    //----------------- Validate Transport Name ---------------
    if (Number($('#NumFreightPay').val()) > 0 && !$('#ddlTransportName').val()) {
        setInvalid($('#ddlTransportName'), "Transport Name must be selected if Freight Amount is greater than 0.");
        isValid = false;
        return false;
    }

    ////----------------- Validate GR Date ---------------
    //if (Number($('#NumFreightPay').val()) > 0 && !$('#chkGRDate').is(':checked') && $('#txtGRNo').val().trim() !== "" && $('#txtGRNo').val().trim() !== "0") {
    //    setInvalid($('#DtGRDate'), "GR Date required with GR Number.");
    //    isValid = false;
    //    return false;
    //}

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
    if ((Number($('#NumWBAmount').val()) > 0 || Number($('#NumWBTDS2').val()) > 0) && Number($('#ddlWBDebitAC').val()) === 0 && Number($('#ddlWBCreditAC').val()) === 0) {
        setInvalid($('#ddlWBDebitAC'), "Weighbridge Debit A/c must be selected if Weighbridge Amount or Weighbridge Tax Amount is greater than 0.");
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
    if ((Number($('#NumUnloadAmt').val()) > 0 || Number($('#NumUnloadTDS2').val()) > 0) && Number($('#ddlUnloadDebitAC').val()) === 0 && Number($('#ddlUnloadCreditAC').val()) === 0) {
        setInvalid($('#ddlUnloadDebitAC'), "Unloading Debit A/c must be selected if Unloading Amount or Unloading Tax Amount is greater than 0.");
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

        const taxValidation = await validateTaxType(Number($('#ddlCityPD').val()), Number($('#NumIgst').val()), Number($('#NumCgst').val()),
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

    return isValid;

}

async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/PurchaseBillPassEntryDirect/CheckValidDate', {
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
    const mrnType = $('#TxtMRNNo2').find(':selected').data('vtype');
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
                // HSN mismatch
                if (result.hsnMismatch) {
                    showToast(result.hsnMessage, { type: "warning" });
                    itemVsBillHSNCodeDiff = result.item_vs_Bill_HSNCodeDiff;
                }
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

            //Validate Tax
            if (Number(c.taxCode.value) === 0) {
                setInvalid(c.taxCode, "Tax Type not selected.");
                return false;
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
            url: "/PurchaseBillPassEntryDirect/ValidatePurchaseRow",
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


async function getPurchaseVoucherNo(transportName, grNo, currentVoucher, purchaseOrSale) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntryDirect/GetPurchaseOrSaleVoucherNo",
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
            url: "/PurchaseBillPassEntryDirect/CheckPaymentExists",
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


async function checkDuplicateBill(partyCode, billNo, currentVNo) {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntryDirect/CheckDuplicateBill",
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
            url: "/PurchaseBillPassEntryDirect/ValidateTaxType",
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
            url: "/PurchaseBillPassEntryDirect/ValidatePartyGst",
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
        crNoteAmt: row.querySelector(".cr-note-amt"),

        qltyDiffDrAmt: row.querySelector(".qlty-diff-dr-amt"),
        rateDiffDrAmt: row.querySelector(".rate-diff-dr-amt"),
        qcDiffDrAmt: row.querySelector(".qc-diff-dr-amt"),
        qtyDiffDrAmt: row.querySelector(".qty-diff-dr-amt"),
        otherDiffDrAmt: row.querySelector(".other-diff-dr-amt"),

    };
}

async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/PurchaseBillPassEntryDirect/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            pubDefPOInMRN = d.pubDefPOInMRN;
            compCode = d.compCode;
            yearCode = d.yearCode;
            branchCode = d.branchCode;
            dataSource = d.dataSource;
            userLevel = d.userLevel;
            companyName = d.companyName;
            add1 = d.add1;
            add2 = d.add2;
            db = d.db;
            companyGst = d.companyGst;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
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

//--------------------Reports------------------
//Dr Note

async function DebitNoteReport() {

    var reportName = "";
    const vType = ($("#ddlDocType").val() || "").trim();
    const vNo = parseInt($("#NumDocNo").val() || 0);

    if (vType === "RMPB" || vType === "BFPB") {
        reportName = "rawvoucherRM";
    }
    else {
        reportName = "rawvoucher";
    }
    // Crystal Report Formula
    var formula =
        "{PURCHASE1.V_TYPE} = '" + vType + "'" +
        " AND {PURCHASE1.V_NO} = " + vNo +
        " AND {PURCHASE1.COMP_CODE} = " + compCode +
        " AND {PURCHASE1.YEAR_CODE} = " + yearCode +
        " AND {PURCHASE1.BRANCH_CODE} = " + branchCode;


    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,
        Parameters: {
            RPTNAME: "DEBIT NOTE",
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2
        //    gst: "GSTIN: " + companyGst
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

//Cr Note
async function CreditNoteReport() {

    var reportName = "rawvoucher1";
    const vType = ($("#ddlDocType").val() || "").trim();
    const vNo = parseInt($("#NumDocNo").val() || 0);

    // Crystal Report Formula
    const formula =
        "{PURCHASE1.V_TYPE} = '" + vType + "'" +
        " AND {PURCHASE1.V_NO} = " + vNo +
        " AND {PURCHASE1.COMP_CODE} = " + compCode +
        " AND {PURCHASE1.YEAR_CODE} = " + yearCode +
        " AND {PURCHASE1.BRANCH_CODE} = " + branchCode;

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,
        Parameters: {
            RPTNAME: "CREDIT NOTE",
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2
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

//-----------------End Reports----------------
//-----------------HELPERS----------------
function setFormReadonly() {
    const form = $('#PurchaseBillPassEntryForm');
    $('#btn-save').hide();
    form.addClass('erppage-readonly');
    //form.find('input, textarea, select').prop('disabled', true);
    $('#copyFromDropdown, .erppagedropdown-toggle')
        .prop('disabled', true)
        .removeAttr('data-bs-toggle')
        .css({
            'opacity': '0.5',
            'cursor': 'not-allowed',
            'pointer-events': 'none'
        });

    $('.btn-add-action, .btn-delete-action, #btnAdvanceTDS')
        .prop('disabled', true)
        .css({
            'opacity': '0.5',
            'cursor': 'not-allowed',
            'pointer-events': 'none'
        });
    $('#dropZone')
        .css({
            'pointer-events': 'none',
            //'opacity': '0.65',      
            'cursor': 'not-allowed'
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
            vType: rowIdVType
        },
        success: function (response) {
            console.log('Response:', response);
            // Pending with another user
            if (response.success === false) {
                showToast(`Pending With Another User : ${response.fullName} (${response.userCode})`,
                    { type: "warning" });
                return;
            }
            // Approval_Code = 5
            if (response.approvalCode8 === true) {
                OpenApprovalModal({
                    DocType: rowIdVType,
                    DocNo: $('#NumDocNo').val(),
                    TableName: DBTableName
                });
                return;
            }
            // Approval_Code != 8
            OpenSendForApprovalModal({
                DocType: rowIdVType,
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
    OpenApprovalModal({
        DocType: rowIdVType,
        DocNo: $('#NumDocNo').val(),
        TableName: DBTableName
    });
});
//===========Cr/Dr Note Button Visibilty============
function checkDrCrNoteVisibility() {

    // Initially hide all
    $('#BtnDrNotePrint').hide();
    $('#BtnCrNotePrint').hide();

    const drTotal =
        (parseFloat($('#TxtTds194q2').val()) || 0) +
        (parseFloat($('#TxtQualityDiffDebitAmt').val()) || 0) +
        (parseFloat($('#TxtQualityDiffDebitTax').val()) || 0) +
        (parseFloat($('#TxtRateDiffDebitAmt').val()) || 0) +
        (parseFloat($('#TxtRateDiffDebitTax').val()) || 0) +
        (parseFloat($('#TxtQCDebitNoteAmt').val()) || 0) +
        (parseFloat($('#TxtQCDebitNoteTax').val()) || 0) +
        (parseFloat($('#TxtWeightDebitAmt').val()) || 0) +
        (parseFloat($('#TxtWeightDebitTax').val()) || 0) +
        (parseFloat($('#TxtOtherDebitAmt').val()) || 0) +
        (parseFloat($('#TxtOtherDebitTax').val()) || 0);


    if (drTotal > 0) {

        $('#BtnDrNotePrint').show();

        return;
    }


    const crTotal =
        (parseFloat($('#TxtQualityCreditNoteAmt').val()) || 0) +
        (parseFloat($('#TxtQualityCreditNoteVal').val()) || 0) +
        (parseFloat($('#TxtQCCreditNoteAmt').val()) || 0) +
        (parseFloat($('#TxtQCCreditNoteVal').val()) || 0) +
        (parseFloat($('#TxtWeightCreditNoteAmt').val()) || 0) +
        (parseFloat($('#TxtWeightCreditNoteVal').val()) || 0) +
        (parseFloat($('#TxtRateDiffCreditNoteAmt').val()) || 0) +
        (parseFloat($('#TxtRateDiffCreditNoteVal').val()) || 0);


    if (crTotal > 0) {

        $('#BtnCrNotePrint').show();
    }
}
//==========Duplicate==========
function checkDuplicateItems(currentSelect) {

    const value = currentSelect.value;
    if (!value) return false;

    let duplicate = false;

    document.querySelectorAll('#tblItemRecordPBPE .item-name').forEach(el => {
        if (el === currentSelect) return;

        if (el.value === value) {
            duplicate = true;
        }
    });

    if (duplicate) {

        $(currentSelect).addClass("is-invalid");
        showToast("Duplicate item found!", { type: "warning" });
    } else {
        $(currentSelect).removeClass("is-invalid");
    }

    return duplicate;
}

function itemNameChanged(itemName) {
    if (isLoadForEdit) return;

    const row = itemName.closest("tr");
    const selectedOption = itemName.options[itemName.selectedIndex];

    const uomCode = selectedOption?.dataset.ucode;
    const uomName = selectedOption?.dataset.unit;

    row.querySelector(".uom-code").value = uomCode || "";
    row.querySelector(".uom-name").value = uomName || "";

    // Duplicate
    if (checkDuplicateItems(itemName)) {
        return;
    }
}