
async function GetVNo(Vtype) {

    try {

        const res = await fetch(
            `/InventoryOpeningEntry/GetVNo?Vtype=${encodeURIComponent(Vtype)}`
        );

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        if (data.error) {
            throw new Error(data.error);
        }

        if (!data.v_NO) {
            throw new Error('Response missing V_NO');
        }

        $('#NumDocno').val(data.v_NO);

    } catch (error) {

        console.error("Error loading Document Number:", error);

        showToast(
            "Error loading Document Number",
            { type: "error" }
        );

        throw error;
    }
}

async function LoadDropDown()
{
    try {
        await Promise.all([
            DDLVtype(),
            DDLSTATUS(),
            DDLProdType(),
            DDLDO(),
            DDlFromPlace()         
        ]);

        let vtype = $('#ddlDocType').val();
        await DDlItemName(vtype);

        console.log("All dropdowns loaded successfully");
    } catch (error) {
        console.error("LoadDropDown Error:", error);
        showToast("Error loading dropdowns",{ type: "error" } );

        throw error;
    }
}

async function DDLVtype() 
{
    try {
        const res = await fetch('/InventoryDepartmentIssue/DDlVType');
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlDocType');

        ddl.empty();
            
        data.forEach(item => {
            ddl.append( `<option value="${item.value}">${item.text}</option>` );
        });

    }
    catch (error)
    {
        console.error("Error loading VType:", error);
        throw error;
    }
}

async function DDLSTATUS() {

    try {

        const res = await fetch('/InventoryDepartmentIssue/DDLSTATUS');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlStatus');

        ddl.empty();
            

        data.forEach(item => {

            ddl.append(
                `<option value="${item.value}">${item.text}</option>`
            );

        });

    } catch (error) {

        console.error("Error loading VType:", error);

        throw error;
    }
}

async function DDLProdType() {
    try {

        const res = await fetch('/InventoryDepartmentIssue/DDLProdType');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlProdOrdNo');

        // Destroy existing Select2 if already initialized
        if (ddl.hasClass("select2-hidden-accessible"))
        {
            ddl.select2('destroy');
        }

        ddl.empty();

        data.forEach(item => {
            ddl.append(
                `<option value="${item.text}"> ${item.value}||${item.text} </option>`
            );
        });

        // Enable search
        ddl.select2({
            placeholder: 'Search Product...',
            allowClear: true,
            width: '100%'
        });

    } catch (error) {
        console.error("Error loading VType:", error);
        throw error;
    }
}

async function DDLDO(VType, VNo)
{
    try
    {

        const url = `/InventoryDepartmentIssue/DDLDO?VType=${encodeURIComponent(VType)}&VNo=${encodeURIComponent(VNo)}`;

        const res = await fetch(url);
   
        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }
        const data = await res.json();
        const ddl = $('#ddlDoNo');
        if (ddl.hasClass("select2-hidden-accessible"))
        {
            ddl.select2('destroy');
        }
        ddl.empty();
        data.forEach(item => {
            ddl.append(`<option value="${item.text}"> ${item.value}||${item.text} </option>` );
        });
        ddl.select2({ placeholder: 'Search ...', allowClear: true, width: '100%' });
    } catch (error) {
        console.error("Error loading VType:", error);
        throw error;
    }
}
function DDlItemName(V_TYPE) {

    return $.ajax({
        url: `/InventoryDepartmentIssue/DDlItemName?V_TYPE=${encodeURIComponent(V_TYPE)}`,
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDlItemName:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDlItemName response is not an array");
            }

            // Keep complete data for later use
            ItemDetailsList = data;

            ItemNameList = data
                .map(x =>
                    `<option value="${x.itemCode}">${x.itemName}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading ItemName:", error);

            throw error;
        });
}
function DDlFromPlace() {

    return $.ajax({
        url: `/InventoryDepartmentIssue/DDlPlaceFrom`,
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDlPlaceFrom:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDlPlaceFrom response is not an array");
            }


            PlaceFromList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading ItemName:", error);

            throw error;
        });
}
function AddRow(data = {}) {

    let tbody = $('#tblItemdetails tbody');

    let newRow = `
        <tr class="no-border-input">
            <td>  <input class="erppagetable-control ItemCode" value="${data.itemCode ?? ''}" readonly /> </td>
            <td> <select class="erppagetable-control ddlItemname"> <option value="">-- Select Item --</option>  ${ItemNameList}  </select> </td>
            <td class="hidden-col"> <input class="erppagetable-control txtunitcode" value="${data.unitCode ?? ''}" readonly /> </td>
            <td>  <input class="erppagetable-control txtunitname" value="${data.unitName ?? ''}" readonly />  </td>
            <td>  <input type="number"  class="erppagetable-control txt_lot" value="${data.lot ?? ''}"  oninput="limitMaxLength(this, 10)" /> </td>
            <td> <input type="number" class="erppagetable-control TxtNos" value="${data.nos ?? ''}"  oninput="limitMaxLength(this, 10)" />  </td>
            <td>  <input type="number" class="erppagetable-control Txtweight" value="${data.weight ?? ''}"  oninput="limitMaxLength(this, 13)" />  </td>
            <td> <select class="erppagetable-control TxtToPlace">  <option value="">-- Select To Place --</option>  ${PlaceFromList} </select>  </td>
            <td> <select class="erppagetable-control TxtPlaceFrom">  <option value="">-- Select From Place --</option>  ${PlaceFromList} </select>  </td>
            <td>  <button type="button" class="btn btn-primary">  Show Batch  </button>  </td>
            <td> <input type="text"  class="erppagetable-control TxtRemark" value="${data.remark ?? ''}"  oninput="limitMaxLength(this, 13)" /> </td>
            <td>  <input type="number"  class="erppagetable-control TxtRate"  value="${data.rate ?? ''}" oninput="limitMaxLength(this, 13)" />   </td>
            <td>  <input type="number" class="erppagetable-control TxtAmount" value="${data.Amount ?? ''}" oninput="limitMaxLength(this, 13)" />  </td>
            <td>   <input type="number"  class="erppagetable-control TxtLDRate" value="${data.LDRate ?? ''}" oninput="limitMaxLength(this, 13)" /> </td>
            <td> <input type="number" class="erppagetable-control TxtLDAmount"  value="${data.LDAmount ?? ''}" oninput="limitMaxLength(this, 13)" />   </td>
            <td> <input type="number" class="erppagetable-control TxtProdType" value="${data.ProdType ?? ''}"  oninput="limitMaxLength(this, 13)" />  </td>
            <td>  <input type="number" class="erppagetable-control TxtProdNo" value="${data.ProdNo ?? ''}" oninput="limitMaxLength(this, 13)" /> </td>
            <td class="action-col">
                <button type="button"  class="act-btn add"  onclick="AddRow()">   <i class="fa fa-plus-circle"></i> </button>
                <button type="button"  class="act-btn delete"   onclick="DeleteRow(this)"> <i class="fa fa-trash"></i>  </button>
            </td>

        </tr>
    `;

    tbody.append(newRow);

    let $row = tbody.find('tr:last');


    $row.find('.ddlItemname').val(data.itemCode ?? '');
    $row.find('.TxtPlaceFrom').val(data.placeCode ?? '');
    $row.find('.TxtToPlace').val(data.TOplaceCode ?? '');

    // Load item details
    if (data.itemCode) {
        SetItemDetails($row);
    }
}
function SetItemDetails($row) {

    let itemCode = $row.find('.ddlItemname').val();

    if (!itemCode) {
        $row.find('.ItemCode').val('');
        $row.find('.txtunitcode').val('');
        $row.find('.txtunitname').val('');
        return;
    }

    let item = ItemDetailsList.find(x =>
        String(x.itemCode) === String(itemCode)
    );

    if (!item) {
        console.warn("Item not found:", itemCode);
        return;
    }

    // Same row only
    $row.find('.ItemCode').val(item.itemCode);
    $row.find('.txtunitcode').val(item.ucode);
    $row.find('.txtunitname').val(item.unit);
}
function CalculateAmount($row) {

    let nos = parseFloat($row.find('.TxtNos').val()) || 0;
    let rate = parseFloat($row.find('.TxtRate').val()) || 0;

    let amount = nos * rate;

    $row.find('.TxtAmount').val(amount.toFixed(2));
    $row.find('.TxtLDRate').val(rate.toFixed(2));
    $row.find('.TxtLDAmount').val(amount.toFixed(2));
}
function DeleteRow(button) {

    let row = $(button).closest('tr');

    if (row.length === 0) {
        return;
    }

    row.remove();
}
function formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    if (isNaN(d)) return '';

    return d.getFullYear() + '-' +
        String(d.getMonth() + 1).padStart(2, '0') + '-' +
        String(d.getDate()).padStart(2, '0');
}
function limitMaxLength(input, maxLength) {
    // Remove anything except digits
    input.value = input.value.replace(/\D/g, '');

    // Limit maximum digits
    if (input.value.length > maxLength) {
        input.value = input.value.substring(0, maxLength);
    }
}

async function CopyData(V_TYPE) {
    try {
        const Res = await $.ajax({
            url: '/InventoryDepartmentIssue/CopyData',
            type: 'GET',
            data: { V_TYPE: V_TYPE }
        });

        console.log("Res:", Res);

        // Res is an array
        if (Array.isArray(Res)) {
            Res.forEach(function (item) {
                CopyDataAddRow(item);
            });
        } 

    } catch (error) {
        console.error("Error:", error);
    }
}
function CopyDataAddRow(data = {}) {

    let tbody = $('#tbladjustmentissue tbody');

    let newRow = `
        <tr class="no-border-input">
            <td class="freeze-item"> <input type="checkbox" class="erppage-checkbox-input chk_box" /> </td>
            <td> <input class="erppagetable-control txt_vno" value="${data.vNo ?? ''}" readonly />  </td>
            <td>  <input class="erppagetable-control txt_vdate" value="${data.vDate ?? ''}" readonly />  </td>
            <td> <input class="erppagetable-control txt_Itemname" value="${data.itemName ?? ''}" readonly /> </td>
            <td> <input type="number" class="erppagetable-control TxtNos" value="${data.nos ?? ''}" /> </td>
            <td> <input type="number"  class="erppagetable-control txt_qty" value="${data.qty ?? ''}" />  </td>
            <td> <input type="text" class="erppagetable-control txt_unit" value="${data.unit ?? ''}" />  </td>
            <td>  <input type="text" class="erppagetable-control txt_make" value="${data.make ?? ''}" />  </td>
            <td>  <input type="text"  class="erppagetable-control txt_place"  value="${data.place ?? ''}" />  </td>
            <td> <input type="text" class="erppagetable-control txt_remarks"  value="${data.remarks ?? ''}" /> </td>
            <td class="hidden-col"> <input type="number"  class="erppagetable-control txt_itemcode" value="${data.itemCode ?? ''}" /> </td>
            <td class="hidden-col"> <input type="number" class="erppagetable-control txt_uomcode" value="${data.uoM_CODE ?? ''}" />  </td>
            <td class="hidden-col"> <input type="number" class="erppagetable-control txt_makecode" value="${data.makE_CODE ?? ''}" /> </td>
            <td class="hidden-col">  <input type="number"  class="erppagetable-control txt_placecode" value="${data.placeCode ?? ''}" /> </td>
        </tr>
    `;

    tbody.append(newRow);

    let $row = tbody.find('tr:last');

    // Set item code
    $row.find('.ddlItemname').val(data.itemCode ?? '');

    // Existing item
    if (data.itemCode) {
        SetItemDetails($row);
    }
}
function GetSelectedRowsData()
{
    let selectedData = [];
    $('#tbladjustmentissue tbody tr').each(function ()
    {
        let $row = $(this);

        if ($row.find('.chk_box').is(':checked')) {

            let rowData = {
                vNo: $row.find('.txt_vno').val(),
                vDate: $row.find('.txt_vdate').val(),      
                nos: $row.find('.TxtNos').val(),
                qty: $row.find('.txt_qty').val(),       
                remarks: $row.find('.txt_remarks').val(),
                itemCode: $row.find('.txt_itemcode').val(),
                unitCode: $row.find('.txt_uomcode').val(),
                unitName: $row.find('.txt_unit').val(),
                makE_CODE: $row.find('.txt_makecode').val(),
                placeCode: $row.find('.txt_placecode').val(),
                place: $row.find('.txt_place').val()
            };
            selectedData.push(rowData);
        }
    });
    return selectedData;
}
function GetInventoryDepartmentIssueDetails() {

    let details = [];

    $('#tblItemdetails tbody tr').each(function (index) {
        let $row = $(this);
        details.push({
            SNO: index + 1,
            ITEM_CODE: $row.find('.ItemCode').val() || null,
            ITEM_NAME: $row.find('.ddlItemname option:selected').text() || null,
            UOM_CODE: $row.find('.txtunitcode').val() || null,
            UOM_NAME: $row.find('.txtunitname').val() || null,
            LOT_NO: $row.find('.txt_lot').val() || null,
            NOS: $row.find('.TxtNos').val() || null,
            QTY: $row.find('.Txtweight').val() || null,
            TO_DEPT: $row.find('.TxtToPlace').val() || null,
            FROM_DEPT: $row.find('.TxtPlaceFrom').val() || null,
            REMARKS: $row.find('.TxtRemark').val() || null,
            RATE: $row.find('.TxtRate').val() || null,
            AMOUNT: $row.find('.TxtAmount').val() || null,
            LAND_RATE: $row.find('.TxtLDRate').val() || null,
            LAND_AMT: $row.find('.TxtLDAmount').val() || null,
            PORD_TYPE: $row.find('.TxtProdType').val() || null,
            PORD_NO: $row.find('.TxtProdNo').val() || null
        });
    });
    return details;
}


async function LoadData() {

    try {

        const res = await $.ajax({
            url: '/InventoryDepartmentIssueList/GetDataByCode',
            type: 'POST',
            data: {
                DocID: rowId
            }
        });

        console.log("API Response:", res);

        if (!res.success) {
            console.error("Server Error:", res.message);
            alert(res.message || "Unable to load data.");
            return null;
        }

        const header = res.data.header;
        const details = res.data.details;


        console.log("Header Data:", header);    
        console.log("Details Data:", details);
      
        return res.data;
    }
    catch (error) {

        console.error("Error loading data:", error);

        if (error.responseJSON) {
            console.error("Server Response:", error.responseJSON);
        }

        alert("Error while loading inventory opening data.");

        return null;
    }
}



