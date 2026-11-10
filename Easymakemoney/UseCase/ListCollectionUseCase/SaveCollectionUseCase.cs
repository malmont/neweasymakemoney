
using Easymakemoney.Services.Session;
using Easymakemoney.Utilities;

namespace Easymakemoney.UseCase.ListCollectionUsecase
{
public class SaveCollectionUseCase
{
    private readonly CreateCollectionUseCase _createCollectionUseCase;
    private readonly ISessionService _sessionService;
    // private readonly ListNewCollectionViewModel _collectionViewModel;

    public SaveCollectionUseCase(CreateCollectionUseCase createCollectionUseCase, ISessionService sessionService)
    {
        _createCollectionUseCase = createCollectionUseCase;
        _sessionService = sessionService;
        // _collectionViewModel = collectionViewModel;
    }

    public async Task<bool> ExecuteAsync(CollectionFormModel collectionForm)
    {
        var user = await _sessionService.GetUserAsync();
        var userId = user?.Id ?? 0;
        var newCollection = new ListCollection
        {
            budgetCollection = MoneyHelper.ToCents(collectionForm.BudgetCollection),
            startDateCollection = collectionForm.StartDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            endDateCollection = collectionForm.EndDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            del = collectionForm.Del,
            nomCollection = collectionForm.NomCollection,
            photoCollection = collectionForm.PhotoCollection,
            userId = userId
        };

        var result = await _createCollectionUseCase.ExecuteAsync(newCollection);
      
        return result;
    }
}
  }
