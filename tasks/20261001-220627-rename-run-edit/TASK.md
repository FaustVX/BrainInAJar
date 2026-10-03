# Rename `run edit` to `run stat` and allow to get or set stats

- STATUS: CLOSED
- PRIORITY: 50
- TAGS: feature

## Description
- modify a statistic:
  - `dotnet ... run stat property value`
- obtain a statistic:
  - `dotnet ... run stat property`
- advanced usage:
  - set a property value from another property
    - `dotnet ... run stat property1 $(dotnet ... run stat property2)`
