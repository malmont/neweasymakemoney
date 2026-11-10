using Easymakemoney.UseCase.ListNoteDeFraisUseCase;
using Easymakemoney.Utilities;

namespace Easymakemoney.UseCase.ListNoteDeFraisUseCase
{
    public class SaveNoteDeFraisUseCase
    {
        private readonly CreateNoteDeFraisUseCase _createNoteDeFrais;
        
        public SaveNoteDeFraisUseCase(CreateNoteDeFraisUseCase createNoteDeFrais)
        {
            _createNoteDeFrais = createNoteDeFrais;
        }

        public async Task<bool> ExecuteAsync(NoteDeFraisForModel NoteDeFraisForm,int CollectionId)
    {
        var newNoteDeFrais = new NoteDeFrais
        {
            name = NoteDeFraisForm.Name,
            date = NoteDeFraisForm.Date.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            description = NoteDeFraisForm.Description,
            montant = MoneyHelper.ToCents(NoteDeFraisForm.Montant),
            typeNoteDeFraisId = NoteDeFraisForm.SelectedTypeNoteDeFrais.id,
     
        };

        var result = await _createNoteDeFrais.ExecuteAsync(newNoteDeFrais, CollectionId);

        return result;
    }


    }
}