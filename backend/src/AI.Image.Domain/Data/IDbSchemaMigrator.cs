using System.Threading.Tasks;

namespace AI.Image.Data;

public interface IDbSchemaMigrator
{
    Task MigrateAsync();
}
