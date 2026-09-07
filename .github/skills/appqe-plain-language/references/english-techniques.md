<!-- appqe:managed v0.14.8 | do-not-edit-manually | regenerate: appqe sync -->
# English techniques

Numeric targets are tripwires for a second look, not hard caps.

## Relevant - what goes in

- **Cut throat-clearing.** "It is important to note that", "As previously
  mentioned", "In order to". Delete the opener, keep the clause it delayed.
- **Cut the process.** Which step ran, which model, how many retries, how many
  files you read. The telemetry already has it and the reader does not need it.
- **Cut defensive hedging aimed at yourself.** "It may possibly be the case
  that" protects the writer, not the reader.

## Findable - structure

- **The verdict first.** One sentence, at the top, stating what is wrong or what
  was decided. Reasons after.
- **Headings state the message**, not the category. "PUT /user accepts an invalid
  email" beats "Validation findings".
- **One finding per paragraph.** Five sentences is the tripwire for splitting.
- **A list for three or more parallel items.** Keep items grammatically parallel.
- **A table when the reader will compare values.** Prose for reasoning.

## Understandable - wording

- **One idea per sentence.** Twenty words is the tripwire; two ideas in one
  sentence is the actual defect.
- **Active voice with a named actor.** "The API returns 200" not "a 200 is
  returned".
- **Verbs, not nominalisations.** "decide" not "make a determination"; "fails"
  not "exhibits a failure condition".
- **Common words.** use / not utilise. about / not approximately. help / not
  facilitate. Unless the rarer word is the term of art.
- **One name per thing, for the whole document.** Pick "the login flow" or "the
  sign-in journey" and never alternate. Synonym variety helps prose and hurts
  documents.
- **Gloss an abbreviation at first use.** AC, RCA, TRX, POM.
- **One hedge, when the uncertainty is real.** "may potentially, in some cases"
  becomes "may". Never delete a hedge that carries genuine doubt.

## Usable - what the reader does next

- **Instructions as imperatives with an object.** "Add a 422 case for a duplicate
  email" not "additional validation coverage should be considered".
- **Every obligation names its owner.**
- **Condition before instruction**, so a reader can stop early: "If the suite is
  red for this reason, quarantine the test."
- **End with the next step**, when there is one.

## Quantities and evidence

- Give the number, the unit, and what it was measured against. "12 of 23 ACs
  covered" beats "coverage is low".
- Quote an exact string when the reader will search for it: an endpoint, a status
  code, a test name, an AC id.
- Say what you ran. A claim about behaviour carries the command or the query that
  produced it.

## Self-check before you submit

1. Can the named reader say, after one reading, what is wrong and what to do?
2. Is the verdict in the first two lines?
3. Any sentence carrying two ideas? Any paragraph carrying two findings?
4. Any term used under two names? Any abbreviation left unglossed?
5. Did every number, condition, scope qualifier and honest hedge survive?
6. Would this be safe to paste into a client-visible ticket unedited?
