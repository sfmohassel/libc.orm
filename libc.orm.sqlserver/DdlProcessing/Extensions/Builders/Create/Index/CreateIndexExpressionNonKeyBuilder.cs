#region License

// Copyright (c) 2007-2018, Sean Chambers and the FluentMigrator Project
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

using libc.orm.DatabaseMigration.Abstractions;
using libc.orm.DatabaseMigration.Abstractions.Builders.Create.Index;
using libc.orm.sqlserver.DdlProcessing.Extensions.SqlServer;
namespace libc.orm.sqlserver.DdlProcessing.Extensions.Builders.Create.Index {
    internal class CreateIndexExpressionNonKeyBuilder : ICreateIndexNonKeyColumnSyntax {
        public CreateIndexExpressionNonKeyBuilder(ICreateIndexOnColumnSyntax expression,
            ISupportAdditionalFeatures supportAdditionalFeatures) {
            Expression = expression;
            SupportAdditionalFeatures = supportAdditionalFeatures;
        }
        public ICreateIndexOnColumnSyntax Expression { get; }
        public ISupportAdditionalFeatures SupportAdditionalFeatures { get; }
        public ICreateIndexNonKeyColumnSyntax Include(string columnName) {
            SupportAdditionalFeatures.Include(columnName);
            return this;
        }
    }
}