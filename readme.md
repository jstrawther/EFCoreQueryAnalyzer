# Query Analyzer

A console app to analyze query trace output from EntityFrameworkCore and detect repetitive/redundant and long-running queries.

- Distinct SELECT clauses occurring more than X times
- Distinct queries occurring more than X times
- Distinct FROM clauses occuring more than X times
- Queries taking longer than the average query runtime for the batch
