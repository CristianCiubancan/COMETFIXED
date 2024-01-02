using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Comet.Database;
using Comet.Database.Entities;
using Comet.Shared;
using Microsoft.EntityFrameworkCore;

namespace Comet.Account.Database
{
    /// <summary>
    ///     Server database client context implemented using Entity Framework Core, an open
    ///     source object-relational mapping framework for ADO.NET. Substitutes in MySQL
    ///     support through a third-party framework provided by Pomelo Foundation.
    /// </summary>
    public class ServerDbContext : AbstractDbContext
    {
        // Table Definitions
        public virtual DbSet<DbAccount> Accounts { get; set; }
        public virtual DbSet<DbRealm> Realms { get; set; }
        public virtual DbSet<DbRealmStatus> RealmStatus { get; set; }
        public virtual DbSet<DbRecordUser> RecordUsers { get; set; }

        /// <summary>
        ///     Typically called only once when the first instance of the context is created.
        ///     Allows for model building before the context is fully initialized, and used
        ///     to initialize composite keys and relationships.
        /// </summary>
        /// <param name="builder">Builder for creating models in the context</param>
        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.Entity<DbAccount>(e => e.HasKey(x => x.AccountID));
            builder.Entity<DbAccount>().Property(x => x.StatusID).HasConversion<ushort>();

            builder.Entity<DbRealm>(e => e.HasKey(x => x.RealmID));
        }

        /// <summary>
        ///     Tests that the database connection is alive and configured.
        /// </summary>
        public static bool Ping()
        {
            try
            {
                using var ctx = new ServerDbContext();
                return ctx.Database.CanConnect();
            }
            catch (Exception ex)
            {
                _ = Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }


        public static async Task<bool> SaveAsync<T>(T entity) where T : class
        {
            try
            {
                await using var db = new ServerDbContext();
                db.Update(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public static async Task<bool> SaveAsync<T>(List<T> entity) where T : class
        {
            try
            {
                await using var db = new ServerDbContext();
                db.UpdateRange(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public static async Task<bool> DeleteAsync<T>(T entity) where T : class
        {
            try
            {
                await using var db = new ServerDbContext();
                db.Remove(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public static async Task<bool> DeleteAsync<T>(List<T> entity) where T : class
        {
            try
            {
                await using var db = new ServerDbContext();
                db.RemoveRange(entity);
                await db.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public static async Task<string> ScalarAsync(string query)
        {
            await using var db = new ServerDbContext();
            DbConnection connection = db.Database.GetDbConnection();
            ConnectionState state = connection.State;

            string result;
            try
            {
                if ((state & ConnectionState.Open) == 0)
                    await connection.OpenAsync();

                DbCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = query;

                result = (await cmd.ExecuteScalarAsync())?.ToString();
            }
            finally
            {
                if (state != ConnectionState.Closed)
                    await connection.CloseAsync();
            }

            return result;
        }
    }
}