# Security Policy

## Supported development status

AshkanLMS is an evolving portfolio project. Do not deploy it with real learner records or sensitive organizational data until authentication, authorization, persistence, logging, deployment, and data-protection behavior have been reviewed and tested in the target environment.

## Reporting a concern

Please do not publish passwords, connection strings, personal records, or exploitable details in a public issue. Contact the repository maintainer privately through a verified contact channel and include:

- A concise description and impact
- The affected stage/version and environment
- Reproduction steps using synthetic data
- Relevant sanitized logs (remove tokens, credentials, hostnames, and personal data)

## Security practices for contributors

- Never commit secrets or local configuration files.
- Use synthetic data in screenshots, tests, and examples.
- Review SQL scripts and back up target databases before applying migrations.
- Do not export raw exception messages or connection secrets in diagnostics.
- Require authorization checks on administrative and diagnostic features.
- Document known limitations; do not claim a security control has been tested unless it has.
