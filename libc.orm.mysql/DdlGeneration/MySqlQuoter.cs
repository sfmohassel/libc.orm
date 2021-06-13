#region License

// Copyright (c) 2007-2018, FluentMigrator Project
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

using System;
using libc.orm.DatabaseMigration.Abstractions.Builders;
using libc.orm.DatabaseMigration.DdlGeneration;

namespace libc.orm.mysql.DdlGeneration
{
    public class MySqlQuoter : GenericQuoter
    {
        public override string OpenQuote => "`";
        public override string CloseQuote => "`";

        public override string QuoteValue(object value)
        {
            return base.QuoteValue(value).Replace(@"\", @"\\");
        }

        public override string FromTimeSpan(TimeSpan value)
        {
            return string.Format("{0}{1:00}:{2:00}:{3:00}{0}"
                , ValueQuote
                , value.Hours + value.Days * 24
                , value.Minutes
                , value.Seconds);
        }

        public override string FormatSystemMethods(SystemMethods value)
        {
            switch (value)
            {
                case SystemMethods.NewGuid:
                case SystemMethods.NewSequentialId:
                    return "(SELECT UUID())";
                case SystemMethods.CurrentDateTime:
                    return "CURRENT_TIMESTAMP";
                case SystemMethods.CurrentUTCDateTime:
                    return "UTC_TIMESTAMP";
                case SystemMethods.CurrentUser:
                    return "CURRENT_USER()";
            }

            return base.FormatSystemMethods(value);
        }

        public override string QuoteSchemaName(string schemaName)
        {
            // This database doesn't support schemata
            return string.Empty;
        }
    }
}