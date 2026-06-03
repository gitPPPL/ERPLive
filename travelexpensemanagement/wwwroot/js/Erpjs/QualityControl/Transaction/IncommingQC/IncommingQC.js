    $(document).ready(function () {
        function getQueryParam(param) {
            const urlParams = new URLSearchParams(window.location.search);
            return urlParams.get(param);
        }
    const vType = getQueryParam('vType');
    const vNo = getQueryParam('vNo');
    const mode = getQueryParam('mode');
    const isReadOnly = (mode === 'view');
    window.isReadOnly = isReadOnly;

    if (vNo) {
        $.ajax({
            url: '/IncommingQC/GetAllDatadetails',
            type: 'POST',
            contentType: 'application/json',
            dataType: 'json',
            data: JSON.stringify({ vNo: vNo, vType: vType }),
            success: function (response) {
                GetallDataIncommingQC(response);
                if (mode === "view") {
                    setFormReadOnly(0);
                }
            },
            error: function (xhr) {
                toastr.error('Error: ' + xhr.responseText);
            }
        });
    }
    const $tbody = $('#tblIncommingQC tbody');
    function addRow(data = { }) {
        // remove previous "+" buttons
        $tbody.find('.btn-add-action').remove();

    const row = `
    <tr>
        <td style="display:none;">${data.code || ''}</td>
        <td><select class="form-control ddlItemName"></select></td>
        <td><select class="form-control ddlParticulars"></select></td>
        <td><select class="form-control ddlUnits"></select></td>
        <td><input type="number" class="form-control" value="${data.level || ''}" /></td>
        <td><input type="text" class="form-control" value="${data.result || ''}" /></td>
        <td><input type="text" class="form-control" value="${data.remarks || ''}" /></td>
        <td><input type="number" class="form-control" value="${data.deductionAmount || ''}" /></td>
        <td><input type="number" class="form-control" value="${data.allowAmount || ''}" /></td>
        <td><input type="text" class="form-control" value="${data.deductionNarration || ''}" /></td>
        <td>
            <i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>
            <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row" style="cursor:pointer;"></i>
        </td>
    </tr>
    `;

    $tbody.append(row);
    const $lastRow = $tbody.find('tr:last');
    bindDropdown('IncommingQC','ItemMaster', $lastRow.find('.ddlItemName'), '-- Select Item --', data.itemName);
    bindDropdown('IncommingQC','Particulars', $lastRow.find('.ddlParticulars'),'-- Select Particular --', data.particulars);
    bindDropdown('IncommingQC', 'Units', $lastRow.find('.ddlUnits'),'-- Select Unit --',data.unit);
    }

    addRow();
    // $(document).on('click', '.btn-add-row', function () {
        //     addRow();
        // });
        $(document).on('click', '.btn-delete-action', function () {
            const $row = $(this).closest('tr');
            const isLastRow = $row.is(':last-child');
            $row.remove();

            if (isLastRow) {
                const $lastRow = $tbody.find('tr:last');
                if ($lastRow.length && !$lastRow.find('.btn-add-action').length) {
                    $lastRow.find('td:last').prepend(
                        `<i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>`
                    );
                }
            }
        });
    $('#btnAddRow').on('click', function () {
        addRow();
    });
    ddlDocType();
    bindDropdown('IncommingQC', 'QCIncharg', '#ddlQCIncharge', '-- Select QC Incharge --');
    bindDropdown('IncommingQC', 'Chem', '#ddlChem', '-- Select Chemist --');
    bindDropdown('IncommingQC', 'PartyName', '#ddlPartyName', '-- Select Party Name --');

    const now = new Date();
    const formattedDate = now.toISOString().split('T')[0];
    $("#DtDocDate").val(formattedDate);

    $('#ddlDocType').on('change', function () {
            const selectedValue = $(this).val();
    const selectedText = $(this).find("option:selected").text();
    $('#NumDocNo').prop('readonly', true);

    if (selectedValue !== "") {
        sendDocType(selectedValue, selectedText);
            } else {
        $('#NumDocNo').val('');
            }
    });
    // $('#ddlMRNNo').on('change', function () {
        //         var selectedNo = $(this).val();
        //         const selectedText = $(this).find('option:selected').text();

        //         if (selectedNo !== "0" && selectedNo !== "") {
        //             $.ajax({
        //                 url: '/IncommingQC/GetAllDatadetails',
        //                 type: 'POST',
        //                 data: {
        //                     StrVNo: selectedNo,
        //                     StrV_type: selectedText
        //                 },
        //                 success: function (response) {
        //                     GetalldatafetchGatonchange(response);
        //                 },
        //                 error: function (xhr) {
        //                     console.error("Error:", xhr.responseText);
        //                 }
        //             });
        //         }
        // });
        $('#Btn-fill').on('click', function () {
            $("#field_hide").hide();
            var DocType = $('#ddlDocType').val();
            var mrnText = $('#ddlMRNNo option:selected').text();
            var VNo = $('#ddlMRNNo').val();
            $.ajax({
                url: '/IncommingQC/SendDropdownData',
                type: 'POST',
                data: {
                    DocType: DocType,
                    MRNText: mrnText,
                    VNo: VNo
                },
                success: function (response) {
                    if (response.success === false) {
                        toastr.warning(response.message);
                    }
                    console.log('response Btn-fill', response);
                    if (response.headerData && response.headerData.length > 0) {
                        const header = response.headerData[0];
                        // Fill header fields
                        $('#ddlPartyName').append(`<option value="${header.partY_CODE}" selected>${header.name}</option>`);
                        $('#TxtTransport').val(safeValue(header.transporT_NAME));
                        $('#TxtInvoiceQty').val(safeValue(header.bilL_QTY));
                        $('#TxtRecordedQty').val(safeValue(header.recD_QTY));
                        $('#TxtPurchaseType').val(safeValue(header.documenT_NAME));
                        $('#DtMRNDate').val(convertToISODate(header.v_DATE));
                        $('#TxtBillNo').val(safeValue(header.bilL_NO));
                        $('#DtBillDate').val(convertToISODate(header.bilL_DATE));
                        $('#TxtTruckNo').val(safeValue(header.trucK_NO));
                    }
                    // Clear previous table rows
                    $tbody.empty();
                    // ================= ITEM DATA =================
                    if (response.itemData && response.itemData.length > 0) {
                        response.itemData.forEach(item => {
                            addRow({
                                code: item.iteM_CODE,
                                itemName: item.iteM_NAME,
                                particulars: item.particulaR_NAME,
                                unit: item.uniT_NAME || '',
                                level: item.stD_LEVEL,
                                deductionAmount: item.deduction_amt,
                                allowAmount: item.allow_amt,
                                deductionNarration: item.deduction_narration
                            });
                        });
                        setFormReadOnly(1);
                    }
                    else {
                        addRow();
                        // setFormReadOnly(2);
                    }
                },
                error: function (xhr, status, error) {
                    console.error('Error:', error);
                    toastr.error('Error sending data.');
                }
            });
            function safeValue(value) {
                if (value === null || value === undefined) return '';
                if (typeof value === 'object') return '';
                return value;
            }
            function convertToISODate(dateStr) {
                if (!dateStr) return '';
                const parts = dateStr.split('/');
                if (parts.length === 3) {
                    return `${parts[2]}-${parts[1]}-${parts[0]}`;
                }
                return '';
            }
        });
    function addRow(data = { }) {
        $tbody.find('.btn-add-action').remove();
    const row = `
    <tr>
        <td style="display:none;">${data.code || ''}</td>
        <td><select class="form-control ddlItemName"></select></td>
        <td><select class="form-control ddlParticulars"></select></td>
        <td><select class="form-control ddlUnits"></select></td>
        <td><input type="number" class="form-control" value="${data.level || '0.0000'}" /></td>
        <td><input type="text" class="form-control" value="${data.result || ''}" /></td>
        <td><input type="text" class="form-control" value="${data.remarks || ''}" /></td>
        <td><input type="number" class="form-control" value="${data.deductionAmount || '0.00'}" /></td>
        <td><input type="number" class="form-control" value="${data.allowAmount || '0.00'}" /></td>
        <td><input type="text" class="form-control" value="${data.deductionNarration || ''}" /></td>
    </tr>
    `;
         // <td>
         //     <i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>
         //     <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row" style="cursor:pointer;"></i>
         // </td>
    $tbody.append(row);
    const $lastRow = $tbody.find('tr:last');
    bindDropdown('IncommingQC','ItemMaster', $lastRow.find('.ddlItemName'),'-- Select Item --', data.itemName );
    bindDropdown('IncommingQC', 'Particulars', $lastRow.find('.ddlParticulars'), '-- Select Particular --', data.particulars);
    bindDropdown('IncommingQC','Units', $lastRow.find('.ddlUnits'), '-- Select Unit --', data.unit);
    }
    $('#btn-save').on('click', function (e) {
            e.preventDefault();

            // 1️⃣ Collect table items
            let tableData = [];
            $('#tblIncommingQC tbody tr').each(function () {
                const $row = $(this);

                tableData.push({
                    iteM_CODE: parseInt($row.find('.ddlItemName').val()) || 0,
                    particulaR_NAME: $row.find('.ddlParticulars').val(),
                    uniT_NAME: $row.find('.ddlUnits').val() || "",
                    stD_LEVEL: parseFloat($row.find('input[type="number"]').eq(0).val()) || 0,
                    result: $row.find('input[type="text"]').eq(0).val() || "",
                    remarks: $row.find('input[type="text"]').eq(1).val() || "",
                    deduction_amt: parseFloat($row.find('input[type="number"]').eq(1).val()) || 0,
                    allow_amt: parseFloat($row.find('input[type="number"]').eq(2).val()) || 0,
                    deduction_narration: $row.find('input[type="text"]').eq(2).val() || "",
                    qcP_CODE: parseInt($row.find('.ddlQCPCode').val()) || 0,
                    nos: parseInt($row.find('.txtNos').val()) || 0
                });
            });

            console.log('item', tableData)
            // 2️⃣ Collect header
            const headerData = {
                DocType: $('#ddlDocType').val(),
                MRNNo: $('#ddlMRNNo option:selected').text(),
                V_TYPE: $('#ddlVType').val(),
                v_NO: $('#ddlMRNNo').val(),
                DocNo: $('#NumDocNo').val(),
                DocDate: new Date($('#DtDocDate').val()).toISOString(),
                QCIncharge: $('#ddlQCIncharge').val(),
                Chemist: $('#ddlChem').val(),
                PartyCode: parseInt($('#ddlPartyName').val()) || 0,
                Transport: $('#TxtTransport').val(),
                InvoiceQty: parseFloat($('#TxtInvoiceQty').val()) || 0,
                RecordedQty: parseFloat($('#TxtRecordedQty').val()) || 0,
                PurchaseType: $('#TxtPurchaseType').val(),
                Wastage: parseFloat($('#TxtWastage').val()) || 0,
                MRNDate: new Date($('#DtMRNDate').val()).toISOString(),
                Bales: parseFloat($('#TxtBales').val()) || 0,
                BillNo: $('#TxtBillNo').val(),
                BillDate: new Date($('#DtBillDate').val()).toISOString(),
                TruckNo: $('#TxtTruckNo').val(),
                Shortage: parseFloat($('#TxtShortage').val()) || 0,
                DeductionAmount: parseFloat($('#TxtDeductionAmount').val()) || 0,
                DeductionNarration: $('#TxtDeductionNarration').val(),
                Remarks: $('#TxtRemarks').val(),
                ACTION: vNo > 0 ? "UPDATE" : "INSERT",
            };
            console.log('headerData', headerData)

            // 3️⃣ Send via AJAX
            $.ajax({
                url: '/IncommingQC/SaveQCData',
                type: 'POST',
                contentType: 'application/json',
                data: JSON.stringify({ header: headerData, items: tableData }),
                success: function (response) {
                    if (response.success) {
                        toastr.success('Data saved successfully!');
                    } else {
                        toastr.error(response.message || 'Something went wrong.');
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error('Error saving data.');
                    console.error(error);
                }
            });
        });

    $("#btn-pending").click(function () {
            $("#pendingQCModal").modal("show");
        });

    function GetallDataIncommingQC(response) {
        console.log('Full Response => ', response);
    if (!response || !response.header || response.header.length === 0) {
        toastr.warning('No record found.');
    return;
        }
    const header = response.header[0];
    const items = response.items || [];
    $('#ddlDocType').val(header.DocType);
    $('#NumDocNo').val(header.DocNo);
    $('#DtDocDate').val(header.DocDate ? header.DocDate.split('T')[0] : '' );
    $('#TxtTransport').val(header.TRANSPORT);
    $('#TxtInvoiceQty').val(header.INV_QTY);
    $('#TxtRecordedQty').val(header.RECD_QTY);
    $('#TxtPurchaseType').val(header.PUR_TYPE);
    $('#TxtWastage').val(header.WASTE_WGT);
    $('#DtMRNDate').val(header.MRN_DATE ? header.MRN_DATE.split('T')[0] : '' );
    $('#TxtBales').val(header.BALES);
    $('#TxtBillNo').val(header.BILL_NO);
    $('#DtBillDate').val(header.BILL_DATE ? header.BILL_DATE.split('T')[0] : '' );


    $('#TxtTruckNo').val(header.TRUCK_NO);
    $('#TxtShortage').val(header.SHORT_QTY);
    $('#TxtDeductionAmount').val(header.DEDUCT_AMT);
    $('#TxtDeductionNarration').val(header.DEDUCT_NARR);
    $('#TxtRemarks').val(header.REMARKS);
    ddlMRNNo(header.DocNo, header.DocType, header.DocNo);


    bindDropdown('IncommingQC', 'QCIncharg', '#ddlQCIncharge', '-- Select QC Incharge --', header.QC_INCHARGE);
    bindDropdown('IncommingQC', 'Chem', '#ddlChem', '-- Select Chemist --', header.CHEMIST);
    bindDropdown('IncommingQC', 'PartyName', '#ddlPartyName', '-- Select Party Name --', header.PARTY_CODE);
    const tbody = $('#tblIncommingQC tbody');
    tbody.empty();
    if (items.length === 0) {
        tbody.append(`<tr><td colspan="11" class="text-center"> No items found </td> </tr>`);
        } else {
        $.each(items, function (index, item) {
            const row = `
                    <tr>
                        <td style="display:none;"> ${item.ITEM_CODE || ''} </td>
                        <td><select class="form-control ddlItemName"></select></td>
                        <td><select class="form-control ddlParticulars"></select></td>
                        <td><select class="form-control ddlUnits"></select></td>
                        <td><input type="number" class="form-control txtLevel" value="${item.ACCEPTANCE || 0}" /></td>
                        <td><input type="text" class="form-control txtResult" value="${item.RESULT || ''}" /> </td>
                        <td><input type="text" class="form-control txtRemark" value="${item.REMARK || ''}" /> </td>
                        <td><input type="number" class="form-control txtDeduction" value="${item.DEDU_AMT || 0}" /></td>
                        <td><input type="number" class="form-control txtAllow" value="${item.ALLOW_AMT || 0}" /></td>
                        <td><input type="text" class="form-control txtDeductionNarr" value="${item.DEDU_NARR || ''}" /></td>
                    </tr>
                `;
            tbody.append(row);
            const $lastRow = tbody.find('tr:last');
            bindDropdown('IncommingQC', 'ItemMaster', $lastRow.find('.ddlItemName'), '-- Select Item --', item.ITEM_CODE);
            bindDropdown('IncommingQC', 'Particulars', $lastRow.find('.ddlParticulars'), '-- Select Particular --', item.QCP_CODE);
            bindDropdown('IncommingQC', 'Units', $lastRow.find('.ddlUnits'), '-- Select Unit --', item.UNIT);
        });
        }
    }
    // function GetallDataIncommingQC(response) {
        //     console.log('response', response);
        //     if (!response || !response.header || response.header.length === 0) {
        //         toastr.warning('No record found.');
        //         return;
        //     }
        //     const header = response.header[0];
        //     const items = response.items || [];
        //     $('#ddlDocType').val(header.DocType);
        //     $('#NumDocNo').val(header.DocNo);
        //     $('#DtDocDate').val(header.DocDate ? header.DocDate.split('T')[0] : '');
        //     // $('#ddlQCIncharge').val(header.QC_INCHARGE);

        //     // $('#ddlChem').val(header.CHEMIST);
        //     // $('#ddlPartyName').val(header.PARTY_CODE);
        //     $('#TxtTransport').val(header.TRANSPORT);
        //     $('#TxtInvoiceQty').val(header.INV_QTY);
        //     $('#TxtRecordedQty').val(header.RECD_QTY);
        //     $('#TxtPurchaseType').val(header.PUR_TYPE);
        //     $('#TxtWastage').val(header.WASTE_WGT);
        //     $('#DtMRNDate').val(header.MRN_DATE ? header.MRN_DATE.split('T')[0] : '');
        //     $('#TxtBales').val(header.BALES);
        //     $('#TxtBillNo').val(header.BILL_NO);
        //     $('#DtBillDate').val(header.BILL_DATE ? header.BILL_DATE.split('T')[0] : '');
        //     $('#TxtTruckNo').val(header.TRUCK_NO);
        //     $('#TxtShortage').val(header.SHORT_QTY);
        //     $('#TxtDeductionAmount').val(header.DEDUCT_AMT);
        //     $('#TxtDeductionNarration').val(header.DEDUCT_NARR);
        //     $('#TxtRemarks').val(header.REMARKS);
        //     ddlMRNNo(header.DocNo, header.DocType, header.DocNo);

        //   bindDropdown(
        //     'IncommingQC',
        //     'QCIncharg',
        //     '#ddlQCIncharge',
        //     '-- Select QC Incharge --',
        //     header.QC_INCHARGE
        // );

        // bindDropdown(
        //     'IncommingQC',
        //     'Chem',
        //     '#ddlChem',
        //     '-- Select Chemist --',
        //     header.CHEMIST
        // );

        // bindDropdown(
        //     'IncommingQC',
        //     'PartyName',
        //     '#ddlPartyName',
        //     '-- Select Party Name --',
        //     header.PARTY_CODE
        // );

        //     const tbody = $('#tblIncommingQC tbody');
        //     tbody.empty();

        // if (items.length === 0) {
        //     tbody.append(`<tr><td colspan="11" class="text-center">No items found</td></tr>`);
        // } else {
        //     $.each(items, function (index, item) {
        //         const row = `
        //             <tr>
        //                 <td style="display:none;">${item.ITEM_CODE || ''}</td>
        //                 <td>
        //                     <select class="form-control ddlItemName"></select>
        //                 </td>
        //                 <td>
        //                     <select class="form-control ddlParticulars"></select>
        //                 </td>
        //                 <td>
        //                     <select class="form-control ddlUnits"></select>
        //                 </td>
        //                 <td>
        //                     <input type="number" class="form-control txtLevel" value="${item.ACCEPTANCE || 0}" />
        //                 </td>
        //                 <td>
        //                     <input type="text" class="form-control txtResult" value="${item.RESULT || ''}" />
        //                 </td>
        //                 <td>
        //                     <input type="text" class="form-control txtRemark" value="${item.REMARK || ''}" />
        //                 </td>
        //                 <td>
        //                     <input type="number" class="form-control txtDeduction" value="${item.DEDU_AMT || 0}" />
        //                 </td>
        //                 <td>
        //                     <input type="number" class="form-control txtAllow" value="${item.ALLOW_AMT || 0}" />
        //                 </td>
        //                 <td>
        //                     <input type="text" class="form-control txtDeductionNarr" value="${item.DEDU_NARR || ''}" />
        //                 </td>
        //             </tr>
        //         `;
        //         // <td>
        //         //    <i class="fa fa-plus text-success btn-add-row" title="Add Row" style="cursor:pointer;"></i>
        //         //    <i class="fa fa-trash text-danger btn-delete-row" title="Delete Row" style="cursor:pointer;"></i>
        //         // </td>
        //         tbody.append(row);
        //         const $lastRow = tbody.find('tr:last');
        //         // ddlItemMaster($lastRow.find('.ddlItemName'), item.ITEM_CODE);
        //         // Particulars($lastRow.find('.ddlParticulars'), item.QCP_CODE);
        //         // Units($lastRow.find('.ddlUnits'), item.UNIT);
        //         bindDropdown('IncommingQC','ItemMaster', $lastRow.find('.ddlItemName'),'-- Select Item --',item.ITEM_CODE);
        //         bindDropdown('IncommingQC','Particulars',$lastRow.find('.ddlParticulars'),'-- Select Particular --',item.QCP_CODE);
        //         bindDropdown('IncommingQC','Units', $lastRow.find('.ddlUnits'),'-- Select Unit --', item.UNIT);

        //     });
        // }
        //     // toastr.success('Data loaded successfully!');
        // }
        function setFormReadOnly(Code) {
            switch (Code) {
                case 0:
                    $('#ddlDocType').prop('disabled', true);
                    $('#NumDocNo').prop('readonly', true);
                    $('#DtDocDate').prop('readonly', true);
                    $('#ddlMRNNo').prop('disabled', true);
                    $('#DtMRNDate').prop('readonly', true);
                    $('#ddlPartyName').prop('disabled', true);
                    $('#TxtBillNo').prop('readonly', true);
                    $('#DtBillDate').prop('readonly', true);
                    $('#TxtInvoiceQty').prop('readonly', true);
                    $('#TxtRecordedQty').prop('readonly', true);
                    $('#TxtShortage').prop('readonly', true);
                    $('#TxtTruckNo').prop('readonly', true);
                    $('#TxtTransport').prop('readonly', true);
                    $('#TxtPurchaseType').prop('readonly', true);
                    $('#TxtWastage').prop('readonly', true);
                    $('#TxtBales').prop('readonly', true);
                    $('#TxtDeductionAmount').prop('readonly', true);
                    $('#TxtDeductionNarration').prop('readonly', true);
                    $('#Btn-fill').prop('disabled', true);
                    $('#btn-calculate').prop('disabled', true);
                    $('#btn-pending').prop('disabled', true);
                    $('#chkBillDate').prop('disabled', true);
                    $('#IncommingQCForm input, #IncommingQCForm textarea').css({ 'background-color': '#f5f5f5', 'cursor': 'not-allowed' });
                    $('#IncommingQCForm select').css({ 'background-color': '#f5f5f5', 'cursor': 'not-allowed' });
                    $('.btn-add-action').hide();
                    $('.btn-delete-action').hide();
                    $('#btn-save').prop('disabled', false)
                        .show();
                    break;

                case 1:
                    $('#ddlDocType').prop('disabled', true);
                    $('#NumDocNo').prop('readonly', true);
                    $('#DtDocDate').prop('readonly', true);
                    $('#ddlMRNNo').prop('disabled', true);
                    $('#DtMRNDate').prop('readonly', true);
                    $('#ddlPartyName').prop('disabled', true);
                    $('#TxtBillNo').prop('readonly', true);
                    $('#DtBillDate').prop('readonly', true);
                    $('#TxtInvoiceQty').prop('readonly', true);
                    $('#TxtRecordedQty').prop('readonly', true);
                    $('#TxtShortage').prop('readonly', true);
                    $('#TxtTruckNo').prop('readonly', true);
                    $('#TxtTransport').prop('readonly', true);
                    $('#TxtPurchaseType').prop('readonly', true);
                    $('#TxtWastage').prop('readonly', true);
                    $('#TxtBales').prop('readonly', true);
                    $('#TxtDeductionAmount').prop('readonly', true);
                    $('#TxtDeductionNarration').prop('readonly', true);
                    $('#Btn-fill').prop('disabled', true);
                    $('#btn-calculate').prop('disabled', true);
                    $('#btn-pending').prop('disabled', true);
                    $('#chkBillDate').prop('disabled', true);
                    $('#IncommingQCForm input, #IncommingQCForm textarea').css({ 'background-color': '#f5f5f5', 'cursor': 'not-allowed' });
                    $('#IncommingQCForm select').css({ 'background-color': '#f5f5f5', 'cursor': 'not-allowed' });
                    // QC Incharge Dropdown
                    $('#ddlQCIncharge').prop('disabled', false).css({ 'background-color': '', 'cursor': 'pointer' });
                    $('#ddlChem').prop('disabled', false).css({ 'background-color': '', 'cursor': 'pointer' });

                    // Remarks Editable
                    $('#TxtRemarks').prop('readonly', false).css({ 'background-color': '', 'cursor': 'text' });
                    // Dropdown Disable
                    $('#tblIncommingQC tbody .ddlItemName').prop('disabled', true);
                    $('#tblIncommingQC tbody .ddlParticulars').prop('disabled', true);
                    $('#tblIncommingQC tbody .ddlUnits').prop('disabled', true);
                    $('.btn-add-action').hide();
                    $('.btn-delete-action').hide();
                    $('#btn-save').prop('disabled', false).show();
                    break;

            }
        }

    });
    function ddlDocType(callback) {
        $.ajax({
            url: '/IncommingQC/GetddlDocType',
            type: 'GET',
            dataType: 'json',
            success: function (data) {
                const ddl = $('#ddlDocType');
                ddl.empty().append('<option value="">-- Select Doc Type --</option>');
                $.each(data, function (index, item) {
                    ddl.append(`<option value="${item.value}">${item.text}</option>`);
                });
                if (typeof callback === "function") callback();
            },
            error: function (xhr) {
                console.error("Error loading ddlDocType:", xhr.responseText);
            }
        });
    }
    function sendDocType(docType, docName) {
        $.ajax({
            url: '/IncommingQC/GetDocNo',
            type: 'POST',
            data: {
                docType: docType,
                docName: docName
            },
            success: function (response) {
                if (response.success) {
                    const vNo = response.nextVNo;
                    const vType = response.docType;

                    $('#NumDocNo').val(vNo);
                    ddlMRNNo(vNo, vType);
                }
            },
            error: function (xhr) {
                console.error("Error:", xhr.responseText);
            }
        });
    }
    function ddlMRNNo(vNo, vType1, selectedValue = null) {
        const ddl = $('#ddlMRNNo');
    return $.ajax({
        url: '/IncommingQC/GetddlMRNNo',
    type: 'GET',
    dataType: 'json',
    data: {VNo: vNo, Vtype: vType1 },
    success: function (data) {
        console.log('MRN dropdown data:', data, 'Selected Value:', selectedValue);
    // Destroy old select2 if exists
    if (ddl.hasClass("select2-hidden-accessible")) {
        ddl.select2('destroy');
        }
        // Clear and add default option
        ddl.empty().append('<option value="">-- Select MRN No --</option>');
      // Append new options
      data.forEach(item => {
          const value = item.value || item.Value;
          const text = item.text || item.Text;
          ddl.append(new Option(text, value));
      });

    // ✅ Initialize select2
    ddl.select2({
        placeholder: "-- Select MRN No --",
    allowClear: true,
    width: '100%',
                    minimumResultsForSearch: data.length >= 5 ? 0 : Infinity,
    language: {
        inputTooShort: () => 'Type to search...',
    searchInputPlaceholder: "Search MRN..."
                    }
                });

    // ✅ Set preselected value AFTER select2 initialized
    if (selectedValue) {
        setTimeout(() => {
            ddl.val(selectedValue).trigger('change');
            console.log('MRN dropdown preselected:', selectedValue);
        }, 200);
                }
            },
    error: function (xhr, status, error) {
        console.error("Error loading MRN list:", xhr.responseText);
    toastr.error('Error loading MRN list: ' + error);
            }
        });
    }
