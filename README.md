# ASP.NET Core on AWS Lambda — 2026 edition

A regular ASP.NET Core controller-based API, running locally with Kestrel and on
AWS Lambda behind an API Gateway HTTP API. Develop and integration-test it like
any other ASP.NET Core app; Lambda only changes the hosting layer.

This demo uses **.NET 10 LTS**, the Lambda `dotnet10` managed runtime on Amazon
Linux 2023, and GitHub Actions with short-lived AWS credentials via OIDC.
[AWS added .NET 10 support in January 2026][lambda-support].

## Run and test locally

Install the [.NET 10 SDK][dotnet], then run from the repository root:

```sh
dotnet restore
dotnet test --configuration Release
dotnet run --project src/HelloWorld --urls http://localhost:5000
```

In another terminal:

```sh
curl http://localhost:5000/api/values
# ["value1","value2"]
```

No AWS credentials or Docker are needed for these steps.

## How it works

* [`Program.cs`](src/HelloWorld/Program.cs) registers controllers and a trivial
  `IValuesService`. `AddAWSLambdaHosting(LambdaEventSource.HttpApi)` replaces
  Kestrel with Lambda's HTTP API v2 adapter only when running in Lambda. The
  Lambda handler is the assembly name, `HelloWorld`.
* [`TestValuesController.cs`](test/HelloWorld.Tests/TestValuesController.cs) uses
  `WebApplicationFactory<Program>` to test the real HTTP pipeline, both with the
  default service and with a test implementation injected through DI.
* [`serverless.yml`](serverless.yml) is an **AWS SAM / CloudFormation** template,
  not a Serverless Framework configuration. It defines the function, its `live`
  alias, and an HTTP API. API Gateway provides the public HTTPS endpoint, so the
  app does not redirect local HTTP requests to HTTPS.
* [`.github/workflows/ci.yml`](.github/workflows/ci.yml) tests, packages, and lints
  on pull requests and pushes to `master`. A separate deployment job assumes an
  AWS role only on pushes to `master` in the configured repository.
  Fresh template copies therefore pass CI without AWS setup. Deployments are
  serialized and reuse the exact ZIP produced by the build job.

Deployment uses a separate, administrator-managed CloudFormation bootstrap stack
for the GitHub role and CloudFormation execution role. SAM creates or discovers
its managed artifact bucket automatically with `--resolve-s3`; no bucket name is
required. The role bootstrap template is managed outside this repository. GitHub uploads artifacts and
submits changes; CloudFormation assumes the execution role to provision resources.

The original sample endpoints remain intentionally minimal: `GET /api/values/{id}`
returns the first value regardless of ID; POST, PUT, and DELETE are no-op stubs.
This is a hosting example, not a persistent CRUD service. The API is public and
unauthenticated; add authorization before exposing private data.

## Package and deploy manually

Install the [AWS SAM CLI][sam-install] and configure AWS credentials. Package on
Linux x64 (as CI does) for the template's `x86_64` architecture. ReadyToRun is
enabled and the package is framework-dependent: Lambda supplies .NET 10.

```sh
dotnet tool restore
dotnet lambda package --project-location src/HelloWorld
sam validate --lint --template-file serverless.yml --region us-east-1
sam deploy --template-file serverless.yml --stack-name hello-world-app \
  --resolve-s3 --s3-prefix hello-world-app \
  --role-arn YOUR_CLOUDFORMATION_ROLE_ARN --capabilities CAPABILITY_IAM
```

Use the CloudFormation role described below.
Deployment creates billable AWS resources. SAM prints the `ApiUrl` stack
output; append `/api/values` to call the API. To remove the demo stack when done,
use an administrator session (the GitHub role cannot delete stacks):

```sh
sam delete --stack-name hello-world-app --region YOUR_REGION
```

## Set up continuous deployment

Create a repository from this template, then configure [GitHub's AWS OIDC
integration][oidc]. No `AWS_ACCESS_KEY_ID` or `AWS_SECRET_ACCESS_KEY` secrets are
needed.

1. Add the IAM OIDC identity provider `https://token.actions.githubusercontent.com`
   with audience `sts.amazonaws.com` to your AWS account, if it does not exist.
2. Provision two roles outside this repository:
   a GitHub OIDC role with the branch-restricted trust described below, and an
   execution role trusted by `cloudformation.amazonaws.com`. Give GitHub only
   the SAM setup, artifact, change-set, and `iam:PassRole` permissions described below.
3. In the workflow's deployment job, update the repository guard, `AWS_ROLE_ARN`,
   `CFN_EXECUTION_ROLE_ARN`, `AWS_REGION`, and
   `allowed-account-ids` using those resources and your account. These identifiers
   are not secrets. The checked-in settings target this repository's account,
   `887559014507`, in `us-east-1`.
4. Push to `master`. The Deploy step prints the stack outputs, including `ApiUrl`.

For this repository, the bootstrap stack is
`demo-serverless-aspnetcore-deployment` in `us-east-1`. An administrator can retrieve
its template with `aws cloudformation get-template --stack-name
demo-serverless-aspnetcore-deployment --region us-east-1`.

The GitHub role trusts only audience `sts.amazonaws.com` and subject
`repo:OWNER/REPO:ref:refs/heads/master`. Normal PR tokens have a different subject
and are rejected by AWS, even if a contributor edits the PR workflow. Keep the
`pull_request` trigger; do not run PR code in a privileged `pull_request_target`
job. Protect `master` and review workflow and build-script changes before merging.

The GitHub role can upload/read artifacts under the `hello-world-app/` prefix in
SAM-managed buckets in this account and manage change sets for `hello-world-app`
in the bootstrap region. It can also create/discover SAM's
`aws-sam-cli-managed-default` stack and provision its bucket through CloudFormation.
SAM uses the caller's permissions for this bucket setup, not the application's
execution role; the bucket permissions are limited to SAM's generated bucket-name prefix.
It can pass only the CloudFormation execution
role, and only to CloudFormation. It has no direct Lambda, API Gateway, or IAM
administration permissions. SAM must supply that execution role with `--role-arn`.

**The CloudFormation execution role intentionally has `AdministratorAccess`.**
Anyone able to deploy trusted templates through `master` therefore has effective
administrator authority through CloudFormation, despite the GitHub role's narrow
direct permissions. This separation is not a sandbox for malicious templates.
SAM creates the application's ordinary Lambda execution role separately; the
function does not run with CloudFormation's administrator role.

If you use `main` instead, update both branch references in the workflow and the
GitHub role's OIDC trust policy. Remove any obsolete long-lived AWS keys
from GitHub and revoke them in IAM after migrating an existing setup.

[dotnet]: https://dotnet.microsoft.com/download/dotnet/10.0
[lambda-support]: https://aws.amazon.com/about-aws/whats-new/2026/01/aws-lambda-dot-net-10/
[sam-install]: https://docs.aws.amazon.com/serverless-application-model/latest/developerguide/install-sam-cli.html
[oidc]: https://github.com/aws-actions/configure-aws-credentials#oidc
