
async function LoadDropdown() {
    try {
        await Promise.all([    
            ddlPartyName(),
            cmbProductName(),
            cmbSALESThough()
        ]);
    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function ddlPartyName() {
    try {
        const res = await fetch('/SalesTarget/cmbPartyName');
        const data = await res.json();
        console.log("ddlPartyName", data);
        const ddl = $('#ddlPartyName');
        ddl.empty();
        ddl.append('<option value="">---Select Party Name---</option>');

        data.forEach(item => {
            const option = `<option value="${item.value}">${item.text}</option>`;
            ddl.append(option);
        });

        ddl.select2({
            placeholder: "---Select Party Name---",
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        console.error("Error loading Party Name:", error);
    }
}

async function cmbProductName() {
    try {
        const res = await fetch('/SalesTarget/cmbProductName');
        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status} `);
        }
        const data = await res.json();
        if (!Array.isArray(data)) {
            throw new Error("cmbProductName response is not an array");
        }
        ProductList = data.map(x =>  `<option value="${x.value}">${x.text}</option>` ).join('');

        return ProductList;

    } catch (error) {
        console.error("Error loading ItemName:", error);
        throw error;
    }
}

async function GetVNo(Vtype, tableName) {
    const res = await fetch(`/SalesTarget/GetVNo?Vtype=${encodeURIComponent(Vtype)}&tableName=${encodeURIComponent(tableName)}`);
    const data = await res.json();
    if (data.v_NO)
    {
        $('#NumDocumentNo').val(data.v_NO);
    }
}
function DeleteRow(button)
{
    let row = $(button).closest('tr');
    if (row.length === 0) {
        return;
    }
    row.remove();
}
function SetFormReadOnly(isReadOnly = true)
{
    $('#SalesTargetForm') .find('input, textarea') .prop('readonly', isReadOnly);
    $('#SalesTargetForm') .find('select, input[type="checkbox"], input[type="radio"]') .prop('disabled', isReadOnly);
    $('#SalesTargetForm') .find('button') .not('#btn-save') .prop('disabled', isReadOnly);
    $('#btn-save').prop('disabled', isReadOnly);
}

async function cmbSALESThough() {
    try {
        const res = await fetch('/SalesTarget/cmbSALESThough');

        if (!res.ok) {
            throw new Error(`HTTP error! Status: ${res.status} `);
        }
        const data = await res.json();
        if (!Array.isArray(data)) {
            throw new Error("cmbSALESThough response is not an array");
        }
        salesThroughList = data.map(x =>
            `<option value="${x.value}">${x.text}</option>`
        ).join('');


        return salesThroughList;

    } catch (error) {
        console.error("Error loading sales Through:", error);
        throw error;
    }
}
