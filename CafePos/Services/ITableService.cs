using CafePos.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public interface ITableService
    {
        Task<IEnumerable<Table>> GetAllTablesAsync();
        Task<IEnumerable<Table>> GetActiveTablesAsync();
        Task<Table?> GetTableByIdAsync(int id);
        Task CreateTableAsync(Table table);
        Task UpdateTableAsync(Table table);
        Task ToggleTableStatusAsync(int id);
    }
}