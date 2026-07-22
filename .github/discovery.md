# appqe Discovery Log

Institutional QE memory. **Read the rules below before starting a run**; append a
new one whenever a run learns something a future run should apply.

## Rules in force

<!-- appqe:rules -->
- **ALLURE-XUNIT-VERSION-INCOMPATIBILITY** — Allure.Xunit 2.12.1 + Allure.Net.Commons 2.12.1 could not parse allureConfig.json with "links" as array of objects. Fixed by: (1) upgrading to Allure.Xunit 2.14.1, (2) updating Newtonsoft.Json to 13.0.4, (3) removing malformed "links" array from config. Now tests discover and run successfully. (Status: RESOLVED | Cause: Schema mismatch in Allure.Net.Commons JSON deserializer | Solution: Simplified allureConfig.json)
<!-- /appqe:rules -->

## Findings

Full context for each rule above - what was seen, why, and how it was resolved.
