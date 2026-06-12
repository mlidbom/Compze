export const meta = {
  name: 'team-review-and-fix-code-standard-issues',
  description: 'Distill the project coding standards into actionable checks, fan out one hunter per check to search the whole codebase for violations (+ fix suggestions), then judge and adversarially vet them into a high-confidence worklist (no code changes)',
  phases: [
    { title: 'Distill', detail: 'one agent turns the standards (+ rationale sidecars) into actionable, searchable checks' },
    { title: 'Hunt', detail: 'one agent per check searches the whole codebase for violations + fixes' },
    { title: 'Synthesize', detail: 'judge: dedup across checks, resolve conflicts, rank, drop noise' },
    { title: 'Vet', detail: '3 adversarial lenses per candidate (validity / improvement / risk)' },
  ],
}

// ---- args (all optional) ----
// args may arrive as an object or as a JSON string depending on how the workflow was invoked; normalize once.
let A = args
if (typeof A === 'string') { try { A = JSON.parse(A) } catch { A = undefined } }

const GATE = (A && typeof A.gate === 'number') ? A.gate : 0.75
const INCLUDE_JUDGMENT_CALLS = !!(A && A.includeJudgmentCalls)

// One canonical empty/short-circuit result so every early return carries the full documented shape.
function emptyResult(extra) {
  return Object.assign({ confirmed: [], readyToApply: [], needsDesign: [], borderline: [], rejected: [], conflicts: [], droppedLowConfidence: 0, judgeNotes: '', countsByRule: {}, rawFindingCount: 0 }, extra || {})
}

// ---- schemas ----
const CHECK_PROPS = {
  id: { type: 'string' }, // K001, K002, ...
  ruleId: { type: 'string' }, // rule file name or title this check derives from
  ruleTitle: { type: 'string' },
  severity: { type: 'string', enum: ['high', 'medium', 'low'] },
  signature: { type: 'string' }, // precisely what a violation LOOKS LIKE in code
  huntStrategy: { type: 'string' }, // how to FIND it (grep patterns / what to scan / what to read)
  fixPattern: { type: 'string' }, // what a correct fix generally looks like
}
const CHECKS_SCHEMA = {
  type: 'object',
  properties: {
    checks: { type: 'array', items: { type: 'object', properties: CHECK_PROPS, required: ['id', 'ruleId', 'ruleTitle', 'severity', 'signature', 'huntStrategy', 'fixPattern'] } },
    notes: { type: 'string' },
  },
  required: ['checks', 'notes'],
}

const FINDING_PROPS = {
  file: { type: 'string' },
  line: { type: 'integer' },
  severity: { type: 'string', enum: ['high', 'medium', 'low'] },
  confidence: { type: 'number' },
  title: { type: 'string' },
  current: { type: 'string' },
  proposal: { type: 'string' },
  rationale: { type: 'string' },
}
const FINDING_REQ = ['file', 'line', 'severity', 'confidence', 'title', 'current', 'proposal', 'rationale']
const HUNT_SCHEMA = {
  type: 'object',
  properties: { checkId: { type: 'string' }, findings: { type: 'array', items: { type: 'object', properties: FINDING_PROPS, required: FINDING_REQ } } },
  required: ['checkId', 'findings'],
}

const CANDIDATE_PROPS = Object.assign({ id: { type: 'string' }, checkId: { type: 'string' }, ruleId: { type: 'string' } }, FINDING_PROPS)
const WORKLIST_SCHEMA = {
  type: 'object',
  properties: {
    candidates: { type: 'array', items: { type: 'object', properties: CANDIDATE_PROPS, required: ['id', 'ruleId'].concat(FINDING_REQ) } },
    conflicts: { type: 'array', items: { type: 'object', properties: { ids: { type: 'array', items: { type: 'string' } }, issue: { type: 'string' }, resolution: { type: 'string' } }, required: ['issue', 'resolution'] } },
    droppedLowConfidence: { type: 'integer' },
    notes: { type: 'string' },
  },
  required: ['candidates', 'conflicts', 'droppedLowConfidence', 'notes'],
}
const VERDICT_SCHEMA = {
  type: 'object',
  properties: { pass: { type: 'boolean' }, confidence: { type: 'number' }, reasoning: { type: 'string' }, suggestedRevision: { type: 'string' } },
  required: ['pass', 'confidence', 'reasoning'],
}

// ---- prompts ----
function distillPrompt() {
  return [
    'You are turning this project\'s coding standards into a flat list of ACTIONABLE, INDEPENDENTLY-SEARCHABLE checks that other agents will each hunt for across the codebase.',
    '',
    'The standards are ALREADY in your context — the project rules / CLAUDE.md are auto-loaded, with their file paths shown. You do NOT need to go re-read the rule bodies. Two things you DO need from disk: read the `.rationale.txt` sidecar beside each rule file (those carry the load-bearing "why" and are NOT auto-loaded), and skim any rule a CLAUDE.md references but does not inline. Find them from the paths already shown to you — do not assume a fixed rules folder.',
    '',
    'Convert the standards into checks, decomposing IF/AS REQUIRED:',
    '- A simple rule may be a single check. A compound rule MUST be split so each check describes ONE concrete violation shape (e.g. "avoid primitives" → "a method takes >=2 primitive params" AND "a raw id/handle primitive is threaded where a domain type belongs").',
    '- Each check must be huntable by an agent that has the rule but not the others. Cover every rule; do not invent checks the rules do not support.',
    '',
    'For each check: id (K001, K002, ...), ruleId (the rule file name or title), ruleTitle, severity (high/medium/low — how load-bearing the rule is), signature (precisely what a violation LOOKS LIKE in code), huntStrategy (how to FIND it — concrete grep patterns / what to scan / what to read, tailored to THIS violation; some rules grep cleanly, cohesion rules need a structural survey), fixPattern (what a correct fix generally looks like). Return the schema.',
  ].join('\n')
}

function huntPrompt(ch, A) {
  return [
    'You hunt the WHOLE codebase for every violation of ONE specific coding-standard check and propose a concrete fix for each. False positives are costly — report only genuine violations; finding none is a fine result, do NOT invent issues.',
    '',
    'Your check:',
    '- ' + ch.id + ' — rule: ' + ch.ruleId + ' (' + ch.ruleTitle + ')   severity: ' + ch.severity,
    '- A violation looks like: ' + ch.signature,
    '- How to hunt: ' + ch.huntStrategy,
    '- A fix looks like: ' + ch.fixPattern,
    '',
    (A && A.target) ? ('Search within: ' + A.target) : 'Search the first-party source — the app/library code this repo exists to build. Find that root yourself; read a project-orientation/architecture doc if one exists so you skip the right things.',
    'EXCLUDE legacy/superseded code, vendored code and submodules, build output (bin/obj/node_modules), and generated files.' + ((A && A.exclude) ? (' Also exclude: ' + A.exclude + '.') : ''),
    '',
    'SEARCH — do not read everything: lead with Grep/Glob patterns fit to THIS violation, then Read each candidate to confirm in context (the code around a match may make it a non-issue, or a real carve-out). The cited rule is already in your context; read its `.rationale.txt` sidecar if you need the why. Honor the rules\' own carve-outs (e.g. the defensive-coding point-of-use exception).',
    'For each real violation: file, line, severity of THIS instance, honest confidence 0-1 (below 0.5 for genuine judgment calls), short title, current code, a concrete proposed fix, terse rationale tied to the rule. Echo your checkId (' + ch.id + '). Return the schema.',
  ].join('\n')
}

function judgePrompt(findings) {
  return [
    'You are consolidating raw rule-violation findings (each produced by an agent hunting for ONE check across the codebase) into ONE deduplicated, ranked candidate worklist. Be a strict gatekeeper — false positives are costly — but do NOT drop a real violation just because the same rule is broken in many places (each place is its own candidate).',
    '',
    'Raw findings as JSON (each carries checkId + ruleId):',
    JSON.stringify(findings),
    '',
    'Do:',
    '1. Deduplicate: merge findings at the same file:line. If several checks/rules flag the same code, keep ONE candidate under the most severe rule and name the other rules it also violates in its rationale. Keep the clearest current/proposal/rationale.',
    '2. Resolve conflicts: when two rules pull opposite ways on the same code, record it in conflicts with a concrete resolution (prefer the simpler, higher-cohesion end state; never recommend churn).',
    '3. Drop low-signal noise (speculative, or weak confidence x severity); count them in droppedLowConfidence. Never drop anything because a convention/analyzer/"by design" endorses the current shape.',
    '4. Assign each surviving candidate a stable id: C001, C002, ... Preserve checkId/ruleId/file/line/severity/confidence/current/proposal/rationale.',
    '5. Rank by severity then confidence.',
    'Return the schema. Apply no changes.',
  ].join('\n')
}

const LENSES = [
  { key: 'validity', instr: 'VALIDITY: Is this a genuine violation of the cited rule as written, including carve-outs? Re-read the rule (it is in your context) AND its `.rationale.txt` sidecar, then the actual code. REJECT every one of these NON-DEFENSES — none makes a real violation disappear: "it is the conventional purpose of such a type/file"; "a framework or analyzer (e.g. CA1060) endorses it"; "it is by design"; "no domain type exists yet" (introducing one is the work); "wrapping would be circular" (the boundary returns a thin handle, the richer type wraps it). DO honor the real defensive-coding carve-out (point-of-use validation of intractable external/OS state is correct). pass=true only if a rule-grounded violation remains after discarding those non-defenses.' },
  { key: 'improvement', instr: 'IMPROVEMENT: Would the proposed change actually improve simplicity, cohesion, and clarity, or is it churn that merely relocates/renames without reducing complexity? Does the proposed home/type genuinely fit? pass=true only if it is a clear net improvement; if the diagnosis is right but the fix is weak, pass=true and put a better fix in suggestedRevision.' },
  { key: 'risk', instr: 'RISK: Could applying this harm correctness, break an invariant, change observable behavior, or hurt performance/UX? Would it strip a load-bearing guard or move a member away from state it needs? pass=true only if applying it is SAFE.' },
]

function skepticPrompt(c, lens) {
  return [
    'You are an adversarial reviewer whose job is to KILL a proposed refactoring unless it clearly survives. Default to skepticism.',
    '',
    'Candidate ' + c.id + ':',
    '- File: ' + c.file + '   Line: ' + c.line,
    '- Rule cited: ' + c.ruleId,
    '- Title: ' + c.title,
    '- Current: ' + c.current,
    '- Proposal: ' + c.proposal,
    '- Rationale: ' + c.rationale,
    '',
    'VERIFY AGAINST REALITY FIRST: read the actual file(s) and the cited rule (+ its rationale sidecar — it is on disk, not auto-loaded) before judging — the snippet may be stale or out of context.',
    '',
    'Apply ONLY this lens:',
    lens.instr,
    '',
    'Give an honest confidence 0-1 and terse reasoning grounded in what you read. Return the schema.',
  ].join('\n')
}

// ---- Phase 1: Distill (one agent turns the standards into actionable checks — barrier before the fan-out) ----
phase('Distill')
let checks = (A && A.checksOverride)
if (!checks || !checks.length) {
  const distilled = await agent(distillPrompt(), { label: 'distill', phase: 'Distill', agentType: 'general-purpose', schema: CHECKS_SCHEMA })
  if (!distilled) return emptyResult({ judgeNotes: 'Distill was skipped — cannot hunt without checks.' })
  checks = distilled.checks
}
if (!checks.length) return emptyResult({ judgeNotes: 'No checks distilled from the standards.' })
log('Distill: ' + checks.length + ' actionable checks')

// ---- Phase 2: Hunt (one agent per check, in parallel; each sweeps the whole codebase for its one violation) ----
phase('Hunt')
const huntResults = await parallel(checks.map(ch => () =>
  agent(huntPrompt(ch, A), { label: 'hunt:' + ch.id, phase: 'Hunt', agentType: 'general-purpose', schema: HUNT_SCHEMA })
))
const findings = huntResults
  .map((r, i) => ({ r, ch: checks[i] }))
  .filter(x => x.r)
  .flatMap(({ r, ch }) => (r.findings || []).map(f => Object.assign({ checkId: ch.id, ruleId: ch.ruleId }, f)))
log('Hunt: ' + findings.length + ' raw violations across ' + checks.length + ' checks')
if (findings.length === 0) return emptyResult({ judgeNotes: 'No violations found.' })

// ---- Phase 3: Synthesize (judge dedups/ranks) ----
phase('Synthesize')
const worklist = await agent(judgePrompt(findings), { label: 'synthesize', phase: 'Synthesize', schema: WORKLIST_SCHEMA })
if (!worklist) return emptyResult({ judgeNotes: 'Synthesis was skipped.', rawFindingCount: findings.length })
log('Synthesized ' + worklist.candidates.length + ' candidates (dropped ' + worklist.droppedLowConfidence + ' low-confidence findings)')
if (worklist.candidates.length === 0) return emptyResult({ conflicts: worklist.conflicts, droppedLowConfidence: worklist.droppedLowConfidence, judgeNotes: worklist.notes, rawFindingCount: findings.length })

// ---- Phase 4: Adversarial vet (3 lenses per candidate; validity+improvement gate surfacing, risk gates application) ----
phase('Vet')
const vetted = await parallel(worklist.candidates.map(c => () =>
  parallel(LENSES.map(lens => () =>
    agent(skepticPrompt(c, lens), { label: 'vet:' + c.id + ':' + lens.key, phase: 'Vet', agentType: 'general-purpose', schema: VERDICT_SCHEMA })
  )).then(verdicts => {
    const byKey = {}
    LENSES.forEach((lens, i) => { byKey[lens.key] = verdicts[i] || null })
    const v = byKey.validity, im = byKey.improvement, rk = byKey.risk
    // Two SEPARATE questions: is it a real violation worth fixing (validity+improvement)? is the PROPOSED FIX
    // safe to apply as-written (risk)? A risk failure gates auto-application, NOT whether the finding surfaces.
    const diagnosisSound = !!(v && v.pass && im && im.pass)
    const diagnosisConfidence = (v && im) ? ((v.confidence || 0) + (im.confidence || 0)) / 2 : 0
    const applyReady = !!(rk && rk.pass)
    return Object.assign({}, c, {
      verdicts: byKey, diagnosisSound, diagnosisConfidence, applyReady,
      confirmed: diagnosisSound && diagnosisConfidence >= GATE,
      applyBlocker: applyReady ? null : ((rk && rk.reasoning) || 'risk lens did not pass'),
    })
  })
))

const judged = vetted.filter(Boolean)
const confirmed = judged.filter(v => v.confirmed)            // real, worth-doing violations at the gate
const readyToApply = confirmed.filter(v => v.applyReady)     // ...and the proposed fix is safe as-written
const needsDesign = confirmed.filter(v => !v.applyReady)     // ...but the fix needs design first (risk-flagged)
const borderline = INCLUDE_JUDGMENT_CALLS ? judged.filter(v => v.diagnosisSound && !v.confirmed) : []
const rejected = judged.filter(v => !v.diagnosisSound).map(v => ({
  id: v.id, checkId: v.checkId, ruleId: v.ruleId, file: v.file, line: v.line, title: v.title, diagnosisConfidence: v.diagnosisConfidence,
  failedLenses: ['validity', 'improvement'].filter(k => !(v.verdicts[k] && v.verdicts[k].pass)),
  reasons: ['validity', 'improvement'].map(k => k + ': ' + ((v.verdicts[k] && v.verdicts[k].reasoning) || 'no verdict')),
}))
log('Vetting: ' + confirmed.length + ' confirmed (' + readyToApply.length + ' ready, ' + needsDesign.length + ' need fix-design), ' + borderline.length + ' borderline, ' + rejected.length + ' rejected')

const countsByRule = {}
confirmed.forEach(c => { countsByRule[c.ruleId] = (countsByRule[c.ruleId] || 0) + 1 })

return {
  confirmed,
  readyToApply,
  needsDesign,
  borderline,
  rejected,
  conflicts: worklist.conflicts,
  droppedLowConfidence: worklist.droppedLowConfidence,
  judgeNotes: worklist.notes,
  countsByRule,
  rawFindingCount: findings.length,
  candidateCount: worklist.candidates.length,
  checkCount: checks.length,
  gate: GATE,
}
