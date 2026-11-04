
var globalVars = window.globalVariables || { };
let LoginDate = globalVars.LoginDate;
var controllerName = window.location.pathname.split('/')[1];

$(document).ready(async function () {
    SetFYDate('DtToDate', LoginDate);
    SetFYDate('DtFromDate', LoginDate);
    LoadDropdown();

    $('#btn_Report').on('click', async function () {
    try {

        TransitReport();

    } catch (error) {
        console.error("Error generating report:", error);
    }
});

});


async function LoadDropdown() {
    try {
        await Promise.all([
            DDlDocumentType(),
            DDlItemType(),
            OSGroup(),
            DDlTransport(),
            DDlACGroup(),
            DDlCountry(),
            DDlItem(),
            DDlPartyName()

        ]);
    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function DDlDocumentType() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlDocumentType');
    const data = await res.json();

        const ddl = $('#ddlforDocumentType');

    ddl.empty();
        ddl.append('<option value="">--- Select Document Type ---</option>');

        data.forEach(item => {
        ddl.append(
            `<option value="${item.value}">
                    ${item.value} - ${item.text}
                </option>`
        );
        });

    // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Document Type---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Document Type:", error);
    }
}
async function DDlItemType() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlItemType');
        const data = await res.json();
        const ddl = $('#ddlforItemType');
        ddl.empty().append('<option value="">---Select Item Type--</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading Item Type:", error);
    }
}
async function OSGroup() {
    try {
        const res = await fetch('/PerformaRegisterReport/OSGroup');
        const data = await res.json();
        const ddl = $('#ddlforOSGroup');
        ddl.empty();
        ddl.append('<option value="">--- Select OS Group ---</option>');

        data.forEach(item => { ddl.append( `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` ); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select OS Group---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading OS Group:", error);
    }
}
async function DDlTransport() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlTransport');
        const data = await res.json();
        const ddl = $('#ddlforTransport');
        ddl.empty();
        ddl.append('<option value="">--- Select Transport ---</option>');

        data.forEach(item => { ddl.append( `<option value="${item.value}"> ${item.value} - ${item.text}  </option>` ); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Transport---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Transport:", error);
    }
}

async function DDlACGroup() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlACGroup');
        const data = await res.json();
        const ddl = $('#ddlforAcGroup');
        ddl.empty();
        ddl.append('<option value="">--- Select AC Group ---</option>');

        data.forEach(item => { ddl.append(`<option value="${item.value}"> ${item.value} - ${item.text}  </option>`); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select AC Group---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading AC Group:", error);
    }
}

async function DDlCountry()
{
    try {
        const res = await fetch('/PerformaRegisterReport/DDlCountry');
        const data = await res.json();
        const ddl = $('#ddlForCountry');
        ddl.empty();
        ddl.append('<option value="">--- Select Country ---</option>');

        data.forEach(item => { ddl.append(`<option value="${item.value}"> ${item.value} - ${item.text}  </option>`); });
        // Initialize searchable dropdown
        ddl.select2({ placeholder: '--- Select Country---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Country:", error);
    }
}

async function DDlItem() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlItem');

        if (!res.ok) {
            throw new Error('Failed to load Item Name');
        }

        const data = await res.json();
        const ddl = $('#ddlItem');

        // Destroy existing Select2
        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.select2('destroy');
        }

        ddl.empty();
        ddl.append('<option value="">--- Select Item Name ---</option>');

        // Bind Party Names
        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });

        // Initialize Select2 Multiple Checkbox
        ddl.select2({
            placeholder: "--- Select Item ---",
            closeOnSelect: false,
            allowClear: true,
            width: '100%',
            templateResult: function (item) {

                if (!item.id) {
                    return item.text;
                }

                const checkbox = $('<span><input type="checkbox" class="me-2" />' + item.text + '</span>');

                checkbox.find('input').prop(
                    'checked',
                    item.element.selected
                );

                return checkbox;
            },
            templateSelection: function (item) {
                return item.text;
            }
        });

    } catch (error) {
        console.error("Error loading Item :", error);
    }
}

async function DDlPartyName() {
    try {
        const res = await fetch('/PerformaRegisterReport/DDlPartyName');

        if (!res.ok) {
            throw new Error('Failed to load Party Names');
        }

        const data = await res.json();
        const ddl = $('#ddlforPartyName');

        // Destroy existing Select2
        if (ddl.hasClass('select2-hidden-accessible')) {
            ddl.select2('destroy');
        }

        ddl.empty();
        ddl.append('<option value="">--- Select Party Name ---</option>');

        // Bind Party Names
        data.forEach(item => { ddl.append(  `<option value="${item.value}">${item.text}</option>` );
        });

        // Initialize Select2 Multiple Checkbox
        ddl.select2({
            placeholder: "--- Select Party ---",
            closeOnSelect: false,
            allowClear: true,
            width: '100%',
            templateResult: function (item) {

                if (!item.id) {
                    return item.text;
                }

                const checkbox = $('<span><input type="checkbox" class="me-2" />' + item.text + '</span>');

                checkbox.find('input').prop(
                    'checked',
                    item.element.selected
                );

                return checkbox;
            },
            templateSelection: function (item) {
                return item.text;
            }
        });

    } catch (error) {
        console.error("Error loading Party Names:", error);
    }
}

async function TransitReport() {
    try {

        // 1. Get form values
        const ToDate = $('#DtToDate').val();
        const FromDate = $('#DtFromDate').val();
        const InsuranceId = $('#ddlInsurancesName').val();

        const reportName = "INSU_REGISTER";

        if (!FromDate || !ToDate) {
            toastr.warning("Please select From Date and To Date.");
            return;
        }

        // 2. Prepare TEMP_GST and calculate Opening Balance
        const validation = await $.ajax({
            url: '/InsurancesRegister/Report',
            type: 'POST',
            data: {
                ToDate: ToDate,
                FromDate: FromDate,
                InsuranceId: InsuranceId
            }
        });

        console.log("Opening Balance Response:", validation);

        const openingBalance = Number(
            validation.openingBalance ??
            validation.OpeningBalance ?? 0
        );

        // 3. Prepare Selection Formula
        const formula =
            "{TEMP_GST.COMP_CODE}=" + Number(globalVars.CompCode) +
            " and {TEMP_GST.WSID}= '" +
            (globalVars.PubWorkStationID || "").replace(/'/g, "''") + "' " +
            " and {TEMP_GST.USERID}=" + Number(globalVars.UserId) +
            " AND {TEMP_GST.V_TYPE}='INSU' " +
            " AND {SALE1.V_TYPE}='SAGT'";

        // 4. Prepare Formula Fields
        const Formulafield =
            "From " + ReportformatDate(FromDate) +
            " To " + ReportformatDate(ToDate);

        const payload = {
            Reportname: reportName,
            Database: database,
            selectionFormula: formula,

            Parameters: {
                comp_name: "'" + (globalVars.CompanyName || "") + "'",
                comp_add1: "'" + (globalVars.Address1 || "") + "'",
                comp_add2: "'" + (globalVars.Address2 || "") + "'",
                RPTNAME: "'STATEMENT OF INSURANCE DECLARATION'",
                F1: "'" + Formulafield + "'",
                OPAMT: openingBalance
            }
        };

        console.log("Selection Formula:", formula);
        console.log("Report Payload:", payload);

        // 5. Generate Timestamp
        const now = new Date();

        const timestamp =
            String(now.getDate()).padStart(2, '0') +
            String(now.getMonth() + 1).padStart(2, '0') +
            String(now.getFullYear()).slice(-2) + "_" +
            String(now.getHours()).padStart(2, '0') +
            String(now.getMinutes()).padStart(2, '0') +
            String(now.getSeconds()).padStart(2, '0');

        // 6. Generate Crystal Reports PDF
        const response = await $.ajax({
            url: 'http://localhost:24085/Report/PendingQCReport',
            type: 'POST',
            data: JSON.stringify(payload),
            contentType: "application/json; charset=utf-8",
            xhrFields: {
                responseType: 'blob'
            }
        });

        // 7. Validate PDF response
        if (!response || response.size === 0) {
            throw new Error("Crystal Reports returned an empty PDF.");
        }

        // 8. Download PDF
        const fileName = `${reportName}_${timestamp}.pdf`;

        const fileUrl = URL.createObjectURL(
            new Blob([response], { type: 'application/pdf' })
        );

        const link = document.createElement('a');

        link.href = fileUrl;
        link.download = fileName;

        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);

        setTimeout(() => {
            URL.revokeObjectURL(fileUrl);
        }, 1000);

        toastr.success("Report generated successfully.");

    }
    catch (error) {

        console.error("Transit Report Error:", error);

        // 9. Read actual API error
        let errorMessage = error.message || "Unknown error";

        if (error.response instanceof Blob) {

            try {
                const responseText = await error.response.text();

                console.error("API Error Response:", responseText);

                try {
                    const errorJson = JSON.parse(responseText);

                    errorMessage =
                        errorJson.Message ||
                        errorJson.message ||
                        errorJson.ExceptionMessage ||
                        responseText;
                }
                catch {
                    errorMessage = responseText;
                }

            }
            catch (readError) {
                console.error("Error reading API response:", readError);
            }
        }

        console.error("Final Error:", errorMessage);

        toastr.error(
            "Report generation failed: " + errorMessage
        );
    }
}
function ReportformatDate(value) {

    if (!value) return "";

    const [year, month, day] = value.split("-").map(Number);

    const months = [
        "Jan", "Feb", "Mar", "Apr", "May", "Jun",
        "Jul", "Aug", "Sep", "Oct", "Nov", "Dec"
    ];

    if (!year || !month || !day || month > 12 || day > 31) {
        return "";
    }

    return `${String(day).padStart(2, '0')}/${months[month - 1]}/${year}`;
}