using System.Text.Json.Nodes;

namespace travelexpensemanagement.Models
{
    public class CommonModel
    {
        public class SaveRequest
        {
            public JsonObject header { get; set; }

            public JsonArray? footer { get; set; }

            public JsonArray? documents { get; set; }

            public JsonArray? EPRDocuments { get; set; }
        }
    }
}
