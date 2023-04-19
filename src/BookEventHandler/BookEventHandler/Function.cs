using System.Text;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace BookEventHandler;

public class Function
{
    private readonly EventMessageUploadService _messageUploadService;

    /// <summary>
    /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
    /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
    /// region the Lambda function is executed in.
    /// </summary>
    public Function()
    {
        _messageUploadService = new EventMessageUploadService(new AmazonS3Client());
    }


    /// <summary>
    /// This method is called for every Lambda invocation. This method takes in an SQS event object and can be used 
    /// to respond to SQS messages.
    /// </summary>
    /// <param name="evnt"></param>
    /// <param name="context"></param>
    /// <returns></returns>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach(var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        context.Logger.LogInformation($"Processed message {message.Body}");

        await _messageUploadService.UploadAsync(message, context);
    }
}

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
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.Message);
        }
    }
}