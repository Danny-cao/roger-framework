using System;
using System.Collections.Generic;
using RabbitMQBus.Publisher;
using RabbitMQBus.Receiver;

namespace RabbitMQBus.Bus
{
    public interface IBusContext : IDisposable 
    {
        string ExchangeName { get; }
        IMessageSender CreateMessageSender();
        IMessageReceiver CreateMessageReceiver(string queueName, IEnumerable<string> topicExpressions);
    }
}
