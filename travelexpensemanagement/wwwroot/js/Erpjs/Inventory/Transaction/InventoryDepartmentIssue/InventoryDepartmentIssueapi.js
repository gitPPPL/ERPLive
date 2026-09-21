
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
            DDlFromPlace(),
            DDLCOSTCAT(),
            DDLCostSubCategory(),
            DDLCostCenter()
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
       
        if (ddl.hasClass("select2-hidden-accessible"))
        {
            ddl.select2('destroy');
        }

        ddl.empty().append('<option value="">-- Select Prod Type --</option>');

        data.forEach(item => {
            ddl.append(
                `<option value="${item.value}"> ${item.text}||${item.value} </option>`
            );
        });
               
        ddl.select2({ placeholder: 'Search Product...', allowClear: true, width: '100%'  });

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
        ddl.empty().append('<option value="">-- Select Do No --</option>');
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
function DDLCOSTCAT() {

    return $.ajax({
        url: `/InventoryDepartmentIssue/DDLCOSTCAT`,
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            if (!Array.isArray(data)) {
                throw new Error("DDLCOSTCAT response is not an array");
            }
            PCostCategorylist = data.map(x => `<option value="${x.value}">${x.text}</option>` ) .join('');
        })
        .catch(function (error) {

            console.error("Error loading DDLCOSTCAT:", error);

            throw error;
        });
}
function DDLCostSubCategory() {

    return $.ajax({
        url: `/InventoryDepartmentIssue/DDLCostSubCategory`,
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            if (!Array.isArray(data)) {
                throw new Error("DDLCostSubCategory response is not an array");
            }
            CostSubCategorylist = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        })
        .catch(function (error) {

            console.error("Error loading DDLCostSubCategory:", error);

            throw error;
        });
}
function DDLCostCenter() {

    return $.ajax({
        url: `/InventoryDepartmentIssue/DDLCostCenter`,
        method: 'GET',
        dataType: 'json'
    })
        .then(function (data) {

            if (!Array.isArray(data)) {
                throw new Error("DDLCostCenter response is not an array");
            }
            CostCenterlist = data.map(x => `<option value="${x.value}">${x.text}</option>`).join('');
        })
        .catch(function (error) {

            console.error("Error loading DDLCostCenter:", error);

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
            <td class="hidden-col"> <select class="erppagetable-control TxtPlaceFrom">  <option value="">-- Select From Place --</option>  ${PlaceFromList} </select>  </td>
            <td> <select class="erppagetable-control TxtPCostCategory">  <option value="">-- Select c --</option>  ${PCostCategorylist} </select>  </td>
            <td> <select class="erppagetable-control TxtCostSubCategory">  <option value="">-- Select Cost Sub Category --</option>  ${CostSubCategorylist} </select>  </td>
            <td> <select class="erppagetable-control TxtCostCenter ">  <option value="">-- Select Cost Center --</option>  ${CostCenterlist} </select>  </td>
            <td> <input type="text"  class="erppagetable-control TxtRemark" value="${data.remark ?? ''}"  /> </td>
            <td>  <input type="number"  class="erppagetable-control TxtRate"  value="${data.rate ?? ''}" oninput="limitMaxLength(this, 13)" />   </td>
            <td>  <input type="number" class="erppagetable-control TxtAmount" value="${data.Amount ?? ''}" oninput="limitMaxLength(this, 13)" readonly />  </td>
            <td>   <input type="number"  class="erppagetable-control TxtLDRate" value="${data.LDRate ?? ''}" oninput="limitMaxLength(this, 13)" readonly /> </td>
            <td> <input type="number" class="erppagetable-control TxtLDAmount"  value="${data.LDAmount ?? ''}" oninput="limitMaxLength(this, 13)"  readonly/>   </td>
            <td> <input type="number" class="erppagetable-control TxtProdType" value="${data.ProdType ?? ''}"   readonly />  </td>
            <td>  <input type="number" class="erppagetable-control TxtProdNo" value="${data.ProdNo ?? ''}"   readonly/> </td>
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
    $row.find('.TxtPCostCategory').val(data.COSTCAT_CODE ?? '');
    $row.find('.TxtCostSubCategory').val(data.COSTSCAT_CODE ?? '');
    $row.find('.TxtCostCenter').val(data.COSTCENTER_CODE ?? '');

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
    $row.find('.ddlItemname').val(data.itemCode ?? '');

    if (data.itemCode)
    {
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

            ITEM_CODE: toInt($row.find('.ItemCode').val()),
            ITEM_NAME: $row.find('.ddlItemname option:selected').text() || null,

            UOM_CODE: toInt($row.find('.txtunitcode').val()),
            UOM_NAME: $row.find('.txtunitname').val() || null,

            LOT_NO: $row.find('.txt_lot').val() || null,

            NOS: toInt($row.find('.TxtNos').val()),
            QTY: toDecimal($row.find('.Txtweight').val()),

            TO_DEPT: toInt($row.find('.TxtToPlace').val()),
            FROM_DEPT: toInt($row.find('.TxtPlaceFrom').val()),

            COSTCAT_CODE: toInt($row.find('.TxtPCostCategory').val()),
            COSTSCAT_CODE: toInt($row.find('.TxtCostSubCategory').val()),
            COSTCENTER_CODE: toInt($row.find('.TxtCostCenter').val()),

            REMARKS: $row.find('.TxtRemark').val() || null,

            RATE: toDecimal($row.find('.TxtRate').val()),
            AMOUNT: toDecimal($row.find('.TxtAmount').val()),

            LAND_RATE: toDecimal($row.find('.TxtLDRate').val()),
            LAND_AMT: toDecimal($row.find('.TxtLDAmount').val()),

            PORD_TYPE: $row.find('.TxtProdType').val() || null,
            PORD_NO: toInt($row.find('.TxtProdNo').val())
        });
    });

    return details;
}

async function LoadData()
{
    try {
        const res = await $.ajax({
            url: '/InventoryDepartmentIssueList/GetDataByCode',
            type: 'POST',
            data: { DocID: rowId }
        });

        if (!res.success) {
            console.error("Server Error:", res.message);
            alert(res.message || "Unable to load data.");
            return null;
        }

        const header = res.data.header;
        const details = res.data.details;

        console.log("Header Data:", header);    
        console.log("Details Data:", details);

        // Header
        $('#CODE').val(header.doC_ID);
        $('#ddlDocType').val(header.v_TYPE);
        $('#NumDocno').val(header.v_NO);
        $('#DtDocDate').val(formatDate(header.v_DATE));
        $('#ddlStatus').val(header.status);
        $('#ddlShift').val(header.shift);
        $('#NumSlipNo').val(header.sliP_NO);

        $('#ddlProdOrdNo').val(header.porD_NO).trigger('change');

   
        $('#ddlDoNo') .val(header.plaN_NO) .trigger('change');



        $('#TxtRemarks').val(header.remarks);
        // Header End

        // Table Data
        let tbody = $('#tblItemdetails tbody');

        tbody.empty();

        if (Array.isArray(details) && details.length > 0) {

            details.forEach(item => {

                AddRow({
                    itemCode: item.iteM_CODE ?? '',
                    unitCode: item.uoM_CODE ?? '',
                    unitName: item.uoM_NAME ?? '',
                    lot: item.loT_NO ?? '',
                    nos: item.nos ?? '',
                    weight: item.qty ?? '',
                    placeCode: item.froM_DEPT ?? '',
                    TOplaceCode: item.tO_DEPT ?? '',
                    COSTCAT_CODE: item.costcaT_CODE ?? '',
                    COSTSCAT_CODE: item.costscaT_CODE ?? '',
                    COSTCENTER_CODE: item.costcenteR_CODE ?? '',
                    remark: item.remarks ?? '',
                    rate: item.rate ?? '',
                    Amount: item.amount ?? '',
                    LDRate: item.lanD_RATE ?? '',
                    LDAmount: item.lanD_AMT ?? '',
                    ProdType: item.porD_TYPE ?? '',
                    ProdNo: item.porD_NO ?? ''
                });
            });
        }
        else
        {
         AddRow();
        }
        //Table End
              
        return res.data;
    }
    catch (error)
    {

        console.error("Error loading data:", error);

        if (error.responseJSON) {
            console.error("Server Response:", error.responseJSON);
        }

        alert("Error while loading inventory opening data.");

        return null;
    }
}
function validateInventoryDetails() {

    let isValid = true;
    let firstInvalidRow = null;
    let hasSelectedItem = false;
    let v_type = $('#ddlDocType').val();
    $('#tblItemdetails tbody tr').each(function (index) {

        let row = $(this);

        let itemCode = $.trim(row.find('.ddlItemname').val() || '');

  
        if (itemCode !== '') {

            hasSelectedItem = true;

            let qty = $.trim(row.find('.TxtNos').val() || '');
            let fromDept = $.trim(row.find('.TxtPlaceFrom').val() || '');
            let toDept = $.trim(row.find('.TxtToPlace').val() || '');
       

            if (PubUserLevel != 1) {
                if (qty === '') {
                    showToast(`Please enter Qty in row ${index + 1}`, "Error");
                    firstInvalidRow = row;
                    isValid = false;
                    return false;
                }
            }
                      
            if (v_type == 'RAID') {
                if (fromDept === '' || toDept === '') {
                    showToast(`From Department OR To Department is empty at line no.=> ${index + 1}`, "Error");
                    firstInvalidRow = row;
                    isValid = false;
                    return false;
                }         
            }           
                   
        }
    });

    if (!hasSelectedItem) {
        showToast("Please select at least one Item.", "Error");
        return false;
    }

    if (firstInvalidRow) {
        $('html, body').animate({ scrollTop: firstInvalidRow.offset().top - 150 }, 300);
    }

    return isValid;
}
function toInt(value) {
    value = $.trim(value || '');
    return value === '' ? null : parseInt(value, 10);
}
function toDecimal(value) {
    value = $.trim(value || '');
    return value === '' ? null : parseFloat(value);
}
function TransitReport() {

    if (!rowId) {
        showToast(`Please save the data before printing the report.`, { type: "info" });
        return;
    }

    var reportName = "RAW11";

    var v_no = $('#NumDocno').val();
    var v_type = $('#ddlDocType').val();
    var selectedText = $('#ddlDocType option:selected').text();

    var formula =
        "{ISSUE1.V_TYPE} = '" + v_type + "'" +
        " and {ISSUE1.V_NO} = " + v_no + "" +
        " and {ISSUE1.COMP_CODE} = " + globalVars.CompCode + "" +
        " and {ISSUE1.YEAR_CODE} = " + globalVars.FYearCode + "" +
        " and {ISSUE1.BRANCH_CODE} = " + globalVars.BranchCode + "";


    var payload = {
        Reportname: reportName,
        selectionFormula: formula,
        Database: database,
        Parameters: {
            comp_name: globalVars.CompanyName || "",
            comp_add1: globalVars.Address1 || "",
            comp_add2: globalVars.Address2 || "",        
            RPTNAME: selectedText
        }
    };


    var now = new Date();
    var timestamp =
        String(now.getDate()).padStart(2, '0') +
        String(now.getMonth() + 1).padStart(2, '0') +
        String(now.getFullYear()).slice(-2) + "_" +
        String(now.getHours()).padStart(2, '0') +
        String(now.getMinutes()).padStart(2, '0') +
        String(now.getSeconds()).padStart(2, '0');


    $.ajax({
        url: 'http://localhost:24085/Report/PendingQCReport',
        type: 'POST',
        data: JSON.stringify(payload),
        contentType: "application/json",
        xhrFields: { responseType: 'blob' },

        success: function (response) {

            var file = new Blob([response], { type: 'application/pdf' });
            var fileName = `${reportName}_${timestamp}.pdf`;


            var link = document.createElement('a');
            link.href = URL.createObjectURL(file);
            link.download = fileName;
            document.body.appendChild(link);
            link.click();
            document.body.removeChild(link);
        },

        error: function (xhr, status, error) {
            if (xhr.status === 0) {
                console.error("Cannot connect to API. Is the backend running?");
            } else {
                console.error('Error generating report:', xhr.status, xhr.statusText, error);
                xhr.responseText && console.error('Response:', xhr.responseText);
            }
        }
    });
}