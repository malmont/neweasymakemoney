namespace  Easymakemoney.UseCase.DashBoardCollectionUseCase
{
    public class GetDashBoardCollectionUseCase 
    {
        private readonly IDashBoardCollectionService _dashBoardCollectionService;
        public GetDashBoardCollectionUseCase(IDashBoardCollectionService dashBoardCollectionService)
        {
            _dashBoardCollectionService = dashBoardCollectionService;
        }
        public async Task<DashBoardCollection> ExecuteAsync(int CollectionId)
        {
            return await _dashBoardCollectionService.GetDashBoardCollection(CollectionId);
        }
    }
}