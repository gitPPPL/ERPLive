using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Models.QualityControl.Master;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class UOMMasterController : Controller
    {
        private readonly IUOMMasterRepository _repository;

        private int? userLevel;
        public UOMMasterController(IUOMMasterRepository repository)
        {
            _repository = repository;
        }
        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/UOMMaster/Index.cshtml");
        }
        [HttpPost]
        public IActionResult SaveUOM([FromBody] QCPUNIT_MAST model)
        {
            if (string.IsNullOrWhiteSpace(model.NAME))
            {
                return Json(new { success = false, message = "QC Unit name cannot be blank." });
            }
            var result = _repository.SaveUOM(model);
            return Json(new { success = result.status, message = result.message});
        }
        
        [HttpGet]
        public JsonResult IsQcUOMDeletable(int docId)
        {
            if(docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _repository.IsQcUOMDeletable(docId);
            if (result.data)
            {
                return Json(new { success = result.status, message = result.message, isExists = result.data });
            }
            return Json(new { success = result.status, message = result.message });
        }
        [HttpPost]
        public JsonResult DeleteUOMByCode(int docId)
        {
            if (docId <= 0)
            {
                return Json(new { success = false, message = "Invalid Id!" });
            }
            var result = _repository.DeleteUOMByCode(docId);
            return Json(new { success = result.status, message = result.message });
        }
    }

}
 