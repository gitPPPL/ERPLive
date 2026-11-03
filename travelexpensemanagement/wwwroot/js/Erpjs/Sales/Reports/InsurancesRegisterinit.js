
    var globalVars = window.globalVariables || { };
    let LoginDate = globalVars.LoginDate;
    var controllerName = window.location.pathname.split('/')[1];

    $(document).ready(async function () {
        SetFYDate('DtToDate', LoginDate);
    SetFYDate('DtFromDate', LoginDate);
    await cmbSalesThrough();

    $('#btn_Report').on('click', async function () {
    try {

        TransitReport();

    } catch (error) {
        console.error("Error generating report:", error);
    }
});

});

 async function cmbSalesThrough() {
    try {
        const res = await fetch('/InsurancesRegister/DDlInsurancesName');
    const data = await res.json();

    const ddl = $('#ddlInsurancesName');

    ddl.empty();
    ddl.append('<option value="">--- Select Insurances Name ---</option>');

        data.forEach(item => {
        ddl.append(
            `<option value="${item.value}">
                    ${item.value} - ${item.text}
                </option>`
        );
        });

    // Initialize searchable dropdown
    ddl.select2({placeholder: '--- Select Insurances Name---', allowClear: true, width: '100%' });

    } catch (error) {
        console.error("Error loading Insurances Name:", error);
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








