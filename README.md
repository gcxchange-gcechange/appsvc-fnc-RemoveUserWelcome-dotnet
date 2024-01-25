# Remove Welcome Group Users

## Summary

This function executes every day at 4 AM.
It check if the creationDate of the user that is part of the welcome group is older than 14 days. If yes, the user is removed from the group.

1. Get all users from the group
2. Get CreateDateTime of the user
3. Check the difference between the user date creation and now. If greater than or equal to 14 days, remove the user

## Prerequisites

## Version 

![dotnet 6](https://img.shields.io/badge/net6.0-blue.svg)

## API permission

MSGraph

| API / Permissions name    | Type        | Admin consent | Justification                       |
| ------------------------- | ----------- | ------------- | ----------------------------------- |
| Group.Read.All            | Delegated   | Yes           | Read welcome group                  |
| GroupMember.ReadWrite.All | Delegated   | Yes           | Remove members from welcome group   |
| User.Read.All             | Delegated   | Yes           | Retrieve user information           |

## App setting

| Name                    	| Description                                                                   				   |
| -------------------------	| ------------------------------------------------------------------------------------------------ |
| AzureWebJobsStorage     	| Connection string for the storage acoount                                     				   |
| clientId                	| The application (client) ID of the app registration                           				   |
| keyVaultUrl             	| Address for the key vault                                                     				   |
| secretName              	| Secret name used to authorize the function app                                				   |
| secretNamePassword        | Secrent name used for the delegated account password                                             | 
| tenantid                	| Id of the Azure tenant that hosts the function app                            		           |
| user-email                | Acccount to use for delegated access                                                             |
| welcomeGroup              | Id(s) of the welcome group(s)                                                                    |

## Version history

Version|Date|Comments
-------|----|--------
1.0| |Initial release
1.1| 2024-01-25| Update batch deletion, readme file

## Disclaimer

**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**