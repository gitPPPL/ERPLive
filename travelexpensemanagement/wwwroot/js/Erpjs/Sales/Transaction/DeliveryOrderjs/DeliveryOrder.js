const urlParams = new URLSearchParams(window.location.search);
const id = urlParams.get('id');
const vtype = urlParams.get('vType');
let isReadOnly = urlParams.get('readOnly') === 'true';
 
let deliveryOrderCalcSettings = null;
let isDistributingInsurance = false;

$(document).ready(async function () {

    await BindVoucherType();
    
    await WireEvent();
    await BindPartyNameDDL();
    await BindConsigneeDDL();
    await BindBrokerDDL();
    await BindStationDDL();
    await BindStationDDL1();
    await BindFoamType();
    await BindProdType();
    await BindStatus();
    await LoadDeliveryOrderCalcSettings();
    
    await BindWaybillList();
    await BindIssueVoucherList();
    await BindTransport();
    await addNewRow();

    $('#btn_save').on('click', async function (e) {
        e.preventDefault();
        await SaveDeliveryOrder();
    });

    if (id && vtype) {
        await LoadEditData(id, vtype);
    } else {
        await GetVNo();
        await GetStock(); 
    }

    // =========================
    // RECONCILE DELIVERY ORDER
    // =========================
    $('#btnReconcileDO').on('click', async function (e) {
        console.log("clicked");
        e.preventDefault();
        await ReconcileDeliveryOrder();
    });

});

async function WireEvent() {        

    SetCurrentDate();
     
    // =========================
    // PARTY NAME CHANGE
    // =========================
    $('#ddlPartyName').on('change', async function () {

        const partyCode = $(this).val();

        $('#TxtCode').val(partyCode || '');

        if (!partyCode) {

            $('#btnPartyInfo').prop('disabled', true);

            ClearPartyAddress();

            $('#ddlConsignee').val(null).trigger('change');

            ClearConsigneeAddress();

            return;
        }

        $('#btnPartyInfo').prop('disabled', false);

        await LoadPartyCreditDetails(partyCode);
        
        await BindPartyAddressDDL(partyCode);

        await LoadPartyLastSaleDetails(partyCode);

        const partyText = $('#ddlPartyName option:selected').text();

        if ($('#ddlConsignee option[value="' + partyCode + '"]').length === 0) {

            const option = new Option(partyText, partyCode, true, true);

            $('#ddlConsignee').append(option).trigger('change');

        } else {

            $('#ddlConsignee').val(partyCode).trigger('change');
        }

    });
    
    // =========================
    // PARTY ADDRESS CHANGE
    // =========================
    $('#ddladdressL1').on('change', async function () {

        const address = $(this).find('option:selected').data('address');

        if (!address) {
            return;
        }

        $('#TxtAddressL1').val(address.add1 || '');
        $('#TxtAddressL2').val(address.add2 || '');
        $('#TxtAddressL3').val(address.add3 || '');
        $('#TxtGST').val(address.gstin || '');
        $('#NumPincode').val(address.pincode || '');

        const stationCode = address.cityCode;

        if (!stationCode) {

            $('#ddlStation').val('').trigger('change');

            return;
        }

        const stationValue = String(stationCode);
          
        if ($('#ddlStation option[value="' + stationValue + '"]').length === 0) {

            const stationName = address.city || '';

            const option = new Option(stationName, stationValue, true, true );

            $('#ddlStation').append(option).trigger('change');

        } else {

            $('#ddlStation').val(stationValue).trigger('change');
        }
    });

    // =========================
    // CONSIGNEE CHANGE
    // =========================
    $('#ddlConsignee').on('change', async function () {

        const consigneeCode = $(this).val();

        if (!consigneeCode) {

            ClearConsigneeAddress();

            return;
        }

        await BindConsigneeAddressDDL(consigneeCode);
    });
    
    // =========================
    // CONSIGNEE ADDRESS CHANGE
    // =========================
    $('#ddlTransactionaddressL1').on('change', function () {

        const address = $(this).find('option:selected').data('address');

        if (!address) {
            return;
        }

        $('#TxtTransactionAddressL1').val(address.add1 || '');
        $('#TxtTransactionAddressL2').val(address.add2 || '');
        $('#TxtTransactionAddressL3').val(address.add3 || '');
        $('#NumTransactionGSTNo').val(address.gstin || '');
        $('#NumTransactionPIN').val(address.pincode || '');

        const stationCode = String(address.cityCode || '');
        
        if (!stationCode) {
            $('#TxtTransactionStation').val('');
            return;
        }

        const ddl = $('#TxtTransactionStation');

        if (ddl.find('option').filter(function () {
            return this.value === stationCode;
        }).length === 0) {

            const stationName = address.city || '';

            ddl.append(new Option(stationName, stationCode));
        }

        ddl.val(stationCode).trigger('change');
    });

    $('#btnPartyInfo').on('click', function () {

        const partyCode = $('#ddlPartyName').val();

        if (!partyCode) {
            return;
        }

        $('#partyInfoModal').modal('show');
    });

    $('#ddlStation').on('change', async function () {

        const stationCode = $(this).val();

        const taxDdl = $('#ddlTaxType');

        taxDdl.empty();
        taxDdl.append(new Option('Select Tax', ''));

        if (!stationCode) {
            return;
        }

        await BindTaxDDL(stationCode);
    });

    //========================================
    // Bind Pack No on behalf of party
    //========================================

    $('#ddlVoucherType, #ddlProdType, #DtBillDate').on('change', async function () {
        await BindPackingVoucherList();
    });

    $('#ddlPartyName').on('change', async function () {
        if ($('#ddlPartyName').val()) {
            await BindPackingVoucherList();
        }
    });

    //================================
    // Pending Details Button Click
    //================================
    $('#btnPendingDetails').on('click', async function () {

        const partyCode = $('#ddlPartyName').val();

        if (!partyCode) {
            showToast('Please select Party Name first', { type: 'warning' });
            return;
        }

        await BindPendingDeliveryOrder(partyCode);

        $('#copyFromModal').modal('show');
    });

    // ==============================
    // PENDING ORDERS: SELECT ALL
    // ==============================
    $('#selectAllPR').on('change', function () {
        $('.pending-order-checkbox:visible').prop('checked', this.checked);
    });

    // ========================================
    // PENDING ORDERS: INDIVIDUAL CHECKBOX
    // ========================================
    $('#tblDeliveryOrderPopUp tbody').on('change', '.pending-order-checkbox', function () {
        const $visibleCheckboxes = $('.pending-order-checkbox:visible');
        const total = $visibleCheckboxes.length;
        const checked = $visibleCheckboxes.filter(':checked').length;

        $('#selectAllPR').prop('checked', total > 0 && total === checked);
    });

    // =========================
    // PENDING ORDERS: SEARCH
    // =========================
    $('#searchBoxPR').on('input', function () {
        const searchText = $(this).val().toLowerCase().trim();
        const $tbody = $('#tblDeliveryOrderPopUp tbody');
        let visibleCount = 0;

        $tbody.find('tr').each(function () {
            const $row = $(this);

            if ($row.hasClass('no-record-found-row')) {
                return;
            }

            const isMatch = $row.text().toLowerCase().includes(searchText);

            $row.toggle(isMatch);

            if (isMatch) {
                visibleCount++;
            }
        });

        $tbody.find('.no-record-found-row').remove();

        if (visibleCount === 0) {
            $tbody.append(`
                <tr class="no-record-found-row">
                    <td colspan="15" class="text-center text-muted py-3">
                        <i class="fa fa-search me-1"></i>
                        No Record Found
                    </td>
                </tr>
            `);
        }

        const $visibleCheckboxes = $('.pending-order-checkbox:visible');
        $('#selectAllPR').prop('checked', $visibleCheckboxes.length > 0 && $visibleCheckboxes.filter(':checked').length === $visibleCheckboxes.length );
    });

    // ================================
    // COPY SELECTED PENDING ORDERS
    // ================================
    $('#btnCopyPendingOrders').on('click', async function () {

        const selectedOrders = [];

        $('#tblDeliveryOrderPopUp tbody .pending-order-checkbox:checked').each(function () {

            const item = $(this).closest('tr').data('pendingOrder');

            if (item) {
                selectedOrders.push(item);
            }
        });

        if (selectedOrders.length === 0) {
            showToast('Please select at least one pending order', { type: 'warning' });
            return;
        }

        const append = $('#chkAppendPR').is(':checked');
        const tbody = $('#tblDeliveryorder tbody');

        if (!append) {
            tbody.empty();
        } else {
           
            const $lastRow = tbody.find('tr:last');
            if ($lastRow.length && !$lastRow.find('.productName').val() && !$lastRow.find('.nos').val() && !$lastRow.find('.netQuantity').val()) {
                $lastRow.remove();
            }
        }

        try {

            for (const item of selectedOrders) {

                await addNewRow();

                const row = tbody.find('tr:last');

                const productDDL = row.find('.productName');
                const itemCode = String(item.code ?? '');

                let option = productDDL.find('option').filter(function () {
                    return String(this.value) === itemCode;
                });
                          
                if (option.length === 0 && itemCode) {
                    option = $(new Option( item.itemName ?? itemCode, itemCode, true,  true ));
                    productDDL.append(option);
                }

                productDDL.val(itemCode).trigger('change');

                row.find('.productCode').val(item.code ?? '');
                //row.find('.nos').val(item.pendingQty ?? '');
                row.find('.netQuantity').val(item.pendingQty ?? '');
                row.find('.rateRs').val(item.rate ?? '');
                row.find('.saudaNo').val(item.saudaNo ?? '');
                row.find('.saudaType').val(item.saudaType ?? '');
                row.find('.saudaRate').val(item.saudaRate ?? '');
                row.find('.stockQuantity').val(productDDL.find('option:selected').data('item')?.stockQty ?? 0);

                ApplySelectedTaxToRow(row);

                CalculateRowAmounts(row);
            }
            
            $('#copyFromModal').modal('hide');

            showToast(`${selectedOrders.length} order(s) copied successfully`, { type: 'success'});

        } catch (error) {
            console.error('Copy Pending Orders Error:', error);
            showToast('Failed to copy pending orders', {type: 'error'});
        }
    });

    //===========================
    // Calculation 
    //===========================

    $('#ddlTaxType').on('change', function () {

        const tax = $(this).find('option:selected').data('tax');

        $('#NumOtherCGST1').val(Number(tax?.cgstPer) || 0);
        $('#NumOtherSGST1').val(Number(tax?.sgstPer) || 0);
        $('#NumOtherIGST1').val(Number(tax?.igstPer) || 0);
        $('#NumOtherCESS1').val(Number(tax?.othPer) || 0);
        $('#NumOtherTCS1').val(Number(tax?.tcsPer) || 0);

        $('#tblDeliveryorder tbody tr').each(function () {

            const row = $(this);

            row.find('.taxType').val(tax?.text || '');
            row.find('.taxCode').val(tax?.id || '');

            const cgstPer = Number(tax?.cgstPer) || 0;
            const sgstPer = Number(tax?.sgstPer) || 0;
            const igstPer = Number(tax?.igstPer) || 0;
            const cessPer = Number(tax?.othPer) || 0;

            if (cgstPer > 0) row.find('.cgstPer').val(cgstPer);
            if (sgstPer > 0) row.find('.sgstPer').val(sgstPer);
            if (igstPer > 0) row.find('.igstPer').val(igstPer);
            if (cessPer > 0) row.find('.cessPer').val(cessPer);

            CalculateRowAmounts(row);
        });

        CalculateFooterTotals();
    });

    // ===========================================
    // FREIGHT / INSURANCE / TDS / TCS CHANGE
    // ===========================================
    $('#NumFreightAmt, #txtins_p, #txttds_p, #txttaxOnFrt_p, #NumOtherTCS1').on('input change', function () {
        CalculateFooterTotals();

        $('#tblDeliveryorder tbody tr').each(function () {
            CalculateRowAmounts($(this));
        });

        CalculateFooterTotals();
    });

    //==============================================
    // Calculate on behlaf of PCS CHECKBOX
    //==============================================
    $('#cbCalOnPcs').on('change', function () {
        $('#tblDeliveryorder tbody tr').each(function () {
            CalculateRowAmounts($(this));
        });

        CalculateFooterTotals();
    });

    //=======================================
    // Calc btn logic (ex Rate)
    //=======================================
    $(document).on('click', '#btnCalcExRate', async function () {
        const exchangeRate = parseFloat($('#NumExRate').val()) || 0;

        if (exchangeRate === 0) {
            showToast('Please enter Exchange Rate First...', { type: 'warning' });
            $('#NumExRate').trigger('focus');
            return;
        }

        const tbody = $('#tblDeliveryorder tbody');

        tbody.find('tr').each(function () {
            const row = $(this);
            const itemCode = row.find('.productCode').val();

            if (!itemCode) return;

            const rateUsd = parseFloat(row.find('.rateUsd').val()) || 0;
            const rateRs = rateUsd * exchangeRate;

            row.find('.rateRs').val(rateRs.toFixed(3));

            CalculateRowAmounts(row);
        });

        CalculateFooterTotals();
    });
}

$(document).on('blur', '#NumOtherPacking2', function () {
    DistributePackingAmount();
});

function SetCurrentDate() {
    const today = new Date();
    const date = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;

    $('#DtBillDate').val(date);
}

async function GetStock() {

    try {

        const response = await fetch('/DeliveryOrder/GetStock');

        if (!response.ok) {
            throw new Error('Failed to update stock');
        }

        const data = await response.json();

        if (!data.success) {
            throw new Error(data.message || 'Stock update failed');
        }

        showToast(data.message ,{ type: "success" });
     
    } catch (error) {
        console.error('Stock Update Error:', error);
        showToast('Failed to update stock', { type: 'error' });
    }
}

async function BindVoucherType() {
    try {
        const response = await fetch('/DeliveryOrder/DocTypeDDL');

        if (!response.ok) {
            throw new Error('Failed to load Voucher Type');
        }

        const data = await response.json();
        const ddl = $('#ddlVoucherType');

        ddl.empty();

        $.each(data, function (index, item) {
            ddl.append($('<option>', { value: item.value, text: item.text }));
        });

    } catch (error) {
        console.error('Error loading Voucher Type:', error);
        showToast("Failed To Load Voucher Type " + error, { type: "error" });
    }
}

async function BindFoamType() {
    try {
        const response = await fetch('/DeliveryOrder/FoamTypeDDL');

        if (!response.ok) {
            throw new Error('Failed to load Foam Type');
        }

        const data = await response.json();
        const ddl = $('#ddlFormType');

        ddl.empty();
        ddl.append($('<option>', {value: '', text: 'Select Form Type'}));

        $.each(data, function (index, item) {
            ddl.append($('<option>', { value: item.value, text: item.text }));
        });

    } catch (error) {
        console.error('Error loading Foam Type:', error);
        showToast("Error loading Foam Type : " + error, { type: "error" });
    }
}

async function BindProdType() {
    try {
        const response = await fetch('/DeliveryOrder/ProdTypeDDL');

        if (!response.ok) {
            throw new Error('Failed to load prod Type');
        }
          
        const data = await response.json();
        const ddl = $('#ddlProdType');

        ddl.empty();
        ddl.append($('<option>', { value: '', text: 'Select Prod Type' }));

        $.each(data, function (index, item) {
            ddl.append($('<option>', { value: item.value, text: item.text }));
        });

    } catch (error) {
        console.error('Error loading Prod Type:', error);
        showToast("Error loading prod Type : " + error, { type: "error" });
    }
}

async function GetVNo() {
    try {

        const vType = $('#ddlVoucherType').val();

        if (!vType) {
            $('#NumVoucherNo').val('');
            return;
        }
        const response = await fetch(
            `/DeliveryOrder/GetNextV_NO?vType=${encodeURIComponent(vType)}`
        );

        if (!response.ok) {
            throw new Error('Failed to generate VNo');
        }

        const data = await response.json();

        if (data.error) {
            console.error(data.error);
            showToast("Voucher No Error: " + data.error, { type: "error" });
            return;
        }

        $('#NumVoucherNo').val(data.v_NO);

        const docId = vType + data.v_NO;
    } catch (error) {
        console.error('Error generating VNo:', error);
        showToast("Error generating vNo: " + err, { type: "error" });
    }
}

async function BindStatus() {
    try {
        const response = await fetch('/DeliveryOrder/DocStatusDDL');

        if (!response.ok) {
            throw new Error('Failed to load Doc Status');
        }

        const data = await response.json();
        const ddl = $('#ddlDocStatus');

        ddl.empty();

        $.each(data, function (index, item) {
            ddl.append($('<option>', { value: item.value, text: item.text }));
        });

    } catch (error) {
        console.error('Error loading Doc Status:', error);
        showToast("Failed To Load Doc Status " + error, { type: "error" });
    }
}

async function BindPartyNameDDL() {

    $('#ddlPartyName').select2({
        placeholder: 'Select Party Name',
        allowClear: true,

        ajax: {
            url: '/DeliveryOrder/PartyNameDDL',
            type: 'GET',
            dataType: 'json',
            delay: 300,
             
            data: function (params) {
                return {
                    search: params.term || '',
                    page: params.page || 1,
                    pageSize: 500
                };
            },

            processResults: function (data, params) {

                params.page = params.page || 1;

                return {
                    results: data.results.map(function (item) {
                        return {
                            id: item.value,
                            text: item.text
                        };
                    }),

                    pagination: {
                        more: data.pagination.more
                    }
                };
            },
             
            cache: true
        },

        minimumInputLength: 0
    });

    $('#ddlPartyName').on('select2:open', function () {
        setTimeout(function () {
            document.querySelector('.select2-search__field').focus();
        }, 0);
    });
}

async function BindPartyAddressDDL(partyCode) {

    try {

        const response = await fetch(
            `/DeliveryOrder/GetPartyAddressList?partyCode=${encodeURIComponent(partyCode)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load party addresses');
        }

        const data = await response.json();

        const ddl = $('#ddladdressL1');

        ddl.empty();

        ddl.append(new Option('Select Address', ''));

        data.forEach(function (item) {

            const addressText = [
                item.add1,
                item.add2,
                item.add3,
                item.city,
                item.state,
                item.pincode,
                item.gstin ? `GSTIN: ${item.gstin}` : ''
            ].filter(x => x).join(', ');

            const option = new Option(addressText, item.addressId);

            $(option).data('address', item);

            ddl.append(option);
        });

        // =========================
        // FIRST ADDRESS
        // =========================

        if (data.length > 0) {

            const firstAddress = data[0];

            ddl.val(firstAddress.addressId);

            ddl.find(':selected').data('address', firstAddress);

            ddl.trigger('change');
        }

    } catch (error) {

        console.error('Party Address Error:', error);
        showToast("Failed to load party addresses" + err, { type: "error" });

    }
}

async function BindConsigneeDDL() {

    $('#ddlConsignee').select2({
        placeholder: 'Select Party Name',
        allowClear: true,

        ajax: {
            url: '/DeliveryOrder/PartyNameDDL',
            type: 'GET',
            dataType: 'json',
            delay: 300,

            data: function (params) {
                return {
                    search: params.term || '',
                    page: params.page || 1,
                    pageSize: 500
                };
            },

            processResults: function (data, params) {

                params.page = params.page || 1;

                return {
                    results: data.results.map(function (item) {
                        return {
                            id: item.value,
                            text: item.text
                        };
                    }),

                    pagination: {
                        more: data.pagination.more
                    }
                };
            },

            cache: true
        },

        minimumInputLength: 0
    });

    $('#ddlConsignee').on('select2:open', function () {
        setTimeout(function () {
            document.querySelector('.select2-search__field').focus();
        }, 0);
    });
}

async function BindConsigneeAddressDDL(partyCode) {

    try {

        const response = await fetch(
            `/DeliveryOrder/GetPartyAddressList?partyCode=${encodeURIComponent(partyCode)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load consignee address');
        }

        const data = await response.json();

        const ddl = $('#ddlTransactionaddressL1');

        ddl.empty();

        ddl.append(new Option('Select Address', ''));

        data.forEach(function (item) {

            const addressText = [
                item.add1,
                item.add2,
                item.add3,
                item.city,
                item.state,
                item.pincode,
                item.gstin ? `GSTIN: ${item.gstin}` : ''
            ]
                .filter(x => x)
                .join(', ');

            const option = new Option(
                addressText,
                item.addressId
            );

            $(option).data('address', item);

            ddl.append(option);
        });

        if (data.length > 0) {

            const firstAddress = data[0];

            ddl.val(firstAddress.addressId);

            ddl.find(':selected').data('address', firstAddress);

            ddl.trigger('change');
        }

    } catch (error) {
        console.error('Consignee Address Error:', error);
        showToast( 'Failed to load consignee address', 'error');
    }
}

function ClearConsigneeAddress() {

    $('#ddlTransactionaddressL1').empty().append('<option value="">Select address</option>');

    $('#TxtTransactionAddressL1').val('');
    $('#TxtTransactionAddressL2').val('');
    $('#TxtTransactionAddressL3').val('');
    $('#NumTransactionGSTNo').val('');
    $('#TxtTransactionStation').val('');
    $('#NumTransactionPIN').val('');
}

function ClearPartyAddress() {

    $('#ddladdressL1').empty().append('<option value="">Select address</option>');
    $('#TxtAddressL1').val('');
    $('#TxtAddressL2').val('');
    $('#TxtAddressL3').val('');
    $('#TxtGST').val('');
    $('#NumPincode').val('');
}

async function LoadPartyCreditDetails(partyCode) {

    try {

        const response = await fetch(
            `/DeliveryOrder/GetPartyCreditDetails?partyCode=${encodeURIComponent(partyCode)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load party credit details');
        }

        const data = await response.json();

        if (!data) {
            return;
        }

        $('#partyCode').val(data.code || '');

        $('#partyInfoName').val(data.partyName || '');

        $('#partyInfoCreditLimit').val(
            Number(data.creditLimit || 0).toFixed(2)
        );

        $('#partyInfoDrBal').val(
            Number(data.drBal || 0).toFixed(2)
        );

        $('#partyInfoCrBal').val(
            Number(data.crBal || 0).toFixed(2)
        );

        // =========================
        // AUTO SELECT BROKER
        // =========================

        if (data.agentCode) {

            const brokerCode = data.agentCode;
            const brokerName = data.agentName || '';

            if ($('#ddlSoldBy option[value="' + brokerCode + '"]').length === 0) {

                const option = new Option(brokerName,  brokerCode, true, true );

                $('#ddlSoldBy').append(option).trigger('change');

            } else {
                $('#ddlSoldBy').val(brokerCode).trigger('change');
            }

        } else {
            $('#ddlSoldBy').val(null).trigger('change');
        }

    } catch (error) {
        console.error('Party Credit Details Error:', error);
        showToast("Failed to load party credit details ",{ type: "error" });
    }
}
 
async function BindBrokerDDL() {

    $('#ddlSoldBy').select2({
        placeholder: 'Select Broker',
        allowClear: true,

        ajax: {
            url: '/DeliveryOrder/BrokerDDL',
            type: 'GET',
            dataType: 'json',
            delay: 300,

            data: function (params) {
                return {
                    search: params.term || ''
                };
            },

            processResults: function (data) {

                return {
                    results: data.map(function (item) {
                        return {
                            id: item.value,
                            text: item.text
                        };
                    })
                };
            },

            cache: true
        },

        minimumInputLength: 0
    });

    $('#ddlSoldBy').on('select2:open', function () {

        setTimeout(function () {

            const searchField =document.querySelector('.select2-search__field');

            if (searchField) {
                searchField.focus();
            }

        }, 0);
    });
}

async function BindTaxDDL(stationCode) {

    try {

        const response = await fetch(
            `/DeliveryOrder/TaxDDL?stationCode=${encodeURIComponent(stationCode)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load tax');
        }

        const data = await response.json();

        const ddl = $('#ddlTaxType');

        ddl.empty();
        ddl.append(new Option('Select Tax', ''));

        data.forEach(function (item) {

            const option = new Option(
                item.text,
                item.id
            );

            $(option).data('tax', item);

            ddl.append(option);
        });

    } catch (error) {
        console.error('Tax Bind Error:', error);
        showToast('Failed to load tax', 'error');
    }
}

async function BindStationDDL() {

    try {

        const response = await fetch('/DeliveryOrder/StationDDL');

        if (!response.ok) {
            throw new Error('Failed to load station');
        }

        const data = await response.json();

        const ddl = $('#ddlStation');

        ddl.empty();
        ddl.append(new Option('Select Station', ''));

        data.forEach(function (item) {

            ddl.append(
                new Option(item.text, item.value)
            );

        });

    } catch (error) {

        console.error('Station Bind Error:', error);
        showToast('Failed to load station', 'error');

    }
}

async function BindStationDDL1() {

    try {

        const response = await fetch('/DeliveryOrder/StationDDL');

        if (!response.ok) {
            throw new Error('Failed to load station');
        }

        const data = await response.json();

        const ddl = $('#TxtTransactionStation');

        ddl.empty();
        ddl.append(new Option('Select Station', ''));

        data.forEach(function (item) {

            ddl.append(
                new Option(item.text, item.value)
            );

        });

    } catch (error) {

        console.error('Station Bind Error:', error);
        showToast('Failed to load station', 'error');

    }
}

async function LoadPartyLastSaleDetails(partyCode) {

    try {

        const response = await fetch(
            `/DeliveryOrder/GetPartyLastSaleDetails?partyCode=${encodeURIComponent(partyCode)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load party sale details');
        }

        const data = await response.json();

        if (!data) {
            return;
        }

        // =========================
        // FORM TYPE
        // =========================

        const formCode = String(data.formCode || '');

        if (formCode) {

            const formDDL = $('#ddlFormType');

            if (formDDL.find(`option[value="${formCode}"]`).length === 0) {
                formDDL.append(new Option(formCode, formCode));
            }

            formDDL.val(formCode).trigger('change');
        }

        // =========================
        // TAX TYPE
        // =========================

        const taxCode = String(data.taxCode || '');

        if (taxCode) {

            const taxDDL = $('#ddlTaxType');

            let option = taxDDL.find('option').filter(function () {
                return String(this.value) === taxCode;
            });

            // Tax dropdown mein record nahi hai toh station ke basis par reload karo.
            if (option.length === 0) {

                const stationCode = $('#ddlStation').val();

                if (stationCode) {
                    await BindTaxDDL(stationCode);
                }

                option = taxDDL.find('option').filter(function () {
                    return String(this.value) === taxCode;
                });
            }

            if (option.length === 0) {
                option = $(new Option(data.taxName || taxCode, taxCode));
                taxDDL.append(option);
            }

            taxDDL.val(taxCode).trigger('change');
        }

        // =========================
        // TRANSPORT DISTANCE
        // =========================

        $('#NumDist').val(data.tptDistance || '');

    } catch (error) {

        console.error('Party Last Sale Details Error:', error);

        showToast('Failed to load party sale details', {
            type: 'error'
        });
    }
}

async function BindPackingVoucherList() {
    try {
        const vType = $('#ddlVoucherType').val();
        const pType = $('#ddlProdType option:selected').text();
        const partyCode = $('#ddlPartyName').val();
        const vDate = $('#DtBillDate').val();

        const $ddlPackNo = $('#ddlPackNo');

        $ddlPackNo.empty().append('<option value="">Select Packing No</option>');

        if (!vType || !partyCode || !vDate) {
            return;
        }

        const params = new URLSearchParams({
            vType: vType,
            pType: pType,
            partyCode: partyCode,
            vDate: vDate
        });

        const response = await fetch(
            `/DeliveryOrder/GetPackingVoucherList?${params.toString()}`
        );

        if (!response.ok) {
            throw new Error('Failed to load packing vouchers');
        }

        const data = await response.json();

        data.forEach(item => {
            const option = new Option(item.vNo, item.vNo);
            $(option).attr('data-vtype', item.vType);
            $ddlPackNo.append(option);
        });

        $ddlPackNo.trigger('change');

    } catch (error) {
        console.error('Packing Voucher Error:', error);
        showToast('Failed to load packing vouchers', { type: 'error' });
    }
}

async function BindWaybillList() {
    try {
        const response = await fetch('/DeliveryOrder/GetWaybillList');

        if (!response.ok) {
            throw new Error('Failed to load waybill list');
        }

        const data = await response.json();
        const ddl = $('#ddlWBNo');

        ddl.empty();
        ddl.append(new Option('Select Waybill No.', '', true, false));

        data.forEach(item => {
            const option = new Option(item.vNo, item.vNo);
            $(option).attr('data-vtype', item.vType);
            ddl.append(option);
        });

        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.select2('destroy');
        }

        ddl.select2({
            placeholder: 'Select Waybill No.',
            allowClear: true,
            width: '100%'
        });

        ddl.off('select2:open.waybill').on('select2:open.waybill', function () {
            requestAnimationFrame(() => {
                const searchBox = document.querySelector(
                    '.select2-container--open .select2-search__field'
                );

                if (searchBox) {
                    searchBox.focus();
                }
            });
        });

        ddl.val('').trigger('change');

    } catch (error) {
        console.error('Waybill List Error:', error);
        showToast('Failed to load waybill list', { type: 'error' });
    }
}

async function BindIssueVoucherList() {
    try {
        const response = await fetch('/DeliveryOrder/GetIssueVoucherList');

        if (!response.ok) {
            throw new Error('Failed to load issue voucher list');
        }

        const data = await response.json();
        const ddl = $('#ddlIssueNo');

        ddl.empty();
        ddl.append(new Option('Select Issue No.', '', true, false));

        data.forEach(item => {
            const option = new Option(item.vNo, item.vNo);
            $(option).attr('data-vtype', item.vType);
            ddl.append(option);
        });

        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.select2('destroy');
        }

        ddl.select2({
            placeholder: 'Select Issue No.',
            allowClear: true,
            width: '100%'
        });

        // Autofocus search box whenever dropdown opens
        ddl.off('select2:open.issue').on('select2:open.issue', function () {
            requestAnimationFrame(() => {
                document.querySelector(
                    '.select2-container--open .select2-search__field'
                )?.focus();
            });
        });

        ddl.val(null).trigger('change');

    } catch (error) {
        console.error('Issue Voucher Error:', error);
        showToast('Failed to load issue voucher list', { type: 'error' });
    }
}

async function BindTransport() {
    try {
        const response = await fetch('/DeliveryOrder/DocTransportDDL');

        if (!response.ok) {
            throw new Error('Failed to load Transport');
        }

        const data = await response.json();
        const ddl = $('#ddlTransport');

        ddl.empty();
        ddl.append(new Option('Select Transport', '', false, false));

        $.each(data, function (index, item) {
            ddl.append(new Option(item.text, item.value, false, false));
        });

        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.select2('destroy');
        }

        ddl.select2({
            placeholder: 'Select Transport',
            allowClear: true,
            width: '100%'
        });

        ddl.off('select2:open.transportFocus').on('select2:open.transportFocus', function () {
            requestAnimationFrame(() => {
                const searchBox = document.querySelector(
                    '.select2-container--open .select2-search__field'
                );

                if (searchBox) {
                    searchBox.focus();
                    searchBox.select();
                }
            });
        });

        ddl.select2('open');

    } catch (error) {
        console.error('Error loading Transport:', error);
        showToast('Error loading Transport: ' + error.message, { type: 'error' });
    }
}

//================================================
//     Footer Table
//================================================

async function addNewRow() {

    const tbody = $("#tblDeliveryorder tbody");

    const row = `
        <tr>
            <!-- Col0: Product Code / ID (hidden) -->
            <td>
                 <input type="number" class="erppagetable-control productCode " readonly>
            </td>
             
            <!-- Col1: Product Name -->
            <td class="freeze-item">
                <select class="erppagetable-control productName" disabled>
                    <option value="">Select Item</option>
                </select>
            </td>
                              
            <!-- Col2: Nos -->
            <td>
                <input type="number" class="erppagetable-control nos">
            </td>

            <!-- Col3: Gross Qty (hidden) -->
            <td style="display:none;">
                <input type="number" class="erppagetable-control grossQuantity" step="0.001">
            </td>

            <!-- Col4: Net Quantity -->
            <td>
                <input type="number" class="erppagetable-control netQuantity" step="0.001">
            </td>

            <!-- Col5: Rate USD -->
            <td>
                <input type="number" class="erppagetable-control rateUsd" step="0.001">
            </td>

            <!-- Col6: Rate Rs -->
            <td>
                <input type="number" class="erppagetable-control rateRs" step="0.001">
            </td>

            <!-- Col7: Amount -->
            <td>
                <input type="number" class="erppagetable-control amount" step="0.01">
            </td>

            <!-- Col8: Pack % -->
            <td>
                <input type="number" class="erppagetable-control packPer" step="0.01">
            </td>

            <!-- Col9: Pack Amount -->
            <td>
                <input type="number" class="erppagetable-control packAmount" step="0.01" readonly>
            </td>

            <!-- Col10: Discount % -->
            <td>
                <input type="number" class="erppagetable-control discPer" step="0.01">
            </td>

            <!-- Col11: Discount Amount -->
            <td>
                <input type="number" class="erppagetable-control discAmount " step="0.01" readonly>
            </td>

            <!-- Col12: Tax Type (hidden) -->
            <td style="display:none;">
                <input type="text" class="erppagetable-control taxType">
            </td>

            <!-- Col13-20: Tax percentages and amounts (hidden) -->
            <td style="display:none;"><input type="number" class="erppagetable-control cgstPer"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control cgstAmount"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control sgstPer"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control sgstAmount"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control igstPer"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control igstAmount"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control cessPer"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control cessAmount"></td>

            <!-- Col21: Remark (hidden) -->
            <td style="display:none;">
                <input type="text" class="erppagetable-control remark" readonly>
            </td>

            <!-- Col22: Pack No -->
            <td>
                <input type="text" class="erppagetable-control packNo" readonly>
            </td>

            <!-- Col23: Lot No -->
            <td>
                <input type="text" class="erppagetable-control lotNo " readonly>
            </td>

            <!-- Col24: Sauda Type (hidden) -->
            <td style="display:none;">
                <input type="text" class="erppagetable-control saudaType">
            </td>

            <!-- Col25: Sauda No -->
            <td>
                <input type="text" class="erppagetable-control saudaNo" readonly>
            </td>

            <!-- Col26: Sauda Rate -->
            <td>
                <input type="number" class="erppagetable-control saudaRate" step="0.001">
            </td>

            <!-- Col27-32: Order/DCN/HSN (hidden) -->
            <td style="display:none;"><input type="text" class="erppagetable-control orderType"></td>
            <td style="display:none;"><input type="text" class="erppagetable-control orderNo"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control orderRate"></td>
            <td style="display:none;"><input type="text" class="erppagetable-control dcnType"></td>
            <td style="display:none;"><input type="text" class="erppagetable-control dcnNo"></td>
            <td style="display:none;"><input type="text" class="erppagetable-control hsn"></td>

            <!-- Col33: ROWID (visible, as in current table header) -->
            <td>
                <input type="number" class="erppagetable-control rowid">
            </td>

            <!-- Col34-38: Tax Code, Freight, Insurance, Cash Discount, WB Qty (hidden) -->
            <td style="display:none;"><input type="text" class="erppagetable-control taxCode"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control freightAmount"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control insuranceAmount"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control cashDiscountAmount"></td>
            <td style="display:none;"><input type="number" class="erppagetable-control wbQty"></td>

            <!-- Col39: Stock Quantity -->
            <td>
                <input type="number" class="erppagetable-control stockQuantity" readonly>
            </td>

            <!-- Action -->
            <td class="action-col">
                <div class="action-wrap">
                    <button type="button" class="act-btn add" title="Add Row">
                        <i class="fa fa-plus"></i>
                    </button>
                    <button type="button" class="act-btn delete" title="Delete Row">
                        <i class="fa fa-trash"></i>
                    </button>
                </div>
            </td>
        </tr>
    `;

    tbody.append(row);

    const lastRow = tbody.find("tr:last");
    
    await BindItemDDL(lastRow);

    ApplySelectedTaxToRow(lastRow);

    lastRow.find('.nos').on('input change', function () {
        CalculateRowQuantities(lastRow);
    });

    lastRow.find('.netQuantity, .rateRs, .packPer, .discPer').on('input change', function () {
        CalculateRowAmounts(lastRow);
    });

    $('#NumCashDiscount1').off('input.deliveryOrderCalc').on('input.deliveryOrderCalc', function () {
        $('#tblDeliveryorder tbody tr').each(function () {
            CalculateRowAmounts($(this));
        });
    });
}

async function BindItemDDL(row) {

    try {

        const response = await fetch('/DeliveryOrder/GetItemStock');

        if (!response.ok) {
            throw new Error('Failed to load items');
        }

        const data = await response.json();

        const ddl = row.find('.productName');

        ddl.empty();
        ddl.append(new Option('Select Item', ''));

        data.forEach(function (item) {

            const option = new Option(item.name, item.code);

            $(option).data('item', item);

            ddl.append(option);
        });

        ddl.on('change', async function () {
            const selectedOption = $(this).find('option:selected');
            const item = selectedOption.data('item');

            if (!item) {
                row.find('.productCode').val('');
                row.find('.grossQuantity').val('');
                row.find('.netQuantity').val('');
                row.find('.stockQuantity').val('');

                row.removeData('itemCalculationDetails');
                return;
            }

            row.find('.productCode').val(item.code);
            row.find('.stockQuantity').val(item.stockQty ?? 0);

            const details = await GetItemCalculationDetails(item.code);

            if (!details) {
                return;
            }

            row.data('itemCalculationDetails', details);

            CalculateRowQuantities(row);
        });

    } catch (error) {
        console.error('Item Bind Error:', error);
        showToast('Failed to load items',{ type: 'error' } );
    }
}

async function BindPendingDeliveryOrder(partyCode) {
    try {
        const response = await fetch(
            `/DeliveryOrder/GetPendingDeliveryOrder?partyCode=${encodeURIComponent(partyCode)}&vType=${encodeURIComponent($('#ddlVoucherType').val() || '')}`
        );

        if (!response.ok) {
            throw new Error('Failed to load pending delivery orders');
        }

        const result = await response.json();
        const tbody = $('#tblDeliveryOrderPopUp tbody');

        tbody.empty();

        if (!result.success || !result.data || result.data.length === 0) {
            tbody.append(`
                <tr>
                    <td colspan="15" class="text-center">
                        ${result.message || 'No pending orders available'}
                    </td>
                </tr>
            `);
            $('#selectAllPR').prop('checked', false);
            return;
        }

        result.data.forEach(function (item) {
            const row = $(`
                <tr>
                    <td><input type="checkbox" class="pending-order-checkbox"></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                    <td></td>
                </tr>
            `);

            row.find('td:eq(1)').text(item.docId ?? '');
            row.find('td:eq(2)').text(item.code ?? '');
            row.find('td:eq(3)').text(item.itemName ?? '');
            row.find('td:eq(4)').text(item.orderQty ?? '');
            row.find('td:eq(5)').text(item.pendingQty ?? '');
            row.find('td:eq(6)').text(item.rate ?? '');
            row.find('td:eq(7)').text(item.orderDate  ? String(item.orderDate).split('T')[0] : '');
            row.find('td:eq(8)').text(item.saudaRate ?? '');
            row.find('td:eq(9)').text(item.saudaType ?? '');
            row.find('td:eq(10)').text(item.saudaNo ?? '');
            row.find('td:eq(11)').text(item.itemType ?? '');
            row.find('td:eq(12)').text(item.orderType ?? '');
            row.find('td:eq(13)').text(item.orderNo ?? '');
            row.find('td:eq(14)').text(item.offerNo ?? '');

            row.data('pendingOrder', item);

            tbody.append(row);
        });

        $('#selectAllPR').prop('checked', false);

    } catch (error) {
        console.error('Pending Orders Error:', error);
        showToast('Failed to load pending orders', { type: 'error' });
    }
}

//========================================
// Calculation
//========================================

async function GetItemCalculationDetails(itemCode) {

    if (!itemCode) {
        return null;
    }

    try {
        return await $.ajax({
            url: '/DeliveryOrder/GetItemCalculationDetails',
            type: 'GET',
            data: { itemCode: itemCode }
        });
    } catch (error) {
        console.error('Item calculation details error:', error);
        return null;
    }
}

async function LoadDeliveryOrderCalcSettings() {
    try {
        const response = await fetch('/DeliveryOrder/GetCalculationSettings');

        if (!response.ok) {
            throw new Error('Failed to load calculation settings');
        }

        deliveryOrderCalcSettings = await response.json();

        if ($('#txtins_p').val() === '' &&
            Number(deliveryOrderCalcSettings.insurancePercent) > 0) {
            $('#txtins_p').val(deliveryOrderCalcSettings.insurancePercent);
        }

        if ($('#NumOtherTCS1').val() === '' &&
            Number(deliveryOrderCalcSettings.tcsPercent) > 0) {
            $('#NumOtherTCS1').val(deliveryOrderCalcSettings.tcsPercent);
        }
        
    } catch (error) {
        console.error('Calculation Settings Error:', error);
        showToast('Failed to load calculation settings', { type: 'error' });
    }
}

function ApplySelectedTaxToRow(row) {

    const tax = $('#ddlTaxType option:selected').data('tax');

    if (!tax) {
        return;
    }

    row.find('.taxType').val(tax.text || '');
    row.find('.taxCode').val(tax.id || '');

    row.find('.cgstPer').val(Number(tax.cgstPer) || 0);
    row.find('.sgstPer').val(Number(tax.sgstPer) || 0);
    row.find('.igstPer').val(Number(tax.igstPer) || 0);
    row.find('.cessPer').val(Number(tax.othPer) || 0);

    CalculateRowAmounts(row);
}

function CalculateRowQuantities(row) {
    const itemCode = row.find('.productCode').val();
    const nos = parseFloat(row.find('.nos').val()) || 0;

    const item = row.data('itemCalculationDetails');
    const settings = deliveryOrderCalcSettings;

    if (!itemCode || !item || !settings) {
        return;
    }

    if (settings.tonnageRate === 'Yes') {
        if (Number(item.netWt) > 0) {
            row.find('.grossQuantity').val(
                (nos * Number(item.netWt)).toFixed(3)
            );
        }

        if (Number(item.packingNos) > 0) {
            row.find('.netQuantity').val(
                (nos * Number(item.packingNos)).toFixed(3)
            );
        }
    }

    if (settings.weightCalcOnBales === 'Yes') {
        if (Number(item.netWt) > 0) {
            row.find('.grossQuantity').val(
                (nos * Number(item.netWt)).toFixed(3)
            );
        }

        if (Number(item.packingWt) > 0) {
            row.find('.netQuantity').val(
                (nos * Number(item.packingWt)).toFixed(3)
            );
        }
    }

    CalculateRowAmounts(row);
}

function CalculateRowAmounts(row) {
    const itemCode = row.find('.productCode').val();
    const netQty = parseFloat(row.find('.netQuantity').val()) || 0;
    const rateRs = parseFloat(row.find('.rateRs').val()) || 0;
    const packPer = parseFloat(row.find('.packPer').val()) || 0;
    const discPer = parseFloat(row.find('.discPer').val()) || 0;
    const cashDiscPer = parseFloat($('#NumCashDiscount1').val()) || 0;

    const item = row.data('itemCalculationDetails');
    const settings = deliveryOrderCalcSettings;

    let amount = 0;

    // 1. Calculate Amount - Legacy PCS Logic
    const calculateOnPcs = $('#cbCalOnPcs').is(':checked');
    const grossQty = parseFloat(row.find('.grossQuantity').val()) || 0;
    const nos = parseFloat(row.find('.nos').val()) || 0;

    if (itemCode) {

        if (calculateOnPcs) {

            // PCS checked: Gross Quantity * Rate
            if (nos > 0) {
                amount = grossQty * rateRs;
            }

        } else if (netQty > 0) {

            // PCS unchecked: Net Quantity * Rate
            amount = netQty * rateRs;

            if (settings?.tonnageRate === 'Yes' && item) {

                const reportType = (item.reportType || '').trim().toLowerCase();

                if (reportType === 'hessian' || reportType === 'sacking') {

                    amount = (netQty * rateRs) / 100;

                } else if ( reportType === 'twine' && Number(item.netWt) > 0
                ) {

                    const taxableRate = Number(item.taxableRate) || 0;

                    amount = taxableRate > 0 ? (netQty * rateRs) / taxableRate : 0;
                }
            }
        }
    }

    // 2. Packing, Discount and Cash Discount
    let packAmount = parseFloat(row.find('.packAmount').val()) || 0;
    let discountAmount = parseFloat(row.find('.discAmount').val()) || 0;

    if (packPer > 0) {
        packAmount = Number((amount * packPer / 100).toFixed(2));
    }
    row.find('.packAmount').val(packAmount.toFixed(2));

    const amountAfterPacking = Number((amount + packAmount).toFixed(2));

    if (discPer > 0) {
        discountAmount = Number(
            (amountAfterPacking * discPer / 100).toFixed(2)
        );
    }
    row.find('.discAmount').val(discountAmount.toFixed(2));

    const cashDiscountAmount = Number(
        (amountAfterPacking * cashDiscPer / 100).toFixed(2)
    );

    row.find('.cashDiscountAmount').val(cashDiscountAmount.toFixed(2));

    // 3. Taxable Amount
    let taxableAmount =  amount + packAmount - discountAmount - cashDiscountAmount;

    if ($('#cbCalOnPcs').is(':checked')) {

        if (settings?.taxOnInsurance === 'Yes') {
            taxableAmount += parseFloat($('#txtins_amt').val()) || 0;
        }

        if (settings?.taxOnFreight === 'Yes') {
            taxableAmount += parseFloat($('#NumFreightAmt').val()) || 0;
        }

    } else {

        if (settings?.taxOnInsurance === 'Yes') {
            taxableAmount += parseFloat(row.find('.insuranceAmount').val()) || 0;
        }

        if (settings?.taxOnFreight === 'Yes') {
            taxableAmount += parseFloat(row.find('.freightAmount').val()) || 0;
        }
    }

    // 4. Tax Percentages
    const cgstPer = parseFloat(row.find('.cgstPer').val()) || 0;
    const sgstPer = parseFloat(row.find('.sgstPer').val()) || 0;
    const igstPer = parseFloat(row.find('.igstPer').val()) || 0;
    const cessPer = parseFloat(row.find('.cessPer').val()) || 0;

    // 5. Tax Amounts
    // Use rounded taxable amount, matching legacy intermediate calculations.
    taxableAmount = Number(taxableAmount.toFixed(2));

    let cgstAmount = parseFloat(row.find('.cgstAmount').val()) || 0;
    let sgstAmount = parseFloat(row.find('.sgstAmount').val()) || 0;
    let igstAmount = parseFloat(row.find('.igstAmount').val()) || 0;
    let cessAmount = parseFloat(row.find('.cessAmount').val()) || 0;

    if (amount > 0) {
        cgstAmount = Number((taxableAmount * cgstPer / 100).toFixed(2));
        sgstAmount = Number((taxableAmount * sgstPer / 100).toFixed(2));
        igstAmount = Number((taxableAmount * igstPer / 100).toFixed(2));
        cessAmount = Number((taxableAmount * cessPer / 100).toFixed(2));
    }

    // 6. Update Row Fields
    row.find('.amount').val(amount.toFixed(2));

    row.find('.cgstAmount').val(cgstAmount.toFixed(2));
    row.find('.sgstAmount').val(sgstAmount.toFixed(2));
    row.find('.igstAmount').val(igstAmount.toFixed(2));
    row.find('.cessAmount').val(cessAmount.toFixed(2));

    CalculateFooterTotals();
}

function CalculateFooterTotals() {

    let totalAmount = 0;
    let totalPacking = 0;
    let totalDiscount = 0;
    let totalCashDiscount = 0;

    let totalCGST = 0;
    let totalSGST = 0;
    let totalIGST = 0;
    let totalCESS = 0;

    let totalNos = 0;
    let totalNetQty = 0;

    let cgstPer = 0;
    let sgstPer = 0;
    let igstPer = 0;
    let cessPer = 0;

    // ================================
    // 1. CALCULATE ITEM ROW TOTALS
    // ================================
    $('#tblDeliveryorder tbody tr').each(function () {

        const row = $(this);

        totalAmount += parseFloat(row.find('.amount').val()) || 0;
        totalPacking += parseFloat(row.find('.packAmount').val()) || 0;
        totalDiscount += parseFloat(row.find('.discAmount').val()) || 0;
        totalCashDiscount += parseFloat(row.find('.cashDiscountAmount').val()) || 0;

        totalCGST += parseFloat(row.find('.cgstAmount').val()) || 0;
        totalSGST += parseFloat(row.find('.sgstAmount').val()) || 0;
        totalIGST += parseFloat(row.find('.igstAmount').val()) || 0;
        totalCESS += parseFloat(row.find('.cessAmount').val()) || 0;

        totalNos += parseFloat(row.find('.nos').val()) || 0;
        totalNetQty += parseFloat(row.find('.netQuantity').val()) || 0;

        cgstPer = parseFloat(row.find('.cgstPer').val()) || cgstPer;
        sgstPer = parseFloat(row.find('.sgstPer').val()) || sgstPer;
        igstPer = parseFloat(row.find('.igstPer').val()) || igstPer;
        cessPer = parseFloat(row.find('.cessPer').val()) || cessPer;
    });

    // ================================
    // 2. SUBTOTAL & TAX
    // ================================
    const subTotal = totalAmount + totalPacking - totalDiscount - totalCashDiscount;

    const totalTax = totalCGST + totalSGST + totalIGST + totalCESS;

    // ================================
    // 3. AMOUNT SUMMARY FIELDS
    // ================================
    $('#NumOtherTotalAmount').val(totalAmount.toFixed(2));
    $('#NumOtherPacking2').val(totalPacking.toFixed(2));
    $('#NumOtherDiscount2').val(totalDiscount.toFixed(2));
    $('#NumCashDiscount2').val(totalCashDiscount.toFixed(2));
    $('#NumSubTotal').val(subTotal.toFixed(2));

    $('#NumOtherCGST2').val(totalCGST.toFixed(2));
    $('#NumOtherSGST2').val(totalSGST.toFixed(2));
    $('#NumOtherIGST2').val(totalIGST.toFixed(2));
    $('#NumOtherCESS2').val(totalCESS.toFixed(2));

    $('#NumOtherCGST1').val(cgstPer.toFixed(2));
    $('#NumOtherSGST1').val(sgstPer.toFixed(2));
    $('#NumOtherIGST1').val(igstPer.toFixed(2));
    $('#NumOtherCESS1').val(cessPer.toFixed(2));

    // Packing and discount percentages
    const packingPer = totalAmount > 0 ? (totalPacking / totalAmount) * 100 : 0;
    const discountBase = totalAmount + totalPacking;
    const discountPer = discountBase > 0 ? (totalDiscount / discountBase) * 100 : 0;

    $('#NumOtherPacking1').val(packingPer.toFixed(2));
    $('#NumOtherDiscount1').val(discountPer.toFixed(2));

    const cashDiscountPer = parseFloat($('#NumCashDiscount1').val()) || 0;

    $('#NumCashDiscount1').val(cashDiscountPer.toFixed(2));

    $('#NumOtherTotalNos').val(totalNos.toFixed(3));
    $('#NumNetQuantity').val(totalNetQty.toFixed(3));

    // ================================
    // 4. FREIGHT & INSURANCE
    // ================================
    const settings = deliveryOrderCalcSettings || {};

    const freightAmount = parseFloat($('#NumFreightAmt').val()) || 0;

    const insurancePer = parseFloat($('#txtins_p').val()) || 0;

    const freightTdsPer = parseFloat($('#txttds_p').val()) || 0;

    //reset these amounts when freight is zero
    if (freightAmount === 0) {
        $('#txtins_amt').val('0');
        $('#txttaxOnFrt_Amt').val('0');
        $('#txttds_amt').val('0');
    }

    const previousNetAmount = parseFloat($('#NumOtherNetAmount').val()) || 0;

    let insuranceBase;

    if (settings.taxOnInsurance === 'Yes') {
        insuranceBase = subTotal + freightAmount;
    } else {
        insuranceBase = previousNetAmount + freightAmount;
    }

    const insuranceAmount = Math.ceil( insuranceBase * (insurancePer / 100000) );

    $('#txtins_amt').val(insuranceAmount.toFixed(2));

    // ================================
    // 5. BILL AMOUNT & TCS
    // ================================
    let billBase = subTotal + totalCGST + totalSGST + totalIGST + totalCESS;

    if (settings.insuranceInBillAmount === 'Yes') {
        billBase += insuranceAmount;
    }

    if (settings.freightInBillAmount === 'Yes') {
        billBase += freightAmount;
    }

    const tcsPer = parseFloat($('#NumOtherTCS1').val()) || 0;

    const tcsAmount = Math.ceil(
        billBase * tcsPer * 0.01
    );

    $('#NumOtherTCS2').val(tcsAmount.toFixed(2));

    // ================================
    // 6. NET AMOUNT & ROUND OFF
    // Legacy VB Math.Round uses midpoint-to-even
    // ================================
    const amountBeforeRounding = billBase + tcsAmount;

    const floorAmount = Math.floor(amountBeforeRounding);
    const fraction = amountBeforeRounding - floorAmount;

    let netAmount;

    if (Math.abs(fraction - 0.5) < 1e-10) {
        netAmount = floorAmount % 2 === 0 ? floorAmount : floorAmount + 1;
    } else {
        netAmount = Math.round(amountBeforeRounding);
    }

    $('#NumOtherNetAmount').val(netAmount.toFixed(2));

    // Legacy round-off calculation
    let roundOffBase = subTotal + totalCGST + totalSGST + totalIGST + totalCESS + tcsAmount;

    if (settings.insuranceInBillAmount === 'Yes') {
        roundOffBase += insuranceAmount;
    }

    if (settings.freightInBillAmount === 'Yes') {
        roundOffBase += freightAmount;
    }

    const roundOff = netAmount - roundOffBase;

    $('#NumOtherRoundOff').val(roundOff.toFixed(2));

    // ================================
    // 7. TDS ON FREIGHT
    // Legacy calculation
    // ================================
    const tdsAmount = Math.round(
        freightAmount * (freightTdsPer / 100)
    );

    $('#txttds_amt').val(tdsAmount.toFixed(2));

}

// =========================================
// Distribute Packing Amount
// =========================================
function DistributePackingAmount() {

    const packingAmount = parseFloat($('#NumOtherPacking2').val()) || 0;
    const totalAmount = parseFloat($('#NumOtherTotalAmount').val()) || 0;

    if (totalAmount <= 0) return;

    $('#tblDeliveryorder tbody tr').each(function () {

        const row = $(this);

        const packPer = parseFloat(row.find('.packPer').val()) || 0;
        const amount = parseFloat(row.find('.amount').val()) || 0;

        if (packPer === 0) {
            const packAmount = (amount * packingAmount) / totalAmount;
            row.find('.packAmount').val(packAmount.toFixed(2));
        }
    });

    CalculateFooterTotals();
}

// =========================================
// Distribute Discount Amount
// =========================================
function DistributeDiscountAmount() {

    const discountAmount = parseFloat($('#NumOtherDiscount2').val()) || 0;
    const totalAmount = parseFloat($('#NumOtherTotalAmount').val()) || 0;

    if (totalAmount <= 0) return;

    $('#tblDeliveryorder tbody tr').each(function () {

        const row = $(this);
        const packPer = parseFloat(row.find('.packPer').val()) || 0;
        const amount = parseFloat(row.find('.amount').val()) || 0;

        if (packPer === 0) {
            const value = (amount * discountAmount) / totalAmount;
            row.find('.discAmount').val(value.toFixed(2));
        }
    });

    CalculateFooterTotals();
}

// =========================================
// Distribute Cash Discount Amount
// =========================================
function DistributeCashDiscountAmount() {

    const cashDiscountAmount = parseFloat($('#NumCashDiscount2').val()) || 0;
    const totalAmount = parseFloat($('#NumOtherTotalAmount').val()) || 0;

    if (totalAmount <= 0) return;

    $('#tblDeliveryorder tbody tr').each(function () {

        const row = $(this);
        const packPer = parseFloat(row.find('.packPer').val()) || 0;
        const amount = parseFloat(row.find('.amount').val()) || 0;

        if (packPer === 0) {
            const value = (amount * cashDiscountAmount) / totalAmount;
            row.find('.cashDiscountAmount').val(value.toFixed(2));
        }
    });

    CalculateFooterTotals();
}

function DistributeInsuranceAmount() {

    const settings = deliveryOrderCalcSettings || {};

    if (settings.taxOnInsurance !== 'Yes') {
        return;
    }

    const insuranceAmount = parseFloat($('#txtins_amt').val()) || 0;
    const totalAmount = parseFloat($('#NumOtherTotalAmount').val()) || 0;

    const rows = $('#tblDeliveryorder tbody tr').filter(function () {
        return !!$(this).find('.productCode').val();
    });

    // Reset insurance allocation for all valid item rows
    rows.each(function () {
        $(this).find('.insuranceAmount').val('0.00');
    });

    if (totalAmount <= 0 || rows.length === 0) {
        return;
    }

    let cumulativeAmount = 0;

    rows.each(function (index) {

        const row = $(this);
        const itemAmount = parseFloat(row.find('.amount').val()) || 0;
        let allocatedAmount;

        // Last item receives the remaining amount to avoid rounding differences
        if (index === rows.length - 1) {

            allocatedAmount = Number(
                (insuranceAmount - cumulativeAmount).toFixed(2)
            );

        } else {

            allocatedAmount = Number(
                (insuranceAmount / totalAmount * itemAmount).toFixed(2)
            );

            cumulativeAmount += allocatedAmount;
        }

        row.find('.insuranceAmount').val(allocatedAmount.toFixed(2));
    });

    // Recalculate row taxes without recursively calling footer calculation
    isDistributingInsurance = true;

    try {
        rows.each(function () {
            CalculateRowAmounts($(this));
        });
    } finally {
        isDistributingInsurance = false;
    }
}

async function SaveDeliveryOrder() {
    try {
        const vType = $('#ddlVoucherType').val();
        const vNo = $('#NumVoucherNo').val();
        const partyCode = $('#ddlPartyName').val();
        const formData = new FormData();

        // =========================
        // HEADER DATA
        // =========================
        const header = {
            V_TYPE: vType,
            V_NO: vNo,
            V_DATE: $('#DtBillDate').val(),
            DEL_DATE: $('#DtDeliveryDate').val(),
            GODOWN_CODE: '',
            BILL_CODE: partyCode,
            BILL_NAME: $('#ddlPartyName option:selected').text(),
            BILL_ADD1: $('#TxtAddressL1').val(),
            BILL_ADD2: $('#TxtAddressL2').val(),
            BILL_ADD3: $('#TxtAddressL3').val(),
            BILL_CITY: $('#ddlStation').val(),
            BILL_CITYNAME: $('#ddlStation option:selected').text(),
            BILL_GST: $('#TxtGST').val(),
            BILL_PINCODE: $('#NumPincode').val(),
            SHIP_CODE: $('#ddlConsignee').val(),
            SHIP_NAME: $('#ddlConsignee option:selected').text(),
            SHIP_ADD1: $('#TxtTransactionAddressL1').val(),
            SHIP_ADD2: $('#TxtTransactionAddressL2').val(),
            SHIP_ADD3: $('#TxtTransactionAddressL3').val(),
            SHIP_CITY: $('#TxtTransactionStation').val(),
            SHIP_CITYNAME: $('#TxtTransactionStation option:selected').text(),
            SHIP_GST: $('#NumTransactionGSTNo').val(),
            SHIP_PINCODE: $('#NumTransactionPIN').val(),
            TAX_CODE: $('#ddlTaxType').val(),
            PACK_TYPE: $('#ddlPackNo option:selected').data('vtype') || '',
            PACK_NO: $('#ddlPackNo').val(),
            ITEM_TYPE: $('#ddlProdType').val(),
            WB_NO: $('#ddlWBNo').val(),
            WB_TYPE: $('#ddlWBNo option:selected').data('vtype') || '',
            ISSUE_NO: $('#ddlIssueNo').val(),
            ISSUE_TYPE: $('#ddlIssueNo option:selected').data('vtype') || '',
            AMOUNT: $('#NumOtherTotalAmount').val(),
            PACK_PER: $('#NumOtherPacking1').val(),
            PACK_AMT: $('#NumOtherPacking2').val(),
            TCS_PER: $('#NumOtherTCS1').val(),
            TCS_AMT: $('#NumOtherTCS2').val(),
            CGST_PER: $('#NumOtherCGST1').val(),
            CGST_AMT: $('#NumOtherCGST2').val(),
            SGST_PER: $('#NumOtherSGST1').val(),
            SGST_AMT: $('#NumOtherSGST2').val(),
            IGST_PER: $('#NumOtherIGST1').val(),
            IGST_AMT: $('#NumOtherIGST2').val(),
            CESS_PER: $('#NumOtherCESS1').val(),
            CESS_AMT: $('#NumOtherCESS2').val(),
            LOAD_PER: $('#txtload_p').val(),
            LOAD_AMT: $('#Txtload_amt').val(),
            LOAD_AC: $('#ddlLoadParty').val(),
            LOAD_REM: $('#TxtLoadRemarks').val(),
            WB_AMT: $('#NumWBAmt').val(),
            WB_AC: $('#ddlWBParty').val(),
            TOT_NOS: $('#NumOtherTotalNos').val(),
            FRT_AMT: $('#NumFreightAmt').val(),
            ROUND_OFF: $('#NumOtherRoundOff').val(),
            NAMOUNT: $('#NumOtherNetAmount').val(),
            INSU_PER: $('#txtins_p').val(),
            INSU_AMT: $('#txtins_amt').val(),
            TDS_PER: $('#txttds_p').val(),
            TDS_AMT: $('#txttds_amt').val(),
            FRT_TAXPER: $('#txttaxOnFrt_p').val(),
            FRT_TAXAMT: $('#txttaxOnFrt_Amt').val(),
            WB_REM: $('#TxtWBRemarks').val(),
            TOT_GROSS: getTableTotal('.grossQuantity'),
            TOT_NET: $('#NumNetQuantity').val() || getTableTotal('.netQuantity'),
            GR_NO: $('#TxtGRNo').val(),
            GR_DATE: $('#DtGRDate').val(),
            VEHICLE_NO: $('#TxtTruckNo').val(),
            TRANSPORT_CODE: $('#ddlTransport').val(),
            TRANSPORT_NAME: $('#ddlTransport option:selected').text(),
            DRIVER_NAME: $('#TxtDriverName').val(),
            DRIVER_NO: $('#TxtDriverNo').val(),
            TPT_MODE: $('#ddlModeOfTransport').val(),
            TPT_DISTANCE: $('#NumDist').val(),
            WAYBILL_NO: $('#TxtWayBillNo').val(),
            FRT_TOPAY: $('#NumToPayFrt').val(),
            REMARK: $('#TxtRemarks').val(),
            WB_QTY: '',
            DISC_PER: $('#NumOtherDiscount1').val(),
            DISC_AMT: $('#NumOtherDiscount2').val(),
            CDISC_PER: $('#NumCashDiscount1').val(),
            CDISC_AMT: $('#NumCashDiscount2').val(),

            AGENT_CODE: $('#ddlSoldBy').val(),
            FORM_CODE: $('#ddlFormType').val(),
            SAUDA_TYPE: null,
            SAUDA_NO: $('#NumSaudaNo').val(),
            SAUDA_RATE: null,

            DEFECTIVE_GOODS: $('#ChkDefectiveGood').is(':checked') ? 1 : 0,
            CAL_ONPCS: $('#cbCalOnPcs').is(':checked') ? '1' : '0',
            PRINT_DETAIL: $('#ChkDetail').is(':checked') ? '1' : '0',

            EXRATE: $('#NumExRate').val(),
            BUYER_ORDNO: $('#TxtBuyerOrder').val(),
            PLACE_RECEIPT: $('#TxtPlaceOfReceipt').val(),
            PORT_LOADING: $('#TxtPortOfLoading').val(),
            PORT_DISCHARGE: $('#TxtPortOfDischarge').val(),
            FINAL_DEST: $('#TxtFinalDestination').val(),
            FINAL_DEST_COUNTRY: $('#TxtFDCountry').val(),
            DELIVERY_TERMS: $('#TxtDeliveryTerms').val(),
            SB_NO: $('#TxtShippingBillNo').val(),
            SB_DATE: $('#DtShippingBillDate').val(),
            PORT_CODE: $('#TxtPortCode').val(),
            LUT_DETAIL: $('#TxtLUTDetail').val(),
            INSU_DETAIL: $('#TxtInsuranceDetail').val(),

            SUPPLY_TYPE: $('#ddlSupplyType').val(),
            LC_NO: $('#TxtLCNo').val(),
            TRAN_TYPE: $('#ddlTransactionType').val(),
            STATUS: $('#ddlDocStatus').val(),
            PAYMENT_TERM: $('#TxtPaymentTerm').val()
        };

        // Append header fields
        Object.entries(header).forEach(([key, value]) => {
            formData.append(key, value ?? '');
        });

        // =========================
        // FOOTER / ITEM TABLE DATA
        // =========================
        const footer = [];

        $('#tblDeliveryorder tbody tr').each(function () {
            const row = $(this);

            const itemCode = row.find('.productCode').val();

            // Skip blank item rows
            if (!itemCode) return;

            footer.push({
                ITEM_CODE: itemCode,
                ITEM_NAME: row.find('.productName option:selected').text(),
                UNIT_NAME: row.find('.unitName').val() || '',
                UNIT_CODE: row.find('.unitCode').val() || '',
                HSN_CODE: row.find('.hsn').val() || '',

                NOS: row.find('.nos').val(),
                GROSS_QTY: row.find('.grossQuantity').val(),
                QTY: row.find('.netQuantity').val(),

                FOR_RATE: row.find('.rateUsd').val(),
                RATE: row.find('.rateRs').val(),
                AMOUNT: row.find('.amount').val(),

                PACK_PER: row.find('.packPer').val(),
                PACK_AMT: row.find('.packAmount').val(),
                DISC_PER: row.find('.discPer').val(),
                DISC_AMT: row.find('.discAmount').val(),

                REMARK: row.find('.remark').val(),
                STATUS: header.STATUS,
                PACK_TYPE: row.find('.packType').val() || '',
                PACK_NO: row.find('.packNo').val(),
                LOT_No: row.find('.lotNo').val(),

                SAUDA_TYPE: row.find('.saudaType').val(),
                SAUDA_NO: row.find('.saudaNo').val(),
                SAUDA_RATE: row.find('.saudaRate').val(),

                ORD_TYPE: row.find('.orderType').val(),
                ORD_NO: row.find('.orderNo').val(),
                ORD_RATE: row.find('.orderRate').val(),
                DCN_TYPE: row.find('.dcnType').val(),
                DCN_NO: row.find('.dcnNo').val(),

                ROW_ID: row.find('.rowid').val(),
                TAX_CODE: row.find('.taxCode').val(),

                FRT_AMT: row.find('.freightAmount').val(),
                INSU_AMT: row.find('.insuranceAmount').val(),
                CDISC_AMT: row.find('.cashDiscountAmount').val(),
                WB_QTY: row.find('.wbQty').val()
            });
        });

        // Send footer as Footer[0].ITEM_CODE, Footer[1].ITEM_CODE, etc.
        footer.forEach((item, index) => {
            Object.entries(item).forEach(([key, value]) => {
                formData.append(`Footer[${index}].${key}`, value ?? '');
            });
        });

        // =========================
        // SAVE API
        // =========================
        const response = await fetch('/DeliveryOrder/SaveAndUpdateData', {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            throw new Error(`Save request failed (${response.status})`);
        }

        const responseText = await response.text();

        console.log("Save HTTP Status:", response.status);
        console.log("Save Response:", responseText);

        if (!response.ok) {
            throw new Error(`Save request failed (${response.status}): ${responseText}`);
        }

        let result;
         
        try {
            result = JSON.parse(responseText);
        } catch (error) {
            console.error("Invalid JSON response:", responseText);
            throw new Error("Server returned HTML instead of JSON. Check console for the actual error.");
        }

        if (result.success === true) {
            showToast(result.message || 'Delivery Order saved successfully', { type: 'success' });
            console.log('Delivery Order Save Response:', result);
        } else {
            showToast(result.message || 'Failed to save Delivery Order', { type: 'error' });
            console.error('Delivery Order Save Failed:', result.message);
        }
        console.log('Delivery Order Save Response:', result);

    } catch (error) {
        console.error('Delivery Order Save Error:', error);
        showToast('Failed to save Delivery Order: ' + error.message, { type: 'error' });
    }
}

async function LoadEditData(vNo, vType) {
    try {
        const response = await fetch(
            `/DeliveryOrder/LoadEditData?vNo=${encodeURIComponent(vNo)}&vType=${encodeURIComponent(vType)}`
        );

        if (!response.ok) {
            throw new Error('Failed to load Delivery Order');
        }

        const result = await response.json();

        if (!result.success) {
            showToast(result.message || 'Delivery Order not found', { type: 'error' });
            return;
        }

        const data = result.data;
        const footer = result.footer || [];

        // =========================
        // HEADER DATA
        // =========================
        $('#ddlVoucherType').val(data.V_TYPE).trigger('change');
        $('#NumVoucherNo').val(data.V_NO);

        const setValue = (selector, value) => {
            $(selector).val(value ?? '');
        };

        const setDropdown = (selector, value, text) => {
            if (value === null || value === undefined || value === '') {
                $(selector).val('').trigger('change');
                return;
            }

            const ddl = $(selector);
            const stringValue = String(value);

            if (ddl.find('option').filter(function () {
                return String(this.value) === stringValue;
            }).length === 0) {
                ddl.append(new Option(text || stringValue, stringValue, true, true));
            }

            ddl.val(stringValue).trigger('change');
        };

        const setDate = (selector, value) => {
            setValue(selector, value ? String(value).split('T')[0] : '');
        };

        setDate('#DtBillDate', data.V_DATE);
        setDate('#DtDeliveryDate', data.DEL_DATE);
        setDate('#DtGRDate', data.GR_DATE);
        setDate('#DtShippingBillDate', data.SB_DATE);

        // Party and consignee dropdowns
        setDropdown('#ddlPartyName', data.BILL_CODE, data.BILL_NAME);
        setDropdown('#ddlConsignee', data.SHIP_CODE, data.SHIP_NAME);
        setDropdown('#ddlStation', data.BILL_CITY, data.BILL_CITYNAME);
        setDropdown('#TxtTransactionStation', data.SHIP_CITY, data.SHIP_CITYNAME);
        setDropdown('#ddlSoldBy', data.AGENT_CODE, data.AGENT_CODE);
        setDropdown('#ddlFormType', data.FORM_CODE, data.FORM_CODE);
        setDropdown('#ddlProdType', data.ITEM_TYPE, data.ITEM_TYPE);
        setDropdown('#ddlDocStatus', data.STATUS, data.STATUS);
        setDropdown('#ddlTransport', data.TRANSPORT_CODE, data.TRANSPORT_NAME);
        setDropdown('#ddlModeOfTransport', data.TPT_MODE, data.TPT_MODE);
        setDropdown('#ddlSupplyType', data.SUPPLY_TYPE, data.SUPPLY_TYPE);
        setDropdown('#ddlTransactionType', data.TRAN_TYPE, data.TRAN_TYPE);

        // Party address fields
        setValue('#TxtAddressL1', data.BILL_ADD1);
        setValue('#TxtAddressL2', data.BILL_ADD2);
        setValue('#TxtAddressL3', data.BILL_ADD3);
        setValue('#TxtGST', data.BILL_GST);
        setValue('#NumPincode', data.BILL_PINCODE);

        setValue('#TxtTransactionAddressL1', data.SHIP_ADD1);
        setValue('#TxtTransactionAddressL2', data.SHIP_ADD2);
        setValue('#TxtTransactionAddressL3', data.SHIP_ADD3);
        setValue('#NumTransactionGSTNo', data.SHIP_GST);
        setValue('#NumTransactionPIN', data.SHIP_PINCODE);

        // Header fields
        const fields = {
            '#NumOtherTotalAmount': 'AMOUNT',
            '#NumOtherPacking1': 'PACK_PER',
            '#NumOtherPacking2': 'PACK_AMT',
            '#NumOtherTCS1': 'TCS_PER',
            '#NumOtherTCS2': 'TCS_AMT',
            '#NumOtherCGST1': 'CGST_PER',
            '#NumOtherCGST2': 'CGST_AMT',
            '#NumOtherSGST1': 'SGST_PER',
            '#NumOtherSGST2': 'SGST_AMT',
            '#NumOtherIGST1': 'IGST_PER',
            '#NumOtherIGST2': 'IGST_AMT',
            '#NumOtherCESS1': 'CESS_PER',
            '#NumOtherCESS2': 'CESS_AMT',
            '#txtload_p': 'LOAD_PER',
            '#Txtload_amt': 'LOAD_AMT',
            '#ddlLoadParty': 'LOAD_AC',
            '#TxtLoadRemarks': 'LOAD_REM',
            '#NumWBAmt': 'WB_AMT',
            '#ddlWBParty': 'WB_AC',
            '#NumOtherTotalNos': 'TOT_NOS',
            '#NumFreightAmt': 'FRT_AMT',
            '#NumOtherRoundOff': 'ROUND_OFF',
            '#NumOtherNetAmount': 'NAMOUNT',
            '#txtins_p': 'INSU_PER',
            '#txtins_amt': 'INSU_AMT',
            '#txttds_p': 'TDS_PER',
            '#txttds_amt': 'TDS_AMT',
            '#txttaxOnFrt_p': 'FRT_TAXPER',
            '#txttaxOnFrt_Amt': 'FRT_TAXAMT',
            '#TxtWBRemarks': 'WB_REM',
            '#NumNetQuantity': 'TOT_NET',
            '#TxtGRNo': 'GR_NO',
            '#TxtTruckNo': 'VEHICLE_NO',
            '#TxtDriverName': 'DRIVER_NAME',
            '#TxtDriverNo': 'DRIVER_NO',
            '#NumDist': 'TPT_DISTANCE',
            '#TxtWayBillNo': 'WAYBILL_NO',
            '#NumToPayFrt': 'FRT_TOPAY',
            '#TxtRemarks': 'REMARK',
            '#NumOtherDiscount1': 'DISC_PER',
            '#NumOtherDiscount2': 'DISC_AMT',
            '#NumCashDiscount1': 'CDISC_PER',
            '#NumCashDiscount2': 'CDISC_AMT',
            '#NumSaudaNo': 'SAUDA_NO',
            '#NumExRate': 'EXRATE',
            '#TxtBuyerOrder': 'BUYER_ORDNO',
            '#TxtPlaceOfReceipt': 'PLACE_RECEIPT',
            '#TxtPortOfLoading': 'PORT_LOADING',
            '#TxtPortOfDischarge': 'PORT_DISCHARGE',
            '#TxtFinalDestination': 'FINAL_DEST',
            '#TxtFDCountry': 'FINAL_DEST_COUNTRY',
            '#TxtDeliveryTerms': 'DELIVERY_TERMS',
            '#TxtShippingBillNo': 'SB_NO',
            '#TxtPortCode': 'PORT_CODE',
            '#TxtLUTDetail': 'LUT_DETAIL',
            '#TxtInsuranceDetail': 'INSU_DETAIL',
            '#TxtLCNo': 'LC_NO',
            '#TxtPaymentTerm': 'PAYMENT_TERM'
        };

        Object.entries(fields).forEach(([selector, key]) => {
            setValue(selector, data[key]);
        });

        $('#cbCalOnPcs').prop('checked', String(data.CAL_ONPCS) === '1');
        $('#ChkDetail').prop('checked', String(data.PRINT_DETAIL) === '1');

        setDropdown('#ddlWBNo', data.WB_NO, data.WB_NO);
        setDropdown('#ddlIssueNo', data.ISSUE_NO, data.ISSUE_NO);

        if (data.BILL_CITY) {
            await BindTaxDDL(data.BILL_CITY);
            setDropdown('#ddlTaxType', data.TAX_CODE, data.TAX_CODE);
        }

        // =========================
        // FOOTER / ITEM ROWS
        // =========================
        const tbody = $('#tblDeliveryorder tbody');
        tbody.empty();

        for (const item of footer) {
            await addNewRow();

            const row = tbody.find('tr:last');
            const itemCode = String(item.ITEM_CODE ?? '');
            const productDDL = row.find('.productName');

            let option = productDDL.find('option').filter(function () {
                return String(this.value) === itemCode;
            });

            if (!option.length && itemCode) {
                productDDL.append(
                    new Option(item.ITEM_NAME || itemCode, itemCode, true, true)
                );
            }

            productDDL.val(itemCode);

            row.find('.productCode').val(item.ITEM_CODE ?? '');
            row.find('.nos').val(item.NOS ?? '');
            row.find('.grossQuantity').val(item.GROSS_QTY ?? '');
            row.find('.netQuantity').val(item.QTY ?? '');
            row.find('.rateUsd').val(item.FOR_RATE ?? '');
            row.find('.rateRs').val(item.RATE ?? '');
            row.find('.amount').val(item.AMOUNT ?? '');
            row.find('.packPer').val(item.PACK_PER ?? '');
            row.find('.packAmount').val(item.PACK_AMT ?? '');
            row.find('.discPer').val(item.DISC_PER ?? '');
            row.find('.discAmount').val(item.DISC_AMT ?? '');
            row.find('.remark').val(item.REMARK ?? '');
            row.find('.packType').val(item.PACK_TYPE ?? '');
            row.find('.packNo').val(item.PACK_NO ?? '');
            row.find('.lotNo').val(item.LOT_NO ?? '');
            row.find('.saudaType').val(item.SAUDA_TYPE ?? '');
            row.find('.saudaNo').val(item.SAUDA_NO ?? '');
            row.find('.saudaRate').val(item.SAUDA_RATE ?? '');
            row.find('.orderType').val(item.ORD_TYPE ?? '');
            row.find('.orderNo').val(item.ORD_NO ?? '');
            row.find('.orderRate').val(item.ORD_RATE ?? '');
            row.find('.dcnType').val(item.DCN_TYPE ?? '');
            row.find('.dcnNo').val(item.DCN_NO ?? '');
            row.find('.hsn').val(item.HSN_CODE ?? '');
            row.find('.rowid').val(item.ROW_ID ?? item.DOROW_ID ?? '');
            row.find('.taxCode').val(item.TAX_CODE ?? '');
            row.find('.freightAmount').val(item.FRT_AMT ?? '');
            row.find('.insuranceAmount').val(item.INSU_AMT ?? '');
            row.find('.cashDiscountAmount').val(item.CDISC_AMT ?? '');
            row.find('.wbQty').val(item.WB_QTY ?? '');

            row.find('.cgstPer').val(item.CGST_PER ?? '');
            row.find('.cgstAmount').val(item.CGST_AMT ?? '');
            row.find('.sgstPer').val(item.SGST_PER ?? '');
            row.find('.sgstAmount').val(item.SGST_AMT ?? '');
            row.find('.igstPer').val(item.IGST_PER ?? '');
            row.find('.igstAmount').val(item.IGST_AMT ?? '');
            row.find('.cessPer').val(item.CESS_PER ?? '');
            row.find('.cessAmount').val(item.CESS_AMT ?? '');
        }

        if (footer.length === 0) {
            await addNewRow();
        }

        if (isReadOnly) {
            $('input, select, textarea, button').prop('disabled', true);
        }

    } catch (error) {
        console.error('Load Delivery Order Error:', error);
        showToast('Failed to load Delivery Order: ' + error.message, { type: 'error' });
    }
}

// =========================
// TABLE TOTAL HELPER
// =========================
function getTableTotal(selector) {
    let total = 0;

    $('#tblDeliveryorder tbody tr').each(function () {
        total += Number($(this).find(selector).val()) || 0;
    });

    return total;
}
       
//===============================
//  Recocile Delivery order(check maxching or not )
//===============================

async function ReconcileDeliveryOrder() {
    try {
        const footer = [];

        $('#tblDeliveryorder tbody tr').each(function () {
            const row = $(this);
            const itemCode = row.find('.productCode').val();

            if (!itemCode) {
                return;
            }

            footer.push({
                ITEM_CODE: parseInt(itemCode) || 0,
                DOROW_ID: row.find('.rowid').val() || '',

                ITEM_NAME: row.find('.productName option:selected').text(),
                HSN_CODE: row.find('.hsn').val() || '',

                NOS: parseFloat(row.find('.nos').val()) || 0,
                GROSS_QTY: parseFloat(row.find('.grossQuantity').val()) || 0,
                QTY: parseFloat(row.find('.netQuantity').val()) || 0,

                RATE_USD: parseFloat(row.find('.rateUsd').val()) || 0,
                RATE_RS: parseFloat(row.find('.rateRs').val()) || 0,

                CGST_PER: parseFloat(row.find('.cgstPer').val()) || 0,
                IGST_PER: parseFloat(row.find('.igstPer').val()) || 0,

                ORD_TYPE: row.find('.orderType').val() || '',
                ORD_NO: parseInt(row.find('.orderNo').val()) || 0,
                ORD_RATE: parseFloat(row.find('.orderRate').val()) || 0,

                SAUDA_TYPE: row.find('.saudaType').val() || '',
                SAUDA_NO: parseInt(row.find('.saudaNo').val()) || 0,
                SAUDA_RATE: parseFloat(row.find('.saudaRate').val()) || 0,

                ROW_PACK_NO: parseInt(row.find('.packNo').val()) || 0,

                DC_TYPE: row.find('.dcnType').val() || '',
                DC_NO: parseInt(row.find('.dcnNo').val()) || 0,

                WB_QTY: parseFloat(row.find('.wbQty').val()) || 0
            });
        });

        const model = {
            V_TYPE: $('#ddlVoucherType').val(),
            V_NO: parseInt($('#NumVoucherNo').val()) || 0,
            V_DATE: $('#DtBillDate').val(),

            PARTY_CODE: parseInt($('#TxtCode').val()) || 0,
            SUPPLY_TYPE: $('#ddlSupplyType').val(),
            PROD_TYPE: $('#ddlProdType option:selected').text(),
            FORM_TYPE: $('#ddlFormType').val() || '',

            SUBTOTAL: parseFloat($('#NumSubTotal').val()) || 0,
            NET_AMOUNT: parseFloat($('#NumOtherNetAmount').val()) || 0,

            FREIGHT: parseFloat($('#NumFreightAmt').val()) || 0,
            INSURANCE_PERCENT: parseFloat($('#txtins_p').val()) || 0,
            INSURANCE_AMOUNT: parseFloat($('#txtins_amt').val()) || 0,
            TDS_PERCENT: parseFloat($('#txttds_p').val()) || 0,
            TDS_AMOUNT: parseFloat($('#txttds_amt').val()) || 0,

            PACK_NO: parseInt($('#ddlPackNo').val()) || 0,
            PACK_TYPE: $('#ddlPackNo option:selected').attr('data-vtype') || '',

            ISSUE_TYPE: $('#ddlIssueNo option:selected').data('vtype') || '',
            ISSUE_NO: parseInt($('#ddlIssueNo').val()) || 0,

            TAX_CODE: parseInt($('#ddlTaxType').val()) || 0,

            TRANSPORT_CODE: parseInt($('#ddlTransport').val()) || 0,
            TRANSPORT_NAME: $('#ddlTransport option:selected').text() || '',
            GR_NO: $('#TxtGRNo').val() || '',
            GR_DATE: $('#DtGRDate').val() || null,
            DRIVER_NAME: $('#TxtDriverName').val() || '',
            DRIVER_NO: $('#TxtDriverNo').val() || '',

            TCS_PERCENT: parseFloat($('#NumOtherTCS1').val()) || 0,

            CGST_AMOUNT: parseFloat($('#NumOtherCGST2').val()) || 0,
            SGST_AMOUNT: parseFloat($('#NumOtherSGST2').val()) || 0,
            IGST_AMOUNT: parseFloat($('#NumOtherIGST2').val()) || 0,

            STATION_CODE: parseInt($('#ddlStation').val()) || 0,
            TRANSPORT_DISTANCE: parseFloat($('#NumDist').val()) || 0,
            TRANSPORT_MODE: $('#ddlModeOfTransport').val() || '',

            CAL_ON_PCS: $('#cbCalOnPcs').is(':checked'),
            EXCHANGE_RATE: parseFloat($('#NumExRate').val()) || 0,

            Footer: footer
        };

        const response = await fetch('/DeliveryOrder/ReconcileDeliveryOrder', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(model)
        });

        const result = await response.json();

        if (!response.ok || result.success === false) {
            showToast(result.message || 'Reconciliation failed', { type: 'error' });
            return;
        }

        // ---------------- TRANSPORT DETAILS ----------------
        const transportCode = result.transportCode ?? result.TRANSPORT_CODE;
        const transportName = result.transportName ?? result.TRANSPORT_NAME;

        if (transportCode) {
            if ($('#ddlTransport option[value="' + transportCode + '"]').length === 0) {
                $('#ddlTransport').append(
                    new Option(transportName || '', transportCode, true, true)
                );
            }

            $('#ddlTransport').val(String(transportCode)).trigger('change');
        }
        $('#TxtTruckNo').val(result.truckNo ?? result.TRUCK_NO ?? '');
        $('#TxtDriverName').val(result.driverName ?? result.DRIVER_NAME ?? '');
        $('#TxtDriverNo').val(result.driverNo ?? result.DRIVER_NO ?? '');
        $('#TxtGRNo').val(result.grNo ?? result.GR_NO ?? '');

        const grDate = result.grDate ?? result.GR_DATE;
        $('#DtGRDate').val(grDate ? String(grDate).substring(0, 10) : '');

        // ---------------- WEIGHBRIDGE ----------------
        const wbType = result.wbType ?? result.WB_TYPE;
        const wbNo = result.wbNo ?? result.WB_NO;
        const wbQty = result.wbQty ?? result.WB_QTY;

        if (wbNo) {
            const wbValue = String(wbNo);
            if ($('#ddlWBNo option[value="' + wbValue + '"]').length === 0) {
                $('#ddlWBNo').append(
                    new Option(wbValue, wbValue, true, true)
                );
            }

            $('#ddlWBNo').val(wbValue).trigger('change');

            if (wbType) {
                $('#ddlWBNo option:selected').attr('data-vtype', wbType);
            }
        }
        $('#NumWbQuantity').val(wbQty ?? 0);

        // ---------------- BUYER ORDER / PAYMENT TERM ----------------
        $('#TxtBuyerOrder').val(result.buyerOrderNo ?? result.BUYER_ORDNO ?? '');

        $('#TxtPaymentTerm').val(result.paymentTerm ?? result.PAYMENT_TERM ?? '');

        // ---------------- FREIGHT / INSURANCE / TDS ----------------
        $('#NumFreightAmt').val( result.transportFreight ?? result.TRANSPORT_FREIGHT ?? result.freight ?? result.FREIGHT ?? 0);

        $('#txtins_p').val(result.insurancePercent ?? result.INSURANCE_PERCENT ?? 0);

        $('#txtins_amt').val( result.insuranceAmount ?? result.INSURANCE_AMOUNT ?? 0);

        $('#txttds_p').val(result.tdsPercent ?? result.TDS_PERCENT ?? 0);

        $('#txttds_amt').val( result.tdsAmount ?? result.TDS_AMOUNT ?? 0 );

        // ---------------- PACKING VOUCHER ----------------
        const packType = result.packType ?? result.PACK_TYPE;
        const packNo = result.packNo ?? result.PACK_NO;

        if (packNo) {
            const packValue = String(packNo);

            if ($('#ddlPackNo option[value="' + packValue + '"]').length === 0) {
                $('#ddlPackNo').append(
                    new Option(packValue, packValue, true, true)
                );
            }

            $('#ddlPackNo').val(packValue).trigger('change');

            if (packType) {
                $('#ddlPackNo option:selected').attr('data-vtype', packType);
            }
        }

        // ---------------- ISSUE VOUCHER ----------------
        const issueType = result.issueType ?? result.ISSUE_TYPE;
        const issueNo = result.issueNo ?? result.ISSUE_NO;

        if (issueNo) {
            const issueValue = String(issueNo);

            if ($('#ddlIssueNo option[value="' + issueValue + '"]').length === 0) {
                $('#ddlIssueNo').append(
                    new Option(issueValue, issueValue, true, true)
                );
            }

            $('#ddlIssueNo').val(issueValue).trigger('change');

            if (issueType) {
                $('#ddlIssueNo option:selected').attr('data-vtype', issueType);
            }
        }

        // ---------------- TAX PERCENTAGES ----------------
        const cgstPercent = result.cgstPercent ?? result.CGST_PERCENT;
        const sgstPercent = result.sgstPercent ?? result.SGST_PERCENT;
        const igstPercent = result.igstPercent ?? result.IGST_PERCENT;

        if (cgstPercent !== undefined) {
            $('#NumOtherCGST1').val(cgstPercent);
        }

        if (sgstPercent !== undefined) {
            $('#NumOtherSGST1').val(sgstPercent);
        }

        if (igstPercent !== undefined) {
            $('#NumOtherIGST1').val(igstPercent);
        }

        // ---------------- FOOTER ITEM UPDATES ----------------
        const itemUpdates = result.itemUpdates ?? result.ItemUpdates ?? [];

        itemUpdates.forEach(function (item) {
            const itemCode = item.item_CODE ?? item.ITEM_CODE;
            const rowId = item.dorow_ID ?? item.DOROW_ID;
            const source = item.source ?? item.SOURCE;

            let row = $();

            if (source === 'Packing') {
                // Packing: ITEM_CODE + DOROW_ID both must match
                if (rowId === undefined || rowId === null || rowId === '') {
                    return;
                }

                row = $('#tblDeliveryorder tbody tr').filter(function () {
                    const currentRow = $(this);

                    return String(currentRow.find('.productCode').val()) === String(itemCode) &&
                        String(currentRow.find('.rowid').val() || '') === String(rowId);
                })

            } else if (source === 'Issue') {
                row = $('#tblDeliveryorder tbody tr').filter(function () {
                    return String($(this).find('.productCode').val()) === String(itemCode);
                });

            } else {
                return;
            }

            if (!row.length) {
                return;
            }

            const nos = item.nos ?? item.NOS;
            const grossQty = item.gross_QTY ?? item.GROSS_QTY;
            const qty = item.qty ?? item.QTY;
            const hsnCode = item.hsn_CODE ?? item.HSN_CODE;
            const taxCode = item.tax_CODE ?? item.TAX_CODE;

            if (nos !== undefined) {
                row.find('.nos').val(nos);
            }

            if (grossQty !== undefined) {
                row.find('.grossQuantity').val(grossQty);
            }

            if (qty !== undefined) {
                row.find('.netQuantity').val(qty);
            }

            if (hsnCode !== undefined) {
                row.find('.hsn').val(hsnCode);
            }

            if (taxCode !== undefined) {
                row.find('.taxCode').val(taxCode);
            }

            const itemPackNo = item.pack_NO ?? item.PACK_NO;

            if (itemPackNo !== undefined) {
                row.find('.packNo').val(itemPackNo);
            }

            const wbQty = item.wb_QTY ?? item.WB_QTY;

            if (wbQty !== undefined) {
                row.find('.wbQty').val(wbQty);
            }
        });

        // ---------------- RECONCILIATION MESSAGES ----------------
        const messages = result.messages ?? result.Messages ?? [];

        messages.forEach(function (message) {
            showToast(message, { type: 'warning' });
        });

        showToast(result.message || 'Reconciliation completed', {type: 'success' });

        await UpdateDeliveryOrder();

    } catch (error) {
        console.error('Reconcile Delivery Order Error:', error);
        showToast('Failed to reconcile Delivery Order: ' + error.message, {type: 'error'});
    }
}

//===========================
// Proforma Invoice
//===========================

$(document).on('click', '#btnCreateProforma', function (e) {
    e.preventDefault();
    GetProformaVNo();
});

function getProformaModel() {
    const model = {
        V_NO: parseInt($('#NumVoucherNo').val(), 10) || 0,
        V_TYPE: $('#ddlVoucherType').val(),
        V_DATE: $('#DtBillDate').val() || null,
        DEL_DATE: $('#DtDeliveryDate').val() || null,

        GODOWN_CODE: null,

        BILL_CODE: $('#ddlPartyName').val() ? parseInt($('#ddlPartyName').val(), 10) : null,
        BILL_NAME: $('#ddlPartyName option:selected').text(),
        BILL_ADD1: $('#TxtAddressL1').val(),
        BILL_ADD2: $('#TxtAddressL2').val(),
        BILL_ADD3: $('#TxtAddressL3').val(),
        BILL_CITY: $('#ddlStation').val() ? parseInt($('#ddlStation').val(), 10) : null,
        BILL_CITYNAME: $('#ddlStation option:selected').text(),
        BILL_GST: $('#TxtGST').val(),
        BILL_PINCODE: $('#NumPincode').val(),

        SHIP_CODE: $('#ddlConsignee').val() ? parseInt($('#ddlConsignee').val(), 10) : null,
        SHIP_NAME: $('#ddlConsignee option:selected').text(),
        SHIP_ADD1: $('#TxtTransactionAddressL1').val(),
        SHIP_ADD2: $('#TxtTransactionAddressL2').val(),
        SHIP_ADD3: $('#TxtTransactionAddressL3').val(),
        SHIP_CITY: $('#TxtTransactionStation').val() ? parseInt($('#TxtTransactionStation').val(), 10) : null,
        SHIP_CITYNAME: $('#TxtTransactionStation option:selected').text(),
        SHIP_GST: $('#NumTransactionGSTNo').val(),
        SHIP_PINCODE: $('#NumTransactionPIN').val(),

        TAX_CODE: $('#ddlTaxType').val() ? parseInt($('#ddlTaxType').val(), 10) : null,
        PACK_TYPE: $('#ddlPackNo option:selected').data('vtype') || '',
        PACK_NO: $('#ddlPackNo').val() ? parseInt($('#ddlPackNo').val(), 10) : null,
        ITEM_TYPE: $('#ddlProdType').val(),

        WB_NO: $('#ddlWBNo').val() ? parseInt($('#ddlWBNo').val(), 10) : null,
        WB_TYPE: $('#ddlWBNo option:selected').data('vtype') || '',

        ISSUE_NO: $('#ddlIssueNo').val() ? parseInt($('#ddlIssueNo').val(), 10) : null,
        ISSUE_TYPE: $('#ddlIssueNo option:selected').data('vtype') || '',

        AMOUNT: $('#NumOtherTotalAmount').val() !== '' ? parseFloat($('#NumOtherTotalAmount').val()) : null,
        PACK_PER: $('#NumOtherPacking1').val() !== '' ? parseFloat($('#NumOtherPacking1').val()) : null,
        PACK_AMT: $('#NumOtherPacking2').val() !== '' ? parseFloat($('#NumOtherPacking2').val()) : null,

        TCS_PER: $('#NumOtherTCS1').val() !== '' ? parseFloat($('#NumOtherTCS1').val()) : null,
        TCS_AMT: $('#NumOtherTCS2').val() !== '' ? parseFloat($('#NumOtherTCS2').val()) : null,

        CGST_PER: $('#NumOtherCGST1').val() !== '' ? parseFloat($('#NumOtherCGST1').val()) : null,
        CGST_AMT: $('#NumOtherCGST2').val() !== '' ? parseFloat($('#NumOtherCGST2').val()) : null,
        SGST_PER: $('#NumOtherSGST1').val() !== '' ? parseFloat($('#NumOtherSGST1').val()) : null,
        SGST_AMT: $('#NumOtherSGST2').val() !== '' ? parseFloat($('#NumOtherSGST2').val()) : null,
        IGST_PER: $('#NumOtherIGST1').val() !== '' ? parseFloat($('#NumOtherIGST1').val()) : null,
        IGST_AMT: $('#NumOtherIGST2').val() !== '' ? parseFloat($('#NumOtherIGST2').val()) : null,
        CESS_PER: $('#NumOtherCESS1').val() !== '' ? parseFloat($('#NumOtherCESS1').val()) : null,
        CESS_AMT: $('#NumOtherCESS2').val() !== '' ? parseFloat($('#NumOtherCESS2').val()) : null,

        LOAD_PER: $('#NumLoadPer').val() !== '' ? parseFloat($('#NumLoadPer').val()) : null,
        LOAD_AMT: $('#Txtload_amt').val() !== '' ? parseFloat($('#Txtload_amt').val()) : null,
        LOAD_AC: $('#ddlLoadParty').val(),
        LOAD_REM: $('#TxtLoadRemarks').val(),

        WB_AMT: $('#NumWBAmt').val() !== '' ? parseFloat($('#NumWBAmt').val()) : null,
        WB_AC: $('#ddlWBParty').val(),

        TOT_NOS: $('#NumOtherTotalNos').val() !== '' ? parseInt($('#NumOtherTotalNos').val(), 10) : null,
        TOT_GROSS: getTableTotal('.grossQuantity'),
        TOT_NET: $('#NumNetQuantity').val() !== '' ? parseFloat($('#NumNetQuantity').val()) : getTableTotal('.netQuantity'),

        FRT_AMT: $('#NumFreightAmt').val() !== '' ? parseFloat($('#NumFreightAmt').val()) : null,
        ROUND_OFF: $('#NumOtherRoundOff').val() !== '' ? parseFloat($('#NumOtherRoundOff').val()) : null,
        NAMOUNT: $('#NumOtherNetAmount').val() !== '' ? parseFloat($('#NumOtherNetAmount').val()) : null,

        INSU_PER: $('#txtins_p').val() !== '' ? parseFloat($('#txtins_p').val()) : null,
        INSU_AMT: $('#txtins_amt').val() !== '' ? parseFloat($('#txtins_amt').val()) : null,
        TDS_PER: $('#txttds_p').val(),
        TDS_AMT: $('#txttds_amt').val() !== '' ? parseFloat($('#txttds_amt').val()) : null,

        FRT_TAXPER: $('#txttaxOnFrt_p').val() !== '' ? parseFloat($('#txttaxOnFrt_p').val()) : null,
        FRT_TAXAMT: $('#txttaxOnFrt_Amt').val() !== '' ? parseFloat($('#txttaxOnFrt_Amt').val()) : null,

        WB_REM: $('#TxtWBRemarks').val(),

        GR_NO: $('#TxtGRNo').val(),
        GR_DATE: $('#DtGRDate').val() || null,
        VEHICLE_NO: $('#TxtTruckNo').val(),

        TRANSPORT_CODE: $('#ddlTransport').val() ? parseInt($('#ddlTransport').val(), 10) : null,
        TRANSPORT_NAME: $('#ddlTransport option:selected').text(),

        DRIVER_NAME: $('#TxtDriverName').val(),
        DRIVER_NO: $('#TxtDriverNo').val(),

        TPT_MODE: $('#ddlModeOfTransport').val() ? parseInt($('#ddlModeOfTransport').val(), 10) : null,
        TPT_DISTANCE: $('#NumDist').val() !== '' ? parseInt($('#NumDist').val(), 10) : null,

        WAYBILL_NO: $('#TxtWayBillNo').val(),
        FRT_TOPAY: $('#NumToPayFrt').val() !== '' ? parseFloat($('#NumToPayFrt').val()) : null,
        REMARK: $('#TxtRemarks').val(),
        WB_QTY: null,

        DISC_PER: $('#NumOtherDiscount1').val() !== '' ? parseFloat($('#NumOtherDiscount1').val()) : null,
        DISC_AMT: $('#NumOtherDiscount2').val() !== '' ? parseFloat($('#NumOtherDiscount2').val()) : null,

        CDISC_PER: $('#NumCashDiscount1').val() !== '' ? parseFloat($('#NumCashDiscount1').val()) : null,
        CDISC_AMT: $('#NumCashDiscount2').val() !== '' ? parseFloat($('#NumCashDiscount2').val()) : null,

        AGENT_CODE: $('#ddlSoldBy').val() ? parseInt($('#ddlSoldBy').val(), 10) : null,
        FORM_CODE: $('#ddlFormType').val() ? parseInt($('#ddlFormType').val(), 10) : null,

        SAUDA_TYPE: null,
        SAUDA_NO: $('#NumSaudaNo').val() ? parseInt($('#NumSaudaNo').val(), 10) : null,
        SAUDA_RATE: null,

        DEFECTIVE_GOODS: null,
        CAL_ONPCS: $('#cbCalOnPcs').is(':checked') ? 1 : 0,
        PRINT_DETAIL: $('#ChkDetail').is(':checked') ? 1 : 0,

        EXRATE: $('#NumExRate').val() !== '' ? parseFloat($('#NumExRate').val()) : null,

        BUYER_ORDNO: $('#TxtBuyerOrder').val(),
        PLACE_RECEIPT: $('#TxtPlaceOfReceipt').val(),
        PORT_LOADING: $('#TxtPortOfLoading').val(),
        PORT_DISCHARGE: $('#TxtPortOfDischarge').val(),

        FINAL_DEST: $('#TxtFinalDestination').val(),
        FINAL_DEST_COUNTRY: $('#TxtFDCountry').val(),
        DELIVERY_TERMS: $('#TxtDeliveryTerms').val(),

        SB_NO: $('#TxtShippingBillNo').val(),
        SB_DATE: $('#DtShippingBillDate').val() || null,
        PORT_CODE: $('#TxtPortCode').val(),

        LUT_DETAIL: $('#TxtLUTDetail').val(),
        INSU_DETAIL: $('#TxtInsuranceDetail').val(),

        SUPPLY_TYPE: $('#ddlSupplyType').val(),
        LC_NO: $('#TxtLCNo').val(),
        TRAN_TYPE: $('#ddlTransactionType').val(),
        STATUS: $('#ddlDocStatus').val() ? parseInt($('#ddlDocStatus').val(), 10) : null,
        PAYMENT_TERM: $('#TxtPaymentTerm').val(),

        Footer: []
    };

    // FOOTER DATA
    $('#tblDeliveryorder tbody tr').each(function () {

        const row = $(this);
        const itemCode = row.find('.productCode').val();

        if (!itemCode) return;

        model.Footer.push({

            ITEM_CODE: parseInt(itemCode, 10),
            ITEM_NAME: row.find('.productName option:selected').text(),

            UNIT_NAME: row.find('.unitName').val() || '',
            UNIT_CODE: row.find('.unitCode').val() ? parseInt(row.find('.unitCode').val(), 10) : null,
            HSN_CODE: row.find('.hsn').val() || '',

            NOS: row.find('.nos').val() !== '' ? parseInt(row.find('.nos').val(), 10) : null,
            GROSS_QTY: row.find('.grossQuantity').val() !== '' ? parseFloat(row.find('.grossQuantity').val()) : null,
            QTY: row.find('.netQuantity').val() !== '' ? parseFloat(row.find('.netQuantity').val()) : null,

            FOR_RATE: row.find('.rateUsd').val() !== '' ? parseFloat(row.find('.rateUsd').val()) : null,
            RATE: row.find('.rateRs').val() !== '' ? parseFloat(row.find('.rateRs').val()) : null,
            AMOUNT: row.find('.amount').val() !== '' ? parseFloat(row.find('.amount').val()) : null,

            PACK_PER: row.find('.packPer').val() !== '' ? parseFloat(row.find('.packPer').val()) : null,
            PACK_AMT: row.find('.packAmount').val() !== '' ? parseFloat(row.find('.packAmount').val()) : null,

            DISC_PER: row.find('.discPer').val() !== '' ? parseFloat(row.find('.discPer').val()) : null,
            DISC_AMT: row.find('.discAmount').val() !== '' ? parseFloat(row.find('.discAmount').val()) : null,

            REMARK: row.find('.remark').val(),
            STATUS: model.STATUS,

            PACK_TYPE: row.find('.packType').val() || '',
            PACK_NO: row.find('.packNo').val() ? parseInt(row.find('.packNo').val(), 10) : null,
            LOT_No: row.find('.lotNo').val(),

            SAUDA_TYPE: row.find('.saudaType').val(),
            SAUDA_NO: row.find('.saudaNo').val() ? parseInt(row.find('.saudaNo').val(), 10) : null,
            SAUDA_RATE: row.find('.saudaRate').val() !== '' ? parseFloat(row.find('.saudaRate').val()) : null,

            ORD_TYPE: row.find('.orderType').val(),
            ORD_NO: row.find('.orderNo').val() ? parseInt(row.find('.orderNo').val(), 10) : null,
            ORD_RATE: row.find('.orderRate').val() !== '' ? parseFloat(row.find('.orderRate').val()) : null,

            DCN_TYPE: row.find('.dcnType').val(),
            DCN_NO: row.find('.dcnNo').val() ? parseInt(row.find('.dcnNo').val(), 10) : null,

            ROW_ID: row.find('.rowid').val(),
            TAX_CODE: row.find('.taxCode').val() ? parseInt(row.find('.taxCode').val(), 10) : null,

            FRT_AMT: row.find('.freightAmount').val() !== '' ? parseFloat(row.find('.freightAmount').val()) : null,
            INSU_AMT: row.find('.insuranceAmount').val() !== '' ? parseFloat(row.find('.insuranceAmount').val()) : null,
            CDISC_AMT: row.find('.cashDiscountAmount').val() !== '' ? parseFloat(row.find('.cashDiscountAmount').val()) : null,
            WB_QTY: row.find('.wbQty').val() !== '' ? parseFloat(row.find('.wbQty').val()) : null
        });
    });

    return model;
}

async function GetProformaVNo() {
    try {
        const response = await fetch('/DeliveryOrder/GetProformaV_NO');

        if (!response.ok) {
            throw new Error('Failed to generate VNo');
        }

        const data = await response.json();

        if (data.error) {
            showToast("Voucher No Error: " + data.error, { type: "error" });
            return;
        }

        const vNo = data.v_NO;
        const docId = 'DMEU' + vNo;

        console.log("Proforma VNo:", vNo);
        console.log("Proforma DocId:", docId);

        // Existing page model ko yahan apne actual form-collection
        // code se populate karna hoga.
        const model = getProformaModel();

        console.log("Proforma JSON:", JSON.stringify(model));

        const saveResponse = await fetch(
            `/DeliveryOrder/SaveProformaData?vNo=${encodeURIComponent(vNo)}`,
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(model)
            }
        );

        const result = await saveResponse.json();
        console.log("Controller Response:", result);

        if (!saveResponse.ok || !result.success) {
            showToast(result.message || "Proforma save failed.", { type: "error" });
            return;
        }

        showToast("Proforma saved successfully: " + docId, { type: "success" });

    } catch (error) {
        console.error('Proforma Error:', error);
        showToast("Error: " + error.message, { type: "error" });
    }
}

//===========================
// Sales Invoice
//===========================

$(document).on('click', '#btnCreateSalesInvoice', function (e) {
    e.preventDefault();
    GetSalesInvoiceVNo();
});

function getSalesInvoiceModel(vNo) {
    const model = {
        ...getProformaModel(),
        V_NO: parseInt(vNo, 10),
        V_TYPE: "SAGT"
    };

    return model;
}

//===========================
// Sales Invoice VNo & Save
//===========================
async function GetSalesInvoiceVNo() {
    try {

        const vType = "SAGT";

        const response = await fetch('/DeliveryOrder/GetSalesInvoiceV_NO');

        if (!response.ok) {
            throw new Error('Failed to generate Sales Invoice VNo');
        }

        const data = await response.json();

        if (data.error) {
            showToast("Voucher No Error: " + data.error, { type: "error" });
            return;
        }

        const vNo = data.v_NO;
        const docId = vType + vNo;

        console.log("Sales Invoice VNo:", vNo);
        console.log("Sales Invoice DocId:", docId);

        // Get Sales Invoice Model
        const model = getSalesInvoiceModel(vNo);

        console.log("Sales Invoice JSON:", JSON.stringify(model));

        // Save Sales Invoice
        const saveResponse = await fetch(
            `/DeliveryOrder/SaveSaleData?vNo=${encodeURIComponent(vNo)}`,
            {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Accept': 'application/json'
                },
                body: JSON.stringify(model)
            }
        );

        const result = await saveResponse.json();

        console.log("Controller Response:", result);

        if (!saveResponse.ok || !result.success) {
            showToast(result.message || "Sales Invoice save failed.", { type: "error" });
            return;
        }

        showToast("Sales Invoice saved successfully: " + docId, { type: "success" });

    } catch (error) {
        console.error('Sales Invoice Error:', error);
        showToast("Error: " + error.message, { type: "error" });
    }
}

//============================
// UPDATE DO AFTER RECONCILE
//============================
async function UpdateDeliveryOrder() {
    try {
        const vType = $('#ddlVoucherType').val();
        const vNo = $('#NumVoucherNo').val();

        const header = {
            V_TYPE: vType,
            V_NO: parseInt(vNo, 10),

            V_DATE: $('#DtBillDate').val(),
            DEL_DATE: $('#DtDeliveryDate').val(),
            GODOWN_CODE: '',

            BILL_CODE: $('#ddlPartyName').val(),
            BILL_NAME: $('#ddlPartyName option:selected').text(),
            BILL_ADD1: $('#TxtAddressL1').val(),
            BILL_ADD2: $('#TxtAddressL2').val(),
            BILL_ADD3: $('#TxtAddressL3').val(),
            BILL_CITY: $('#ddlStation').val(),
            BILL_CITYNAME: $('#ddlStation option:selected').text(),
            BILL_GST: $('#TxtGST').val(),
            BILL_PINCODE: $('#NumPincode').val(),

            SHIP_CODE: $('#ddlConsignee').val(),
            SHIP_NAME: $('#ddlConsignee option:selected').text(),
            SHIP_ADD1: $('#TxtTransactionAddressL1').val(),
            SHIP_ADD2: $('#TxtTransactionAddressL2').val(),
            SHIP_ADD3: $('#TxtTransactionAddressL3').val(),
            SHIP_CITY: $('#TxtTransactionStation').val(),
            SHIP_CITYNAME: $('#TxtTransactionStation option:selected').text(),
            SHIP_GST: $('#NumTransactionGSTNo').val(),
            SHIP_PINCODE: $('#NumTransactionPIN').val(),

            TAX_CODE: $('#ddlTaxType').val(),
            PACK_TYPE: $('#ddlPackNo option:selected').data('vtype') || '',
            PACK_NO: $('#ddlPackNo').val(),
            ITEM_TYPE: $('#ddlProdType').val(),
            WB_NO: $('#ddlWBNo').val(),
            WB_TYPE: $('#ddlWBNo option:selected').data('vtype') || '',
            ISSUE_NO: $('#ddlIssueNo').val(),
            ISSUE_TYPE: $('#ddlIssueNo option:selected').data('vtype') || '',

            AMOUNT: $('#NumOtherTotalAmount').val(),
            PACK_PER: $('#NumOtherPacking1').val(),
            PACK_AMT: $('#NumOtherPacking2').val(),
            TCS_PER: $('#NumOtherTCS1').val(),
            TCS_AMT: $('#NumOtherTCS2').val(),
            CGST_PER: $('#NumOtherCGST1').val(),
            CGST_AMT: $('#NumOtherCGST2').val(),
            SGST_PER: $('#NumOtherSGST1').val(),
            SGST_AMT: $('#NumOtherSGST2').val(),
            IGST_PER: $('#NumOtherIGST1').val(),
            IGST_AMT: $('#NumOtherIGST2').val(),
            CESS_PER: $('#NumOtherCESS1').val(),
            CESS_AMT: $('#NumOtherCESS2').val(),

            LOAD_PER: $('#txtload_p').val(),
            LOAD_AMT: $('#Txtload_amt').val(),
            LOAD_AC: $('#ddlLoadParty').val(),
            LOAD_REM: $('#TxtLoadRemarks').val(),
            WB_AMT: $('#NumWBAmt').val(),
            WB_AC: $('#ddlWBParty').val(),
            TOT_NOS: $('#NumOtherTotalNos').val(),
            FRT_AMT: $('#NumFreightAmt').val(),
            ROUND_OFF: $('#NumOtherRoundOff').val(),
            NAMOUNT: $('#NumOtherNetAmount').val(),

            INSU_PER: $('#txtins_p').val(),
            INSU_AMT: $('#txtins_amt').val(),
            TDS_PER: $('#txttds_p').val(),
            TDS_AMT: $('#txttds_amt').val(),
            FRT_TAXPER: $('#txttaxOnFrt_p').val(),
            FRT_TAXAMT: $('#txttaxOnFrt_Amt').val(),
            WB_REM: $('#TxtWBRemarks').val(),
            TOT_GROSS: getTableTotal('.grossQuantity'),
            TOT_NET: $('#NumNetQuantity').val() || getTableTotal('.netQuantity'),

            GR_NO: $('#TxtGRNo').val(),
            GR_DATE: $('#DtGRDate').val(),
            VEHICLE_NO: $('#TxtTruckNo').val(),
            TRANSPORT_CODE: $('#ddlTransport').val(),
            TRANSPORT_NAME: $('#ddlTransport option:selected').text(),
            DRIVER_NAME: $('#TxtDriverName').val(),
            DRIVER_NO: $('#TxtDriverNo').val(),
            TPT_MODE: $('#ddlModeOfTransport').val(),
            TPT_DISTANCE: $('#NumDist').val(),
            WAYBILL_NO: $('#TxtWayBillNo').val(),
            FRT_TOPAY: $('#NumToPayFrt').val(),
            REMARK: $('#TxtRemarks').val(),
            WB_QTY: '',

            DISC_PER: $('#NumOtherDiscount1').val(),
            DISC_AMT: $('#NumOtherDiscount2').val(),
            CDISC_PER: $('#NumCashDiscount1').val(),
            CDISC_AMT: $('#NumCashDiscount2').val(),

            AGENT_CODE: $('#ddlSoldBy').val(),
            FORM_CODE: $('#ddlFormType').val(),
            SAUDA_TYPE: null,
            SAUDA_NO: $('#NumSaudaNo').val(),
            SAUDA_RATE: null,

            DEFECTIVE_GOODS: $('#ChkDefectiveGood').is(':checked') ? 1 : 0,
            CAL_ONPCS: $('#cbCalOnPcs').is(':checked') ? '1' : '0',
            PRINT_DETAIL: $('#ChkDetail').is(':checked') ? '1' : '0',

            EXRATE: $('#NumExRate').val(),
            BUYER_ORDNO: $('#TxtBuyerOrder').val(),
            PAYMENT_TERM: $('#TxtPaymentTerm').val(),
            PLACE_RECEIPT: $('#TxtPlaceOfReceipt').val(),
            PORT_LOADING: $('#TxtPortOfLoading').val(),
            PORT_DISCHARGE: $('#TxtPortOfDischarge').val(),
            FINAL_DEST: $('#TxtFinalDestination').val(),
            FINAL_DEST_COUNTRY: $('#TxtFDCountry').val(),
            DELIVERY_TERMS: $('#TxtDeliveryTerms').val(),
            SB_NO: $('#TxtShippingBillNo').val(),
            SB_DATE: $('#DtShippingBillDate').val(),
            PORT_CODE: $('#TxtPortCode').val(),
            LUT_DETAIL: $('#TxtLUTDetail').val(),
            INSU_DETAIL: $('#TxtInsuranceDetail').val(),

            SUPPLY_TYPE: $('#ddlSupplyType').val(),
            LC_NO: $('#TxtLCNo').val(),
            TRAN_TYPE: $('#ddlTransactionType').val(),
            STATUS: $('#ddlDocStatus').val(),

            Footer: []
        };

        // =========================
        // FOOTER DATA
        // =========================

        $('#tblDeliveryorder tbody tr').each(function () {
            const row = $(this);
            const itemCode = row.find('.productCode').val();

            if (!itemCode) return;

            header.Footer.push({
                ITEM_CODE: itemCode,
                ITEM_NAME: row.find('.productName option:selected').text(),
                UNIT_NAME: row.find('.unitName').val() || '',
                UNIT_CODE: row.find('.unitCode').val() || '',
                HSN_CODE: row.find('.hsn').val() || '',

                NOS: row.find('.nos').val(),
                GROSS_QTY: row.find('.grossQuantity').val(),
                QTY: row.find('.netQuantity').val(),

                FOR_RATE: row.find('.rateUsd').val(),
                RATE: row.find('.rateRs').val(),
                AMOUNT: row.find('.amount').val(),

                PACK_PER: row.find('.packPer').val(),
                PACK_AMT: row.find('.packAmount').val(),
                DISC_PER: row.find('.discPer').val(),
                DISC_AMT: row.find('.discAmount').val(),

                REMARK: row.find('.remark').val(),
                PACK_TYPE: row.find('.packType').val() || '',
                PACK_NO: row.find('.packNo').val(),
                LOT_No: row.find('.lotNo').val(),

                SAUDA_TYPE: row.find('.saudaType').val(),
                SAUDA_NO: row.find('.saudaNo').val(),
                SAUDA_RATE: row.find('.saudaRate').val(),

                ORD_TYPE: row.find('.orderType').val(),
                ORD_NO: row.find('.orderNo').val(),
                ORD_RATE: row.find('.orderRate').val(),
                DCN_TYPE: row.find('.dcnType').val(),
                DCN_NO: row.find('.dcnNo').val(),

                ROW_ID: row.find('.rowid').val(),
                TAX_CODE: row.find('.taxCode').val(),

                FRT_AMT: row.find('.freightAmount').val(),
                INSU_AMT: row.find('.insuranceAmount').val(),
                CDISC_AMT: row.find('.cashDiscountAmount').val(),
                WB_QTY: row.find('.wbQty').val()
            });
        });

        // =========================
        // UPDATE API
        // =========================

        const response = await fetch('/DeliveryOrder/UpdateDO', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(header)
        });

        const result = await response.json();

        if (result.success) {
            showToast(result.message || 'Delivery Order updated successfully', {type: 'success'});
        } else {
            showToast(result.message || 'Failed to update Delivery Order', { type: 'error' });
        }

        console.log('Update DO Response:', result);

    } catch (error) {
        console.error('Update DO Error:', error);
        showToast('Failed to update Delivery Order: ' + error.message, { type: 'error'});
    }
}

// =========================================
// Header Amount Distribution Events
// =========================================
$(document).on('blur', '#NumOtherPacking2', DistributePackingAmount);
$(document).on('blur', '#NumOtherDiscount2', DistributeDiscountAmount);
$(document).on('blur', '#NumCashDiscount2', DistributeCashDiscountAmount);
