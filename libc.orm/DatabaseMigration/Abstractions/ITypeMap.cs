using System;
using System.Data;
using JetBrains.Annotations;
namespace libc.orm.DatabaseMigration.Abstractions {
    /// <summary>
    ///     A map of <see cref="DbType" /> to an SQL type
    /// </summary>
    public interface ITypeMap {
        /// <summary>
        ///     Get the SQL type for a <see cref="DbType" />
        /// </summary>
        /// <param name="type">The <see cref="DbType" /> to get the SQL type for</param>
        /// <param name="size">The requested size (in DB lingua: precision)</param>
        /// <param name="precision">The requested precision (in DB lingua: scale)</param>
        /// <returns>The SQL type</returns>
        [Obsolete]
        string GetTypeMap(DbType type, int size, int precision);
        /// <summary>
        ///     Get the SQL type for a <see cref="DbType" />
        /// </summary>
        /// <param name="type">The <see cref="DbType" /> to get the SQL type for</param>
        /// <param name="size">The requested size (in DB lingua: precision)</param>
        /// <param name="precision">The requested precision (in DB lingua: scale)</param>
        /// <returns>The SQL type</returns>
        
        string GetTypeMap(DbType type, int? size, int? precision);
    }
}