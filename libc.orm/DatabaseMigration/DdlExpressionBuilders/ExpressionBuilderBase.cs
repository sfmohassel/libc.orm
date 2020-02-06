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

using libc.orm.DatabaseMigration.Abstractions.Expressions.Base;
namespace libc.orm.DatabaseMigration.DdlExpressionBuilders {
    /// <summary>
    ///     The base class for builders with underlying expressions
    /// </summary>
    /// <typeparam name="T">A type that implements <see cref="IMigrationExpression" /></typeparam>
    public abstract class ExpressionBuilderBase<T>
        where T : class, IMigrationExpression {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ExpressionBuilderBase{T}" /> class.
        /// </summary>
        /// <param name="expression">The underlying expression</param>
        protected ExpressionBuilderBase(T expression) {
            Expression = expression;
        }
        /// <summary>
        ///     Gets the underlying migration expression
        /// </summary>
        public T Expression { get; }
    }
}