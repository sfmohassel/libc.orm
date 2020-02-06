using libc.orm.DatabaseMigration.DdlGeneration;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.sqlserver.DdlGeneration;
using Microsoft.Extensions.Logging;
namespace libc.orm.sqlserver.DdlProcessing {
    public class SqlServerProcessorFactory : IProcessorFactory {
        private static readonly string[] _dbTypes = {
            "SqlServer"
        };
        public IProcessor Create(ProcessorOptions options, ILogger logger) {
            return new SqlServerProcessor(_dbTypes,
                new SqlServer2016Generator(new SqlServer2008Quoter(), new GeneratorOptions()), logger, options);
        }
    }
}