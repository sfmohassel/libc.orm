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
using libc.orm.postgres.DdlGeneration.Postgres;
using Microsoft.Extensions.Logging;

namespace libc.orm.postgres.DdlProcessing.Postgres
{
    public class PostgresProcessorFactory : IProcessorFactory
    {
        public IProcessor Create(ProcessorOptions options, ILogger logger)
        {
            var postgresOptions = new PostgresOptions
            {
                ForceQuote = true
            };

            var quoter = new PostgresQuoter(postgresOptions);
            var generatorOptions = new GeneratorOptions();
            var generator = new PostgresGenerator(quoter, generatorOptions, new PostgresTypeMap());

            return new PostgresProcessor(generator, logger, options, postgresOptions);
        }
    }
}