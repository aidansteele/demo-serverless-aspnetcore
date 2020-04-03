# ASP.Net Core 3.1 on AWS Lambda demo

<p align="center">
  <img alt="logo" src="https://user-images.githubusercontent.com/369053/78321254-2f39bf00-75b7-11ea-9d6f-5962c7cf4dd8.png">
</p>

As of the end of March 2020, AWS Lambda [supports ASP.Net Core 3.1][lambda-support].
As of mid-March 2020, API Gateway [HTTP APIs become generally available][http-api-ga].
The combination of these two releases means that the best way (in my opinion!) of 
writing, deploying and running serverless web apps in the cloud is now even better.

My favourite pattern for architecting a serverless .Net website is to put a regular
ASP.Net Core website into a Lambda function wholesale. This means that developers 
can do local development, unit tests, integration tests the exact same way they
know and love **and** take advantage of serverless infrastructure.

This repo contains everything you need to take the standard ASP.Net Core "web API"
template and continuously deploy it to AWS Lambda. Here's what's been added:

## Additions to standard template

* [`.github/workflows/ci.yml`](.github/workflows/ci.yml): This is the [GitHub Actions][actions]
  pipeline for building and deploying this project to AWS Lambda. The steps are:
  
  * Setting up .Net SDK and AWS Lambda CLI
  * Run unit and integration tests
  * Run [ReSharper checks][resharper-action] and reports on PRs
  * Build and package app into a zip file suitable for upload to AWS
  * Log into AWS (this requires you to [configure AWS creds in GitHub][aws-action])
  * Use CloudFormation to deploy the Lambda function and HTTP API
  
* [`src/HelloWorld/Program.cs`](src/HelloWorld/Program.cs): This file has been
  refactored to support the slightly different way that an ASP.Net Core app is
  started in Lambda. You shouldn't need to touch this file at all, except for
  changing logging.
  
* [`src/HelloWorld/Startup.cs`](src/HelloWorld/Startup.cs): The only change to
  this file is to add a (trivial) dependency-injected `IValuesService` to demonstrate
  integration testing in the test project.
  
* [`test/HelloWorld.Tests/TestValuesController.cs`](test/HelloWorld.Tests/TestValuesController.cs): 
  This file demonstrates [ASP.Net Core integration tests][anc-tests] in the style
  made possible by `Microsoft.AspNetCore.Mvc.Testing`. A mock `IValuesService` 
  is injected. This shows that tests don't have to be written any differently 
  just because the app is hosted in Lambda.
  
* [`serverless.yml`](serverless.yml): This file contains the entirety of the
  serverless infrastructure needed to host the website. The key to the file's
  conciseness is the [`AWS::Serverless::Function`][sam-function] that can magic up
  an API.

## So what should I do?

First, you'll want to create your own copy of this template repo by clicking 
this button on the top right of this page:

<img width="132" alt="Use this template" src="https://user-images.githubusercontent.com/369053/78318746-483f7180-75b1-11ea-95b9-6c97f7677125.png">

Once your repo has been created, the first run in GitHub Actions will unfortunately 
fail because you haven't yet setup secrets. You'll want to follow [this AWS guide][aws-action]
to setup your secrets in GitHub. You'll know it's done correctly when your secrets 
look like this:

<img width="264" alt="Example of well-configured secrets" src="https://user-images.githubusercontent.com/369053/78318752-4bd2f880-75b1-11ea-9acf-587757961f45.png">

Finally, once your secrets are configured correctly your pipeline will run 
successfully. PRs have will run unit tests and building, but only the `master` 
branch will get deployed. To access your website, go to your Action's logs,
click the arrow next to the _Deploy_ step and look for the `ApiUrl` output. It
should look something like this:

<img width="589" alt="Example output" src="https://user-images.githubusercontent.com/369053/78318925-b3894380-75b1-11ea-978a-640cf915bf8d.png">

You can then navigate to that URL in your browser - and add `/api/values` onto 
the end of the URL to see the fruits of your labour!

[lambda-support]: https://aws.amazon.com/blogs/compute/announcing-aws-lambda-supports-for-net-core-3-1/
[http-api-ga]: https://aws.amazon.com/blogs/compute/building-better-apis-http-apis-now-generally-available/
[actions]: https://github.com/features/actions
[aws-action]: https://github.com/aws-actions/configure-aws-credentials
[anc-tests]: https://docs.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-3.1
[sam-function]: https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/sam-resource-function.html
[resharper-action]: https://github.com/glassechidna/resharper-action
