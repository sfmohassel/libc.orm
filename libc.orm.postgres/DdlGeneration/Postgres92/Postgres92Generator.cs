#region License

// Copyright (c) 2019, FluentMigrator Project
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

using JetBrains.Annotations;
using libc.orm.DatabaseMigration.DdlGeneration;
using libc.orm.postgres.DdlGeneration.Postgres;
namespace libc.orm.postgres.DdlGeneration.Postgres92 {
    public class Postgres92Generator : PostgresGenerator {
        public Postgres92Generator([NotNull] PostgresQuoter quoter,
            [NotNull] GeneratorOptions generatorOptions)
            : base(quoter, generatorOptions, new Postgres92TypeMap()) {
        }
    }
}