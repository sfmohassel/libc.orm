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

using System.Collections.Generic;
using JetBrains.Annotations;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.postgres.DdlGeneration.Postgres92;
using libc.orm.postgres.DdlProcessing.Postgres;
using Microsoft.Extensions.Logging;
namespace libc.orm.postgres.DdlProcessing.Postgres92 {
    public class Postgres92Processor : PostgresProcessor {
        public Postgres92Processor([NotNull] Postgres92Generator generator,
            [NotNull] ILogger<PostgresProcessor> logger,
            [NotNull] ProcessorOptions options,
            [NotNull] PostgresOptions pgOptions)
            : base(generator, logger, options, pgOptions) {
        }
        public override string DatabaseType => "Postgres92";
        public override IList<string> DatabaseTypeAliases { get; } = new List<string> {
            "PostgreSQL92"
        };
    }
}