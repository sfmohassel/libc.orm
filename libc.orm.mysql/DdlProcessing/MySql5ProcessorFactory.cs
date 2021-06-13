#region License

// Copyright (c) 2007-2018, FluentMigrator Project
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

#endregion

using libc.orm.DatabaseMigration.DdlGeneration;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.mysql.DdlGeneration;
using Microsoft.Extensions.Logging;

namespace libc.orm.mysql.DdlProcessing
{
    public class MySql5ProcessorFactory : IProcessorFactory
    {
        public IProcessor Create(ProcessorOptions options, ILogger logger)
        {
            var qutoer = new MySqlQuoter();
            var generatorOptions = new GeneratorOptions();
            var generator = new MySql5Generator(new MySqlColumn(new MySql5TypeMap(), qutoer), qutoer, generatorOptions);

            return new MySql5Processor(generator, logger, options);
        }
    }
}