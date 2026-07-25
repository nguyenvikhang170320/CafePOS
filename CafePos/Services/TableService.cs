using CafePos.Data;
using CafePos.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafePos.Services
{
    public class TableService : ITableService
    {
        private readonly CafePosDbContext _context;

        public TableService(CafePosDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Table>> GetAllTablesAsync()
        {
            return await _context.Tables.ToListAsync();
        }

        public async Task<IEnumerable<Table>> GetActiveTablesAsync()
        {
            return await _context.Tables.Where(t => t.IsActive).ToListAsync();
        }

        public async Task<Table?> GetTableByIdAsync(int id)
        {
            return await _context.Tables.FindAsync(id);
        }

        public async Task CreateTableAsync(Table table)
        {
            _context.Tables.Add(table);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateTableAsync(Table table)
        {
            _context.Entry(table).State = EntityState.Modified;
            await _context.SaveChangesAsync();
        }

        public async Task ToggleTableStatusAsync(int id)
        {
            var table = await _context.Tables.FindAsync(id);
            if (table != null)
            {
                table.IsActive = !table.IsActive;
                await _context.SaveChangesAsync();
            }
        }
    }
}