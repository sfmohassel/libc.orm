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

namespace libc.orm.DatabaseMigration.Abstractions.Builders.Create.Index
{
    /// <summary>
    ///     Definition of the schema the table belongs to
    /// </summary>
    public interface ICreateIndexOnColumnOrInSchemaSyntax : ICreateIndexOnColumnSyntax
    {
        /// <summary>
        ///     Defines the schema of the table to create the index for
        /// </summary>
        /// <param name="schemaName">The schema name</param>
        /// <returns>Definition of index columns</returns>
        ICreateIndexOnColumnSyntax InSchema(string schemaName);
    }
}