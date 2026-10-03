# Use CLI to perform a single action then exit

- STATUS: OPEN
- PRIORITY: 75
- TAGS: feature

## Description
- `run mood|status|history`:
  - easy to implement
- `run activity|edit`:
  - need to have an non-interactive way to use it, maybe with complex cli arguments

- `run activity eat|play`:
  - print dice results
- `run activity eat|play <a> <b> <c> <d>`
  - select the dice with 0-based dice indices
- `run activity clean [<code>]`:
  - print dice results
- `run activity clean [<code>] <a> <b> <c> <d>`
  - select the dice with 0-based dice indices and output a continuation code on matching pairs
