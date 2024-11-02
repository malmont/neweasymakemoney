namespace Easymakemoney.Models
{
    public interface IFraisData
    {
        public FraisDetails TotalFrais { get; set; }
        public string PeriodLabel { get; }
        public Color BackgroundColor { get; set; }
    }
}