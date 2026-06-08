using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class ParameterMasterController : Controller
    {
        private readonly DropdownService _dropdownService;
        private readonly IParameterMasterRepository _repository;

        public ParameterMasterController(DropdownService dropdownService, IParameterMasterRepository repository)
        {
            _dropdownService = dropdownService;
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/ParameterMaster/Index.cshtml");
        }

        [HttpGet]
        public JsonResult GetDropdown(string type)
        {
            string query = "";
            switch (type)
            {
                case "QCUnit":
                    query = $@"Select CODE, NAME from QCPUNIT_MAST where Active=1 Order by Name";
                    break;
            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }

        public class ParameterModel
        {
            public int? code { get; set; }        
            public string? Name { get; set; }
            public string? ShortName { get; set; }
            public int? QUnitCd { get; set; }
            public int? Qty { get; set; }
            public int? active { get; set; }
        }

        [HttpGet]
        public JsonResult getExistOrNot(string inputData)
        {
            if (string.IsNullOrWhiteSpace(inputData))
            {
                return Json(new { status = false, message = "Invalid Name!" });
            }
            var result = _repository.GetExistOrNotAsync(inputData);
            return Json(new { status = result.status, exists = result.data, message = result.message});
        }

        [HttpPost]
        public async Task<IActionResult> SaveQParamMast([FromBody] ParameterModel model)
        {
            if(model == null)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }
            var result = await _repository.SaveQParamMastAsync(model);
            return Json(new { status = result.status, message = result.message });
        }

        [HttpGet]
        public async Task<IActionResult> GetQParameterDetailsById(string id)
        {
            if (Convert.ToInt32(id) <= 0)
            {
                return Json(new { status = false, message = "Invalid Id!" });
            }
            var result = await _repository.GetQParameterDetailsByIdAsync(id);
            if (result.data != null)
            {
                return Json(new { status = true, data = result.data });
            }
            return Json(new { status = result.status, message = result.message });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQParameterMast([FromBody] ParameterModel model)
        {
            if (model == null)
            {
                return Json(new { status = false, message = "Data Save Failed" });
            }
            var result = await _repository.UpdateQParameterMastAsync(model);
            return Json(new { status = result.status, message = result.message });

        }
    }
}
