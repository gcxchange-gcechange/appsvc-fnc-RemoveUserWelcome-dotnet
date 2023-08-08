using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Kiota.Abstractions;

namespace appsvc_fnc_RemoveUserWelcome_dotnet
{
    public static class RemoveUser
    {
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
                foreach (var groupid in WelcomeGroup)
                {
                    log.LogInformation("groupid "+groupid);
                    var format = "yyyy-MM-ddTHH:mm:ssK";
                    DateTime less14 = DateTime.UtcNow.AddDays(-14); //get formatdate of 14days ago
                    log.LogInformation("Today less 14days is "+less14);
                   
                    var usersInGroup = await graphServiceClient.Groups[groupid].Members.GraphUser.GetAsync((requestConfiguration) =>
                    {
                        requestConfiguration.QueryParameters.Count = true;
                        requestConfiguration.QueryParameters.Top = 2; ///Get 999 user per call
                        requestConfiguration.QueryParameters.Select = new string[] { "CreatedDateTime", "Id" };
                        requestConfiguration.QueryParameters.Filter = "createdDateTime le "+ less14.ToString(format); //Get all user that the creation date is older than 14 days ago.
                        requestConfiguration.Headers.Add("ConsistencyLevel", "eventual");
                    });

                    var pageIterator = PageIterator<User, UserCollectionResponse>.CreatePageIterator(graphServiceClient, usersInGroup, async (user) =>
                    {
                        log.LogInformation("Info on deleted user. Id" + user.Id + " CreatedDateTime " + user.CreatedDateTime);
                        await graphServiceClient.Groups[groupid].Members[user.Id].Ref.DeleteAsync();
                        return true;
                    });

                    await pageIterator.IterateAsync();
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
    }   
}

