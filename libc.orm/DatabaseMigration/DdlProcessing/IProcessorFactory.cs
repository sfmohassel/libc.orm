using Microsoft.Extensions.Logging;
namespace libc.orm.DatabaseMigration.DdlProcessing {
    public interface IProcessorFactory {
        IProcessor Create(ProcessorOptions options, ILogger logger);
    }
}