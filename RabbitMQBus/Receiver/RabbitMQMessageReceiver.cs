using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQBus.Bus;
using System;
using System.Collections.Generic;

namespace RabbitMQBus.Receiver
{
    public class RabbitMQMessageReceiver : IMessageReceiver
    {
        private readonly RabbitMQBusContext _context;
        private IModel _channel;
        private bool _hasStartedReceivingMessages;
        private bool _hasStartedHandlingMessages;
        private bool _isDisposed;

        public string QueueName { get; private set; }
        public IEnumerable<string> TopicExpressions { get; set; }

        public RabbitMQMessageReceiver(RabbitMQBusContext context, string queueName, IEnumerable<string> topicExpressions) 
        {
            _context = context;
            QueueName = queueName;
            TopicExpressions = topicExpressions;

            _hasStartedReceivingMessages = false;
            _hasStartedHandlingMessages = false;

            _isDisposed = false;

            _channel = context.Connection.CreateModel();
        }

        public void StartReceivingMessages()
        {
            CanStartReceivingMessages();
            _hasStartedReceivingMessages = true;

            _channel.ExchangeDeclare(_context.ExchangeName, ExchangeType.Topic);
            _channel.QueueDeclare(
                queue: QueueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null);

            foreach (var topicExpression in TopicExpressions) 
            {
                _channel.QueueBind(QueueName, _context.ExchangeName, topicExpression);
            }
        }

        public void StartHandlingMessages(EventMessageReceivedCallback Callback)
        {
            CanStartHandlingMessages();
            _hasStartedHandlingMessages = true;

            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += (object sender, BasicDeliverEventArgs e) =>
            {
                var eventMesage = ConvertToEventMessage(e);
                Callback.Invoke(eventMesage);

            };
            _channel.BasicConsume(QueueName, autoAck: true, consumer);
        }


        public void Dispose()
        {
            _isDisposed = true;
            _channel?.Dispose();
        }


        private void CanStartReceivingMessages()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMQMessageReceiver));
            }
            else if (_hasStartedReceivingMessages)
            {
                throw new BusException("Cannot call 'StartReceivingMessages' multiple times.");
            }
        }


        private void CanStartHandlingMessages()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(RabbitMQMessageReceiver));
            }
            else if (!_hasStartedReceivingMessages)
            {
                throw new BusException("Before calling 'StartHandlingMessages', call 'StartReceivingMessages' first to declare queue and topics.");
            }
            else if (_hasStartedHandlingMessages)
            {
                throw new BusException("Cannot call 'StartHandlingMessages' multiple times.");
            }
        }

        private EventMessage ConvertToEventMessage(BasicDeliverEventArgs e)
        {
            if (!Guid.TryParse(e.BasicProperties.CorrelationId, out Guid guid))
            {
                guid = Guid.Empty;
            }
            return new EventMessage
            {
                Topic = e.RoutingKey,
                CorrelationId = guid,
                Timestamp = e.BasicProperties.Timestamp.UnixTime,
                EventType = e.BasicProperties.Type,
                Body = e.Body.ToArray(),
            };
        }
    }
}
