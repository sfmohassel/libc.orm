#region License

//
// Copyright (c) 2018, Fluent Migrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//   http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//

#endregion

using libc.orm.DatabaseMigration.DdlGeneration;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.sqlite.DdlGeneration;
using libc.orm.sqlite.DdlProcessing.BatchParser;
using Microsoft.Extensions.Logging;
namespace libc.orm.sqlite.DdlProcessing {
    // ReSharper disable once InconsistentNaming
    public class SQLiteProcessorFactory : IProcessorFactory {
        public IProcessor Create(ProcessorOptions options, ILogger logger) {
            var generatorOptions = new GeneratorOptions();
            var generator = new SQLiteGenerator(generatorOptions);
            return new SQLiteProcessor(generator, logger, options, new SQLiteBatchParser());
        }
    }
}