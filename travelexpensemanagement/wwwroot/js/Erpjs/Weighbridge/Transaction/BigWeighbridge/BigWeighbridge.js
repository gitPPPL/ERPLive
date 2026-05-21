let gateType;
let gateNo;
const allFieldIds = [
    "DtDocDate", "ddlWBType", "TxtQRCode", "DdlGateNo", "ddlPartyName",
    "NumPartyGrossWeight", "NumTrWeight", "NumNetWeight", "TxtVehicleNo",
    "TxtPartyWBNo", "ChkCrystalReport", "txtRemarks", "NumNoBag25", "NumNoBag500", "NumNoBag1000"
];

const itemRecords = [
    "TxtWeight", "TxtDate", "TxtTime", "ddlItemname", "DdlFromPlace", "DdlToPlace"
];

let docId = "", readOnly;

function getQueryParam(param) {
    return new URLSearchParams(window.location.search).get(param);
}

$(document).ready(function () {
    initPage();
    $("#ddlDocType").focus();
});

async function initPage() {
    try {
        await GetDocTypeAsync();
        await handleDocLoad();
        $('#ddlDocType').on('change blur', function () {
            const selectedValue = $(this).val();
            if (selectedValue) {
                if (event.type === 'change') {
                    GetDocid(selectedValue);
                }
                // Disable dropdown when value is selected and focus leaves
                $(this).prop('disabled', true);
            }
        });

        // addItemRecordRow();
        if (!docId) {
            await GetPartyList();
            addItemRecordRow();
        }
        setEnterKeyFocus(allFieldIds);
        wireEvents();
    } catch (err) {
        toastr.error('Initialization failed: ' + err);
    }
}

function wireEvents() {

    $("#NumPartyGrossWeight, #NumTrWeight").on('input', updateNetWeight);

    $('#btn-save').on('click', async (e) => {
        e.preventDefault();

        if (!(await validateBigWeighbridgeForm())) return;

        const isValidDate = await checkValidDate();
        if (!isValidDate) return;

        try {
            const data = await collectFormData();
            console.log(data);
            docId ? UpdateData(data) : SaveData(data);
        } catch (err) {
            toastr.error("Error saving data: " + err);
        }
    });

    $('#ddlDocType').on('change', () => {
        const val = $('#ddlDocType').val();
        if (val) GetDocid(val);
    });

    $('#ddlWBType').on('change', function () {
        const val = $(this).val();

        if (!val) {
            $('#DdlGateNo').empty().append('<option value="">Select Gate No</option>');
            return;
        }

        GetGateEntryList(val);
    });

    $(document).on('click', '.btn-delete-action, .btn-Itemdelete-action', function () {
        const tbody = $(this).closest('tbody');
        if (tbody.find('tr').length > 1) {
            $(this).closest('tr').remove();
        } else {
            toastr.error('Cannot delete the first row.');
        }
    });

    $(document).on('click', '.btn-add-action , .btn-add-row', async function () {
        await addItemRecordRow();
    });

    $('#DdlGateNo').on('change', function () {
        const gateNo = $(this).val();
        const selectedOption = $(this).find('option:selected');

        if (gateNo) {
            // Fill Vehicle No
            $('#TxtVehicleNo').val(
                selectedOption.data('vehicalno') || ''
            );

            // Fill Party Name
            const partyCode = selectedOption.data('party');
            const partyName = selectedOption.data('partynm');

            ensureOption($('#ddlPartyName'), partyCode, partyName);
            $('#ddlPartyName').val(partyCode).trigger('change');

            // Disable both fields after auto-fill
            $('#ddlPartyName').prop('disabled', true);
            $('#TxtVehicleNo').prop('readonly', true);
        }
        else {
            // Clear values
            $('#TxtVehicleNo').val('');
            $('#ddlPartyName').val('').trigger('change');

            // Enable fields again
            $('#ddlPartyName').prop('disabled', false);
            $('#TxtVehicleNo').prop('readonly', false);
        }
    });

    function ensureOption($dropdown, code, name) {
        if (code && $dropdown.find(`option[value="${code}"]`).length === 0) {
            $dropdown.append(`<option value="${code}">${name}</option>`);
        }
    }

}

async function validateBigWeighbridgeForm() {

    const wbType = $('#ddlWBType').val()?.trim();
    const docType = $('#ddlDocType').val()?.trim();
    const gateNo = $('#DdlGateNo').val()?.trim();

    // HEADER VALIDATION
    if (!validateRequiredField('#ddlDocType', 'Doc Type')) return;
    if (!validateRequiredField('#NumDocNo', 'Doc Number')) return;
    if (!validateRequiredField('#DtDocDate', 'Doc Date')) return;
    if (!validateRequiredField('#ddlWBType', 'WB Type')) return;

    // GATE VALIDATION
    const gateRequiredTypes = ["Raw Material", "Store", "Sales Return", "Fuel", "Misc", "RGP", "Sales"];

    if (gateRequiredTypes.includes(wbType) && !gateNo) {
        setInvalid($('#DdlGateNo'), 'Gate Inward No is required.');
        return false;
    }

    if (!validateRequiredField('#ddlPartyName', 'Party Name')) return;
    if (!validateRequiredField('#TxtVehicleNo', 'Vehicle Number')) return;

    // KANT + MISC BLOCK RULE
    if (docType === "KANT" && wbType === "Misc") {
        showToast("Entry not allowed in Misc for Inward/Outward.", {
            type: "warning"
        });
        return false;
    }

    // GRID CHECK
    const rows = $('#tblBigWeighbridge tbody tr');

    let hasAnyData = false;

    rows.each(function () {
        const $row = $(this);

        const weight = parseDecimalSafe(
            $row.find("input[id^='TxtWeight']").val()
        ) || 0;

        const date = $row.find("input[id^='TxtDate']").val()?.trim();
        const time = $row.find("input[id^='TxtTime']").val()?.trim();
        const item = $row.find("select[id^='ddlItemname']").val();
        const fromPlace = $row.find("select[id^='DdlFromPlace']").val();
        const toPlace = $row.find("select[id^='DdlToPlace']").val();

        if (weight > 0 || time || item || fromPlace || toPlace) {
            hasAnyData = true;
            return false; // break loop
        }
    });

    if (!hasAnyData) {
        showToast("No Record in grid to save.", { type: "warning" });
        return false;
    }

    let validRowCount = 0;
    let weights = [];
    let firstWeight = null;

    // ROW VALIDATION
    for (let i = 0; i < rows.length; i++) {

        const $row = $(rows[i]);
        const rowNo = i + 1;
        // Get values from current row
        const weight = parseDecimalSafe(
            $row.find("input[id^='TxtWeight']").val()
        ) || 0;

        const date = $row.find("input[id^='TxtDate']").val()?.trim();
        const time = $row.find("input[id^='TxtTime']").val()?.trim();
        const item = $row.find("select[id^='ddlItemname']").val();
        const fromPlace = $row.find("select[id^='DdlFromPlace']").val();
        const toPlace = $row.find("select[id^='DdlToPlace']").val();

        // Count valid rows
        if (weight > 0) {
            validRowCount++;
            weights.push(weight);

            if (firstWeight === null) {
                firstWeight = weight;
            }
        }

        // =========================
        //  ROW 1 SPECIAL RULE
        // =========================
        if (rowNo === 1) {

            if (weight > 0) {

                if (!date) {
                    setInvalid($row.find("input[id^='TxtDate']"), "Date required in row 1");
                    return false;
                }

                if (!time) {
                    setInvalid($row.find("input[id^='TxtTime']"), "Time required in row 1");
                    return false;
                }
            }

            continue; 
        }

        // =========================
        // ROW 2+ FULL VALIDATION
        // =========================
        if (weight > 0) {

            if (!date) {
                setInvalid($row.find("input[id^='TxtDate']"), `Date is required in row ${rowNo}`);
                return false;
            }

            if (!time) {
                setInvalid($row.find("input[id^='TxtTime']"), `Time is required in row ${rowNo}`);
                return false;
            }

            if (!item) {
                setInvalid($row.find("select[id^='ddlItemname']"), `Item Name is required in row ${rowNo}`);
                return false;
            }

            if (!fromPlace) {
                setInvalid($row.find("select[id^='DdlFromPlace']"), `From Place is required in row ${rowNo}`);
                return false;
            }

            if (!toPlace) {
                setInvalid($row.find("select[id^='DdlToPlace']"), `To Place is required in row ${rowNo}`);
                return false;
            }
        }
    }

    // NO VALID DATA CHECK
    if (validRowCount === 0) {
        const $firstWeightInput = $(rows[0]).find("input[id^='TxtWeight']");
        setInvalid($firstWeightInput, "Weight should not be blank or 0.");
        return false;
    }

    // SINGLE ROW CHECK
    if (rows.length === 1 && firstWeight === 0) {
        const $weightInput = $(rows[0]).find('input');
        setInvalid($weightInput, 'Weight should not be blank or 0.');
        return false;
    }

    // WEIGHT ORDER VALIDATION
    if (weights.length >= 2) {

        // Sales → Ascending
        if (wbType === "Sales") {
            for (let i = 1; i < weights.length; i++) {
                if (weights[i] < weights[i - 1]) {
                    showToast(
                        "Weight must be in ASCENDING order for Sales.",
                        { type: "warning" }
                    );
                    return false;
                }
            }
        }

        // Raw Material / Store / Fuel / Sales Return → Descending
        if (["Raw Material", "Store", "Fuel", "Sales Return"].includes(wbType)) {
            for (let i = 1; i < weights.length; i++) {
                if (weights[i] > weights[i - 1]) {
                    showToast("Weight must be in DESCENDING order.", { type: "warning" });
                    return false;
                }
            }
        }

        // Outsider / Misc → Detect flow dynamically
        if (["Outsider", "Misc"].includes(wbType) && weights.length >= 3) {
            let flow = "";
            if (weights[1] < weights[0]) {
                flow = "DESC";
            } else if (weights[1] > weights[0]) {
                flow = "ASC";
            }

            for (let i = 2; i < weights.length; i++) {

                if (flow === "DESC" && weights[i] > weights[i - 1]) {
                    showToast("Invalid weight. Weight must be in DESCENDING order.", { type: "warning" });
                    return false;
                }

                if (flow === "ASC" && weights[i] < weights[i - 1]) {
                    showToast("Invalid weight. Weight must be in ASCENDING order.", { type: "warning" });
                    return false;
                }
            }
        }
    }

    async function showWeightConfirmation(weight) {
        const result = await Swal.fire({
            title: 'Invalid Weight',
            text: `Weight ${weight} Kg is less than 1000 Kg. Do you want to continue?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'Yes, Continue',
            cancelButtonText: 'No, Cancel',
            reverseButtons: true
        });

        return result.isConfirmed;
    }

    // Use inside validateBigWeighbridgeForm()
    for (let i = 0; i < weights.length; i++) {
        const weight = weights[i] || 0;

        if (weight > 0 && weight < 1000) {
            const proceed = await showWeightConfirmation(weight);

            if(!proceed) {
                return false;
            }
        }
    }

    // IMAGE VALIDATION (ASYNC)

    if (
        [1, 4].includes(pubCompCode) &&
        wbType === "Raw Material" &&
        $('#ddlStatus').val()?.toUpperCase() === "CLOSE"
    ) {
        const imgOk = await checkVehicleImages();

        if (!imgOk) {
            showToast(
                "Front and Back both Images of Vehicle not captured.",
                { type: "warning" }
            );
            return false;
        }
    }

    return true;
}

function updateNetWeight() {
    const g = parseFloat($("#NumPartyGrossWeight").val()) || 0;
    const t = parseFloat($("#NumTrWeight").val()) || 0;
    $("#NumNetWeight").val((g - t).toFixed(2));
}

async function handleDocLoad() {
    docId = getQueryParam('id');
    readOnly = getQueryParam('readOnly');
    if (docId) {
        $('#ddlDocType').prop('disabled', true);
        await GetDocData(docId, readOnly);

        var wbType = $('#ddlWBType').val();
        if (wbType === "Outsider" || wbType === "Sales Return") {
            $('#ddlPartyName').prop('disabled', false);
            $('#TxtVehicleNo').prop('readOnly', false);
            // var partyCode=$('#ddlPartyName').val();
            const partyCode = $('#ddlPartyName').val();
            await GetPartyList(partyCode);
        } else {
            $('#ddlPartyName').prop('disabled', true);
            $('#TxtVehicleNo').prop('readOnly', true);
        }

    } else {
        $('#ddlDocType').prop('selectedIndex', 0);
        const Vtype = $('#ddlDocType').val();
        if (Vtype) {
            GetDocid(Vtype);
        }
         //GetGateEntryList();
        //const wbType = $('#ddlWBType').val();
        //GetGateEntryList(wbType);

        const today = new Date();
        const todayDate = today.getFullYear() + '-' +
            (today.getMonth() + 1).toString().padStart(2, '0') + '-' +
            today.getDate().toString().padStart(2, '0');
        $('#DtDocDate').val(todayDate);

        const now = new Date();
        const yyyy = now.getFullYear();
        const mm = String(now.getMonth() + 1).padStart(2, '0');
        const dd = String(now.getDate()).padStart(2, '0');
        const hh = String(now.getHours()).padStart(2, '0');
        const min = String(now.getMinutes()).padStart(2, '0');
        const formattedDateTime = `${yyyy}-${mm}-${dd}T${hh}:${min}`;
        $('#DtStatusDate').val(formattedDateTime);

    }
}

function SaveData(saveDt) {
    $.ajax({
        url: '/BigWeighbridge/SaveOrUpdateWeighBridgeEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(saveDt),
        success: function (response) {
            if (response?.status) {
                toastr.success("Data Insert successfully");
                $('#btn-save').hide();
            } else {
                toastr.error(response?.message || "Save failed. Please try again.");
            }
        },
        error: function () {
            toastr.error("Error occurred while saving. Please contact admin.");
        }
    });
}

function UpdateData(UpdateDt) {
    $.ajax({
        url: '/BigWeighbridge/SaveOrUpdateWeighBridgeEntry',
        type: 'POST',
        contentType: 'application/json',
        data: JSON.stringify(UpdateDt),
        dataType: 'json',
        success: function (response) {
            if (response?.status) {
                console.log("Update", response);
                toastr.success("Data Update successfully");
                $('#btn-save').hide();
            } else {
                toastr.error("Update failed: " + (response?.message || "Unknown error."));
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Data not updated: " + error);
        }
    });
}

async function fillHeaderData(headdata) {
    if (!Array.isArray(headdata) || headdata.length === 0) {
        toastr.error("No header data found.");
        return;
    }

    const data = headdata[0];

    $("#TxtDocId").val(data.DOC_ID ?? "");
    $("#ddlDocType").val(data.V_TYPE ?? "");
    $("#NumDocNo").val(data.V_NO ?? "");
    $("#DtDocDate").val((data.V_DATE || '').substring(0, 10));
    $("#ddlWBType").val(data.WB_TYPE ?? "").trigger('change');
    $("#TxtVehicleNo").val(data.VEHICLE_NO ?? "");
    $("#txtRemarks").val(data.REMARKS ?? "");
    $("#NumNetWeight").val(data.PARTY_QTY ?? "");
    $("#NumPartyGrossWeight").val(data.PARTY_GROSSWT ?? "");
    $("#NumTrWeight").val(data.PARTY_TRWT ?? "");
    $("#TxtPartyWBNo").val(data.PARTY_WBNO ?? "");
    $("#NumNoBag25").val(data.SMALL_BAG ?? "");
    $("#NumNoBag500").val(data.MEDIUM_BAG ?? "");
    $("#NumNoBag1000").val(data.LARGE_BAG ?? "");

    $('#ddlStatus').val(data.STATUS ?? "").trigger('change');
    $("#DtStatusDate").val(formatDateTimeLocal(data.STATUS_DATE));
    const wbType = data.WB_TYPE ?? "";

    // Gate No bind
    GetGateEntryList(wbType, data.GATE_NO);

    gateType = data.GATE_TYPE;
    gateNo = data.GATE_NO;

    // Party Name bind
    if (wbType === "Outsider" || wbType === "Sales Return") {
        $('#ddlPartyName').prop('disabled', false);
        await GetPartyList(data.PARTY_CODE);
    }
    else {
        bindDropdownValue(
            '#ddlPartyName',
            data.PARTY_CODE,
            data.partyname
        );
    }
}

function bindDropdownValue(selector, value, text = null) {
    const $el = $(selector);
    const valStr = String(value ?? '');
    if (!$el.length) {
        console.warn(`Dropdown ${selector} not found`);
        return;
    }
    const optionExists = $el.find("option").filter(function () {
        return $(this).val() == valStr;
    }).length > 0;
    if (!optionExists) {
        const displayText = text ?? valStr;
        $el.append(new Option(displayText, valStr));
        console.log(`Appended missing option to ${selector}: [${valStr}] ${displayText}`);
    }
    $el.val(valStr).trigger('change');
}

async function collectFormData() {

    const { items: WB2Items, finalWeight } = await collectItemsDetail();
    const selectedOption = $('#DdlGateNo option:selected');
    const vtype = selectedOption.data('vtype');

    return {
        DOC_ID: toNullableString(document.getElementById("TxtDocId")?.value),
        V_TYPE: toNullableString($("#ddlDocType").val()),
        V_NO: parseIntSafe($("#NumDocNo").val()),
        V_DATE: toNullableDate(document.getElementById("DtDocDate")?.value),
        V_SHIFT: null,
        WB_TYPE: toNullableString($("#ddlWBType").val()),
        GATE_TYPE: vtype,
        GATE_NO: parseIntSafe($("#DdlGateNo").val()),
        PARTY_QTY: parseDecimalSafe($("#NumNetWeight").val()),
        PARTY_CODE: parseIntSafe($("#ddlPartyName").val()) ||null,
        GROSS_NO: null,
        TARE_NO: null,
        VEHICLE_NO: toNullableString($("#TxtVehicleNo").val()),
        REMARKS: toNullableString($("#txtRemarks").val()),
        STATUS: parseIntSafe($('#ddlStatus').val()),
        NET_WGT: finalWeight,
        FINAL_TYPE: null,
        FINAL_REM: null,
        PARTY_GROSSWT: parseDecimalSafe($("#NumPartyGrossWeight").val()),
        PARTY_TRWT: parseDecimalSafe($("#NumTrWeight").val()),
        PARTY_WBNO: toNullableString($("#TxtPartyWBNo").val()),
        SMALL_BAG: toNullableString($("#NumNoBag25").val()),
        MEDIUM_BAG: toNullableString($("#NumNoBag500").val()),
        LARGE_BAG: toNullableString($("#NumNoBag1000").val()),
        SaveOrUpdate: (!docId || docId === "") ? "Save" : "Update",
        WB2Data: WB2Items,

        oldGateType: gateType,
        oldGateNo: gateNo
    };
}

async function collectItemsDetail() {
    const items = [];
    let previousWeight = 0;
    let firstWeight = null;
    let lastWeight = null;

    $('#tblBigWeighbridge tbody tr').each(function (index) {
        const idx = this.id.replace('row', '');
        const $r = $(this);

        const currentWeight = parseDecimalSafe($r.find(`#TxtWeight${idx}`).val());
        let netWeight = 0;

        if (index === 0) {
            firstWeight = currentWeight;
        } else {
            netWeight = currentWeight - previousWeight;
        }

        previousWeight = currentWeight;
        lastWeight = currentWeight;

        items.push({
            SNO: parseIntSafe(idx),
            WEIGHT: currentWeight,
            WGT_DATE: toNullableDate($r.find(`#TxtDate${idx}`).val()),
            WGT_TIME: toNullableString($r.find(`#TxtTime${idx}`).val()),
            ITEM_CODE: parseIntSafe($r.find(`#ddlItemname${idx}`).val()),
            ITEM_NAME: $r.find(`#ddlItemname${idx}`).val() ? $r.find(`#ddlItemname${idx} option:selected`).text() : null,
            FROM_PLACE: parseIntSafe($r.find(`#DdlFromPlace${idx}`).val()),
            TO_PLACE: parseIntSafe($r.find(`#DdlToPlace${idx}`).val()),
            V_SHIFT: null,
            TYPE: null,
            TARE_WGT: null,
            NET_WGT: netWeight,
            FROM_NAME: $r.find(`#DdlFromPlace${idx}`).val() ? $r.find(`#DdlFromPlace${idx} option:selected`).text() : null,
            TO_NAME: $r.find(`#DdlToPlace${idx}`).val() ? $r.find(`#DdlToPlace${idx} option:selected`).text() : null,
            REMARKS: null,
            STATUS: null,
            Ref_type: null,
            Ref_no: null,
            wb_time: null,
            COND: null,
            MOIS_PER: null,
            MOIS_WT: null
        });
    });

    const finalWeight = firstWeight !== null && lastWeight !== null
        ? lastWeight - firstWeight
        : 0;

    return {
        items,
        finalWeight
    };
}

async function fillItemDetailTable(itemsData) {
    const $tbody = $('#tblBigWeighbridge tbody');
    $tbody.empty();

    if (!itemsData || itemsData.length === 0) {
        console.log("No detail records found.");
        return;
    }

    for (let index = 0; index < itemsData.length; index++) {
        const item = itemsData[index];
        const idx = index + 1;

        // Step 1: Create row
        addItemRecordRow();

        // Step 2: Wait for dropdowns to load
        await bindDropdownData(idx);

        // Step 3: Bind text fields
        $(`#TxtWeight${idx}`).val(item.WEIGHT ?? "");
        $(`#TxtDate${idx}`).val(formatDateOnly(item.WGT_DATE));


        $(`#TxtTime${idx}`).val(
            convertTo24HourFormat(item.WGT_TIME ?? "")
        );

        // Step 4: Bind dropdown values
        $(`#ddlItemname${idx}`).val(item.ITEM_CODE ?? "").trigger('change');
        $(`#DdlFromPlace${idx}`).val(item.FROM_PLACE ?? "").trigger('change');
        $(`#DdlToPlace${idx}`).val(item.TO_PLACE ?? "").trigger('change');
    }
}

function GetDocTypeAsync(selectedValue) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/BigWeighbridge/GetDocType',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    const $dropdown = $('#ddlDocType');
                    $dropdown.empty();
                    $.each(response.data, function (index, item) {
                        $dropdown.append(`<option value="${item.CODE}">${item.NAME}</option>`);
                    });

                    if (selectedValue && $dropdown.find(`option[value="${selectedValue}"]`).length > 0) {
                        $dropdown.val(selectedValue).trigger('change');
                    } else {
                        $dropdown.prop('selectedIndex', 0);
                    }

                    resolve();
                } else {
                    reject("Invalid response status.");
                }
            },
            error: function (xhr, status, error) {
                toastr.error("Document Type Load failed: " + error);
                reject(error);
            }
        });
    });
}

function GetDocid(VType) {
    $.ajax({
        url: '/BigWeighbridge/GetMaxVNo',
        type: 'GET',
        data: { V_type: VType },
        success: function (response) {
            if (response.status === true && response.data) {
                $('#NumDocNo').val(response.data.vNo || '');
                $('#TxtDocId').val(response.data.docId || '');
            } else {
                $('#txtDocNo').val('');
                $('#TxtDocId').val('');
            }
        },
        error: function (xhr, status, error) {
            toastr.error('Error fetching Doc ID:', error);
        }
    });
}

function GetGateEntryList(wbType = null, selectedValue = null) {
    $.ajax({
        url: '/BigWeighbridge/GetgateNo',
        type: 'GET',
        dataType: 'json',
        data: { wbType: wbType },
        success: function (response) {
            if (response.status) {
                console.log("Gate Node :", response);
                const $DropdownId = $('#DdlGateNo');
                $DropdownId.empty();
                $DropdownId.append('<option value="">- Select Gate No -</option>');

                $.each(response.data, function (index, item) {
                    $DropdownId.append(`
                            <option
                                data-VType="${item.V_TYPE}"
                                data-VehicalNo="${item.TRUCK_NO}"
                                data-party="${item.PARTY_CODE}"
                                data-partynm="${item.partyName}"
                                value="${item.V_NO}">
                                ${item.V_NO} || ${item.V_TYPE} || ${item.TRUCK_NO}
                            </option>
                        `);
                });

                $DropdownId.select2({
                    placeholder: "- Select -",
                    allowClear: true,
                    emplateResult: function (data) {
                        return data.text;
                    },

                    // Select hone ke baad sirf value (Gate No) dikhana
                    templateSelection: function (data) {
                        // Placeholder show karne ke liye
                        if (!data.id) {
                            return data.text;
                        }

                        // Selected hone par sirf value show karega
                        return data.id;
                    }
                });

                $DropdownId.on('select2:open', function () {
                    setTimeout(function () {
                        let searchBox = document.querySelector(
                            '.select2-container--open .select2-search__field'
                        );

                        if (searchBox) {
                            searchBox.focus();
                        }
                    }, 0);
                });

                if (selectedValue && $DropdownId.find(`option[value="${selectedValue}"]`).length > 0) {
                    $DropdownId.val(selectedValue).trigger('change');
                }
                else {
                    $DropdownId.val('').trigger('change');
                }

            } else {
                toastr.error("Gate No. Load failed");
            }
        },
        error: function (xhr, status, error) {
            toastr.error("Gate No. Load failed", error);
        }
    });
}

function GetPartyList(selectedValue = null) {
    return new Promise((resolve, reject) => {
        $.ajax({
            url: '/BigWeighbridge/GetPartyList',
            type: 'GET',
            dataType: 'json',
            success: function (response) {
                if (response.status) {
                    const $DropdownId = $('#ddlPartyName');
                    $DropdownId.empty();
                    $DropdownId.append('<option value="">- Select Party Name -</option>');

                    $.each(response.data, function (index, item) {
                        $DropdownId.append(
                            `<option value="${item.CODE}">${item.NAME}</option>`
                        );
                    });

                    $DropdownId.select2({
                        placeholder: "- Select -",
                        allowClear: true
                    });

                    $DropdownId.on('select2:open', function () {
                        setTimeout(function () {
                            let searchBox = document.querySelector(
                                '.select2-container--open .select2-search__field'
                            );

                            if (searchBox) {
                                searchBox.focus();
                            }
                        }, 0);
                    });

                    if (selectedValue &&
                        $DropdownId.find(`option[value="${selectedValue}"]`).length > 0) {
                        $DropdownId.val(selectedValue).trigger('change');
                    }

                    resolve();
                } else {
                    toastr.error("Party Name Load failed");
                    reject();
                }
            },
            error: function (xhr, status, error) {
                toastr.error("Party Name Load failed: " + error);
                reject(error);
            }
        });
    });
}

function formatDate(dateStr) {
    if (!dateStr) return '';
    const d = new Date(dateStr);
    return d.toISOString().split('T')[0];
}

function parseIntSafe(value) {
    const parsed = parseInt(value, 10);
    return isNaN(parsed) ? null : parsed;
}

function toNullableDate(val) {
    const date = new Date(val);
    return isNaN(date.getTime()) ? null : val;
}

function toNullableString(val) {
    return val?.trim() || null;
}

function allowOnlyDecimal(input) {
    input.value = input.value
        .replace(/[^0-9.]/g, '')
        .replace(/(\..*)\./g, '$1');
}

function parseDecimalSafe(val) {
    const num = parseFloat(val);
    return isNaN(num) ? null : num;
}

function setEnterKeyFocus(sequence) {
    sequence.forEach((id, index) => {
        $(`#${id}`).on('keypress', function (e) {
            if (e.key === 'Enter') {
                e.preventDefault();
                if (index + 1 < sequence.length) {
                    $(`#${sequence[index + 1]}`).focus();
                }
            }
        });
    });
}

async function GetDocData(MasterTblId, readOnly) {
    try {
        const response = await $.ajax({
            url: '/BigWeighbridge/GetWeighBridgeById',
            type: 'GET',
            data: { id: MasterTblId }
        });
        if (response.status) {
            await fillHeaderData(response.header);
            await fillItemDetailTable(response.detail);
            if (readOnly === 'true') {
                $('#btn-save, #cancelBtn').hide();
                disableAllFields();
            } else {
                $('#btn-save, #cancelBtn').show();
                enableAllFields();
            }
        } else {
            toastr.error('No data returned.');
        }
    } catch (error) {
        toastr.error('Failed to load data.');
        console.error(error);
    }
}

function enableAllFields() {
    allFieldIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.disabled = false;
    });
}

function disableAllFields() {
    allFieldIds.forEach(id => {
        const el = document.getElementById(id);
        if (el) el.disabled = true;
    });
}

function addItemRecordRow() {
    let tbody = $('#tblBigWeighbridge tbody');
    let rowCount = tbody.find('tr').length + 1;
    let newRow = `
        <tr class="no-border-input" id="row${rowCount}">
            <td><input type="text" style="min-width: 100px; max-width: 200px;" class="form-control" oninput="allowOnlyDecimal(this)" id="TxtWeight${rowCount}" /></td>
            <td><input type="date" style="min-width: 100px; max-width: 200px;" class="form-control"  id="TxtDate${rowCount}" /></td>
            <td><input type="time" style="min-width: 100px; max-width: 200px;" class="form-control"  id="TxtTime${rowCount}" /></td>
            <td>
                <select style="min-width: 500px;" class="form-control" id="ddlItemname${rowCount}">
                    <option value="">-select item Name-</option>
                </select>
            </td>
            <td>
                <select class="form-control" id="DdlFromPlace${rowCount}">
                    <option value="">Select From</option>
                </select>
                </td>
                <td>
                    <select class="form-control" id="DdlToPlace${rowCount}">
                        <option value="">Select To</option>
                    </select>
                </td>
                <td>
                 <i class="fa fa-plus btn-add-action text-success" title="Add Row"></i>
                <i class="fa fa-trash btn-delete-action btn-Itemdelete-action" title="Delete Row"></i>
            </td>
        </tr>
        `;
    tbody.append(newRow);
    // Auto-fill date and time when weight is entered
    $(`#TxtWeight${rowCount}`).on('input', function () {
        let currentDate = new Date();
        let yyyy = currentDate.getFullYear();
        let mm = String(currentDate.getMonth() + 1).padStart(2, '0');
        let dd = String(currentDate.getDate()).padStart(2, '0');
        let hours = String(currentDate.getHours()).padStart(2, '0');
        let minutes = String(currentDate.getMinutes()).padStart(2, '0');

        let formattedDateTime = `${yyyy}-${mm}-${dd}`;
        let formattedTime = `${hours}:${minutes}`;
        $(`#TxtDate${rowCount}`).val(formattedDateTime);
        $(`#TxtTime${rowCount}`).val(formattedTime);
        $(`#TxtDate${rowCount}`).prop('readonly', true);
        $(`#TxtTime${rowCount}`).prop('readonly', true);
    });

    bindDropdownData(rowCount);
    
}

function bindDropdownData(rowCount) {
    const itemSelect = $(`#ddlItemname${rowCount}`);
    const fromAddSelect = $(`#DdlFromPlace${rowCount}`);
    const toAddSelect = $(`#DdlToPlace${rowCount}`);

    const loadDropdown = (url, selectElem, defaultText, formatter) => {
        return new Promise((resolve, reject) => {
            $.ajax({
                url: url,
                type: 'GET',
                dataType: 'json',
                success: function (response) {
                    if (response.status) {
                        selectElem.empty().append(
                            $('<option>', {
                                value: '',
                                text: defaultText
                            })
                        );

                        $.each(response.data, function (i, item) {
                            selectElem.append(formatter(item));
                        });

                        selectElem.select2({
                            width: '300px',
                            placeholder: defaultText,
                            allowClear: true,
                            minimumResultsForSearch: 0
                        });

                        selectElem.on('select2:open', function () {
                            setTimeout(function () {
                                let searchBox = document.querySelector(
                                    '.select2-container--open .select2-search__field'
                                );

                                if (searchBox) {
                                    searchBox.focus();
                                }
                            }, 0);
                        });

                        resolve();
                    } else {
                        toastr.error(`${defaultText} load failed`);
                        resolve();
                    }
                },
                error: function (xhr, status, error) {
                    toastr.error(`Error loading ${defaultText}: ${error}`);
                    reject(error);
                }
            });
        });
    };

    return Promise.all([
        loadDropdown(
            '/BigWeighbridge/GetPlaceMast',
            fromAddSelect,
            "- Select From Place -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        ),
        loadDropdown(
            '/BigWeighbridge/GetPlaceMast',
            toAddSelect,
            "- Select To Place -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        ),
        loadDropdown(
            '/BigWeighbridge/GetItemList',
            itemSelect,
            "- Select Item -",
            item => `<option value="${item.CODE}">${item.NAME}</option>`
        )
    ]);
}

function convertTo24HourFormat(timeStr) {
    if (!timeStr) return '';

    if (/^\d{2}:\d{2}(:\d{2})?$/.test(timeStr)) {
        return timeStr.substring(0, 5);
    }

    const [time, modifier] = timeStr.split(' ');
    if (!time || !modifier) return '';

    let [hours, minutes] = time.split(':');

    if (modifier.toUpperCase() === 'PM' && hours !== '12') {
        hours = String(Number(hours) + 12);
    } else if (modifier.toUpperCase() === 'AM' && hours === '12') {
        hours = '00';
    }

    return `${hours.padStart(2, '0')}:${minutes.padStart(2, '0')}`;
}

function formatDateOnly(dateStr) {
    if (!dateStr) return '';
    return dateStr.split('T')[0];
}

function formatDateTimeLocal(dateStr) {
    if (!dateStr) return '';

    const d = new Date(dateStr);
    if (isNaN(d.getTime())) return '';

    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, '0');
    const dd = String(d.getDate()).padStart(2, '0');
    const hh = String(d.getHours()).padStart(2, '0');
    const min = String(d.getMinutes()).padStart(2, '0');

    return `${yyyy}-${mm}-${dd}T${hh}:${min}`;
}

async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: $("#ddlDocType").val(),
        vno: $("#NumDocNo").val()
    };

    try {

        const response = await fetch('/BigWeighbridge/CheckValidDate', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(data)
        });
        const result = await response.json();

        if (result.status === false) {
            showToast(result.message, { type: "warning" });
            return false;
        }
        return true;

    } catch (error) {
        console.error(error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}