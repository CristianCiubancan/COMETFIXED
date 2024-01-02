using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Account.Database.Repositories
{
    /// <summary>
    ///     Repository for defining data access layer (DAL) logic for the account table. Allows
    ///     the server to fetch account details for player authentication. Accounts are fetched
    ///     on demand for each player authentication request.
    /// </summary>
    public static class AccountsRepository
    {
        /// <summary>
        ///     Fetches an account record from the database using the player's username as a
        ///     unique key for selecting a single record.
        /// </summary>
        /// <param name="username">Username to pull account info for</param>
        /// <returns>Returns account details from the database.</returns>
        public static async Task<DbAccount> FindAsync(string username)
        {
            await using var db = new ServerDbContext();
            return await db.Accounts
                           .Where(x => x.Username == username)
                           .SingleOrDefaultAsync();
        }

        public static async Task<DbAccount> FindAsync(uint identity)
        {
            await using var db = new ServerDbContext();
            return await db.Accounts
                           .Where(x => x.AccountID == identity)
                           .SingleOrDefaultAsync();
        }
    }
}