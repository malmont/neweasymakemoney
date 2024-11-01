

namespace Easymakemoney.Views.Statistiques

{
    public partial class OrderNumberPage: ContentPage
    {
        private readonly  OrderCountUseCase _orderCountUseCase;
         private readonly OrderCountViewModel _viewModel;
        public OrderNumberPage(OrderCountViewModel viewModel, OrderCountUseCase orderCountUseCase)
        {
            InitializeComponent();
            _viewModel = viewModel;
            this.BindingContext = viewModel;
            _orderCountUseCase = orderCountUseCase;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var data = await _orderCountUseCase.GetOrderStatistics();
            _viewModel.LoadOderCount(data);
            _viewModel.UpdateRevenueData(1);
        
        }
    }
}