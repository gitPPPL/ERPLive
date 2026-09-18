const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
let isReadOnly = urlParams.get('readOnly') === 'true';
var controllerName = window.location.pathname.split('/')[1];

let isLoadByEdit = false;

let compCode = "";
let yearCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";

$(document).ready(async function () {
    checkPermissionForEntryPage(controllerName, function () {});
    await GetVNo();
    const currentDate = getCurrentDateYMD();
    $('#DtDocDate').val(currentDate);

    if (!isReadOnly) {
        $('#DtDocDate').focus();
    }

    getGlobalValues();
    await bindHeaderDropdowns();
    wireEvents();
    await addNewRowBelow();
    if (rowId && !isNaN(rowId)) {
        GetDataById();
    }
})

//=============GENERATE VNO=================
async function GetVNo() {
    try {
        const res = await fetch('/SalesExportCosting/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//=============DROPDOWN=================
async function bindHeaderDropdowns() {
    return Promise.all([
        bindDropdown("SalesExportCosting", "party", '#ddlPartyName', '--Select Party--', null, null, false, null, true),
        bindDropdown("SalesExportCosting", "delivery", '#ddlDeliveryAt', '--Select Delivery--', null, null, false, null, true),
        bindDropdown("SalesExportCosting", "agent", '#ddlAgentName', '--Select Agent--', null, null, false, null, true),
        bindDropdown("SalesExportCosting", "currency", '#ddlCurrency', '--Select Currency--', null, null, true, null, true)
    ]);
}
function loadItemList(dropdownId) {
    const ddl = $(dropdownId);
    if (!ddl.length) return;
    if (ddl.hasClass('select2-hidden-accessible')) return;

    ddl.select2({
        placeholder: "-- Select --",
        allowClear: true,
        minimumInputLength: 0,
        ajax: {
            url: '/SalesExportCosting/GetItemList',
            dataType: 'json',
            delay: 250,
            global: false,
            data: function (params) {
                return {
                    searchTerm: params.term || "",
                    page: params.page || 1
                };
            },
            processResults: function (data, params) {
                params.page = params.page || 1;

                const results = data.data.map(item => ({
                    id: item.Value,
                    text: item.Text
                }));

                return {
                    results: results,
                    pagination: {
                        more: (params.page * 30) < data.totalCount
                    }
                };
            },
            cache: true
        }
    });
    ddl.on('select2:open', function () {

        setTimeout(function () {
            const searchBox = document.querySelector('.select2-container--open .select2-search__field');
            if (searchBox) searchBox.focus();
        }, 0);
    });
}
function setSelect2Value(selector, value, text) {
    const $ddl = $(selector);
    if (!$ddl.length || value == null) return;
    const option = new Option(text || '', value, true, true);
    $ddl.append(option).trigger('change');
}

//=============ADD ROWS IN FOOTER TABLE=================
function createRowHtml(data = {}) {

    return `
        <tr>
            <td><input class="form-control form-control-sm item-code" type="number" value="${data.iteM_CODE || data.ITEM_CODE || ''}" disabled/></td>
            <td><select class="form-control form-control-sm item-name"></select></td>

            <td><input class="form-control form-control-sm rate" type="number" value="${data.rate || data.RATE || ''}"/></td>
            <td><input class="form-control form-control-sm qty" type="number" value="${data.QTY || data.qty || ''}"/></td>
            <td><input class="form-control form-control-sm hsn-code" type="text" value="${data.hsN_CODE || data.HSN_CODE || ''}"/></td>

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

    const $previousLastRow = $("#tblSalesExportCosting tbody tr:last");
    $previousLastRow.find(".btn-add-action").remove();

    let rowHtml = createRowHtml(data);
    $("#tblSalesExportCosting tbody").append(rowHtml);

    const $lastRow = $("#tblSalesExportCosting tbody tr:last");

    const itemName = $lastRow[0].querySelector(".item-name");
    loadItemList(itemName);
    setSelect2Value(itemName, data.iteM_CODE || data.ITEM_CODE, data.iteM_NAME || data.ITEM_NAME)
}

//=============EVENTS===============
function wireEvents() {
    //--------- Item Change ---------
    $(document).on("change", ".item-name", function () {
        if (isLoadByEdit) return;
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) return;

        const $row = $(this).closest("tr");
        $row.find(".item-code").val($(this).val() || "");
        setTimeout(() => {
            $row.find(".rate").trigger("focus");
        }, 0);
    });
    //---------- Delete Row Button Click -----------
    $(document).on('click', '.btn-delete-action', function () {
        const $tbody = $('#tblSalesExportCosting tbody');
        if ($tbody.find('tr').length === 1) return;

        const $row = $(this).closest('tr');
        $row.remove();

        const $lastRow = $tbody.find('tr:last');

        if ($lastRow.length) {
            if ($lastRow.find('.btn-add-action').length === 0) {
                $lastRow.find('.action-wrap').append(`
                <button type="button" class="act-btn add btn-add-action" title="Add Row"><i class="fa fa-plus-circle"></i></button>
            `);
            }
        }
    });
    //---------- Add Row Button Click -----------
    $(document).on('click', '.btn-add-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) return;

        await addNewRowBelow();
    });
    //--------- Save btn Click -----------
    $('#btn_save').on('click', async function (e) {
        e.preventDefault();
        const isValid = await validate();
        if (!isValid) return;

        try {
            SaveSalesExportCosting()
        }
        catch (error) {
            console.error(error);
        }
    })

    //--------- Header Calculation Events -----------
    $(document).on('input change', `#NumLoadingQty, #txtExRateFreight, #txtExRateRate, #txtOceanFreight, #txtDoorDelivery, #txtDuty28, #txtFactoryStuffing, #txtRailFreight, #txtTHC,
    #txtSealCharges, #txtBLCharges, #txtClearingCharges, #txtDocumentCharges, #txtTransportCharges, #txtCostKgExPlant, #txtOurOfferRate, #txtCommRate`, function () {
        calculation();
    });
}

//=============VALIDATIONS===============
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/SalesExportCosting/CheckValidDate', {
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
        console.error("Error:", error);
        return false;
    }
}
async function validate() {
    // Validate VNo
    if (!validateRequiredField('#NumDocNo', 'Doc Number')) return false;

    // Validate VDate
    const isValidVDate = await checkValidDate();
    if (!isValidVDate) return false;

    const $rows = $('#tblSalesExportCosting tbody tr');
    let hasItem = false;

    $rows.each(function () {
        const itemCode = $(this).find('.item-name').val();

        if (itemCode && itemCode !== '0') {
            hasItem = true;
            return false;
        }
    });

    if (!hasItem) {
        showToast('Items Details Required, Please Check!', { type: "warning" });
        return false;
    }

    // Validate Item Rows
    let isValid = true;

    $rows.each(function () {

        const $row = $(this);

        const $item = $row.find('.item-name');
        const itemCode = $item.val();

        // Item selected
        if (!itemCode || parseFloat(itemCode) <= 0) {
            setInvalid($item, 'Item Required, Please Check!');
            isValid = false;
            return false;
        }

        const currentSelect = $row.find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            isValid = false;
            return false;
        }

    });

    return isValid;
}
function checkDuplicateItems(currentSelect) {

    const value = currentSelect.value;
    if (!value) return false;

    let duplicate = false;

    document.querySelectorAll('#tblSalesExportCosting .item-name').forEach(el => {
        if (el === currentSelect) return;
        if (el.value === value) duplicate = true;
    });

    if (duplicate) {
        $(currentSelect).addClass("is-invalid");
        showToast("Duplicate item found!", { type: "warning" });
    } else {
        $(currentSelect).removeClass("is-invalid");
    }

    return duplicate;
}

//=============Collect Data For Save & Update==========
function CollectHeaderData() {

    const items = CollectFooterData();
    const request = {

        V_NO: parseInt($('#NumDocNo').val()) || 0,
        V_DATE: $('#DtDocDate').val() || "",
        PARTY_CODE: parseInt($('#ddlPartyName').val()) || 0,
        DEL_LOCATION: parseInt($('#ddlDeliveryAt').val()) || 0,
        AGENT_CODE: parseInt($('#ddlAgentName').val()) || 0,
        COMM_RATE: parseFloat($('#txtCommRate').val()) || 0,
        CURRENCY: $('#ddlCurrency').val() || "",
        EX_RATE: parseFloat($('#txtExRateRate').val()) || 0,
        EX_FRTRATE: parseFloat($('#txtExRateFreight').val()) || 0,
        LOADING_QTY: parseFloat($('#NumLoadingQty').val()) || 0,
        STUFF_QTY: parseFloat($('#NumStuffingQty').val()) || 0,
        OCEAN_FRTUSD: parseFloat($('#txtOceanFreight').val()) || 0,
        OCEAN_FRTEXPS: parseFloat($('#txtOceanFreightTotal').val()) || 0,
        OCEAN_ANSCH: parseFloat($('#NumOceanANSCharges').val()) || 0,
        OCEAN_ILHAUCOST: parseFloat($('#ddlOceanInlandHaulageCost').val()) || 0,
        OCEAN_PORTHANDCH: parseFloat($('#txtTHC').val()) || 0,
        OCEAN_BLFEE: parseFloat($('#txtOceanBLFees').val()) || 0,
        OCEAN_SEALCOST: parseFloat($('#txtSealCharges').val()) || 0,
        OCEAN_DOCFEEEXPORT: parseFloat($('#txtOceanDocFeeExport').val()) || 0,
        CONCER_EXPS: parseFloat($('#txtFactoryStuffing').val()) || 0,
        SHIP_RAILFRT: parseFloat($('#txtRailFreight').val()) || 0,
        BUSY_SEASONCH: parseFloat($('#NumBusySeasonCharges').val()) || 0,
        LOCAL_TPTCOST: parseFloat($('#txtTransportCharges').val()) || 0,
        INSU_COST: parseFloat($('#NumInsuranceCost').val()) || 0,
        BANK_CHARGES: parseFloat($('#txtBankingCharges').val()) || 0,
        BL_CHARGES: parseFloat($('#txtBLCharges').val()) || 0,
        CLEARING_COST: parseFloat($('#txtClearingCharges').val()) || 0,
        DOOR_DELUSD: parseFloat($('#txtDoorDelivery').val()) || 0,
        DOOR_DELCOST: parseFloat($('#txtDoorDeliveryTotal').val()) || 0,
        DOC_CHARGES: parseFloat($('#txtDocumentCharges').val()) || 0,
        CHA_AGENCYCOST: parseFloat($('#txtCHAAgencyCost').val()) || 0,
        CHA_NOMCOST: parseFloat($('#txtCHANominationCost').val()) || 0,
        CHA_EXAMCOST: parseFloat($('#txtCHAExaminationCost').val()) || 0,
        CHA_CGMCOST: parseFloat($('#NumCHACGMCost').val()) || 0,
        CHA_CMCCOST: parseFloat($('#txtCHACMCCost').val()) || 0,
        CHA_VGMCOST: parseFloat($('#NumCHAVGM').val()) || 0,
        CHA_LULCOST: parseFloat($('#txtCHALoadingUnloadingCost').val()) || 0,
        SAMPLE_COSTING: $('#ddlSampleCosting').val() || "",
        SAMPLE_COSTAMT: parseFloat($('#NumSampleCostingAmt').val()) || 0,
        CUSTOM_COST: parseFloat($('#txtDuty28').val()) || 0,
        GRS_LESS1PER: parseFloat($('#NumGrossLess').val()) || 0,
        DISC_AMT: parseFloat($('#NumDiscAmt').val()) || 0,
        EPCG_AMT: parseFloat($('#NumEPCGAmt').val()) || 0,
        ADV_LICAMT: parseFloat($('#NumAdvLicence').val()) || 0,
        ROAD_TAPEAMT: parseFloat($('#NumRoadTapeAmt').val()) || 0,
        DDBAK_AMT: parseFloat($('#NumDDBKAmt').val()) || 0,
        COSTPERKG_EXPLANT: parseFloat($('#txtCostKgExPlant').val()) || 0,
        ADD_DBK: parseFloat($('#txtDBK').val()) || 0,
        LESS_MEIS: parseFloat($('#txtMEISBenefit').val()) || 0,
        OUR_OFFERRATE: parseFloat($('#txtOurOfferRate').val()) || 0,
        COSTING_TYPE: $('#ddlCostingType').val() || "",
        REMARKS: $('#txtremarks').val() || "",
        ACTION: (rowId && !isNaN(rowId)) ? "UPDATE" : "INSERT",

        items: items
    };

    return request;
}
function CollectFooterData() {
    const items = [];

    $('#tblSalesExportCosting tbody tr').each(function () {

        const $row = $(this);

        const item = {
            ITEM_CODE: parseInt($row.find('.item-name').val()) || 0,
            RATE: parseFloat($row.find('.rate').val()) || 0,
            QTY: parseFloat($row.find('.qty').val()) || 0,
            HSN_CODE: $row.find('.hsn-code').val() || ''
        };

        items.push(item);
    });

    return items;
}
function SaveSalesExportCosting() {
    const request = CollectHeaderData();
    console.log("Request Data For Save: ", request);
    $.ajax({
        url: '/SalesExportCosting/SaveSalesExportCosting',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function (res) {
            if (res.success) {
                showToast("Saved Successfully!", { type: "success" });
                setTimeout(() => window.location.href = '/SalesExportCosting/Index?id=' + encodeURIComponent($('#NumDocNo').val()) + '&readOnly=true', 1500);
            }
            else {
                showToast(res.message, { type: "warning" });
            }
        },
        error: function (xhr) {
            console.error(xhr);
            showToast("An error occurred while saving!", { type: "error" });
        }
    })
}

//===========EDIT & VIEW==================
async function GetDataById() {
    try {
        isLoadByEdit = true;

        const res = await $.ajax({
            url: '/SalesExportCosting/GetDataById',
            type: 'GET',
            data: { docId: rowId },
            dataType: 'JSON'
        });

        if (!res.success) {
            showToast(res.message, { type: "warning" });
            return;
        }

        console.log("Get By Id: ", res.data);

        await bindDataById(res.data);
    }
    catch (xhr) {
        console.error(xhr);
        showToast('An error occurred while fetching data by Id!', { type: "error" });
    }
    finally {
        isLoadByEdit = false;
    }
}
async function bindDataById(data) {

    if (!data) return;

    // ================= HEADER =================
    $('#NumDocNo').val(data.v_NO ?? '');

    if (data.v_DATE) {
        $('#DtDocDate').val(data.v_DATE.substring(0, 10));
    } else {
        $('#DtDocDate').val('');
    }

    $('#ddlPartyName').val(data.partY_CODE || "").trigger('change');
    $('#ddlDeliveryAt').val(data.deL_LOCATION || "").trigger('change');
    $('#ddlAgentName').val(data.agenT_CODE || "").trigger('change');
    $('#ddlCurrency').val(data.currency || "").trigger('change');

    $('#NumLoadingQty').val(data.loadinG_QTY ?? '');
    $('#NumStuffingQty').val(data.stufF_QTY ?? '');
    $('#NumEPCGAmt').val(data.epcG_AMT ?? '');
    $('#NumGrossLess').val(data.grS_LESS1PER ?? '');
    $('#NumDiscAmt').val(data.disC_AMT ?? '');
    $('#NumRoadTapeAmt').val(data.roaD_TAPEAMT ?? '');
    $('#NumAdvLicence').val(data.adV_LICAMT ?? '');
    $('#NumBusySeasonCharges').val(data.busY_SEASONCH ?? '');
    $('#NumDDBKAmt').val(data.ddbaK_AMT ?? '');
    $('#NumInsuranceCost').val(data.insU_COST ?? '');
    $('#ddlOceanInlandHaulageCost').val(data.oceaN_ILHAUCOST ?? '');
    $('#ddlCostingType').val(data.costinG_TYPE ?? '').trigger('change');
    $('#ddlSampleCosting').val(data.samplE_COSTING ?? '').trigger('change');
    $('#NumSampleCostingAmt').val(data.samplE_COSTAMT ?? '');
    $('#NumCHAVGM').val(data.chA_VGMCOST ?? '');
    $('#NumCHACGMCost').val(data.chA_CGMCOST ?? '');
    $('#txtCHACMCCost').val(data.chA_CMCCOST ?? '');
    $('#txtCHAExaminationCost').val(data.chA_EXAMCOST ?? '');
    $('#txtCHALoadingUnloadingCost').val(data.chA_LULCOST ?? '');
    $('#txtCHAAgencyCost').val(data.chA_AGENCYCOST ?? '');
    $('#txtCHANominationCost').val(data.chA_NOMCOST ?? '');

    $('#txtremarks').val(data.remarks ?? '');

    $('#txtExRateFreight').val(data.eX_FRTRATE ?? '');
    $('#txtExRateRate').val(data.eX_RATE ?? '');
    $('#txtOceanFreight').val(data.oceaN_FRTUSD ?? '');
    $('#txtOceanFreightTotal').val(data.oceaN_FRTEXPS ?? '');
    $('#txtDoorDelivery').val(data.dooR_DELUSD ?? '');
    $('#txtDoorDeliveryTotal').val(data.dooR_DELCOST ?? '');
    $('#txtRailFreight').val(data.shiP_RAILFRT ?? '');
    $('#txtFactoryStuffing').val(data.conceR_EXPS ?? '');
    $('#txtTHC').val(data.oceaN_PORTHANDCH ?? '');
    $('#txtSealCharges').val(data.oceaN_SEALCOST ?? '');
    $('#txtBLCharges').val(data.bL_CHARGES ?? '');
    $('#txtClearingCharges').val(data.clearinG_COST ?? '');
    $('#txtDocumentCharges').val(data.doC_CHARGES ?? '');
    $('#txtTransportCharges').val(data.locaL_TPTCOST ?? '');
    $('#txtOceanBLFees').val(data.oceaN_BLFEE ?? '');
    $('#txtOceanDocFeeExport').val(data.oceaN_DOCFEEEXPORT ?? '');
    $('#txtBankingCharges').val(data.banK_CHARGES ?? '');
    $('#txtDuty28').val(data.custoM_COST ?? '');
    $('#txtCostKgExPlant').val(data.costperkG_EXPLANT ?? '');
    $('#txtDBK').val(data.adD_DBK ?? '');
    $('#txtMEISBenefit').val(data.lesS_MEIS ?? '');
    $('#txtOurOfferRate').val(data.ouR_OFFERRATE ?? '');
    $('#txtCommRate').val(data.comM_RATE ?? '');
    $('#txtProductValue').val(data.productValue ?? '');
    $('#txtTotalCost').val(data.totalCost ?? '');
    $('#txtCostKg').val(data.costKg ?? '');
    $('#txtTotalCostKg').val(data.totalCostKg ?? '');
    $('#txtTotalCostKgRs').val(data.totalCostKgRs ?? '');
    $('#txtTotalCostMTRs').val(data.totalCostMTRs ?? '');
    $('#txtCostKgUSD').val(data.costKgUSD ?? '');
    $('#txtCostMTUSD').val(data.costMTUSD ?? '');
    $('#txtTotal').val(data.total ?? '');
    $('#NumOceanANSCharges').val(data.oceaN_ANSCH ?? '');

    // ================= ITEMS =================

    const $tbody = $('#tblSalesExportCosting tbody');

    $tbody.empty();

    if (Array.isArray(data.items) && data.items.length > 0) {
        for (const item of data.items) {
            await addNewRowBelow(item);
        }
    }
    else {
        await addNewRowBelow();
    }

    // ================= READ ONLY =================
    if (isReadOnly) {
        setFormReadonly();
    }

    calculation();
}
function setFormReadonly() {
    const form = $('#SalesExportCostingform');
    form.addClass('erppage-readonly');
    $('#btn_save').hide();
    $('#tblSalesExportCosting .btn-delete-action, #tblSalesExportCosting .btn-add-action')
        .prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
}

//===========Calculations=================
function calculation() {
    // 1. FIBER
    const costPerKgExPlant = parseFloat($('#txtCostKgExPlant').val()) || 0;
    const lulQty = parseFloat($('#NumLoadingQty').val()) || 0;
    const fiber = costPerKgExPlant * lulQty;
    $('#txtProductValue').val(fiber || 0);

    // 2. OCEAN FREIGHT
    const oceanFrtUSD = parseFloat($('#txtOceanFreight').val()) || 0;
    const exRateFrt = parseFloat($('#txtExRateFreight').val()) || 0;
    const oceanFrt = oceanFrtUSD * exRateFrt;
    const oceanFrtRounded = Number(oceanFrt.toFixed(0));
    $('#txtOceanFreightTotal').val(oceanFrtRounded.toFixed(0));

    // 3. TOTAL EXPENSES
    const doorDelCost = parseFloat($('#txtDoorDeliveryTotal').val()) || 0;
    const duty = parseFloat($('#txtDuty28').val()) || 0;
    const railFrt = parseFloat($('#txtRailFreight').val()) || 0;
    const concurExps = parseFloat($('#txtFactoryStuffing').val()) || 0;
    const thc = parseFloat($('#txtTHC').val()) || 0;
    const sealCost = parseFloat($('#txtSealCharges').val()) || 0;
    const blCharges = parseFloat($('#txtBLCharges').val()) || 0;
    const clearingCost = parseFloat($('#txtClearingCharges').val()) || 0;
    const docCharges = parseFloat($('#txtDocumentCharges').val()) || 0;
    const transportCharge = parseFloat($('#txtTransportCharges').val()) || 0;

    const totalExps = oceanFrtRounded + doorDelCost + duty + railFrt + concurExps + thc + sealCost + blCharges + clearingCost + docCharges + transportCharge;
    const totalExpsRounded = Number(totalExps.toFixed(0));
    $('#txtTotalCost').val(totalExpsRounded.toFixed(0));

    // 4. COST PER KG
    let costPerKg = 0;
    if (lulQty !== 0) {
        costPerKg = totalExpsRounded / lulQty;
    }

    const costPerKgRounded = Number(costPerKg.toFixed(2));
    $('#txtCostKg').val(costPerKgRounded.toFixed(2));

    // 5. TOTAL COST
    const totalCost = costPerKgRounded + costPerKgExPlant;
    const totalCostRounded = Number(totalCost.toFixed(2));
    $('#txtTotalCostKg').val(totalCostRounded.toFixed(2));

    // 6. ADD DBK
    const ourRate = parseFloat($('#txtOurOfferRate').val()) || 0;
    let addDBK = 0;
    if (lulQty !== 0) {
        addDBK = ((ourRate / 1000) - (oceanFrtUSD / lulQty)) * 1.2 * 0.01;
    }

    const addDBKRounded = Number(addDBK.toFixed(3));
    $('#txtDBK').val(addDBKRounded.toFixed(3));

    // 7. LESS MEIS
    let lessMEIS = 0;
    if (lulQty !== 0) {
        lessMEIS = ((ourRate / 1000) - (oceanFrtUSD / lulQty)) * 1.3 * 0.01;
    }

    const lessMEISRounded = Number(lessMEIS.toFixed(3));
    $('#txtMEISBenefit').val(lessMEISRounded.toFixed(3));

    // 8. COST PER KG RS
    $('#txtTotalCostKgRs').val(totalCostRounded.toFixed(2));

    // 9. COST PER MTR RS
    const costPerMTRs = totalCostRounded * 1000;
    const costPerMTRsRounded = Number(costPerMTRs.toFixed(2));
    $('#txtTotalCostMTRs').val(costPerMTRsRounded.toFixed(2));

    // 10. COST PER KG USD
    const exRate = parseFloat($('#txtExRateRate').val()) || 0;
    let costPerKgUSD = 0;

    if (exRate !== 0) {
        costPerKgUSD = totalCostRounded / exRate;
    }

    const costPerKgUSDRounded = Number(costPerKgUSD.toFixed(3));
    $('#txtCostKgUSD').val(costPerKgUSDRounded.toFixed(3));

    // 11. COST PER MT USD
    const costPerMTUSD = costPerKgUSDRounded * 1000;
    const costPerMTUSDRounded = Number(costPerMTUSD.toFixed(0));
    $('#txtCostMTUSD').val(costPerMTUSDRounded.toFixed(0));


    // 12. TOTAL AMOUNT
    const commRate = parseFloat($('#txtCommRate').val()) || 0;
    const totalAmt = costPerMTUSDRounded + commRate;
    const totalAmtRounded = Number(totalAmt.toFixed(0));
    $('#txtTotal').val(totalAmtRounded.toFixed(0));
}

//=============REPORT=================
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/SalesExportCosting/GetGlobalValues",
            type: "GET",
            dataType: "json"
        });

        if (response.success) {
            const d = response.data;
            compCode = d.compCode;
            yearCode = d.yearCode;
            branchCode = d.branchCode;
            companyName = d.companyName;
            add1 = d.add1;
            add2 = d.add2;
            db = d.db;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}
function ExportCostingReport() {

    var reportName = "EXPORT_COST_MAST";
    var vType = "EXPC";
    var VNO = ($("#NumDocNo").val() || "").trim();

    var formula =
        "{COSTING_EXPORT1.COMP_CODE} = " + compCode +
        " AND {COSTING_EXPORT1.BRANCH_CODE} = " + branchCode +
        " AND {COSTING_EXPORT1.YEAR_CODE} = " + yearCode +
        " AND {COSTING_EXPORT1.V_TYPE} = '" + vType + "'" +
        " AND {COSTING_EXPORT1.V_NO} = " + VNO;

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,

        Parameters: {
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2,
            RPTNAME: "EXPORT COSTING",
        }
    };

    // Generate timestamp
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
        xhrFields: { responseType: 'blob' },

        success: function (response) {
            console.log("PDF response:", response);
            var file = new Blob([response], { type: 'application/pdf' });

            var fileName = `${reportName}_${timestamp}.pdf`;
            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
            URL.revokeObjectURL(link.href);
        },

        error: function (xhr, status, error) {
            console.error("Error generating report:", error);
            console.error("Response:", xhr.responseText);
        }
    });
}

//=============Set MaxLength=================
function SetMaxlength(selector, precision, scale) {
    let value = $(selector).val();

    const integerDigits = precision - scale;

    const regex = new RegExp(
        `^\\d{0,${integerDigits}}(\\.\\d{0,${scale}})?$`
    );

    if (!regex.test(value)) {
        $(selector).val(value.slice(0, -1));
    }
}