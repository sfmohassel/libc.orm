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

using System;
using System.ComponentModel.DataAnnotations;
using System.Data;
using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.Resources;
namespace libc.orm.DatabaseMigration.Abstractions.Expressions {
    /// <summary>
    ///     Expression that allows the execution of DB operations
    /// </summary>
    public class PerformDBOperationExpression : MigrationExpressionBase {
        /// <summary>
        ///     Gets or sets the operation to be executed for a given database connection
        /// </summary>
        [Required(ErrorMessageResourceType = typeof(Dmt), ErrorMessageResourceName = nameof(Dmt.OperationCannotBeNull))]
        public Action<IDbConnection, IDbTransaction> Operation { get; set; }
        public override void ExecuteWith(IProcessor processor) {
            processor.Process(this);
        }
    }
}