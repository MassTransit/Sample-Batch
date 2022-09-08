﻿using MassTransit.EntityFrameworkCoreIntegration;

namespace SampleBatch.Components
{
    public class CustomSqlLockStatementProvider :
        SqlLockStatementProvider
    {
        const string DefaultSchemaName = "dbo";

        public CustomSqlLockStatementProvider()
            : base(DefaultSchemaName, new CustomSqlLockStatementFormatter())
        {
        }
    }
}
