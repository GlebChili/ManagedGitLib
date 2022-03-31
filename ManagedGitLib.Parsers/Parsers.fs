namespace ManagedGitLib.Parsers

exception CommitParsingException of string

open FParsec

module Parsers =
    let ParseCommitt committText =
        let parsingResult = run CommitParsers.committParser committText
        match parsingResult with
        | Success (r, _, _) -> r
        | Failure (m, _, _) -> raise (CommitParsingException $"Unable to parse commit object. Parsing error: {m}")
