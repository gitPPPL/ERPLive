const $tbody = $('#tblSalesReturn tbody');
let taxOptions = [];

const urlParams = new URLSearchParams(window.location.search);
const rowId = parseInt(urlParams.get('id'));
const rowIdVType = urlParams.get('vtype');
let isReadOnly = urlParams.get('readOnly') === 'true';

let isLoadByRef = false;
let isLoadByGateNo = false;
let isLoadById = false;

let compCode = "";
let yearCode = "";
let branchCode = "";
let companyName = "";
let add1 = "";
let add2 = "";
let db = "";
let pubBPTCSPer = "";
let compPhone = "";
let compGSTIN = "";
let compPAN = "";
let compWebsite = "";
let compEmail = "";
let compRegAdd1 = "";
let compCIN = "";
let compRegAdd2 = "";
let pubUserLevel = "";

let billGst = '';
let shipGst = '';

let currentDate = '';

var controllerName = window.location.pathname.split('/')[1];
const DBTableName = "SALE1";

let partyCache = null;

const canEditTpt = urlParams.get('canEdit') === 'true';

$(document).ready(async function () {
    checkPermissionForEntryPage(controllerName);

    $('#ddlDocumentType').focus();
    toggleDate();

    getGlobalValues();

    try {
        await bindHeaderDropdowns();
        await getPartyList();
        await wireEvents();

        currentDate = getCurrentDateYMD();
        $("#DtDocumentDate, #DtReferenceDate, #DtGRdate").val(currentDate);

        if (rowId && rowIdVType) {
            await GetSalesReturnById(rowId, rowIdVType);
            checkApprovalStatus(rowIdVType, rowId, DBTableName);
        } else {
            //await GetVNo($('#ddlDocumentType').val());
            addNewRowBelow(canEditTpt);
        }
    } catch (err) {
        console.error('Page initialization failed:', err);
        showToast('Failed to initialize page', { type: "error" });
    }
});


//=============DROPDOWNS=================
async function bindHeaderDropdowns() {
    return Promise.all([
        bindDropdown("SalesReturn", "doctype", '#ddlDocumentType', '--Select DocType--', null, null, false, null, false),
        //bindDropdown("SalesReturn", "party", '#ddlPartyName', '--Select Party--', null, null, false, null, true),
        //bindDropdown("SalesReturn", "party", '#ddlWBParty', '--Select WB Party--', null, null, false, null, true),
        //bindDropdown("SalesReturn", "party", '#ddlLoadParty', '--Select Load Party--', null, null, false, null, true),
        bindDropdown("SalesReturn", "salethrough", '#ddlSaleThrough', '--Select Sale Through--', null, null, false, null, true),
        //bindDropdown("SalesReturn", "party", '#ddlConsignee', '--Select Consignee--', null, null, false, null, true),
        //bindDropdown("SalesReturn", "tax", '#ddlTaxType', '--Select Tax--', null, null, false, null, true),
        bindDropdown("SalesReturn", "reference", '#ddlReferenceNo', '--Select Reference No--', null, null, false, null, true),
        bindDropdown("SalesReturn", "wb", '#ddlWBNo', '--Select WB No--', null, null, false, null, true),
        //bindDropdown("SalesReturn", "transport", '#ddlTransport', '--Select Transport--', null, null, false, null, true),
        bindDropdown("SalesReturn", "city", '#Station', '--Select Station--', null, null, false, null, false),
        bindDropdown("SalesReturn", "city", '#TxtTransactionStation', '--Select Station--', null, null, false, null, false),
        bindDropdown("SalesReturn", "formtype", '#ddlFormType', '--Select Form Type--', null, null, false, null, false),
        bindDropdown("SalesReturn", "prodtype", '#ddlProductionType', '--Select Prod Type--', 'PSF', null, false, null, false),
        loadSaudaDropdown(),
        loadTransportDropdown(),
        loadTaxOptions(),
        bindPartyDropdown('#ddlPartyName', '--Select Party--'),
        bindPartyDropdown('#ddlConsignee', '--Select Consignee--'),
        bindPartyDropdown('#ddlWBParty', '--Select WB Party--'),
        bindPartyDropdown('#ddlLoadParty', '--Select Load Party--')
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
            url: '/SalesReturn/GetItemList',
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
                    text: item.Text,
                    hsn: item.HSN_CODE
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
            url: '/SalesReturn/GetDropdown',
            type: 'GET',
            data: {
                type: 'tax'
            },
            dataType: 'json'
        });

        taxOptions = response || [];

        console.log("Tax Options:", taxOptions);

        bindOptions($('#ddlTaxType'), taxOptions, '--Select Tax--');
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
            .attr('data-vat-per', item.VAT_PER ?? 0)
            .attr('data-tds-per', item.TDS_PER ?? 0)
            .attr('data-oth-per', item.OTH_PER ?? 0);

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
async function ddlPackNo(docType, partyCode, selectedValue = null) {
    if (!docType) return;
    partyCode = partyCode || 0;

    try {
        const response = await fetch(`/SalesReturn/GetddlPackNo?docType=${encodeURIComponent(docType)}&partyCode=${partyCode}`);

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }

        const data = await response.json();
        const ddl = $('#ddlPackNo');

        ddl.empty();
        ddl.append('<option value="">Select Pack No</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        initSelect2(ddl);
        if (selectedValue) {
            ddl.val(selectedValue).trigger('change');
        }

    } catch (err) {
        console.error("Error loading Pack No dropdown:", err);
        showToast("Error loading Pack No dropdown", { type: "error" });
    }
}
async function loadSaudaDropdown() {
    try {
        const data = await $.get('/SalesReturn/GetDropdown', { type: 'sauda' });

        const ddl = $('#ddlSaudaNo');
        ddl.empty().append('<option value="">--Select Sauda No--</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}" data-rate="${item.Rate}">${item.text}</option>`
            );
        });

        initSelect2(ddl);

    } catch (err) {
        console.error("Sauda dropdown error:", err);
    }
}
async function loadTransportDropdown() {
    try {
        const data = await $.get('/SalesReturn/GetDropdown', { type: 'transport' });

        const ddl = $('#ddlTransport');
        ddl.empty().append('<option value="">--Select Transport--</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}" data-tds="${item.TDS_PER}">${item.text}</option>`
            );
        });

        console.log("Transport data: ", data);

        initSelect2(ddl);

    } catch (err) {
        console.error("Sauda dropdown error:", err);
    }
}


async function getPartyList() {
    if (partyCache) return partyCache;

    partyCache = await $.ajax({
        url: '/SalesReturn/GetDropdown',
        type: 'GET',
        data: { type: 'party' }
    });

    return partyCache;
}
async function bindPartyDropdown(selector, placeholder) {
    const data = await getPartyList();

    const $ddl = $(selector);
    $ddl.empty().append(`<option value="">${placeholder}</option>`);

    data.forEach(x => {
        $ddl.append(
            $('<option>', {
                value: x.value,
                text: x.text
            })
        );
    });

    initSelect2($ddl);
}

//=============GENERATE VNO=================
async function GetVNo(vtype) {
    try {
        const res = await fetch(`/SalesReturn/GetVNo/?vType=${encodeURIComponent(vtype)}`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocumentNo').val(data.v_NO);
    }
    catch (e) {
        showToast('Error loading Document Number: ' + e.message, { type: "warning" });
    }
}


//=============EVENTS=================
async function wireEvents() {
    //----------DocType Change--------
    $("#ddlDocumentType").on('change', async function () {
        var vType = $(this).val();
        if (!vType) return;
        try {
            await GetVNo(vType);
        } catch (err) {
            console.error("Error loading Document No:", err);
        }
        bindDropdown("SalesReturn", "gate", '#ddlGateNo', '--Select Gate No--', null, null, false, vType, true);
        //await ddlPackNo(vType);
        await ddlPackNo(vType, $('#ddlPartyName').val());
    });

    //--------- Bill From List Change --------
    $('#ddlPartyName').on('change', async function () {
        if (isLoadByGateNo || isLoadById) return;

        const billFromCode = $(this).val();
        bindDropdown("SalesReturn", "address", '#ddladdressL1', '', null, null, true, billFromCode, false).then(function (data) {
            if (data && data.length > 0) {
                $('#ddladdressL1').val(data[0].value).trigger('change');
            }
        })

        await ddlPackNo($('#ddlDocumentType').val(), billFromCode);
        clearControlsOnBillChange();

        if ($('#ddlDocumentType').val() === 'SART' && $('#DtDocumentDate').val().replace(/-/g, '') >= '20201001') {
            $('#NumOtherTCS1').val(pubBPTCSPer);
        }

        GRID_CALCULATER();
        //$('#ddlConsignee').val(billFromCode).trigger('change');
    });

    //--------- Ship From List Change --------
    $('#ddlConsignee').on('change', function () {
        if (isLoadById) return;

        const billFromCode = $(this).val();
        bindDropdown("SalesReturn", "address", '#ddlTransactionaddressL1', '', null, null, true, billFromCode, false).then(function (data) {
            if (data && data.length > 0) {
                $('#ddlTransactionaddressL1').val(data[0].value).trigger('change');
            }
        });

        //if (isLoadBySaudaNo) return;

        $('#TxtTransactionAddressL1').val('');
        $('#TxtTransactionAddressL2').val('');
        $('#TxtTransactionAddressL3').val('');
        $('#NumTransactionPIN').val('');
    });

    //---------- Bill Address Change ----------
    bindAddressChange({
        addressSelector: '#ddladdressL1',
        partySelector: '#ddlPartyName',
        add1Selector: '#TxtAddressL1',
        add2Selector: '#TxtAddressL2',
        add3Selector: '#TxtAddressL3',
        pincodeSelector: '#NumPincode',
        citySelector: '#Station',
        isBillChange: true
    });

    //---------- Ship Address Change -----------
    bindAddressChange({
        addressSelector: '#ddlTransactionaddressL1',
        partySelector: '#ddlConsignee',
        add1Selector: '#TxtTransactionAddressL1',
        add2Selector: '#TxtTransactionAddressL2',
        add3Selector: '#TxtTransactionAddressL3',
        pincodeSelector: '#NumTransactionPIN',
        citySelector: '#TxtTransactionStation',
        isShipChange: true
    });

    //--------- Item Change ---------
    $('#tblSalesReturn').on("change", ".item-name", function () {
        if (isLoadByRef || isLoadByGateNo || isLoadById) return;
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        //Duplicate
        if (checkDuplicateItems(currentSelect)) return;

        const $row = $(this).closest("tr");
        $row.find(".item-code").val($(this).val() || "");

        const itemData = $(this).select2('data')[0];
        $row.find(".hsn").val(itemData?.hsn || "");



        // Find an already populated row having voucher data
        const $sourceRow = $('#tblSalesReturn tbody tr').filter(function () {
            return $(this).find(".sauda-type").val() ||
                $(this).find(".sauda-no").val() ||
                $(this).find(".sauda-rate").val() ||
                $(this).find(".order-type").val() ||
                $(this).find(".order-no").val() ||
                $(this).find(".order-rate").val();
        }).first();

        // If another row already contains voucher data, copy it
        if ($sourceRow.length && !$sourceRow.is($row)) {
            $row.find(".rate").val($sourceRow.find(".rate").val() || "");
            $row.find(".sauda-type").val($sourceRow.find(".sauda-type").val() || "");
            $row.find(".sauda-no").val($sourceRow.find(".sauda-no").val() || "");
            $row.find(".sauda-rate").val($sourceRow.find(".sauda-rate").val() || "");
            $row.find(".order-type").val($sourceRow.find(".order-type").val() || "");
            $row.find(".order-no").val($sourceRow.find(".order-no").val() || "");
            $row.find(".order-rate").val($sourceRow.find(".order-rate").val() || "");
        }
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

    ///---------- Delete Row Button Click -----------
    $('#tblSalesReturn').on('click', '.btn-delete-action', function () {
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

        GRID_CALCULATER();
    });

    //---------- Add Row Button Click -----------
    $('#tblSalesReturn').on('click', '.btn-add-action', async function () {
        const currentSelect = $(this).closest('tr').find('.item-name')[0];
        //Duplicate
        if (checkDuplicateItems(currentSelect)) return;

        await addNewRowBelow();
    });

    //---------- Sauda No Change -----------
    $('#ddlSaudaNo').on('change', function () {
        if (isLoadById) return;
        const saudaValue = $(this).val() || '';
        const saudaRate = parseFloat($(this).find(':selected').data('rate')) || 0;
        GetSaudaDetails(saudaValue, saudaRate);
    });

    //---------- Tax Code changed -----------
    $('#tblSalesReturn tbody').on('change', '.tax-code', function () {

        if (isLoadById) return;

        const $row = $(this).closest('tr');

        // Get selected tax information
        const selectedTax = $(this).find(':selected');

        const cgstPer = parseFloat(selectedTax.data('cgst-per')) || 0;
        const sgstPer = parseFloat(selectedTax.data('sgst-per')) || 0;
        const igstPer = parseFloat(selectedTax.data('igst-per')) || 0;
        const vatPer = parseFloat(selectedTax.data('vat-per')) || 0;

        $row.find('.cgst-per').val(formatVal(cgstPer, 'AMT'));
        $row.find('.sgst-per').val(formatVal(sgstPer, 'AMT'));
        $row.find('.igst-per').val(formatVal(igstPer, 'AMT'));
        $row.find('.vat-per').val(formatVal(vatPer, 'AMT'));

        GRID_CALCULATER();
    });

    //---------- Total fields Changed -----------
    $("#NumConsFreight, #NumTDSFreight1, #NumInsurance1, #NumOtherTCS1").on("keyup", function () {
        GRID_CALCULATER();
    });

    $('#NumOtherPacking2').on('blur', function () {
        const amt = parseFloat($(this).val()) || 0;
        PackingAmountChange(amt);
    });

    $('#NumOtherDiscount2').on('blur', function () {
        const amt = parseFloat($(this).val()) || 0;
        discAmountChange(amt);
    });

    $('#NumOtherIGST1').on('blur', function () {
        if ((parseFloat($('#NumOtherCESS2').val()) || 0) === 0) {
            const cessPercent = parseFloat($('#NumOtherCESS1').val()) || 0;

            $('#tblSalesReturn tbody tr').each(function () {
                if ((parseInt($(this).find('.item-code').val()) || 0) > 0) {
                    $(this).find('.cess-per').val(cessPercent);
                }
            });
            GRID_CALCULATER();
        }
    });

    $('#NumOtherCGST1').on('blur', function () {
        const cgstPercent = parseFloat($(this).val()) || 0;

        if (cgstPercent > 0) {
            $('#tblSalesReturn tbody tr').each(function () {
                $(this).find('.cgst-per').val(cgstPercent);
            });
            GRID_CALCULATER();
        }
    });

    $('#NumOtherIGST1').on('blur', function () {
        const igstPercent = parseFloat($(this).val()) || 0;

        if (igstPercent > 0) {
            $('#tblSalesReturn tbody tr').each(function () {
                if ((parseInt($(this).find('.item-code').val()) || 0) !== 0) {
                    $(this).find('.igst-per').val(igstPercent);
                }
            });
            GRID_CALCULATER();
        }
    });

    $('#NumOtherSGST1').on('blur', function () {
        const sgstPercent = parseFloat($(this).val()) || 0;

        if (sgstPercent > 0) {
            $('#tblSalesReturn tbody tr').each(function () {
                if ((parseInt($(this).find('.item-code').val()) || 0) !== 0) {
                    $(this).find('.sgst-per').val(sgstPercent);
                }
            });
            GRID_CALCULATER();
        }
    });

    $('#NumOtherPacking1').on('blur', function () {
        const packPercent = parseFloat($(this).val()) || 0;

        if (packPercent > 0) {
            $('#tblSalesReturn tbody tr').each(function () {
                if ((parseInt($(this).find('.item-code').val()) || 0) !== 0) {
                    $(this).find('.pack-per').val(packPercent);
                }
            });
            GRID_CALCULATER();
        }
    });

    $('#NumOtherDiscount1').on('blur', function () {
        const discPercent = parseFloat($(this).val()) || 0;

        if (discPercent > 0) {
            $('#tblSalesReturn tbody tr').each(function () {
                if ((parseInt($(this).find('.item-code').val()) || 0) > 0) {
                    $(this).find('.disc-per').val(discPercent);
                }
            });
            GRID_CALCULATER();
        }
    });

    //------------ Round Off change ----------
    $('#NumOtherRoundOff').on('keyup', function () {
        const subtotal = (parseFloat($('#NumOtherTotalAmount').val()) || 0) + (parseFloat($('#NumOtherPacking2').val()) || 0) -
            (parseFloat($('#NumOtherDiscount2').val()) || 0);
        $('#NumOtherSubTotal').val(formatVal(subtotal, 'AMT'));

        const pubRes1Dbl = (parseFloat($('#NumOtherSubTotal').val()) || 0) + (parseFloat($('#NumOtherCGST2').val()) || 0) +
            (parseFloat($('#NumOtherSGST2').val()) || 0) + (parseFloat($('#NumOtherIGST2').val()) || 0) +
            (parseFloat($('#NumOtherCESS2').val()) || 0);

        const tcsAmt = Math.ceil(pubRes1Dbl * (parseFloat($('#NumOtherTCS1').val()) || 0) * 0.01);
        $('#NumOtherTCS2').val(tcsAmt);

        const netAmt = pubRes1Dbl + tcsAmt + (parseFloat($('#NumOtherRoundOff').val()) || 0);
        $('#NumOtherNetAmount').val(formatVal(netAmt, 'AMT'));

        if ((parseFloat($('#NumInsurance1').val()) || 0) > 0) {
            const insAmt = Math.round((parseFloat($('#NumOtherNetAmount').val()) || 0) * ((parseFloat($('#NumInsurance1').val()) || 0) / 100000));
            $('#NumInsurance2').val(formatVal(insAmt, 'AMT'));
        }

        if ((parseFloat($('#NumTDSFreight1').val()) || 0) > 0) {
            const tdsAmt = Math.round((parseFloat($('#NumConsFreight').val()) || 0) * ((parseFloat($('#NumTDSFreight1').val()) || 0) / 100));
            $('#NumTDSFreight2').val(formatVal(tdsAmt, 'AMT'));
        }
    });

    //------------ Transport change ----------
    $('#ddlTransport').on('change', function () {
        if (isLoadById) return;
        const tds = $(this).find(':selected').data('tds') || 0;
        $('#NumTDSFreight1').val(tds);
    });

    //------------ WB No change ----------
    $('#ddlWBNo').on('change', async function () {
        if (isLoadById) return;
        const wbno = $(this).val().trim();
        await getWBQty(wbno);
    });

    //------------ Pack No change ----------
    $('#ddlPackNo').on('change', async function () {
        if (isLoadById) return;
        const packDocId = $(this).val().trim();
        await validatePackNo(packDocId);
    });

    //------------ Gate No change ----------
    $('#ddlGateNo').on('change', async function () {
        if (isLoadById) return;
        if ($('#ddlDocumentType').val() !== 'SAJR') return;
        const gateDocId = $(this).val();
        await GetGateDetails(gateDocId);
    });

    //------------ Reference No change ----------
    $('#ddlReferenceNo').on('change', async function () {
        if (isLoadById) return;
        var refValue = $(this).val();
        //var refText = $('#ddlReferenceNo option:selected').text().trim();
        if (!refValue) return;
        await getRefData(refValue);
    });

    //---------- Tax Type Change ----------
    $('#ddlTaxType').on('change', function () {
        if (isLoadById) return;

        const taxCode = $(this).val() || '';
        const $selected = $(this).find(':selected');

        if (!taxCode) {
            $('#NumOtherCGST1,#NumOtherSGST1,#NumOtherIGST1,#NumOtherCESS1,#NumTDSFreight1').val('');
            GRID_CALCULATER();
            return;
        }

        $('#NumOtherCGST1').val(formatVal($selected.data('cgst-per') || 0, 'AMT'));
        $('#NumOtherSGST1').val(formatVal($selected.data('sgst-per') || 0, 'AMT'));
        $('#NumOtherIGST1').val(formatVal($selected.data('igst-per') || 0, 'AMT'));
        $('#NumTDSFreight1').val(formatVal($selected.data('tds-per') || 0, 'AMT'));
        $('#NumOtherCESS1').val(formatVal($selected.data('oth-per') || 0, 'AMT'));


        GRID_CALCULATER();
    });

    //---------- Truck No Input ----------
    $('#NumTruckNo').on('input', function () {
        this.value = this.value.toUpperCase();
    });

    //---------- Grid Input ----------
    $(document).on('input change', '#tblSalesReturn input', function () {
        const row = $(this).closest('tr');

        if (!row.length) return;

        GRID_CALCULATER();
    });

    $(document).on('click', '#btn-Calculator', async function () {
        await CalculateSaudaRate();
    });

    //==========================Save Funcation Start Block=================

    $('#btn-save').click(async function (e) {
        e.preventDefault();
        if (!await validateData()) return;
        try {
            await saveSalesReturn();
        }
        catch (ex) {
            console.error(ex);
            showToast("Error occurred while saving the record.", { type: "error" })
        }
    });

    //==========================Transport Details Edit And Update=================
    $(document).on('click', '#btn-EditTptData', async function () {
        if (daysSinceVoucher() >= 3) {
            await Swal.fire({
                icon: 'error',
                title: 'Validation',
                text: 'E-Invoice generated 3 days ago, Modification not allowed.',
                confirmButtonText: 'OK'
            });

            if (Number(pubUserLevel) !== 1) return;
        }

        TPT_FIELDS.forEach(sel => {
            $(sel).prop('disabled', false).prop('readonly', false).addClass('tpt-editable');
            $(sel).next('.select2-container').addClass('tpt-editable');
        });

        ['#ddlLoadParty', '#ddlWBParty', '#ddlTransport'].forEach(sel => {
            const $el = $(sel);
            $el.prop('disabled', false).prop('readonly', false);

            // Select2 ko apni disabled state refresh karni padti hai
            $el.trigger('change.select2');

            // container class lagao (next() ke bajay Select2 ka apna container use karo)
            const $container = $el.data('select2')
                ? $el.data('select2').$container
                : $el.nextAll('.select2-container').first();

            $container.addClass('tpt-editable').removeClass('select2-container--disabled');
        });

        // GR date ke liye
        $('#chkGRDate').prop('disabled', false).addClass('tpt-editable');
        $('#DtGRdate').prop('disabled', !$('#chkGRDate').is(':checked')).addClass('tpt-editable');

        $('#SalesReturnForm').addClass('tpt-editing');

        $('#btn-EditTptData').prop('disabled', true);
        $('#btn-SaveTptData').show().prop('disabled', false);
    });

    $(document).on('click', '#btn-SaveTptData', async function () {
        await saveTptDetails();
    });
}
function clearControlsOnBillChange() {
    //Bill Details
    $('#TxtAddressL1').val('');
    $('#TxtAddressL2').val('');
    $('#TxtAddressL3').val('');
    $('#Station').val('');
    $('#NumPincode').val('');

    //Ship Details
    $('#TxtTransactionAddressL1').val('');
    $('#TxtTransactionAddressL2').val('');
    $('#TxtTransactionAddressL3').val('');
    $('#TxtTransactionStation').val('');
    $('#NumTransactionPIN').val('');
}
function bindAddressChange({ addressSelector, partySelector, add1Selector, add2Selector, add3Selector, pincodeSelector, citySelector,
    isBillChange = false, isShipChange = false }) {
    $(addressSelector).on('change', async function () {
        //if (isLoadByGateNo) return;

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
                    url: '/SalesReturn/GetPartyAddress',
                    type: 'GET',
                    data: { code, addressId }
                });

                const address = response.data[0];
                //console.log("address details on party/consignee change: ", address);
                $(add1Selector).val(address.add1);
                $(add2Selector).val(address.add2);
                $(add3Selector).val(address.add3);
                $(pincodeSelector).val(address.pincode);

                bindDropdown("SalesOrder", "city", citySelector, '--Select city--', address.cityCode, null, false, null, false)

                // Party address changed
                if (isBillChange) {
                    billGst = address.gstin;
                    $('#ddlConsignee').val(code).trigger('change');
                    await bindDropdown("SalesReturn", "address", '#ddlTransactionaddressL1', '', null, null, true, code, false);
                    $('#ddlTransactionaddressL1').val(addressId).trigger('change');
                }
                else if (isShipChange) {
                    shipGst = address.gstin;
                    console.log("shipGst: ", shipGst)
                }
            } catch (error) {
                showToast('Error loading address', { type: "error" });
            }
        }
    });
}


//=============Add Footer Rows==========
function createRowHtml(data = {}) {
    // blank stays blank, otherwise formatted like VB formatVal
    const f = (v, t) => (v === '' || v === null || v === undefined) ? '' : formatVal(v, t);

    const grossQty = data.GROSS_QTY ?? data.grosS_QTY;
    const netQty = data.qty ?? data.QTY;
    const rate = data.rate ?? data.RATE;
    const amount = data.amount ?? data.AMOUNT;
    const packPer = data.pacK_PER ?? data.PACK_PER;
    const packAmt = data.pacK_AMT ?? data.PACK_AMT;
    const discPer = data.disC_PER ?? data.DISC_PER;
    const discAmt = data.disC_AMT ?? data.DISC_AMT;
    const cgstPer = data.cgsT_PER ?? data.CGST_PER;
    const cgstAmt = data.cgsT_AMT ?? data.CGST_AMT;
    const sgstPer = data.sgsT_PER ?? data.SGST_PER;
    const sgstAmt = data.sgsT_AMT ?? data.SGST_AMT;
    const igstPer = data.igsT_PER ?? data.IGST_PER;
    const igstAmt = data.igsT_AMT ?? data.IGST_AMT;
    const cessPer = data.cesS_PER ?? data.CESS_PER;
    const cessAmt = data.cesS_AMT ?? data.CESS_AMT;

    return `
        <tr class="no-border-input">

            <td><input class="form-control form-control-sm item-code" type="number" value="${data.iteM_CODE || data.ITEM_CODE || ''}" disabled/></td>
            <td><select class="form-control form-control-sm item-name"></select></td>

            <td><input class="form-control form-control-sm nos" type="number" value="${data.nos || data.NOS || ''}"/></td>

            <td><input class="form-control form-control-sm gross-qty" type="number" value="${f(grossQty, 'QTY')}" oninput="SetMaxlength(this, 18, 4);"/></td>
            <td><input class="form-control form-control-sm net-qty" type="number" value="${f(netQty, 'QTY')}" oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><input class="form-control form-control-sm rate" type="number" value="${f(rate, 'RATE')}" oninput="SetMaxlength(this, 18, 4);"/></td>
            <td><input class="form-control form-control-sm amount" type="number" value="${f(amount, 'AMT')}" oninput="SetMaxlength(this, 18, 4);" disabled/></td>

            <td><input class="form-control form-control-sm pack-per" type="number" value="${f(packPer, 'AMT')}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm pack-amt" type="number" value="${f(packAmt, 'AMT')}" oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><input class="form-control form-control-sm disc-per" type="number" value="${f(discPer, 'AMT')}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm disc-amt" type="number" value="${f(discAmt, 'AMT')}" oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><select class="form-control form-control-sm tax-code"></select></td>

            <td><input class="form-control form-control-sm cgst-per" type="number" value="${f(cgstPer, 'AMT')}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm cgst-amt" type="number" value="${f(cgstAmt, 'AMT')}"
            ${(data.cgsT_PER || data.CGST_PER) ? '' : 'disabled'} oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><input class="form-control form-control-sm sgst-per" type="number" value="${f(sgstPer, 'AMT')}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm sgst-amt" type="number" value="${f(sgstAmt, 'AMT')}"
            ${(data.sgsT_PER || data.SGST_PER) ? '' : 'disabled'} oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><input class="form-control form-control-sm igst-per" type="number" value="${f(igstPer, 'AMT')}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm igst-amt" type="number" value="${f(igstAmt, 'AMT')}"
            ${(data.igsT_PER || data.IGST_PER) ? '' : 'disabled'} oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><input class="form-control form-control-sm cess-per" type="number" value="${f(cessPer, 'AMT')}" oninput="SetMaxlength(this, 7, 4);"/></td>
            <td><input class="form-control form-control-sm cess-amt" type="number" value="${f(cessAmt, 'AMT')}" oninput="SetMaxlength(this, 18, 4);"/></td>

            <td><input class="form-control form-control-sm remarks" type="text" value="${data.remarks || data.REMARKS || ''}"/></td>

            <td><input class="form-control form-control-sm pack-no" type="text" value="${data.pacK_NO || data.PACK_NO || ''}" disabled/></td>
            <td><input class="form-control form-control-sm lot-no" type="text" value="${data.loT_No || data.LOT_No || data.LOT_NO || ''}" disabled maxlength="50"/></td>
            <td><input class="form-control form-control-sm sauda-type" type="text" value="${data.saudA_TYPE || data.SAUDA_TYPE || ''}" disabled maxlength="4"/></td>
            <td><input class="form-control form-control-sm sauda-no" type="text" value="${data.saudA_NO || data.SAUDA_NO || ''}" disabled/></td>
            <td><input class="form-control form-control-sm sauda-rate" type="text" value="${data.saudA_RATE || data.SAUDA_RATE || ''}" oninput="SetMaxlength(this, 18, 4);" disabled/></td>
            <td><input class="form-control form-control-sm order-type" type="text" value="${data.orD_TYPE || data.ORD_TYPE || ''}" disabled maxlength="4"/></td>
            <td><input class="form-control form-control-sm order-no" type="text" value="${data.orD_NO || data.ORD_NO || ''}" disabled/></td>
            <td><input class="form-control form-control-sm order-rate" type="text" value="${data.orD_RATE || data.ORD_RATE || ''}" oninput="SetMaxlength(this, 18, 4);" disabled/></td>
            <td><input class="form-control form-control-sm dcn-type" type="text" value="${data.dcN_TYPE || data.DCN_TYPE || ''}" disabled maxlength="4"/></td>
            <td><input class="form-control form-control-sm dcn-no" type="text" value="${data.dcN_NO || data.DCN_NO || ''}" disabled/></td>
            <td><input class="form-control form-control-sm hsn" type="text" value="${data.hsn || data.HSN || ''}" maxlength="20"/></td>

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
    const $previousLastRow = $("#tblSalesReturn tbody tr:last");
    $previousLastRow.find(".btn-add-action").remove();

    let rowHtml = createRowHtml(data);
    $("#tblSalesReturn tbody").append(rowHtml);

    const $lastRow = $("#tblSalesReturn tbody tr:last");

    //setDateControl(data.DELIVERY_DATE || data.delDate, $lastRow.find(".delivery-date"), $lastRow.find(".delivery-date-check"))

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


//==============Reference Data=========
async function getRefData(refValue) {
    $.ajax({
        url: '/SalesReturn/GetReferenceDetails',
        type: 'POST',
        data: { refValue },
        success: async function (res) {

            console.log("Reference Data:", res);
            if (res.success) {
                if (res.header.length > 0 && res.items.length > 0) {
                    await bindSalesReturnByRefNo(res);
                }
                else {
                    clearReferenceFields();
                }
            } else {
                showToast('No data found', { type: "error" });
                clearReferenceFields();
            }
        },
        error: function () {
            showToast('Server Error', { type: "error" });
        }
    });
}
async function bindSalesReturnByRefNo(res) {
    try {
        isLoadByRef = true;

        const data = res.header?.[0];
        if (!data) return;

        //$('#DtDocumentDate').val(data.V_DATE?.substring(0, 10));
        setDateControl(data.V_DATE, '#DtReferenceDate', '#chkReferenceDate');

        $('#ddlSupplyType').val('B2B').trigger('change');
        $('#ddlProductionType').val(data.ITEM_TYPE).trigger('change');

        $('#ddlPartyName').val(data.BILL_CODE).trigger('change');
        $('#TxtAddressL1').val(data.BILL_ADD1);
        $('#TxtAddressL2').val(data.BILL_ADD2);
        $('#TxtAddressL3').val(data.BILL_ADD3);
        $('#Station').val(data.BILL_CITY).trigger('change');
        $('#NumPincode').val(data.BILL_PINCODE);

        $('#ddlSaleThrough').val(data.AGENT_CODE).trigger('change');
        $('#ddlConsignee').val(data.SHIP_CODE).trigger('change');
        $('#TxtTransactionAddressL1').val(data.SHIP_ADD1);
        $('#TxtTransactionAddressL2').val(data.SHIP_ADD2);
        $('#TxtTransactionAddressL3').val(data.SHIP_ADD3);
        $('#TxtTransactionStation').val(data.SHIP_CITY).trigger('change');
        $('#NumTransactionPIN').val(data.SHIP_PINCODE);
        $('#ddlFormType').val(data.FORM_CODE).trigger('change');

        $('#ddlTaxType').val(data.TAX_CODE).trigger('change');

        $('#ddlWBNo').val(data.WB_TYPE + data.WB_NO).trigger('change');
        if (data.SAUDA_TYPE && data.SAUDA_NO) {
            $('#ddlSaudaNo').val(data.SAUDA_TYPE + data.SAUDA_NO).trigger('change');
            $('#NumSaudaRate').val(data.SAUDA_RATE);
        }

        $('#NumOtherTotalAmount').val(data.AMOUNT);

        $('#NumOtherPacking1').val(data.PACK_PER);
        $('#NumOtherPacking2').val(data.PACK_AMT);

        $('#NumOtherCGST1').val(data.CGST_PER);
        $('#NumOtherCGST2').val(data.CGST_AMT);

        $('#NumOtherSGST1').val(data.SGST_PER);
        $('#NumOtherSGST2').val(data.SGST_AMT);

        $('#NumOtherIGST1').val(data.IGST_PER);
        $('#NumOtherIGST2').val(data.IGST_AMT);

        $('#NumOtherCESS1').val(data.CESS_PER);
        $('#NumOtherCESS2').val(data.CESS_AMT);

        $('#NumLoadAmount1').val(data.LOAD_PER);
        $('#NumLoadAmount2').val(data.LOAD_AMT);
        $('#ddlLoadParty').val(data.LOAD_AC).trigger('change');
        $('#TxtRemarks1').val(data.LOAD_REM);

        $('#NumWBAmount').val(data.WB_AMT);
        $('#ddlWBParty').val(data.WB_AC).trigger('change');
        $('#TxtRemarks2').val(data.WB_REM);
        $('#NumWBQty').val(data.WB_QTY);

        $('#NumOtherTotalNos').val(data.TOT_NOS);
        $('#NumConsFreight').val(data.FRT_AMT);
        $('#NumOtherRoundOff').val(data.ROUND_OFF);
        $('#NumOtherNetAmount').val(data.NAMOUNT);

        $('#NumInsurance1').val(data.INSU_PER);
        $('#NumInsurance2').val(data.INSU_AMT);

        $('#NumTDSFreight1').val(data.TDS_PER);
        $('#NumTDSFreight2').val(data.TDS_AMT);

        $('#NumGrossQty').val(data.TOT_GROSS);
        $('#NumNetQty').val(data.TOT_NET);

        $('#NumWayBillNo').val(data.WAYBILL_NO);

        $('#NumPayFreight').val((data.FRT_TOPAY).toFixed(4));
        $('#TxtRemarks3').val(data.REMARK);

        $('#NumOtherDiscount1').val(data.DISC_PER);
        $('#NumOtherDiscount2').val(data.DISC_AMT);

        //$('#ddlTransport').val(data.TRANSPORT_CODE).trigger('change');
        //$('#NumGRNo').val(data.GR_NO);
        //$('#DtGRdate').val(data.GR_DATE?.substring(0, 10));
        //$('#NumTruckNo').val(data.VEHICLE_NO);
        //$('#txtDriverName').val(data.DRIVER_NAME);
        //$('#NumMobileNo').val(data.DRIVER_NO);

        const items = res.items || [];
        $tbody.empty();
        for (const item of items) {
            await addNewRowBelow(item);
        }
    } catch (e) {
        console.error(e);
        showToast("Error in binding details by Reference No.", { type: "error" });
        addNewRowBelow();
    }
    finally {
        isLoadByRef = false;
    }

}
function clearReferenceFields() {
    // Reference
    $('#DtReferenceDate').val(currentDate).prop('disabled', true);
    $('#chkReferenceDate').prop('checked', false);

    // Party / Bill
    $('#ddlPartyName').val('').trigger('change');
    $('#TxtAddressL1, #TxtAddressL2, #TxtAddressL3').val('');
    $('#Station').val('').trigger('change');
    $('#NumPincode').val('');

    // Consignee / Shipping
    $('#ddlConsignee').val('').trigger('change');
    $('#TxtTransactionAddressL1, #TxtTransactionAddressL2, #TxtTransactionAddressL3').val('');
    $('#TxtTransactionStation').val('').trigger('change');
    $('#NumTransactionPIN').val('');

    // Other dropdowns
    $('#ddlSupplyType').val('').trigger('change');
    $('#ddlProductionType').val('').trigger('change');
    $('#ddlSaleThrough').val('').trigger('change');
    $('#ddlFormType').val('').trigger('change');
    $('#ddlTaxType').val('').trigger('change');
    $('#ddlWBNo').val('').trigger('change');
    $('#ddlSaudaNo').val('').trigger('change');
    $('#ddlLoadParty').val('').trigger('change');
    $('#ddlWBParty').val('').trigger('change');

    // Sauda
    $('#NumSaudaRate').val('');

    // Other Amounts
    $('#NumOtherTotalAmount').val('');
    $('#NumOtherPacking1, #NumOtherPacking2').val('');

    $('#NumOtherCGST1, #NumOtherCGST2').val('');
    $('#NumOtherSGST1, #NumOtherSGST2').val('');
    $('#NumOtherIGST1, #NumOtherIGST2').val('');
    $('#NumOtherCESS1, #NumOtherCESS2').val('');

    // Loading
    $('#NumLoadAmount1, #NumLoadAmount2').val('');
    $('#TxtRemarks1').val('');

    // Way Bill
    $('#NumWBAmount, #NumWBQty').val('');
    $('#TxtRemarks2').val('');

    // Totals
    $('#NumOtherTotalNos').val('');
    $('#NumConsFreight').val('');
    $('#NumOtherRoundOff').val('');
    $('#NumOtherNetAmount').val('');

    // Insurance / TDS
    $('#NumInsurance1, #NumInsurance2').val('');
    $('#NumTDSFreight1, #NumTDSFreight2').val('');

    // Quantity
    $('#NumGrossQty').val('');
    $('#NumNetQty').val('');

    // Waybill / Freight / Remarks
    $('#NumWayBillNo').val('');
    $('#NumPayFreight').val('');
    $('#TxtRemarks3').val('');

    // Discount
    $('#NumOtherDiscount1, #NumOtherDiscount2').val('');

    // Items
    $tbody.empty();
    addNewRowBelow();
}


//==============Sauda Data===========
async function GetSaudaDetails(saudaValue, saudaRate) {
    try {
        const partyCode = parseInt($('#ddlPartyName').val()) || 0;

        $('#NumSaudaRate').val(saudaRate || '');

        if (!saudaValue) {
            $('#tblSalesReturn tbody tr').each(function () {
                $(this).find('.sauda-type,.sauda-no,.sauda-rate,.order-type,.order-no,.order-rate').val('');
            });
            return;
        }

        const saudaType = saudaValue.substring(0, 4);
        const saudaNo = saudaValue.substring(4);

        if ((parseFloat(saudaRate) || 0) === 0) {
            return;
        }

        //if (partyCode <= 0) {
        //    showToast('Please select Party.', { type: 'error' });
        //    return;
        //}

        const itemCodes = [];

        $('#tblSalesReturn tbody tr').each(function () {
            const $row = $(this);
            const itemCode = parseInt($row.find('.item-code').val()) || 0;

            if (itemCode > 0) {
                itemCodes.push(itemCode);

                const oldSaudaNo = String($row.find('.sauda-no').val() || '').trim();

                if (oldSaudaNo !== String(saudaNo).trim()) {
                    $row.find('.sauda-type,.sauda-no,.sauda-rate, .order-type,.order-no,.order-rate').val('');
                }
            }
        });

        if (!itemCodes.length) {
            return;
        }

        const response = await $.get('/SalesReturn/GetSaudaItemDetails', {
            saudaType: saudaType,
            saudaNo: saudaNo,
            partyCode: partyCode,
            itemCodes: itemCodes.join(',')
        });

        if (!response.success) {
            showToast(response.message || 'Error loading Sauda details.', { type: 'error' });
            return;
        }

        console.log('Sauda details:', response.data);

        const saudaMap = new Map(
            (response.data || []).map(item => [
                String(item.iteM_CODE), item
            ])
        );

        // Apply Sauda details to grid
        $('#tblSalesReturn tbody tr').each(function () {
            const $row = $(this);
            const itemCode = parseInt($row.find('.item-code').val()) || 0;

            if (itemCode <= 0) {
                return;
            }

            const item = saudaMap.get(String(itemCode));

            // No matching ORDER2/discount data for this item.
            if (!item) {
                return;
            }

            const rate = (parseFloat(saudaRate) || 0) + (parseFloat(item.sizE_DIFF) || 0) + (parseFloat(item.coloR_DIFF) || 0) + (parseFloat(item.graM_DIFF) || 0) +
                (parseFloat(item.iteM_DIFF) || 0);

            $row.find('.sauda-type').val(saudaType);
            $row.find('.sauda-no').val(saudaNo);
            $row.find('.sauda-rate').val(saudaRate);

            $row.find('.rate').val(formatVal(rate, 'AMT')).prop('readonly', true);

            $row.find('.order-type').val(item.orD_TYPE || '');
            $row.find('.order-no').val(item.orD_NO || '');
            $row.find('.order-rate').val(item.orD_RATE || '');
        });

        GRID_CALCULATER();

    } catch (err) {
        console.error('GetSaudaDetails error:', err);
        showToast(err.message || 'Error while loading Sauda details.', { type: 'error' });
    }
}


//==============Calculations==========
function GRID_CALCULATER() {
    try {
        const val = function (value) {
            if (value === null || value === undefined || value === '') return 0;
            const n = parseFloat(value);
            return isNaN(n) ? 0 : n;
        };

        const isPcs = $('#ChkPCS').is(':checked');
        const amtType = isPcs ? 'AMTSALE' : 'AMT';

        const fAmt = v => formatVal(v, amtType);
        // value after VB-style formatting (VB reads Val() of the formatted cell)
        const rAmt = v => parseFloat(formatVal(v, amtType));

        // header tax %
        const hCgst = val($('#NumOtherCGST1').val());
        const hSgst = val($('#NumOtherSGST1').val());
        const hIgst = val($('#NumOtherIGST1').val());
        const hCess = val($('#NumOtherCESS1').val());

        let totalAmount = 0, totalNos = 0, totalGross = 0, totalNet = 0;
        let totalPackAmt = 0, totalDiscAmt = 0;
        let totalCGSTAmt = 0, totalSGSTAmt = 0, totalIGSTAmt = 0, totalCESSAmt = 0;

        $('#tblSalesReturn tbody tr').each(function () {
            const row = $(this);
            const itemCode = val(row.find('.item-code').val());

            // VB: non-PCS -> <> 0 , PCS -> > 0
            if (isPcs ? !(itemCode > 0) : itemCode === 0) return;

            const nos = val(row.find('.nos').val());
            const gross = val(row.find('.gross-qty').val());
            const net = val(row.find('.net-qty').val());
            const rate = val(row.find('.rate').val());

            // ---- Amount ----
            let amount = val(row.find('.amount').val());
            if (isPcs) {
                if (nos > 0) {
                    amount = rAmt(gross * rate);
                    row.find('.amount').val(fAmt(amount));
                }
            } else {
                if (net > 0) {
                    amount = rAmt(net * rate);
                    row.find('.amount').val(fAmt(amount));
                }
            }

            // ---- Pack (on amount) ----
            const packPer = val(row.find('.pack-per').val());
            let packAmt = val(row.find('.pack-amt').val());
            if (packPer > 0) {
                packAmt = rAmt(amount * (packPer / 100));
                row.find('.pack-amt').val(fAmt(packAmt));
            }

            let base = amount + packAmt;

            // ---- Disc (on amount + pack) ----
            const discPer = val(row.find('.disc-per').val());
            let discAmt = val(row.find('.disc-amt').val());
            if (discPer > 0) {
                discAmt = rAmt(base * discPer / 100);
                row.find('.disc-amt').val(fAmt(discAmt));
            }

            base = amount + packAmt - discAmt;

            // ---- Tax % from header (VB sets only when > 0) ----
            if (hCgst > 0) row.find('.cgst-per').val(hCgst);
            if (hSgst > 0) row.find('.sgst-per').val(hSgst);
            if (hIgst > 0) row.find('.igst-per').val(hIgst);
            if (hCess > 0) row.find('.cess-per').val(hCess);

            // ---- Tax amounts (only when amount > 0) ----
            let cgstAmt = val(row.find('.cgst-amt').val());
            let sgstAmt = val(row.find('.sgst-amt').val());
            let igstAmt = val(row.find('.igst-amt').val());
            let cessAmt = val(row.find('.cess-amt').val());

            if (amount > 0) {
                cgstAmt = rAmt(base * val(row.find('.cgst-per').val()) / 100);
                sgstAmt = rAmt(base * val(row.find('.sgst-per').val()) / 100);
                igstAmt = rAmt(base * val(row.find('.igst-per').val()) / 100);
                cessAmt = rAmt(base * val(row.find('.cess-per').val()) / 100);

                row.find('.cgst-amt').val(fAmt(cgstAmt));
                row.find('.sgst-amt').val(fAmt(sgstAmt));
                row.find('.igst-amt').val(fAmt(igstAmt));
                row.find('.cess-amt').val(fAmt(cessAmt));
            }

            // ---- Totals ----
            totalNos += nos;
            totalGross += gross;
            totalNet += net;
            totalAmount += amount;
            totalPackAmt += packAmt;
            totalDiscAmt += discAmt;
            totalCGSTAmt += cgstAmt;
            totalSGSTAmt += sgstAmt;
            totalIGSTAmt += igstAmt;
            totalCESSAmt += cessAmt;
        });

        // ---- Header (VB: AMT / QTY) ----
        $('#NumOtherTotalAmount').val(formatVal(totalAmount, 'AMT'));
        $('#NumOtherTotalNos').val(formatVal(totalNos, 'AMT'));
        $('#NumGrossQty').val(formatVal(totalGross, 'QTY'));
        $('#NumNetQty').val(formatVal(totalNet, 'QTY'));
        $('#NumOtherPacking2').val(formatVal(totalPackAmt, 'AMT'));
        $('#NumOtherDiscount2').val(formatVal(totalDiscAmt, 'AMT'));

        // VB keeps CGST/SGST/IGST totals unformatted; toFixed(2) only removes float noise (sum of 2-dec values)
        const cgstTot = Number(totalCGSTAmt.toFixed(2));
        const sgstTot = Number(totalSGSTAmt.toFixed(2));
        const igstTot = Number(totalIGSTAmt.toFixed(2));
        $('#NumOtherCGST2').val(cgstTot);
        $('#NumOtherSGST2').val(sgstTot);
        $('#NumOtherIGST2').val(igstTot);
        $('#NumOtherCESS2').val(formatVal(totalCESSAmt, 'AMT'));

        // VB: subtotal = Val(total) + Val(pack) - Val(disc), AMT
        const subtotal = parseFloat(formatVal(totalAmount, 'AMT')) + parseFloat(formatVal(totalPackAmt, 'AMT')) - parseFloat(formatVal(totalDiscAmt, 'AMT'));
        $('#NumOtherSubTotal').val(formatVal(subtotal, 'AMT'));

        const subtotalR = parseFloat(formatVal(subtotal, 'AMT'));
        const PubRes1Dbl = subtotalR + cgstTot + sgstTot + igstTot + parseFloat(formatVal(totalCESSAmt, 'AMT'));

        const tcsPer = val($('#NumOtherTCS1').val());
        const tcsAmt = Math.ceil(PubRes1Dbl * tcsPer * 0.01);
        $('#NumOtherTCS2').val(tcsAmt);

        const roundOff = val($('#NumOtherRoundOff').val());
        const netAmt = parseFloat(formatVal(PubRes1Dbl + tcsAmt + roundOff, 'AMT'));
        $('#NumOtherNetAmount').val(formatVal(netAmt, 'AMT'));

        const insPer = val($('#NumInsurance1').val());
        if (insPer > 0) {
            $('#NumInsurance2').val(formatVal(Math.round(netAmt * (insPer / 100000)), 'AMT'));
        }

        const tdsPer = val($('#NumTDSFreight1').val());
        const consFreight = val($('#NumConsFreight').val());
        if (tdsPer > 0) {
            $('#NumTDSFreight2').val(formatVal(Math.round(consFreight * (tdsPer / 100)), 'AMT'));
        }

    } catch (ex) {
        console.error(ex);
        showToast(ex.message || 'Error', { type: "error" });
    }
}
function PackingAmountChange(amt) {
    try {

        const packAmt = parseFloat(amt) || 0;
        const totalAmt = parseFloat($('#NumOtherTotalAmount').val()) || 0;

        $('#tblSalesReturn tbody tr').each(function () {
            const $row = $(this);
            const packPercent = parseFloat($row.find('.pack-per').val()) || 0;

            if (!packPercent) {
                const amount = parseFloat($row.find('.amount').val()) || 0;
                const packAmount = totalAmt ? (amount * packAmt / totalAmt) : 0;
                $row.find('.pack-amt').val(formatVal(packAmount, 'AMT'));
            }
        });

        GRID_CALCULATER();
    } catch (err) {
        console.error('Pack Amount error:', err);
        showToast('Error while calculating Pack Amount.', { type: "error" });
    }
}
function discAmountChange(amt) {
    try {
        const discAmt = parseFloat(amt) || 0;
        const totalAmt = parseFloat($('#NumOtherTotalAmount').val()) || 0;

        $('#tblSalesReturn tbody tr').each(function () {
            const $row = $(this);
            const packPercent = parseFloat($row.find('.pack-per').val()) || 0;

            if (!packPercent) {
                const amount = parseFloat($row.find('.amount').val()) || 0;
                const discountAmount = totalAmt ? amount * discAmt / totalAmt : 0;
                $row.find('.disc-amt').val(formatVal(discountAmount, 'AMT'));
            }
        });

        GRID_CALCULATER();
    } catch (err) {
        console.error('Discount Amount error:', err);
        showToast('Error while calculating Discount Amount.', { type: "error" });
    }
}
async function CalculateSaudaRate() {
    try {
        if (!validateRequiredField('#NumSaudaRate', 'Sauda Rate')) return;
        const saudaRate = parseFloat($('#NumSaudaRate').val()) || 0;
        const partyCode = parseInt($('#ddlPartyName').val()) || 0;

        if (saudaRate === 0) {
            return;
        }

        const itemCodes = [];

        $('#tblSalesReturn tbody tr').each(function () {
            const itemCode = parseInt($(this).find('.item-code').val()) || 0;

            if (itemCode > 0) {
                itemCodes.push(itemCode);
            }
        });

        if (!itemCodes.length) {
            return;
        }

        const response = await $.get('/SalesReturn/CalculateSaudaRate', {
            partyCode: partyCode,
            itemCodes: itemCodes.join(',')
        });

        if (!response.success) {
            showToast(response.message || 'Error calculating Sauda rate.', {
                type: 'error'
            });
            return;
        }

        const saudaMap = new Map(
            (response.data || []).map(item => [
                String(item.iteM_CODE),
                item
            ])
        );

        $('#tblSalesReturn tbody tr').each(function () {
            const $row = $(this);
            const itemCode = parseInt($row.find('.item-code').val()) || 0;

            if (itemCode <= 0) {
                return;
            }

            const item = saudaMap.get(String(itemCode));

            if (!item) {
                return;
            }

            const rate =
                saudaRate +
                (parseFloat(item.sizE_DIFF) || 0) +
                (parseFloat(item.coloR_DIFF) || 0) +
                (parseFloat(item.graM_DIFF) || 0) +
                (parseFloat(item.iteM_DIFF) || 0);

            $row.find('.rate')
                .val(rate)
                .prop('readonly', true);

            $row.find('.sauda-rate').val(saudaRate);
        });

        GRID_CALCULATER();

    } catch (err) {
        console.error('CalculateSaudaRate error:', err);
        showToast(err.message || 'Error while calculating Sauda rate.', { type: 'error' });
    }
}

//==============Wb Qty==========
async function getWBQty(wbno) {
    try {
        if (wbno && wbno.trim().length > 0) {
            const response = await $.get('/SalesReturn/GetWBWeight', {
                wbDocId: wbno
            });

            if (response.success) {
                $('#NumWBQty').val((response.data || 0).toFixed(4));
            } else {
                showToast(response.message || 'Unable to get WB quantity', { type: "error" });
                $('#NumWBQty').val(0);
            }
        } else {
            $('#NumWBQty').val(0);
        }
    } catch (err) {
        console.error(err);
        showToast(err.message || 'Error occurred', { type: "error" });
        $('#NumWBQty').val(0);
    }
}


//==============Packing Data============
async function validatePackNo(docId) {
    try {
        if (!docId) {
            $('#ddlPackNo').val('');
            return;
        }

        const packType = docId.substring(0, 4);
        const packNo = parseInt(docId.substring(4)) || 0;

        if (!packNo) {
            showToast('Invalid Packing Slip No.', { type: 'error' });
            return;
        }

        const response = await $.get('/SalesReturn/GetPackingData', {
            packType: packType,
            packNo: packNo,
            vType: $('#ddlDocumentType').val(),
            vNo: parseInt($('#NumDocumentNo').val()) || 0
        });

        if (!response.success) {
            showToast(response.message || 'Error occurred', { type: 'error' });
            return;
        }

        if (response.exists) {
            showToast(`This Packing Slip No. already Exist on Serial No. ${response.existingVNo}`, { type: 'error' });
            return;
        }

        console.log("Packing details: ", response.data);


        if (!response.data || response.data.length === 0) {
            showToast('Invalid Entry', { type: 'error' });
            $('#ddlPackNo').val('').trigger('change');
            return;
        }

        const taxType = $('#ddlTaxType').val();

        const hasTax = (parseFloat($('#NumOtherCESS1').val()) || 0) > 0 || (parseFloat($('#NumOtherCGST1').val()) || 0) > 0 || (parseFloat($('#NumOtherIGST1').val()) || 0) > 0 ||
            (parseFloat($('#NumOtherSGST1').val()) || 0) > 0;

        for (const item of response.data) {
            if (checkDuplicatePackingItem(item.iteM_CODE, packNo, item.loT_No)) {
                Swal.fire({
                    icon: 'warning',
                    title: 'Duplicate Packing Slip',
                    html: `This Packing Slip No. already Exist in Grid of Item :(<b>${item.iteM_CODE}) ${item.iteM_NAME}</b>. Item Can not added again with same Slip No.`,
                    confirmButtonText: 'OK'
                });

                return;
            }

            //if (taxType && hasTax && (parseInt(item.iteM_CODE) || 0) > 0) {
            //    item.TAX_CODE = taxType;
            //} else {
            //    item.TAX_CODE = '';
            //}

            await addNewRowBelow(item);
        }

        gridFunction();
    } catch (err) {
        console.error(err);
        showToast(err.message || 'Error occurred', { type: 'error' });
    }
}
function checkDuplicatePackingItem(itemCode, packNo, lotNo) {
    const targetItem = String(itemCode ?? '').trim();
    const targetPack = String(packNo ?? '').trim();
    const targetLot = String(lotNo ?? '').trim();

    let duplicate = false;

    $('#tblSalesReturn tbody tr').each(function () {
        const rowItem = String($(this).find('.item-code').val() ?? '').trim();
        const rowPack = String($(this).find('.pack-no').val() ?? '').trim();
        const rowLot = String($(this).find('.lot-no').val() ?? '').trim();

        console.log({
            target: {
                item: targetItem,
                pack: targetPack,
                lot: targetLot
            },
            row: {
                item: rowItem,
                pack: rowPack,
                lot: rowLot
            }
        });

        if (
            rowItem === targetItem &&
            rowPack === targetPack &&
            rowLot === targetLot
        ) {
            duplicate = true;
            return false;
        }
    });

    return duplicate;
}


//==============Gate Data============
async function GetGateDetails(gateDocId) {
    try {
        isLoadByGateNo = true;

        if (!gateDocId || gateDocId.length <= 4) return;

        //const gateNo = parseInt(gateDocId.substring(4)) || 0;

        if (!gateDocId) {
            showToast('Invalid Gate No.', { type: 'error' });
            return;
        }

        const response = await $.get('/SalesReturn/GetGateData', {
            GateDocId: gateDocId
        });

        if (!response.success) {
            showToast(response.message || 'Error occurred', { type: 'error' });
            return;
        }

        console.log("Gate Details: ", response);
        // Party Details
        if (response.party) {
            $('#ddlPartyName').val(response.party.party_code).trigger('change');
            $('#TxtAddressL1').val(response.party.add1);
            $('#TxtAddressL2').val(response.party.add2);
            $('#TxtAddressL3').val(response.party.add3);
            $('#Station').val(response.party.party_city);
            $('#NumPincode').val(response.party.party_Pincode);

            bindDropdown("SalesReturn", "address", '#ddladdressL1', '', null, null, true, response.party.party_code, false);
        }

        // Gate Items
        $('#tblSalesReturn tbody').empty();

        if (response.items && response.items.length > 0) {
            for (const item of response.items) {
                await addNewRowBelow(item);
            }
        }
        else {
            addNewRowBelow();
        }

        gridFunction();
    } catch (err) {
        console.error(err);
        showToast(err.message || 'Error occurred', { type: 'error' });
    }
    finally {
        isLoadByGateNo = false;
    }
}


//==============Validations============
async function validateData() {
    debugger;
    // 1. Basic validations
    if (!validateRequiredField('#ddlDocumentType', 'Invoice Type') || !validateRequiredField('#NumDocumentNo', 'Invoice No.')) {
        return false;
    }

    if (!await checkValidDate()) return false;

    if (!validateRequiredField('#ddlPartyName', 'Party Name')) return false;

    const vType = ($('#ddlDocumentType').val() || '').trim();
    const productType = ($('#ddlProductType').val() || '').trim();

    // Reference No.
    if (vType !== "SAJR") {
        if (!validateRequiredField('#ddlReferenceNo', 'Reference No.')) return false;
    }

    // Gate No.
    if (Number(compCode) !== 2 && ($('#ddlGateNo').val() || '').trim() === "") {
        setInvalid($('#ddlGateNo'), "Please select Gate Inward No.");
        return false;
    }

    // 2. Product Type / Packing validation
    let chkVal = false;

    if (String(compCode) === "1" && productType === "PSF")
        chkVal = true;
    else if ((String(compCode) === "2" || String(compCode) === "5") &&
        (productType === "Fabric" || productType === "Sacks"))
        chkVal = true;
    else if (String(compCode) === "4" && productType === "Finish")
        chkVal = true;

    if (chkVal) {
        const packingDocId = ($('#txtPackingNo').val() || '').trim();

        if (vType === "SAJR" && packingDocId === "") {
            setInvalid($('#txtPackingNo'), "Packing No. can not be Blank, Please Check it.");
            return false;
        }
    }

    // 3. Tax Type
    if (!validateRequiredField('#ddlTaxType', 'Tax Type')) return false;

    // 4. Grid validations
    const items = [];
    let gridValid = true;

    $('#tblSalesReturn tbody tr').each(function () {
        if (!gridValid) return;

        const row = $(this);
        const el = this;

        const itemCode = Number(row.find('.item-code').val() || 0);
        const itemName = el.querySelector('.item-name')?.selectedOptions[0]?.textContent.trim() || '';
        const quantity = Number(row.find('.net-qty').val() || 0);

        if (itemCode === 0) return;

        if (itemName === "") {
            setInvalid(row.find('.item-name'), "Item Name can not be Blank.");
            gridValid = false;
            return;
        }

        if (quantity === 0) {
            setInvalid(row.find('.net-qty'), `Quantity can not be Zero for Item ${itemName}.`);
            gridValid = false;
            return;
        }

        items.push({
            ItemCode: itemCode,
            ItemName: itemName,
            Quantity: quantity
        });
    });

    if (!gridValid) return false;

    // 5. Collect values
    const requestData = {
        GateDocId: ($('#ddlGateNo').val() || '').trim(),
        DocumentType: ($('#ddlDocumentType').val() || '').trim(),
        DocumentNo: Number($('#NumDocumentNo').val() || 0),
        PartyCode: Number($('#ddlPartyName').val() || 0),

        ReferenceDocId: $('#ddlReferenceNo').val() || '',
        //ReferenceNo: Number($('#ddlReferenceNo').val() || 0),

        PackingDocId: ($('#ddlPackNo').val() || '').trim(),
        ProductType: ($('#ddlProductionType').val() || '').trim(),

        Transport: ($('#ddlTransport').val() || '').trim(),
        GrNo: ($('#NumGRNo').val() || '').trim(),

        InvoiceDate: $('#DtDocumentDate').val(),

        CgstAmount: Number($('#NumOtherCGST2').val() || 0),
        SgstAmount: Number($('#NumOtherSGST2').val() || 0),
        IgstAmount: Number($('#NumOtherIGST2').val() || 0),

        GrossWeight: Number($('#NumGrossQty').val() || 0),
        TruckNo: ($('#NumTruckNo').val() || '').trim(),
        WbNo: ($('#ddlWBNo').val() || '').trim(),

        Items: items
    };

    // 6. Server-side / DB validations
    try {
        const response = await $.ajax({
            url: '/SalesReturn/ValidateData',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(requestData)
        });

        if (!response.success) {
            /*showToast(response.message, {type:"warning"});*/
            Swal.fire({
                icon: 'warning',
                title: 'Warning',
                text: response.message,
                confirmButtonText: 'OK'
            });
            return false;
        }

        return true;
    }
    catch (error) {
        console.error(error);
        const message = error.responseJSON?.message || "Error occurred while validating data.";
        showToast(message, { type: "error" });
        return false;
    }
}
async function checkValidDate() {
    const data = {
        vdate: $("#DtDocumentDate").val(),
        vtype: $("#ddlDocumentType").val(),
        vno: $("#NumDocumentNo").val()
    };
    try {
        const response = await fetch('/SalesReturn/CheckValidDate', {
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


//==============Save & Update=========
function getSalesReturnHeaderData() {
    const packValue = $('#ddlPackNo').val() || '';
    console.log("packValue: ", packValue);
    const wbValue = $('#ddlWBNo').val() || '';
    const saudaValue = $('#ddlSaudaNo').val() || '';
    const refValue = $('#ddlReferenceNo').val() || '';
    const gateValue = $('#ddlGateNo').val() || '';

    return {
        V_TYPE: $('#ddlDocumentType').val() || null,
        V_NO: Number($('#NumDocumentNo').val()) || 0,
        V_DATE: $('#DtDocumentDate').val() || null,

        BILL_CODE: Number($('#ddlPartyName').val()) || 0,
        BILL_NAME: $('#ddlPartyName option:selected').text() || null,
        BILL_ADD1: $('#TxtAddressL1').val() || null,
        BILL_ADD2: $('#TxtAddressL2').val() || null,
        BILL_ADD3: $('#TxtAddressL3').val() || null,
        BILL_CITY: Number($('#Station').val()) || null,
        BILL_CITYNAME: $('#Station option:selected').text() || null,
        BILL_GST: billGst,
        BILL_PINCODE: $('#NumPincode').val() || null,
        BILL_ADDRESSID: Number($('#ddladdressL1').val()) || null,

        SHIP_CODE: Number($('#ddlConsignee').val()) || null,
        SHIP_NAME: $('#ddlConsignee option:selected').text() || null,
        SHIP_ADD1: $('#TxtTransactionAddressL1').val() || null,
        SHIP_ADD2: $('#TxtTransactionAddressL2').val() || null,
        SHIP_ADD3: $('#TxtTransactionAddressL3').val() || null,
        SHIP_CITY: Number($('#TxtTransactionStation').val()) || null,
        SHIP_CITYNAME: $('#TxtTransactionStation option:selected').text() || null,
        SHIP_GST: shipGst,
        SHIP_PINCODE: $('#NumTransactionPIN').val() || null,
        SHIP_ADDRESSID: Number($('#ddlTransactionaddressL1').val()) || null,

        TAX_CODE: Number($('#ddlTaxType').val()) || null,

        PACK_NO: Number(packValue.substring(4)) || null,
        PACK_TYPE: packValue.substring(0, 4) || null,

        ITEM_TYPE: $('#ddlProductionType').val() || null,

        WB_NO: Number(wbValue.substring(4)) || null,
        WB_TYPE: wbValue.substring(0, 4) || null,

        AMOUNT: Number($('#NumOtherTotalAmount').val()) || 0,

        PACK_PER: Number($('#NumOtherPacking1').val()) || 0,
        PACK_AMT: Number($('#NumOtherPacking2').val()) || 0,

        CGST_PER: Number($('#NumOtherCGST1').val()) || 0,
        CGST_AMT: Number($('#NumOtherCGST2').val()) || 0,

        SGST_PER: Number($('#NumOtherSGST1').val()) || 0,
        SGST_AMT: Number($('#NumOtherSGST2').val()) || 0,

        IGST_PER: Number($('#NumOtherIGST1').val()) || 0,
        IGST_AMT: Number($('#NumOtherIGST2').val()) || 0,

        CESS_PER: Number($('#NumOtherCESS1').val()) || 0,
        CESS_AMT: Number($('#NumOtherCESS2').val()) || 0,

        LOAD_PER: Number($('#NumLoadAmount1').val()) || 0,
        LOAD_AMT: Number($('#NumLoadAmount2').val()) || 0,
        LOAD_AC: $('#ddlLoadParty').val() || null,
        LOAD_REM: $('#TxtRemarks1').val() || null,

        WB_AMT: Number($('#NumWBAmount').val()) || 0,
        WB_AC: $('#ddlWBParty').val() || null,
        WB_REM: $('#TxtRemarks2').val() || null,

        BUYER_ORDNO: null,

        TOT_NOS: Number($('#NumOtherTotalNos').val()) || 0,
        FRT_AMT: Number($('#NumConsFreight').val()) || 0,
        ROUND_OFF: Number($('#NumOtherRoundOff').val()) || 0,
        NAMOUNT: Number($('#NumOtherNetAmount').val()) || 0,

        INSU_PER: Number($('#NumInsurance1').val()) || 0,
        INSU_AMT: Number($('#NumInsurance2').val()) || 0,

        TCS_PER: Number($('#NumOtherTCS1').val()) || 0,
        TCS_AMT: Number($('#NumOtherTCS2').val()) || 0,

        TDS_PER: Number($('#NumTDSFreight1').val()) || 0,
        TDS_AMT: Number($('#NumTDSFreight2').val()) || 0,

        TOT_NET: Number($('#NumNetQty').val()) || 0,
        TOT_GROSS: Number($('#NumGrossQty').val()) || 0,

        GR_NO: $('#NumGRNo').val() || null,
        GR_DATE: getOptionalDate('#chkGRDate', '#DtGRdate'),

        VEHICLE_NO: $('#NumTruckNo').val() || null,
        TRANSPORT_CODE: Number($('#ddlTransport').val()) || null,
        TRANSPORT_NAME: $('#ddlTransport').val() === "" ? "" : $('#ddlTransport').find("option:selected").text().trim(),

        DRIVER_NAME: $('#txtDriverName').val() || null,
        DRIVER_NO: $('#NumMobileNo').val() || null,

        WAYBILL_NO: $('#NumWayBillNo').val() || null,
        FRT_TOPAY: Number($('#NumPayFreight').val()) || 0,

        REMARK: $('#TxtRemarks3').val() || null,

        WB_QTY: Number($('#NumWBQty').val()) || 0,

        DISC_PER: Number($('#NumOtherDiscount1').val()) || 0,
        DISC_AMT: Number($('#NumOtherDiscount2').val()) || 0,

        AGENT_CODE: Number($('#ddlSaleThrough').val()) || null,
        FORM_CODE: Number($('#ddlFormType').val()) || null,

        SAUDA_TYPE: saudaValue.substring(0, 4) || null,
        SAUDA_NO: Number(saudaValue.substring(4)) || null,
        SAUDA_RATE: Number($('#NumSaudaRate').val()) || 0,

        CAL_ONPCS: $('#ChkPCS').is(':checked') ? 1 : 0,

        GATE_TYPE: gateValue.substring(0, 4) || null,
        GATE_NO: Number(gateValue.substring(4)) || null,

        REF_TYPE: refValue.substring(0, 4) || null,
        REF_NO: Number(refValue.substring(4)) || null,
        REF_DATE: getOptionalDate('#chkReferenceDate', '#DtReferenceDate'),

        INSU_DETAIL: null,
        SHIPMENT_TYPE: null,

        TRAN_TYPE: $('#ddlTransactionThrough').val() || null,
        SUPPLY_TYPE: $('#ddlSupplyType').val() || null,

        BILL_STATE: null,
        BILL_COUNTRY: null,
        BILL_STATENAME: null,
        BILL_COUNTRYNAME: null,

        GODOWN_CODE: null,
        SHIP_STATE: null,
        SHIP_COUNTRY: null,
        SHIP_STATENAME: null,
        SHIP_COUNTRYNAME: null,

        IMPORT_CURRENCY: null,
        EXRATE: null,

        DEFECTIVE_GOODS: null,

        PRINT_DETAIL: null,

        FRT_BILLNO: null,
        FRT_BILLDT: null,
        FRT_PASSDT: null,
        FRT_CHQ: null,
        FRT_REMARK: null,

        TPT_MODE: null,
        TPT_DISTANCE: null,

        INSU_TYPE: null,

        INSU_NO: null,

        ORD_AMT: null,
        COMM_RATE1: null,
        COMM_RATE2: null,
        GST_RATE: null,
        TDS_RATE: null,

        STATUS: null,
        RCM_NO: null,
        PAYREF_DOCID: null,
        PAY_AMT: null,

        ISSUE_TYPE: null,
        ISSUE_NO: null,
        PLACE_RECEIPT: null,
        PORT_LOADING: null,
        PORT_DISCHARGE: null,
        FINAL_DEST: null,
        FINAL_DEST_COUNTRY: null,
        DELIVERY_TERMS: null,
        LUT_DETAIL: null,

        COND_DATE: null,
        COND_MNTH: null,
        APPROVAL_USER: null,

        IRN: null,
        SIGNED_JSON: null,
        SIGNED_QR: null,
        EINVOICE_FLG: null,
        EWAYBILL_FLG: null,
        EWAYBILL_NO: null,
        EWAYBILL_JSON: null,
        EWAYBILL_DATE: null,
        EWAYBILL_VALIDDATE: null,

        CDISC_AMT: null,
        CDISC_PER: null,

        LC_NO: null,
        TRADE_TERM: null,
        DISP_PLACE: null,
        MODEOF_PAYMENT: null,
        INCOTERM: null,

        FRT_TAXPER: null,
        FRT_TAXAMT: null,

        SB_NO: null,
        SB_DATE: null,
        PORT_CODE: null,
        DEL_DATE: null,

        FOB_VALUE: null,
        FOB_FRT: null,
        FOB_INSU: null,
        FOB_OTHER: null,
        BILLOF_LADING: null,
        EXPV_TYPE: null,
        EXPV_NO: null,
        SHIP_TYPE: null,
        CURRENCY: null,
        BANK_CODE: null,
        DEL_SCH: null,
        LUT_NO: null,
        LUT_DATE: null,
        PAY_TERM: null,
        SOLD_BY: null,
        INV_STATUS: null,
        INSUCR_DAYS: null,
        CONTAINER_SIZE: null,
        PAYMENT_TERM: null,
        LICENCE_NO: null,
        LUL_BILLNO: null,
        LUL_BILLDT: null,
        LUL_PASSDT: null,
        LUL_CHQ: null,
        LUL_REMARK: null,
        LICENCE_TYPE: null,
        LICENCE_DATE: null,

        ACTION: (rowId || !isNaN(rowId)) ? "UPDATE" : "INSERT"
    };
}
function getSalesReturnGridData() {
    const rows = [];
    const packValue = $('#ddlPackNo').val() || '';
    const packType = packValue.substring(0, 4) || null;

    $('#tblSalesReturn tbody tr').each(function () {
        const row = $(this);

        rows.push({
            ITEM_CODE: Number(row.find('.item-code').val()) || 0,
            //ITEM_NAME: row.find('.item-name option:selected').text() || null,
            //UNIT_NAME: null,
            //UNIT_CODE: null,
            HSN_CODE: row.find('.hsn').val() || null,

            NOS: Number(row.find('.nos').val()) || 0,
            QTY: Number(row.find('.net-qty').val()) || 0,
            GROSS_QTY: Number(row.find('.gross-qty').val()) || 0,

            RATE: Number(row.find('.rate').val()) || 0,
            AMOUNT: Number(row.find('.amount').val()) || 0,

            PACK_PER: Number(row.find('.pack-per').val()) || 0,
            PACK_AMT: Number(row.find('.pack-amt').val()) || 0,

            DISC_PER: Number(row.find('.disc-per').val()) || 0,
            DISC_AMT: Number(row.find('.disc-amt').val()) || 0,

            TAX_CODE: Number(row.find('.tax-code').val()) || null,

            CGST_PER: Number(row.find('.cgst-per').val()) || 0,
            CGST_AMT: Number(row.find('.cgst-amt').val()) || 0,

            SGST_PER: Number(row.find('.sgst-per').val()) || 0,
            SGST_AMT: Number(row.find('.sgst-amt').val()) || 0,

            IGST_PER: Number(row.find('.igst-per').val()) || 0,
            IGST_AMT: Number(row.find('.igst-amt').val()) || 0,

            CESS_PER: Number(row.find('.cess-per').val()) || 0,
            CESS_AMT: Number(row.find('.cess-amt').val()) || 0,

            REMARK: row.find('.remarks').val() || null,

            PACK_TYPE: packType,
            PACK_NO: Number(row.find('.pack-no').val()) || null,

            LOT_No: row.find('.lot-no').val() || null,

            ORD_TYPE: row.find('.order-type').val() || null,
            ORD_NO: Number(row.find('.order-no').val()) || null,
            ORD_RATE: Number(row.find('.order-rate').val()) || 0,

            SAUDA_TYPE: row.find('.sauda-type').val() || null,
            SAUDA_NO: Number(row.find('.sauda-no').val()) || null,
            SAUDA_RATE: Number(row.find('.sauda-rate').val()) || 0,

            DCN_TYPE: row.find('.dcn-type').val() || null,
            DCN_NO: Number(row.find('.dcn-no').val()) || null,

            LAND_RATE: null,
            LAND_AMT: null,
            GATE_QTY: null,
            FOR_RATE: null,
            DEPT_CODE: null,
            FINAL_LOCK: null,
            STATUS: null,
            CDISC_AMT: null,
            INSU_AMT: null,
            FRT_AMT: null,
            WBQTY: null,
            FEXCH_USD: null,
            ROW_ID: null,
            GATE_INQTY: null,
            MIS_GROUP: null,
            FREIGHT_AMT: null,
            PROD_DESC: null,
            MIS_GRP: null
        });
    });

    return rows;
}
async function saveSalesReturn() {
    const requestData = {
        FormData: getSalesReturnHeaderData(),
        RowData: getSalesReturnGridData()
    };
    console.log("requestData: ", requestData);
    try {
        const res = await fetch('/SalesReturn/Save', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestData)
        });

        const response = await res.json();
        console.log("Save & Update Response: ", response);
        if (!res.ok || !response.success) {
            showToast(response.message || 'Error occurred', { type: 'error' });
            return false;
        }

        const docType = ($('#ddlDocumentType').val() || '').trim();
        const docCode = parseInt($('#NumDocumentNo').val() || 0);

        setTimeout(() => window.location.href = `/SalesReturn/Index?id=${encodeURIComponent(docCode)}&vtype=${encodeURIComponent(docType)}&readOnly=true`, 1000);
        setFormReadOnly();
        showToast(response.message || 'Sales Return saved successfully', { type: 'success' });
        return true;
    } catch (error) {
        console.error(error);
        showToast('Error occurred while saving Sales Return', { type: 'error' });
        return false;
    }
}


//==============Edit & View============
async function GetSalesReturnById(id, vType) {
    try {
        const res = await $.ajax({
            url: '/SalesReturn/GetID',
            type: 'GET',
            data: { id, vType }
        });

        if (!res.success) {
            showToast(res.message || 'Unable to load Sales Return', { type: "warning" });
            return;
        }

        console.log("Get By Id: ", res);

        if (res.header?.length && res.items?.length) {
            await bindSalesReturn(res);
        }
    } catch (xhr) {
        showToast(xhr.responseJSON?.message || 'Error while loading Sales Return', { type: "error" });
    }
}
async function bindSalesReturn(res) {
    try {
        isLoadById = true;
        const h = res.header[0];
        // Document
        $('#NumDocumentNo').val(h.v_NO);
        $('#DtDocumentDate').val(h.v_DATE?.split('T')[0] ?? '');
        $('#ddlDocumentType').val(h.v_TYPE);
        $('#ddlTransactionThrough').val(h.traN_TYPE);
        $('#ddlSupplyType').val(h.supplY_TYPE);

        // Transaction
        $('#ddlTaxType').val(h.taX_CODE).trigger('change');
        $('#ddlProductionType').val(h.iteM_TYPE);
        $('#ddlReferenceNo').val(h.reF_TYPE + h.reF_NO).trigger('change');
        setDateControl(h.reF_DATE, '#DtReferenceDate', '#chkReferenceDate');
        bindDropdown("SalesReturn", "gate", '#ddlGateNo', '--Select Gate No--', h.gatE_TYPE + h.gatE_NO, null, false, h.v_TYPE, true);
        $('#ddlWBNo').val(h.wB_TYPE + h.wB_NO).trigger('change');
        //$('#ddlPackNo').val(h.pacK_TYPE + h.pacK_NO).trigger('change');
        await ddlPackNo(h.v_TYPE, h.bilL_CODE, h.pacK_TYPE + h.pacK_NO);
        $('#ddlSaudaNo').val(h.saudA_TYPE + h.saudA_NO).trigger('change');
        $('#NumSaudaRate').val(h.saudA_RATE);
        $('#ChkPCS').prop('checked', h.caL_ONPCS == 1);

        // Party
        $('#ddlPartyName').val(h.bilL_CODE).trigger('change');
        $('#TxtAddressL1').val(h.bilL_ADD1);
        $('#TxtAddressL2').val(h.bilL_ADD2);
        $('#TxtAddressL3').val(h.bilL_ADD3);
        $('#Station').val(h.bilL_CITY).trigger('change');
        $('#NumPincode').val(h.bilL_PINCODE);

        // Consignee
        $('#ddlSaleThrough').val(h.agenT_CODE).trigger('change');
        $('#ddlConsignee').val(h.shiP_CODE).trigger('change');
        $('#TxtTransactionAddressL1').val(h.shiP_ADD1);
        $('#TxtTransactionAddressL2').val(h.shiP_ADD2);
        $('#TxtTransactionAddressL3').val(h.shiP_ADD3);
        $('#TxtTransactionStation').val(h.shiP_CITY).trigger('change');
        $('#NumTransactionPIN').val(h.shiP_PINCODE);
        $('#ddlFormType').val(h.forM_CODE).trigger('change');

        // Amount
        $('#NumOtherTotalAmount').val(h.amount);
        $('#NumOtherTotalNos').val(h.toT_NOS);

        $('#NumOtherPacking1').val(h.pacK_PER);
        $('#NumOtherPacking2').val(h.pacK_AMT);

        $('#NumOtherCESS1').val(h.cesS_PER);
        $('#NumOtherCESS2').val(h.cesS_AMT);

        $('#NumOtherDiscount1').val(h.disC_PER);
        $('#NumOtherDiscount2').val(h.disC_AMT);

        $('#NumOtherTCS1').val(h.tcS_PER);
        $('#NumOtherTCS2').val(h.tcS_AMT);

        $('#NumOtherSubTotal').val(h.amount);
        $('#NumOtherRoundOff').val(h.rounD_OFF);

        $('#NumOtherCGST1').val(h.cgsT_PER);
        $('#NumOtherCGST2').val(h.cgsT_AMT);

        $('#NumOtherNetAmount').val(h.namount);

        $('#NumOtherSGST1').val(h.sgsT_PER);
        $('#NumOtherSGST2').val(h.sgsT_AMT);

        $('#NumInsurance1').val(h.insU_PER);
        $('#NumInsurance2').val(h.insU_AMT);

        $('#NumOtherIGST1').val(h.igsT_PER);
        $('#NumOtherIGST2').val(h.igsT_AMT);

        $('#NumConsFreight').val(h.frT_AMT);

        $('#NumTDSFreight1').val(h.tdS_PER);
        $('#NumTDSFreight2').val(h.tdS_AMT);

        // Quantity / Freight
        $('#NumGrossQty').val(h.toT_GROSS);
        $('#NumNetQty').val(h.toT_NET);
        $('#NumPayFreight').val((h.frT_TOPAY || 0).toFixed(4));
        $('#NumWBQty').val(h.wB_QTY);

        $('#NumLoadAmount1').val(h.loaD_PER);
        $('#NumLoadAmount2').val(h.loaD_AMT);

        $('#NumWayBillNo').val(h.waybilL_NO);
        $('#ddlLoadParty').val(h.loaD_AC).trigger('change');
        $('#TxtRemarks1').val(h.loaD_REM);

        $('#NumWBAmount').val(h.wB_AMT);
        $('#TxtRemarks2').val(h.wB_REM);
        $('#ddlWBParty').val(h.wB_AC).trigger('change');

        // Transport
        $('#NumGRNo').val(h.gR_NO);
        //$('#DtGRdate').val(h.gR_DATE ? new Date(h.gR_DATE).toISOString().split('T')[0] : '');
        setDateControl(h.gR_DATE, '#DtGRdate', '#chkGRDate');
        $('#ddlTransport').val(h.transporT_CODE).trigger('change');
        $('#NumTruckNo').val(h.vehiclE_NO);
        $('#txtDriverName').val(h.driveR_NAME);
        $('#NumMobileNo').val(h.driveR_NO);
        $('#TxtRemarks3').val(h.remark);

        // Hidden
        $('#TxtCode').val(h.v_NO);

        //$('select').trigger('change');


        if (h.irn !== "") {
            $('#lbl-e-invoice').show();
        }

        if (h.status === 2) {
            $('#lbl-cancelled').show();
        }


        if (h.einvoicE_FLG > 0) {
            $('#btn-EditTptData').show();
        }
        updateTptButtons();

        const items = res.items || []
        $tbody.empty();
        for (const item of items) {
            await addNewRowBelow(item);
        }

        gridFunction();
    } catch (ex) {
        console.error(ex);
        showToast("Error occurred while loading Data.", { type: "error" })
    }
    finally {
        isLoadById = false;
        if (isReadOnly) {
            setFormReadOnly();
        }
    }
}
function setFormReadOnly() {
    const form = $('#SalesReturnForm');
    form.addClass('erppage-readonly');
    $('#btn-save').hide();
    $('#btn-Calculator').closest('span')
        .addClass('disabled')
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed',
            //'opacity': '0.5'
        });

    $('#SalesReturnForm input[type="checkbox"]').prop('disabled', true);
    $('#tblSalesReturn .btn-delete-action, #tblSalesReturn .btn-add-action')
        .prop('disabled', true)
        .css({
            'pointer-events': 'none',
            'cursor': 'not-allowed'
        });
}


//==============Global Variables=========
async function getGlobalValues() {
    try {
        const response = await $.ajax({
            url: "/SalesReturn/GetGlobalValues",
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
            pubBPTCSPer = d.tcsper;
            compPhone = d.phone;
            compGSTIN = d.gst;
            compPAN = d.pan;
            compWebsite = d.website;
            compEmail = d.email;
            compRegAdd1 = d.regadd1;
            compCIN = d.cin;
            compRegAdd2 = d.regadd2;
            pubUserLevel = d.userlevel;
        } else {
            showToast(response.message || "Failed to load global values.", { type: "error" });
        }
    } catch (error) {
        console.error("Error loading global values:", error);
        showToast("An error occurred while loading global values.", { type: "error" });
    }
}


//===============Approval===============
$(document).on('click', '#btn_Sendapproval', function () {
    var FromName = window.location.pathname.split('/')[1];
    $.ajax({
        url: '/Approval/CheckPendingUser',
        type: 'POST',
        data: {
            vNo: $('#NumDocumentNo').val(),
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
                    DocNo: $('#NumDocumentNo').val(),
                    TableName: DBTableName
                });
                return;
            }
            // Approval_Code != 8
            OpenSendForApprovalModal({
                DocType: rowIdVType,
                DocNo: $('#NumDocumentNo').val(),
                UserCode: null,
                UserName: null,
                DocDate: null,
                TableName: DBTableName,
                FromName, FromName
            });

        },
        error: function (xhr, status, error) {
            console.log(error);
            showToast('Error while checking approval status.', { type: "error" });
        }
    });

});
$(document).on('click', '#btn_Approved', function () {
    OpenApprovalModal({
        DocType: rowIdVType,
        DocNo: $('#NumDocumentNo').val(),
        TableName: DBTableName
    });
});


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


//===============Report================
async function SalesReturnReport() {
    try {
        var vType = ($("#ddlDocumentType").val() || "").trim();
        var vNo = ($("#NumDocumentNo").val() || "").trim();

        if (!vType || !vNo) {
            showToast("Voucher Type and Voucher No are required.", { type: "warning" });
            return;
        }

        var freight = parseFloat($("#NumConsFreight").val()) || 0;
        var tdsAmt = parseFloat($("#NumTDSFreight2").val()) || 0;
        var netAmt = parseFloat($("#NumOtherNetAmount").val()) || 0;

        // Posting required when Freight/TDS exists
        if (freight > 0 || tdsAmt > 0) {
            var postingResponse = await fetch(
                `/SalesReturnList/PostSalesReturn?vType=${encodeURIComponent(vType)}&vNo=${vNo}`
            );

            var postingResult = await postingResponse.json();

            if (!postingResult.success) {
                await Swal.fire({
                    icon: "warning",
                    title: "Report not display",
                    text: postingResult.message || "Voucher not posted.",
                    confirmButtonText: "OK"
                });
                return;
            }
        }

        var reportName = "";

        if (vType === "SAJR") {
            reportName = compCode == 3 ? "rptJW" : "INVOICE_ChallanSale";
        }
        else {
            reportName = "INVOICE_GSTRETQR";
        }

        var formula =
            "{SALE1.V_TYPE} = '" + vType + "'" +
            " AND {SALE1.V_NO} = " + vNo +
            " AND {SALE1.COMP_CODE} = " + compCode +
            " AND {SALE1.YEAR_CODE} = " + yearCode +
            " AND {SALE1.BRANCH_CODE} = " + branchCode;

        var rptName = vType === "SAJR" ? "JOBWORK RECEIVED CHALLAN" : "SALES RETURN / CREDIT NOTE";

        var formulaFields = {
            Reportname: reportName,
            selectionFormula: formula,
            Database: db,

            Parameters: {
                RPTNAME: rptName,
                comp_name: companyName,
                comp_add1: add1,
                comp_add2: add2,
                comp_phone: "Mobile :" + compPhone,
                GST: "GSTIN       :" + compGSTIN,
                PAN: "PAN NO.   :" + compPAN,
                Website: "Web  :" + compWebsite,
                EMAIL: "Email   :" + compEmail,
                Comp_reg: (compRegAdd1 || "").trim() && (compCIN || "").trim()
                    ? "Reg.Office :" + compRegAdd1 + ", " + compRegAdd2 + "  CIN :" + compCIN
                    : "",
                INWORD: numToWord(netAmt, "Rs.", "PAISE")
            }
        };

        console.log("Report Data:", formulaFields);

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
    catch (ex) {
        console.error("Error generating report:", ex);
    }
}


//===============Duplicate============
function checkDuplicateItems(currentSelect) {

    const value = currentSelect.value;
    if (!value) return false;

    let duplicate = false;

    document.querySelectorAll('#tblSalesReturn .item-name').forEach(el => {
        if (el === currentSelect)
            return;
        if (el.value === value)
            duplicate = true;
    });

    if (duplicate) {
        $(currentSelect).addClass("is-invalid");
        showToast("Duplicate item found!", { type: "warning" });
    } else {
        $(currentSelect).removeClass("is-invalid");
    }

    return duplicate;
}


//===============Grid Disable Method====
function gridFunction() {
    try {
        //if ($('#lblAction').data('tag') == 2)
        //    return;

        $('#tblSalesReturn tbody tr').each(function () {
            const row = $(this);

            const packNo = row.find('.pack-no').val();
            const rate = row.find('.rate').val();

            if (packNo && rate) {
                row.find('.item-name').prop('disabled', true);
                row.find('.nos').prop('readonly', true);
                row.find('.gross-qty').prop('readonly', true);
                row.find('.net-qty').prop('readonly', true);
                row.find('.rate').prop('readonly', true);
                row.find('.amount').prop('disabled', true);
            }
        });
    }
    catch (error) {
        console.error(error);
        showToast(error.message, { type: "error" });
    }
}

//==============formatVal==========
function formatVal(value, type) {
    const n = parseFloat(value);
    const v = isNaN(n) ? 0 : n;

    switch ((type || '').toUpperCase()) {
        case 'AMT': return v.toFixed(2);
        case 'RATE': return v.toFixed(4);
        case 'QTY': return v.toFixed(2);
        case 'AMTSALE': return v.toFixed(2);
        default: return v.toFixed(2);
    }
}


//============== TRansport Edit=======
function updateTptButtons() {
    const show = canEditTpt;
    $('#btn-EditTptData').prop('disabled', !show);
}
const TPT_FIELDS = [
    '#NumConsFreight',   // txtcons_freight
    '#NumTDSFreight1',   // txttds_p
    '#NumTDSFreight2',   // txttds_amt
    '#NumPayFreight',    // txttopay_frt
    '#NumLoadAmount2',   // Txtload_amt
    '#NumLoadAmount1',   // txtload_p
    '#NumWayBillNo',     // txtway_billno
    '#NumWBQty',         // txtt_wb
    '#ddlLoadParty',     // cmbLoadingParty
    '#TxtRemarks2',      // txtreamrk2
    '#TxtRemarks1',      // txtremark1
    '#NumWBAmount',      // txtwb_amt
    '#ddlWBParty',       // cmbWBParty
    '#NumGRNo',          // txtgr_no
    '#ddlTransport',     // txttransport
    '#NumTruckNo',       // txttruck
    '#txtDriverName',    // txtdriver
    '#NumMobileNo',      // txtdriver_no
    '#TxtRemarks3',      // txtremark3
    '#NumInsurance1',    // txtins_p
    '#NumInsurance2'     // txtins_amt
];
function daysSinceVoucher() {
    const parts = ($('#DtDocumentDate').val() || '').split('-');
    if (parts.length !== 3) return 0;

    const v = new Date(+parts[0], +parts[1] - 1, +parts[2]);
    const t = new Date();
    const today = new Date(t.getFullYear(), t.getMonth(), t.getDate());

    return Math.round((today - v) / 86400000);
}


//==============Save Transport Data==========
const TPT_SELECT2 = ['#ddlLoadParty', '#ddlWBParty', '#ddlTransport'];
function getTptData() {
    const transportCode = Number($('#ddlTransport').val()) || 0;

    return {
        VType: ($('#ddlDocumentType').val() || '').trim(),
        VNo: Number($('#NumDocumentNo').val()) || 0,

        FrtAmt: Number($('#NumConsFreight').val()) || 0,
        TdsPer: Number($('#NumTDSFreight1').val()) || 0,
        TdsAmt: Number($('#NumTDSFreight2').val()) || 0,
        FrtToPay: Number($('#NumPayFreight').val()) || 0,

        LoadPer: Number($('#NumLoadAmount1').val()) || 0,
        LoadAmt: Number($('#NumLoadAmount2').val()) || 0,
        LoadAc: Number($('#ddlLoadParty').val()) || 0,
        LoadRem: $('#TxtRemarks1').val() || '',

        WbAmt: Number($('#NumWBAmount').val()) || 0,
        WbAc: Number($('#ddlWBParty').val()) || 0,

        GrNo: ($('#NumGRNo').val() || '').trim(),
        GrDate: getOptionalDate('#chkGRDate', '#DtGRdate'),

        TransportCode: transportCode,
        TransportName: transportCode ? $('#ddlTransport option:selected').text().trim() : '',
        VehicleNo: ($('#NumTruckNo').val() || '').trim(),
        DriverName: ($('#txtDriverName').val() || '').trim(),
        DriverNo: ($('#NumMobileNo').val() || '').trim(),
        Remark: $('#TxtRemarks3').val() || '',

        InsuPer: Number($('#NumInsurance1').val()) || 0,
        InsuAmt: Number($('#NumInsurance2').val()) || 0
    };
}
function lockTptFields() {
    TPT_FIELDS.forEach(sel => {
        $(sel).prop('disabled', true).removeClass('tpt-editable');
    });

    TPT_SELECT2.forEach(sel => {
        const $el = $(sel);
        $el.prop('disabled', true).trigger('change.select2');
        ($el.data('select2') ? $el.data('select2').$container : $el.nextAll('.select2-container').first())
            .removeClass('tpt-editable');
    });

    $('#chkGRDate').prop('disabled', true).removeClass('tpt-editable');
    $('#DtGRdate').prop('disabled', true).removeClass('tpt-editable');

    $('#btn-EditTptData').prop('disabled', false);
    $('#btn-SaveTptData').prop('disabled', true);
}
async function saveTptDetails() {
    const $btn = $('#btn-SaveTptData');
    if ($btn.prop('disabled')) return;

    const gross = Number($('#NumGrossQty').val()) || 0;
    const itemType = ($('#ddlProductionType').val() || '').trim();

    if (itemType !== 'Other' && gross > 500 && !($('#NumTruckNo').val() || '').trim()) {
        setInvalid($('#NumTruckNo'), 'Truck No. can not be Blank.');
        return;
    }

    $btn.prop('disabled', true);

    try {
        const res = await fetch('/SalesReturn/SaveTransport', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(getTptData())
        });

        const response = await res.json();

        if (!res.ok || !response.success) {
            showToast(response.message || 'Error occurred', { type: 'error' });
            $btn.prop('disabled', false);
            return;
        }

        lockTptFields();

        if (response.warnings && response.warnings.length) {
            await Swal.fire({
                icon: 'warning',
                title: 'Warning',
                html: response.warnings.map(w => `<p>${w}</p>`).join(''),
                confirmButtonText: 'OK'
            });
        }

        showToast(response.message || 'Transport data saved successfully.', { type: 'success' });

    } catch (err) {
        console.error(err);
        showToast('Error occurred while saving transport data.', { type: 'error' });
        $btn.prop('disabled', false);
    }
}