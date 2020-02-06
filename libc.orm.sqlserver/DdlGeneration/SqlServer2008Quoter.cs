#region License

// Copyright (c) 2018, FluentMigrator Project
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

using libc.orm.DatabaseMigration.Abstractions.Builders;
namespace libc.orm.sqlserver.DdlGeneration {
    public class SqlServer2008Quoter : SqlServer2005Quoter {
        public override string FormatSystemMethods(SystemMethods value) {
            switch (value) {
                case SystemMethods.CurrentDateTimeOffset:
                    return "SYSDATETIMEOFFSET()";
            }
            return base.FormatSystemMethods(value);
        }
    }
}