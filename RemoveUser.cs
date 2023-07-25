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
                    log.LogInformation(groupid);
                    var groupMember = await graphServiceClient.Groups[groupid].Members.Request().GetAsync();

                    foreach (DirectoryObject user in groupMember)
                    {
                        log.LogInformation("In userlist");
                        var userInfo = await graphServiceClient.Users[user.Id].Request().Select("CreatedDateTime").GetAsync();
                        log.LogInformation($" user {userInfo.CreatedDateTime}");

                        DateTimeOffset UserDate = (DateTimeOffset)userInfo.CreatedDateTime;
                        int daysLeft = (DateTime.Today - UserDate).Days;

                        if (daysLeft >= 14)
                        {
                            await graphServiceClient.Groups[groupid].Members[user.Id].Reference.Request().DeleteAsync();
                            log.LogInformation("User remove");
                        }
                        log.LogInformation($"{daysLeft}");
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
    }   
}

