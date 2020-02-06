using libc.orm.DatabaseMigration.DdlGeneration;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.sqlserver.DdlGeneration;
using Microsoft.Extensions.Logging;
namespace libc.orm.sqlserver.DdlProcessing {
    public class SqlServer2014ProcessorFactory : IProcessorFactory {
        public IProcessor Create(ProcessorOptions options, ILogger logger) {
            return new SqlServer2014Processor(logger,
                new SqlServer2014Generator(new SqlServer2008Quoter(), new GeneratorOptions()), options);
        }
    }
}