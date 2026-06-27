export const meta = {
  name: 'plan-and-fix-roadmap',
  description: 'Plan, review, adversarially review, implement, commit (logical chunks) and document all ROADMAP.md bug fixes using parallel agents',
  whenToUse: 'When a ROADMAP.md (or similar bug list) exists and you want every bug planned, peer- and adversarially-reviewed, implemented in parallel by disjoint file groups, committed in logical chunks, and documented with learnings. Run with args.stage="plan" first, insert a human/advisor review, then args.stage="implement". args.stage="all" runs end-to-end.',
  phases: [
    { title: 'Scout' },
    { title: 'Plan' },
    { title: 'Review' },
    { title: 'Coordinate' },
    { title: 'Adversarial Review' },
    { title: 'Implement' },
    { title: 'Verify' },
    { title: 'Commit' },
    { title: 'Document' },
  ],
}

// ----- inputs -----
// args.stage: 'plan' (plan+review+coordinate+adversarial), 'implement' (implement+verify+commit+document), or 'all'
// args.roadmap: path to the bug roadmap (default 'ROADMAP.md')
// args.branch: git branch to commit fixes onto (default 'fix/roadmap-bugs')
const STAGE = (args && args.stage) || 'all'
const ROADMAP = (args && args.roadmap) || 'ROADMAP.md'
const BRANCH = (args && args.branch) || 'fix/roadmap-bugs'
const doPlan = STAGE === 'plan' || STAGE === 'all'
const doImpl = STAGE === 'implement' || STAGE === 'all'

// ----- schemas -----
const BUGS_SCHEMA = {
  type: 'object',
  required: ['bugs'],
  properties: {
    bugs: {
      type: 'array',
      items: {
        type: 'object',
        required: ['id', 'title', 'severity', 'files', 'summary'],
        properties: {
          id: { type: 'string' },
          title: { type: 'string' },
          severity: { type: 'string' },
          live: { type: 'boolean' },
          files: { type: 'array', items: { type: 'string' } },
          summary: { type: 'string' },
        },
      },
    },
  },
}

const PLAN_SCHEMA = {
  type: 'object',
  required: ['id', 'planPath', 'files', 'risk', 'approach', 'breaksCompat'],
  properties: {
    id: { type: 'string' },
    planPath: { type: 'string' },
    files: { type: 'array', items: { type: 'string' } },
    risk: { type: 'string', enum: ['low', 'medium', 'high'] },
    breaksCompat: { type: 'boolean' },
    approach: { type: 'string' },
  },
}

const REVIEW_SCHEMA = {
  type: 'object',
  required: ['id', 'approved', 'issues'],
  properties: {
    id: { type: 'string' },
    approved: { type: 'boolean' },
    issues: { type: 'array', items: { type: 'string' } },
    requiredChanges: { type: 'string' },
  },
}

const GROUPING_SCHEMA = {
  type: 'object',
  required: ['groups', 'deferred', 'commitChunks', 'sharedContracts'],
  properties: {
    groups: {
      type: 'array',
      items: {
        type: 'object',
        required: ['name', 'files', 'bugIds'],
        properties: {
          name: { type: 'string' },
          files: { type: 'array', items: { type: 'string' } },
          bugIds: { type: 'array', items: { type: 'string' } },
          notes: { type: 'string' },
        },
      },
    },
    deferred: {
      type: 'array',
      items: {
        type: 'object',
        required: ['id', 'reason'],
        properties: { id: { type: 'string' }, reason: { type: 'string' } },
      },
    },
    sharedContracts: { type: 'array', items: { type: 'string' } },
    commitChunks: {
      type: 'array',
      items: {
        type: 'object',
        required: ['message', 'bugIds'],
        properties: {
          message: { type: 'string' },
          bugIds: { type: 'array', items: { type: 'string' } },
        },
      },
    },
  },
}

const ADVISOR_SCHEMA = {
  type: 'object',
  required: ['approved', 'blockingIssues', 'recommendations'],
  properties: {
    approved: { type: 'boolean' },
    blockingIssues: { type: 'array', items: { type: 'string' } },
    recommendations: { type: 'array', items: { type: 'string' } },
  },
}

const IMPL_SCHEMA = {
  type: 'object',
  required: ['group', 'filesEdited', 'bugsFixed', 'concerns'],
  properties: {
    group: { type: 'string' },
    filesEdited: { type: 'array', items: { type: 'string' } },
    bugsFixed: { type: 'array', items: { type: 'string' } },
    concerns: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' },
  },
}

const VERIFY_SCHEMA = {
  type: 'object',
  required: ['group', 'ok', 'concerns'],
  properties: {
    group: { type: 'string' },
    ok: { type: 'boolean' },
    concerns: { type: 'array', items: { type: 'string' } },
  },
}

const CONTEXT = `Project: Turán RMS — an 11-year-old Hungarian speaker-dependent isolated-word (command) speech recognizer in C#/.NET (Windows, x86, VS2013).
Pipeline: MFCC/LPC feature extraction -> DTW template matching. No build toolchain is available in this environment (no dotnet/mono/msbuild), so correctness MUST be established by careful code reading and diff inspection, NOT by compiling. Several source files are DUPLICATED across modules (e.g. H_FELDOLGOZO.cs x2, dtwApp_match.cs x2, HTK_Interface.cs x3) — a fix usually must be applied to every copy.
Authoritative bug list: ${ROADMAP}. Companion analysis: reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md.`

// ============================ PLAN STAGE ============================
let bugs = []
let grouping = null

if (doPlan) {
  phase('Scout')
  const scouted = await agent(
    `${CONTEXT}\n\nRead ${ROADMAP} in full. Extract EVERY bug as a structured record. For 'files', list the concrete source files each fix must touch, INCLUDING all duplicated copies (search the repo to confirm every copy). Mark 'live' true if the bug is on a runtime-reachable path.`,
    { label: 'scout-roadmap', phase: 'Scout', schema: BUGS_SCHEMA }
  )
  bugs = (scouted && scouted.bugs) || []
  log(`Scouted ${bugs.length} bugs from ${ROADMAP}`)

  phase('Plan')
  // Plan each bug, then peer-review it — pipelined (no barrier): a bug's review starts as soon as its plan is done.
  const planned = await pipeline(
    bugs,
    (bug) =>
      agent(
        `${CONTEXT}\n\nWrite a precise, surgical FIX PLAN for bug ${bug.id} — "${bug.title}" (severity ${bug.severity}; files: ${bug.files.join(', ')}).\n` +
          `Read the actual source at every listed location (and every duplicated copy) before planning. The plan MUST contain: (1) root-cause restated from the code; (2) the exact change per file/line, as before/after snippets; (3) every duplicated copy that needs the same change; (4) whether it changes on-disk/template/data formats or otherwise BREAKS backward compatibility (if so, how to stay compatible, e.g. versioned read); (5) any shared contract another bug's fix depends on (e.g. a serialization format shared between Creator.SerializeArray and Engine.DeSerializeArray); (6) how to self-verify WITHOUT a compiler (what to read/trace). Keep behavior changes minimal and well-justified. Do NOT edit any source yet.\n` +
          `Write the plan to plans/${bug.id}.md and return the structured summary.`,
        { label: `plan:${bug.id}`, phase: 'Plan', schema: PLAN_SCHEMA }
      ),
    (plan, bug) =>
      agent(
        `${CONTEXT}\n\nPeer-review the fix plan at ${plan.planPath} for bug ${bug.id} ("${bug.title}"). Read the plan AND the real source it targets. Check: correctness of root cause; whether the proposed change actually fixes it; completeness across ALL duplicated copies; backward-compat/data-format risks; off-by-one and integer-division traps; whether it introduces new bugs; whether shared contracts are consistent with other fixes. If you find problems, set approved=false and give concrete requiredChanges. Append a "## Peer review" section to ${plan.planPath} with your findings.`,
        { label: `review:${bug.id}`, phase: 'Review', schema: REVIEW_SCHEMA }
      ).then((rev) => ({ plan, bug, review: rev }))
  )

  const valid = planned.filter(Boolean)
  const unapproved = valid.filter((p) => p.review && !p.review.approved)
  log(`Planned ${valid.length} bugs; ${unapproved.length} flagged by peer review`)

  phase('Coordinate')
  // One coordinator builds the disjoint file-ownership map + logical commit chunks, and persists it.
  grouping = await agent(
    `${CONTEXT}\n\nYou are the implementation coordinator. Read every plan in plans/*.md.\n` +
      `Produce a plan for PARALLEL, CONFLICT-FREE implementation:\n` +
      `- Partition all bug fixes into GROUPS such that NO TWO GROUPS TOUCH THE SAME FILE (account for duplicated copies). Each group gets disjoint file ownership so its agent can run in parallel without write races.\n` +
      `- If a single bug spans files that would otherwise sit in different groups (e.g. a serialization format shared by Creator.cs and Engine.cs), either put all those files in one group OR pin the exact shared contract in 'sharedContracts' so both groups implement it identically.\n` +
      `- DEFER any change that is a broad cross-cutting refactor which would conflict with all parallel work (e.g. de-duplicating the triplicated files into a shared library) — list it in 'deferred' with a reason. Prefer stabilizing fixes over risky refactors.\n` +
      `- Propose LOGICAL COMMIT CHUNKS: an ordered list of commits, each grouping related bug fixes with a clear conventional-commit message. Keep docs/scaffolding separate from code fixes.\n` +
      `Write the result as JSON to plans/_grouping.json AND a human-readable plans/_PLAN_SUMMARY.md. Return the structured grouping.`,
    { label: 'coordinate', phase: 'Coordinate', schema: GROUPING_SCHEMA, effort: 'high' }
  )

  phase('Adversarial Review')
  // High-effort skeptic: try to find what will break before any code is touched.
  const adv = await agent(
    `${CONTEXT}\n\nAdversarial pre-implementation review. Read plans/_grouping.json, plans/_PLAN_SUMMARY.md, and the individual plans. Assume the plans are WRONG until proven right. Hunt specifically for: (a) two groups that actually touch the same file (race); (b) a duplicated copy a plan forgot; (c) a fix that silently breaks existing trained templates or data formats; (d) integer-division / off-by-one / vector-width mismatches the plans missed; (e) shared contracts that two groups would implement inconsistently; (f) fixes that cannot be verified without a compiler and are therefore high-risk. Be specific and cite files. Set approved=false if any blocking issue exists, and list concrete blockingIssues + recommendations. Append your review to plans/_PLAN_SUMMARY.md.`,
    { label: 'adversarial-review', phase: 'Adversarial Review', schema: ADVISOR_SCHEMA, effort: 'high' }
  )

  log(`Plan stage complete. Adversarial verdict: ${adv.approved ? 'APPROVED' : 'CHANGES REQUIRED'} (${adv.blockingIssues.length} blocking).`)
  if (!doImpl) {
    return {
      stage: 'plan',
      bugs: bugs.length,
      groups: grouping.groups.map((g) => ({ name: g.name, files: g.files, bugIds: g.bugIds })),
      deferred: grouping.deferred,
      commitChunks: grouping.commitChunks,
      adversarial: adv,
    }
  }
}

// ============================ IMPLEMENT STAGE ============================
if (doImpl) {
  phase('Implement')
  // When run as a separate invocation, re-read the persisted grouping.
  if (!grouping) {
    grouping = await agent(
      `Read plans/_grouping.json and return it exactly as the required structured object.`,
      { label: 'load-grouping', phase: 'Implement', schema: GROUPING_SCHEMA }
    )
  }
  const groups = (grouping && grouping.groups) || []
  const contracts = (grouping && grouping.sharedContracts) || []
  log(`Implementing ${groups.length} disjoint file groups in parallel.`)

  // Parallel implementation — groups own disjoint files, so no write races. Agents EDIT ONLY; they do not run git.
  const implResults = await parallel(
    groups.map((g) => () =>
      agent(
        `${CONTEXT}\n\nImplement the approved fixes for group "${g.name}". You own EXACTLY these files (and no others): ${g.files.join(', ')}. Bugs to fix: ${g.bugIds.join(', ')}.\n` +
          (contracts.length ? `Shared contracts you MUST honor verbatim: ${contracts.join(' | ')}.\n` : '') +
          `Follow the per-bug plans in plans/<BUG-ID>.md exactly. Apply each change to EVERY duplicated copy among your owned files. Keep edits surgical and match surrounding code style. Do NOT touch files outside your list. Do NOT run git. Since there is no compiler here, re-read each edited region to confirm the change is syntactically valid C# and complete. Report files edited, bugs fixed, and any remaining concerns.`,
        { label: `impl:${g.name}`, phase: 'Implement', schema: IMPL_SCHEMA }
      )
    )
  )
  const impl = implResults.filter(Boolean)
  log(`Implemented ${impl.reduce((n, r) => n + r.bugsFixed.length, 0)} fixes across ${impl.length} groups.`)

  phase('Verify')
  const verdicts = await parallel(
    groups.map((g) => () =>
      agent(
        `${CONTEXT}\n\nVerify the implementation for group "${g.name}" (files: ${g.files.join(', ')}; bugs: ${g.bugIds.join(', ')}). Inspect the working-tree changes with \`git diff -- ${g.files.join(' ')}\` and read the edited files. Confirm: every listed bug is actually fixed in every duplicated copy; no syntax breakage; no out-of-scope edits; shared contracts honored; no new defects introduced. Set ok=false with specific concerns if anything is wrong.`,
        { label: `verify:${g.name}`, phase: 'Verify', schema: VERIFY_SCHEMA }
      )
    )
  )
  const verify = verdicts.filter(Boolean)
  const failed = verify.filter((v) => !v.ok)
  log(`Verification: ${verify.length - failed.length}/${verify.length} groups clean.`)

  phase('Commit')
  // SINGLE sequential agent does all git work in logical chunks — no parallel git, no index races.
  const commitChunks = (grouping && grouping.commitChunks) || []
  const commitResult = await agent(
    `${CONTEXT}\n\nCommit the implemented fixes in LOGICAL CHUNKS. Steps:\n` +
      `1. Ensure you are NOT on master/main: \`git rev-parse --abbrev-ref HEAD\`. If on master/main, create and switch to branch '${BRANCH}'. Otherwise stay on the current non-default branch.\n` +
      `2. Make these commits IN ORDER, each staging only the relevant files, using the messages/grouping in plans/_grouping.json -> commitChunks: ${JSON.stringify(commitChunks)}.\n` +
      `3. Stage precisely (\`git add -- <files>\`) per chunk; verify with \`git status\` between commits. Do not stage unrelated files. Do not commit plans/_grouping.json's secrets — there are none, but keep scaffolding commits separate from code-fix commits.\n` +
      `4. End every commit message body with the line: 'Co-Authored-By: Claude Opus 4.8 (1M context) <noreply@anthropic.com>'.\n` +
      `Do NOT push. Report the branch name and the ordered list of commit hashes + subjects you created.`,
    { label: 'commit-logical-chunks', phase: 'Commit' }
  )

  phase('Document')
  const docResult = await agent(
    `${CONTEXT}\n\nFinalize documentation now that fixes are implemented and committed.\n` +
      `1. Update ${ROADMAP}: mark each fixed bug (e.g. add a '✅ Fixed (<commit-subject>)' note), mark deferred bugs as 'Deferred' with the reason from plans/_grouping.json, and keep statuses honest (note that fixes are code-review-verified, NOT compiler-verified, since no build toolchain exists here).\n` +
      `2. Write plans/PROGRESS.md capturing: what was fixed and where; per-bug outcome; ERRORS encountered during planning/implementation; INSIGHTS about the codebase (e.g. duplicated files, dead code, format-compat traps); LEARNINGS for the next run; and any verification gaps (no compiler).\n` +
      `3. Add a short 'Status (<date>)' note to the top of reports/Turan_RMS_architecture_and_ASR_comparison_2026-06-27.md pointing to ROADMAP.md and plans/PROGRESS.md.\n` +
      `Then commit these doc updates as a final logical chunk 'docs: record roadmap fix progress and learnings' on the same branch (sequential, end the message with the Co-Authored-By line). Report what you changed.`,
    { label: 'document', phase: 'Document' }
  )

  return {
    stage: STAGE,
    groups: groups.map((g) => g.name),
    implemented: impl,
    verification: verify,
    verifyFailures: failed,
    commit: commitResult,
    document: docResult,
  }
}
