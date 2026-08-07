
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

function addItemRecordRow(item = null) {

    let tbody = $('#tblImportExportDocAttachmentList tbody');

    // Count only actual data rows
    let rowCount = tbody.find('tr[id^="row"]').length + 1;

    let newRow = `
        <tr id="row${rowCount}" class="no-border-input">

            <!-- 0. Code -->
            <td class="hidden-col">  <input type="hidden" id="TxtCode${rowCount}" value="${item?.code ?? ""}" /> </td>

            <!-- 1. Sauda No -->
            <td>
                <input type="text"
                       id="TxtSaudaNo${rowCount}"
                       class="erppagetable-control"
                       value="${item?.saudA_NO ?? ""}" />
            </td>

            <!-- 2. Sauda Date -->
            <td>
                <input type="text"
                       id="TxtSaudaDate${rowCount}"
                       class="erppagetable-control"
                       value="${item?.sauda_Date ?? ""}" />
            </td>

            <!-- 3. Inv No -->
            <td>
                <input type="text"
                       id="TxtInvNo${rowCount}"
                       class="erppagetable-control"
                       value="${item?.v_NO ?? ""}" />
            </td>

            <!-- 4. Inv Date -->
            <td>
                <input type="text"
                       id="TxtInvDate${rowCount}"
                       class="erppagetable-control"
                       value="${item?.invDate ?? ""}" />
            </td>

            <!-- 5. B E No. -->
            <td>
                <input type="text"
                       id="TxtBENo${rowCount}"
                       class="erppagetable-control"
                       value="${item?.bE_NO ?? ""}" />
            </td>

            <!-- 6. Exim No -->
            <td>
                <input type="text"
                       id="TxtEximNo${rowCount}"
                       class="erppagetable-control"
                       value="${item?.eximNo ?? ""}" />
            </td>

            <!-- 7. Exim Date -->
            <td>
                <input type="text"
                       id="TxtEximDate${rowCount}"
                       class="erppagetable-control"
                       value="${item?.eximDate ?? ""}" />
            </td>

            <!-- 8. Party Name -->
            <td>
                <input type="text"
                       id="TxtPartyName${rowCount}"
                       class="erppagetable-control"
                       value="${item?.partyName ?? ""}" />
            </td>

            <!-- 9. Party Code -->
            <td>
                <input type="text"
                       id="TxtPartyCode${rowCount}"
                       class="erppagetable-control"
                       value="${item?.partY_CODE ?? ""}" />
            </td>

            <!-- 10. Checkbox -->
            <td class="text-center">
                <input type="checkbox"
                       id="Chk${rowCount}" />
            </td>

            <!-- 11. P I Copy -->
            <td>
                <input type="text"
                       id="TxtPICopy${rowCount}"
                       class="erppagetable-control"
                       value="${item?.piCopy ?? ""}" />
            </td>

            <!-- 12. B L Copy -->
            <td>
                <input type="text"
                       id="TxtBLCopy${rowCount}"
                       class="erppagetable-control"
                       value="${item?.blCopy ?? ""}" />
            </td>

            <!-- 13. B E Copy -->
            <td>
                <input type="text"
                       id="TxtBECopy${rowCount}"
                       class="erppagetable-control"
                       value="${item?.beCopy ?? ""}" />
            </td>

            <!-- 14. L C Copy -->
            <td>
                <input type="text"
                       id="TxtLCCopy${rowCount}"
                       class="erppagetable-control"
                       value="${item?.lcCopy ?? ""}" />
            </td>

            <!-- 15. INV Copy -->
            <td>
                <input type="text"
                       id="TxtINVCopy${rowCount}"
                       class="erppagetable-control"
                       value="${item?.invCopy ?? ""}" />
            </td>

            <!-- 16. D P Copy -->
            <td>
                <input type="text"
                       id="TxtDPCopy${rowCount}"
                       class="erppagetable-control"
                       value="${item?.dpCopy ?? ""}" />
            </td>

            <!-- 17. SBLC Copy -->
            <td>
                <input type="text"
                       id="TxtSBLCCopy${rowCount}"
                       class="erppagetable-control"
                       value="${item?.sblcCopy ?? ""}" />
            </td>

            <!-- 18. Oth Copy1 -->
            <td>
                <input type="text"
                       id="TxtOthCopy1${rowCount}"
                       class="erppagetable-control"
                       value="${item?.othCopy1 ?? ""}" />
            </td>

            <!-- 19. Oth Copy2 -->
            <td>
                <input type="text"
                       id="TxtOthCopy2${rowCount}"
                       class="erppagetable-control"
                       value="${item?.othCopy2 ?? ""}" />
            </td>

            <!-- 20. Oth Copy3 -->
            <td>
                <input type="text"
                       id="TxtOthCopy3${rowCount}"
                       class="erppagetable-control"
                       value="${item?.othCopy3 ?? ""}" />
            </td>

            <!-- 21. Oth Copy4 -->
            <td>
                <input type="text"
                       id="TxtOthCopy4${rowCount}"
                       class="erppagetable-control"
                       value="${item?.othCopy4 ?? ""}" />
            </td>

            <!-- 22. Oth Copy5 -->
            <td>
                <input type="text"
                       id="TxtOthCopy5${rowCount}"
                       class="erppagetable-control"
                       value="${item?.othCopy5 ?? ""}" />
            </td>

            <!-- 23. Oth Copy6 -->
            <td>
                <input type="text"
                       id="TxtOthCopy6${rowCount}"
                       class="erppagetable-control"
                       value="${item?.othCopy6 ?? ""}" />
            </td>

            <!-- 24. Oth Copy7 -->
            <td>
                <input type="text"
                       id="TxtOthCopy7${rowCount}"
                       class="erppagetable-control"
                       value="${item?.othCopy7 ?? ""}" />
            </td>

        </tr>
    `;

    tbody.append(newRow);
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

        console.log(res);

        const tbody = $('#tblImportExportDocAttachmentList tbody');

        tbody.empty();

        if (!res.success || !res.data || res.data.length === 0) {

            tbody.append(` <tr class="no-record-row"> <td colspan="25" class="text-center">  No Record Found  </td>  </tr>  `);

            return;
        }

        // Create rows according to table structure
        $.each(res.data, function (index, item)
        {
            addItemRecordRow(item);

        });

    }
    catch (error) {

        console.error('Viewdata Error:', error);

        toastr.error('Error loading data.');
    }
}