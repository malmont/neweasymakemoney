namespace Easymakemoney.UseCase.ListColorsUseCase
{
    public class GetListColorsUseCase 
    {
        private readonly IListColorsServices _listColorsServices;

        public GetListColorsUseCase(IListColorsServices listColorsServices)
        {
            _listColorsServices = listColorsServices;
        }

        public async Task<ObservableCollection<ColorsProduct>> GetListColors()
        {
            return await _listColorsServices.GetColorsList();
        }
    }
}