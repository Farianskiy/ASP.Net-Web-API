
namespace WebSiteElectronicMind.ML.Repositories
{
    public interface IGetCharacteristicRepositories
    {
        Task<string> GetCharacteristicAsync(string characteristic, string name, string type);
        Task<int> GetPolus(string name);
        Task<string> GetTypeEquipment(string name);
    }
}