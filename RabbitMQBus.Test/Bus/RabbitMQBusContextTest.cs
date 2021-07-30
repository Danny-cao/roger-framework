using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitMQ.Client;
using RabbitMQBus.Bus;
using RabbitMQBus.Publisher;
using RabbitMQBus.Receiver;
using System.Linq;

namespace RabbitMQBus.Test.Bus
{
    [TestClass]
    public class RabbitMQBusContextTest
    {
        [TestMethod]
        public void RabbitMQBusContextShouldSetConnectionAndExchange()
        {
            // Arrange
            Mock<IConnection> connectionMock = new Mock<IConnection>(MockBehavior.Strict);
            string exchangeName = "Webshop.ExchangeName";

            // Act
            var target = new RabbitMQBusContext(exchangeName, connectionMock.Object);

            // Assert
            Assert.AreEqual(connectionMock.Object, target.Connection);
            Assert.AreEqual("Webshop.ExchangeName", target.ExchangeName);
        }


        [TestMethod]
        public void CreateMessageSenderShouldCreateRabbitMQMessageSender()
        {
            // Arrange
            Mock<IConnection> ConnectionMock = new Mock<IConnection>();
            string ExchangeName = "Webshop.OrderExchange";

            Mock<IModel> ModelMock = new Mock<IModel>();
            ConnectionMock.Setup(model => model.CreateModel()).Returns(ModelMock.Object);

            var context = new RabbitMQBusContext(ExchangeName, ConnectionMock.Object);


            // Act
            var result = context.CreateMessageSender();


            // Assert
            Assert.AreEqual(typeof(RabbitMQMessageSender), result.GetType());
        }

        [TestMethod]
        public void CreateMessageReceiverShouldCreatesRabbitMQMessageReceiver()
        {
            // Arrange
            Mock<IModel> channelMock = new Mock<IModel>();
            Mock<IConnection> connectionMock = new Mock<IConnection>(MockBehavior.Strict);
            connectionMock.Setup(conn => conn.CreateModel()).Returns(channelMock.Object);

            var target = new RabbitMQBusContext( "Webshop.ExchangeName", connectionMock.Object);

            // Act
            string queueName = "Webshop.ListenQueue";
            string[] topicExpressions = new string[] { "Webshop.Topic", "Webshop.Topic2" };
            var receiver = target.CreateMessageReceiver(queueName, topicExpressions);

            // Assert
            Assert.IsInstanceOfType(receiver, typeof(RabbitMQMessageReceiver));
            connectionMock.Verify(conn => conn.CreateModel());
            Assert.AreEqual("Webshop.ListenQueue", receiver.QueueName);
            CollectionAssert.AreEquivalent(topicExpressions, receiver.TopicExpressions.ToList());
        }

        [TestMethod]
        public void RabbitMQBusContextShouldDisposesConnection()
        {
            // Arrange 
            Mock<IConnection> connectionMock = new Mock<IConnection>(MockBehavior.Strict);
            connectionMock.Setup(conn => conn.Dispose());
            var target = new RabbitMQBusContext("Webshop.ExchangeName", connectionMock.Object);

            // Act
            target.Dispose();

            // Assert
            connectionMock.Verify(c => c.Dispose());
        }
    }
}