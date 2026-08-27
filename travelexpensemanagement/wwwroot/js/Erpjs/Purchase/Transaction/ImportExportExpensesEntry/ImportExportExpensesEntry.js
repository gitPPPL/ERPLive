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
        await loadInitialDropdowns();
        wireEvents();
        loadCopyFromMenu();

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
    //bindBankEvents();
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
        $(this).prop('disabled', true);
        loadMRNList(vType);
        $('#TxtMRNNo1').val('');
        //SetLatestDebitAccount(vType);
        loadDropdown("drcrbyvtype", "#ddlDebitAC", { vType });
        loadDropdown("drcrbyvtype", "#ddlFreightDebitAC", { vType });
    });

    //--------------- MRN Change ------------
    $('#TxtMRNNo2').on('change', function () {
        if (isLoadForEdit) return;
        const mrnNo = $(this).val();
        if (!mrnNo) {
            $("#TxtMRNNo1").val("");
            return;
        }
        let mrnType = $(this).find(':selected').data('vtype');
        let docType = $('#ddlDocType').val();

        if (docType === "RMDP") {
            $("#TxtMRNNo1").val(mrnType);
            loadRMDPMRNData(mrnType, mrnNo);
        }
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
        $('#ddlStateSF').val('');
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
        $('#ddlStatePD').val('');
        $('#NumPincodeBL').val('');
        $('#TxtGSTNo').val('');
        loadDropdown("address", "#ddlBillFromAddress", { shipFromCode: billFromCode }).then(function () {
            const $addressDropdown = $('#ddlBillFromAddress');

            // Get first actual address, ignoring "-- Select Address --"
            const $firstAddress = $addressDropdown.find('option[value!=""]').first();
            if ($firstAddress.length) {
                $addressDropdown.val($firstAddress.val()).trigger('change');
            }
        });

        // ---------------- SHIP FROM ----------------
        // Same party/code bind
        $('#ddlShipFrom1').val(billFromCode).trigger('change');
        $('#ddlCreditAC').val(billFromCode).trigger('change');
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
        citySelector: '#ddlCityPD',
        isBillChange: true
    });

    //--------- Ship City and Bill City Change ----------
    bindCityStateChange('#ddlCitySF', '#ddlStateSF');
    bindCityStateChange('#ddlCityPD', '#ddlStatePD');

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
    $(document).on("change", ".item-name", async function () {
        await itemNameChanged(this);
    });

    //--------- Row Calculation ---------
    $(document).on("change",
        ".rate,.bill-qty,.recd-qty,.pack-per,.disc-per,.cess-per,.oth-amt,.pack-amt,.disc-amt,.cgst-amt,.sgst-amt,.igst-amt,.cess-amt,.vat-per,.vat-amt",
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

        $row.find(".rate").val(rate.toFixed(4));
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
        calculateLandAmount()
        toggleTaxAmountFields($row);
        // Move focus to Pack %
        $row.find(".pack-per").focus();
    });

}

function bindFreightEvents() {
    //----------- Freight TDS % Change ---------
    $('#NumTDSonFRT1').on('change', function () {
        if (isLoadForEdit) return;
        const freightPay = parseFloat($('#NumFreightPay').val()) || 0;
        const freightTdsPer = parseFloat($(this).val()) || 0;
        const freightTds = (freightPay * freightTdsPer / 100).toFixed(2);
        $('#NumTDSonFRT2').val(freightTds);
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

    //Copy to item grid
    $("#btnCopy").on("click", async function () {
        const selectedRows = $(".copyfrom-check:checked");
        if (selectedRows.length === 0) {
            showToast("Please select at least one row.", { type: "warning" });
            return;
        }
        showLoader();

        try {

            const count = selectedRows.length;
            for (let i = 0; i < count; i++) {
                const index = $(selectedRows[i]).data("index");
                await addNewRowBelow(copyFromRows[index]);

                // Keeps UI responsive
                if (i % 2 === 0) {
                    await new Promise(resolve => setTimeout(resolve, 0));
                }
            }
            $("#purchaseorderModal").modal("hide");

            showToast(`${count} ${count === 1 ? "Row" : "Rows"} copied successfully.`, { type: "success" });
        }
        finally {
            hideLoader();
        }

    });

}

function bindCityStateChange(citySelector, stateSelector) {
    $(citySelector).on('change', function () {
        if (isLoadForEdit) return;

        const cityCode = parseInt($(this).val()) || 0;
        //loadStateList(stateSelector, cityCode);
        loadDropdown("state", stateSelector, { cCode: cityCode });
    });
}

function bindAddressChange({ addressSelector, partySelector, add1Selector, add2Selector, add3Selector, gstSelector, pincodeSelector,
    citySelector, isBillChange = false }) {
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
                url: '/ImportExportExpensesEntry/GetAddressByBillToParty',
                type: 'GET',
                data: {
                    code,
                    addressId
                }
            });

            const address = response.addressDetails;
            console.log("address details on bill/ship change: ", address);
            $(add1Selector).val(address.add1);
            $(add2Selector).val(address.add2);
            $(add3Selector).val(address.add3);
            $(gstSelector).val(address.gstin);
            $(pincodeSelector).val(address.pincode);
            await loadDropdown("city", citySelector, {}, address.cityCode);
            if (isBillChange) {
                if (address.einv_party === 1) {
                    $('#lblE_invoice_suppl').show();
                }
                else {
                    $('#lblE_invoice_suppl').hide();
                }
            }
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
        const res = await fetch(`/ImportExportExpensesEntry/GetVNo?vType=${encodeURIComponent(vType)}`);
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
function loadMRNList(vType, selectedValue = null) {

    return $.ajax({
        url: "/ImportExportExpensesEntry/GetMrnNoList",
        type: "GET",
        data: { vType },
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
        doctype: "-- Select Doc Type --",
        party: "-- Select --",
        drcr: "-- Select --",
        drcrbyvtype: "-- Select --",
        item: "-- Select --",
        address: "-- Select Address --",
        department: "-- Select --",
        city: "-- Select City --",
        state: null,
        tax: "-- Select --",
        status: "-- Select Status --",
        transport: "-- Select Transport --",
        mrn: "-- Select MRN No --",
        transportgst: "-- Select GST --"
    };

    //const defaultOption = defaultOptions[typeKey] ?? "-- Select --";
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
        "tax",
        "employee"
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
        url: "/ImportExportExpensesEntry/GetList",
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
                    data-ucode="${item.ucode || ""}"
                    data-hsncode="${item.hsncode || ""}">
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
            "mrn",
            "employee",
            "item",
            "department",
            "drcrbyvtype"
        ].includes(type)
    ) {
        initSelect2(ddl);
    }

    // Auto-select first value
    if (
        [
            "status",
            "transportgst"
        ].includes(type)
        && list.length
    ) {
        ddl.val(list[0].Value)
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

        loadDropdown("party", "#ddlBillFrom"),
        loadDropdown("party", "#ddlShipFrom1"),

        loadDropdown("city", "#ddlDispCity"),
        loadDropdown("city", "#ddlCityPD"),
        loadDropdown("city", "#ddlCitySF"),

        loadDropdown("transport", "#ddlTransportName"),

        loadDropdown("drcr", "#ddlCreditAC"),
        //loadDropdown("drcr", "#ddlFreightDebitAC"),
        loadDropdown("drcr", "#ddlFreightCreditAC"),
        loadDropdown("drcr", "#ddlWBDebitAC"),
        loadDropdown("drcr", "#ddlWBCreditAC"),
        loadDropdown("drcr", "#ddlUnloadDebitAC"),
        loadDropdown("drcr", "#ddlUnloadCreditAC"),
        loadDropdown("drcr", "#ddlTdsAccount"),

        loadDropdown("employee", "#ddlEmployee")
    ]);
}
//=========== DROPDOWN END ============

//LOAD DATA by V_No
async function loadFullQuotationByVno(vNo, vType, isViewMode) {

    try {

        const response = await $.ajax({
            url: "/ImportExportExpensesEntry/GetFullQuotationByVno",
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
        await loadMRNList(vTypeToBind, header.reF_NO);
        $("#TxtMRNNo1").val(header.reF_TYPE || "");
        //==========================
        // Bill From
        //==========================

        await loadDropdown("party", "#ddlBillFrom", {}, header.partY_CODE);
        $("#TxtAdd1PD").val(header.bilL_ADD1 || "");
        $("#TxtAdd2PD").val(header.bilL_ADD2 || "");
        $("#TxtAdd3PD").val(header.bilL_ADD3 || "");
        await loadDropdown("city", "#ddlCityPD", {}, header.bilL_CITY);
        await loadDropdown("state", "#ddlStatePD", { cCode: header.bilL_CITY }, header.bilL_STATE);
        $("#NumPincodeBL").val(header.bilL_PINCODE || "");
        $("#TxtGSTNo").val(header.bilL_GST || "");
        $("#TxtDispFromAdd").val(header.disP_ADDRESS || "");
        await loadDropdown("city", "#ddlDispCity", {}, header.disP_CITY);

        //==========================
        // Ship From
        //==========================

        await loadDropdown("party", "#ddlShipFrom1", {}, header.shiP_CODE);

        $("#TxtAdd1SF").val(header.shiP_ADD1 || "");
        $("#TxtAdd2SF").val(header.shiP_ADD2 || "");
        $("#TxtAdd3SF").val(header.shiP_ADD3 || "");
        await loadDropdown("city", "#ddlCitySF", {}, header.shiP_CITY);
        await loadDropdown("state", "#ddlStateSF", { cCode: header.shiP_CITY }, header.shiP_STATE);
        $("#TxtPincodeSF").val(header.shiP_PINCODE || "");
        $("#TxtGSTNoSF").val(header.shiP_GST || "");

        //==========================
        // Bill Details
        //==========================

        $("#TxtBillNo").val(header.bilL_NO || "");
        setDateControl(header.bilL_DATE, "#DtBillDate", "#chkBillDate");

        $("#TxtChallanNo").val(header.chalL_NO || "");
        setDateControl(header.chalL_DATE, "#DtChDate", "#chkChDate");

        $("#TxtBLNo").val(header.bL_NO || "");
        setDateControl(header.bL_DT, "#DtBLDate", "#chkBLDate");

        $("#TxtWaybillNo").val(header.waybilL_NO || "");
        $("#TxtWayBillInvNo").val(header.ewB_INVNO || "");
        $("#DtWaybillDate").val(formatDateYMD(header.ewB_DATE));
        $("#DtWaybillExpiry").val(formatDateYMD(header.ewB_EXPDATE));

        if (header.einV_PARTY === 1) {
            $('#lblE_invoice_suppl').show();
        }
        else {
            $('#lblE_invoice_suppl').hide();
        }

        //==========================
        // Accounts
        //==========================
        await loadDropdown("drcr", "#ddlDebitAC", {}, header.debiT_AC);
        await loadDropdown("drcr", "#ddlCreditAC", {}, header.crediT_AC);

        await loadDropdown("employee", "#ddlEmployee", {}, header.emP_CODE);
        $("#ddlExpsType").val(header.expS_TYPE || "");

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

        if (isViewMode) {
            $('#Dtsysdate').val(formatDateYMD(header.udate));
        }
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
        await loadDropdown("drcr", "#ddlFreightDebitAC", {}, header.frtpaY_DRAC);
        await loadDropdown("drcr", "#ddlFreightCreditAC", {}, header.frtpaY_CRAC);

        $("#NumTDSonFRT1").val(header.frT_TDSPER || 0);
        $("#NumTDSonFRT2").val(header.frT_TDS || 0);
        $("#TxtBillNoLD").val(header.trP_BILLNO || "");
        setDateControl(header.frT_BILLDT, "#DtBillDateLD", "#chkBillDateLD");
        //await loadTranGSTByFrtCrAc(header.frtpaY_CRAC, header.trP_GSTNO)


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

        //TDS Adjustment
        if (response.existingTDS > 0 && response.existingTDS) {
            $('#btnTDSAdjsustment span').html(`TDS Adjustment<br/> (Amt. Adjusted = ${response.existingTDS})`);
        }
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

            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos || data.NOS || ''}"/></td>
            <td><input class="form-control form-control-sm recd-qty" type="number" value="${data.recD_QTY || data.RECD_QTY || ''}"/></td>
            <td><input class="form-control form-control-sm bill-qty" type="number" value="${data.bilL_QTY || data.BILL_QTY || ''}"/></td>

            <td><input class="form-control form-control-sm rate" type="number" value="${data.rate || data.RATE || ''}"/></td>
            <td><input class="form-control form-control-sm amount" type="number" value="${data.amount || data.AMOUNT || ''}"/></td>

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

            <td><input class="form-control form-control-sm pack-per" type="number" value="${data.pacK_PER || data.PACK_PER || ''}"/></td>
            <td><input class="form-control form-control-sm pack-amt" type="number" value="${data.pacK_AMT || data.PACK_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm disc-per" type="number" value="${data.disC_PER || data.DISC_PER || ''}"/></td>
            <td><input class="form-control form-control-sm disc-amt" type="number" value="${data.disC_AMT || data.DISC_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm cgst-per" type="number" value="${data.cgsT_PER || data.CGST_PER || ''}" disabled/></td>
            <td><input class="form-control form-control-sm cgst-amt" type="number" value="${data.cgsT_AMT || data.CGST_AMT || ''}" ${(data.cgsT_PER || data.CGST_PER) ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm sgst-per" type="number" value="${data.sgsT_PER || data.SGST_PER || ''}" disabled/></td>
            <td><input class="form-control form-control-sm sgst-amt" type="number" value="${data.sgsT_AMT || data.SGST_AMT || ''}" ${(data.sgsT_PER || data.SGST_PER) ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm igst-per" type="number" value="${data.igsT_PER || data.IGST_PER || ''}" disabled/></td>
            <td><input class="form-control form-control-sm igst-amt" type="number" value="${data.igsT_AMT || data.IGST_AMT || ''}" ${(data.igsT_PER || data.IGST_PER) ? '' : 'disabled'} /></td>

            <td><input class="form-control form-control-sm cess-per" type="number" value="${data.cesS_PER || data.CESS_PER || ''}"/></td>
            <td><input class="form-control form-control-sm cess-amt" type="number" value="${data.cesS_AMT || data.CESS_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm vat-per" type="number" value="${data.vaT_PER || data.VAT_PER || ''}"/></td>
            <td><input class="form-control form-control-sm vat-amt" type="number" value="${data.vaT_AMT || data.VAT_AMT || ''}"/></td>

            <td><input class="form-control form-control-sm oth-amt" type="number" value="${data.otH_AMT || data.OTH_AMT || ''}"/></td>
            <td><input class="form-control form-control-sm net-amt" type="number" value="${data.neT_AMT || data.NET_AMT || ''}" disabled/></td>

            <td>
                <select class="form-control form-control-sm make-code">
                </select>
            </td>
            <td>
                <select class="form-control form-control-sm dept-code"></select>
            </td>

            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || data.REMARKS || ''}"/></td>

            <td><input class="form-control form-control-sm land-rate" type="number" value="${data.lanD_RATE || data.LAND_RATE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm land-amt" type="number" value="${data.lanD_AMT || data.LAND_AMT || ''}" disabled/></td>

            <td><input class="form-control form-control-sm po-type" type="text" value="${data.pO_TYPE || data.PO_TYPE || ''}" disabled/></td>
            <td><input class="form-control form-control-sm po-no" type="number" value="${data.pO_NO || data.PO_NO || ''}" disabled/></td>

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
    //initRowSelect2($lastRow);

    const itemName = $lastRow[0].querySelector(".item-name");
    itemName.value = data.iteM_CODE || data.ITEM_CODE || "";
    await itemNameChanged(itemName, data.makE_CODE || data.MAKE_CODE || "");

    $lastRow.find(".tax-code").val(data.taX_CODE || data.TAX_CODE || "");
    $lastRow.find(".dept-code").val(data.depT_CODE || data.DEPT_CODE || "");
    initRowSelect2($lastRow);
}


//-------- SELECT2 HELPER -------------
function initSelect2($ddl) {
    //$ddl.select2({
    //    placeholder: '-- Select --',
    //    allowClear: true,
    //    width: '100'
    //});
    const width = $ddl.outerWidth();
    $ddl.select2({
        placeholder: '-- Select --',
        allowClear: true,
        width: 'style'
    });

    const $container = $ddl.siblings('.select2-container');

    if ($container.length) {
        $container[0].style.setProperty('width', `${width}px`, 'important');
    }
    $ddl.on('select2:open', function () {
        setTimeout(function () {
            let searchBox = document.querySelector('.select2-container--open .select2-search__field');

            if (searchBox) {
                searchBox.focus();
            }
        }, 0);
    });
}

function initRowSelect2($row) {
    $row.find(".item-name, .tax-code, .dept-code").each(function () {
        initSelect2($(this));
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

function loadRMDPMRNData(mrnType, mrnNo) {
    $.ajax({
        url: '/ImportExportExpensesEntry/loadRMDPMRNData',
        type: 'GET',
        data: {
            mrnType: mrnType,
            mrnNo: mrnNo
        },
        success: function (response) {
            if (response.success) {

                var data = response.mrnData;
                console.log("MRN Change data: ", data);
                if (data) {
                    $('#TxtChallanNo').val(data.challNo);
                    setDateControl(data.challDate, "#DtChDate", "#chkChDate");
                    $('#TxtBLNo').val(data.blNo);
                    setDateControl(data.blDate, "#DtBLDate", "#chkBLDate");
                }
            }
            else {
                alert(response.message || 'No data found.');
            }
        },
        error: function (xhr, status, error) {
            console.error("Error:", error);
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
    //let usdRate = parseFloat($row.find('.usd-rate').val()) || 0;

    let billQty = parseFloat($row.find('.bill-qty').val()) || 0;
    let rate = parseFloat($row.find('.rate').val()) || 0;
    //let exRate = parseFloat($row.find('.exch-rate').val()) || 0;

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
        //if (exRate > 0) {
        //    rate = usdRate * exRate;
        //    $row.find('.rate').val(rate.toFixed(4));
        //}
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
        url: '/ImportExportExpensesEntry/GetPackOnBasic',
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

    $row.find(".cgst-amt").val(cgst.toFixed(2));
    $row.find(".sgst-amt").val(sgst.toFixed(2));
    //}

    //if (currentField !== "igst-amt") {
    igst = grossAmt * igstPer / 100;
    $row.find(".igst-amt").val(igst.toFixed(2));
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
function calculateLandAmount() {

    let gAmt = 0;
    let lAmt = 0;

    // ---------- TOTAL GROSS / LAND AMOUNT ----------
    $("#tblItemRecordPBPE tbody tr").each(function () {

        const $row = $(this);

        // Skip empty rows
        const itemCode = parseFloat($row.find(".item-code").val()) || 0;
        if (itemCode <= 0) return;

        const netAmt = parseFloat($row.find(".net-amt").val()) || 0;
        const cgst = parseFloat($row.find(".cgst-amt").val()) || 0;
        const sgst = parseFloat($row.find(".sgst-amt").val()) || 0;
        const igst = parseFloat($row.find(".igst-amt").val()) || 0;
        const cess = parseFloat($row.find(".cess-amt").val()) || 0;

        gAmt += netAmt - cgst - sgst - igst - cess;
    });

    // ---------- FREIGHT ----------
    const frtPay = parseFloat($("#NumFreightPay").val()) || 0;
    const frtTax = parseFloat($("#NumFrtTax2").val()) || 0;


    lAmt = gAmt + frtPay - frtTax;

    // ---------- TOTAL BASIC AMOUNT ----------
    const totalAmt = parseFloat($("#NumAmount").val()) || 0;

    // ---------- ALLOCATE LAND AMOUNT TO EACH ROW ----------
    $("#tblItemRecordPBPE tbody tr").each(function () {

        const $row = $(this);

        const itemCode = parseFloat($row.find(".item-code").val()) || 0;

        if (itemCode <= 0) {
            $row.find(".land-amt").val("0.00");
            $row.find(".land-rate").val("0.00");
            return;
        }

        const billQty = parseFloat($row.find(".bill-qty").val()) || 0;
        const amount = parseFloat($row.find(".amount").val()) || 0;

        let landAmt = 0;
        let landRate = 0;

        if (totalAmt !== 0) {
            landAmt = (lAmt / totalAmt) * amount;
        }

        if (billQty !== 0) {
            landRate = landAmt / billQty;
        }

        $row.find(".land-amt").val(landAmt.toFixed(2));
        $row.find(".land-rate").val(landRate.toFixed(2));
    });
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
    calculateLandAmount();
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
        url: '/ImportExportExpensesEntry/CheckExistingTDS',
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
        url: '/ImportExportExpensesEntry/GetFrtCrAcByTransCode',
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

        DEBIT_AC: parseInt($('#ddlDebitAC').val()) || 0,
        CREDIT_AC: parseInt($('#ddlCreditAC').val()) || 0,

        EMP_CODE: parseInt($('#ddlEmployee').val()) || 0,
        EXPS_TYPE: $('#ddlExpsType').val() || "",

        INPUT_TYPE: $('#ddlInputType').val().trim() || '',
        STATUS: parseInt($('#ddlStatus').val()) || 0,
        //EXCH_RATE: parseFloat($('#NumExRate').val()) || 0,
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
        TRANSPORT_NAME: $('#ddlTransportName').val() === "" ? ""
            : $('#ddlTransportName').find("option:selected").text().trim(),
        //TRANSPORT_NAME: $('#ddlTransportName option:selected').text() || "",

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

            PO_TYPE: headerData.V_TYPE,
            PO_NO: headerData.V_NO,

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
        url: '/ImportExportExpensesEntry/SavePurchaseBillPassEntry',
        method: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(data),
        success: function (response) {
            if (response.success) {
                showToast('Saved successfully!', { type: "success" });
                setFormReadonly();
                isReadOnly = true;
                setTimeout(() => window.location.href = '/ImportExportExpensesEntry/Index?id=' + encodeURIComponent($('#NumDocNo').val()) + '&vtype=' + encodeURIComponent($('#ddlDocType').val()) + '&readOnly=true', 1000);
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
    const inputTypeEl = $('#ddlInputType');
    const billGSTEl = $('#TxtGSTNo');
    const billFromEl = $('#ddlBillFrom');
    const mrnTypeEl = $('#TxtMRNNo1');
    const mrnNoEl = $('#TxtMRNNo2');
    const tdsPerEl = $("#TxtTds1");
    const tdsAmtEl = $("#TxtTds2");
    const tds194QPerEl = $("#TxtTds194q1");
    const tds194QAmtEl = $("#TxtTds194q2");
    const totalNetAmtEl = $("#NumNetAmount");

    if (!validateRequiredField(VTypeEl, 'Document Type') || !validateRequiredField(VNoEl, 'Document Number') || !validateRequiredField($('#TxtBillNo'), 'Bill No')
        || !validateRequiredField($('#DtBillDate'), 'Bill Date') || !validateRequiredField($('#TxtChallanNo'), 'Challan No')
        || !validateRequiredField($('#DtChDate'), 'Challan Date') || !validateRequiredField($('#ddlBillFrom'), 'Party')
        || !validateRequiredField($('#ddlDebitAC'), 'Debit Ac') || !validateRequiredField($('#ddlCreditAC'), 'Credit Ac')
        || !validateRequiredField($('#ddlShipFrom1'), 'Ship To')    ) {
        isValid = false;
        return false;
    }
    //------------- Validate VDate ------------
    const isValidVDate = await checkValidDate();
    if (!isValidVDate) {
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

    //----------------- Validate Debit and Credit Account ---------------
    if ($("#ddlDebitAC").val() !== "" && $("#ddlCreditAC").val() !== "" && $("#ddlDebitAC").val() === $("#ddlCreditAC").val())
    {
        setInvalid($("#ddlCreditAC"), "Debit A/c and Credit A/c must be different.");
        isValid = false;
        return false;
    }

    //-----------Validate Bill To GST------------
    if ($('#TxtGSTNo').val() !== "" && $('#TxtGSTNo').val())
    {
        const result = await validatePartyGst("BillTo", $("#ddlBillFrom").val(), $("#TxtGSTNo").val());
        if (result && !result.isValid) {
            showToast(result.message, { type: "warning" });
            $("#TxtGSTNo").focus();
        }
    }
    
    //----------------- Validate TDS Account ---------------
    if (Number($("#TxtTds2").val()) > 0 && !$("#ddlTdsAccount").val()) {
        setInvalid($("#ddlTdsAccount"), "TDS A/c must be selected if TDS amount is greater than 0.");
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
        showToast("As the party is covered under Section 206AB, TDS shall be deducted at a rate exceeding 5%, in accordance with the applicable provisions.",
            { type: "warning" });
        isValid = false;
        return false;
    }

    //----------------- Validate Duplicate Bill No ---------------
    if ($('#TxtBillNo').val().trim()) {
        const duplicateBill = await checkDuplicateBill(Number($('#ddlBillFrom').val()), $('#TxtBillNo').val().trim(), Number(VNoEl.val()));
        if (duplicateBill && duplicateBill.exists) {
            showToast(`Bill No ${$('#TxtBillNo').val().trim()} already exists in Serial No: ${duplicateBill.docId} dated: ${duplicateBill.vDate}`,
                { type: "warning" });
            isvalid = false;
            return false;
        }
    }


    //----------------- Validate Exps ---------------
    const ExpsResult = await validateFreightExpense(mrnTypeEl.val(), mrnNoEl.val(), $('#ddlExpsType').val());
    if (!ExpsResult.success) {
        showToast(ExpsResult.message, { type: 'warning' });
        return false;
    }

    //----------------- Validate Container Tracking ---------------
    const ContResult = await validateImportTracking(mrnTypeEl.val(), mrnNoEl.val(), billFromEl.val(), $('#TxtBillNo').val(), billFromEl.find('option:selected').text().trim(),);
    if (!ContResult.success) {
        showToast(ContResult.message, { type: "warning" });
        return false;
    }

    //-----------------COST ALLOCATION VALIDATION-------------------
    const CostResult = await validateCostAllocation();
    if (!CostResult.status || !CostResult.data) {
        showToast(CostResult.message, { type: "warning" });
        return false;
    }
   
    //----------------- Validate Party State Tax ---------------
    if (Number($('#ddlBillFrom').val()) > 0) {
        if ($('#ddlBillFrom').val() !== "STDP") {
            const taxValidation = await validateTaxType(Number($('#ddlBillFrom').val()), Number($('#NumIgst').val()), Number($('#NumCgst').val()),
                Number($('#NumSgst').val())
            );

            if (taxValidation && !taxValidation.isValid) {
                showToast(taxValidation.message, { type: "warning" });
                isValid = false;
                return false;
            }
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
        const response = await fetch('/ImportExportExpensesEntry/CheckValidDate', {
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
    let validItemCount = 0;

    const rows = document.querySelectorAll("#tblItemRecordPBPE tbody tr");

    for (const row of rows) {

        const c = getRowControls(row);

        if (Number(c.item.value) > 0)
            validItemCount++;

        //Validate Item
        if (!validateItem(c)) return false;

        if (Number(c.item.value) > 0) {
          
            //Validate Received Quantity
            if (Number(c.item.value) > 0 && Number(c.recdQty.value) === 0) {
                setInvalid($(c.recdQty), "Received Qty is 0.");
                return false;
            }

            //Validate Amount
            if (Number(c.amount.value) === 0) {
                setInvalid($(c.amount), "Amount must not be 0.");
                return false;
            }


            //Validate Tax
            if (Number(c.taxCode.value) === 0) {
                setInvalid($(c.taxCode), "Tax Type not selected.");
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

async function getPartyPurchaseAmount(partyCode, vType, vNo, currentAmount) {
    try {
        const response = await $.ajax({
            url: "/ImportExportExpensesEntry/GetPartyPurchaseAmount",
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
            url: "/ImportExportExpensesEntry/GetTDS206Apply",
            type: "GET",
            dataType: "json",
            data: {
                partyCode: partyCode
            }
        });

        if (response.success) {
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

async function getPurchaseVoucherNo(transportName, grNo, currentVoucher, purchaseOrSale) {
    try {
        const response = await $.ajax({
            url: "/ImportExportExpensesEntry/GetPurchaseOrSaleVoucherNo",
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
            url: "/ImportExportExpensesEntry/CheckPaymentExists",
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
            url: "/ImportExportExpensesEntry/CheckDuplicateBill",
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
            url: "/ImportExportExpensesEntry/ValidateTaxType",
            type: "GET",
            dataType: "json",
            data: {
                billToCode: cityCode,
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
            url: "/ImportExportExpensesEntry/ValidatePartyGst",
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
function validateFreightExpense(refType, refVNo, expsType) {
    return $.ajax({
        url: '/ImportExportExpensesEntry/ValidateFreightExpense',
        type: 'GET',
        data: {refType: refType, refVNo: refVNo, expsType: expsType}
    });
}
function validateImportTracking(refVType, refVNo, billFromCode, billNo, billToName) {
    return $.ajax({
        url: '/ImportExportExpensesEntry/ValidateImportTracking',
        type: 'GET',
        data: {
            refVType: refVType,
            refVNo: refVNo,
            billFromCode: billFromCode,
            billNo: billNo,
            billToName: billToName
        }
    });
}
function validateCostAllocation() {
    var model = {
        DrActCode: $('#ddlDebitAC').val(),
        VType: $('#ddlDocType').val(),
        VNo: $('#NumDocNo').val(),
        InputType: $('#ddlInputType').val(),

        TotAmt: $('#NumAmount').val(),
        TotPacking: $('#NumPacking').val(),
        TotDisc: $('#NumDiscount').val(),
        TotNetAmount: $('#TxtNetAmount').val(),

        QDiffDrAmt: $('#TxtQualityDiffDebitAmt').val(),
        QDiffDrTax: $('#TxtQualityDiffDebitTax').val(),
        RateDiffDrAmt: $('#TxtRateDiffDebitAmt').val(),
        RateDiffDrTax: $('#TxtRateDiffDebitTax').val(),

        QcDrNoteAmt: $('#TxtQCDebitNoteAmt').val(),
        QcDrNoteTax: $('#TxtQCDebitNoteTax').val(),
        WgtDrNoteAmt: $('#TxtWeightDebitAmt').val(),
        WgtDrNoteTax: $('#TxtWeightDebitTax').val(),
        OthDrNoteAmt: $('#TxtOtherDebitAmt').val(),
        OthDrNoteTax: $('#TxtOtherDebitTax').val(),

        QCrNoteAmt: $('#TxtQualityCreditNoteAmt').val(),
        QCrNoteTax: $('#TxtQualityCreditNoteVal').val(),
        RDiffCrNoteAmt: $('#TxtRateDiffCreditNoteAmt').val(),
        RDiffCrNoteTax: $('#TxtRateDiffCreditNoteVal').val(),

        QcCrNoteAmt: $('#TxtQCCreditNoteAmt').val(),
        QcCrNoteTax: $('#TxtQCCreditNoteVal').val(),
        WgtCrNoteAmt: $('#TxtWeightCreditNoteAmt').val(),
        WgtCrNoteTax: $('#TxtWeightCreditNoteVal').val()
    };

    return $.ajax({
        url: '/ImportExportExpensesEntry/ValidateCostAllocation',
        type: 'POST',
        data: model
    });
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

        poType: row.querySelector(".po-type"),
        poNo: row.querySelector(".po-no"),

    };
}

async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/ImportExportExpensesEntry/GetGlobalValues",
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
        url: '/ImportExportExpensesEntry/CalculateTDS',
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
                showToast(response.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            showToast(xhr.responseJSON?.message || "Error while calculating TDS.", { type: "error" });
        }
    });
}

//-------------Copy From--------------
function loadCopyFromMenu() {

    $.ajax({
        url: '/ImportExportExpensesEntry/GetCopyFromMenu',
        type: 'GET',
        success: function (response) {

            if (!response.success) {
                showToast(response.message, { type: "warning" });
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
            showToast("An error occurred while loading Copy From options.", { type: "warning" });
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

    //const billNo = $("#TxtBillNo").val();
    //if ((billNo || "").trim() === "") {
    //    setInvalid($("#TxtBillNo"), "Please select Bill No.");
    //    return;
    //}

    const request = {
        //vType: $("#ddlDocType").val(),
        billTo: billTo,
        //billNo: billNo,
        vNo: $("#NumDocNo").val() || 0,
        currentVType: code
    };

    $.ajax({
        url: '/ImportExportExpensesEntry/GetCopyFromData',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),

        success: function (response) {

            if (!response.success) {
                showToast(response.message, { type: "warning" });
                return;
            }

            console.log(response.data);
            console.log("Columns:", response.data.columns);
            console.log("Rows:", response.data.rows);
            console.log("Rows Length:", response.data.rows.length);
            bindCopyFromGrid(
                response.data.columns,
                response.data.rows
            );

            $('#purchaseorderModal').modal("show");
        },

        error: function (xhr) {
            showToast("Unable to load Copy From data.", { type: "error" });
            console.log(xhr);
        }
    });
}
function bindCopyFromGrid(columns, rows) {
    copyFromRows = rows;

    const table = $("#tblpurchaseordermodal");
    const thead = table.find("thead");
    const tbody = table.find("tbody");

    table.find("colgroup").remove();
    thead.empty();
    tbody.empty();

    if (!rows || rows.length === 0) {
        tbody.html(`<tr><td colspan="100%" class="text-center">No Record Found</td></tr>`);
        return;
    }

    //-------------------------
    // ColGroup
    //-------------------------

    let colgroup = "<colgroup>";
    colgroup += `<col style="width:50px;">`;
    columns.forEach(col => {
        let maxLength = col.title.length;
        rows.forEach(r => {
            let value = r[col.field];
            value = value == null ? "" : value.toString();
            if (value.length > maxLength)
                maxLength = value.length;
        });

        let width;
        if (typeof rows[0][col.field] === "number") {
            width = 90;
        } else {
            width = Math.max(maxLength * 9, 80);
            width = Math.min(width, 300);
        }

        colgroup += `<col style="width:${width}px;">`;
    });

    colgroup += "</colgroup>";

    table.prepend(colgroup);

    //-------------------------
    // Header
    //-------------------------

    let header = `<tr><th style="text-align:center"><input type="checkbox" id="selectAllPR"></th>`;
    columns.forEach(col => {
        header += `<th>${col.title}</th>`;
    });

    header += "</tr>";
    thead.html(header);

    //-------------------------
    // Body
    //-------------------------

    let html = "";
    rows.forEach((row, index) => {
        html += `<tr>`;
        html += `<td style="text-align:center"><input type="checkbox" class="copyfrom-check" data-index="${index}"></td>`;
        columns.forEach(col => {
            const value = row[col.field] ?? "";
            html += `<td title="${value}">${value}</td>`;
        });
        html += "</tr>";
    });

    tbody.html(html);
    makeColumnsResizable("#tblpurchaseordermodal");
}

//--------------Pending Approval List---------
function bindPendingApprovalGrid(data) {
    const tbody = $("#tblpendingapprovalmodal tbody");
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`<tr><td colspan="15" class="text-center">No Record Found</td></tr>`);
        return;
    }

    let rows = "";

    data.forEach(item => {

        rows += `
        <tr>
            <td class="hidden-col"></td>

            <td>${item.type ?? ""}</td>
            <td>${item.docID ?? ""}</td>
            <td>${item.docDate ?? ""}</td>
            <td>${item.sendBy ?? ""}</td>
            <td>${item.sendDate ?? ""}</td>
            <td>${item.sendTo ?? ""}</td>
            <td>${item.status ?? ""}</td>
            <td>${item.approvalStatus ?? ""}</td>
            <td>${item.remarks ?? ""}</td>
            <td>${item.createdBy ?? ""}</td>
            <td>${item.createdDate ?? ""}</td>

            <td class="hidden-col"></td>
        </tr>`;
    });

    tbody.html(rows);
}

function onGetPendingAppListClick() {
    $.ajax({
        url: "/ImportExportExpensesEntry/GetPendingApprovalList",
        type: "GET",
        success: function (response) {

            if (!response.success) {
                showToast(response.message, { type: "warning" });
                return;
            }

            bindPendingApprovalGrid(response.data);

            $("#pendingapprovalModal").modal("show");
        },
        error: function () {
            showToast("Unable to load pending approval list.", { type: "error" });
        }
    });
}

//----------------Cost Allocation-----------
function onCostAllocationClick() {

    const drc =
        (parseFloat($("#TxtQualityDiffDebitAmt").val()) || 0) +
        (parseFloat($("#TxtRateDiffDebitAmt").val()) || 0) +
        (parseFloat($("#TxtQCDebitNoteAmt").val()) || 0) +
        (parseFloat($("#TxtWeightDebitAmt").val()) || 0) +
        (parseFloat($("#TxtOtherDebitAmt").val()) || 0);

    const crc =
        (parseFloat($("#TxtQualityCreditNoteAmt").val()) || 0) +
        (parseFloat($("#TxtRateDiffCreditNoteAmt").val()) || 0) +
        (parseFloat($("#TxtQCCreditNoteAmt").val()) || 0) +
        (parseFloat($("#TxtWeightCreditNoteAmt").val()) || 0);

    const drcWT =
        (parseFloat($("#TxtQualityDiffDebitAmt").val()) || 0) +
        (parseFloat($("#TxtQualityDiffDebitTax").val()) || 0) +
        (parseFloat($("#TxtRateDiffDebitAmt").val()) || 0) +
        (parseFloat($("#TxtRateDiffDebitTax").val()) || 0) +
        (parseFloat($("#TxtQCDebitNoteAmt").val()) || 0) +
        (parseFloat($("#TxtQCDebitNoteTax").val()) || 0) +
        (parseFloat($("#TxtWeightDebitAmt").val()) || 0) +
        (parseFloat($("#TxtWeightDebitTax").val()) || 0) +
        (parseFloat($("#TxtOtherDebitAmt").val()) || 0) +
        (parseFloat($("#TxtOtherDebitTax").val()) || 0);

    const crcWT =
        (parseFloat($("#TxtQualityCreditNoteAmt").val()) || 0) +
        (parseFloat($("#TxtQualityCreditNoteVal").val()) || 0) +
        (parseFloat($("#TxtRateDiffCreditNoteAmt").val()) || 0) +
        (parseFloat($("#TxtRateDiffCreditNoteVal").val()) || 0) +
        (parseFloat($("#TxtQCCreditNoteAmt").val()) || 0) +
        (parseFloat($("#TxtQCCreditNoteVal").val()) || 0) +
        (parseFloat($("#TxtWeightCreditNoteAmt").val()) || 0) +
        (parseFloat($("#TxtWeightCreditNoteVal").val()) || 0);

    let vamt = 0;

    if ($("#ddlInputType").val() === "GST Input") {
        vamt =
            parseFloat($("#NumAmount").val() || 0) +
            parseFloat($("#NumPacking").val() || 0) -
            parseFloat($("#NumDiscount").val() || 0) +
            crc - drc;
    } else {
        vamt =
            parseFloat($("#NumNetAmount").val() || 0) + crcWT - drcWT;
    }

    const ddlBillFrom = document.getElementById("ddlBillFrom");
    const partyCode = ddlBillFrom.value;
    const partyName = ddlBillFrom.options[ddlBillFrom.selectedIndex].text;

    let data = {
        partyName: partyName,
        partyCode: partyCode,
        refNo: $('#NumDocNo').val(),
        refType: $('#ddlDocType').val(),
        amount: vamt,
        date: $('#DtDocDate').val()
    }

    CostAllocation.open(data);
}

//--------------------Reports------------------

//Dr Note
async function DebitNoteReport() {

    var reportName = "rawvoucher";
    const vType = ($("#ddlDocType").val() || "").trim();
    const vNo = parseInt($("#NumDocNo").val() || 0);

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
    console.log("formula: ", formula);

    var creditNoteAmt = 0;

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
    $('#copyFromDropdown, .erppagedropdown-toggle').prop('disabled', true).removeAttr('data-bs-toggle')
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

async function itemNameChanged(element, makeCode) {
    console.log("makeCode: ", makeCode);
    const $row = $(element).closest("tr");
    const itemCode = $(element).val();
    if (isLoadForEdit) {
        // Load Make based on Item
        await loadDropdown(
            "make",
            $row.find(".make-code"),
            {
                itemCode: itemCode
            },
            makeCode
        );

        return;
    }

    
    const selectedOption = $(element).find("option:selected");

    const uomCode = selectedOption.data("ucode") || 0;
    const uomName = selectedOption.data("unit") || '';
    const hsncode = selectedOption.data("hsncode") || '';
   

    // UOM
    $row.find(".uom-code").val(uomCode || "");
    $row.find(".uom-name").val(uomName || "");

    // HSN
    $row.find(".hsn-code").val(hsncode);

    // RCM Y/N
    const inputType = $('#ddlInputType').val();

    if (inputType === 'RCM') {
        $row.find('.rcm-yn').val('YES');
    } else {
        $row.find('.rcm-yn').val('NO');
    }

    // Input Y/N
    $row.find('.input-yn').val('YES');

    // Load Make based on Item
    await loadDropdown(
        "make",
        $row.find(".make-code"),
        {
            itemCode: itemCode
        },
        makeCode
    );

    // Duplicate
    if (checkDuplicateItems(element)) {
        return;
    }
}

//--------------Import Invoice List---------
function bindImportInvoiceGrid(data) {
    console.log("Import Invoice List Data: ", data);

    const tbody = $("#tblimportInvoiceListmodal tbody");
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`<tr><td colspan="6" class="text-center">No Record Found</td></tr>`);
        return;
    }

    let rows = "";

    data.forEach(item => {

        rows += `
        <tr>
            <td class="hidden-col"></td>

            <td>${item.saudaNo ?? ""}</td>
            <td>${item.expenseType ?? ""}</td>
            <td>${item.invNo ?? ""}</td>
            <td>${item.invDate ?? ""}</td>
            <td>${item.invAmt ?? ""}</td>
            <td>${item.partyName ?? ""}</td>
            
            <td class="hidden-col"></td>
        </tr>`;
    });

    tbody.html(rows);
}

function onImportInvoiceListClick() {
    const partyCode = parseInt($('#ddlBillFrom').val() || 0);
    $.ajax({
        url: "/ImportExportExpensesEntry/GetImportInvoiceList",
        type: "GET",
        data: { partyCode: partyCode },
        success: function (response) {

            if (!response.success) {
                showToast(response.message, { type: "warning" });
                return;
            }

            bindImportInvoiceGrid(response.data);

            $("#importInvoiceListModal").modal("show");
        },
        error: function () {
            showToast("Unable to load import invoice list.", { type: "error" });
        }
    });
}

//--------------TDS Adjustment--------------
function onTDSAdjustmentClick() {
    const ddlBillFrom = document.getElementById('ddlBillFrom');

    const partyCode = ddlBillFrom.value;
    const partyName = ddlBillFrom.options[ddlBillFrom.selectedIndex]?.text || '';

    const refType = $('#ddlDocType').val() || '';
    const refNo = $('#NumDocNo').val() || '';
    const refDate = $('#DtDocDate').val() || '';

    $('#TxtPartyCodeTDSAdj').val(partyCode);
    $('#TxtPartyNameTDSAdj').val(partyName);
    $('#txtRefTDSAdjVTypeCA').val(refType);
    $('#NumRefTDSAdjVno').val(refNo);
    $('#DtTDSAdjVDate').val(refDate);
}

let totalExistingAdjustment = 0;
function loadTDSAdjData() {
    const request = {
        PartyCode: parseInt($('#TxtPartyCodeTDSAdj').val() || 0),
        VType: $('#txtRefTDSAdjVTypeCA').val() || '',
        VNo: parseInt($('#NumRefTDSAdjVno').val() || 0),
        VDate: $('#DtTDSAdjVDate').val() || '',
    }

    $.ajax({
        url: '/ImportExportExpensesEntry/LoadTDSAdjustmentData',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function (res) {
            if (!res.success) {
                showToast("No Records Found", { type: "warning" });
                return;
            }

            bindTDSAdjData(res)
        },
        error: function (xhr) {

        }
    })
}

function bindTDSAdjData(res) {
    const tbody = $("#tblTDSAdjustmentmodal tbody");
    tbody.empty();
    if (!res || res.data.length === 0) {
        tbody.append(`<tr><td colspan="8" class="text-center">No Record Found</td></tr>`);
        return;
    }

    let rows = "";

    res.data.forEach(item => {

        rows += `
        <tr>
            <td class="hidden-col"></td>

            <td class="tdsAdjVType">${item.vType ?? ""}</td>
            <td class="tdsAdjVNo">${item.vNo ?? ""}</td>
            <td class="tdsAdjVDate">${formatDate(item.vDate)}</td>
            <td class="tdsAdjAmount">${item.amount ?? ""}</td>
            <td class="tdsAdjNarr">${item.narration ?? ""}</td>
            <td class="tdsAdjadjustedAmount">${item.adjustedAmount ?? ""}</td>
            <td>
                <input
                    class="form-control form-control-sm numBalAdjAmt" type="number" step="0.01" value="${item.balanceAdjustment ?? ''}"
                    onchange="onBalAdjAmtChange();"
                />
            </td>
            <td>${item.crName ?? ""}</td>
            
            <td class="hidden-col"></td>
        </tr>`;
    });
    totalExistingAdjustment = res.totalExistingAdjustment || 0
    $('#NumTotAdjustedAmt').val(res.totalExistingAdjustment || 0);
    $('#lbltotRec').show();
    $('#lbltotRec').text(`Total Records: ${res.totalRecords}`);

    tbody.html(rows);
}

function onBalAdjAmtChange() {
    var total = 0;
    $('#NumTotAdjustedAmt').val('');

    $('#tblTDSAdjustmentmodal tbody tr').each(function () {
        var value = parseFloat($(this).find('.numBalAdjAmt').val()) || 0;
        total += value;
    });

    $('#NumTotAdjustedAmt').val((total + totalExistingAdjustment));
}

function saveTDSAdjustment() {
    var rows = [];

    $('#tblTDSAdjustmentmodal tbody tr').each(function () {

        var row = $(this);

        rows.push({
            VType: row.find('.tdsAdjVType').text(),
            VNo: parseInt(row.find('.tdsAdjVNo').text()) || 0,
            VDate: row.find('.tdsAdjVDate').text(),
            Amount: parseFloat(row.find('.tdsAdjAmount').text()) || 0,
            Narration: row.find('.tdsAdjNarr').text(),
            AdjustedAmount: parseFloat(row.find('.tdsAdjadjustedAmount').text()) || 0,
            BalanceAdjustment: parseFloat(row.find('.numBalAdjAmt').val()) || 0
        });
    });

    if (rows.length === 0) {
        showToast('No records found.', {type:"warning"});
        return;
    }

    var request = {
        PartyCode: parseInt($('#TxtPartyCodeTDSAdj').val() || 0),
        VType: $('#txtRefTDSAdjVTypeCA').val() || '',
        VNo: parseInt($('#NumRefTDSAdjVno').val()) || 0,
        VDate: $('#DtTDSAdjVDate').val(),
        Rows: rows
    };

    console.log("TDS Adjustment Save Request: ", request);
    $.ajax({
        url: '/ImportExportExpensesEntry/SaveTDSAdjustment',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        data: JSON.stringify(request),

        beforeSend: function () {
            $('#btnSave').prop('disabled', true);
            showLoader();
        },

        success: function (response) {
            if (response.success) {
                showToast(response.message, {type:"success"});
            } else {
                showToast(response.message, {type:"warning"});
            }
        },

        error: function (xhr) {
            showToast('Error while saving data.', {type:"error"});
        },

        complete: function () {
            $('#btnSave').prop('disabled', false);
            hideLoader();
        }
    });
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    if (isNaN(date)) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}