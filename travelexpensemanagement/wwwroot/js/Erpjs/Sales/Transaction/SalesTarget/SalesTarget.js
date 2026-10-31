function AddRow(data = {}) {

    let tbody = $('#tblSalesTaget tbody');

    let newRow = `
        <tr class="no-border-input">
            <td> <input class="erppagetable-control ID" value="${data.ID ?? ''}" readonly /> </td>
            <td> <select class="erppagetable-control ddlProductName">  <option value="">-- Select Product --</option>${ProductList} </select> </td>
            <td> <input type="number" class="erppagetable-control TxtLMS" value="${data.LastMonsale ?? ''}" />  </td>
            <td>  <input type="number" class="erppagetable-control TxtTargetQty" value="${data.TargetQty ?? ''}" />  </td>

            <td>  <select class="erppagetable-control TxtSalesThough"> <option value="">-- Select Sales Through --</option>  ${salesThroughList}  </select> </td>

            <td>   <input type="number" class="erppagetable-control TxtminPrice" value="${data.minPrice ?? ''}" />  </td>
            <td>  <input type="number"   class="erppagetable-control TxtMaxPrice" value="${data.MaxPrice ?? ''}" /> </td>
            <td>  <input type="number" class="erppagetable-control TxtAppPrice"  value="${data.AppPrice ?? ''}" /> </td>

            <td> <input type="number" class="erppagetable-control TxtVirginPrice" value="${data.VirginPrice ?? ''}" />  </td>

            <td>  <input type="text"  class="erppagetable-control TxtRemarks" value="${data.Remarks ?? ''}" /> </td>

            <td class="action-col">
            <button type="button" class="act-btn add"  onclick="AddRow()">   <i class="fa fa-plus-circle"></i>
                </button>

                <button type="button" class="act-btn delete"
                        onclick="DeleteRow(this)">
                    <i class="fa fa-trash"></i>
                </button>
            </td>
        </tr>
    `;

    tbody.append(newRow);

    let $row = tbody.find('tr:last');

    // Set selected dropdown values
    $row.find('.ddlProductName').val(data.Productcode ?? '');
    $row.find('.TxtSalesThough').val(data.salesThough ?? '');

    // Update item code when product changes
    $row.find('.ddlProductName').on('change', function () {
        const ItemCode = $(this).val();
        $row.find('.ID').val(ItemCode || '');
    });
}

function GetSalesTargetDetails() {
    const details = [];

    $("#tblSalesTaget tbody tr").each(function (index) {
        const row = $(this);

        const planMonth = $("#DtPalningMonthDate").val();

        details.push({
            V_TYPE: "STGT",

            V_NO: parseInt($("#NumDocumentNo").val(), 10) || null,
            V_DATE: $("#DtDocumentDate").val() || null,
            PLAN_MONTH: planMonth ? planMonth + "-01" : null,
            PARTY_CODE: parseInt($("#ddlPartyName").val(), 10) || null,
            DOC_ID: $("#TxtCode").val() || null,
            ITEM_CODE: parseInt(row.find(".ddlProductName").val(), 10) || null,
            SOLD_QTY: parseFloat(row.find(".TxtLMS").val()) || 0,
            TARGET_QTY: parseFloat(row.find(".TxtTargetQty").val()) || 0,
            salesThrough: parseInt(row.find(".TxtSalesThough").val(), 10) || null,
            MIN_PRICE: parseFloat(row.find(".TxtminPrice").val()) || 0,
            MAX_PRICE: parseFloat(row.find(".TxtMaxPrice").val()) || 0,
            APP_PRICE: parseFloat(row.find(".TxtAppPrice").val()) || 0,
            VIR_PRICE: parseFloat(row.find(".TxtVirginPrice").val()) || 0,
            REMARKS: row.find(".TxtRemarks").val() || "",
            SRNO: index + 1
        });
    });

    console.log("Sales Target Details:", details);

    return details;
}

async function LoadData() {
    try {
        const res = await $.ajax({
            url: '/SalesTargetList/GetDataByCode',
            type: 'POST',
            data: { DOC_ID: rowId }
        });
        await LoadDropdown();
        console.log("Selected docId Data", res);

        if (!res || !res.success || !res.data) {
            console.error("No data found");
            return;
        }
        // Header data
        const header = res.data.details?.[0] || {};

        $('#DtDocumentDate').val(formatDate(header.v_DATE));
        $('#NumDocumentNo').val(header.v_NO);
        $('#DtPalningMonthDate').val(
            header.plaN_MONTH ? header.plaN_MONTH.substring(0, 7) : ''
        );
        $('#ddlPartyName').val(header.partY_CODE || '').trigger('change');

        const Details = res.data.details || [];


        $('#tblSalesTaget tbody').empty();
        if (Array.isArray(Details) && Details.length > 0) {
            Details.forEach(item => {
                AddRow({
                    ID: item.iteM_CODE ?? '',
                    Productcode: item.iteM_CODE ?? '',
                    LastMonsale: item.solD_QTY ?? '',
                    TargetQty: item.targeT_QTY ?? '',
                    salesThough: item.salesThrough ?? '',
                    minPrice: item.miN_PRICE ?? '',
                    MaxPrice: item.maX_PRICE ?? '',
                    AppPrice: item.apP_PRICE ?? '',
                    VirginPrice: item.viR_PRICE ?? '',
                    Remarks: item.remarks ?? ''
                });
            });

        } else {
            console.log("No detail records found");
        }

    }
    catch (error) {
        console.error("Error loading data:", error);
    }
}
