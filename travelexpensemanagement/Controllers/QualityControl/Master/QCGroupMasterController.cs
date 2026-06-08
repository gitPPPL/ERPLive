using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class QCGroupMasterController : Controller
    {
        private readonly IQCGroupMasterRepository _repository;
        
        public QCGroupMasterController(IQCGroupMasterRepository repository)
        {
            _repository = repository;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/QCGroupMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveQCGroup([FromBody] QCG_MAST model)
        {
            if (string.IsNullOrWhiteSpace(model.NAME))
            {
                return Json(new { success = false, message = "QC Group Name cannot be blank." });
            }

            if (string.IsNullOrWhiteSpace(model.QC_TYPE))
            {
                return Json(new { success = false, message = "QC Type cannot be blank." });
            }
            var result = _repository.SaveQCGroup(model);
            return Json(new { success = result.status, message = result.message });
        }
        

        [HttpGet]
        public JsonResult IsQcGroupDeletable(int docId)
        {
            if(docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _repository.IsQcGroupDeletable(docId);
            return Json(new { success = result.status, message = result.message, isExists = result.data});
        }

        [HttpPost]
        public JsonResult DeleteQCGroupByCode(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _repository.DeleteQCGroupByCode(docId);
            return Json(new { success = result.status, message = result.message });
        }

    }
}
 