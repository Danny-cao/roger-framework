using RabbitMQ.Client;
using System;

namespace RabbitMQBus.Bus
{
    public class RabbitMQBusContextBuilder : IBusContextBuilder
    {
        public string ExchangeName { get; private set; }
        public string HostName { get; private set; }
        public int Port { get; private set; }
        public string UserName { get; private set; }
        public string Password { get; private set; }

        public RabbitMQBusContextBuilder()
        {
            ExchangeName = "RogerFramework.EventBus";
            HostName = "localhost";
            Port = 5672;
            UserName = "guest";
            Password = "guest";
        }

        public RabbitMQBusContextBuilder WithExchange(string exchangeName)
        {
            ExchangeName = exchangeName;
            return this;
        }

        public RabbitMQBusContextBuilder WithAddress(string hostName, int port)
        {
            HostName = hostName;
            Port = port;
            return this;
        }

        public RabbitMQBusContextBuilder WithCredentials(string userName, string password)
        {
            UserName = userName;
            Password = password;
            return this;
        }

        public RabbitMQBusContextBuilder ReadFromEnvironmentVariables()
        {
            ExchangeName = Environment.GetEnvironmentVariable("eventbus-exchangename") ?? ExchangeName;
            HostName = Environment.GetEnvironmentVariable("eventbus-hostname") ?? HostName;
            if (int.TryParse(Environment.GetEnvironmentVariable("eventbus-port"), out int port))
            {
                Port = port;
            }
            UserName = Environment.GetEnvironmentVariable("eventbus-username") ?? UserName;
            Password = Environment.GetEnvironmentVariable("eventbus-password") ?? Password;
            return this;
        }

        public IBusContext CreateContext()
        {
            IConnectionFactory factory = new ConnectionFactory
            {
                HostName = HostName,
                Port = Port,
                UserName = UserName,
                Password = Password,
            };

            IConnection connection = factory.CreateConnection();

            return new RabbitMQBusContext(ExchangeName, connection);
        }
    }
}
