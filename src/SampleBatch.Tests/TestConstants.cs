using System;

namespace SampleBatch.Tests
{
    public static class TestConstants
    {
        public static readonly string ConnectionString = "Server=tcp:localhost,1433;Initial Catalog=sample-batch-tests;Persist Security Info=False;User ID=sa;Password=MTsample1;MultipleActiveResultSets=True;TrustServerCertificate=True;Connection Timeout=30;";
        //public static readonly string ConnectionString = "Data Source=(LocalDb)\\MSSQLLocalDB;Initial Catalog=sample-batch;Trusted_Connection=True;MultipleActiveResultSets=True;Connection Timeout=30;";
        public static readonly Uri QuartzAddress = new Uri("loopback://localhost/quartz");
    }
}
