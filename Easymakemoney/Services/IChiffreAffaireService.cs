namespace Easymakemoney.Services
{
    public interface IChiffreAffaireService 
    {
        Task<RevenueStatistics> GetRevenueStatistics();
    }
    
       
}