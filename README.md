# ReprocessDLQ
The tool is a simple one to use. It's a console app built with .NET 6. After cloning and restoring the NuGet packages, you can simply enter the connection string, topic/queue name and any other requisite parameter and you are good to go.

# Caveat
This app was built using the ReceiveMessagesAsync. As has been clarified by the Azure team, there is no guarantee that the maxCount value will be returned from the queue, even if you have more than the max count in your DLQ. A way around to speed up processing can be to start the app via "Start Without Debugging", and open many instances.

An equally elegant way to automate your 'boring' requeue work, especially when you have a lot of messages, is to modify this code to use a while loop. That is, while it's still able to fetch messages from the DLQ, it should continue to reprocess. But I will leave the implementation up to you.
