using System.Dynamic;
using travelexpensemanagement.Models.QualityControl.Transaction;

namespace travelexpensemanagement.Repositories.Interfaces.QualityControl.Transaction
{
    public interface IQCTemperatureEntryRepository
    {
        Task<RepositoryResponse> saveOrUpdate(QcTemperature model);
        Task<RepositoryResponseData<dynamic>> ImportDataByReading(int timeInterval, string type, string shift, int deptCode, string vType);
        Task<RepositoryResponseData<QCTempEntryDto>> GetById(string id);
        Task<RepositoryResponseData<bool>> getExist(DateTime V_DATE, DateTime V_TIME, string SHIFT, int plantCode, int VNo);
        Task<RepositoryResponseData<List<testParamDto>>> FillDataByLineNo(int deptCode);
        
        public class QCTempEntryDto
        {
            public List<ExpandoObject> Header { get; set; }
            public List<ExpandoObject> Detail { get; set; }
        }
        public class testParamDto
        {
            public int? ROOM_CODE { get; set; }
            public string? RoomName { get; set; }
            public string? TYPE { get; set; }
        }
    }
}
