namespace ManagedGitLib.Parsers

open FParsec

type CommitAuthorHeader = { name: string; email: string; date: int64 }
type CommitCommitterHeader = { name: string; email: string; date: int64 }
type CommitTreeHeader = { hash: string }
type CommitParentHeader = { hash: string }

type ParsedCommit = { tree: CommitTreeHeader
                      parents: CommitParentHeader array
                      author: CommitAuthorHeader
                      committer: CommitCommitterHeader
                      additionalHeaders: AdditionalHeader array
                      message: string }

module private CommitParsers =
    let authorParser: Parser<CommitAuthorHeader, unit> =
        pstring "author" >>. spaces >>. CommonParsers.signatureParser .>> newline >>= fun {name = name; email = email; date = date} ->
            preturn  {name = name; email = email; date = date}

    let committerParser: Parser<CommitCommitterHeader, unit> =
        pstring "committer" >>. spaces >>. CommonParsers.signatureParser .>> newline >>= fun {name = name; email = email; date = date} ->
            preturn {name = name; email = email; date = date}

    let treeParser: Parser<CommitTreeHeader, unit> =
        pstring "tree" >>. spaces >>. CommonParsers.hashParser .>> newline >>=
            fun hash -> preturn {hash = hash}

    let parentParser: Parser<CommitParentHeader, unit> =
        pstring "parent" >>. spaces >>. CommonParsers.hashParser .>> newline >>=
            fun hash -> preturn {hash = hash}

    let allParentsParser: Parser<CommitParentHeader list, unit> =
        many parentParser

    let messageParser: Parser<string, unit> =
        (many1 newline) >>. manyChars anyChar

    let committParser: Parser<ParsedCommit, unit> =
        parse { let! tree = treeParser
                let! parents = allParentsParser
                let! author = authorParser
                let! commiter = committerParser
                let! additionalHeaders = CommonParsers.allAdditionalHeadersParser
                let! message = messageParser
                return {tree = tree
                        parents = List.toArray parents
                        author = author
                        committer = commiter
                        additionalHeaders = List.toArray additionalHeaders
                        message = message} }
