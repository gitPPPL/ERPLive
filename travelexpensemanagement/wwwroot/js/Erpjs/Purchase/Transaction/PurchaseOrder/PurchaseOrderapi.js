function calculateTaxAmounts(rowId) {
    const rate = parseFloat($(`#TxtRate${rowId}`).val()) || 0;
    const qty = parseFloat($(`#TxtQty${rowId}`).val()) || 0;
    const amount = rate * qty;

    const discPer = parseFloat($(`#TxtDiscPercent${rowId}`).val()) || 0;
    const discAmt = (amount * discPer) / 100;
    $(`#TxtDisc${rowId}`).val(discAmt.toFixed(2));

    const packPer = parseFloat($(`#TxtPackPercent${rowId}`).val()) || 0;
    const packAmt = (amount * packPer) / 100;
    $(`#TxtPack${rowId}`).val(packAmt.toFixed(2));

    const taxableAmount = amount - discAmt + packAmt;

    const cgstPer = parseFloat($(`#TxtCgstPercent${rowId}`).val()) || 0;
    const sgstPer = parseFloat($(`#TxtSgstPercent${rowId}`).val()) || 0;
    const igstPer = parseFloat($(`#TxtIgstPercent${rowId}`).val()) || 0;
    const cessPer = parseFloat($(`#TxtCessPercent${rowId}`).val()) || 0;
    const tcsPer = parseFloat($(`#TxtTcsPer${rowId}`).val()) || 0;
    const vatPer = parseFloat($(`#TxtVatPercent${rowId}`).val()) || 0;
    const othPer1 = parseFloat($(`#TxtOthPer${rowId}`).val()) || 0;
    const othPer2 = parseFloat($(`#TxtOthPer2${rowId}`).val()) || 0;

    const cgstAmt = (taxableAmount * cgstPer) / 100;
    const sgstAmt = (taxableAmount * sgstPer) / 100;
    const igstAmt = (taxableAmount * igstPer) / 100;
    const cessAmt = (taxableAmount * cessPer) / 100;
    const tcsAmt = (taxableAmount * tcsPer) / 100;
    const vatAmt = (taxableAmount * vatPer) / 100;
    const othAmt1 = (taxableAmount * othPer1) / 100;
    const othAmt2 = (taxableAmount * othPer2) / 100;

    const totalTax = cgstAmt + sgstAmt + igstAmt + cessAmt + tcsAmt + vatAmt + othAmt1 + othAmt2;
    const netAmt = taxableAmount + totalTax;

    // Update DOM
    $(`#TxtAmount${rowId}`).val(amount.toFixed(2));
    $(`#TxtCgst${rowId}`).val(cgstAmt.toFixed(2));
    $(`#TxtSgst${rowId}`).val(sgstAmt.toFixed(2));
    $(`#TxtIgst${rowId}`).val(igstAmt.toFixed(2));
    $(`#TxtCess${rowId}`).val(cessAmt.toFixed(2));
    $(`#TxtTcsAmt${rowId}`).val(tcsAmt.toFixed(2));
    $(`#TxtVat${rowId}`).val(vatAmt.toFixed(2));
    $(`#TxtOthAmt${rowId}`).val(othAmt1.toFixed(2));
    $(`#TxtOthAmt2${rowId}`).val(othAmt2.toFixed(2));
    $(`#TxtNetAmt${rowId}`).val(netAmt.toFixed(2));

    calculateAllTotals(); // Recalculate footer totals
}