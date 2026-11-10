

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

        private readonly StatistiquesPageLoader _loader = new();

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _loader.RunOnceAsync(async () =>
            {
                var data = await _orderCountUseCase.GetOrderStatistics();
                _viewModel.LoadOderCount(data);
                _viewModel.UpdateRevenueData(1);
            });
        }
    }
}