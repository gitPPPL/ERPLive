

async function DDLVtype() {

    try {

        const res = await fetch('/InventoryOpeningEntry/DDlVType');

        if (!res.ok) {
            throw new Error(`HTTP ${res.status}`);
        }

        const data = await res.json();

        const ddl = $('#ddlDocType');

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
function DDlItemName() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDlItemName',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDlItemName:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDlItemName response is not an array");
            }

            ItemNameList = data
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
function DDlUnit() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDlUnit',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDlUnit:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDlUnit response is not an array");
            }

            unitnameList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading Unit:", error);

            throw error;
        });
}
function DDLItemmake() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDLItemmake',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDLItemmake:", data);

            if (!Array.isArray(data)) {
                throw new Error("DDLItemmake response is not an array");
            }

            ItemmakeList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading ItemMake:", error);

            throw error;
        });
}
function DDLItemDapt() {

    return $.ajax({
        url: '/InventoryOpeningEntry/DDLItemDapt',
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            console.log("DDLItemDapt:", data);

            if (!Array.isArray(data)) {

                console.error(
                    "ItemDeptList response is not an array:",
                    data
                );

                throw new Error(
                    "Invalid ItemDeptList response"
                );
            }

            ItemDeptList = data
                .map(x =>
                    `<option value="${x.value}">${x.text}</option>`
                )
                .join('');

        })
        .catch(function (error) {

            console.error("Error loading Item Department:", error);

            throw error;
        });
}

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

async function LoadDropDown() {

    try {

        await Promise.all([
            DDLVtype(),
            DDlItemName(),
            DDlUnit(),
            DDLItemmake(),
            DDLItemDapt()
        ]);

        console.log("All dropdowns loaded successfully");

    } catch (error) {

        console.error("LoadDropDown Error:", error);

        showToast(
            "Error loading dropdowns",
            { type: "error" }
        );

        throw error;
    }
}

function AddRow(data = {}) {

    let tbody = $('#tblInventoryOpeningEntry tbody');

    // Generate next row number
    let rowCount = tbody.find('tr[id^="row"]').length + 1;

    let newRow = `
        <tr id="row${rowCount}" class="no-border-input">
            <td class="hidden-col"> <input type="hidden"   value="${data.code ?? ""}" /> </td>
     
            <td>
                <select  class="erppagetable-control ddlItemname">  <option value="">-- Select Item --</option>  ${ItemNameList} </select>
            </td>

      
            <td>
                <select  class="erppagetable-control ItemMake"> <option value="">-- Select Make --</option>  ${ItemmakeList} </select>
            </td>
                
            <td>
                <select   class="erppagetable-control Unit">  <option value="">-- Select Unit --</option> ${unitnameList} </select>
            </td>

            <td>
                <input type="text"   class="erppagetable-control TxtNos" value="${data.nos ?? ""}" />
            </td>

            <td>
                <input type="text"   class="erppagetable-control TxtQty"  value="${data.qty ?? ""}" />
            </td>

   
            <td>
                <input type="text"   class="erppagetable-control TxtRate" value="${data.rate ?? ""}" />
            </td>

   
            <td>
                <input type="text"   class="erppagetable-control TxtAmount" value="${data.amount ?? ""}"  readonly />
            </td>

            <td>
                <select  class="erppagetable-control ItemDept">  <option value="">-- Select Department --</option> ${ItemDeptList}  </select>
            </td>

            <td>
                <input type="text"   class="erppagetable-control TxtRemarks"  value="${data.Remark}" />
            </td>

            <td class="text-center">
                <button type="button"  class="act-btn add" onclick="AddRow()">  <i class="fa fa-plus-circle"></i>  </button>
                <button type="button" class="act-btn delete" onclick="DeleteRow(this)">   <i class="fa fa-trash"></i> </button>
            </td>

        </tr>
    `;

    tbody.append(newRow);


    // =========================================================
    // Set Existing Data When Editing
    // =========================================================

    if (Object.keys(data).length > 0) {

        $(`#ddlItemname${rowCount}`)
            .val(data.itemCode ?? data.itemName ?? data.itemname ?? "");

        $(`#ItemMake${rowCount}`)
            .val(data.itemMake ?? data.itemmake ?? "");

        $(`#Unit${rowCount}`)
            .val(data.unit_code ?? data.unit ?? "");

        $(`#ItemDept${rowCount}`)
            .val(data.itemDept ?? data.itemdept ?? "");


        // Calculate amount for existing row
        let qty = parseFloat(data.qty) || 0;
        let rate = parseFloat(data.rate) || 0;

        if (qty && rate) {
            $(`#TxtAmount${rowCount}`).val(
                (qty * rate).toFixed(2)
            );
        }
    }
}

function DeleteRow(button) {

    let row = $(button).closest('tr');

    if (row.length === 0) {
        return;
    }

    row.remove();
}

async function FetchdatabyItemCode(ItemCode) {

    try {

        const res = await $.ajax({
            url: 'InventoryOpeningEntry/GetDataByItemcode',
            type: 'POST',
            data: {
                ItemCode: ItemCode
            }
        });

        return res;

    } catch (error) {

        console.log("Error:", error);
        return null;

    }
}
