$(document).ready(function () {

    createTableHeader();


    var today = new Date();
    var currentDate = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');
    $('#Dtfrom').val(currentDate);
    $('#Dtto').val(currentDate);
    LoadDropdown();

    $('#btnviewdata').on('click', async function () {

        $('#tblImportExportDocAttachmentList tbody').empty();
        // Call once when page loads

        Viewdata();

    });

    $('#ddltype').on('change', function () {
        createTableHeader();
    });
});