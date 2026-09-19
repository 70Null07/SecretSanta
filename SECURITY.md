# Security Policy

## Supported versions

SecretSanta is under active development and does not currently have versioned
releases. Security updates are applied only to the latest commit on the default
branch. Older commits, forks, and locally modified deployments are not
supported.

| Version | Supported |
| --- | --- |
| Latest commit on the default branch | Yes |
| Older commits and forks | No |

## Reporting a vulnerability

Please do not disclose suspected vulnerabilities in a public issue, discussion,
or pull request.

Report vulnerabilities privately through
[GitHub's private vulnerability reporting](https://github.com/70Null07/SecretSanta/security/advisories/new).
Include, where possible:

- a description of the vulnerability and its potential impact;
- the affected component, commit, and configuration;
- reproducible steps or a minimal proof of concept;
- any known mitigations or workarounds; and
- whether the vulnerability has been disclosed elsewhere.

Do not include real credentials, personal data, or other secrets in the report.
Use test accounts and synthetic data when demonstrating the issue.

The maintainers will acknowledge the report as soon as practical, investigate
it, and use the private advisory to coordinate remediation and disclosure. You
may be asked for additional information while the report is being validated.
Please allow a reasonable remediation period before publishing details.

## Scope

Reports about the application code, container images, deployment configuration,
authentication or authorization, secret handling, and dependency usage are in
scope. Vulnerabilities in third-party services or infrastructure not controlled
by this project should be reported to the relevant provider.
