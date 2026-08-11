
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
        {
            text: ` <input type="checkbox" id="chkSelectAllExport" class="form-check-input" title="Select All"> `, width: "60px"
        },
    { text: "S B Copy", width: "120px" },
    { text: "S B Path", width: "120px" },
    { text: "B L Copy", width: "120px" },
    { text: "B L Path", width: "120px" },
    { text: "BRC Copy", width: "120px" },
    { text: "BRC Path", width: "120px" },
    { text: "Oth Copy1", width: "120px" },
    { text: "Oth Path1", width: "120px" },
    { text: "Oth Copy2", width: "120px" },
    { text: "Oth Path2", width: "120px" },
    { text: "Oth Copy3", width: "120px" },
    { text: "Oth Path3", width: "120px" },
    { text: "Oth Copy4", width: "120px" },
    { text: "Oth Path4", width: "120px" },
    { text: "Oth Copy5", width: "120px" },
    { text: "Oth Path5", width: "120px" },
    { text: "Oth Copy6", width: "120px" },
    { text: "Oth Path6", width: "120px" },
    { text: "Oth Copy7", width: "120px" },
    { text: "Oth Path7", width: "120px" }

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

    {
        text: ` <input type="checkbox" id="chkSelectAllImport" class="form-check-input" title="Select All"> `, width: "60px"
    },

        { text: "P I Copy", width: "120px" },
        { text: "P I Path", width: "120px" },
        { text: "B L Copy", width: "120px" },
        { text: "B L Path", width: "120px" },
        { text: "B E Copy", width: "120px" },
        { text: "B E Path", width: "120px" },
        { text: "L C Copy", width: "120px" },
        { text: "L C Path", width: "120px" },
        { text: "INV Copy", width: "120px" },
        { text: "INV Path", width: "120px" },
        { text: "D P Copy", width: "120px" },
        { text: "D P Path", width: "120px" },
        { text: "SBLC Copy", width: "120px" },
        { text: "SBLC Path", width: "120px" },
        { text: "Oth Copy1", width: "120px" },
        { text: "Oth Path1", width: "120px" },
        { text: "Oth Copy2", width: "120px" },
        { text: "Oth Path2", width: "120px" }
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

            <td class="text-center">
             <input type="checkbox" id="Chk${rowCount}" class="form-check-input row-select" />
            </td>

            <td>  <input type="text"  id="TxtPICopy${rowCount}"  class="erppagetable-control"  value="${item?.piCopy ?? ""}" readonly />  </td>
            <td>  <input type="text"  id="TxtPIPath${rowCount}"  class="erppagetable-control"  value="${item?.piCopyFILE_Path ?? ""}" readonly />  </td>
            <td>  <input type="text"  id="TxtBLCopy${rowCount}"  class="erppagetable-control"  value="${item?.blCopy ?? ""}"  readonly/> </td>
            <td>  <input type="text"  id="TxtBLPath${rowCount}"  class="erppagetable-control"  value="${item?.blCopyFILE_Path ?? ""}"  readonly/> </td>
            <td> <input type="text"  id="TxtBECopy${rowCount}" class="erppagetable-control"  value="${item?.beCopy ?? ""}"  readonly/>  </td>
            <td> <input type="text"  id="TxtBEPath${rowCount}" class="erppagetable-control"  value="${item?.beCopyFILE_Path ?? ""}"  readonly/>  </td>
            <td>  <input type="text" id="TxtLCCopy${rowCount}" class="erppagetable-control" value="${item?.lcCopy ?? ""}" readonly />  </td>
            <td>  <input type="text" id="TxtLCPath${rowCount}" class="erppagetable-control" value="${item?.lcCopyFILE_Path ?? ""}" readonly />  </td>
            <td>  <input type="text" id="TxtINVCopy${rowCount}"  class="erppagetable-control"  value="${item?.invCopy ?? ""}"  readonly/>  </td>
            <td>  <input type="text" id="TxtINVPath${rowCount}"  class="erppagetable-control"  value="${item?.invCopyFILE_Path ?? ""}"  readonly/>  </td>
            <td> <input type="text"  id="TxtDPCopy${rowCount}"  class="erppagetable-control"  value="${item?.dpCopy ?? ""}"  readonly/>   </td>
            <td> <input type="text"  id="TxtDPPath${rowCount}"  class="erppagetable-control"  value="${item?.dpCopyFILE_Path ?? ""}"  readonly/>   </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.sblcCopy ?? ""}"  readonly/>  </td>
            <td> <input type="text"  id="TxtSBLCPath${rowCount}"  class="erppagetable-control"  value="${item?.sblcCopyFILE_Path ?? ""}"  readonly/>  </td>
            <td>  <input type="text" id="TxtOthCopy1${rowCount}"  class="erppagetable-control" value="${item?.othCopy1 ?? ""}"  readonly/>  </td>
            <td>  <input type="text" id="TxtOthPath${rowCount}"  class="erppagetable-control" value="${item?.othCopy1FILE_Path ?? ""}"  readonly/>  </td>
            <td> <input type="text"  id="TxtOthCopy2${rowCount}"  class="erppagetable-control"  value="${item?.othCopy2 ?? ""}" readonly /> </td>
            <td> <input type="text"  id="TxtOthPath${rowCount}"  class="erppagetable-control"  value="${item?.othCopy2FILE_Path ?? ""}" readonly /> </td>
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
            <td class="text-center">
            <input type="checkbox" id="Chk${rowCount}" class="form-check-input row-select" />
            </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.sbCopy ?? ""}" readonly />  </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.sbCopyFILE_Path ?? ""}" readonly />  </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.blCopy ?? ""}"  readonly />  </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.blCopyFILE_Path ?? ""}"  readonly />  </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.brcCopy ?? ""}" readonly />  </td>
            <td> <input type="text"  id="TxtSBLCCopy${rowCount}"  class="erppagetable-control"  value="${item?.brcCopyFILE_Path ?? ""}" readonly />  </td>
            <td>  <input type="text" id="TxtOthCopy1${rowCount}"  class="erppagetable-control" value="${item?.othCopy1 ?? ""}"  readonly />  </td>
            <td>  <input type="text" id="TxtOthCopy1${rowCount}"  class="erppagetable-control" value="${item?.othCopy1FILE_Path ?? ""}"  readonly />  </td>
            <td> <input type="text"  id="TxtOthCopy2${rowCount}"  class="erppagetable-control"  value="${item?.othCopy2 ?? ""}"  readonly /> </td>
            <td> <input type="text"  id="TxtOthCopy2${rowCount}"  class="erppagetable-control"  value="${item?.othCopy2FILE_Path ?? ""}"  readonly /> </td>
            <td> <input type="text" id="TxtOthCopy3${rowCount}" class="erppagetable-control" value="${item?.othCopy3 ?? ""}" readonly />  </td>
            <td> <input type="text" id="TxtOthCopy3${rowCount}" class="erppagetable-control" value="${item?.othCopy3FILE_Path ?? ""}" readonly />  </td>
            <td> <input type="text"  id="TxtOthCopy4${rowCount}"  class="erppagetable-control"  value="${item?.othCopy4 ?? ""}"  readonly /> </td>
            <td> <input type="text"  id="TxtOthCopy4${rowCount}"  class="erppagetable-control"  value="${item?.othCopy4FILE_Path ?? ""}"  readonly /> </td>
            <td> <input type="text"  id="TxtOthCopy5${rowCount}"  class="erppagetable-control" value="${item?.othCopy5 ?? ""}" readonly /> </td>
            <td> <input type="text"  id="TxtOthCopy5${rowCount}"  class="erppagetable-control" value="${item?.othCopy5FILE_Path ?? ""}" readonly /> </td>
            <td> <input type="text" id="TxtOthCopy6${rowCount}" class="erppagetable-control" value="${item?.othCopy6 ?? ""}" readonly />  </td>
            <td> <input type="text" id="TxtOthCopy6${rowCount}" class="erppagetable-control" value="${item?.othCopy6FILE_Path ?? ""}" readonly />  </td>
            <td>  <input type="text"  id="TxtOthCopy7${rowCount}" class="erppagetable-control" value="${item?.othCopy7 ?? ""}" readonly /> </td>
            <td>  <input type="text"  id="TxtOthCopy7${rowCount}" class="erppagetable-control" value="${item?.othCopy7FILE_Path ?? ""}" readonly /> </td>
        </tr>
    `;
    }

    tbody.append(newRow);
}

function getSelectedRowData() {

    let v_type = ($('#ddltype').val() || "").trim().toUpperCase();
    let selectedData = [];

    $('#tblImportExportDocAttachmentList tbody tr').each(function () {

        let row = $(this);

        // Only selected checkbox rows
        if (!row.find('.row-select').is(':checked')) {
            return;
        }

        let rowNo = row.attr('id').replace('row', '');

        let data = {
            Code: $(`#TxtCode${rowNo}`).val() || "",
            V_TYPE: v_type,

            PartyName: $(`#TxtPartyName${rowNo}`).val() || "",
            PartyCode: $(`#TxtPartyCode${rowNo}`).val() || "",
            BENo: $(`#TxtBENo${rowNo}`).val() || ""
        };

        if (v_type === "IMPORT") {

            data.SaudaNo = $(`#TxtSaudaNo${rowNo}`).val() || "";
            data.SaudaDate = $(`#TxtSaudaDate${rowNo}`).val() || "";
            data.EximNo = $(`#TxtEximNo${rowNo}`).val() || "";
            data.EximDate = $(`#TxtEximDate${rowNo}`).val() || "";
            data.PICopy = $(`#TxtPICopy${rowNo}`).val() || "";
            data.PIPath = $(`#TxtPIPath${rowNo}`).val() || "";
            data.BLCopy = $(`#TxtBLCopy${rowNo}`).val() || "";
            data.BLPath = $(`#TxtBLPath${rowNo}`).val() || "";
            data.BECopy = $(`#TxtBECopy${rowNo}`).val() || "";
            data.BEPath = $(`#TxtBEPath${rowNo}`).val() || "";
            data.LCCopy = $(`#TxtLCCopy${rowNo}`).val() || "";
            data.LCPath = $(`#TxtLCPath${rowNo}`).val() || "";
            data.INVCopy = $(`#TxtINVCopy${rowNo}`).val() || "";
            data.INVPath = $(`#TxtINVPath${rowNo}`).val() || "";
            data.DPCopy = $(`#TxtDPCopy${rowNo}`).val() || "";
            data.DPPath = $(`#TxtDPPath${rowNo}`).val() || "";
            data.SBLCCopy = $(`#TxtSBLCCopy${rowNo}`).val() || "";
            data.SBLCPath = $(`#TxtSBLCPath${rowNo}`).val() || "";
            data.OthCopy1 = $(`#TxtOthCopy1${rowNo}`).val() || "";
            data.OthPath1 = $(`#TxtOthPath${rowNo}`).val() || "";
            data.OthCopy2 = $(`#TxtOthCopy2${rowNo}`).val() || "";
            data.OthPath2 = $(`#TxtOthPath2${rowNo}`).val() || "";
        }

        else if (v_type === "EXPORT") {

            data.InvNo = $(`#TxtInvNo${rowNo}`).val() || "";
            data.InvDate = $(`#TxtInvDate${rowNo}`).val() || "";

            data.EximNo = $(`#TxtEximNo${rowNo}`).val() || "";
            data.EximDate = $(`#TxtEximDate${rowNo}`).val() || "";

            data.SBNo = $(`#TxtSBNo${rowNo}`).val() || "";

            data.SBLCCopy = $(`#TxtSBLCCopy${rowNo}`).val() || "";
            data.SBLCPath = $(`#TxtSBLCPath${rowNo}`).val() || "";

            data.BLCopy = $(`#TxtBLCopy${rowNo}`).val() || "";
            data.BLPath = $(`#TxtBLPath${rowNo}`).val() || "";

            data.BRCCopy = $(`#TxtBRCCopy${rowNo}`).val() || "";
            data.BRCPath = $(`#TxtBRCPath${rowNo}`).val() || "";

            data.OthCopy1 = $(`#TxtOthCopy1${rowNo}`).val() || "";
            data.OthPath1 = $(`#TxtOthPath1${rowNo}`).val() || "";

            data.OthCopy2 = $(`#TxtOthCopy2${rowNo}`).val() || "";
            data.OthPath2 = $(`#TxtOthPath2${rowNo}`).val() || "";

            data.OthCopy3 = $(`#TxtOthCopy3${rowNo}`).val() || "";
            data.OthPath3 = $(`#TxtOthPath3${rowNo}`).val() || "";

            data.OthCopy4 = $(`#TxtOthCopy4${rowNo}`).val() || "";
            data.OthPath4 = $(`#TxtOthPath4${rowNo}`).val() || "";

            data.OthCopy5 = $(`#TxtOthCopy5${rowNo}`).val() || "";
            data.OthPath5 = $(`#TxtOthPath5${rowNo}`).val() || "";

            data.OthCopy6 = $(`#TxtOthCopy6${rowNo}`).val() || "";
            data.OthPath6 = $(`#TxtOthPath6${rowNo}`).val() || "";

            data.OthCopy7 = $(`#TxtOthCopy7${rowNo}`).val() || "";
            data.OthPath7 = $(`#TxtOthPath7${rowNo}`).val() || "";
        }

        selectedData.push(data);
    });

    return selectedData;
}



