namespace Easymakemoney.Services
{
    public interface IHomeTilesService
    {
        Task<List<HomeTile>> GetTilesAsync(string locale);
        Task DismissAsync(string tileId);
    }
}
