---
paths:
  - "DevScripts/**/*.ps1"
  - "DevScripts/**/*.psm1"
---

# DevScripts conventions

## Output

Do not write output for each step a script performs — success is silent. Only write output when something
goes wrong, or when the function's purpose is to provide information.
