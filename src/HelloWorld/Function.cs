using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace HelloWorld
{ 
    public class Function
    {
        static HttpClient Client => new();

        static async Task<string> GetCallingIp()
        {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var massage = await Client.GetStringAsync("http://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext:false);

            return massage.Replace("\n","");
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest eventPayload, ILambdaContext context)
        {
            if (string.IsNullOrEmpty(eventPayload.Body))
            {
                return new APIGatewayProxyResponse
                {
                    Body = "Invalid request: Body is null or empty",
                    StatusCode = 400,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
            try
            {
                var eventToJob = JsonSerializer.Deserialize<Job>(eventPayload.Body);
                
                if (string.IsNullOrEmpty(eventToJob.Title))
                {
                    return new APIGatewayProxyResponse
                    {
                        Body = "Invalid request: Deserialize error",
                        StatusCode = 500,
                        Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } }
                    };
                }

                if (eventPayload.RequestContext.HttpMethod.ToUpper() == "Get")
                {
                    var dynamoContext = new DynamoDBContext(new AmazonDynamoDBClient());
                    var jobContext = await dynamoContext.LoadAsync<Job>(eventToJob.Title);
        
                    var location = await GetCallingIp();
                    var body = new Dictionary<string, string>
                    {
                        { "Job", jobContext.Title},
                        { "Job description", jobContext.Description}
                    };

                    return new APIGatewayProxyResponse
                    {
                        Body = JsonSerializer.Serialize(body),
                        StatusCode = 200,
                        Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                    };
                }
                
                return new APIGatewayProxyResponse
                {
                    Body = $"Error occurred: Post is not made yet",
                    StatusCode = 500,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
            catch (Exception exception)
            {
                return new APIGatewayProxyResponse
                {
                    Body = $"Error occurred: {exception.Message}",
                    StatusCode = 500,
                    Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
                };
            }
        }
    }

    public class Job
    {
        [DynamoDBHashKey] 
        public string Title { get; set; }
        public string Description { get; set; }
    
        public Job() { }
    }
}
