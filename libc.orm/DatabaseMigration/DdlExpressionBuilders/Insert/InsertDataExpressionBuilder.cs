#region License

//
// Copyright (c) 2007-2018, Sean Chambers <schambers80@gmail.com>
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
using System.ComponentModel;
using libc.orm.DatabaseMigration.Abstractions;
using libc.orm.DatabaseMigration.Abstractions.Builders.Insert;
using libc.orm.DatabaseMigration.Abstractions.Expressions;
using libc.orm.DatabaseMigration.Abstractions.Model;

namespace libc.orm.DatabaseMigration.DdlExpressionBuilders.Insert
{
    /// <summary>
    ///     An expression builder for a <see cref="InsertDataExpression" />
    /// </summary>
    public class InsertDataExpressionBuilder : IInsertDataOrInSchemaSyntax, ISupportAdditionalFeatures
    {
        private readonly InsertDataExpression _expression;

        /// <summary>
        ///     Initializes a new instance of the <see cref="InsertDataExpressionBuilder" /> class.
        /// </summary>
        /// <param name="expression">The underlying expression</param>
        public InsertDataExpressionBuilder(InsertDataExpression expression)
        {
            _expression = expression;
        }

        /// <inheritdoc />
        public IInsertDataSyntax Row(object dataAsAnonymousType)
        {
            var data = ExtractData(dataAsAnonymousType);

            return Row(data);
        }

        /// <inheritdoc />
        public IInsertDataSyntax Row(IDictionary<string, object> data)
        {
            var dataDefinition = new InsertionDataDefinition();
            dataDefinition.AddRange(data);
            _expression.Rows.Add(dataDefinition);

            return this;
        }

        /// <inheritdoc />
        public IInsertDataSyntax InSchema(string schemaName)
        {
            _expression.SchemaName = schemaName;

            return this;
        }

        /// <inheritdoc />
        public IDictionary<string, object> AdditionalFeatures => _expression.AdditionalFeatures;

        private static IDictionary<string, object> ExtractData(object dataAsAnonymousType)
        {
            var data = new Dictionary<string, object>();
            var properties = TypeDescriptor.GetProperties(dataAsAnonymousType);

            foreach (PropertyDescriptor property in properties)
                data.Add(property.Name, property.GetValue(dataAsAnonymousType));

            return data;
        }
    }
}