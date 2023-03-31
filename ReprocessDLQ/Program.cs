// See https://aka.ms/new-console-template for more information
using ReprocessDLQ;

//Enter the connection string, as applicable
string connectionString = "";
string topicName = "";
string subscriptionName = "";
await TransferDeadLetterMessages.ProcessTopicAsync(connectionString, topicName, subscriptionName, 10);

//If you wanted to reprocess the DLQ messages in a Service Bus Queue
//await TransferDeadLetterMessages.ProcessQueueAsync(connectionString: , queueName: );