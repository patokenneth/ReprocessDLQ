// See https://aka.ms/new-console-template for more information
using ReprocessDLQ;

string connectionString = "";
string topicName = "";
string subscriptionName = "";
await TransferDeadLetterMessages.ProcessTopicAsync(connectionString, topicName, subscriptionName, 10);
