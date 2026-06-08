using Microsoft.AspNetCore.Mvc;
using travelexpensemanagement.Authorize;
using travelexpensemanagement.Common.DropdownService;
using travelexpensemanagement.Common.Globalvariable;
using travelexpensemanagement.Repositories.Interfaces.QualityControl.Master;

namespace travelexpensemanagement.Controllers.QualityControl.Master
{
    [SessionAuthorize]
    public class TapeAndFabricMasterController : Controller
    {
        private readonly GlobalVariableService _globalValue;
        private readonly DropdownService _dropdownService;
        private readonly ITapeAndFabricMasterRepository _repository;


        
        public TapeAndFabricMasterController(GlobalVariableService globalValue, DropdownService dropdownService, ITapeAndFabricMasterRepository repository)
        {
            _globalValue = globalValue;
            _dropdownService = dropdownService;
            _repository = repository;
        }

        public IActionResult Index()
        {
            return View("~/Views/QualityControl/Master/TapeAndFabricMaster/Index.cshtml");
        }

        public JsonResult GetDropdown(string type)
        {
            string query = "";
            var gv = _globalValue.GetGlobalVariables();
            switch (type)
            {
                case "Color":
                    query = $@"select distinct code, Name from COLOR_MAST where COMP_CODE={gv.PubCompCode}  order by Name";
                    break;
                case "Mesh":
                    query = $@"select distinct code, Name from MESH_MAST where COMP_CODE={gv.PubCompCode}  order by Name";
                    break;
            }
            var data = _dropdownService.GetDropdownList(query);
            return Json(data);
        }
        public class TapeNFabricModel
        {          
            public int? Code { get; set; }
            public string? Name { get; set; }
            public int? MeshCode { get; set; }
            public decimal? StdGram { get; set; }
            public decimal? MinGram { get; set; }
            public decimal? MaxGram { get; set; }
            public decimal? Gsm { get; set; }
            public decimal? Denier { get; set; }
            public string? UnitName { get; set; }
            public int? ColorCode { get; set; }
            public decimal? Width { get; set; }
            public decimal? Gpd { get; set; }
            public decimal? MinGpd { get; set; }
            public decimal? MaxGpd { get; set; }
            public decimal? StdStrength { get; set; }
            public decimal? StrengthMax { get; set; }
            public decimal? StrengthMin { get; set; }
            public decimal? StdElong { get; set; }
            public decimal? ElongMax { get; set; }
            public decimal? ElongMin { get; set; }
            public decimal? UnlamFab { get; set; }
            public decimal? LamFab { get; set; }
            public int? Active { get; set; }
        }

        [HttpGet]
        public async Task<JsonResult> GetExistOrNot(string inputData)
        {
            if (string.IsNullOrWhiteSpace(inputData))
            {
                return Json(new { status = false, message = "Invalid Name!" });
            }
            var response = await _repository.GetExistOrNotAsync(inputData);
            return Json(new { status = response.status, exists = response.data });
        }

        [HttpPost]
        public async Task<IActionResult> SaveTape_NFabricMast([FromBody] TapeNFabricModel model)
        {
            if (model == null)
            {
                return Json(new { status = false, message = "Invalid Data!" });
            }
            var response = await _repository.SaveTapeAndFabricAsync(model);
            return Json(new { status = response.status, message = response.message });
        }

        [HttpGet]
        public async Task<IActionResult> GetTape_NFabricDetailsById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Json(new { status = false, message = "Invalid Id!" });
            }
            var response = await _repository.GetTapeAndFabricDetailsByIdAsync(id);
            if(response.data != null)
            {
                return Json(new { status = response.status, data = response.data });
            }
            return Json(new { status = response.status, message = response.message });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTape_NFabricMast([FromBody] TapeNFabricModel model)
        {
            if (model == null)
            {
                return Json(new { status = false, message = "Data update Failed" });
            }
            var response = await _repository.UpdateTapeAndFabricAsync(model);
            return Json(new { status = response.status, message = response.message });

        }

    }
}
