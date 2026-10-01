---
description: Write a self-contained handoff doc into the repo, commit and push it, so a fresh cloud session can continue without this conversation's context
---

Write a handoff document for a **fresh Claude Code session that will have zero memory of this conversation**. Do this now, before responding to anything else. The session's local disk disappears when it ends, so the handoff must live in git.

1. Determine today's date/time and the current branch (`git branch --show-current`).
2. Write the doc to `docs/handoffs/<YYYYMMDD-HHMMSS>-<branch-with-slashes-replaced-by-dashes>.md` (the directory exists).
3. The doc must be self-contained. Cover, in this order:
   - **What task is being worked on, and why** (the user's actual goal, not just the literal last request).
   - **Current state of the working tree**: uncommitted changes (run `git status` and `git diff --stat`), anything deliberately left messy, anything mid-investigation that is not obvious from `git log`. Commit finished work first so the tree is as clean as possible.
   - **What is done**, and HOW it was verified (tests run with counts, or explicitly "written but not run").
   - **What is broken or unfinished**, with enough technical detail (root causes ruled out, approaches tried and why they failed, exact file paths and line numbers) that the next session does not repeat dead ends.
   - **Exact next steps**, in order.
   - **Key paths, commands, environment facts** needed to continue (test and build commands, the plan or spec being executed). Never paste secrets; say where they live.
4. Write it as if briefing a competent engineer who was never in the room. No vague "continue the work".
5. `git add` only the handoff file (plus any finished work you had not yet committed, by explicit path), commit with a normal-prose message ending with `Co-Authored-By: Claude Sonnet 5 <noreply@anthropic.com>`, and push the current branch to origin. Never push to master, never force-push.
6. Tell the user the exact file path and the branch name, and that a fresh session should start on that branch and be told: "Read docs/handoffs/<file> and continue." You cannot end this session or start the next one yourself.
