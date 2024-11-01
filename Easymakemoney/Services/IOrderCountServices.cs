namespace Easymakemoney.Services
{
    public interface IOrderCountServices
    {
      Task<OrderStatistics> GetOrderStatistics();
    }
}