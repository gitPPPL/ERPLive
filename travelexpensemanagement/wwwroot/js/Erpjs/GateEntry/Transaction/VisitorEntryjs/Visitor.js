// ================= STATE =================
const urlParams = new URLSearchParams(location.search);
const rowId = (urlParams.get('docId'));
const isReadOnly = urlParams.get('readOnly') === 'true';

let isMobileDataLoaded = false;
let isMobileLoading = false;
let mobileRequest = null;
//let isMobileReady = true;
let originalMobile = "";
let formState = {
    isSaved: false,
    isReadOnlyFromUrl: isReadOnly
};

// ============ INIT ============
function VisitorInit() {

    setCurrentDate();
    setCurrentTime();

    if (rowId) {

        loadEmpList().then(() => {
            initSelect2();   // init AFTER data load
            loadVisitorEntry(rowId);
            if (isReadOnly) {
                setVisitorEntryFormReadOnly();
            }
        });

    } else {
        loadEmpList().then(() => {
            initSelect2();   // same for new form
            GetVNo();
        });
    }

    //==== For Handle Submit ======
    //$('#VisitorEntryForm').on('submit', function (e) {
    //    onFormSubmit(e);
    //});
    $('#VisitorEntryForm')
        .off('submit')   // 🔴 remove old binding
        .on('submit', function (e) {
            onFormSubmit(e);
        });

    if (!rowId) {
        $('#chkOutDate').prop('checked', false);
        $('#DtOutDate').prop('disabled', true);
    }

    $('#NumMobileNo').on('keypress', function (e) {

        if (e.which === 13) {

            e.preventDefault();

            const mobile = $(this).val().trim();

            const wait = setInterval(() => {
                if (!isMobileLoading) {
                    clearInterval(wait);
                    $('#TxtVisitorName').focus();
                }
            }, 50);
        }
    });

    // ===== CHECKBOX =====
    $('#chkOutDate').off('change').on('change', function () {

        if ($(this).is(':checked')) {

            $('#DtOutDate').prop('disabled', false);

            let today = new Date();
            let dd = String(today.getDate()).padStart(2, '0');
            let mm = String(today.getMonth() + 1).padStart(2, '0');
            let yyyy = today.getFullYear();

            $('#DtOutDate').val(`${yyyy}-${mm}-${dd}`);

            setCurrentOutTime();

        } else {
            $('#DtOutDate').prop('disabled', true);
            $('#DtOutDate').val('');
            $('#TMOutTime').val('');
        }
    });

    // ===== MEET EMPLOYEE =====
    $('#ddlMeetEmployee').off('change').on('change', function () {

        const selectedText = $("#ddlMeetEmployee option:selected").text();
        const selectedValue = $(this).val();

        if (selectedValue) {
            const nameOnly = selectedText.split('|')[0].trim();
            $('#TxtMeetOther').val(nameOnly);

            clearInvalid($('#ddlMeetEmployee'));
            clearInvalid($('#TxtMeetOther'));
        } else {
            $('#TxtMeetOther').val('');
        }
    });

    // ===== MOBILE AUTOFILL=====

    $('#NumMobileNo').off('blur').on('blur', function () {

        const mobile = $(this).val().trim();

        if (!mobile || mobile.length < 10) return;
        if (isMobileLoading) return;

        // small delay to avoid blur race
        setTimeout(() => {
            getMobileData(mobile)
                .catch(() => {
                    showToast("Failed to fetch visitor data", { type: "error" });
                });
        }, 200);
    });

    //$('#NumMobileNo').off('blur').on('blur', function (e) {
    //    e.preventDefault();
    //    isMobileDataLoaded = false;
       
    //    //if (rowId) return;
       
    //    const mobile = $(this).val().trim();

    //    if (rowId && mobile === originalMobile) return;

    //    if (!mobile || mobile.length < 10) {
    //        isMobileLoading = false;
    //        return;
    //    }

    //    //if (mobileRequest) {
    //    //    mobileRequest.abort(); // ✅ cancel previous
    //    //}

    //    isMobileLoading = true;
    //    VisitorAPI.getVisitorByMobile(mobile)

    //    .done(function (res) {

    //        if (!res.success || !res.data) return;

    //        const v = res.data;

    //        $('#TxtVisitorName').val(v.name || '');
    //        $('#TxtAddress').val(v.address || '');
    //        $('#TxtOrganization').val(v.organization || '');
    //        $('#ddlPurpose').val(v.purpose || '').trigger('change');
    //        $('#ddlMeetEmployee').val(v.meet_CODE || '').trigger('change');
    //        $('#TxtMeetOther').val(v.meet_NAME || '');
    //        $('#TxtVehicleNo').val(v.vehicle_NO || '');
    //        $('#TxtMaterial').val(v.material || '');
    //        $('#TxtRemarks').val(v.remarks || '');

    //        isMobileDataLoaded = true;

    //        setTimeout(() => {
    //            $('#TxtVisitorName').focus();
    //        }, 50);
    //    })

    //    .fail(function () {
    //        showToast("Failed to fetch visitor data", { type: "error" });
    //    })

    //    .always(function () {
    //        isMobileLoading = false;
    //    });
    //});

    // ===== PRINT BUTTON(logic) =====
    $('#btn_Print').off('click').on('click', function () {

        const vType = 'VISI';
        const vNo = $('#NumDocNo').val()?.trim();
        const docId = $('#DOCID').val()?.trim();

        if (!vNo || vNo === "0") {
            showToast("Invalid document number", { type: "warning" });
            return;
        }

        if (isReadOnly === true) {
            openPrint(vNo, vType, docId);
            return;
        }

        if (vNo && vType) {
            openPrint(vNo, vType);
            return;
        }

        showToast("Please save data first", { type: "warning" });
    });
}

//=== initilize Select2===
function initSelect2() {
    $('#ddlMeetEmployee').select2({
        placeholder: "Search Employee..",
        allowClear: true,
        width: '100%'
    });
}

// =====BIND EMPLIST =========
function loadEmpList() {

    empMap = {};

    return VisitorAPI.loadEmpList().done(function (data) {

        const ddl = $('#ddlMeetEmployee');
        ddl.empty();
        ddl.append('<option value="">-- Select Meet Employee --</option>');

        $.each(data, function (i, item) {

            const text = item.text + ' | ' + item.value;

            ddl.append(`<option value="${item.value}">${text}</option>`);
            empMap[item.value] = item.text;
        });
        ddl.trigger('change.select2');
    });
}

//========= LOAD VISITOR ========
function loadVisitorEntry(docId) {

    $('.circle-loader').css('display', 'flex');

    VisitorAPI.getVisitorById(docId)
    .done(function (res) {

        if (!res.success || !res.data) {
            showToast("Visitor record not found.", { type: "warning" });
            return;
        }

        const visitor = res.data;

        $('#DOCID').val(visitor.doc_id || '');
        $('#NumDocNo').val(visitor.v_NO || '');
        $('#DtDocDate').val(visitor.v_DATE ? visitor.v_DATE.substring(0, 10) : '');

        $('#TxtVisitorName').val(visitor.name || '');
        $('#ddlCardNo').val(visitor.carD_NO || '').trigger('change');
        $('#TxtOrganization').val(visitor.organization || '');
        $('#TxtAddress').val(visitor.address || '');

        const meetCode = visitor.meeT_CODE;
        const meetName = visitor.meeT_NAME || '';

        const empExists = meetCode && $('#ddlMeetEmployee option[value="' + meetCode + '"]').length > 0;

        if (empExists) {
            $('#ddlMeetEmployee').val(meetCode).trigger('change');
                
        } else {
            $('#ddlMeetEmployee').val('').trigger('change');
            $('#TxtMeetOther').val(meetName);
        }

        $('#TMInTime').val(visitor.iN_TIME || '');
        $('#DtOutDate').val(visitor.ouT_DATE ? visitor.ouT_DATE.substring(0, 10) : '');
        $('#chkOutDate').prop('checked', !!visitor.ouT_DATE);
        $('#TMOutTime').val(visitor.ouT_TIME || '');
        $('#NumMobileNo').val(visitor.mobilE_NO || '');
        originalMobile = visitor.mobilE_NO || '';
        $('#ddlPurpose').val(visitor.purpose || '').trigger('change');
        $('#TxtVehicleNo').val(visitor.vehiclE_NO || '');
        $('#TxtMaterial').val(visitor.material || '');
        $('#TxtRemarks').val(visitor.remarks || '');

        const base64Image = res.base64Image;

        if (base64Image && base64Image.length > 0) {

            const imageType =
                visitor.filE_NAME && visitor.filE_NAME.toLowerCase().endsWith('.png')
                    ? 'png'
                    : 'jpeg';

            $('#previewImage')
                .attr('src', 'data:image/' + imageType + ';base64,' + base64Image)
                .show();

            $('#WebcamImage').val(base64Image);
            $('#VisitorImage').val('');

        } else {
            $('#previewImage').hide().attr('src', '');
            $('#WebcamImage').val('');
        }
    })
    .fail(function () {
        showToast("Failed to load visitor data", { type: "error" });
    })
    .always(function () {
        $('.circle-loader').hide();
    });
}

//=======Save and Update========
async function onFormSubmit(e) {

    e.preventDefault();
   
    const btn = $('#btn-save');
    btn.prop('disabled', true);

    if ( isMobileLoading) {
        showToast("Please wait, mobile data loading...", { type: "warning" });
        btn.prop('disabled', false);
        return;
    }

    // if (!validateVisitorForm()) return;
    if (!validateVisitorForm()) {
        btn.prop('disabled', false); // ❗ re-enable
        return;
    }
    
    const validateDate = await checkValidDate();
    if (validateDate == false) {
        return;
    }

    const meetEmployee = $('#ddlMeetEmployee').val();
    const meetOtherName = $('#TxtMeetOther').val().trim();

    // ===== IMAGE =====
    const fileInput = $('#VisitorImage')[0];
    let base64Image = "";
    let fileName = null;

    if (capturedBase64 && capturedBase64 !== "") {
        base64Image = capturedBase64;
        fileName = "captured_" + Date.now() + ".png";
    }
    else if (fileInput.files.length > 0) {
        const file = fileInput.files[0];
        base64Image = await toBase64(file);
        fileName = file.name;
    }

    // ===== MEET NAME =====
    let MEET_NAME = "";

    if (meetOtherName !== "") {
        MEET_NAME = meetOtherName;
    }
    else if (meetEmployee) {
        MEET_NAME = $('#ddlMeetEmployee option:selected').text();
    }

    const payload = {
        Visitor: {
            DOC_ID: $('#DOCID').val(),
            V_NO: $('#NumDocNo').val(),
            V_TYPE: 'VISI',
            V_DATE: $('#DtDocDate').val(),
            NAME: $('#TxtVisitorName').val(),
            CARD_NO: $('#ddlCardNo').val() || null,
            ORGANIZATION: $('#TxtOrganization').val() || null,
            ADDRESS: $('#TxtAddress').val() || null,
            MEET_NAME: MEET_NAME,
            MEET_CODE: meetEmployee || 0,
            REMARKS: $('#TxtRemarks').val() || null,
            IN_TIME: $('#TMInTime').val(),
            OUT_DATE: $('#chkOutDate').is(':checked') ? $('#DtOutDate').val() : null,
            OUT_TIME: $('#TMOutTime').val(),
            MOBILE_NO: $('#NumMobileNo').val(),
            PURPOSE: $('#ddlPurpose').val(),
            VEHICLE_NO: $('#TxtVehicleNo').val() || null,
            MATERIAL: $('#TxtMaterial').val() || null
        },
        Image: {
            FileName: fileName,
            Base64Content: base64Image,
            IsRemoved: $('#IsImageRemoved').val() === "true"
        }
    };

    VisitorAPI.saveVisitor(payload)
    .done(function (response) {

        if (response.success) {

            formState.isSaved = true;
            formState.isReadOnlyFromUrl = true;

            if (response.message.includes("Saved")) {

                if (response.docId) {
                    $('#DOCID').val(response.docId);
                }

                showToast("Data Saved Successfully", { type: "success" });

            } else {
                showToast("Data Updated Successfully", { type: "success" });
            }

            setVisitorEntryFormReadOnly();

        } else {
            showToast(response.message, { type: "error" });
        }
    })
    .fail(function () {
        showToast("Error while saving", { type: "error" });
    })
    .always(function () {
       
        btn.prop('disabled', false); 
    });
}

//=========Validation ===========
function validateVisitorForm() {

    //==Voucher No==
    const vNo = $('#NumDocNo').val();
    if (!vNo || parseInt(vNo) === 0) {
        showToast("Invalid Voucher No.", { type: "warning" });
        $('#NumDocNo').addClass('is-invalid').focus();
        return false;
    } else {
        $('#NumDocNo').removeClass('is-invalid');
    }

    //==Doc Date==
    const docDate = $('#DtDocDate').val();
    if (!docDate) {
        showToast("Doc Date required.", { type: "warning" });
        $('#DtDocDate').addClass('is-invalid').focus();
        return false;
    } else {
        $('#DtDocDate').removeClass('is-invalid');
    }

    //===Mobile===
    const mobile = $('#NumMobileNo').val();
    if (mobile) {
        if (!validatePhone(mobile)) {
            showToast("Please enter valid 10-digit mobile number.", { type: "warning" });
            $('#NumMobileNo').addClass('is-invalid').focus();
            return false;
        } else {
            $('#NumMobileNo').removeClass('is-invalid');
        }
    }

    if (isMobileLoading) {
        showToast("Please wait, fetching visitor data...", { type: "warning" });
        return false;
    }

    //==Visitor Name==
    const visitorName = $('#TxtVisitorName').val().trim();
    if (!visitorName) {
        setInvalid($('#TxtVisitorName'), 'Visitor Name is required');
        return false;
    } else {
        clearInvalid($('#TxtVisitorName'));
    }

    //===Purpose===
    if (!$('#ddlPurpose').val()) {
        setInvalid($('#ddlPurpose'), 'Purpose is required');
        return false;
    } else {
        clearInvalid($('#ddlPurpose'));
    }

    //==Meet Employee OR Other==
    const meetEmployee = $('#ddlMeetEmployee').val();
    const meetOtherName = $('#TxtMeetOther').val().trim();

    if (!meetEmployee && !meetOtherName) {
        showToast("Meet Employee OR Meet Other is Required.", { type: "warning" });
        $('#ddlMeetEmployee').addClass('is-invalid');
        $('#TxtMeetOther').addClass('is-invalid');
        $('#ddlMeetEmployee').focus();
        return false;
    } else {
        $('#ddlMeetEmployee').removeClass('is-invalid');
        $('#TxtMeetOther').removeClass('is-invalid');
    }

    //==In Time==
    const inTime = $('#TMInTime').val();
    if (!inTime) {
        showToast("In Time required.", { type: "warning" });
        $('#TMInTime').addClass('is-invalid').focus();
        return false;
    } else {
        $('#TMInTime').removeClass('is-invalid');
    }

    //== Out Date vs Doc Date ==
    const outDate = $('#DtOutDate').val();
    const isOutDateChecked = $('#chkOutDate').is(':checked');

    if (isOutDateChecked && outDate) {
        let doc = new Date(docDate);
        let out = new Date(outDate);

        if (out < doc) {
            showToast("Out Date cannot be less than Doc Date.", { type: "warning" });
            $('#DtOutDate').addClass('is-invalid').focus();
            return false;
        } else {
            $('#DtOutDate').removeClass('is-invalid');
        }
    }

    //==Out Time Validation==
    const outTime = $('#TMOutTime').val();

    if (inTime && outTime) {

        let inDateTime = new Date(docDate + 'T' + inTime);
        let outDateTime;

        if (outTime && !isOutDateChecked) {
            showToast("Please select Out Date checkbox.", { type: "warning" });
            $('#chkOutDate').focus();
            return false;
        }

        if (outTime && isOutDateChecked && !outDate) {
            showToast("Out Date required when Out Time is entered.", { type: "warning" });
            $('#DtOutDate').addClass('is-invalid').focus();
            return false;
        } else {
            $('#DtOutDate').removeClass('is-invalid');
        }

        if (isOutDateChecked && outDate) {
            outDateTime = new Date(outDate + 'T' + outTime);
        } else {
            outDateTime = new Date(docDate + 'T' + outTime);
        }

        if (inDateTime > outDateTime) {
            showToast("Invalid Out Time. Out Time cannot be less than In Time.", { type: "warning" });
            $('#TMOutTime').addClass('is-invalid').focus();
            return false;
        } else {
            $('#TMOutTime').removeClass('is-invalid');
        }
    }

    return true;
}

//======Generate VNo=========
async function GetVNo() {

    try {

        const data = await VisitorAPI.getVNo();

        if (data && data.v_NO) {
            $('#NumDocNo').val(data.v_NO);
        } else {
            console.warn("v_NO not found in response");
        }

    } catch (error) {
        console.error("Error in GetVNo:", error);
        showToast("Failed to generate document number", { type: "error" });
    }
}

//=====Readonly Mode=======
function setVisitorEntryFormReadOnly() {
    const form = $('#VisitorEntryForm');

    // disable inputs
    form.find('input, select, textarea, button[type="submit"]').prop('disabled', true);
    form.find('button').not('#btnbacklist , #btn_Print').prop('disabled', true);

    // add readonly class
    form.addClass('erppage-readonly');

    $('#VisitorImage').prop('disabled', true);
    $('#canvas').hide();
    $('#previewImage').css('pointer-events', 'none');

    $('#btn-save').hide();
}

//=== handle back====
function returnBack(url) {
    if (formState.isSaved || formState.isReadOnlyFromUrl) {
        window.location.href = url;
        return;
    }
    handleBack(url, false);
}

//===Doc Date=======
function setCurrentDate() {
    let today = new Date();
    let dd = String(today.getDate()).padStart(2, '0');
    let mm = String(today.getMonth() + 1).padStart(2, '0');
    let yyyy = today.getFullYear();

    let formattedDate = `${yyyy}-${mm}-${dd}`;

    // set values
    $('#DtDocDate').val(formattedDate);

}

//===Set Current Time====
function setCurrentTime() {
    const now = new Date();

    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');

    const currentTime = `${hours}:${minutes}`;

    $('#TMInTime').val(currentTime);
}

//===Set outTime====
function setCurrentOutTime() {
    const now = new Date();

    const hours = String(now.getHours()).padStart(2, '0');
    const minutes = String(now.getMinutes()).padStart(2, '0');

    $('#TMOutTime').val(`${hours}:${minutes}`);
}

//====Validate Date======
async function checkValidDate() {

    const data = {
        vdate: $("#DtDocDate").val(),
        vtype: "VISI",
        vno: $("#NumDocNo").val()
    };
    try {

        const result = await VisitorAPI.checkValidDate(data);

        if (result.status === false) {
            showToast(result.message || "Invalid Date", { type: "warning" });
            return false;
        }

        return true;

    } catch (error) {
        console.error("Error:", error);
        showToast("Date validation failed", { type: "error" });
        return false;
    }
}

//====Get Data Form Mobile====
function getMobileData(mobile) {
    return new Promise((resolve, reject) => {

        console.log("API CALL:", mobile);

        if (mobileRequest) {
            mobileRequest.abort();
        }

        isMobileLoading = true;

        mobileRequest = VisitorAPI.getVisitorByMobile(mobile);

        mobileRequest
        .done(function (res) {

            console.log("API RESPONSE RECEIVED");

            if (!res.success || !res.data) {
                resolve(null);
                return;
            }

            const v = res.data;

            // 🔥 IMPORTANT: prevent stale overwrite
            if ($('#NumMobileNo').val().trim() !== mobile) {
                resolve(null);
                return;
            }

            $('#TxtVisitorName').val(v.name || '');
            $('#TxtAddress').val(v.address || '');
            $('#TxtOrganization').val(v.organization || '');
            $('#ddlPurpose').val(v.purpose || '').trigger('change');
            $('#ddlMeetEmployee').val(v.meet_CODE || '').trigger('change');
            $('#TxtMeetOther').val(v.meet_NAME || '');
            $('#TxtVehicleNo').val(v.vehicle_NO || '');
            $('#TxtMaterial').val(v.material || '');
            $('#TxtRemarks').val(v.remarks || '');

            resolve(v);
        })
        .fail(function () {
            reject();
        })
        .always(function () {
            console.log("API FINISHED");
            isMobileLoading = false;
            mobileRequest = null;
        });
    });
}

//===Base File For(image)====
function toBase64(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.readAsDataURL(file);
        reader.onload = () => resolve(reader.result);
        reader.onerror = error => reject(error);
    });
}

//====Camera Code Started=====
let stream = null;
let opened = false;
let capturedBase64 = "";

async function startCamera() {
    const video = document.getElementById('video');

    try {

        if (stream) {
            stream.getTracks().forEach(track => track.stop());
        }

        stream = await navigator.mediaDevices.getUserMedia({
            video: {
                facingMode: "environment",
                width: { ideal: 1280 },
                height: { ideal: 720 }
            }
        });

        video.srcObject = stream;
        video.style.display = 'block';

        opened = true;

    } catch (err) {
        console.error("Camera error:", err);

        if (err.name === "NotAllowedError") {
            showToast("Camera access was denied. Please allow permission.", { type: "error" });
        }
        else if (err.name === "NotFoundError") {
            showToast("No camera device found.", { type: "error" });
        }
        else if (err.name === "NotReadableError") {
            showToast("Camera is already in use by another application.", { type: "error" });
        }
        else {
            showToast("Unable to access the camera. Please try again.", { type: "error" });
        }
    }
}

function capturePhoto() {
    const video = document.getElementById('video');
    const canvas = document.getElementById('canvas');
    const img = document.getElementById('previewImage');

    if (!stream || video.readyState !== 4) {

        showToast("Please start camera", { type: "warning" });
        return;
    }

    canvas.width = video.videoWidth;
    canvas.height = video.videoHeight;

    const context = canvas.getContext('2d');
    context.drawImage(video, 0, 0, canvas.width, canvas.height);

    capturedBase64 = canvas.toDataURL('image/png');
    img.src = capturedBase64;
    img.style.display = 'block';

    $('#WebcamImage').val(capturedBase64.split(',')[1]);

    stream.getTracks().forEach(track => track.stop());
    stream = null;
}

function removePhoto() {

    const previewSrc = $('#previewImage').attr('src');
    const webcamImage = $('#WebcamImage').val();

    // Check if image exists
    if (!previewSrc && !capturedBase64 && !webcamImage) {
        showToast("No image found to remove", { type: "warning" });
        return;
    }

    // Remove image
    $('#previewImage').attr('src', '').hide();
    $('#VisitorImage').val('');
    $('#WebcamImage').val('');

    capturedBase64 = "";
    $('#IsImageRemoved').val("true");

    // Stop camera if running
    if (stream) {
        stream.getTracks().forEach(track => track.stop());
        stream = null;
    }
    showToast("Image removed successfully", { type: "success" });

}

//=== Camera Code End=========

//===Open Print======
function openPrint(vNo, vType) {
    $.ajax({
        url: '/VisitorEntry/PrintSlip',
        type: 'GET',
        data: { vNo: vNo, vType: vType },

        success: function (res) {
            // No Data Found case
            if (res.success === false) {
                showToast(res.message, { type: "warning" });
                return;
            }
            const printWindow = window.open("", "_blank", "width=900,height=700");
            printWindow.document.open();
            printWindow.document.write(res.html || res);
            printWindow.document.close();
        },

        error: function () {
            showToast("Error while printing", { type: "error" });
        }
    });
}

