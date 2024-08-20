namespace Easymakemoney.UseCase.ListFournisseurUseCase
{
    public class SaveFournisseurUseCase
    {
        private readonly CreateFournisseurUseCase _createFournisseurUseCase;

        public SaveFournisseurUseCase(CreateFournisseurUseCase createFournisseurUseCase)
        {
            _createFournisseurUseCase = createFournisseurUseCase;
        }

        public async Task<bool> ExecuteAsync(FournisseurFormModel FourniseursForm, int commandId)
        {
            var newFournisseur = new ListFournisseur
            {
            
                name = FourniseursForm.Name,
                adresse = FourniseursForm.Adresse,
                ville = FourniseursForm.Ville,
                pays = FourniseursForm.Pays,
                photo = FourniseursForm.Photo,
                tel = FourniseursForm.Tel
                
            
            };

            return await _createFournisseurUseCase.ExecuteAsync(newFournisseur, commandId);

        }
    }
}