$(document).ready(function () {

    createTableHeader();

    var today = new Date();
    var currentDate = today.getFullYear() + '-' + String(today.getMonth() + 1).padStart(2, '0') + '-' + String(today.getDate()).padStart(2, '0');
    $('#Dtfrom').val(currentDate);
    $('#Dtto').val(currentDate);

    LoadDropdown();

    $('#btnviewdata').on('click', async function ()
    {
        $('#tblImportExportDocAttachmentList tbody').empty();
        Viewdata();
    });

    $('#ddltype').on('change', function () {
        $('#tblImportExportDocAttachmentList tbody').empty();
        createTableHeader();
    });

    $('#btnexportsave').on('click', function () {

        let selectedRows = getSelectedRowData();

        if (!selectedRows || selectedRows.length === 0) {
            Swal.fire("Please select at least one row.");
            return;
        }

        let attachments = [];

        selectedRows.forEach(function (row) {

            let vType = (row.V_TYPE || "").toUpperCase();

            if (vType === "IMPORT") {

                addAttachment(attachments, row.PICopy, row.PIPath);
                addAttachment(attachments, row.BLCopy, row.BLPath);
                addAttachment(attachments, row.BECopy, row.BEPath);
                addAttachment(attachments, row.LCCopy, row.LCPath);
                addAttachment(attachments, row.INVCopy, row.INVPath);
                addAttachment(attachments, row.DPCopy, row.DPPath);
                addAttachment(attachments, row.SBLCCopy, row.SBLCPath);
                addAttachment(attachments, row.OthCopy1, row.OthPath1);
                addAttachment(attachments, row.OthCopy2, row.OthPath2);
            }
            else if (vType === "EXPORT") {

                addAttachment(attachments, row.SBLCCopy, row.SBLCPath);
                addAttachment(attachments, row.BLCopy, row.BLPath);
                addAttachment(attachments, row.BRCCopy, row.BRCPath);

                for (let i = 1; i <= 7; i++) {
                    addAttachment(
                        attachments,
                        row[`OthCopy${i}`],
                        row[`OthPath${i}`]
                    );
                }
            }
        });



        $.ajax({
            url: '/ImportExportDocAttachmentList/DownloadAttachments',
            type: 'POST',
            contentType: 'application/json; charset=utf-8',
            data: JSON.stringify(attachments),
            xhrFields: {
                responseType: 'blob'
            },
            success: function (response, status, xhr) {

                let blob = new Blob([response], {
                    type: 'application/zip'
                });

                let url = window.URL.createObjectURL(blob);

                let a = document.createElement('a');
                a.href = url;
                a.download = 'Attachments.zip';

                document.body.appendChild(a);
                a.click();

                a.remove();
                window.URL.revokeObjectURL(url);
            },
            error: function (xhr) {
                Swal.fire(
                    "Error",
                    xhr.responseText || "Unable to download attachments.",
                    "error"
                );
            }
        });









        console.log("Selected Rows:", selectedRows);
        console.log("Attachments:", attachments);
    });


    function addAttachment(list, fileName, filePath) {

        if (!fileName || !filePath)
            return;

        list.push({
            fileName: fileName,
            filePath: filePath
        });
    }

    $(document).on("change", "#chkSelectAllImport, #chkSelectAllExport", function ()
    {
        const isChecked = $(this).prop("checked");
        $("#tblImportExportDocAttachmentList tbody").find("input.row-select[type='checkbox']").prop("checked", isChecked);
    });

});