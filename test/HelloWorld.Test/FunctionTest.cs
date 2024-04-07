using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System.Text.Json;
using Xunit;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using Xunit.Abstractions;

namespace HelloWorld.Tests
{
  public class FunctionTest
  {
      private readonly ITestOutputHelper _testOutputHelper;

      public FunctionTest(ITestOutputHelper testOutputHelper)
      {
          _testOutputHelper = testOutputHelper;
      }

      static HttpClient Client => new HttpClient();

    static async Task<string> GetCallingIp()
    {
            Client.DefaultRequestHeaders.Accept.Clear();
            Client.DefaultRequestHeaders.Add("User-Agent", "AWS Lambda .Net Client");

            var stringTask = Client.GetStringAsync("http://checkip.amazonaws.com/").ConfigureAwait(continueOnCapturedContext:false);

            var massage = await stringTask;
            return massage.Replace("\n","");
    }

    [Fact]
    public async Task TestHelloWorldFunctionHandler()
    {
            var request = new APIGatewayProxyRequest();
            var context = new TestLambdaContext();
            string location = GetCallingIp().Result;
            Dictionary<string, string> body = new Dictionary<string, string>
            {
                { "message", "hello world" },
                { "location", location },
            };

            var expectedResponse = new APIGatewayProxyResponse
            {
                Body = JsonSerializer.Serialize(body),
                StatusCode = 200,
                Headers = new Dictionary<string, string> { { "Content-Type", "application/json" } }
            };

            var function = new Function();
            var response = await function.FunctionHandler(request, context);

            _testOutputHelper.WriteLine("Lambda Response: \n" + response.Body);
            _testOutputHelper.WriteLine("Expected Response: \n" + expectedResponse.Body);

            Assert.Equal(expectedResponse.Body, response.Body);
            Assert.Equal(expectedResponse.Headers, response.Headers);
            Assert.Equal(expectedResponse.StatusCode, response.StatusCode);
    }
  }
}