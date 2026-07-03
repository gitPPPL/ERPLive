

async function LoadDropdown() {
    try {
        await Promise.allSettled([
            DDlPartyList(),
            DDlShipPartyList(),
            GetCurrencyList(),
            GetPayTermList(),
            DDLCityMast(),
            GetPlaceList(),
            DDLTxtCity1SDt()
        ]);

        addItemRecordRow();
        addAttachmentRow();

    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function GetShipFromList(ShipCode) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPartyList',
            type: 'GET',
            dataType: 'json'
        });

        console.log("GetShipFromList", response);

    } catch (error) {
        console.log(error);
        toastr.error("Party Load failed");
    }
}

async function GetPartyList(selectedValue = null) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPartyList',
            type: 'GET',
            data: { selectedValue: selectedValue },
            dataType: 'json'
        });

        console.log("GetPartyList", response);

    } catch (error) {
        console.log(error);
        toastr.error("Party Load failed");
    }
}

async function GetPayTermList(selectedValue = null) {
    try {
        const response = await $.ajax({
            url: '/PurchaseOrder/GetPayTermList',
            type: 'GET',
            dataType: 'json'
        });

        if (response && response.status) {

            const $dropdown = $('#ddlPaymentTerm');
            $dropdown.empty();

            $dropdown.append('<option value="">- Select Payment Term -</option>');

            $.each(response.data, function (i, item) {
                $dropdown.append(new Option(item.NAME, item.CODE));
            });

            $dropdown.val(selectedValue || '').trigger('change');

        } else {
            toastr.error("Payment term load failed");
        }

    } catch (error) {
        console.log(error);
        toastr.error("Payment term load failed");
    }
}

async function DDLCityMast() {
    try {
        const res = await fetch('/PurchaseOrder/DDLCityMast');
        const data = await res.json();
        const ddl = $('#TxtCity1PD');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}

async function DDlPartyList() {
    try {
        const res = await fetch('/PurchaseOrder/DDlPartyList');
        const data = await res.json();

        const ddl = $('#ddlPartyName');

        // clear old options
        ddl.empty();

        // default option
        ddl.append('<option value=""></option>');


        data.forEach(item => {
            ddl.append(new Option(item.text, item.value));
        });

        // initialize / refresh select2
        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.trigger('change'); // refresh
        } else {
            ddl.select2({
                placeholder: "-- Select Party Name --",
                allowClear: true,
                width: '100%'
            });
        }

    } catch (error) {
        console.error("Error loading Party:", error);
    }
}

async function DDlShipPartyList() {
    try {
        const res = await fetch('/PurchaseOrder/DDlPartyList');
        const data = await res.json();

        const ddl = $('#ddlShipFrom');

        // clear old options
        ddl.empty();

        // default option
        ddl.append('<option value=""></option>');

        // add new options
        data.forEach(item => {
            ddl.append(new Option(item.text, item.value));
        });

        // initialize / refresh select2
        if (ddl.hasClass("select2-hidden-accessible")) {
            ddl.trigger('change'); // refresh
        } else {
            ddl.select2({
                placeholder: "-- Select Ship Party Name --",
                allowClear: true,
                width: '100%'
            });
        }

    } catch (error) {
        console.error("Error loading Party:", error);
    }
}

async function GetCurrencyList() {
    try {
        const res = await fetch('/PurchaseOrder/GetCurrencyMast');
        const data = await res.json();
        const ddl = $('#ddlCurrency');
        ddl.empty().append('<option value="">-- Select Currency --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Currency:", error);
    }
}

async function GetPlaceList() {
    try {
        const res = await fetch('/PurchaseOrder/GetPlaceMast');
        const data = await res.json();
        const ddl = $('#ddlPlace');
        ddl.empty().append('<option value="">-- Select Place --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Place:", error);
    }
}

async function DDLTxtCity1SDt() {
    try {
        const res = await fetch('/PurchaseOrder/DDLCityMast');
        const data = await res.json();
        const ddl = $('#TxtCity1SD');
        ddl.empty().append('<option value="">-- Select City Name --</option>');
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City:", error);
    }
}


async function GetSaudanoList(partyCd) {
    try {
        const data = await $.ajax({
            url: '/PurchaseOrder/GetSaudaList',
            method: 'GET',
            data: { partyCd: partyCd }
        });

        const ddl = $('#ddSaudaNo');
        ddl.empty().append('');

        data.forEach(item => {
            ddl.append(`<option value="${item.text}">${item.value}</option>`);
        });

    } catch (error) {
        console.error("Error loading Sauda No:", error);
    }
}

async function loadModificationdata() {

    try {
        const V_NO = $('#txtDocNo').val()?.trim();
        const V_date = $('#dtDocDate').val()?.trim();

        if (!V_NO) {
            toastr.info("Please enter document number");
            return;
        }

        const res = await $.ajax({
            url: '/PurchaseOrder/GetModificationData',
            method: 'GET',
            data: { V_NO: V_NO }
        });

        console.log("Full response:", res);

        if (res.success) {

            const data = res.data || [];

            if (!data || data.length === 0) {
                toastr.info("Purchase Modification Not Found For this Doc No = " + V_NO + " and Doc Date " + V_date);
                return;
            }
            renderPurchaseModification(data);
        }

    } catch (err) {
        console.error("AJAX Error:", err);
        toastr.info("Something went wrong while fetching data");
    }
}

function renderPurchaseModification(data) {
    console.log("Rendering Purchase Modification Data:", data);
    const tbody = $('#modificationList tbody');
    tbody.empty();

    if (!data || data.length === 0) {
        tbody.append(`
            <tr>
                <td colspan="4" class="text-center">No data found</td>
            </tr>
        `);
    } else {
        data.forEach(item => {
            tbody.append(`
                <tr>
                    <td>${item.saudaNo ?? ''}</td>
                    <td>${item.party ?? ''}</td>
                    <td>${item.itemName ?? ''}</td>
                    <td>${item.qty ?? ''}</td>
                    <td>${item.rate ?? ''}</td>
                    <td>${item.remark ?? ''}</td>
                    <td>${item.modifyDate ?? ''}</td>
                </tr>
            `);
        });
    }

    const modalElement = document.getElementById('modificationModal');
    const myModal = new bootstrap.Modal(modalElement);
    myModal.show();
}
