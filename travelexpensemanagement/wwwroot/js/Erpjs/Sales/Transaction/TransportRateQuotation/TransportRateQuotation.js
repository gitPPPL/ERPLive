async function LoadDropDown() {
    try {
        await Promise.all([
            DDlDo(),
            DDlCityName(),
            DDlTransPortName(),
            DDlBillto()
        ]);
    } catch (error) {
        console.error("Error loading dropdowns:", error);
    }
}

async function fetchDataByDo(Dono, Vno) {
    try {
        const response = await $.ajax({
            url: '/TransportRateQuotation/GetDatabyDo',
            type: 'GET',
            data: { Dono: Dono, Vno: Vno }
        });

        if (response.success === false) {
            toastr.info(response.message);
            $('#Vtpye').val('');
            $('#TxtAddressL1').val('');
            $('#TxtAddressL2').val('');
            $('#TxtAddressL3').val('');
            $('#DDlCityname').val('');
            return;
        } else {
            $('#Vtpye').val(response.data.v_TYPE || '');
            $('#TxtAddressL1').val(response.data.shiP_ADD1 || '');
            $('#TxtAddressL2').val(response.data.shiP_ADD2 || '');
            $('#TxtAddressL3').val(response.data.shiP_ADD3 || '');
            $('#DDlCityname').val(response.data.code || '');
        }
    } catch (error) {
        console.error("Error fetching data: ", error);
        alert("An error occurred while fetching data.");
    }
}

async function GetTransportData() {
    try {
        const res = await fetch('/TransportRateQuotation/GetFillData');
        if (!res.ok) throw new Error(`HTTP error! status: ${res.status}`);

        const data = await res.json();
        $('#tblTransportRateQuotation tbody').empty();
        data.forEach(transport => {
            addRow({
                code: transport.code,
                transportName: transport.code,
                rate: '',
                finalRate: '',
                truckNo: '',
                grNo: '',
                grDate: ''
            });
        });
    } catch (error) {
        console.error('Error fetching transport data:', error);
    }
}

function addRow(data = {}) {
    $tbody.find('.btn-add-action').remove();

    const transportOptions = transportData.map(transport => {
        return `<option value="${transport.value}" ${data.transportName === transport.value ? 'selected' : ''}>${transport.text}</option>`;
    }).join('');

    const row = `
            <tr class="no-border-input">
                <!-- Transport Code (hidden, will show selected transport code) -->
                <td><input type="text" class="form-control transport-code" value="${data.code || ''}" readonly /></td>

                <!-- Transport Name (Dropdown) -->
                <td>
                    <select class="form-control transport-dropdown">
                        <option value="">Select Transport</option>
                        ${transportOptions}
                    </select>
                </td>
                 <td><input type="number" class="form-control rate" value="${data.rate || ''}" /></td>
                 <td><input type="number" class="form-control finalRate" value="${data.finalRate || ''}" /></td>
                 <td><input type="text" class="form-control truckNo" value="${data.truckNo || ''}" /></td>
                 <td><input type="text" class="form-control grNo" value="${data.grNo || ''}" /></td>
                 <td><input type="date" class="form-control grDate" value="${data.grDate || ''}" /></td>
                <td>
                    <i class="fa fa-plus btn-add-action text-success" title="Add Row" style="cursor:pointer;"></i>
                    <i class="fa fa-trash btn-delete-action text-danger" title="Delete Row" style="cursor:pointer;"></i>
                </td>
            </tr>
        `;

    $tbody.append(row);

    $tbody.find('.transport-dropdown').last().on('change', function () {
        const selectedTransport = $(this).val();
        const $codeField = $(this).closest('tr').find('.transport-code');
        $codeField.val(selectedTransport);
    });

    if (data.transportName) {
        const $selectedOption = $tbody.find('.transport-dropdown').last().find(`option[value="${data.transportName}"]`);
        if ($selectedOption.length) {
            $selectedOption.prop('selected', true);
            const selectedTransportCode = $selectedOption.val();
            $tbody.find('.transport-code').last().val(selectedTransportCode);
        }
    }
}

function formatDate(dateStr) {
    if (!dateStr) return null;
    const date = new Date(dateStr);
    if (isNaN(date)) return null;
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
}

function collectTableData() {
    const tableData = [];
    $tbody.find('tr').each(function () {
        const rowData = {
            TRANSPORT_CODE: $(this).find('.transport-code').val(),
            TRANSPORT_NAME: $(this).find('.transport-dropdown option:selected').text(),

            RATE: $(this).find('.rate').val(),
            OUR_RATE: $(this).find('.finalRate').val(),
            TRUCK_NO: $(this).find('.truckNo').val(),
            GRNO: $(this).find('.grNo').val(),
            GRDATE: $(this).find('.grDate').val()
        };
        tableData.push(rowData);
    });
    const collectedData = tableData;
    return collectedData;
}


async function LoadFormByID(code) {
    try {
        const res = await $.ajax({
            url: '/TransportRateQuotationList/GetDataByCode',
            method: 'GET',
            data: { code: code }
        });

        if (!res.success) {
            toastr.error(res.message || "Failed to load data.");
            return;
        }
        const header = res.data.header?.[0];
        const details = res.data.details || [];

        if (!header) {
            toastr.warning("No header data found.");
            return;
        }

        $('#TxtCode').val(header.DOC_ID ?? '');
        $('#NumDocumentNo').val(header.V_NO ?? '');
        $('#DtDocumentDate').val(formatDate(header.V_DATE));
        $('#ddlDONo').val(header.DO_NO ?? '').trigger('change');
        $('#TxtBillTo').val(header.BILL_CODE ?? '').trigger('change');
        $('#TxtAddressL1').val(header.SHIP_ADD1 ?? '');
        $('#TxtAddressL2').val(header.SHIP_ADD2 ?? '');
        $('#TxtAddressL3').val(header.SHIP_ADD3 ?? '');
        $('#DDlCityname').val(header.SHIP_CITY ?? '');
        $('#TxtRemarks').val(header.REMARKS ?? '');
        $('#NumQuantity').val(header.QTY ?? '');
        $('#Vtpye').val(header.DO_TYPE ?? '');

        const $tbody = $('#tblTransportRateQuotation tbody');
        $tbody.empty();
        if (details && details.length > 0) {
            details.forEach(d => {
                addRow({
                    code: d.TRANSPORT_CODE ?? '',
                    transportName: d.TRANSPORT_CODE ?? '',
                    rate: d.RATE ?? '',
                    finalRate: d.OUR_RATE ?? '',
                    truckNo: d.TRUCK_NO ?? '',
                    grNo: d.GRNO ?? '',
                    grDate: d.GRDATE ? formatDate(d.GRDATE) : ''
                });
            });
        } else {
            addRow();
        }


    } catch (err) {
        console.error("Failed to load data", err);
        toastr.error("Something went wrong while loading the form.");
    }
}

function readOnlyForm() {
    const form = document.getElementById('TransportRateQuotationForm');

    if (!form) return;

    form.querySelectorAll('input, textarea, select, button').forEach(element => {
        element.disabled = true;
    });

    // Disable any editable table elements
    form.querySelectorAll('[contenteditable="true"]').forEach(element => {
        element.setAttribute('contenteditable', 'false');
    });
}

async function SendMessage(mobileno, smsMatter, smstemplate) {
    try {
        console.log("mobileno", mobileno);
        console.log("smsMatter", smsMatter);
        console.log("smstemplate", smstemplate);

        const response = await $.ajax({
            url: '/TransportRateQuotation/SMSSend',
            type: 'POST',  // Change GET to POST to match the server method
            data: {
                mobileno: mobileno,
                smsMatter: smsMatter,
                smstemplate: smstemplate
            },
            success: function (response) {
                console.log("SMS Response:", response);
                if (response.success) {
                    alert(response.message);  // Show success message
                } else {
                    alert("Error: " + response.message);  // Show error message
                }
            },
            error: function (error) {
                console.error("Error fetching data: ", error);
                alert("An error occurred while fetching data.");
            }
        });

    } catch (error) {
        console.error("Error while sending message: ", error);
        alert("An error occurred while sending the message.");
    }
}