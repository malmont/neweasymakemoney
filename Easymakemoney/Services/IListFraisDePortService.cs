namespace Easymakemoney.Services
{
    public interface IListFraisDePortService
    {
        Task<FraisDePort>GetListFraisDePort(int commandId);
        Task<bool> PostFraisDePort(FraisDePort newFraisDePort, int commandId);
        Task<bool> DeleteFraisDePort(int id);
    }
  
    
}