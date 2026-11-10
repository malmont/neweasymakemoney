using Easymakemoney.Utilities;

namespace Easymakemoney.UseCase.LisCommandeUseCase
{
public class SaveCommandUseCase
{
    private readonly CreateCommandUseCase _createCommandUseCase;

    public SaveCommandUseCase(CreateCommandUseCase createCommandUseCase)
    {
        _createCommandUseCase = createCommandUseCase;
     
    }

    public async Task<bool> ExecuteAsync(CommandFormModel commandForm,int CollectionId)
    {
        var newCommand = new ListCommand
        {
            budget = MoneyHelper.ToCents(commandForm.Budget),
            date = commandForm.Date.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            name = commandForm.Name,
            photo = commandForm.Photo,
            collectionId =  CollectionId,
            fournisseur = commandForm.SelectedFournisseurs,
            exchangeRate = commandForm.NeedsExchangeRate ? commandForm.ExchangeRate : null
        };

        var result = await _createCommandUseCase.ExecuteAsync(newCommand, CollectionId);

        return result;
    }
}
}
