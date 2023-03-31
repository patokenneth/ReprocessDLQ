using Azure.Messaging.ServiceBus;
using NLog;

namespace ReprocessDLQ
{
    class TransferDeadLetterMessages
    {
        // https://github.com/Azure/azure-sdk-for-net/blob/Azure.Messaging.ServiceBus_7.2.1/sdk/servicebus/Azure.Messaging.ServiceBus/README.md

        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static ServiceBusClient client;
        private static ServiceBusSender sender;

        public static async Task ProcessTopicAsync(string connectionString, string topicName, string subscriberName, int fetchCount = 10)
        {
            try
            {
                logger.Info("Begin processing...");
                client = new ServiceBusClient(connectionString);
                sender = client.CreateSender(topicName);

                ServiceBusReceiver dlqReceiver = client.CreateReceiver(topicName, subscriberName, new ServiceBusReceiverOptions
                {
                    SubQueue = SubQueue.DeadLetter,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

                await ProcessDeadLetterMessagesAsync($"topic: {topicName} -> subscriber: {subscriberName}", fetchCount, sender, dlqReceiver);
            }
            catch (Azure.Messaging.ServiceBus.ServiceBusException ex)
            {
                if (ex.Reason == Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    logger.Error(ex, $"Topic:Subscriber '{topicName}:{subscriberName}' not found. Check that the name provided is correct.");
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                await sender.CloseAsync();
                await client.DisposeAsync();
            }
        }

        public static async Task ProcessQueueAsync(string connectionString, string queueName, int fetchCount = 10)
        {
            try
            {
                client = new ServiceBusClient(connectionString);
                sender = client.CreateSender(queueName);

                ServiceBusReceiver dlqReceiver = client.CreateReceiver(queueName, new ServiceBusReceiverOptions
                {
                    SubQueue = SubQueue.DeadLetter,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

                await ProcessDeadLetterMessagesAsync($"queue: {queueName}", fetchCount, sender, dlqReceiver);
            }
            catch (Azure.Messaging.ServiceBus.ServiceBusException ex)
            {
                if (ex.Reason == Azure.Messaging.ServiceBus.ServiceBusFailureReason.MessagingEntityNotFound)
                {
                    logger.Error(ex, $"Queue '{queueName}' not found. Check that the name provided is correct.");
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                await sender.CloseAsync();
                await client.DisposeAsync();
            }
        }

        private static async Task ProcessDeadLetterMessagesAsync(string source, int fetchCount, ServiceBusSender sender, ServiceBusReceiver dlqReceiver)
        {
            var wait = new System.TimeSpan(0, 0, 10);

            logger.Info($"fetching messages ({wait.TotalSeconds} seconds retrieval timeout)");
            logger.Info(source);

            IReadOnlyList<ServiceBusReceivedMessage> dlqMessages = await dlqReceiver.ReceiveMessagesAsync(fetchCount, wait);

            logger.Info($"dl-count: {dlqMessages.Count}");

            int i = 1;

            foreach (var dlqMessage in dlqMessages)
            {
                logger.Info($"start processing message {i}");
                logger.Info($"dl-message-dead-letter-message-id: {dlqMessage.MessageId}");
                logger.Info($"dl-message-dead-letter-reason: {dlqMessage.DeadLetterReason}");
                logger.Info($"dl-message-dead-letter-error-description: {dlqMessage.DeadLetterErrorDescription}");

                ServiceBusMessage resubmittableMessage = new ServiceBusMessage(dlqMessage);

                await sender.SendMessageAsync(resubmittableMessage);

                await dlqReceiver.CompleteMessageAsync(dlqMessage);

                logger.Info($"finished processing message {i}");
                logger.Info("--------------------------------------------------------------------------------------");

                i++;
            }

            await dlqReceiver.CloseAsync();

            logger.Info($"finished");
        }
    }
}