# Contributing to Library System

We welcome contributions to this project! To maintain code quality, security, and legal clarity, please follow these guidelines.

## 1. Code of Conduct
Please be respectful and professional in all communications and contributions.

## 2. Acceptable Contributions
We accept bug fixes, documentation improvements, architectural proposals, and test coverage enhancements. 
All contributions must:
- Follow the Clean Architecture and DDD principles documented in the repository.
- Include unit/integration tests covering any new logic.
- Ensure all CI/CD pipelines (compilation, automated tests, CodeQL static analysis) pass without errors.
- Keep dependency updates separate from functional changes.

## 3. Developer Certificate of Origin (DCO)
To ensure all contributions are legally authorized, we require contributors to assert the Developer Certificate of Origin (DCO) on every commit.

By adding a `Signed-off-by` line to your git commit message, you certify the following:

```text
Developer Certificate of Origin
Version 1.1

Copyright (C) 2004, 2006 The Linux Foundation and its contributors.
1. The contribution was created in whole or in part by me and I
   have the right to submit it under the open source license
   indicated in the file; or
2. The contribution is based upon previous work that, to the best
   of my knowledge, is covered under an appropriate open source
   license and I have the right under that license to submit that
   work with modifications, whether created in whole or in part
   by me, under the same open source license (unless I am
   permitted to submit under a different license), as indicated
   in the file; or
3. The contribution was provided directly to me by some other
   person who certified (a), (b) or (c) and I have not modified
   it.
4. I understand and agree that this project and the contribution
   are public and that a record of the contribution (including all
   personal information I submit with it, including my sign-off)
   is maintained indefinitely and may be redistributed consistent
   with this project or the open source license(s) involved.
```

### How to Sign Off
To sign your commit, use the `-s` or `--signoff` flag when committing:
```bash
git commit -s -m "feat: add new lending validator"
```
This will automatically append a line to your commit message:
`Signed-off-by: Your Name <your.email@example.com>`
