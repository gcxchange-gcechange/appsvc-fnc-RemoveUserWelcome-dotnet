using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;

namespace appsvc_fnc_RemoveUserWelcome_dotnet
{
    public static class RemoveUser
    {
        [FunctionName("RemoveUser")]
         public static async Task Run([TimerTrigger("0 0 4 * * *")]TimerInfo myTimer, ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            IConfiguration config = new ConfigurationBuilder()

            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

            string welcomeGroup = config["welcomeGroup"];
            Auth auth = new Auth();
            var graphAPIAuth = auth.graphAuth(log);
            var result = await CheckMemberWelcome(graphAPIAuth, welcomeGroup, log);

            if(!result)
            {
               throw new SystemException("Something happen, please check the logs");
            }
        }

        public static async Task<bool> CheckMemberWelcome(GraphServiceClient graphServiceClient, string WelcomeGroup, ILogger log)
        {
            bool result = false;
            try
            {
                var groupMember = await graphServiceClient.Groups[WelcomeGroup]
                .Members
                .Request()
                .GetAsync();
           
                foreach (DirectoryObject user in groupMember)
                {
                    var userInfo = await graphServiceClient.Users[user.Id]
                            .Request()
                            .Select("CreatedDateTime")
                            .GetAsync();
                    log.LogInformation($" user {userInfo.CreatedDateTime}");
  
                    DateTimeOffset UserDate = (DateTimeOffset)userInfo.CreatedDateTime;
                    int daysLeft = (DateTime.Today - UserDate).Days;

                    if(daysLeft >= 14)
                    {
                        await graphServiceClient.Groups[WelcomeGroup].Members[user.Id].Reference
                            .Request()
                            .DeleteAsync();
                        log.LogInformation("User remove");
                    }
                    log.LogInformation($"{daysLeft}");
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

