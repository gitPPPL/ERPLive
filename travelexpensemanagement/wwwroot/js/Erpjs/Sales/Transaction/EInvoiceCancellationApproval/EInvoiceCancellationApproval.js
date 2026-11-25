function AddRow(data = {})
{
    console.log("data", data);
    let tbody = $('#tblEinVoicce tbody');
    function formatDate(dateString) {
        if (!dateString) return '';
        return dateString.split('T')[0];
    }

    let newRow = `
        <tr class="no-border-input">
            <td> <input type="checkbox" class="erppagetable-control chk_Select" ${data.IsSelected ? 'checked' : ''} /> </td>
            <td> <input type="text" class="erppagetable-control TxtVType" value="${data.v_Type ?? ''}" readonly />  </td>
            <td> <input type="number" class="erppagetable-control TxtVNo" value="${data.v_No ?? ''}" readonly /> </td>
            <td> <input type="date" class="erppagetable-control TxtVDate" value="${formatDate(data.v_date)}" readonly /> </td>
            <td> <input type="number" class="erppagetable-control TxtNetAmt" value="${data.namount ?? ''}" readonly />  </td>
            <td> <input type="text" class="erppagetable-control TxtBillto"  value="${data.bill_To ?? ''}" readonly /> </td>
            <td> <input type="text" class="erppagetable-control TxtBillGst" value="${data.bill_GST ?? ''}" readonly />  </td>
            <td class="d-none">  <input type="text" class="erppagetable-control TxtfaproV_STATUS" value="${data.faproV_STATUS ?? ''}" readonly /> </td>
            <td>  <input type="text" class="erppagetable-control TxtEInvoice" value="${data.einv ?? ''}" readonly /> </td>
            <td> <input type="date" class="erppagetable-control TxtEInvoiceDate" value="${formatDate(data.einvDate)}" readonly /> </td>
            <td>  <input type="text" class="erppagetable-control TxtEWaybill" value="${data.ewb ?? ''}" readonly />  </td>
            <td> <input type="date" class="erppagetable-control TxtEwbGeneDate" value="${formatDate(data.ewbDate)}" readonly /> </td>
            <td>
                <select class="erppagetable-control TxtStatus">
                    <option value="Open" ${data.cStatus === 'Open' ? 'selected' : ''}>Open</option>
                    <option value="Close" ${data.cStatus === 'Close' ? 'selected' : ''}>Close</option>
                </select>
            </td>
            <td class="d-none">  <input type="text" class="erppagetable-control TxtTransType" value="${data.tranType ?? ''}" readonly />  </td>
            <td class="d-none">  <input type="text" class="erppagetable-control TxtIRN" value="${data.IRN ?? ''}" readonly /> </td>
            <td class="d-none"> <input type="text" class="erppagetable-control TxtBILLCODE" value="${data.BillCode ?? ''}" readonly /> </td>
            <td class="d-none"> <input type="text" class="erppagetable-control TxtBILLPinCODE" value="${data.BillPinCode ?? ''}" readonly /> </td>
            <td class="d-none"> <input type="text" class="erppagetable-control TxtShipCODE" value="${data.ShipCode ?? ''}" readonly /> </td>
            <td class="d-none"> <input type="text" class="erppagetable-control TxtShipName"  value="${data.ShipName ?? ''}" readonly /> </td>
            <td class="d-none"> <input type="text" class="erppagetable-control TxtShipGst" value="${data.ShipGst ?? ''}"  readonly/> </td>
            <td class="d-none"> <input type="text" class="erppagetable-control TxtShipPinCode"  value="${data.ShipPinCode ?? ''}"  readonly/> </td>
            <td> <input type="text" class="erppagetable-control TxtReason" value="${data.REASON ?? ''}" /> </td>
        </tr>
    `;

    tbody.append(newRow);
}

async function GetVNo(Vtype) {
    const res = await fetch(`/EInvoiceCancellationApproval/GetVNo?doctype=${encodeURIComponent(Vtype)}`);
    const data = await res.json();
    if (data.v_NO) {
        $('#NumDocumentNo').val(data.v_NO);
    }
}

async function Getdata() {
    try {
        const V_TYPE = $('#DDlEInvoiceDocument').val();

        const res = await $.ajax({
            type: 'GET',
            url: '/EInvoiceCancellationApproval/GetData',
            data: { V_TYPE: V_TYPE },
            dataType: 'json'
        });

        console.log("Response:", res);

        $('#tblEinVoicce tbody').empty();

        if (!res.success)
        {
            toastr.info(res.message || "Unable to load data.");
            return;
        }

        if (!res.data || res.data.length === 0)
        {
            toastr.info("No records found.");
            return;
        }

        res.data.forEach(function (item) {

            AddRow({
                v_Type: item.v_Type ?? '',
                v_No: item.v_No ?? '',
                v_date: formatDate(item.v_date) ?? '',
                namount: item.namount ?? '',
                bill_To: item.bill_Name ?? '',
                bill_GST: item.bill_GST ?? '',
                faproV_STATUS: item.faproV_STATUS ?? '',
                einv: item.einv ?? '',
                einvDate: formatDate(item.einvDate) ?? '',
                ewb: item.ewb ?? '',
                ewbDate: formatDate(item.ewbDate) ?? '',
                cStatus: item.cStatus ?? '',
                tranType: item.tranType ?? '',
                IRN: item.irn ?? '',
                BillCode: item.bill_Code ?? '',
                BillPinCode: item.bilL_PINCODE ?? '',
                ShipCode: item.shiP_CODE ?? '',
                ShipName: item.shiP_NAME ?? '',
                ShipGst: item.shiP_GST ?? '',
                ShipPinCode: item.shiP_PINCODE ?? '',
                REASON: item.reason ?? ''
            });

        });

    }
    catch (error) {
        console.error("Error Loading Data:", error);
        toastr.error("Error loading invoice data.");
    }
}
function CollectTableData() {

    let Details = [];

    $('#tblEinVoicce tbody tr').each(function () {

        let row = $(this);

        // Collect only checked rows
        if (!row.find('.chk_Select').is(':checked')) {
            return;
        }

        let detail = {
            V_TYPE: $('#DDlDocumentType').val() || '',
            V_NO: parseInt($('#NumDocumentNo').val()) || null,
            V_DATE: $('#DtDocumentDate').val() || null,
            REF_TYPE: row.find('.TxtVType').val() || '',
            REF_NO: parseInt(row.find('.TxtVNo').val()) || null,
            REF_DATE: row.find('.TxtVDate').val() || null,
            INV_STATUS: row.find('.TxtfaproV_STATUS').val() || '',
            EINV_STATUS: row.find('.TxtEInvoice').val() || '',
            EINV_DATE: row.find('.TxtEInvoiceDate').val() || null,
            EWB_STATUS: row.find('.TxtEWaybill').val() || '',
            EWB_DATE: row.find('.TxtEwbGeneDate').val() || null,
            BILL_AMT: parseFloat(row.find('.TxtNetAmt').val()) || 0,
            BILL_CODE: parseInt(row.find('.TxtBILLCODE').val()) || null,
            BILL_NAME: row.find('.TxtBillto').val() || '',
            BILL_GST: row.find('.TxtBillGst').val() || '',
            BILL_PINCODE: row.find('.TxtBILLPinCODE').val() || '',
            SHIP_CODE: parseInt(row.find('.TxtShipCODE').val()) || null,
            SHIP_NAME: row.find('.TxtShipName').val() || '',
            SHIP_GST: row.find('.TxtShipGst').val() || '',
            SHIP_PINCODE: row.find('.TxtShipPinCode').val() || '',
            BILL_TYPE: row.find('.TxtTransType').val() || '',
            IRN: row.find('.TxtIRN').val() || '',
            STATUS: row.find('.TxtStatus').val() || '',
            REASON: row.find('.TxtReason').val() || '',
            REMARKS: '',
            FAPROV_STATUS: row.find('.TxtfaproV_STATUS').val() || '',
            FAPROV_REMARKS: '',
            action: (!rowId || rowId.trim() === "") ? "INSERT" : "UPDATE",
            SNO: Details.length + 1
        };

        Details.push(detail);
    });

    console.log("Selected Rows Data:", Details);

    return Details;
}

async function LoadData()
{
    try
    {
        const res = await $.ajax({
            type: 'POST',
            url: '/EInvoiceCancellationApprovalList/GetDataByCode',
            data: { DOC_ID: rowId },
            dataType: 'json'
        });

        console.log("Response:", res);
        // Clear existing table rows
        $('#tblEinVoicce tbody').empty();

        if (!res.success)
        {
            toastr.error(res.message || "Unable to load data.");
            return;
        }

        if (!res.data || res.data.length === 0)
        {
            toastr.info("No records found.");
            return;
        }

        const HeaderData = res.data[0];

        console.log("HeaderData", HeaderData);

        $('#DDlDocumentType').val(HeaderData.v_TYPE).trigger('change');
        $('#NumDocumentNo').val(HeaderData.v_NO);
        $('#DtDocumentDate').val(HeaderData.v_DATE ? HeaderData.v_DATE.split('T')[0] : '');

        // Pass API data into AddRow()
        res.data.forEach(function (item) {

            AddRow({
                IsSelected: true,
                v_Type: item.v_TYPE ?? '',
                v_No: item.v_NO ?? '',
                v_date: formatDate(item.v_DATE) ?? '',
                namount: item.bilL_AMT ?? '',
                bill_To: item.bilL_NAME ?? '',
                bill_GST: item.bilL_GST ?? '',
                faproV_STATUS: item.faproV_STATUS ?? '',
                einv: item.einV_STATUS ?? '',
                einvDate: formatDate(item.einV_DATE) ?? '',
                ewb: item.ewB_STATUS ?? '',
                ewbDate: formatDate(item.ewB_DATE) ?? '',
                cStatus: item.status ?? '',
                tranType: item.reF_TYPE ?? '',
                IRN: item.irn ?? '',
                BillCode: item.bilL_CODE ?? '',
                BillPinCode: item.bilL_PINCODE ?? '',
                ShipCode: item.shiP_CODE ?? '',
                ShipName: item.shiP_NAME ?? '',
                ShipGst: item.shiP_GST ?? '',
                ShipPinCode: item.shiP_PINCODE ?? '',
                REASON: item.reason ?? ''
            });

        });

       

    }
    catch (error) {
        console.error("Error Loading Data:", error);
        toastr.error("Error loading invoice data.");
    }
}
function ValidateRows() {
    let isValid = true;

    $('#tblEinVoicce tbody tr').each(function () {
        let row = $(this);

        let isChecked = row.find('.chk_Select').prop('checked');
        let reason = (row.find('.TxtReason').val() || '').trim();

        if (isChecked && reason === '') {
            toastr.error('Reason is required for selected rows.');

            row.find('.TxtReason').focus();

            isValid = false;
            return false;
        }
    });

    return isValid;
}
function SetFormReadOnly() {
    const form = $('#TransportRateQuotationForm');

    // Disable complete form
    form.find('input, select, textarea, button')
        .prop('disabled', true);

    form.find('input[type="hidden"]')
        .prop('disabled', false);

    // Disable complete table
    $('#tblEinVoicce')
        .find('*')
        .prop('disabled', true)
        .prop('readonly', true);

    // Prevent all mouse interaction inside table
    $('#tblEinVoicce').css({
        'pointer-events': 'none',
        'user-select': 'none'
    });

    // Disable Select2
    form.find('select.select2-hidden-accessible')
        .prop('disabled', true)
        .trigger('change.select2');
}