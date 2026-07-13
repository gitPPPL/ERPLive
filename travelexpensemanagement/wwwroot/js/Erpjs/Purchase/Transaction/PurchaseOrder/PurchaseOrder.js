
async function LoadDropdown() {
    try {
        await Promise.all([
            DDlPartyList(),
            DDlShipPartyList(),
            GetCurrencyList(),
            GetPayTermList(),
            DDLCityMast(),
            GetPlaceList(),
            DDLTxtCity1SDt(),
            loadItemNameDropdown(),
            loadMakeDropdown(),
            loadUnitDropdown(),
            loadPlaceDropdown(),
            loadDepartmentDropdown(),
            loadTaxTypeDropdown()
        ]);

    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function GetShipFromList(ShipCode) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPartyList',
            type: 'GET',
            dataType: 'json'
        });

        console.log("GetShipFromList", response);

    } catch (error) {
        console.log(error);
        toastr.error("Party Load failed");
    }
}

async function GetPartyList(selectedValue = null) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPartyList',
            type: 'GET',
            data: { selectedValue: selectedValue },
            dataType: 'json'
        });

        console.log("GetPartyList", response);

    } catch (error) {
        console.log(error);
        toastr.error("Party Load failed");
    }
}

async function GetPayTermList(selectedValue = null) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPayTermList',
            type: 'GET',
            dataType: 'json'
        });

        if (response && response.status) {

            const $dropdown = $('#ddlPaymentTerm');
            $dropdown.empty();

            $dropdown.append('<option value="">- Select Payment Term -</option>');

            $.each(response.data, function (i, item) {
                $dropdown.append(new Option(item.NAME, item.CODE));
            });

            $dropdown.val(selectedValue || '').trigger('change');

        } else {
            toastr.error("Payment term load failed");
        }

    } catch (error) {
        console.log(error);
        toastr.error("Payment term load failed");
    }
}

async function DDLCityMast() {
    try {
        const res = await fetch('/PurchaseOrder/DDLCityMast');
        const data = await res.json();
        const ddl = $('#TxtCity1PD');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function DDlPartyList() {
    try {
        const res = await fetch('/PurchaseOrder/DDlPartyList');
        const data = await res.json();

        const ddl = $('#ddlPartyName');

        // clear old options
        ddl.empty();

        // default option
        ddl.append('<option value=""></option>');


        data.forEach(item => {
            ddl.append(new Option(item.text, item.value));
        });

        // initialize / refresh select2
        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.trigger('change'); // refresh
        } else {
            ddl.select2({
                placeholder: "-- Select Party Name --",
                allowClear: true,
                width: '100%'
            });
        }

    } catch (error) {
        console.error("Error loading Party:", error);
    }
}

async function DDlShipPartyList() {
    try {
        const res = await fetch('/PurchaseOrder/DDlPartyList');
        const data = await res.json();

        const ddl = $('#ddlShipFrom');

        // clear old options
        ddl.empty();

        // default option
        ddl.append('<option value=""></option>');

        // add new options
        data.forEach(item => {
            ddl.append(new Option(item.text, item.value));
        });

        // initialize / refresh select2
        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.trigger('change'); // refresh
        } else {
            ddl.select2({
                placeholder: "-- Select Ship Party Name --",
                allowClear: true,
                width: '100%'
            });
        }

    } catch (error) {
        console.error("Error loading Party:", error);
    }
}

async function GetCurrencyList() {
    try {
        const res = await fetch('/PurchaseOrder/GetCurrencyMast');
        const data = await res.json();
        const ddl = $('#ddlCurrency');
        ddl.empty().append('<option value="">-- Select Currency --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Currency:", error);
    }
}

async function GetPlaceList() {
    try {
        const res = await fetch('/PurchaseOrder/GetPlaceMast');
        const data = await res.json();
        const ddl = $('#ddlPlace');
        ddl.empty().append('<option value="">-- Select Place --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Place:", error);
    }
}

async function DDLTxtCity1SDt() {
    try {
        const res = await fetch('/PurchaseOrder/DDLCityMast');
        const data = await res.json();
        const ddl = $('#TxtCity1SD');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function GetSaudanoList(partyCd) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetSaudaList',
            method: 'GET',
            data: { partyCd: partyCd }
        });

        const ddl = $('#ddSaudaNo');
        ddl.empty().append('');

        data.forEach(item => {
            ddl.append(`<option value="${item.text}">${item.value}</option>`);
        });

    } catch (error) {
        console.error("Error loading Sauda No:", error);
    }
}

async function loadModificationdata() {

    try {
        const V_NO = $('#txtDocNo').val()?.trim();
        const V_date = $('#dtDocDate').val()?.trim();

        if (!V_NO) {
            toastr.info("Please enter document number");
            return;
        }

        const res = await $.ajax({
            url: '/PurchaseOrder/GetModificationData',
            method: 'GET',
            data: { V_NO: V_NO }
        });

        console.log("Full response:", res);

        if (res.success) {

            const data = res.data || [];

            if (!data || data.length === 0) {
                toastr.info("Purchase Modification Not Found For this Doc No = " + V_NO + " and Doc Date " + V_date);
                return;
            }
            renderPurchaseModification(data);
        }

    } catch (err) {
        console.error("AJAX Error:", err);
        toastr.info("Something went wrong while fetching data");
    }
}
function renderPurchaseModification(data) {
    console.log("Rendering Purchase Modification Data:", data);
    const tbody = $('#modificationList tbody');
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`
            <tr>
                <td colspan="4" class="text-center">No data found</td>
            </tr>
        `);
    } else {
        data.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.saudaNo ?? ''}</td>
                    <td>${item.party ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.rate ?? ''}</td>
                    <td>${item.remark ?? ''}</td>
                    <td>${item.modifyDate ?? ''}</td>
                </tr>
            `);
        });
    }

    const modalElement = document.getElementById('modificationModal');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();
}
function calculateAllRows() {

    if (Calculation === false) return;

    let totalNos = 0,
        totalQty = 0,
        totalRate = 0,
        totalAmount = 0,
        totalPackAmt = 0,
        totalDiscAmt = 0,
        totalCgstAmt = 0,
        totalSgstAmt = 0,
        totalIgstAmt = 0,
        totalVatAmt = 0,
        totalCessAmt = 0,
        totalTcsAmt = 0,
        totalOtherAmt = 0,
        totalNetAmt = 0;

    $('#tblItemRecordPO tbody tr').each(function () {
        const $row = $(this);
        const idx = $row.attr('id').replace('row', '');

        totalNos = parseFloat($(`#TxtNos${idx}`).val()) || 0;
        totalQty = parseFloat($(`#TxtQty${idx}`).val()) || 0;
        totalRate = parseFloat($(`#TxtRate${idx}`).val()) || 0;
        totalAmount = totalQty + totalRate;
        $(`#TxtAmount${idx}`).val(totalAmount);

        PackPer = parseFloat($(`#TxtPackPercent${idx}`).val()) || 0;
        DiscPer = parseFloat($(`#TxtDiscPercent${idx}`).val()) || 0;
        CgstPer = parseFloat($(`#TxtCgstPercent${idx}`).val()) || 0;
        SgstPer = parseFloat($(`#TxtSgstPercent${idx}`).val()) || 0;
        IgstPer = parseFloat($(`#TxtIgstPercent${idx}`).val()) || 0;
        VatPer = parseFloat($(`#TxtVatPercent${idx}`).val()) || 0;
        CessPer = parseFloat($(`#TxtCessPercent${idx}`).val()) || 0;

        CgstAmt = (totalAmount * CgstPer) / 100;
        SgstAmt = (totalAmount * SgstPer) / 100;
        IgstAmt = (totalAmount * IgstPer) / 100;
        DiscAmt = (totalAmount * PackPer) / 100;

        $(`#TxtCgst${idx}`).val(CgstAmt);
        $(`#TxtSgst${idx}`).val(SgstAmt);
        $(`#TxtIgst${idx}`).val(IgstAmt);
        $(`#TxtNetAmt${idx}`).val((totalAmount + CgstAmt + SgstAmt + IgstAmt) - DiscAmt);
    });

    $('#NumTotalNosIt').val(totalNos.toFixed(2));
    $('#NumQtyIt').val(totalQty.toFixed(2));
    $('#NumAmountIt').val(totalAmount.toFixed(2));
    $('#NumPackingAmtIt').val(totalPackAmt.toFixed(2));
    $('#NumDiscAmtIt').val(totalDiscAmt.toFixed(2));
    $('#NumCgstAmtIt').val(totalCgstAmt.toFixed(2));
    $('#NumSgstAmtIt').val(totalSgstAmt.toFixed(2));
    $('#NumIgstAmtIt').val(totalIgstAmt.toFixed(2));
    $('#NumVatAmtIt').val(totalVatAmt.toFixed(2));
    $('#NumCessAmtIt').val(totalCessAmt.toFixed(2));
    $('#NumTCSIt').val(totalTcsAmt.toFixed(2));
    $('#NumOtherAmtIt').val(totalOtherAmt.toFixed(2));
    $('#NumNetAmtIt').val(totalNetAmt.toFixed(2));
}
function calculateAllTotals() {

    if (Calculation === false) return;

    let totalNos = 0,
        totalQty = 0,
        totalAmount = 0,
        totalPackAmt = 0,
        totalDiscAmt = 0,
        totalCgstAmt = 0,
        totalSgstAmt = 0,
        totalIgstAmt = 0,
        totalVatAmt = 0,
        totalCessAmt = 0,
        totalTcsAmt = 0,
        totalOtherAmt = 0,
        totalNetAmt = 0;

    $('#tblItemRecordPO tbody tr').each(function () {
        const $row = $(this);
        const idx = $row.attr('id').replace('row', '');

        totalNos += parseFloat($(`#TxtNos${idx}`).val()) || 0;
        totalQty += parseFloat($(`#TxtQty${idx}`).val()) || 0;
        totalAmount += parseFloat($(`#TxtAmount${idx}`).val()) || 0;
        totalPackAmt += parseFloat($(`#TxtPack${idx}`).val()) || 0;
        totalDiscAmt += parseFloat($(`#TxtDisc${idx}`).val()) || 0;
        totalCgstAmt += parseFloat($(`#TxtCgst${idx}`).val()) || 0;
        totalSgstAmt += parseFloat($(`#TxtSgst${idx}`).val()) || 0;
        totalIgstAmt += parseFloat($(`#TxtIgst${idx}`).val()) || 0;
        totalVatAmt += parseFloat($(`#TxtVat${idx}`).val()) || 0;
        totalCessAmt += parseFloat($(`#TxtCess${idx}`).val()) || 0;
        totalTcsAmt += parseFloat($(`#TxtTcsAmt${idx}`).val()) || 0;
        totalOtherAmt += parseFloat($(`#TxtOthAmt${idx}`).val()) || 0;
        totalOtherAmt += parseFloat($(`#TxtOthAmt2${idx}`).val()) || 0;
        totalNetAmt += parseFloat($(`#TxtNetAmt${idx}`).val()) || 0;
    });

    $('#NumTotalNosIt').val(totalNos.toFixed(2));
    $('#NumQtyIt').val(totalQty.toFixed(2));
    $('#NumAmountIt').val(totalAmount.toFixed(2));
    $('#NumPackingAmtIt').val(totalPackAmt.toFixed(2));
    $('#NumDiscAmtIt').val(totalDiscAmt.toFixed(2));
    $('#NumCgstAmtIt').val(totalCgstAmt.toFixed(2));
    $('#NumSgstAmtIt').val(totalSgstAmt.toFixed(2));
    $('#NumIgstAmtIt').val(totalIgstAmt.toFixed(2));
    $('#NumVatAmtIt').val(totalVatAmt.toFixed(2));
    $('#NumCessAmtIt').val(totalCessAmt.toFixed(2));
    $('#NumTCSIt').val(totalTcsAmt.toFixed(2));
    $('#NumOtherAmtIt').val(totalOtherAmt.toFixed(2));
    $('#NumNetAmtIt').val(totalNetAmt.toFixed(2));
}
function calculateTaxAmounts(rowId) {
    const rate = parseFloat($(`#TxtRate${rowId}`).val()) || 0;
    const qty = parseFloat($(`#TxtQty${rowId}`).val()) || 0;
    const amount = rate * qty;

    const discPer = parseFloat($(`#TxtDiscPercent${rowId}`).val()) || 0;
    const discAmt = (amount * discPer) / 100;
    $(`#TxtDisc${rowId}`).val(discAmt.toFixed(2));

    const packPer = parseFloat($(`#TxtPackPercent${rowId}`).val()) || 0;
    const packAmt = (amount * packPer) / 100;
    $(`#TxtPack${rowId}`).val(packAmt.toFixed(2));

    const taxableAmount = amount - discAmt + packAmt;

    const cgstPer = parseFloat($(`#TxtCgstPercent${rowId}`).val()) || 0;
    const sgstPer = parseFloat($(`#TxtSgstPercent${rowId}`).val()) || 0;
    const igstPer = parseFloat($(`#TxtIgstPercent${rowId}`).val()) || 0;
    const cessPer = parseFloat($(`#TxtCessPercent${rowId}`).val()) || 0;
    const tcsPer = parseFloat($(`#TxtTcsPer${rowId}`).val()) || 0;
    const vatPer = parseFloat($(`#TxtVatPercent${rowId}`).val()) || 0;
    const othPer1 = parseFloat($(`#TxtOthPer${rowId}`).val()) || 0;
    const othPer2 = parseFloat($(`#TxtOthPer2${rowId}`).val()) || 0;

    const cgstAmt = (taxableAmount * cgstPer) / 100;
    const sgstAmt = (taxableAmount * sgstPer) / 100;
    const igstAmt = (taxableAmount * igstPer) / 100;
    const cessAmt = (taxableAmount * cessPer) / 100;
    const tcsAmt = (taxableAmount * tcsPer) / 100;
    const vatAmt = (taxableAmount * vatPer) / 100;
    const othAmt1 = (taxableAmount * othPer1) / 100;
    const othAmt2 = (taxableAmount * othPer2) / 100;

    const totalTax = cgstAmt + sgstAmt + igstAmt + cessAmt + tcsAmt + vatAmt + othAmt1 + othAmt2;
    const netAmt = taxableAmount + totalTax;

    // Update DOM
    $(`#TxtAmount${rowId}`).val(amount.toFixed(2));
    $(`#TxtCgst${rowId}`).val(cgstAmt.toFixed(2));
    $(`#TxtSgst${rowId}`).val(sgstAmt.toFixed(2));
    $(`#TxtIgst${rowId}`).val(igstAmt.toFixed(2));
    $(`#TxtCess${rowId}`).val(cessAmt.toFixed(2));
    $(`#TxtTcsAmt${rowId}`).val(tcsAmt.toFixed(2));
    $(`#TxtVat${rowId}`).val(vatAmt.toFixed(2));
    $(`#TxtOthAmt${rowId}`).val(othAmt1.toFixed(2));
    $(`#TxtOthAmt2${rowId}`).val(othAmt2.toFixed(2));
    $(`#TxtNetAmt${rowId}`).val(netAmt.toFixed(2));

    calculateAllTotals(); // Recalculate footer totals
}

function loadItemNameDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLGridItem',
        method: 'GET',
        success: function (data) {
            itemNameOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });
}
function loadMakeDropdown(ItemCode = 0) {
    $.ajax({
        url: '/PurchaseOrder/DDLGridMake',
        method: 'GET',
        data: {
            ItemCode: ItemCode
        },
        success: function (data) {
            MakeNameOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}
function loadUnitDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLUnitList',
        method: 'GET',
        success: function (data) {
            UnitOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}
function loadPlaceDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLPlaceList',
        method: 'GET',
        success: function (data) {
            PlaceOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}
function loadDepartmentDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLDepartmentList',
        method: 'GET',
        success: function (data) {
            DepartmentOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}
function loadTaxTypeDropdown() {
    $.ajax({
        url: '/PurchaseOrder/DDLTaxTypeList',
        method: 'GET',
        success: function (data) {
            TaxTypeOptions = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        }
    });

}


