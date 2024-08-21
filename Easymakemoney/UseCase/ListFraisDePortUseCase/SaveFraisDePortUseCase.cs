namespace Easymakemoney.UseCase.ListFraisDePortUseCase
{
    public class SaveFraisDePortUseCase

    {
        private readonly CreateFraisDePortUseCase _createFraisDePortUseCase;

        public SaveFraisDePortUseCase(CreateFraisDePortUseCase createFraisDePortUseCase)
        {
            _createFraisDePortUseCase = createFraisDePortUseCase;
        }
        public async Task<bool> ExecuteAsync(FraisDePortFormModel FraisDePortForm, int commandId)
        {
            var newFraisDePort = new FraisDePort
            {
                name = FraisDePortForm.Name,
                facture = FraisDePortForm.Facture,
                tracknumber = FraisDePortForm.Tracknumber,
                image = FraisDePortForm.Image,
                transporteur= FraisDePortForm.SelectedTransporteur,
            };
            return await _createFraisDePortUseCase.ExecuteAsync(newFraisDePort, commandId);
        }

    }

}