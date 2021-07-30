using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RabbitMQBus.Bus;
using RabbitMQBus.Receiver;

namespace RabbitMQBus.IntegrationTest
{
    [TestClass]
    public class SendAndReceive
    {
        [ClassInitialize]
        public static void ClassInitialize(TestContext context)
        {
            try
            {
                var builder = new RabbitMQBusContextBuilder()
                    .WithExchange("Webshop.Eventbus")
                    .WithAddress("localhost", 5672)
                    .WithCredentials("guest", "guest");

                using (IBusContext busContext = builder.CreateContext())
                {
                }
            }
            catch
            {
                Process.Start("docker", "run --name rabbitmq -p 5672:5672 -p 15672:15672 rabbitmq:management");
                Thread.Sleep(10000);
            }
        }

        [TestMethod]
        public void SendAndReceiveTest()
        {
            var sentMessage = new EventMessage
            {
                CorrelationId = Guid.NewGuid(),
                EventType = "string",
                Timestamp = 100000,
                Topic = "Webshop.Orderbeheer.OrderAfgerond",
                Body = Encoding.Unicode.GetBytes("Order #152 is afgerond"),
            };

            var receivedFlag = new AutoResetEvent(false);
            EventMessage receivedMessage = null;

            var builder = new RabbitMQBusContextBuilder()
                .WithExchange("MVM.Eventbus")
                .WithAddress("localhost", 5672)
                .WithCredentials("guest", "guest");

            using (IBusContext context = builder.CreateContext())
            // Act - receiver
            using (IMessageReceiver receiver = context.CreateMessageReceiver(
                    queueName: "Webshop.Magazijnbeheer",
                    topicExpressions: new string[] { "Webshop.Orderbeheer.OrderAfgerond" }))
            {
                receiver.StartReceivingMessages();
                receiver.StartHandlingMessages((EventMessage message) =>
                {
                    receivedMessage = message;
                    receivedFlag.Set();
                });

                // Act - sender
                var sender = context.CreateMessageSender();
                sender.SendMessageAsync(sentMessage).Wait();

                // Assert
                bool messageHasBeenReveived = receivedFlag.WaitOne(2000);
                Assert.IsTrue(messageHasBeenReveived);
                Assert.AreEqual(sentMessage.CorrelationId, receivedMessage.CorrelationId);
                Assert.AreEqual(sentMessage.EventType, receivedMessage.EventType);
                Assert.AreEqual(sentMessage.Timestamp, receivedMessage.Timestamp);
                Assert.AreEqual(sentMessage.Topic, receivedMessage.Topic);
                CollectionAssert.AreEqual(sentMessage.Body, receivedMessage.Body);
            }
        }


    }
}
