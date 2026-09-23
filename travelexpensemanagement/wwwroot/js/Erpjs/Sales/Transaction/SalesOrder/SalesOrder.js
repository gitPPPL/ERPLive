const AllFieldsId = [
    'TxtDocId', 'NumDocNo', 'DtDocDate',
    'ddlSaudaNo',
    'ddlBillTo',
    'TxtAddressL1',
    'ddladdressL1',
    'TxtAddressL2',
    'TxtAddressL3',
    'ddlCity',
    'NumPincode',
    'TxtGSTNo',
    'NumMobile',
    'TxtDelivery',
    'NumIssueNo',
    'ddlShipTo',
    'TxtAddressShipL1',
    'ddlShipaddressL1',
    'TxtAddressShipL2',
    'TxtAddressShipL3',
    'ddlShipCity',
    'NumShipPincode',
    'TxtShipGST',
    'NumContactNo',
    'NumPackingNo',
    'ddlPaymentTerm', 'TxtDeliveryPeriod',
    'ddlStatus'
];

const urlParams = new URLSearchParams(location.search);
const rowId = parseInt(urlParams.get('id'));
const rowIdVType = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';
let DBTableName = "ORDER1";

let isLoadBySaudaNo = false;
let isLoadByIssueNo = false;
let isLoadOnEdit = false;
let taxOptions = [];
let currentDate;
let pubDefSSINSO;

let compCode = "";
let yearCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";
function getQueryParam(param) {
    const urlParams = new URLSearchParams(window.location.search);
    return urlParams.get(param);
}

$(async function () {
    try {
        getGlobalValues();
        $('#btn_createdelivery').prop('disabled', true).css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });

        //await bindHeaderDropdowns();
        //await loadTaxOptions();
        await Promise.all([
            bindHeaderDropdowns(),
            loadTaxOptions()
        ]);

        currentDate = getCurrentDateYMD();
        $('#DtDocDate').val(currentDate);

        if (rowId && !isNaN(rowId)) {
            $('#SaudaDetail').show();
            $('#ddlStatus').prop('disabled', false);
            await GetDocData();
            checkApprovalStatus(rowIdVType, rowId, DBTableName);
        } else {
            $('#btnOrderAdjustmentDetails').hide();
            GetVNo();
            addNewRowBelow();
        }
        
        setEnterKeyFocus(AllFieldsId);
        wireEvents();

        // Focus AFTER everything is initialized
        if (!isReadOnly) {
            setTimeout(() => {
                $('#ddlSaudaNo').focus();
            }, 100);
        }

        wireDeliveryPlanEvents();

        toggleDate();
    } catch (error) {
        console.error(error);
        toastr.error('Failed to load document types: ' + error.message);
    }
});

//=============GENERATE VNO=================
async function GetVNo() {
    try {
        const res = await fetch('/SalesOrder/GetVNo');
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocNo').val(data.v_NO);

        pubDefSSINSO = data.pubDefSSINSO;
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}


//=============DROPDOWN=================
async function bindHeaderDropdowns() {
    return Promise.all([
        bindDropdown("SalesOrder", "saudano", '#ddlSaudaNo', '--Select Sauda--', null, null, false, null, true),
        bindDropdown("SalesOrder", "paymentterm", '#ddlPaymentTerm', '--Select Payment Term--', null, null, false, null, true),
        bindDropdown("SalesOrder", "issueno", '#ddlIssueNo', '--Select Issue--', null, null, false, null, true),
        bindDropdown("SalesOrder", "billtoshipto", '#ddlBillTo', '--Select Bill To--', null, null, false, null, true),
        bindDropdown("SalesOrder", "billtoshipto", '#ddlShipTo', '--Select Ship To--', null, null, false, null, true),
        bindDropdown("SalesOrder", "packingno", '#ddlPackingNo', '--Select Packing--', null, null, false, null, true),
        bindDropdown("SalesOrder", "status", '#ddlStatus', '--Select Status--', null, null, true, null, false),
    ]);
}
function loadItemList(dropdownId, dropdownParent = null) {
    const ddl = $(dropdownId);
    if (!ddl.length) return;
    if (ddl.hasClass('select2-hidden-accessible')) return;

    ddl.select2({
        placeholder: "-- Select --",
        allowClear: true,
        minimumInputLength: 0,
        dropdownParent: dropdownParent ? $(dropdownParent) : undefined,
        ajax: {
            url: '/SalesOrder/GetItemList',
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
async function loadTaxOptions() {
    try {
        const response = await $.ajax({
            url: '/SalesOrder/GetDropdown',
            type: 'GET',
            data: {
                type: 'tax'
            },
            dataType: 'json'
        });

        taxOptions = response || [];

        console.log("Tax Options:", taxOptions);

    } catch (error) {
        console.error("Error loading tax options:", error);
        taxOptions = [];
    }
}
function bindOptions($dropdown, options, placeholder = '--Select--', selectedValue = '') {
    $dropdown.empty();

    $dropdown.append(new Option(placeholder, ''));

    options.forEach(item => {
        const option = new Option(item.text, item.value);

        $(option)
            .attr('data-cgst-per', item.CGST_PER ?? 0)
            .attr('data-sgst-per', item.SGST_PER ?? 0)
            .attr('data-igst-per', item.IGST_PER ?? 0)
            .attr('data-vat-per', item.VAT_PER ?? 0);

        $dropdown.append(option);
    });

    if (selectedValue !== null && selectedValue !== undefined && selectedValue !== '') {
        $dropdown.val(selectedValue).trigger('change');
    }
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


//=============EVENTS========
function wireEvents() {
    //--------- Item Change ---------
    $('#tblSalesOrderEntryModal').on("change", ".item-name", function () {
        if (isLoadBySaudaNo || isLoadByIssueNo || isLoadOnEdit) return;
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
         //Duplicate
        if (checkDuplicateItems(currentSelect, '#tblSalesOrderEntryModal')) return;

        const $row = $(this).closest("tr");
        $row.find(".item-code").val($(this).val() || "");
        //setTimeout(() => {
        //    $row.find(".remarks").trigger("focus");
        //}, 0);
        setTimeout(() => {
            const $fields = $row.find("input:not(:disabled), select:not(:disabled), textarea:not(:disabled)")
                .filter(":visible");

            const currentIndex = $fields.index(this);
            const $nextField = $fields.eq(currentIndex + 1);

            if ($nextField.length) {
                $nextField.focus();
            }
        }, 0);
    });

    //---------- Delete Row Button Click -----------
    $('#tblSalesOrderEntryModal').on('click', '.btn-delete-action', function () {
        const $tbody = $('#tblSalesOrderEntryModal tbody');
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
    $('#tblSalesOrderEntryModal').on('click', '.btn-add-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
         //Duplicate
        if (checkDuplicateItems(currentSelect, '#tblSalesOrderEntryModal')) return;

        await addNewRowBelow();
    });

    //--------- Sauda No Change ---------
    $(document).on('change', '#ddlSaudaNo', async function () {
        const saudaNo = $(this).val();
        try {
            const saudaList = await $.ajax({
                url: '/SalesOrder/GetSaudaDataList',
                type: 'GET',
                dataType: 'json',
                data: { saudaNo: saudaNo }
            });
            console.log("Full response:", saudaList);
            console.log("Data:", saudaList.data);
            await fillISaudaBySaudaNo(saudaList.data);
            await showSaudasDetails(saudaList.data);

            const result = await Swal.fire({
                title: 'Calculate Sauda Rate?',
                text: 'Do you want to calculate Sauda Rate?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Yes',
                cancelButtonText: 'No'
            });

            if (result.isConfirmed) {
                await getSaudaRate();
            }
        } catch (error) {
            console.error('Error loading Sauda data:', error);
        }
    });

    //--------- Bill From List Change --------
    $('#ddlBillTo').on('change', function () {
        if (isLoadOnEdit) return;

        const billFromCode = $(this).val();
        bindDropdown("SalesOrder", "address", '#ddladdressL1', '', null, null, true, billFromCode, false).then(function (data) {
            if (data && data.length > 0) {
                $('#ddladdressL1').val(data[0].value).trigger('change');
            }
        })

        if (isLoadBySaudaNo) return;
        clearControlsOnBillChange();
        $('#ddlShipTo').val(billFromCode).trigger('change');
    });

    //--------- Ship From List Change --------
    $('#ddlShipTo').on('change', function () {
        if (isLoadOnEdit) return;

        const billFromCode = $(this).val();
        bindDropdown("SalesOrder", "address", '#ddlShipaddressL1', '', null, null, true, billFromCode, false).then(function (data) {
            if (data && data.length > 0) {
                $('#ddlShipaddressL1').val(data[0].value).trigger('change');
            }
        });

        if (isLoadBySaudaNo) return;

        $('#TxtAddressShipL1').val('');
        $('#TxtAddressShipL2').val('');
        $('#TxtAddressShipL3').val('');
        $('#NumShipPincode').val('');
        $('#TxtShipGST').val('');
        $('#NumContactNo').val('');
    });

    //---------- Bill Address Change ----------
    bindAddressChange({
        addressSelector: '#ddladdressL1',
        partySelector: '#ddlBillTo',
        add1Selector: '#TxtAddressL1',
        add2Selector: '#TxtAddressL2',
        add3Selector: '#TxtAddressL3',
        gstSelector: '#TxtGSTNo',
        pincodeSelector: '#NumPincode',
        citySelector: '#ddlCity',
        mobileSelector: '#NumMobile',
        isBillChange: true
    });

    //---------- Ship Address Change -----------
    bindAddressChange({
        addressSelector: '#ddlShipaddressL1',
        partySelector: '#ddlShipTo',
        add1Selector: '#TxtAddressShipL1',
        add2Selector: '#TxtAddressShipL2',
        add3Selector: '#TxtAddressShipL3',
        gstSelector: '#TxtShipGST',
        pincodeSelector: '#NumShipPincode',
        citySelector: '#ddlShipCity',
        mobileSelector: '#NumContactNo'
    });

    //---------- Save button click -----------
    $('#btn-save').on('click', async function (e) {
        e.preventDefault();
        try
        {
            const isValid = await Validate();
            if (!isValid) return;
            await SaveData();
        }
        catch (error) {
            console.error("Save Error:", error);
            toastr.error(error.message || error.toString());
        }
    });

    // Tax Code changed
    $('#tblSalesOrderEntryModal tbody').on('change', '.tax-code', function () {

        if (isLoadOnEdit) return;

        const $row = $(this).closest('tr');

        // Get selected tax information
        const selectedTax = $(this).find(':selected');

        const cgstPer = parseFloat(selectedTax.data('cgst-per')) || 0;
        const sgstPer = parseFloat(selectedTax.data('sgst-per')) || 0;
        const igstPer = parseFloat(selectedTax.data('igst-per')) || 0;
        const vatPer = parseFloat(selectedTax.data('vat-per')) || 0;

        // CGST %
        $row.find('.cgst-per').val(cgstPer);
        // SGST %
        $row.find('.sgst-per').val(sgstPer);
        // IGST %
        $row.find('.igst-per').val(igstPer);
        // VAT %
        $row.find('.vat-per').val(vatPer);
    });

    // Load Issue Data Button Click
    $('#btnLoadIssueData').on('click', async function () {
        const issueNo = ($('#ddlIssueNo').val() || '').trim();
        console.log("issue no: ", issueNo);
        await LoadIssueData(issueNo);
    })

    // Load Packing Data Button Click
    $('#btn_loadpacking').on('click', async function () {
        const saudaNo = $('#ddlSaudaNo').val();
        if (!saudaNo) {
            setInvalid($('#ddlSaudaNo'), "Please select Sauda first.");
            return;
        }

        const packingNo = $('#ddlPackingNo').val();

        if (packingNo) {
            const result = await Swal.fire({
                title: 'Question',
                text: 'Do you want to load Packing Data?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Yes',
                cancelButtonText: 'No'
            });
            if (result.isConfirmed) {
                await loadPackingDetail(packingNo);
            }

        }

        else {
            const result = await Swal.fire({
                title: 'Question',
                text: 'Do you want to load only Sale Data?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Yes',
                cancelButtonText: 'No'
            });
            if (result.isConfirmed) {
                await loadPackingDetail('');
            }
        }
    });

    // Grid cell changes
    $('#tblSalesOrderEntryModal tbody').on("change blur", "input, select", async function () {
        if (isLoadOnEdit || isLoadBySaudaNo) return;

        try {
            const $cell = $(this);
            const $row = $cell.closest("tr");
            const rowIndex = $row.index();

            if ($cell.hasClass("nos") || $cell.hasClass("weight")) {
                await CalculateAmt();
            }

            // QTY / RATE
            if ($cell.hasClass("weight") || $cell.hasClass("rate")) {
                calAmount();
                calculateTax(rowIndex, false);
            }

            // PACK / DISCOUNT / CESS
            if ($cell.hasClass("pack-per") || $cell.hasClass("pack-amt") || $cell.hasClass("disc-per") || $cell.hasClass("disc-amt") ||
                $cell.hasClass("cess-per") || $cell.hasClass("cess-amt")) {
                calAmount();
                calculateTax(rowIndex, false);
            }

            // CGST / SGST / IGST
            if ($cell.hasClass("cgst-amt") || $cell.hasClass("sgst-amt") || $cell.hasClass("igst-amt")) {
                calculateTax(rowIndex, false);
            }

            // PURCHASE ORDER PIECE CALCULATION
            if ($cell.hasClass("weight") && $("#ChkCalPCS").prop("checked")) {
                const itemCode = parseInt($row.find(".item-code").val()) || 0;
                if (itemCode > 0) {
                    const packamt = await getPackAmount(itemCode);
                    if (packamt > 0) {
                        const qty = parseFloat($row.find(".weight").val()) || 0;
                        $row.find(".nos").val(qty / packamt);
                    }
                }
            }

            // NOS -> QTY
            if ($cell.hasClass("nos") && $("#ChkCalPCS").prop("checked")) {
                const itemCode = parseInt($row.find(".item-code").val()) || 0;
                if (itemCode > 0) {
                    const packamt = await getPackAmount(itemCode);
                    if (packamt > 0) {
                        const nos = parseFloat($row.find(".nos").val()) || 0;
                        const qty = nos * packamt;
                        $row.find(".weight").val(qty.toFixed(2));
                    }
                }
            }

            // TOTAL AMOUNT
            totalAmt();
        }
        catch (ex) {
            console.error("CellEndEdit()", ex);
            showToast(ex.toString(), { type: "error" });
        }
    }
    );

    // Calc on pcs changes
    $("#ChkCalPCS").on("change", async function () {
        if (isLoadOnEdit) return;

        try {

            const $rows = $("#tblSalesOrderEntryModal tbody tr");
            for (let i = 0; i < $rows.length; i++) {
                calAmount();
                calculateTax(i, true);
            }

            totalAmt();
            CalculateAmt();

        }
        catch (ex) {
            console.error("ChkCalPCS_CheckedChanged()", ex);
        }
    });

}
function clearControlsOnBillChange() {
    //Bill Details
    $('#TxtAddressL1').val('');
    $('#TxtAddressL2').val('');
    $('#TxtAddressL3').val('');
    $('#ddlCity').val('');
    $('#TxtGSTNo').val('');
    $('#NumPincode').val('');
    $('#NumMobile').val('');

    //Ship Details
    $('#TxtAddressShipL1').val('');
    $('#TxtAddressShipL2').val('');
    $('#TxtAddressShipL3').val('');
    $('#ddlShipCity').val('');
    $('#TxtShipGST').val('');
    $('#NumShipPincode').val('');
    $('#NumContactNo').val('');
}
function bindAddressChange({ addressSelector, partySelector, add1Selector, add2Selector, add3Selector, gstSelector, pincodeSelector,
    citySelector, mobileSelector, isBillChange = false }) {
    $(addressSelector).on('change', async function () {

        if (isLoadBySaudaNo) return;

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
                    url: '/SalesOrder/GetPartyAddress',
                    type: 'GET',
                    data: { code, addressId }
                });

                const address = response.data[0];
                console.log("address details on bill/ship change: ", address);
                $(add1Selector).val(address.add1);
                $(add2Selector).val(address.add2);
                $(add3Selector).val(address.add3);
                $(gstSelector).val(address.gstin);
                $(pincodeSelector).val(address.pincode);
                $(mobileSelector).val(address.mobile);

                //await loadDdl("city", citySelector);
                bindDropdown("SalesOrder", "city", citySelector, '--Select city--', address.cityCode, null, false, null, false)
                //$(citySelector).val(address.cityCode).trigger('change');
            } catch (error) {
                showToast('Error loading address', { type: "error" });
            }
        }

    });
}


//=============Add Footer Rows==========
function createRowHtml(data = {}) {
    return `
        <tr class="no-border-input">

            <td><input class="form-control form-control-sm item-code" type="number" value="${data.iteM_CODE || data.ITEM_CODE || ''}" disabled/></td>
            <td><select class="form-control form-control-sm item-name"></select></td>

            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || data.REMARKS || ''}"/></td>
            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos || data.NOS || ''}"/></td>

            <td><input class="form-control form-control-sm weight" type="number" value="${data.qty || data.QTY || ''}" oninput="SetMaxlength(this, 16, 4);"/></td>
            <td><input class="form-control form-control-sm rate" type="number" value="${data.rate || data.RATE || ''}" oninput="SetMaxlength(this, 16, 4);"/></td>
            <td><input class="form-control form-control-sm amount" type="number" value="${data.amount || data.AMOUNT || ''}" oninput="SetMaxlength(this, 16, 4);"/></td>

            <td><input class="form-control form-control-sm pack-per" type="number" value="${data.pacK_PER || data.PACK_PER || ''}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm pack-amt" type="number" value="${data.pacK_AMT || data.PACK_AMT || ''}" oninput="SetMaxlength(this, 16, 4);"/></td>

            <td><input class="form-control form-control-sm disc-per" type="number" value="${data.disC_PER || data.DISC_PER || ''}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm disc-amt" type="number" value="${data.disC_AMT || data.DISC_AMT || ''}" oninput="SetMaxlength(this, 16, 4);"/></td>
            
            <td><select class="form-control form-control-sm tax-code"></select></td>

            <td><input class="form-control form-control-sm cgst-per" type="number" value="${data.cgsT_PER || data.CGST_PER || ''}" disabled oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm cgst-amt" type="number" value="${data.cgsT_AMT || data.CGST_AMT || ''}" 
            ${(data.cgsT_PER || data.CGST_PER) ? '' : 'disabled'} oninput="SetMaxlength(this, 16, 4);"/></td>

            <td><input class="form-control form-control-sm sgst-per" type="number" value="${data.sgsT_PER || data.SGST_PER || ''}" disabled oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm sgst-amt" type="number" value="${data.sgsT_AMT || data.SGST_AMT || ''}" 
            ${(data.sgsT_PER || data.SGST_PER) ? '' : 'disabled'} oninput="SetMaxlength(this, 16, 4);"/></td>

            <td><input class="form-control form-control-sm igst-per" type="number" value="${data.igsT_PER || data.IGST_PER || ''}" disabled oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm igst-amt" type="number" value="${data.igsT_AMT || data.IGST_AMT || ''}" 
            ${(data.igsT_PER || data.IGST_PER) ? '' : 'disabled'} oninput="SetMaxlength(this, 16, 4);"/></td>

            <td><input class="form-control form-control-sm vat-per" type="number" value="${data.vaT_PER || data.VAT_PER || ''}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm vat-amt" type="number" value="${data.vaT_AMT || data.VAT_AMT || ''}" oninput="SetMaxlength(this, 16, 4);"/></td>

            <td><input class="form-control form-control-sm cess-per" type="number" value="${data.cesS_PER || data.CESS_PER || ''}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm cess-amt" type="number" value="${data.cesS_AMT || data.CESS_AMT || ''}" oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><input class="form-control form-control-sm net-amt" type="number" value="${data.neT_AMT || data.NET_AMT || ''}" oninput="SetMaxlength(this, 16, 4);"/></td>
            <td>
                <div class="erppage-datebox">
                    <input type="date"
                           class="erppage-input erppage-dateinput delivery-date" value="${currentDate}">

                    <label class="erppage-checkbox-inside">
                        <input type="checkbox" class="erppage-checkbox-input delivery-date-check" >
                    </label>
                </div>
            </td>

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
    const $previousLastRow = $("#tblSalesOrderEntryModal tbody tr:last");
    $previousLastRow.find(".btn-add-action").remove();

    let rowHtml = createRowHtml(data);
    $("#tblSalesOrderEntryModal tbody").append(rowHtml);

    const $lastRow = $("#tblSalesOrderEntryModal tbody tr:last");

    setDateControl(data.DELIVERY_DATE || data.delDate, $lastRow.find(".delivery-date"), $lastRow.find(".delivery-date-check"))

    //Item Dropdown
    const itemName = $lastRow[0].querySelector(".item-name");
    loadItemList(itemName);
    setSelect2Value(itemName, data.iteM_CODE || data.ITEM_CODE, data.iteM_NAME || data.ITEM_NAME)

    // Tax dropdown
    const $tax = $lastRow.find(".tax-code");
    bindOptions($tax, taxOptions, '--Select Tax--', data.TAX_CODE || data.taX_CODE || '');
    initSelect2($tax);
}


//============Date Controls===========
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
function parseNullableDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    return isNaN(date.getTime()) ? null : date.toISOString();
}
function getOptionalDate(checkboxSelector, dateSelector) {
    return $(checkboxSelector).is(':checked') ? (parseNullableDate($(dateSelector).val()) || null) : null;
}


//==============Sauda Details===============
async function fillISaudaBySaudaNo(datatable) {
    try {
        isLoadBySaudaNo = true;
        const data = datatable[0];
        console.log(data);
        $('#ddlBillTo').val(data.PARTY_CODE).trigger('change');
        $('#TxtAddressL1').val(data.ADD1);
        $('#TxtAddressL2').val(data.ADD2);
        $('#TxtAddressL3').val(data.ADD3);
        $('#TxtGSTNo').val(data.GSTIN);
        $('#NumMobile').val(data.PHONE);
        $('#NumPincode').val(data.PINCODE);

        $('#ddlShipTo').val(data.PARTY_CODE).trigger('change');
        $('#TxtAddressShipL1').val(data.ADD1);
        $('#TxtAddressShipL2').val(data.ADD2);
        $('#TxtAddressShipL3').val(data.ADD3);
        $('#TxtShipGST').val(data.GSTIN);
        $('#NumContactNo').val(data.PHONE);
        $('#NumShipPincode').val(data.PINCODE);

        bindDropdown("SalesOrder", "city", '#ddlCity, #ddlShipCity', '--Select city--', data.CITY_CODE, null, false, null, false)

        $('#ddlPaymentTerm').val(data.PAYTERM_CODE || '').trigger('change');

        $('#TxtDelivery').val(data.PARTY_TO || 'SELF');

        const $tbody = $('#tblSalesOrderEntryModal tbody');
        // Check whether grid already has records
        const hasRecords = $tbody.find('tr').toArray().some(row => {
            const itemCode = $(row).find('.item-code').val();
            return itemCode && itemCode.trim() !== '';
        });


        if (hasRecords) {

            const result = await Swal.fire({
                title: 'Existing Items Found',
                text: 'The grid already contains items. Do you want to remove them and load the Sauda item?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Yes, Replace',
                cancelButtonText: 'No, Keep Existing',
                reverseButtons: true
            });

            // User clicked Cancel / No
            if (!result.isConfirmed) {
                return;
            }
        }

        // Grid is empty OR user confirmed replacement
        $tbody.empty();
        addNewRowBelow(data);
    }
    finally {
        isLoadBySaudaNo = false;
    }
}
async function showSaudasDetails(datas) {
    const data = datas[0];
    console.log("sauda details", data);
    $('#TxtParty').val(data.Party);
    if (data.SHORTNAME !== "" || data.SHORTNAME !== null) {
        $('#TxtItemName').val(data.SHORTNAME ?? '');
    }
    else {
        $('#TxtItemName').val(data.ITEM_NAME ?? '');
    }
    $('#NumQuantity').val(data.QTY);
    $('#NumRate').val(data.RATE);
    $('#TxtTenaCity').val(data.TENACITY_GRP);
}


//==============Issue Details===============
async function LoadIssueData(issueNo) {
    try {
        isLoadByIssueNo = true;
        const issueList = await $.ajax({
            url: '/SalesOrder/GetIssueDetails',
            type: 'GET',
            dataType: 'json',
            data: { issueNo: issueNo }
        });

        console.log("Full response:", issueList);
        console.log("Data:", issueList.data);

        const $tbody = $('#tblSalesOrderEntryModal tbody');
        $tbody.empty();
        if (!issueList?.data?.length) {
            return;
        }

        // Add every item as a row
        issueList.data.forEach(item => {
            addNewRowBelow(item);
        });

    } catch (error) {
        console.error('Error loading Sauda data:', error);
    }
    finally {
        isLoadByIssueNo = false;
    }
}


//==============Packing Details===============
async function loadPackingDetail(packingNo) {
    try {
        const vno = parseInt($('#NumDocNo').val()) || 0;

        const response = await $.ajax({
            url: '/SalesOrder/GetPackingDetail',
            type: 'GET',
            dataType: 'json',
            data: {
                vno: vno,
                packingNo: packingNo
            }
        });

        console.log('Packing Detail Response:', response);

        if (!response.status) {
            toastr.error(response.message || 'Packing data load failed.');
            return;
        }

        const $tbody = $('#tblSalesOrderEntryModal tbody');
        $tbody.empty();
        if (!response?.data?.length) {
            return;
        }

        // Add every item as a row
        response.data.forEach(item => {
            addNewRowBelow(item);
        });

        console.log('Total Qty:', response.totalQty);
        console.log('Total Nos:', response.totalNos);

    } catch (error) {
        console.error('Error loading packing detail:', error);
        toastr.error('Failed to load packing detail.');
    }
}


//==============Linked Orders===============
async function getLinkedOrders() {
    try {
        const saudaNo = $('#ddlSaudaNo option:selected').text();
        const orderList = await $.ajax({
            url: '/SalesOrder/GetLinkedOrders',
            type: 'GET',
            dataType: 'json',
            data: { saudaNo: saudaNo }
        });

        console.log("Full response:", orderList);
        console.log("Data:", orderList.data);
        bindOrderList(orderList.data);

    } catch (error) {
        console.error('Error loading Sauda data:', error);
    }
}
function bindOrderList(data) {

    const $tbody = $('#tblRMPurchaseModal tbody');
    $tbody.empty();

    if (!Array.isArray(data) || data.length === 0) {
        $tbody.append(`
            <tr>
                <td colspan="7" class="text-center">No records found.</td>
            </tr>
        `);
        return;
    }

    data.forEach(item => {

        $tbody.append(`
            <tr>
                <td>${item.OrderNo ?? ''}</td>
                <td>${item.Party ?? ''}</td>
                <td>${item.ItemName ?? ''}</td>
                <td>${item.Quantity ?? 0}</td>
                <td>${item.Rate ?? 0}</td>
                <td>${item.Tenacity ?? ''}</td>
                <td>${item.TenacityGrp ?? ''}</td>
            </tr>
        `);

    });
}


//==============Calculate Rate===============
async function getSaudaRate() {

    // 1. GET HEADER VALUES
    const saudaNo = Number($('#ddlSaudaNo').find("option:selected").text()) || 0;
    const billToCode = Number($('#ddlBillTo').val()) || 0;
    const stationCode = Number($('#ddlCity').val()) || 0;
    const freight = Number($('#NumFreight').val()) || 0;

    // 2. GET GRID ROWS
    const items = [];

    $('#tblSalesOrderEntryModal tr').each(function () {

        const row = $(this);

        const itemCode = Number(row.find('.item-code').val()) || 0;
        const qty = Number(row.find('.weight').val()) || 0;

        // Skip blank rows
        if (itemCode <= 0) return;

        items.push({
            ItemCode: itemCode,
            Rate: Number(row.find('.rate').val()) || 0,
            TaxCode: Number(row.find('.tax-code').val()) || 0,
            //TaxName: row.find('.tax-code option:selected').text() || '',
            CGSTPer: Number(row.find('.cgst-per').val()) || 0,
            SGSTPer: Number(row.find('.sgst-per').val()) || 0,
            IGSTPer: Number(row.find('.igst-per').val()) || 0,

            Qty: qty
        });
    });

    // 3. CALCULATE TOTAL QTY
    let totalQty = 0;

    $('#tblSalesOrderEntryModal tr').each(function () {
        totalQty += Number($(this).find('.weight').val()) || 0;
    });

    // 4. VALIDATION
    if (saudaNo <= 0) {
        showToast('Please enter Sauda No.', { type: "warning" });
        return;
    }

    if (items.length === 0) {
        showToast('Please enter at least one item.', { type: "warning" });
        return;
    }

    // 5. REQUEST OBJECT
    const request = {
        SaudaNo: saudaNo,
        BillToCode: billToCode,
        StationCode: stationCode,
        Freight: freight,
        TotalQty: totalQty,
        Items: items
    };

    try {

        // 6. CALL CONTROLLER
        const response = await fetch('/SalesOrder/GetSaudaRate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(request)
        });

        const result = await response.json();

        // 7. HANDLE ERROR
        if (!response.ok || !result.status) {
            showToast(result.message || 'Unable to get Sauda Rate.', { type: "error" });
            return;
        }

        // 8. UPDATE GRID
        const updatedItems = result.data?.items || [];

        $('#tblSalesOrderEntryModal tr').each(function () {
            const row = $(this);
            const itemCode = Number(row.find('.item-code').val()) || 0;

            if (itemCode <= 0) return;
            const item = updatedItems.find(x => Number(x.itemCode ?? x.ItemCode) === itemCode);

            if (!item) return;

            // Rate
            row.find('.rate').val(Number(item.rate ?? item.Rate ?? 0).toFixed(2));

            // Tax Code
            const taxCode = item.taxCode ?? item.TaxCode ?? 0;
            row.find(".tax-code").val(taxCode).trigger("change");

            // CGST %
            const cgstPer = Number(item.cgstPer ?? item.CGSTPer ?? 0);
            row.find('.cgst-per').val(cgstPer || '').prop('disabled', true);

            // SGST %
            const sgstPer = Number(item.sgstPer ?? item.SGSTPer ?? 0);
            row.find('.sgst-per').val(sgstPer || '').prop('disabled', true);

            // IGST %
            const igstPer = Number(item.igstPer ?? item.IGSTPer ?? 0);
            row.find('.igst-per').val(igstPer || '').prop('disabled', true);

            // Enable/disable tax amount fields
            row.find('.cgst-amt').prop('disabled', cgstPer <= 0);
            row.find('.sgst-amt').prop('disabled', sgstPer <= 0);
            row.find('.igst-amt').prop('disabled', igstPer <= 0);
        });

        // 9. RECALCULATE TOTALS
        totalAmt();
        console.log('Sauda Rate Response:', result);
    }
    catch (error) {
        console.error('GetSaudaRate Error:', error);
        showToast('Something went wrong while getting Sauda Rate.', { type: "error" });
    }
}
function totalAmt() {
    try {
        const $rows = $("#tblSalesOrderEntryModal tbody tr");
        for (let i = 0; i < $rows.length; i++) {
            const $row = $($rows[i]);

            const itemCode = $row.find(".item-code").val();
            if (itemCode !== null && itemCode !== undefined && itemCode !== "") {
                let BasicAmt = 0.0;

                if ($("#ChkCalPCS").prop("checked")) {
                    BasicAmt = parseFloat($row.find(".nos").val()) || 0;
                    BasicAmt = BasicAmt * (parseFloat($row.find(".rate").val()) || 0);
                } else {
                    BasicAmt = parseFloat($row.find(".weight").val()) || 0;
                    BasicAmt = BasicAmt * (parseFloat($row.find(".rate").val()) || 0);
                }

                $row.find(".amount").val(BasicAmt);

                // PACK AMOUNT
                const packPer = parseFloat($row.find(".pack-per").val()) || 0;
                if (packPer !== 0) {
                    $row.find(".pack-amt").val(BasicAmt * packPer / 100);
                }

                // DISCOUNT AMOUNT
                const discPer = parseFloat($row.find(".disc-per").val()) || 0;
                if (discPer !== 0) {
                    $row.find(".disc-amt").val(BasicAmt * discPer / 100);
                }

                // BASIC AMOUNT AFTER PACK + DISCOUNT
                BasicAmt = BasicAmt + (parseFloat($row.find(".pack-amt").val()) || 0) - (parseFloat($row.find(".disc-amt").val()) || 0);

                // CGST
                const cgstPer = parseFloat($row.find(".cgst-per").val()) || 0;
                if (cgstPer !== 0) {
                    $row.find(".cgst-amt").val(BasicAmt * cgstPer / 100);
                }

                // SGST
                const sgstPer = parseFloat($row.find(".sgst-per").val()) || 0;
                if (sgstPer !== 0) {
                    $row.find(".sgst-amt").val(BasicAmt * sgstPer / 100);
                }

                // IGST
                const igstPer = parseFloat($row.find(".igst-per").val()) || 0;
                if (igstPer !== 0) {
                    $row.find(".igst-amt").val(BasicAmt * igstPer / 100);
                }

                // CESS
                const cessPer = parseFloat($row.find(".cess-per").val()) || 0;
                if (cessPer !== 0) {
                    $row.find(".cess-amt").val(BasicAmt * cessPer / 100);
                }

                // VAT
                const vatPer = parseFloat($row.find(".vat-per").val()) || 0;
                if (vatPer !== 0) {
                    $row.find(".vat-amt").val(BasicAmt * vatPer / 100);
                }
            }
        }

        // TOTAL VARIABLES
        let totNos = 0;
        let totQty = 0;
        let totAmt = 0;
        let totPack = 0;
        let totdisc = 0;
        let tototh = 0;
        let totcgst = 0;
        let totsgst = 0;
        let totigst = 0;
        let totvat = 0;
        let totcess = 0;
        let totbulkqty = 0;
        let totbulkamt = 0;
        let totNetAmt = 0;

        totcess = 0;

        for (let i = 0; i < $rows.length; i++) {
            const $row = $($rows[i]);
            // TOTAL NOS
            totNos += parseFloat($row.find(".nos").val()) || 0;

            // TOTAL QTY
            if ($("#ChkCalPCS").prop("checked")) {
                totQty += parseFloat($row.find(".nos").val()) || 0;
            } else {
                totQty += parseFloat($row.find(".weight").val()) || 0;
            }

            // TOTAL AMOUNT
            totAmt += parseFloat($row.find(".amount").val()) || 0;

            // TOTAL PACK
            totPack += parseFloat($row.find(".pack-amt").val()) || 0;

            // TOTAL DISCOUNT
            totdisc += parseFloat($row.find(".disc-amt").val()) || 0;

            // TOTAL CGST
            totcgst += parseFloat($row.find(".cgst-amt").val()) || 0;

            // TOTAL SGST
            totsgst += parseFloat($row.find(".sgst-amt").val()) || 0;

            // TOTAL IGST
            totigst += parseFloat($row.find(".igst-amt").val()) || 0;

            // TOTAL VAT
            totvat += parseFloat($row.find(".vat-amt").val()) || 0;

            // TOTAL CESS
            totcess += parseFloat($row.find(".cess-amt").val()) || 0;

            // TOTAL NET AMOUNT
            totNetAmt += parseFloat($row.find(".net-amt").val()) || 0;
        }

        // SET TOTAL VALUES
        $("#NumTotalNos").val(totNos.toFixed(2));
        $("#NumTotalQty").val(totQty.toFixed(2));
        $("#NumTotalQuantity").val(totQty.toFixed(2));
        $("#NumAmount").val(totAmt.toFixed(2));
        $("#NumPackingAmount").val(totPack.toFixed(2));
        $("#NumDiscAmount").val(totdisc.toFixed(2));
        $("#NumCGSTAmt").val(totcgst.toFixed(2));
        $("#NumSGSTAmt").val(totsgst.toFixed(2));
        $("#NumIGSTAmt").val(totigst.toFixed(2));
        $("#NumVATAmt").val(totvat.toFixed(2));
        $("#NumCessAmt").val(totcess.toFixed(2));
        $("#NumOtherAmt").val(tototh.toFixed(2));

        // TCS
        const tcsPer = parseFloat($("#NumTCSRate").val()) || 0;
        const tcsAmt = totNetAmt * tcsPer * 0.01;
        $("#NumTCSAmount").val(tcsAmt.toFixed(2));

        // NET AMOUNT
        const finalTcsAmt = parseFloat($("#NumTCSAmount").val()) || 0;
        $("#NumNetAmt").val((totNetAmt + finalTcsAmt).toFixed(2));
    }
    catch (ex) {
        console.error("totalAmt()", ex);
        showToast("totalAmt() " + ex.toString(), { type: "warning" });
    }
}
function calculateTax(row, recal) {
    try {

        const $rows = $("#tblSalesOrderEntryModal tbody tr");

        if (row >= 0) {
            const $row = $($rows[row]);
            let grossAmt = (parseFloat($row.find(".amount").val()) || 0) + (parseFloat($row.find(".pack-amt").val()) || 0) -
                (parseFloat($row.find(".disc-amt").val()) || 0);

            // CGST Amount
            const cgstAmt = grossAmt * (parseFloat($row.find(".cgst-per").val()) || 0) * 0.01;
            $row.find(".cgst-amt").val(cgstAmt.toFixed(2));

            // SGST Amount
            const sgstAmt = grossAmt * (parseFloat($row.find(".sgst-per").val()) || 0) * 0.01;
            $row.find(".sgst-amt").val(sgstAmt.toFixed(2));

            // IGST Amount
            const igstAmt = grossAmt * (parseFloat($row.find(".igst-per").val()) || 0) * 0.01;
            $row.find(".igst-amt").val(igstAmt.toFixed(2));

            // VAT Amount
            const vatAmt = grossAmt * (parseFloat($row.find(".vat-per").val()) || 0) * 0.01;
            $row.find(".vat-amt").val(vatAmt.toFixed(2));

            // Net Amount
            const netAmt = grossAmt + (parseFloat($row.find(".cgst-amt").val()) || 0) + (parseFloat($row.find(".sgst-amt").val()) || 0) +
                (parseFloat($row.find(".igst-amt").val()) || 0) + (parseFloat($row.find(".vat-amt").val()) || 0) +
                (parseFloat($row.find(".cess-amt").val()) || 0);
            $row.find(".net-amt").val(netAmt.toFixed(2));

            // CGST / SGST ReadOnly
            if ((parseFloat($row.find(".cgst-amt").val()) || 0) > 0) {
                $row.find(".cgst-amt").prop("disabled", false);
                $row.find(".sgst-amt").prop("disabled", false);
            } else {
                $row.find(".cgst-amt").prop("disabled", true);
                $row.find(".sgst-amt").prop("disabled", true);
            }

            // IGST ReadOnly
            if ((parseFloat($row.find(".igst-amt").val()) || 0) > 0) {
                $row.find(".igst-amt").prop("disabled", false);
            } else {
                $row.find(".igst-amt").prop("disabled", true);
            }

            // VAT ReadOnly
            if ((parseFloat($row.find(".vat-amt").val()) || 0) > 0) {
                $row.find(".vat-amt").prop("disabled", false);
            } else {
                $row.find(".vat-amt").prop("disabled", true);
            }
        }
    }
    catch (ex) {
        console.error("calculateTax()", ex);
        showToast(ex.toString(), { type: "error" });
    }
}
function calAmount() {
    try {
        let pob = 0;

        const $rows = $("#tblSalesOrderEntryModal tbody tr");

        for (let row = 0; row < $rows.length; row++) {
            pob = 0;
            const $currentRow = $($rows[row]);

            const itemCode = $currentRow.find(".item-code").val();
            if (itemCode !== null && itemCode !== undefined && itemCode !== "") {

                if (row >= 0) {
                    // BASIC AMOUNT
                    let basic = 0.0;
                    if ($("#ChkCalPCS").prop("checked")) {
                        basic = (parseFloat($currentRow.find(".nos").val()) || 0) * (parseFloat($currentRow.find(".rate").val()) || 0);
                    } else {
                        basic = (parseFloat($currentRow.find(".weight").val()) || 0) * (parseFloat($currentRow.find(".rate").val()) || 0);
                    }

                    // EXISTING PACK / DISC / CESS
                    let pack = parseFloat($currentRow.find(".pack-amt").val()) || 0;
                    let disc = parseFloat($currentRow.find(".disc-amt").val()) || 0;
                    let cess = parseFloat($currentRow.find(".cess-amt").val()) || 0;

                    // CURRENT CELL CHECK
                    const currentCell = document.activeElement;
                    const isPackAmt = currentCell && currentCell.classList.contains("pack-amt");
                    const isDiscAmt = currentCell && currentCell.classList.contains("disc-amt");
                    const isCgstAmt = currentCell && currentCell.classList.contains("cgst-amt");
                    const isSgstAmt = currentCell && currentCell.classList.contains("sgst-amt");
                    const isIgstAmt = currentCell && currentCell.classList.contains("igst-amt");
                    const isCessAmt = currentCell && currentCell.classList.contains("cess-amt");

                    if (!isPackAmt && !isDiscAmt && !isCgstAmt && !isSgstAmt && !isIgstAmt && !isCessAmt) {
                        // DISCOUNT
                        const discPer = parseFloat($currentRow.find(".disc-per").val()) || 0;
                        if (discPer > 0) {
                            disc = basic * discPer * 0.01;
                        }

                        // PACK
                        const packPer = parseFloat($currentRow.find(".pack-per").val()) || 0;
                        if (pob === 1) {
                            if (packPer > 0) {
                                pack = basic * packPer * 0.01;
                            }
                        } else {
                            if (packPer > 0) {
                                pack = (basic - disc) * packPer * 0.01;
                            }
                        }

                        // CESS
                        const cessPer = parseFloat($currentRow.find(".cess-per").val()) || 0;
                        if (cessPer > 0) {
                            cess = (basic + pack - disc) * cessPer * 0.01;
                        }

                        // SET PACK AMOUNT
                        $currentRow.find(".pack-amt").val(pack.toFixed(2));

                        // SET DISCOUNT AMOUNT
                        $currentRow.find(".disc-amt").val(disc.toFixed(2));

                        // SET CESS AMOUNT
                        $currentRow.find(".cess-amt").val(cess.toFixed(2));
                    }

                    // GROSS AMOUNT
                    let grossAmt = basic + pack - disc;

                    // AMOUNT
                    $currentRow.find(".amount").val(basic);

                    // NET AMOUNT
                    const netAmount = grossAmt + (parseFloat($currentRow.find(".cgst-amt").val()) || 0) + (parseFloat($currentRow.find(".sgst-amt").val()) || 0) + (parseFloat($currentRow.find(".igst-amt").val()) || 0) +
                        (parseFloat($currentRow.find(".vat-amt").val()) || 0) + (parseFloat($currentRow.find(".cess-amt").val()) || 0);

                    $currentRow.find(".net-amt").val(netAmount.toFixed(2));
                }
            }
        }
    }
    catch (ex) {
        console.error("calAmount()", ex);
        showToast(ex.toString(), { type: "error" });
    }
}
async function getPackAmount(itemCode) {
    try {
        const response = await $.ajax({
            url: "/SalesOrder/GetPackAmount",
            type: "GET",
            data: { itemCode: itemCode },
            global: false
        });

        if (response.success) {
            return parseFloat(response.data) || 0;
        }

        return 0;
    }
    catch (ex) {
        console.error("getPackAmount()", ex);
        return 0;
    }
}
async function CalculateAmt() {
    try {

        const $rows = $("#tblSalesOrderEntryModal tbody tr");

        if ($rows.length === 0) return;

        for (let j = 0; j < $rows.length; j++) {

            const $row = $($rows[j]);

            const itemCode = parseInt($row.find(".item-code").val()) || 0;

            if (itemCode <= 0) continue;

            const response = await $.ajax({
                url: "/SalesOrder/GetCalculateAmtData",
                type: "GET",
                data: {
                    itemCode: itemCode
                },
                global: false
            });

            if (!response.success || !response.data) continue;

            const ldata = response.data;

            if (ldata.length === 0) continue;

            for (let i = 0; i < ldata.length; i++) {
                const data = ldata[i];

                const reportType = data.reportType || "";
                const saleRate = parseFloat(data.saleRate) || 0;
                const taxableRate = parseFloat(data.taxableRate) || 0;
                const netWt = parseFloat(data.netWt) || 0;
                const pubDefTonnageRate = data.pubDefTonnageRate || "";
                const pubDefWtCalconBales = data.pubDefWtCalconBales || "";


                // =================================================
                // pubDefTonnageRate = "Yes"
                // =================================================

                if (pubDefTonnageRate === "Yes") {

                    const qty = parseFloat($row.find(".weight").val()) || 0;
                    let amount = 0;

                    if (reportType === "Hessian") {
                        amount = (qty / saleRate) / 0.9144 * 100;
                    }
                    else if (reportType === "Fabric") {
                        amount = (qty / saleRate) * taxableRate;
                    }
                    else if (reportType === "Twine") {
                        amount = (qty / saleRate) / 10000;
                    }
                    else if (reportType === "Sacking") {
                        amount = (qty / saleRate) * 100;
                    }
                    else {
                        amount = qty / saleRate;
                    }
                    $row.find(".pack-per").val(amount.toFixed(2));
                }

                // =================================================
                // pubDefWtCalconBales = "Yes"
                // =================================================

                else if (pubDefWtCalconBales === "Yes") {
                    const nos = parseFloat($row.find(".nos").val()) || 0;
                    const weight = nos / netWt;
                    $row.find(".weight").val(weight.toFixed(2));
                }
            }
        }
    }
    catch (ex) {
        console.error("CalculateAmt()", ex);
    }
}


//==============Validations==============
async function Validate() {
    try {
        // Required Fields
        if (!validateRequiredField('#NumDocNo', 'Document No') || !validateRequiredField('#ddlBillTo', 'Bill To')) {
            return false;
        }

        // Validate VDate
        const isValidVDate = await checkValidDate();
        if (!isValidVDate) {
            return false;
        }

        // Validate Sauda
        if ((pubDefSSINSO || '').toString().toUpperCase() === "YES") {
            if (!validateRequiredField('#ddlSaudaNo', 'Sauda No')) {
                return false;
            }
        }

        const $rows = $('#tblSalesOrderEntryModal tbody tr');
        const itemCodes = [];

        // Get Item Codes
        $rows.each(function () {
            const itemCode = parseInt($(this).find(".item-code").val()) || 0;

            if (itemCode !== 0) {
                itemCodes.push(itemCode);
            }
        });

        // Item Details
        if (itemCodes.length === 0) {
            showToast("Items Details Required, Please Check!", { type: "warning" });
            return false;
        }

        // Database validation for already sold items
        const response = await $.ajax({
            url: "/SalesOrder/ValidateData",
            type: "POST",
            contentType: "application/json",
            data: JSON.stringify({
                vNo: parseInt($("#NumDocNo").val()) || 0,
                itemCodes: itemCodes
            })
        });

        if (!response.success) {
            showToast(response.message, { type: "warning" });
            return false;
        }

        // Validate Grid Rows
        for (let i = 0; i < $rows.length; i++) {
            const $row = $($rows[i]);

            const itemCode = parseInt($row.find(".item-code").val()) || 0;

            if (itemCode === 0) {
                continue;
            }

            // Item Name
            const itemEl = $row.find(".item-name");

            if (!itemEl.val()) {
                setInvalid(itemEl, "Item Name Should not be Blank of Item Code : " + itemCode);
                return false;
            }

            // Weight
            const weightEl = $row.find(".weight");

            if (!weightEl.val()) {
                const itemName = itemEl.find("option:selected").text();

                setInvalid(weightEl, "Weight Should not be Blank of " + itemName);
                return false;
            }

            // Rate
            const RateEl = $row.find(".rate");

            if (!RateEl.val()) {
                const itemName = itemEl.find("option:selected").text();

                setInvalid(RateEl, "Rate Should not be Blank of " + itemName);
                return false;
            }
        }


        return true;
    }
    catch (ex) {
        console.error("Validate()", ex);
        showToast(ex.toString(), { type: "error" });
        return false;
    }
}
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocDate").val(),
        vno: $("#NumDocNo").val()
    };
    try {
        const response = await fetch('/SalesOrder/CheckValidDate', {
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


//=============Save & Update===========
async function SaveData() {
    const tableData = await collectFormData();

    $.ajax({
        url: '/SalesOrder/CheckSaudaApproval',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(tableData),
        success: function (response) {

            if (!response.status) {
                showToast(response.message, {type:"error"});
                return;
            }

            if (response.isApprovalRequired) {
                Swal.fire({
                    title: 'Approval Required',
                    text: response.message,
                    icon: 'warning',
                    confirmButtonText: 'OK'
                }).then((result) => {
                    if (result.isConfirmed) {
                        SaveSalesOrder(tableData);
                    }
                });
                return;
            }

            SaveSalesOrder(tableData);
        },
        error: function () {
            toastr.error("SAUDA validation failed.");
        }
    });
}
async function SaveSalesOrder(tableData) {
    //const tableData = await collectFormData();
    console.log(tableData);

    $.ajax({
        url: '/SalesOrder/SaveOrUpdateSalesOrder',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(tableData),
        success: function (response) {
            if (!response?.status) {
                toastr.error(response?.message || "Save failed. Please try again.");
            }
            else if (response.isWarning) {
                showToast(response.message, { type: "warning" });
            }
            else {
                showToast(response.message, { type: "success" });
                setFormReadOnly();
                isReadOnly = true;
                setTimeout(() => window.location.href = '/SalesOrder/Index?id=' + encodeURIComponent($('#NumDocNo').val()) + '&vtype=SORD&readOnly=true', 1000);
                if (isReadOnly) {
                    const vNo = $('#NumDocNo').val();
                    checkApprovalStatus(vType, vNo, DBTableName);
                }
            }
        },
        error: function () {
            toastr.error("Error occurred while saving. Please contact admin.");
        }
    });
}
async function collectFormData() {
    const itemRecords = await collectOrder2Items();

    return {
        VNo: parseIntSafe($('#NumDocNo').val()),
        VDate: toNullableDate($('#DtDocDate').val()),

        PartyCode: parseIntSafe($('#ddlBillTo').val()),
        ShipCode: parseIntSafe($('#ddlShipTo').val()),

        Nos: parseFloatSafe($('#NumTotalNos').val()),
        Qty: parseFloatSafe($('#NumTotalQty').val()),
        Amount: parseFloatSafe($('#NumAmount').val()),
        PackAmt: parseFloatSafe($('#NumPackingAmount').val()),
        DiscAmt: parseFloatSafe($('#NumDiscAmount').val()),
        CgstAmt: parseFloatSafe($('#NumCGSTAmt').val()),
        SgstAmt: parseFloatSafe($('#NumSGSTAmt').val()),
        IgstAmt: parseFloatSafe($('#NumIGSTAmt').val()),
        OthAmt: parseFloatSafe($('#NumOtherAmt').val()),
        VatAmt: parseFloatSafe($('#NumVATAmt').val()),
        CessAmt: parseFloatSafe($('#NumCessAmt').val()),
        TcsPer: parseFloatSafe($('#NumTCSRate').val()),
        TcsAmt: parseFloatSafe($('#NumTCSAmount').val()),
        NetAmt: parseFloatSafe($('#NumNetAmt').val()),

        PaytermCode: parseIntSafe($('#ddlPaymentTerm').val()),

        SaudaType: "SAUD",
        SaudaNo: parseIntSafe($('#ddlSaudaNo option:selected').text()),
        DeliveryPeriod: toNullableString($('#TxtDeliveryPeriod').val()),
        DeliveryTo: toNullableString($('#TxtDelivery').val()),

        Remarks: toNullableString($('#TxtRemarks').val()),

        CDiscAmt: parseFloatSafe($('#NumCashDiscount').val()),

        BillAdd1: toNullableString($('#TxtAddressL1').val()),
        BillAdd2: toNullableString($('#TxtAddressL2').val()),
        BillAdd3: toNullableString($('#TxtAddressL3').val()),
        BillCity: parseIntSafe($('#ddlCity').val()),
        BillPincode: toNullableString($('#NumPincode').val()),
        BillGst: toNullableString($('#TxtGSTNo').val()),

        ShipAdd1: toNullableString($('#TxtAddressShipL1').val()),
        ShipAdd2: toNullableString($('#TxtAddressShipL2').val()),
        ShipAdd3: toNullableString($('#TxtAddressShipL3').val()),
        ShipCity: parseIntSafe($('#ddlShipCity').val()),
        ShipPincode: toNullableString($('#NumShipPincode').val()),
        ShipGst: toNullableString($('#TxtShipGST').val()),

        PartyName: $('#ddlBillTo option:selected').text(),
        ShipName: $('#ddlShipTo option:selected').text(),

        Status: parseIntSafe($('#ddlStatus').val()),
        FormCode: $('#ChkCalPCS').is(':checked') ? 1 : 0,

        SaveOrUpdate: (!rowId || rowId === 0) ? "Save" : "Update",

        ItemRecords: itemRecords,
    };
}
async function collectOrder2Items() {
    const items = [];
    const saudaNo = parseIntSafe($('#ddlSaudaNo').val());

    $('#tblSalesOrderEntryModal tbody tr').each(function () {
        const $row = $(this);
        const itemCode = parseIntSafe($row.find('.item-code').val());

        if (!itemCode || itemCode === 0) return;

        items.push({

            PlaceCode: null,
            ItemName: toNullableString($row.find('.item-name option:selected').text()),
            ItemCode: itemCode,

            NOS: parseIntSafe($row.find('.nos').val()),
            Qty: parseFloatSafe($row.find('.weight').val()),

            Rate: parseFloatSafe($row.find('.rate').val()),
            Amount: parseFloatSafe($row.find('.amount').val()),

            PackPer: parseFloatSafe($row.find('.pack-per').val()),
            PackAmt: parseFloatSafe($row.find('.pack-amt').val()),

            DiscPer: parseFloatSafe($row.find('.disc-per').val()),
            DiscAmt: parseFloatSafe($row.find('.disc-amt').val()),

            TaxCode: parseIntSafe($row.find('.tax-code').val()),

            CgstPer: parseFloatSafe($row.find('.cgst-per').val()),
            CgstAmt: parseFloatSafe($row.find('.cgst-amt').val()),

            SgstPer: parseFloatSafe($row.find('.sgst-per').val()),
            SgstAmt: parseFloatSafe($row.find('.sgst-amt').val()),

            IgstPer: parseFloatSafe($row.find('.igst-per').val()),
            IgstAmt: parseFloatSafe($row.find('.igst-amt').val()),

            VatPer: parseFloatSafe($row.find('.vat-per').val()),
            VatAmt: parseFloatSafe($row.find('.vat-amt').val()),

            CessPer: parseFloatSafe($row.find('.cess-per').val()),
            CessAmt: parseFloatSafe($row.find('.cess-amt').val()),

            NetAmt: parseFloatSafe($row.find('.net-amt').val()),


            Remarks: toNullableString($row.find('.remarks').val()),

            DeliveryDate: getOptionalDate($row.find('.delivery-date-check'), $row.find('.delivery-date')),
        });
    });

    return items;
}


//==============Edit and View==============
async function GetDocData() {
    try {
        const response = await $.ajax({
            url: '/SalesOrder/GetPurchaseOrderRecordsById',
            type: 'GET',
            data: { id: rowId, vType: rowIdVType }
        });

        if (response.status) {
            console.log(response.header, response.detail);
            await fillFormFields(response)

        } else {
            toastr.error('No data returned.');
        }
    } catch (error) {
        toastr.error('Failed to load data.');
        toastr.error(error);
    }
}
async function fillFormFields(data) {
    if (!data || !Array.isArray(data.header) || data.header.length === 0) {
        toastr.error("Invalid or empty data");
        return;
    }

    try {
        isLoadOnEdit = true;

        const d = data.header[0];

        $('#NumDocNo').val(d.V_NO ?? '');
        $('#DtDocDate').val(d.V_DATE?.split('T')[0] ?? '');

        $('#ddlSaudaNo').val((d.SAUDA_TYPE ?? '') + (d.SAUDA_NO ?? '')).trigger('change');
        $('#ddlBillTo').val(d.PARTY_CODE).trigger('change');
        bindDropdown("SalesOrder", "address", '#ddladdressL1', '', null, null, true, d.PARTY_CODE, false);

        $('#TxtAddressL1').val(d.BILL_ADD1 ?? '');
        $('#TxtAddressL2').val(d.BILL_ADD2 ?? '');
        $('#TxtAddressL3').val(d.BILL_ADD3 ?? '');
        bindDropdown("SalesOrder", "city", '#ddlCity', '--Select city--', d.BILL_CITY, null, false, null, false);
        $('#NumPincode').val(d.BILL_PINCODE ?? '');
        $('#TxtGSTNo').val(d.BILL_GST ?? '');
        $('#NumMobile').val(d.BILL_MOB ?? '');
        $('#TxtDelivery').val(d.DELIVERY_TO ?? '');
        $('#TxtDeliveryPeriod').val(d.DELIVERY_PERIOD ?? '');

        $('#ddlShipTo').val(d.SHIP_CODE).trigger('change');
        bindDropdown("SalesOrder", "address", '#ddlShipaddressL1', '', null, null, true, d.SHIP_CODE, false);

        $('#TxtAddressShipL1').val(d.SHIP_ADD1 ?? '');
        $('#TxtAddressShipL2').val(d.SHIP_ADD2 ?? '');
        $('#TxtAddressShipL3').val(d.SHIP_ADD3 ?? '');
        bindDropdown("SalesOrder", "city", '#ddlShipCity', '--Select city--', d.SHIP_CITY, null, false, null, false)
        $('#NumShipPincode').val(d.SHIP_PINCODE ?? '');
        $('#TxtShipGST').val(d.SHIP_GST ?? '');
        $('#NumContactNo').val(d.SHIP_MOB ?? '');

        $('#ddlPaymentTerm').val(d.PAYTERM_CODE).trigger('change');
        $('#NumCashDiscount').val(d.CDISC_AMT ?? '');
        $('#NumTotalQuantity').val(d.QTY ?? '');
        $('#ddlStatus').val(d.STATUS).trigger('change');
        $('#TxtRemarks').val(d.REMARKS ?? '');

        $('#ChkCalPCS').prop('checked', Number(d.FORM_CODE) === 1);

        $('#NumTotalNos').val(d.NOS ?? '');
        $('#NumTotalQty').val(d.QTY ?? '');
        $('#NumAmount').val(d.AMOUNT ?? '');
        $('#NumPackingAmount').val(d.PACK_AMT ?? '');
        $('#NumDiscAmount').val(d.DISC_AMT ?? '');

        $('#NumCGSTAmt').val(d.CGST_AMT ?? 0);
        $('#NumSGSTAmt').val(d.SGST_AMT ?? 0);
        $('#NumIGSTAmt').val(d.IGST_AMT ?? 0);
        $('#NumVATAmt').val(d.VAT_AMT ?? 0);
        $('#NumCessAmt').val(d.CESS_AMT ?? 0);
        $('#NumTCSRate').val(d.TCS_PER ?? 0);
        $('#NumTCSAmount').val(d.TCS_AMT ?? 0);
        $('#NumOtherAmt').val(d.OTH_AMT ?? 0);
        $('#NumNetAmt').val(d.NET_AMT ?? 0);

        $('#TxtParty').val(d.PARTY_NAME ?? '');
        if (d.SHORTNAME && d.SHORTNAME !== "") {
            $('#TxtItemName').val(d.SHORTNAME ?? '');
        }
        else {
            $('#TxtItemName').val(d.ITEM_NAME ?? '');
        }
        $('#NumQuantity').val(d.QTY ?? '');
        $('#NumRate').val(d.RATE ?? '');
        $('#TxtTenaCity').val(d.TENACITY_GRP ?? '');


        if (Array.isArray(data.detail)) {
            $('#tblSalesOrderEntryModal tbody').empty();

            for (const item of data.detail) {
                await addNewRowBelow(item);
            }

            setExistingItemsReadonly(data.existingItems);
        }
    }
    catch (ex) {
        console.error(ex);
        showToast("Error in loading Data.", { type: "error" });
    }
    finally {
        isLoadOnEdit = false;
        if (isReadOnly) {
            setFormReadOnly();
        }
    }
}
function setExistingItemsReadonly(existingItems) {
    if (!Array.isArray(existingItems) || existingItems.length === 0)
        return;

    const existingItemCodes = new Set(
        existingItems.map(x => String(x.ITEM_CODE))
    );

    $('#tblSalesOrderEntryModal tbody tr').each(function () {
        const $row = $(this);
        const itemCode = String($row.find('.item-code').val() || '');

        if (existingItemCodes.has(itemCode)) {
            //$row.find('.item-code').prop('disabled', true);
            $row.find('.item-name').prop('disabled', true);
        }
    });
}
function setEnterKeyFocus(sequence) {
    sequence.forEach((id, index) => {
        $(`#${id}`).on('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                if (index + 1 < sequence.length) {
                    $(`#${sequence[index + 1]}`).focus();
                }
            }
        });
    });
}
function setFormReadOnly() {
    const form = $('#SalesOrderForm');
    form.addClass('erppage-readonly');
    $('#btn-save').hide();
    $('#btnLoadIssueData, #btn_loadpacking, #btnCalcRate').prop('disabled', true).css({
        'pointer-events': 'none',
        //'opacity': '0.65',      
        'cursor': 'not-allowed'
    });
    $('#btn_createdelivery').prop('disabled', false).css({
        'pointer-events': 'auto',
        //'opacity': '0.65',      
        'cursor': 'allowed'
    });

    $('#tblSalesOrderEntryModal .delivery-date-check').prop('disabled', true)

    $('#tblSalesOrderEntryModal .btn-delete-action, #tblSalesOrderEntryModal .btn-add-action')
        .prop('disabled', true)
        .css({
            //'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
}


//===============Approval===============
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


//===============Order Adjustment=========
async function GetOrderAdjustment() {
    try {
        const response = await $.ajax({
            url: '/SalesOrder/GetOrderAdjustment',
            type: 'GET',
            data: { ordNo: $('#NumDocNo').val() }
        });

        if (response.status) {
            bindOrderAdjustment(response.data);
            //$('#OrderAdjustmentModal').modal('show');
        } else {
            toastr.error(response.message || 'No data found.');
        }
    }
    catch (error) {
        console.error(error);
        toastr.error('Failed to load order adjustment data.');
    }
}
function bindOrderAdjustment(data) {
    const $tbody = $('#tblOrderAdjustmentModal tbody');
    $tbody.empty();

    if (!Array.isArray(data) || data.length === 0) {
        $tbody.append(`<tr><td colspan="4" class="text-center">No records found</td></tr>`);
        return;
    }

    data.forEach(item => {
        $tbody.append(`
            <tr>
                <td>${item.billNo ?? ''}</td>
                <td>${item.billDate ?? ''}</td>
                <td>${item.itemName ?? ''}</td>
                <td>${item.quantity ?? 0}</td>
            </tr>
        `);
    });
}


//===============Delivery Plan============
function createDeliveryRowHtml(data = {}) {
    return `
        <tr class="no-border-input">

            <td><input class="form-control form-control-sm item-code" type="number" value="${data.iteM_CODE || data.ITEM_CODE || ''}" disabled /></td>
            <td><select class="form-control form-control-sm item-name"></select></td>
            <td>
                <div class="erppage-datebox">
                    <input type="date"
                           class="erppage-input erppage-dateinput delivery-plan-date" value="${currentDate}">

                    <label class="erppage-checkbox-inside">
                        <input type="checkbox" class="erppage-checkbox-input delivery-plan-date-check" >
                    </label>
                </div>
            </td>
            <td><input class="form-control form-control-sm quantity" type="number" value="${data.qty || data.QTY || ''}" /></td>
            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || data.REMARKS || ''}" /></td>
            <td class="action-col">
                <div class="action-wrap">
                    <button type="button" class="act-btn delete btn-delDelete-action" title="Delete Row"><i class="fa fa-trash"></i></button>
                    <button type="button" class="act-btn add btn-delAdd-action" title="Add Row"><i class="fa fa-plus-circle"></i></button>
                </div>
            </td>
        </tr>
    `;
}
async function addNewDeliveryRowBelow(data = null) {
    data = data || {};

    console.log("modal data: ", data);

    const $previousLastRow = $("#tblCreateDeliveryModal tbody tr:last");
    $previousLastRow.find(".btn-delAdd-action").remove();

    $("#tblCreateDeliveryModal tbody").append(createDeliveryRowHtml(data));

    const $lastRow = $("#tblCreateDeliveryModal tbody tr:last");

    setDateControl(
        data.deliveryDate || data.delDate,
        $lastRow.find(".delivery-plan-date"),
        $lastRow.find(".delivery-plan-date-check")
    );

    const $itemName = $lastRow.find(".item-name");

    loadItemList($itemName, "#CreateDeliveryModal");

    if (data.ItemCode || data.itemCode) {
        const itemCode = data.ItemCode || data.itemCode;
        const itemName = data.ItemName || data.itemName || "";

        $itemName.append(new Option(itemName, itemCode, true, true)).trigger("change");
    }
}

function wireDeliveryPlanEvents() {
    $("#CreateDeliveryModal").on("shown.bs.modal", async function () {
        
        $("#NumModalDocNo").val($("#NumDocNo").val());
        $("#DtModalDocDate").val($("#DtDocDate").val());

        const vNo = $("#NumDocNo").val();

        $("#tblCreateDeliveryModal tbody").empty();

        if (vNo) {
            await loadDispatchDeliveryPlan(vNo);
        } else {
            await addNewDeliveryRowBelow();
            toggleDate();
        }


    });

    //--------- Item Change ---------
    $('#tblCreateDeliveryModal').on("change", ".item-name", function () {
        if (isLoadBySaudaNo || isLoadByIssueNo || isLoadOnEdit) return;
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        //Duplicate
        if (checkDuplicateItems(currentSelect, '#tblCreateDeliveryModal')) return;

        const $row = $(this).closest("tr");
        $row.find(".item-code").val($(this).val() || "");

        setTimeout(() => {
            const $fields = $row.find("input:not(:disabled), select:not(:disabled), textarea:not(:disabled)")
                .filter(":visible");

            const currentIndex = $fields.index(this);
            const $nextField = $fields.eq(currentIndex + 1);

            if ($nextField.length) {
                $nextField.focus();
            }
        }, 0);
    });

    $('#tblCreateDeliveryModal').on('click', '.btn-delDelete-action', function () {
        const $tbody = $('#tblCreateDeliveryModal tbody');

        // Don't delete the last remaining row
        if ($tbody.find('tr').length === 1) return;

        $(this).closest('tr').remove();

        // Add + button to the new last row
        const $lastRow = $tbody.find('tr:last');

        if ($lastRow.length && $lastRow.find('.btn-delAdd-action').length === 0) {
            $lastRow.find('.action-wrap').append(`
            <button type="button" class="act-btn add btn-delAdd-action" title="Add Row">
                <i class="fa fa-plus-circle"></i>
            </button>
        `);
        }
    });

    $('#tblCreateDeliveryModal').on('click', '.btn-delAdd-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        //Duplicate
        if (checkDuplicateItems(currentSelect, '#tblCreateDeliveryModal')) return;

        await addNewDeliveryRowBelow();
    });

    $(document).on('click', '#btn_dispatchsave', async function (e) {
        e.preventDefault();
        try {
            await SaveDeliveryPlan();
        }
        catch (ex) {
            console.error(ex);
            showToast("Error in saving delivery plan details.", { type: "error" });
        }
    });
}

async function collectDeliveryPlanItems() {
    const items = [];
    const vNo = parseIntSafe($('#NumModalDocNo').val());
    const vDate = toNullableDate($('#DtModalDocDate').val());

    $('#tblCreateDeliveryModal tbody tr').each(function () {
        const $row = $(this);
        const itemCode = parseIntSafe($row.find('.item-code').val());

        if (!itemCode || itemCode === 0) return;

        items.push({
            ItemName: toNullableString($row.find('.item-name option:selected').text()),
            ItemCode: itemCode,
            DeliveryDate: getOptionalDate($row.find('.delivery-plan-date-check'), $row.find('.delivery-plan-date')),
            Qty: parseFloatSafe($row.find('.quantity').val()),
            Remarks: toNullableString($row.find('.remarks').val()),
            v_no: vNo,
            V_DATE: vDate
        });
    });

    return items;
}
async function SaveDeliveryPlan() {
    const items = await collectDeliveryPlanItems();

    const isValid = validateDeliveryPlanItems(items);
    if (!isValid) return;

    if (!items.length) {
        showToast("Please add at least one item.", { type: "warning" });
        return;
    }

    console.log(items);

    $.ajax({
        url: '/SalesOrder/SaveDispatchDetails',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(items),
        success: function (response) {
            if (!response?.status) {
                showToast(response?.message || "Save failed. Please try again.", { type: "error" });
                return;
            }

            showToast(response.message || "Delivery Plan saved successfully.", { type: "success" });
            $("#CreateDeliveryModal").modal("hide");
        },
        error: function () {
            showToast("Error occurred while saving. Please contact admin.", { type: "error" });
        }
    });
}
function validateDeliveryPlanItems(items) {
    if (!items.length) {
        showToast("No Record in grid to save.", { type: "warning" });
        return false;
    }

    const orderDate = $('#DtModalDocDate').val();
    const orderDateObj = new Date(orderDate);

    for (const item of items) {
        if (!item.ItemCode) continue;

        const deliveryDate = new Date(item.DeliveryDate);

        if (deliveryDate < orderDateObj) {
            showToast(`Invalid delivery date of Item Code => ${item.ItemCode}. Delivery date cannot be less than Order Date.`, { type: "warning" });
            return false;
        }
    }

    return true;
}

async function loadDispatchDeliveryPlan(vNo) {
    try {
        const response = await $.ajax({
            url: '/SalesOrder/GetDispatchDeliveryPlan',
            type: 'GET',
            data: { vNo: vNo }
        });

        if (!response?.status) {
            showToast(response?.message || "Failed to load delivery plan.", { type: "error" });
            return;
        }

        const $tbody = $("#tblCreateDeliveryModal tbody");
        $tbody.empty();

        if (!response.data?.length) {
            await addNewDeliveryRowBelow();
            return;
        }

        for (const item of response.data) {
            await addNewDeliveryRowBelow(item);
        }
    }
    catch (ex) {
        console.error(ex);
        showToast("Error while loading delivery plan.", { type: "error" });
    }
}


//===============Duplicate============
function checkDuplicateItems(currentSelect, tableSelector) {

    const value = currentSelect.value;
    if (!value) return false;

    let duplicate = false;

    document.querySelectorAll(`${tableSelector} .item-name`).forEach(el => {
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

//===============SetMaxLength=========
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

//===============Report==============
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/SalesOrder/GetGlobalValues",
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
function SalesOrderReport() {

    var reportName = "ORDER_1";
    var vType = "SORD";
    var VNO = ($("#NumDocNo").val() || "").trim();

    var formula =
        "{vwOrderReport.COMP_CODE} = " + compCode +
        " AND {vwOrderReport.BRANCH_CODE} = " + branchCode +
        " AND {vwOrderReport.YEAR_CODE} = " + yearCode +
        " AND {vwOrderReport.V_TYPE} = '" + vType + "'" +
        " AND {vwOrderReport.V_NO} = " + VNO;

    console.log("formula: ", formula);

    var formulaFields = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: db,

        Parameters: {
            comp_name: companyName,
            comp_add1: add1,
            comp_add2: add2,
            RPTNAME: "Sales Order",
        }
    };

    console.log("formulaFields: ", formulaFields);
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
