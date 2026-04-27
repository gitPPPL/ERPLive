namespace travelexpensemanagement.Models.Production.Master.ChemicalRecipe
{
    public class ChemicalRecipeMaster
    {
        public int? SNo { get; set; }
        public string? V_TYPE { get; set; }
        public DateTime? V_DATE { get; set; }
        public int? DEPT_CODE {  get; set; }
        public string? DEPT_NAME { get; set; }
        public int? V_NO { get; set; }
        public string? Action { get; set; }
        public string ? DOC_ID {  get; set; }

        public List<ChemicalRecipeDetail> Details { get; set; }
    }
    public class ChemicalRecipeDetail
    {
        public int? ITEM_CODE { get; set; }
        public string? ITEM_NAME { get; set; }
        public decimal? PER { get; set; }
    }
}
