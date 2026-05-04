async function BillNoValidation(PARTY_CODE, BILL_NO, V_NO) {
    try {
        const response = await $.ajax({
            url: '/InwardEntry/BillNoValidation',
            type: 'POST',
            data: {
                PARTY_CODE: PARTY_CODE,
                BILL_NO: BILL_NO,
                V_NO: V_NO
            }
        });

        if (!response.success) {
            toastr.error(response.message || "Invalid Bill No");
            return response;
        }

        return response;

    } catch (error) {
        toastr.error("Validation Error", error);
        return { success: false };
    }
}

async function GatenoValidation(V_TYPE, V_NO) {
    try {

        const response = await $.ajax({
            url: '/InwardEntry/GatenoValidation',
            type: 'POST',
            data: { V_TYPE: V_TYPE, V_NO: V_NO }
        });

        if (!response.success) {
            toastr.error(response.message || "Invalid Gate No");
            return response;
        }
        return response;

    } catch (error) {
        toastr.error("Validation Error", error);
        return { success: false };
    }
}