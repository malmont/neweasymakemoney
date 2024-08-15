namespace Easymakemoney.UseCase.LisCommandeUseCase
{
    public class CreateCommandUseCase
    {
        private readonly IListCommandService _listCommandService;

        public CreateCommandUseCase(IListCommandService listCommandService)
        {
            _listCommandService = listCommandService;
        }

        public async Task<bool> ExecuteAsync(ListCommand newCommand,int id)
        {
            var result = await _listCommandService.PostCommandes(newCommand,id);
            if (result)
            {
                Debug.WriteLine("Command created successfully.");
                return true;
            }
            else
            {
                Debug.WriteLine("Failed to create command.");
                return false;
            }
        }
    }
}