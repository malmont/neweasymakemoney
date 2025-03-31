

namespace Easymakemoney.UseCase.ListCollectionUsecase
{
public class SaveCollectionUseCase
{
    private readonly CreateCollectionUseCase _createCollectionUseCase;
    private readonly IPreferenceService _preferenceService;
    // private readonly ListNewCollectionViewModel _collectionViewModel;

    public SaveCollectionUseCase(CreateCollectionUseCase createCollectionUseCase, IPreferenceService preferenceService)
    {
        _createCollectionUseCase = createCollectionUseCase;
        _preferenceService = preferenceService;
        // _collectionViewModel = collectionViewModel;
    }

    public async Task<bool> ExecuteAsync(CollectionFormModel collectionForm)
    {
        var user = _preferenceService.GetUserId();
        var newCollection = new ListCollection
        {
            budgetCollection = (collectionForm.BudgetCollection ?? 0) * 100,
            startDateCollection = collectionForm.StartDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            endDateCollection = collectionForm.EndDateCollection.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            del = collectionForm.Del,
            nomCollection = collectionForm.NomCollection,
            photoCollection = collectionForm.PhotoCollection,
            userId = user
        };

        var result = await _createCollectionUseCase.ExecuteAsync(newCollection);
      
        return result;
    }
}
  }
