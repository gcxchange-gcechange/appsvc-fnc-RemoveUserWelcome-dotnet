using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace appsvc_fnc_RemoveUserWelcome_dotnet
{
    public static class RemoveUser
    {
        const int maxRequestCount = 20; // numer of batch requests per api call, max is 20

        // runs daily at 4 AM
        [FunctionName("RemoveUser")]
        public static async Task Run([TimerTrigger("0 0 4 * * *")] TimerInfo myTimer, ExecutionContext context, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            IConfiguration config = new ConfigurationBuilder().AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables().Build();

            string[] welcomeGroup = config["welcomeGroup"].Split(',');

            ROPCConfidentialTokenCredential auth = new ROPCConfidentialTokenCredential(log);
            var graphAPIAuth = new GraphServiceClient(auth);

            var result = await CheckMemberWelcome(graphAPIAuth, welcomeGroup, log);

            if (!result)
            {
                throw new SystemException("Something happen, please check the logs");
            }
        }

        public static async Task<bool> CheckMemberWelcome(GraphServiceClient graphServiceClient, string[] WelcomeGroup, ILogger log)
        {
            bool result;

            try
            {
                foreach (var groupId in WelcomeGroup)
                {
                    log.LogInformation("Processing groupId: " + groupId);

                    string format = "yyyy-MM-ddTHH:mm:ssK";
                    DateTime less14 = DateTime.UtcNow.AddDays(-14); //get formatdate of 14days ago
                    log.LogInformation("Today less 14days is " + less14);

                    var usersInGroup = await graphServiceClient.Groups[groupId].Members.GraphUser.GetAsync((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Count = true;
                        requestConfiguration.QueryParameters.Top = 999;
                        requestConfiguration.QueryParameters.Select = new string[] { "CreatedDateTime", "Id" };
                        requestConfiguration.QueryParameters.Filter = "createdDateTime le "+ less14.ToString(format); //Get all user that the creation date is older than 14 days ago.

                        requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                    });

                    var batch = new BatchRequestContent(graphServiceClient);
                    int requestCount = 0;

                    var pageIterator = PageIterator<User, UserCollectionResponse>.CreatePageIterator(graphServiceClient, usersInGroup, async (user) =>
                    {
                        log.LogInformation($"Info on deleted user: Id {user.Id}, CreatedDateTime {user.CreatedDateTime}");

                        requestCount = requestCount + 1;

                        var url = $"https://graph.microsoft.com/v1.0/groups/{groupId}/members/{user.Id}/$ref";
                        BatchRequestStep step = new BatchRequestStep(requestCount.ToString(), new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Delete, url));
                        batch.AddBatchRequestStep(step);

                        if (requestCount == maxRequestCount)
                        {
                            await ProcessBatchAsync(graphServiceClient, batch, log);

                            requestCount = 0;
                            batch = new BatchRequestContent(graphServiceClient);
                        }

                        return true;
                    });

                    await pageIterator.IterateAsync();

                    // process any stragglers from the group
                    if (requestCount > 0)
                    {
                        await ProcessBatchAsync(graphServiceClient, batch, log);
                    }
                }

                result = true;
            }
            catch (ServiceException ex)
            {
                log.LogInformation($"Error check group : {ex.Message}");
                result = false;
            };

            return result;
        }

        private static async Task ProcessBatchAsync(GraphServiceClient client, BatchRequestContent batch, ILogger log)
        {
            log.LogInformation("ProcessBatchAsync received a request.");

            try
            {
                var batchResponse = await client.Batch.PostAsync(batch);
                var responses = await batchResponse.GetResponsesAsync();

                foreach (var response in responses)
                {
                    log.LogInformation($"Success: {response.Value.IsSuccessStatusCode}, {(int)response.Value.StatusCode} {response.Value.ReasonPhrase}");
                }
            }
            catch (Exception e)
            {
                log.LogError($"Message: {e.Message}");
                if (e.InnerException is not null) log.LogError($"InnerException: {e.InnerException.Message}");
                log.LogError($"StackTrace: {e.StackTrace}");
            }

            log.LogInformation("ProcessBatchAsync processed a request.");
        }
    }
}