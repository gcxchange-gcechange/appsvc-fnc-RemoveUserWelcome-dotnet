# appsvc-fnc-RemoveUserWelcome-dotnet

This function is execute every day at 4am.
It check if the creationDate of the user that is part of the group is older than 14 days. If yes, the user is remove from the group

Step 1. Get all user from the group.<br>
Step 2. Get CreateDateTime of the user.<br>
Step 3. Check the difference between the user date creation and now. If more or equal to 14 days, it remove the user.<br>

## Required setting

clientId = App configuration client id<br>
secretId = App configuration client secret<br>
tenantId = Tenant id<br>
welcomeGroup = Id of the group<br>
