using System;
using System.Threading.Tasks;
using RabbitMQ.Client;
using RabbitMQBus.Bus;

namespace RabbitMQBus.Publisher
{
    public class RabbitMQMessageSender : IMessageSender
    {
        private readonly RabbitMQBusContext _context;
        private readonly IModel _channel;

        public RabbitMQMessageSender(RabbitMQBusContext context)
        {
            _context = context;
            _channel = _context.Connection.CreateModel();
            _channel.ExchangeDeclare(_context.ExchangeName, ExchangeType.Topic);
        }


        public Task SendMessageAsync(EventMessage message)
        {
            return Task.Run(() =>
            {
                IBasicProperties properties = _channel.CreateBasicProperties();
                properties.CorrelationId = message.CorrelationId.ToString();
                properties.Timestamp = new AmqpTimestamp(message.Timestamp);
                properties.Type = message.EventType ?? "void";

                _channel.BasicPublish(
                    exchange: _context.ExchangeName,
                    routingKey: message.Topic,
                    basicProperties: properties,
                    body: new ReadOnlyMemory<byte>(message.Body));
            });
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
