async function DDlTransPortName() {
    try {
        const response = await fetch('/TransportRateQuotation/DDlTransPortName');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

        transportData = await response.json();
    } catch (error) {
        console.error("Error loading Transport Names:", error);
        toastr.error('Error loading Transport Names: ' + error.message);
    }
}

async function DDlDo() {
    try {
        const response = await fetch('/TransportRateQuotation/DDlDo');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

        const data = await response.json();
        const ddl = $('#ddlDONo');
        ddl.empty().append('<option value="">-- Select DO No --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text} (${item.value})</option>`);
        });

        ddl.select2({
            placeholder: '-- Select DO No --',
            width: '100%'
        });
    } catch (error) {
        console.error("Error loading DO No:", error);
        toastr.error('Error loading DO No: ' + error.message);
    }
}

async function DDlCityName() {
    try {
        const response = await fetch('/TransportRateQuotation/DDlCityName');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

        const data = await response.json();
        const ddl = $('#DDlCityname');
        ddl.empty().append('<option value="">-- Select City Name --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text}</option>`);
        });
    } catch (error) {
        console.error("Error loading City Name:", error);
        toastr.error('Error loading City Name: ' + error.message);
    }
}

async function DDlBillto() {
    try {
        const response = await fetch('/TransportRateQuotation/DDlBillto');
        if (!response.ok) throw new Error(`HTTP error! status: ${response.status}`);

        const data = await response.json();
        const ddl = $('#TxtBillTo');
        ddl.empty().append('<option value="">-- Select Bill To --</option>');

        data.forEach(item => {
            ddl.append(`<option value="${item.value}">${item.text} (${item.value})</option>`);
        });

        ddl.select2({
            placeholder: '-- Select Bill To--',
            width: '100%'
        });
    } catch (error) {
        console.error("Error loading Bill To:", error);
        toastr.error('Error loading Bill To: ' + error.message);
    }
}


async function GetVNo() {
    try {
        const res = await fetch(`/TransportRateQuotation/GetVNo`);
        if (!res.ok) throw new Error(`HTTP ${res.status}`);
        const data = await res.json();
        if (!data.v_NO) throw new Error('Response missing v_NO');
        $('#NumDocumentNo').val(data.v_NO);
    } catch (e) {
        toastr.warning('Error loading Document Number: ' + e.message);
    }
}
