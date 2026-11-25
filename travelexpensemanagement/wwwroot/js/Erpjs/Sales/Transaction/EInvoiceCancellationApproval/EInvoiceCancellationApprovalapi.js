async function LoadDropdown() {
    try {
        await Promise.all([
            cmbV_Type(),
            cmbEInvoiceDocument()        
        ]);
    } catch (error) {
        console.log("Dropdown load failed:", error);
        toastr.error("Failed to load dropdown data");
    }
}

async function cmbV_Type() {
    try {
        const res = await fetch('/EInvoiceCancellationApproval/DDlVType');
        const data = await res.json();
        const ddl = $('#DDlDocumentType');
        ddl.empty();
        data.forEach(item => { ddl.append(`<option value="${item.value}">${item.text}</option>`); });
    }
    catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}

async function cmbEInvoiceDocument() {
    try {
        const res = await fetch('/EInvoiceCancellationApproval/DDlEInvoiceDocument');
        const data = await res.json();
        const ddl = $('#DDlEInvoiceDocument');
        ddl.empty();

        data.forEach(item => { ddl.append(`<option value="${item.value}">${item.text}</option>`); });
    }
    catch (error) {
        console.error("Error loading Doc Type:", error);
    }
}