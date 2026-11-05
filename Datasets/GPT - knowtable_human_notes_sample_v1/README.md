# Knowtable realistic-note sample v1

This sample contains 10 source notes and 10 separate `-tags.json` reference files for a single fictional writing project, *The West Wing Fire*.

## Files

- Each `.md` file is a source document.
- The matching `*-tags.json` contains proposed reference tags with `importance` and `confidence` values from 0 to 1, plus `basis` (`explicit` or `inferred`).
- `relationships.json` records a few intended card links for later grouped-context experiments. It is not part of any source note's tag output.

## Important test protocol

For a **standalone extraction** run, give the extractor only the contents of the `.md` file. Do not pass the filename or `relationships.json` as input. This matters especially for `mara-personality.md`: the body does not name Mara, so a standalone extractor should not be expected to infer her identity from the filename. The corresponding tag file is intentionally about the body alone and does not inherit `Mara Venn` from a parent card.

For a **group-context** run, explicitly provide the selected related notes and their relationship metadata as context. Compare those results separately; don't treat inherited tags as if they were explicit in the subcard.

## About the annotations

These are initial human-style reference judgments, not objective ground truth. Importance means topical centrality, not confidence. Confidence means how strongly the note supports the tag. A highly important tag can have lower confidence when the concept is only implied. Scores are not calibrated probabilities. Please review them before using them as supervised fine-tuning targets.

Each file has fewer than 50 tags by design. The list favors distinct useful concepts over exhausting every possible association. Some low-priority concepts are omitted intentionally, and reasonable annotators may disagree.

## Suggested first use

1. Run YAKE, KeyBERT, or another extractor against the 10 source notes.
2. Compare its phrases against the companion tag file, allowing for label/synonym differences rather than requiring exact string matches.
3. Record missing important concepts, irrelevant outputs, redundancy, unsupported inference, and whether the importance ordering is useful.
4. Only after reviewing the reference tags, decide which examples are appropriate training targets.
