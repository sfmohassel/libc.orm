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

using libc.orm.DatabaseMigration.DdlGeneration;
using libc.orm.DatabaseMigration.DdlProcessing;
using libc.orm.sqlserver.DdlGeneration;
using Microsoft.Extensions.Logging;

namespace libc.orm.sqlserver.DdlProcessing
{
    public class SqlServer2016ProcessorFactory : IProcessorFactory
    {
        public IProcessor Create(ProcessorOptions options, ILogger logger)
        {
            return new SqlServer2016Processor(logger,
                new SqlServer2016Generator(new SqlServer2008Quoter(), new GeneratorOptions()), options);
        }
    }
}