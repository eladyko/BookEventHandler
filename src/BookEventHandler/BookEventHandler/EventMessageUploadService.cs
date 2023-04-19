using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Transfer;

namespace BookEventHandler;

public class EventMessageUploadService
{
    private readonly IAmazonS3 _amazonS3;

    public EventMessageUploadService(IAmazonS3 amazonS3)
    {
        _amazonS3 = amazonS3;
    }

    public async Task UploadAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        try
        {
            var transferUtility = new TransferUtility(_amazonS3);

            var bytes = Encoding.UTF8.GetBytes(message.Body);
            using var memoryStream = new MemoryStream(bytes);

            await transferUtility.UploadAsync(new TransferUtilityUploadRequest
            {
                Key = message.MessageId,
                BucketName = "event-messages",
                ContentType = "text/json",
                InputStream = memoryStream
            });

            context.Logger.LogInformation($"Message processed: {message.Body}");
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.Message);
        }
    }
}