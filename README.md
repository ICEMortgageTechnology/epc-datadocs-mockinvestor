## Encompass Partner Connect
#### Investor Adapter
 
## Table of Contents
1. Introduction
2. System Requirements
3. Source Code Compilation
4. Configure ngrok
5. Configure the Visual Studio Solution
6. Create and Upload the Integration Zip File
7. Testing the Integration
 
## Introduction
This document explains how to set up your Investor Adapter. The Investor Adapter is a pared-down version of the Reference Integration, designed specifically to integrate into the Investor Connect flow.

## System Requirements
- Microsoft Visual Studio 2017
- A production Encompass instance
- Loan Officer Connect
- An Encompass Partner Connect account
- Encompass Partner Connect API credentials
- (Optional) An ngrok account
 
## Source Code Compilation
Clone the Mock Investor repository from GitHub and open it in Visual Studio 2017. Target the build for .netcore 2.0. Confirm that the project builds without errors. Attach the application to either IIS Express or Google Chrome as you launch it; take note of the port the application is launched on (e.g. localhost:65387)
 
## Configure ngrok
The EPC Partner API uses webhooks to notify the Partner that an order is ready to be processed.  In order to receive a webhook notification, a website must have a REST API endpoint listening on the public Internet.  However, most developers would not want their development workstation exposed publically on the Internet in order to test their code.
 
Ngrok is a utility that creates a public-facing URL that can accept webhook notifications, and forwards these notifications to a process running on the development workstation.  This can be done without exposing the development workstation to the public Internet.
 
Ngrok will create a new public-facing URL every time it is invoked, and this URL must be registered with Encompass Partner Connect in order to test the integration.  Ngrok (paid version) can use a static URL that doesn't change every time it is invoked.
 
Navigate to [ngrok.com] (https://ngrok.com/) and download the version of ngrok appropriate for your development environment.
 
To launch ngrok, unzip the contents, open a command prompt to the ngrok folder location, and execute:
```
ngrok http -host-header="localhost:65387" 65387
```
 
Ngrok will display a proxy URL, for example https://123456f23.ngrok.io/.  When developing against EPC, use this proxy URL as the Webhook URL for partner configuration.
 
Ngrok has a Web Console that can be used to see active requests that are being proxied to the development workstation.  The Web Console can be accessed at [http://127.0.0.1:4040/inspect/http] (http://127.0.0.1:4040/inspect/http)

Note that unless you have signed up for a free ngrok account, the standalone client will only host the proxy for 8 hours.
 
## Configure the Visual Studio Solution
 
Plug the EPC WebhookSecret, APIHost, ClientID and ClientSecret in the config file (appsettings.json) which is shared by Ellie Mae during onboarding. The Package Location field determines where loan packages will be dumped on order processing (path relative to the project); logs will similarly be dumped in the TraceLogLocation.
 
## Create and Upload the Integration Zip File
 
In order to register the integration with Encompass Partner Connect, create a zip file containing an HTML file with the user interface you have designed. Once zipped, upload the zip file to the Encompass Partner Connect Portal.
Start ngrok open on the appropriate port, and use that as the Webhook URL in Partner Connect's Integration Upload.
 
## Testing the Integration
 
In the Visual Studio Solution, start debugging, which runs the web site in IIS Express, running on port 65387.
 
Log into Loan Officer Connect (LO Connect) and navigate to Services. Find the newly registered integration under the category specified in the JSON configuration file, and launch it.