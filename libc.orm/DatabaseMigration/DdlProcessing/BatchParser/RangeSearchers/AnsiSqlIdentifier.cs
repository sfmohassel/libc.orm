﻿#region License

// Copyright (c) 2018, Fluent Migrator Project
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

namespace libc.orm.DatabaseMigration.DdlProcessing.BatchParser.RangeSearchers {
    /// <summary>
    ///     A range searcher for ANSI-style SQL identifiers
    /// </summary>
    public sealed class AnsiSqlIdentifier : StringWithNoEscape {
        /// <summary>
        ///     Initializes a new instance of the <see cref="AnsiSqlIdentifier" /> class.
        /// </summary>
        public AnsiSqlIdentifier()
            : base("\"") {
        }
    }
}