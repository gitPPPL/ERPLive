$(document).ready(function () {
    var controllerName = window.location.pathname.split('/')[1];
    checkPermissionForEntryPage(controllerName);
    VisitorInit();
});

