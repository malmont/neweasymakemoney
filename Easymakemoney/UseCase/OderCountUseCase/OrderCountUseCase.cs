namespace Easymakemoney.UseCase.OrderCountUseCase
{
    public class OrderCountUseCase
    {
        private readonly IOrderCountServices _orderCountServices;

        public OrderCountUseCase(IOrderCountServices orderCountServices)
        {
            _orderCountServices = orderCountServices;
        }

        public async Task<OrderStatistics> GetOrderStatistics()
        {
            return await _orderCountServices.GetOrderStatistics();
        }
    }
}