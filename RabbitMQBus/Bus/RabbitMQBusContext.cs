using RabbitMQ.Client;
using RabbitMQBus.Publisher;
using RabbitMQBus.Receiver;
using System;
using System.Collections.Generic;

namespace RabbitMQBus.Bus
{
    public class RabbitMQBusContext : IBusContext
    {
        public string ExchangeName { get; private set; }
        public IConnection Connection { get; private set;  }

        public RabbitMQBusContext(string exchangeName, IConnection connection) 
        {
            ExchangeName = exchangeName;
            Connection = connection;
        }

        public IMessageReceiver CreateMessageReceiver(string queueName, IEnumerable<string> topicExpressions)
        {
            return new RabbitMQMessageReceiver(this, queueName, topicExpressions);
        }

        public IMessageSender CreateMessageSender()
        {
            return new RabbitMQMessageSender(this);
        }

        public void Dispose()
        {
            Connection?.Dispose();
        }
    }
}
