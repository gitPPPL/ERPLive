const ddlCache = new Map();
let isLoadByRefNo = false;
let isLoadByWBNo = false;
let isLoadById = false;

const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const rowIdVType = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';
const DBTableName = 'DC_NOTE1';


let compCode = "";
let yearCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";
let companyGSTIN = "";
let companyPhone = "";
let companyEmail = "";

$(document).ready(async function () {
    var controllerName = window.location.pathname.split('/')[1];
    checkPermissionForEntryPage(controllerName, function () {
    });
    $('#ddlDocType').focus();
    const currentDate = getCurrentDateYMD();
    $('#DtDocDate, #DtRefDate, #DtBillDate, #DtExpectedReturnDate, #DtGRDate').val(currentDate);
    toggleDate();
    getGlobalValues();
    wireEvents();
    await bindAllDropdowns();
    await addNewRowBelow();
    if (rowId && rowIdVType) {
        await GetDataById();
        if (rowIdVType === "DCHL") {
            checkApprovalStatus(rowIdVType, rowId, DBTableName);
        }
    }
    if (isReadOnly) {
        setFormReadonly();
    }
})

//=============DROPDOWN==============
function loadDdl(type, id, vType = '', params = {}) {

    const $ddl = id instanceof jQuery ? id : $('#' + id.replace(/^#/, ''));

    if (!$ddl.length) {
        return Promise.resolve([]);
    }

    const shipFromCode = params.shipFromCode || '';
    const selectFirst = params.selectFirst === true;

    const key = `${type}|${vType}|${shipFromCode}`;

    let request = ddlCache.get(key);

    if (!request) {

        request = $.get('/DeliveryChallanStore/GetDdlList', {
            type: type,
            vType: vType,
            shipFromCode: shipFromCode
        });

        ddlCache.set(key, request);
    }

    return request.then(function (data) {

        $ddl.empty();
        const fragment = document.createDocumentFragment();
        fragment.appendChild(new Option('Select', ''));

        $.each(data, function (_, x) {

            const option = new Option(x.Text, x.Value);

            // Add all properties except Text and Value
            Object.keys(x).forEach(function (key) {

                if (key === 'Text' || key === 'Value') {
                    return;
                }

                const dataKey = key
                    .replace(/([a-z])([A-Z])/g, '$1-$2')
                    .replace(/_/g, '-')
                    .toLowerCase();

                $(option).attr('data-' + dataKey, x[key] ?? '');
            });

            fragment.appendChild(option);
        });

        $ddl[0].appendChild(fragment);

        if (selectFirst) {

            const $first = $ddl.find('option[value!=""]').first();

            if ($first.length) {
                $ddl.val($first.val()).trigger('change');
            }
        }

        return data;
    });
}
async function bindAllDropdowns() {

    const ddl = {
        ddlDocType: 'doctype',
        ddlRefBillType: 'refbilltype',
        ddlbillto: 'party',
        ddlshiptoSD: 'party',
        ddldespparty: 'party',
        ddldesppartySD: 'party',
        ddlResponsiblePerson: 'responsibleperson',
        ddldesploc: 'city',
        ddldesplocSD: 'city',
        ddltransportname: 'transport'
    };

    const requests = Object.entries(ddl).map(([id, type]) => {
        return loadDdl(type, id);
    });

    await Promise.all(requests);

    const select2Ddls = [
        'ddlbillto',
        'ddlshiptoSD',
        'ddldespparty',
        'ddldesppartySD',
        'ddlResponsiblePerson',
        'ddldesploc',
        'ddldesplocSD',
        'ddltransportname'
    ];

    select2Ddls.forEach(function (id) {
        initSelect2($('#' + id));
    });
}
function loadItemList(dropdownId) {
    const ddl = $(dropdownId);
    if (!ddl.length) {
        return;
    }

    if (ddl.hasClass('select2-hidden-accessible')) {
        return;
    }
    ddl.select2({
        placeholder: "-- Select --",
        allowClear: true,
        // 1. Change this to 0 so it triggers as soon as the dropdown opens
        minimumInputLength: 0,
        ajax: {
            url: '/DeliveryChallanStore/GetItemList',
            dataType: 'json',
            delay: 250,
            global: false,
            data: function (params) {
                return {
                    // If params.term is undefined (on first click), pass an empty string
                    searchTerm: params.term || "",
                    page: params.page || 1
                };
            },
            processResults: function (data, params) {
                params.page = params.page || 1;

                const results = data.data.map(item => ({
                    id: item.Value,
                    text: item.Text,
                    // Custom data
                    ucode: item.ucode,
                    unit: item.unit,
                    hsncode: item.hsncode
                }));

                return {
                    results: results,
                    pagination: {
                        // Triggers infinite scroll loading if there are more items
                        more: (params.page * 30) < data.totalCount
                    }
                };
            },
            cache: true
        }
    });
    ddl.on('select2:open', function () {

        setTimeout(function () {

            const searchBox =
                document.querySelector(
                    '.select2-container--open .select2-search__field'
                );

            if (searchBox) {
                searchBox.focus();
            }

        }, 0);
    });
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
function setSelect2Value(selector, value, text) {
    const $ddl = $(selector);
    if (!$ddl.length || value == null) return;
    const option = new Option(text || '', value, true, true);
    $ddl.append(option).trigger('change');
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

//=============EVENTS===============
function wireEvents() {
    bindHeaderEvents();
    bindTotalEvents();
    bindGridEvents();
}
function bindHeaderEvents() {
    //--------- Bill Type Change --------
    $('#ddlRefBillType').on('change', function () {
        const billType = ($(this).val() || '').trim();
        if (billType && billType !== "") {
            loadDdl('refno', 'ddlRefBillNo', billType);
        }
    });

    //--------- Doc Type Change --------
    $('#ddlDocType').on('change', async function () {
        const vTypeEl = $(this);
        const vType = (vTypeEl.val() || '').trim();
        if (vType) {
            await GetVNo(vType);
            vTypeEl.prop('disabled', true);
            $('#ddlRefBillType').focus();
        }
    });

    //--------- Bill From List Change --------
    $('#ddlbillto').on('change', function () {
        if (isLoadById) return;
        const billFromCode = $(this).val();

        clearControlsOnBillChange();

        loadDdl("address", "ddladdl1a", "", { shipFromCode: billFromCode, selectFirst: true });

        if (isLoadByRefNo) return;
        // Same party/code bind
        $('#ddlshiptoSD').val(billFromCode).trigger('change');
        $('#ddldespparty').val(billFromCode).trigger('change');
        $('#ddldesppartySD').val(billFromCode).trigger('change');
    });

    //--------- Ship From List Change --------
    $('#ddlshiptoSD').on('change', function () {
        if (isLoadById) return;
        const billFromCode = $(this).val();

        //Ship Details
        $('#Txtaddl1SD').val('');
        $('#Txtaddl2SD').val('');
        $('#Txtaddl3SD').val('');
        $('#ddlBillCitySD').val('');
        $('#NumBillGSTSD').val('');
        $('#NumPincodeSD').val('');

        loadDdl("address", "ddladdl1aSD", "", { shipFromCode: billFromCode, selectFirst: true });
    });

    //---------- Bill Address Change ----------
    bindAddressChange({
        addressSelector: '#ddladdl1a',
        partySelector: '#ddlbillto',
        add1Selector: '#Txtaddl1',
        add2Selector: '#Txtaddl2',
        add3Selector: '#Txtaddl3',
        gstSelector: '#NumBillGST',
        pincodeSelector: '#NumPincode',
        citySelector: '#ddlBillCity',
        isBillChange: true
    });

    //---------- Ship Address Change -----------
    bindAddressChange({
        addressSelector: '#ddladdl1aSD',
        partySelector: '#ddlshiptoSD',
        add1Selector: '#Txtaddl1SD',
        add2Selector: '#Txtaddl2SD',
        add3Selector: '#Txtaddl3SD',
        gstSelector: '#NumBillGSTSD',
        pincodeSelector: '#NumPincodeSD',
        citySelector: '#ddlBillCitySD'
    });

    //--------- Ref No Load Button Click --------
    $('#btn__load').on('click', async function () {
        const vType = ($('#ddlRefBillType').val() || '').trim();
        const vNo = parseInt($('#ddlRefBillNo').val() || 0);
        await getDetailsOnRefTypeLoad(vType, vNo);
    })

    //--------- WB Load Qty Button Click --------
    $('#btn__loadqty').on('click', async function () {
        const vNo = parseInt($('#NumWBDocNo').val() || 0);
        await getDetailsOnWBLoad(vNo);
    })

    $('#ddlMovtype').on('change', function () {
        const movementType = $(this);
        ((movementType.val() || '').trim() === "Outward") ? $('#lblbillTo').text('Bill To') : $('#lblbillTo').text('Bill From');
    });

    //--------- Save btn Click -----------
    $('#btn_save').on('click', async function (e) {
        e.preventDefault();
        const isValid = await validate();
        if (!isValid) return;

        const isWBDone = await validateWB();
        //if (!isWBDone) {
        //    return;
        //}
        try {
            SaveDeliveryChallanDetails()
        }
        catch (error) {
            console.error(error);
        }
    })
}
function clearControlsOnBillChange() {
    //Bill Details
    $('#Txtaddl1').val('');
    $('#Txtaddl2').val('');
    $('#Txtaddl3').val('');
    $('#ddlBillCity').val('');
    $('#NumBillGST').val('');
    $('#NumPincode').val('');
    $('#ddlPOSCity').val('');
    $('#ddldespgst').val('');
    $('#ddldesploc').val('').trigger('change');

    //Ship Details
    $('#Txtaddl1SD').val('');
    $('#Txtaddl2SD').val('');
    $('#Txtaddl3SD').val('');
    $('#ddlBillCitySD').val('');
    $('#NumBillGSTSD').val('');
    $('#NumPincodeSD').val('');
    $('#ddldesppartySD').val('');
    $('#ddldespgstSD').val('');
    $('#ddldesplocSD').val('').trigger('change');
}
function bindAddressChange({ addressSelector, partySelector, add1Selector, add2Selector, add3Selector, gstSelector, pincodeSelector,
    citySelector, isBillChange = false }) {
    $(addressSelector).on('change', async function () {

        if (isLoadByRefNo || isLoadById) return;

        const $address = $(this);
        let addressId = $address.val();

        if (!addressId) {
            $address.prop('selectedIndex', 0);
            addressId = $address.val();
        }

        const code = $(partySelector).val();
        if (code && code > 0) {
            try {
                const response = await $.ajax({
                    url: '/DeliveryChallanStore/GetAddressByBillToParty',
                    type: 'GET',
                    data: {code, addressId}
                });

                const address = response.addressDetails;
                console.log("address details on bill/ship change: ", address);
                $(add1Selector).val(address.add1);
                $(add2Selector).val(address.add2);
                $(add3Selector).val(address.add3);
                $(gstSelector).val(address.gstin);
                $(pincodeSelector).val(address.pincode);

                setTranType();

                await loadDdl("city", citySelector);
                $(citySelector).val(address.cityCode).trigger('change');
                if (isBillChange) {
                    await loadDdl("city", '#ddlPOSCity');
                    $('#ddldespgst, #ddldespgstSD').val(address.gstin);
                    $('#ddlPOSCity, #ddldesploc, #ddldesplocSD').val(address.cityCode).trigger('change');
                }
            } catch (error) {
                showToast('Error loading address', { type: "error" });
            }
        }

    });
}
function bindTotalEvents() {
    //Pack %
    $('#NumPacking1').on('input', function () {
        if (isLoadById) return;
        recalculateTotals({
            calculatePackingAmount: true,
            calculateGridTotal: true
        });
    });
    //Pack Amt
    $('#NumPacking2').on('input', function () {
        if (isLoadById) return;

        const discountPer = getNumber('#NumDiscount1');
        if (discountPer === 0 && chkGridPackPercent() === true) {
            $('#NumPacking2').val('0');
            gridTotal();
            calculateNetAmount(false);
            return;
        }
        // Recalculate packing from Packing %
        recalculateTotals({
            calculatePackingAmount: true,
            calculateGridTotal: true
        });

    });
    //Disc %
    $('#NumDiscount1').on('input', function () {
        if (isLoadById) return;
        recalculateTotals({
            calculateDiscountAmount: true,
            calculateGridTotal: true
        });
    });
    //Disc Amt
    $('#NumDiscount2').on('input', function () {
        if (isLoadById) return;
        const discountPer = getNumber('#NumDiscount1');
        if (discountPer === 0 && chkGridDiscPercent() === true) {
            $('#NumDiscount2').val('0');
            gridTotal();
            calculateNetAmount(false);
            return;
        }

        // Recalculate discount from Discount %
        recalculateTotals({
            calculateDiscountAmount: true,
            calculateGridTotal: true
        });
    });
    //Round Amt
    $('#NumRoundOff').on('keyup', function () {
        if (isLoadById) return;

        const totalAmt = parseFloat($('#NumAmount').val()) || 0;
        const packing = parseFloat($('#NumPacking2').val()) || 0;
        const cgst = parseFloat($('#NumCGST2').val()) || 0;
        const sgst = parseFloat($('#NumSGST2').val()) || 0;
        const igst = parseFloat($('#NumIGST2').val()) || 0;
        const discount = parseFloat($('#NumDiscount2').val()) || 0;
        const roundOff = parseFloat($('#NumRoundOff').val()) || 0;

        const netAmount = totalAmt + packing + cgst + sgst + igst - discount + roundOff;
        $('#NumNetAmount').val(netAmount.toFixed(2));
    });
}
function bindGridEvents() {
    //---------- Delete Row Button Click -----------
    $(document).on('click', '.btn-delete-action', function () {
        // Prevent deleting if only one row exists
        const $tbody = $('#tblItemrecord tbody');
        if ($tbody.find('tr').length === 1) {
            return;
        }
        const $row = $(this).closest('tr');
        $row.remove();
    });

    //---------- Add Row Button Click -----------
    $(document).on('click', '.btn-add-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            return;
        }
        await addNewRowBelow();
    });

    //--------- Item Change ---------
    $(document).on("change", ".item-name", function () {
        if (isLoadByRefNo || isLoadByWBNo || isLoadById) return;

        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        // Duplicate
        if (checkDuplicateItems(currentSelect)) {
            return;
        }

        const item = $(this).select2('data')[0];

        if (!item) return;

        const $row = $(this).closest("tr");

        $row.find(".uom-code").val(item.ucode || "");
        $row.find(".uom-name").val(item.unit || "");
        $row.find(".hsn-code").val(item.hsncode || "");
    });

    //pack %
    $('#tblItemrecord tbody').on('input change', '.pack-per', function () {
        if (isLoadById) return;

        const $row = $(this).closest('tr');

        recalculateRow($row);
        gridTotal();
        calculateNetAmount(false);
        $('#NumPacking1').val('0');
    });
    //disc %
    $('#tblItemrecord tbody').on('input change', '.disc-per', function () {
        if (isLoadById) return;

        const $row = $(this).closest('tr');

        recalculateRow($row);
        gridTotal();
        calculateNetAmount(false);
        $('#NumDiscount1').val('0');
    });
    //pack %, disc % and amount changed
    $('#tblItemrecord tbody').on('input change', '.amount, .pack-per, .disc-per, .nos, .recd-qty, .inv-qty, .cgst-per, .sgst-per, .igst-per, .cgst-amt, .sgst-amt, .igst-amt',
        function () {
            if (isLoadById) return;

        const $row = $(this).closest('tr');

        recalculateRow($row);
        gridTotal();
        calculateNetAmount(false);
    });

    // Inv Qty and Rate changed
    $('#tblItemrecord tbody').on('focusout', '.inv-qty, .rate', function () {
        if (isLoadById) return;

        const $row = $(this).closest('tr');
        const qty = parseFloat($row.find('.inv-qty').val()) || 0;
        const rate = parseFloat($row.find('.rate').val()) || 0;

        $row.find('.amount').val((qty * rate).toFixed(2)).trigger('change');;
        calTax($row);
    });

    // Amount changed
    $('#tblItemrecord tbody').on('focusout', '.amount', function () {

        if (isLoadById) return;

        const $row = $(this).closest('tr');

        const amount = parseFloat($(this).val()) || 0;
        const qty = parseFloat($row.find('.inv-qty').val()) || 0;

        if (qty !== 0) {
            $row.find('.rate').val((amount / qty).toFixed(2)).trigger('change');
        }

        calTax($row);
        gridTotal();
        calculateNetAmount(false);
    });

    // Tax Code changed
    $('#tblItemrecord tbody').on('change', '.tax-code', function () {

        if (isLoadById) return;

        const $row = $(this).closest('tr');

        // Get selected tax information
        const selectedTax = $(this).find(':selected');

        const cgstPer = parseFloat(selectedTax.data('cgst-per')) || 0;
        const sgstPer = parseFloat(selectedTax.data('sgst-per')) || 0;
        const igstPer = parseFloat(selectedTax.data('igst-per')) || 0;

        const amount = parseFloat($row.find('.amount').val()) || 0;

        // CGST %
        $row.find('.cgst-per').val(cgstPer).trigger('change');
        // CGST Amount
        $row.find('.cgst-amt').val((amount * cgstPer / 100).toFixed(2)).trigger('change');
        // SGST %
        $row.find('.sgst-per').val(sgstPer).trigger('change').trigger('change');
        // SGST Amount
        $row.find('.sgst-amt').val((amount * sgstPer / 100).toFixed(2)).trigger('change');
        // IGST %
        $row.find('.igst-per').val(igstPer).trigger('change');
        // IGST Amount
        $row.find('.igst-amt').val((amount * igstPer / 100).toFixed(2)).trigger('change');

        gridTotal();
        calculateNetAmount(false);
    });
}
//=============GENERATE VNO=================
async function GetVNo(vType) {
    try {
        const res = await fetch(`/DeliveryChallanStore/GetVNo?vType=${encodeURIComponent(vType)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocno').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}

//=============ADD ROW=================
function createRowHtml(data = {}) {

    return `
        <tr>

            <td class="freeze-item"><select class="form-control form-control-sm item-name"></select></td>

            <td><input class="form-control form-control-sm hsn-code" type="text" value="${data.hsN_CODE || data.HSN_CODE || ''}"/></td>
            <td>
                <input class="form-control form-control-sm uom-code" type="hidden" value="${data.uoM_CODE || data.UOM_CODE || ''}" disabled/>
                <input class="form-control form-control-sm uom-name" type="text" value="${data.unit || data.UNIT || data.iteM_UNIT || data.ITEM_UNIT || ''}" disabled/>
            </td>

            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos || data.NOS || ''}"/></td>
            <td><input class="form-control form-control-sm recd-qty" type="number" value="${data.recD_QTY || data.RECD_QTY || data.GROSS || data.gross || ''}"/></td>
            <td><input class="form-control form-control-sm inv-qty" type="number" value="${data.QTY || data.qty || ''}"/></td>

            <td><input class="form-control form-control-sm rate" type="number" value="${data.rate || data.RATE || ''}"/></td>
            <td><input class="form-control form-control-sm amount" type="number" value="${data.amount || data.AMOUNT || ''}"/></td>

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

    // Remove delete button from the current last row
    const $previousLastRow = $("#tblItemrecord tbody tr:last");
    $previousLastRow.find(".btn-add-action").remove();

    let rowHtml = createRowHtml(data);
    $("#tblItemrecord tbody").append(rowHtml);

    const $lastRow = $("#tblItemrecord tbody tr:last");

    //Item Dropdown
    const itemName = $lastRow[0].querySelector(".item-name");
    loadItemList(itemName);
    setSelect2Value(itemName, data.iteM_CODE || data.ITEM_CODE, data.iteM_NAME || data.ITEM_NAME)

    // Tax dropdown
    const $tax = $lastRow.find(".tax-code");
    await loadDdl("tax", $tax);
    $tax.val(data.taX_CODE || data.TAX_CODE || "");
    initSelect2($tax);
}

//=============REF TYPE LOAD BTN CLICK=================
async function getDetailsOnRefTypeLoad(vType, vNo) {
    try {
        const res = await $.ajax({
            url: '/DeliveryChallanStore/GetDetailsOnRefNoLoad',
            type: 'GET',
            data: {vType: vType, vNo: vNo},
            dataType: 'JSON'
        });

        if (res.success) {
            console.log("Header and Footer details on Ref. No Load", res);
            await bindRefHeaderAndItemDetails(res);
        }
        else {
            showToast(`No data  found for Doc type = ${vType} and v_no = ${vNo}`, { type: "warning" });
        }
    }
    catch (xhr) {
        console.error(xhr);
        showToast("Error in fetching details.", { type: "error" });
    }
}
async function bindRefHeaderAndItemDetails(res) {
    const data = res.data;
    isLoadByRefNo = true;

    try {
        setDateControl(data.v_DATE, '#DtBillDate', '#chkBillDate');
        setDateControl(data.v_DATE, '#DtRefDate', '#chkRefDate');

        $('#TxtBillDocID').val(data.v_TYPE && data.v_NO ? data.v_TYPE + data.v_NO : '');
        //Bill Details
        $('#ddlbillto').val(data.partY_CODE || '').trigger('change');
        $('#Txtaddl1').val(data.bilL_ADD1 || '');
        $('#Txtaddl2').val(data.bilL_ADD2 || '');
        $('#Txtaddl3').val(data.bilL_ADD3 || '');
        $('#NumBillGST').val(data.bilL_GST || '');
        $('#NumPincode').val(data.bilL_PINCODE || '');
        await Promise.all([
            loadDdl("city", '#ddlBillCity'),
            loadDdl("city", '#ddlPOSCity'),
            loadDdl("city", '#ddlBillCitySD'),
        ]);
        $('#ddlBillCity, #ddlPOSCity').val(data.bilL_CITY).trigger('change');

        //Ship Details
        $('#ddlshiptoSD').val(data.shiP_FROM || '').trigger('change');
        $('#Txtaddl1SD').val(data.shiP_ADD1 || '');
        $('#Txtaddl2SD').val(data.shiP_ADD2 || '');
        $('#Txtaddl3SD').val(data.shiP_ADD3 || '');
        $('#NumBillGSTSD').val(data.shiP_GST || '');
        $('#NumPincodeSD').val(data.shiP_PINCODE || '');
        $('#ddlBillCitySD').val(data.shiP_CITY).trigger('change');

        setTranType();

        const items = data.items || [];
        $('#tblItemrecord tbody').empty();
        if (items && items.length > 0) {
            for (const item of items) {
                await addNewRowBelow(item);
            }
        }
        else {
            await addNewRowBelow();
        }

        gridTotal();
        calculateNetAmount(false);
    }
    finally {
        isLoadByRefNo = false;
    }
}

//=============WB LOAD QTY BTN CLICK=================
async function getDetailsOnWBLoad(vNo) {
    try {
        const res = await $.ajax({
            url: '/DeliveryChallanStore/GetDetailsOnWBLoad',
            type: 'GET',
            data: {vNo: vNo},
            dataType: 'JSON'
        });

        if (res.success) {
            isLoadByWBNo = true;
            console.log("Footer details on WB. No Load", res);
            const items = res.data || [];
            $('#tblItemrecord tbody').empty();
            for (const item of items) {
                await addNewRowBelow(item);
            }

            isLoadByWBNo = false;
        }
        else {
            await addNewRowBelow();
            showToast('No data  found!', { type: "warning" });
        }
    }
    catch (xhr) {
        console.error(xhr);
        showToast("Error in fetching details.", { type: "error" });
    }
}

//=============HELPERS=================
function formatDate(dateStr) {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    if (isNaN(date)) return '';
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}
function parseNullableDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    return isNaN(date.getTime()) ? null : date.toISOString();
}
function getOptionalDate(checkboxSelector, dateSelector) {
    return $(checkboxSelector).is(':checked') ? (parseNullableDate($(dateSelector).val()) || null) : null;
}

//=============Set Transaction Type on Behalf of GST=================
function setTranType() {

    const billGST = ($('#NumBillGST').val() || '').trim();
    const shipGST = ($('#NumBillGSTSD').val() || '').trim();

    if (billGST.length === 15 && shipGST.length === 15) {

        if (billGST === shipGST) {
            $('#ddlTranType').val('Regular').trigger('change');
        }
        else if (billGST !== shipGST) {
            $('#ddlTranType').val('Bill To-Ship To').trigger('change');
        }
    }
}

//=============Calculations============
//------Net Amount---------
function calculateNetAmount(isManualRoundOff = false) {

    const totalAmount = parseFloat($('#NumAmount').val()) || 0;
    const packing = parseFloat($('#NumPacking2').val()) || 0;
    const cgst = parseFloat($('#NumCGST2').val()) || 0;
    const sgst = parseFloat($('#NumSGST2').val()) || 0;
    const igst = parseFloat($('#NumIGST2').val()) || 0;
    const discount = parseFloat($('#NumDiscount2').val()) || 0;

    // Raw net amount
    const netAmount = totalAmount + packing + cgst + sgst + igst - discount;

    let finalNetAmount = netAmount;
    let roundOff = 0;

    // Automatic round-off
    if (!isManualRoundOff) {
        finalNetAmount = Math.round(netAmount);
        roundOff = finalNetAmount - netAmount;
    }

    $('#NumRoundOff').val(roundOff.toFixed(2));
    $('#NumNetAmount').val(finalNetAmount.toFixed(2));
}
//------Total Pack Amount---------
function calculatePacking() {

    const totalAmount = getNumber('#NumAmount');
    const packPer = getNumber('#NumPacking1');

    if (packPer !== 0) {
        const packingAmount = totalAmount * (packPer / 100);
        $('#NumPacking2').val(packingAmount.toFixed(2));
    } else {
        $('#NumPacking1').val('');
        $('#NumPacking2').val('');
    }
}
//------Total Disc Amount---------
function calculateDiscount() {

    const totalAmount = getNumber('#NumAmount');
    const discountPer = getNumber('#NumDiscount1');

    if (discountPer !== 0) {
        const discountAmount = totalAmount * (discountPer / 100);
        $('#NumDiscount2').val(discountAmount.toFixed(2));
    } else {
        $('#NumDiscount1').val('');
        $('#NumDiscount2').val('');
    }
}
//------MAIN RECALCULATION---------
function recalculateTotals(options = {}) {
    const {
        calculatePackingAmount = false,
        calculateDiscountAmount = false,
        calculateGridTotal = false,
        manualRoundOff = false
    } = options;

    if (calculatePackingAmount) {
        calculatePacking();
        calTotalWithPackAmt();
    }

    if (calculateDiscountAmount) {
        calculateDiscount();
        calTotalWithDiscount();
    }

    if (calculateGridTotal) {
        gridTotal();
    }

    calculateNetAmount(manualRoundOff);
}
//------Grid Pack Amount---------
function calTotalWithPackAmt() {

    const packAmt = parseFloat($('#NumPacking2').val()) || 0;
    const totalAmt = parseFloat($('#NumAmount').val()) || 0;
    const packPer = parseFloat($('#NumPacking1').val()) || 0;

    if (totalAmt === 0) return;

    $('#tblItemrecord tbody tr').each(function () {
        const rowAmount = parseFloat($(this).find('.amount').val()) || 0;
        const rowPackAmt = (rowAmount / totalAmt) * packAmt;
        $(this).find('.pack-per').val(packPer);
        $(this).find('.pack-amt').val(rowPackAmt.toFixed(2));
    });
}
//------Grid Disc Amount---------
function calTotalWithDiscount() {
    const discAmt = parseFloat($('#NumDiscount2').val()) || 0;
    const totalAmt = parseFloat($('#NumAmount').val()) || 0;
    const discPer = parseFloat($('#NumDiscount1').val()) || 0;

    if (totalAmt === 0) return;

    $('#tblItemrecord tbody tr').each(function () {
        const rowAmount = parseFloat($(this).find('.amount').val()) || 0;
        const rowDiscAmt = (rowAmount / totalAmt) * discAmt;
        $(this).find('.disc-per').val(discPer);
        $(this).find('.disc-amt').val(rowDiscAmt.toFixed(2));
    });
}
//------Grid Total---------
function gridTotal() {
    let totalNos = 0;
    let totalRecdQty = 0;
    let totalBillQty = 0;
    let totalAmount = 0;
    let totalPackAmt = 0;
    let totalDiscAmt = 0;
    let totalCGST = 0;
    let totalSGST = 0;
    let totalIGST = 0;

    $('#tblItemrecord tbody tr').each(function () {
        totalNos += parseFloat($(this).find('.nos').val()) || 0;
        totalRecdQty += parseFloat($(this).find('.recd-qty').val()) || 0;
        totalBillQty += parseFloat($(this).find('.inv-qty').val()) || 0;
        totalAmount += parseFloat($(this).find('.amount').val()) || 0;
        totalPackAmt += parseFloat($(this).find('.pack-amt').val()) || 0;
        totalDiscAmt += parseFloat($(this).find('.disc-amt').val()) || 0;
        totalCGST += parseFloat($(this).find('.cgst-amt').val()) || 0;
        totalSGST += parseFloat($(this).find('.sgst-amt').val()) || 0;
        totalIGST += parseFloat($(this).find('.igst-amt').val()) || 0;
    });

    // Set totals
    $('#NumTotalnos').val(totalNos);
    $('#NumReceivedQty').val(totalRecdQty);
    $('#NumBillQty').val(totalBillQty);
    $('#NumAmount').val(totalAmount.toFixed(2));
    $('#NumPacking2').val(totalPackAmt.toFixed(2));
    $('#NumDiscount2').val(totalDiscAmt.toFixed(2));
    $('#NumCGST2').val(totalCGST.toFixed(2));
    $('#NumSGST2').val(totalSGST.toFixed(2));
    $('#NumIGST2').val(totalIGST.toFixed(2));
}
//------MAIN GRID RECALCULATION---------
function recalculateRow(row) {
    const $row = $(row);
    const amount = parseFloat($row.find('.amount').val()) || 0;
    const packPer = parseFloat($row.find('.pack-per').val()) || 0;
    const discPer = parseFloat($row.find('.disc-per').val()) || 0;

    // Packing Amount
    $row.find('.pack-amt').val((amount * packPer / 100).toFixed(2)).trigger('change');
    // Discount Amount
    $row.find('.disc-amt').val((amount * discPer / 100).toFixed(2)).trigger('change');
}
//------Calc Tax---------
function calTax(row) {

    const $row = $(row);
    const amount = parseFloat($row.find('.amount').val()) || 0;
    const cgstPer = parseFloat($row.find('.cgst-per').val()) || 0;
    const sgstPer = parseFloat($row.find('.sgst-per').val()) || 0;
    const igstPer = parseFloat($row.find('.igst-per').val()) || 0;

    $row.find('.cgst-amt').val((amount * cgstPer / 100).toFixed(2)).trigger('change');
    $row.find('.sgst-amt').val((amount * sgstPer / 100).toFixed(2)).trigger('change');
    $row.find('.igst-amt').val((amount * igstPer / 100).toFixed(2)).trigger('change');
}
//------Calc Helper---------
function getNumber(selector) {
    const value = parseFloat($(selector).val());
    return Number.isFinite(value) ? value : 0;
}
function chkGridPackPercent() {
    return $('#tblItemrecord tbody .pack-per').toArray()
        .some(el => (parseFloat($(el).val()) || 0) !== 0);
}
function chkGridDiscPercent() {
    return $('#tblItemrecord tbody .disc-per').toArray()
        .some(el => (parseFloat($(el).val()) || 0) !== 0);
}

//=============Validate Data=================
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocno").val()
    };
    try {
        const response = await fetch('/DeliveryChallanStore/CheckValidDate', {
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
async function validate() {
    let isValid = true;
    const billToLabel = $('#lblbillTo').text();
    if (!validateRequiredField('#ddlDocType', 'Doc Type') || !validateRequiredField('#NumDocno', 'Doc Number')
        || !validateRequiredField('#DtDocDate', 'Doc Date') || !validateRequiredField('#ddlbillto', `${billToLabel}`)
        || !validateRequiredField('#ddlPOSCity', 'POS City') || !validateRequiredField('#ddlNatureWork', 'Nature of Work')
        || !validateRequiredField('#ddlshiptoSD', 'Ship To') || !validateRequiredField('#ddlMovtype', 'Movement Type')
        || !validateRequiredField('#ddlTranType', 'Transaction Type')) {
        isValid = false;
        return false;
    }

    //------------- Validate VDate ------------
    const isValidVDate = await checkValidDate();
    if (!isValidVDate) {
        isValid = false;
        return false;
    }

    // Bill Date validate 
    if (!$('#chkBillDate').is(':checked')) {
        setInvalid($('#DtBillDate'), "Bill Date is required!")
        isValid = false;
        return false;
    }

    if (!validateRequiredField('#DtBillDate', 'Bill Date')) {
        isValid = false;
        return false;
    }

    //Validate GST And Address
    function getValue(selector) {
        return $.trim($(selector).val() || "");
    }

    var billAddress = [
        getValue("#Txtaddl1"),
        getValue("#Txtaddl2"),
        getValue("#Txtaddl3"),
        getValue("#ddlBillCity"),
        getValue("#NumPincode")
    ].filter(Boolean).join(" ").toUpperCase();

    var shipAddress = [
        getValue("#Txtaddl1SD"),
        getValue("#Txtaddl2SD"),
        getValue("#Txtaddl3SD"),
        getValue("#ddlBillCitySD"),
        getValue("#NumPincodeSD")
    ].filter(Boolean).join(" ").toUpperCase();

    var billGST = getValue("#NumBillGST").toUpperCase();
    var shipGST = getValue("#NumBillGSTSD").toUpperCase();

    var tranType = getValue("#ddlTranType");

    if (tranType.toUpperCase() === "REGULAR") {

        if (billGST !== shipGST) {
            showToast("Mismatch GST Number in case of 'Regular' Transaction Type. Please Recheck Again.", {type:"warning"});
            //isValid = false
            //return false;
        }

        if (billAddress !== shipAddress) {
            showToast("Mismatch Billing and Shipping Addresses in case of 'Regular' Transaction Type. Please Recheck Again.", {type:"warning"});
            //isValid = false
            //return false;
        }
    }

    if (tranType.toUpperCase() === "BILL TO-SHIP TO") {

        if (billAddress !== shipAddress) {
            showToast("Please Check Billing and Shipping Addresses in case of 'Bill To-Ship To' Transaction Type. Recheck Again.", {type:"warning"});
            //isValid = false
            //return false;
        }
    }

    if ($('#ddlMovtype').val() === 'Outward') {

        const natureOfJW = $('#ddlNatureWork').val();

        if (natureOfJW === 'Jobwork' || natureOfJW === 'Repairing' || natureOfJW === 'Replacement' || natureOfJW === 'Return') {

            // Responsible Person required
            if (!validateRequiredField('#ddlResponsiblePerson', 'Responsible Person Name')) {
                isValid = false;
                return false;
            }

            // Expected Return Date checkbox must be checked
            if (!$('#chkExpectedReturnDate').is(':checked')) {
                setInvalid($('#DtExpectedReturnDate'), 'Expected Return Date is required.');
                isValid = false;
                return false;
            }

            // Expected Return Date value required
            if (!validateRequiredField('#DtExpectedReturnDate', 'Expected Return Date')) {
                isValid = false;
                return false;
            }
        }
    }

    const $rows = $('#tblItemrecord tbody tr');

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

    $('#tblItemrecord tbody tr').each(function () {
        const currentSelect = $(this).find('.item-name')[0];

         //Duplicate
        if (checkDuplicateItems(currentSelect)) {
            console.log("DUPLICATE FOUND");
            isValid = false;
            return false;
        }
    })

    return isValid;
}
async function validateWB() {

    var items = [];

    $('#tblItemrecord tbody tr').each(function () {

        var row = $(this);

        items.push({
            ItemCode: row.find('.item-name').val(),
            ItemName: row.find('.item-name option:selected').text(),
            WbType: row.find('.ref-type').val(),
            WbNo: row.find('.ref-no').val(),
            Quantity: parseFloat(row.find('.inv-qty').val()) || 0
        });
    });

    const response = await CheckWB($("#ddlDocType").val(), items);

    if (!response.success) {
        //showToast(response.message, { type: "warning", duration:5000 });
        await Swal.fire({
            icon: 'warning',
            text: response.message,
            confirmButtonText: 'OK'
        });
        return false;
    }

    return true;
}
function CheckWB(vType, items) {
    return $.ajax({
        url: '/DeliveryChallanStore/CheckWeight',
        type: 'POST',
        contentType: 'application/json; charset=utf-8',
        dataType: 'json',
        data: JSON.stringify({VType: vType, Items: items})
    });
}
function checkDuplicateItems(currentSelect) {

    const value = currentSelect.value;
    if (!value) return false;

    let duplicate = false;

    document.querySelectorAll('#tblItemrecord .item-name').forEach(el => {
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
//=============Collect Data For Save & Update==========
function CollectHeaderData() {

    const items = CollectFooterData();
    const request = {
        V_TYPE: ($('#ddlDocType').val() || "").trim(),
        V_NO: parseInt($('#NumDocno').val()) || 0,
        V_DATE: $('#DtDocDate').val() || "",
        BILL_NO: ($('#TxtBillDocID').val() || "").trim(),
        BILL_DATE: getOptionalDate('#chkBillDate', '#DtBillDate'),
        BILL_CODE: parseInt($('#ddlbillto').val()) || 0,
        BILL_NAME: $('#ddlbillto').val() === "" ? "" : ($("#ddlbillto option:selected").text() || "").trim(),
        BILL_ADD1: ($('#Txtaddl1').val() || "").trim(),
        BILL_ADD2: ($('#Txtaddl2').val() || "").trim(),
        BILL_ADD3: ($('#Txtaddl3').val() || "").trim(),
        BILL_CITY: parseInt($('#ddlBillCity').val()) || 0,
        BILL_GST: ($('#NumBillGST').val() || "").trim(),
        BILL_PINCODE: ($('#NumPincode').val() || "").trim(),
        SHIP_CODE: parseInt($('#ddlshiptoSD').val()) || 0,
        SHIP_NAME: $('#ddlshiptoSD').val() === "" ? "" : ($('#ddlshiptoSD option:selected').text() || "").trim(),
        SHIP_ADD1: ($('#Txtaddl1SD').val() || "").trim(),
        SHIP_ADD2: ($('#Txtaddl2SD').val() || "").trim(),
        SHIP_ADD3: ($('#Txtaddl3SD').val() || "").trim(),
        SHIP_CITY: parseInt($('#ddlBillCitySD').val()) || 0,
        SHIP_GST: ($('#NumBillGSTSD').val() || "").trim(),
        SHIP_PINCODE: ($('#NumPincodeSD').val() || "").trim(),
        CITY_CODE: parseInt($('#ddlPOSCity').val()) || 0,
        DOC_TYPE: ($('#ddlRefBillType').val() || "").trim(),
        DOC_NO: parseInt($('#ddlRefBillNo').val()) || 0,
        DOC_DATE: getOptionalDate('#chkRefDate', '#DtRefDate'),
        DOC_NAME: $('#ddlRefBillType').val() === "" ? "" : ($('#ddlRefBillType option:selected').text() || "").trim(),
        TOT_NOS: parseFloat($('#NumTotalnos').val()) || 0,
        TOT_GROSS: parseFloat($('#NumAmount').val()) || 0,
        TOT_QTY: parseFloat($('#NumBillQty').val()) || 0,
        AMOUNT: parseFloat($('#NumAmount').val()) || 0,
        DISC_PER: parseFloat($('#NumDiscount1').val()) || 0,
        DISC_AMT: parseFloat($('#NumDiscount2').val()) || 0,
        PACK_PER: parseFloat($('#NumPacking1').val()) || 0,
        PACK_AMT: parseFloat($('#NumPacking2').val()) || 0,
        SGST_PER: parseFloat($('#NumSGST1').val()) || 0,
        IGST_PER: parseFloat($('#NumIGST1').val()) || 0,
        CGST_PER: parseFloat($('#NumCGST1').val()) || 0,
        CGST_AMT: parseFloat($('#NumCGST2').val()) || 0,
        SGST_AMT: parseFloat($('#NumSGST2').val()) || 0,
        IGST_AMT: parseFloat($('#NumIGST2').val()) || 0,
        ROUNDOFF: parseFloat($('#NumRoundOff').val()) || 0,
        NAMOUNT: parseFloat($('#NumNetAmount').val()) || 0,
        GR_NO: ($('#TxtGRNo').val() || "").toString().trim(),
        GR_DATE: getOptionalDate('#chkGRDate', '#DtGRDate'),
        TRANSPORT_NAME: $('#ddltransportname').val() === "" ? "" : ($('#ddltransportname option:selected').text() || "").trim(),
        TRANSPORT_CODE: parseInt($('#ddltransportname').val()) || 0,
        TRUCK_NO: ($('#TxtVehicleNo').val() || "").trim(),
        STATION_NAME: $('#ddlBillCity').val() === "" ? "" : ($('#ddlBillCity option:selected').text() || "").trim(),
        STATION_CODE: parseInt($('#ddlBillCity').val()) || 0,
        CONSG_ADD_ID: parseInt($('#ddladdl1a').val()) || 0,
        PARTY_ADD_ID: parseInt($('#ddladdl1aSD').val()) || 0,
        REMARK: ($('#txtreason').val() || "").trim(),
        NATURE_OFWORK: ($('#ddlNatureWork').val() || "").trim(),
        TRAN_TYPE: ($('#ddlTranType').val() || "").trim(),
        MOVE_TYPE: ($('#ddlMovtype').val() || "").trim(),
        DESP_ADDRESS: ($('#txtdispatchfrom').val() || "").trim(),
        JW_NATURE: ($('#TxtOtherWork').val() || "").trim(),
        EWB_NO: ($('#TxtEwaybillno').val() || "").trim(),
        EMP_CODE: parseInt($('#ddlResponsiblePerson').val()) || 0,
        RET_DATE: getOptionalDate('#chkExpectedReturnDate', '#DtExpectedReturnDate'),
        DESP_FROMPARTY: parseInt($('#ddldespparty').val()) || 0,
        DESP_TOPARTY: parseInt($('#ddldesppartySD').val()) || 0,
        DESP_FROMGST: ($('#ddldespgst').val() || "").trim(),
        DESP_TOGST: ($('#ddldespgstSD').val() || "").trim(),
        DESP_FROMCITY: parseInt($('#ddldesploc').val()) || 0,
        DESP_TOPCITY: parseInt($('#ddldesplocSD').val()) || 0,
        TPT_DISTANCE: parseInt($('#NumDistance').val()) || 0,

        ACTION: (rowId && !isNaN(rowId)) ? "UPDATE" : "INSERT",

        items : items

    }

    return request;
}
function CollectFooterData() {
    const items = [];

    $('#tblItemrecord tbody tr').each(function (index) {

        const $row = $(this);
        const item = $row.find('.item-name');

        const data = {
            ITEM_CODE: parseInt(item.val()) || 0,
            ITEM_NAME: item[0].selectedIndex >= 0
                ? item[0].options[item[0].selectedIndex].text.trim()
                : "",
            ITEM_UNIT: $row.find('.uom-name').val() || null,
            HSN_CODE: $row.find('.hsn-code').val() || null,

            NOS: parseFloat($row.find('.nos').val()) || 0,
            GROSS: parseFloat($row.find('.recd-qty').val()) || 0,

            QTY: parseFloat($row.find('.inv-qty').val()) || 0,
            RATE: parseFloat($row.find('.rate').val()) || 0,
            AMOUNT: parseFloat($row.find('.amount').val()) || 0,

            DISC_PER: parseFloat($row.find('.disc-per').val()) || 0,
            DISC_AMT: parseFloat($row.find('.disc-amt').val()) || 0,

            CGST_PER: parseFloat($row.find('.cgst-per').val()) || 0,
            CGST_AMT: parseFloat($row.find('.cgst-amt').val()) || 0,

            SGST_PER: parseFloat($row.find('.sgst-per').val()) || 0,
            SGST_AMT: parseFloat($row.find('.sgst-amt').val()) || 0,

            IGST_PER: parseFloat($row.find('.igst-per').val()) || 0,
            IGST_AMT: parseFloat($row.find('.igst-amt').val()) || 0,

            PACK_PER: parseFloat($row.find('.pack-per').val()) || 0,
            PACK_AMT: parseFloat($row.find('.pack-amt').val()) || 0,

            WB_TYPE: $row.find('.ref-type').val() || null,
            WB_NO: parseInt($row.find('.ref-no').val()) || null,

            TAX_CODE: parseInt($row.find('.tax-code').val()) || null
        };

        items.push(data);
    });

    return items;
}
function SaveDeliveryChallanDetails() {
    const request = CollectHeaderData();
    console.log("Request Data For Save: ", request);
    $.ajax({
        url: '/DeliveryChallanStore/SaveDeliveryChallanStore',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(request),
        success: function (res) {
            if (res.success) {
                showToast("Saved Successfully!", { type: "success" });
                const vType = $('#ddlDocType').val();
                setFormReadonly();
                isReadOnly = true;
                setTimeout(() => window.location.href = '/DeliveryChallanStore/Index?id=' + encodeURIComponent($('#NumDocno').val()) + '&vtype=' + encodeURIComponent($('#ddlDocType').val()) + '&readOnly=true', 4000);
                if (vType === "DCHL") {
                    if (isReadOnly) {
                        const vNo = $('#NumDocno').val();
                        checkApprovalStatus(vType, vNo, DBTableName);
                    }
                }
                //else {
                //    setTimeout(() => window.location.href = '/DeliveryChallanStoreList/Index', 1000);
                //}
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

//=============EDIT==========
async function GetDataById() {
    try {
        const res = await $.ajax({
            url: '/DeliveryChallanStore/GetDataById',
            type: 'GET',
            data: {
                docId: rowId,
                docType: rowIdVType
            },
            dataType: 'JSON'
        });

        if (!res.success) {
            showToast(res.message, { type: "warning" });
            return;
        }

        isLoadById = true;

        console.log("Get By Id: ", res.data);

        await bindDataById(res.data);
    }
    catch (xhr) {
        console.error(xhr);
        showToast(
            'An error occurred while fetching data by Id!',
            { type: "error" }
        );
    }
    finally {
        isLoadById = false;
    }
}
async function bindDataById(data) {

    if (!data) return;
    console.log("Binding Edit Data:", data);

    // HEADER
    $('#ddlDocType').val(data.v_TYPE || '').prop('disabled', true);
    $('#NumDocno').val(data.v_NO ?? '');
    $('#DtDocDate').val(formatDate(data.v_DATE));
    $('#ddlRefBillType').val(data.doC_TYPE || '').trigger('change');
    if (data.doC_TYPE) {
        await loadDdl('refno', 'ddlRefBillNo', data.doC_TYPE);
        $('#ddlRefBillNo').val(data.doC_NO ?? '').trigger('change');
    }
    setDateControl(data.doC_DATE, '#DtRefDate', '#chkRefDate');
    $('#TxtBillDocID').val(data.bilL_NO || '');
    setDateControl(data.bilL_DATE, '#DtBillDate', '#chkBillDate');
    // BILL TO
    $('#ddlbillto').val(data.bilL_CODE ?? '').trigger('change');
    $('#Txtaddl1').val(data.bilL_ADD1 || '');
    $('#Txtaddl2').val(data.bilL_ADD2 || '');
    $('#Txtaddl3').val(data.bilL_ADD3 || '');
    $('#NumBillGST').val(data.bilL_GST || '');
    $('#NumPincode').val(data.bilL_PINCODE || '');

    await loadDdl('city', '#ddlBillCity');
    $('#ddlBillCity').val(data.bilL_CITY ?? '').trigger('change');

    await loadDdl('address', '#ddladdl1a', '', {shipFromCode: data.bilL_CODE});
    $('#ddladdl1a').val(data.consG_ADD_ID).trigger('change');

    $('#ddlshiptoSD').val(data.shiP_CODE).trigger('change');
    
    $('#Txtaddl1SD').val(data.shiP_ADD1 || '');
    $('#Txtaddl2SD').val(data.shiP_ADD2 || '');
    $('#Txtaddl3SD').val(data.shiP_ADD3 || '');
    $('#NumBillGSTSD').val(data.shiP_GST || '');
    $('#NumPincodeSD').val(data.shiP_PINCODE || '');

    await loadDdl('city', '#ddlBillCitySD');
    $('#ddlBillCitySD').val(data.shiP_CITY ?? '');

    await loadDdl('address', '#ddladdl1aSD', '', {shipFromCode: data.shiP_CODE});
    $('#ddladdl1aSD').val(data.partY_ADD_ID).trigger('change');;
    
    await loadDdl('city', '#ddlPOSCity');
    $('#ddlPOSCity').val(data.citY_CODE ?? '');

    $('#ddlTranType').val(data.traN_TYPE || '');
    $('#ddlMovtype').val(data.movE_TYPE || '').trigger('change');
    $('#ddlNatureWork').val(data.naturE_OFWORK || '');
    $('#TxtOtherWork').val(data.jW_NATURE || '');
    $('#ddlResponsiblePerson').val(data.emP_CODE ?? '').trigger('change');
    setDateControl(data.reT_DATE, '#DtExpectedReturnDate', '#chkExpectedReturnDate');
    $('#ddldespparty').val(data.desP_FROMPARTY ?? '').trigger('change');
    $('#ddldespgst').val(data.desP_FROMGST || '');

    $('#ddldesppartySD').val(data.desP_TOPARTY ?? '').trigger('change');
    $('#ddldespgstSD').val(data.desP_TOGST || '');

    $('#ddldesploc').val(data.desP_FROMCITY ?? '').trigger('change');
    $('#ddldesplocSD').val(data.desP_TOPCITY ?? '').trigger('change');

    $('#txtdispatchfrom').val(data.desP_ADDRESS || '');
    $('#TxtEwaybillno').val(data.ewB_NO || '');

    $('#ddltransportname').val(data.transporT_CODE ?? '').trigger('change');

    $('#TxtVehicleNo').val(data.trucK_NO || '');
    $('#TxtGRNo').val(data.gR_NO || '');
    setDateControl(data.gR_DATE, '#DtGRDate', '#chkGRDate');
    $('#NumDistance').val(data.tpT_DISTANCE ?? '');
    $('#txtreason').val(data.remark || '');


    // TOTALS
    $('#NumTotalnos').val(data.toT_NOS ?? 0);
    $('#NumReceivedQty').val(data.toT_GROSS ?? 0);
    $('#NumBillQty').val(data.toT_QTY ?? 0);
    $('#NumAmount').val(data.amount ?? 0);
    $('#NumDiscount1').val(data.disC_PER ?? 0);
    $('#NumDiscount2').val(data.disC_AMT ?? 0);
    $('#NumPacking1').val(data.pacK_PER ?? 0);
    $('#NumPacking2').val(data.pacK_AMT ?? 0);
    $('#NumCGST1').val(data.cgsT_PER ?? 0);
    $('#NumCGST2').val(data.cgsT_AMT ?? 0);
    $('#NumSGST1').val(data.sgsT_PER ?? 0);
    $('#NumSGST2').val(data.sgsT_AMT ?? 0);
    $('#NumIGST1').val(data.igsT_PER ?? 0);
    $('#NumIGST2').val(data.igsT_AMT ?? 0);
    $('#NumRoundOff').val(data.roundoff ?? 0);
    $('#NumNetAmount').val(data.namount ?? 0);

    // Footer
    const items = data.items || [];

    $('#tblItemrecord tbody').empty();

    if (items.length > 0) {
        for (const item of items) {
            await addNewRowBelow(item);
        }
    }
    else {
        await addNewRowBelow();
    }
}
function setFormReadonly() {
    const form = $('#DeliveryChallanStoreform');
    form.addClass('erppage-readonly');
    form.find('input[type = "checkbox"]').prop('disabled', true);
    $('#btn_save').hide();
    $('#btn__load, #btn__loadqty').prop('disabled', true).css({
        'pointer-events': 'none',
        //'opacity': '0.65',      
        'cursor': 'not-allowed'
    });
    $('#tblItemrecord .btn-delete-action, #tblItemrecord .btn-add-action')
        .prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
}

//=============APPROVAL==========
$(document).on('click', '#btn_Sendapproval', function () {
    var FromName = window.location.pathname.split('/')[1];
    $.ajax({
        url: '/Approval/CheckPendingUser',
        type: 'POST',
        data: {
            vNo: $('#NumDocno').val(),
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
                    DocNo: $('#NumDocno').val(),
                    TableName: DBTableName
                });
                return;
            }
            // Approval_Code != 8
            OpenSendForApprovalModal({
                DocType: rowIdVType,
                DocNo: $('#NumDocno').val(),
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

//=============REPORT===========
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/DeliveryChallanStore/GetGlobalValues",
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
            companyGSTIN = d.companyGst;
            companyPhone = d.phone;
            companyEmail = d.email;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}
function DeliveryChallanReport() {

    var reportName = "store_challan";
    var vType = ($("#ddlDocType").val() || "").trim();
    var VNO = ($("#NumDocno").val() || "").trim();

    // Crystal Report Selection Formula
    var docId = vType + VNO;

    var formula =
        "{DC_NOTE1.DOC_ID} = '" + docId + "'" +
        " AND {DC_NOTE1.COMP_CODE} = " + compCode +
        " AND {DC_NOTE1.BRANCH_CODE} = " + branchCode +
        " AND {DC_NOTE1.YEAR_CODE} = " + yearCode;

    // Select report according to company
    if (compCode == 7) {
        reportName = "store_challanK";
    } else {
        reportName = "store_challan";
    }

    // RPTNAME1 according to voucher type
    var rptName1 = "";

    var vType = ($("#ddlDocType").val() || "").trim();

    if (vType == "DCHL") {
        rptName1 = "(Store)";
    }
    else if (vType == "DCIC") {
        rptName1 = "(Internal)";
    }
    else if (vType == "DCOT") {
        rptName1 = "(Other than Jobwork)";
    }

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,

        Parameters: {
            RPTNAME: "Delivery Challan",
            RPTNAME1: rptName1,

            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2,
            GST: "GSTIN:" + companyGSTIN,
            comp_phone: "Phone:" + companyPhone,
            EMAIL: "Email:" + companyEmail
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
        xhrFields: {responseType: 'blob'},

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