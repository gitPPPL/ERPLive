
async function LoadDropdown() {
    try {
        await Promise.all([
            cmbPartyName(),
            cmbLocation()
        ]);


    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function cmbPartyName() {
    try {
        const res = await fetch('/ImportExportDocAttachmentList/cmbPartyName');
        const data = await res.json();
        const ddl = $('#ddlpartyname');
        ddl.empty().append('<option value="">---Select Party Name---</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function cmbLocation() {
    try {
        const res = await fetch('/ImportExportDocAttachmentList/cmbLocation');
        const data = await res.json();
        const ddl = $('#ddllocation');
        ddl.empty().append('<option value="">--Select Location--</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}
function formatDate(date) {
    if (!date) return "";
    const d = new Date(date);
    if (isNaN(d.getTime())) return "";
    const day = String(d.getDate()).padStart(2, '0');
    const month = String(d.getMonth() + 1).padStart(2, '0');
    const year = d.getFullYear();
    return `${day}/${month}/${year}`;
}

const tableHeadersExport = [
    { text: "Code", className: "hidden-col", width: "0px" },
    { text: "Inv No", width: "120px" },
    { text: "Inv Date", width: "110px" },
    { text: "Exim No", width: "120px" },
    { text: "Exim Date", width: "110px" },
    { text: "Party Name", width: "220px" },
    { text: "Party Code", width: "120px" },
    { text: "S B No.", width: "120px" },
    { text: "Chk", width: "60px" },
    { text: "S B Copy", width: "120px" },
    { text: "B L Copy", width: "120px" },
    { text: "BRC Copy", width: "120px" },
    { text: "Oth Copy1", width: "120px" },
    { text: "Oth Copy2", width: "120px" },
    { text: "Oth Copy3", width: "120px" },
    { text: "Oth Copy4", width: "120px" },
    { text: "Oth Copy5", width: "120px" },
    { text: "Oth Copy6", width: "120px" },
    { text: "Oth Copy7", width: "120px" }
];

const tableHeadersForImport = [
    { text: "Code", className: "hidden-col", width: "0px" },
    { text: "Sauda No", width: "120px" },
    { text: "Sauda Date", width: "110px" },
    { text: "Exim No", width: "120px" },
    { text: "Exim Date", width: "110px" },
    { text: "Party Name", width: "220px" },
    { text: "Party Code", width: "120px" },
    { text: "B E No.", width: "120px" },



    { text: "Chk", width: "60px" },



    { text: "P I Copy", width: "120px" },
    { text: "B L Copy", width: "120px" },
    { text: "B E Copy", width: "120px" },
    { text: "L C Copy", width: "120px" },
    { text: "INV Copy", width: "120px" },
    { text: "D P Copy", width: "120px" },
    { text: "SBLC Copy", width: "120px" },
    { text: "Oth Copy1", width: "120px" },
    { text: "Oth Copy2", width: "120px" }
];
function createTableHeader() {

    let v_type = $("#ddltype").val();
    let headers = (v_type === "Import")
        ? tableHeadersForImport
        : tableHeadersExport;

    let theadHtml = "<tr>";
    let colgroupHtml = "";

    headers.forEach(h => {
        theadHtml += `<th class="${h.className || ""}">${h.text}</th>`;
        colgroupHtml += `<col class="${h.className || ""}" style="width:${h.width || "auto"};">`;
    });

    theadHtml += "</tr>";

    $("#tblImportExportDocAttachmentList colgroup").remove();
    $("#tblImportExportDocAttachmentList").prepend(`<colgroup>${colgroupHtml}</colgroup>`);

    $("#tblImportExportDocAttachmentList thead").html(theadHtml);
}

async function Viewdata() {

    try {

        const fromDate = $('#Dtfrom').val();
        const toDate = $('#Dtto').val();
        const V_TYPE = $('#ddltype').val();
        const partycode = $('#ddlpartyname').val();
        const Citycode = $('#ddllocation').val();

        const res = await $.ajax({
            url: '/ImportExportDocAttachmentList/GetViewData',
            type: 'GET',
            data: {
                FromDate: fromDate,
                ToDate: toDate,
                V_TYPE: V_TYPE,
                partycode: partycode,
                Citycode: Citycode
            }
        });

        console.log("Table Data:", res);

        const tbody = $('#tblImportExportDocAttachmentList tbody');

        tbody.empty();

        if (!res.success || !res.data || res.data.length === 0) {

            tbody.append(` <tr class="no-record-row"> <td colspan="25" class="text-center">  No Record Found  </td>  </tr>  `);

            return;
        }

        // Create rows according to table structure
        $.each(res.data, function (index, item) {
            addItemRecordRow(item);

        });

    }
    catch (error) {

        console.error('Viewdata Error:', error);

        toastr.error('Error loading data.');
    }
}
function addItemRecordRow(item = null) {
    let v_type = $('#ddltype').val();

    let tbody = $('#tblImportExportDocAttachmentList tbody');
    let newRow = '';
    // Count only actual data rows
    let rowCount = tbody.find('tr[id^="row"]').length + 1;

    if (v_type == "Import") {
         newRow = `
        <tr id="row${rowCount}" class="no-border-input">

            <td class="hidden-col">  <input type="hidden" id="TxtCode${rowCount}" value="${item?.code ?? ""}" /> </td>
            <td > <input type="text" id="TxtSaudaNo${rowCount}" class="erppagetable-control"  value="${item?.saudA_NO ?? ""}" /> </td>
            <td> <input type="text" id="TxtSaudaDate${rowCount}"  class="erppagetable-control" value="${item?.sauda_Date ?? ""}" />  </td>
            <td > <input type="text" id="TxtSaudaNo${rowCount}" class="erppagetable-control"  value="${item?.v_NO ?? ""}" /> </td>
            <td> <input type="text" id="TxtSaudaDate${rowCount}"  class="erppagetable-control" value="${item?.eximDate ?? ""}" />  </td>
            <td>  <input type="text"  id="TxtPartyName${rowCount}"  class="erppagetable-control"  value="${item?.partyName ?? ""}" />  </td>
            <td> <input type="text"  id="TxtPartyCode${rowCount}"   class="erppagetable-control" value="${item?.partY_CODE ?? ""}" />  </td> 
            <td> <input type="text"  id="TxtBENo${rowCount}"  class="erppagetable-control"  value="${item?.bE_NO ?? ""}" /> </td>
               <td class="text-center"> <input type="checkbox" id="Chk${rowCount}" /> </td>
            <td>  <input type="text"  id="TxtPICopy${rowCount}"  class="erppagetable-control"  value="${item?.piCopy ?? ""}" />  </td>
            <td>  <input type="text"  id="TxtBLCopy${rowCount}"  class="erppagetable-control"  value="${item?.blCopy ?? ""}" /> </td>
            <td> <input type="text"  id="TxtBECopy${rowCount}" class="erppagetable-control"  value="${item?.beCopy ?? ""}" />  </td>
            <td>  <input type="text" id="TxtLCCopy${rowCount}" class="erppagetable-control" value="${item?.lcCopy ?? ""}" />  </td>
            <td>  <input type="text" id="TxtINVCopy${rowCount}"  class="erppagetable-control"  value="${item?.invCopy ?? ""}" />  </td>
            <td> <input type="text"  id="TxtDPCopy${rowCount}"  class="erppagetable-control"  value="${item?.dpCopy ?? ""}" />   </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.sblcCopy ?? ""}" />  </td>
            <td>  <input type="text" id="TxtOthCopy1${rowCount}"  class="erppagetable-control" value="${item?.othCopy1 ?? ""}" />  </td>
            <td> <input type="text"  id="TxtOthCopy2${rowCount}"  class="erppagetable-control"  value="${item?.othCopy2 ?? ""}" /> </td>
            </tr>
    `;
    }
    else {
         newRow = `
        <tr id="row${rowCount}" class="no-border-input">

            <td class="hidden-col">  <input type="hidden" id="TxtCode${rowCount}" value="${item?.code ?? ""}" /> </td>
             <td> <input type="text" id="TxtInvNo${rowCount}" class="erppagetable-control"  value="${item?.saudA_NO ?? ""}" />  </td>
            <td>  <input type="text"  id="TxtInvDate${rowCount}"  class="erppagetable-control" value="${item?.sauda_Date ?? ""}" />  </td>
            <td> <input type="text" id="TxtEximNo${rowCount}" class="erppagetable-control" value="${item?.v_NO ?? ""}" />  </td>
            <td> <input type="text"  id="TxtEximDate${rowCount}"  class="erppagetable-control"  value="${item?.eximDate ?? ""}" />  </td>
            <td>  <input type="text"  id="TxtPartyName${rowCount}"  class="erppagetable-control"  value="${item?.partyName ?? ""}" />  </td>
            <td> <input type="text"  id="TxtPartyCode${rowCount}"   class="erppagetable-control" value="${item?.partY_CODE ?? ""}" />  </td> 
            <td> <input type="text"  id="TXTSBNO{rowCount}"   class="erppagetable-control" value="${item?.bE_NO ?? ""}" />  </td> 
            <td class="text-center"> <input type="checkbox" id="Chk${rowCount}" /> </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.sbCopy ?? ""}" />  </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.blCopy ?? ""}" />  </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.brcCopy ?? ""}" />  </td>
            <td>  <input type="text" id="TxtOthCopy1${rowCount}"  class="erppagetable-control" value="${item?.othCopy1 ?? ""}" />  </td>
            <td> <input type="text"  id="TxtOthCopy2${rowCount}"  class="erppagetable-control"  value="${item?.othCopy2 ?? ""}" /> </td>
            <td> <input type="text" id="TxtOthCopy3${rowCount}" class="erppagetable-control" value="${item?.othCopy3 ?? ""}" />  </td>
            <td> <input type="text"  id="TxtOthCopy4${rowCount}"  class="erppagetable-control"  value="${item?.othCopy4 ?? ""}" /> </td>
            <td> <input type="text"  id="TxtOthCopy5${rowCount}"  class="erppagetable-control" value="${item?.othCopy5 ?? ""}" /> </td>
            <td> <input type="text" id="TxtOthCopy6${rowCount}" class="erppagetable-control" value="${item?.othCopy6 ?? ""}" />  </td>
            <td>  <input type="text"  id="TxtOthCopy7${rowCount}" class="erppagetable-control" value="${item?.othCopy7 ?? ""}" /> </td>
        </tr>
    `;
    }

    tbody.append(newRow);
}
