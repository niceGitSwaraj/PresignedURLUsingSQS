using System;
using Amazon;
using Amazon.SQS;
using Amazon.SQS.Model;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SQSSendReceive
{
    class Program
    {
        private const double timeoutDuration = 12;
        private const int MaxMessages = 1;
        private const int WaitTime = 2;        
        private static async Task SendMessageBatch(
        IAmazonSQS sqsClient, string qUrl, List<SendMessageBatchRequestEntry> messages)
        {
            Console.WriteLine($"\nSending a batch of messages to queue\n  {qUrl}");
            SendMessageBatchResponse responseSendBatch =
              await sqsClient.SendMessageBatchAsync(qUrl, messages);
            // Could test responseSendBatch.Failed here
            foreach (SendMessageBatchResultEntry entry in responseSendBatch.Successful)
                Console.WriteLine($"Message {entry.Id} successfully queued.");
        }

        private static async Task<ReceiveMessageResponse> GetMessage(
        IAmazonSQS sqsClient, string qUrl, int waitTime = 0)
        {
            return await sqsClient.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = qUrl,
                MaxNumberOfMessages = MaxMessages,
                WaitTimeSeconds = waitTime
                // (Could also request attributes, set visibility timeout, etc.)
            });
        }

        private static bool ProcessMessage(Message message)
        {
            Console.WriteLine($"\nMessage body of {message.MessageId}:");
            Console.WriteLine($"{message.Body}");
            Console.WriteLine(presignedURL.GeneratePreSignedURL(timeoutDuration, message.Body));
            return true;
        }

        private static async Task DeleteMessage(
        IAmazonSQS sqsClient, Message message, string qUrl)
        {
            Console.WriteLine($"\nDeleting message {message.MessageId} from queue...");
            await sqsClient.DeleteMessageAsync(qUrl, message.ReceiptHandle);
        }
        static async Task Main(string[] args)
        {
            Console.WriteLine("Amazon SQS");
            string[] incomingMessages = { 
                                         "eiffelTower.jfif",
                                         "bucket.jpg",
                                         "donald.jpg" 
                                        };
            IAmazonSQS sqs = new AmazonSQSClient(RegionEndpoint.USEast2);
            Console.WriteLine("Creating a queue called DataQueue.\n");
            
            var sqsRequest = new CreateQueueRequest
            {
                QueueName = "DataQueue"
            };

            var createQueueResponse = sqs.CreateQueueAsync(sqsRequest).Result;
            var myQueueURL = createQueueResponse.QueueUrl;
            var listQueuesRequest = new ListQueuesRequest();
            var listQueuesResponse = sqs.ListQueuesAsync(listQueuesRequest);

            Console.WriteLine("List of SQS Queues\n");
            foreach (var queueUrl in listQueuesResponse.Result.QueueUrls)
            {
                Console.WriteLine($"QueueUrl: {queueUrl}");
            }

            Console.WriteLine("Seeding The Queue");            
            int counter = 0;

            foreach (var msg in incomingMessages) {                
                var batchMessages = new List<SendMessageBatchRequestEntry>{
                    new SendMessageBatchRequestEntry("msg"+counter, msg)};
                await SendMessageBatch(sqs, myQueueURL, batchMessages);
                counter++;
            }
            Console.WriteLine("End of Data Stream\n");
            
            do
            {
                var msg = await GetMessage(sqs, myQueueURL, WaitTime);               
                if (msg.Messages.Count != 0)
                {
                    if (ProcessMessage(msg.Messages[0])) {
                        await DeleteMessage(sqs, msg.Messages[0], myQueueURL);
                    }        

                }
            } while (!Console.KeyAvailable);

            Console.ReadLine();

        }

      
    }
}
