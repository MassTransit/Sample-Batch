namespace SampleBatch.Api
{
    public class AppConfig
    {
        public RabbitMqSettings RabbitMq { get; set; }
        public AzureServiceBusSettings AzureServiceBus { get; set; }
    }

    public class RabbitMqSettings
    {
        public string HostAddress { get; set; }
        public int Port { get; private set; }
        public string VirtualHost { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class AzureServiceBusSettings
    {
        public string ConnectionString { get; set; }
    }
}
