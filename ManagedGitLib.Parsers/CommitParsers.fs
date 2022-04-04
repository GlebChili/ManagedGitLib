namespace ManagedGitLib.Parsers

open FParsec

type CommitAuthorHeader = { name: string; email: string; date: int64 }
type CommitCommitterHeader = { name: string; email: string; date: int64 }
type CommitTreeHeader = { hash: string }
type CommitParentHeader = { hash: string }
type CommitAdditionalHeader = { name: string; value: string }

type ParsedCommit = { tree: CommitTreeHeader
                      parents: CommitParentHeader array
                      author: CommitAuthorHeader
                      committer: CommitCommitterHeader
                      additionalHeaders: CommitAdditionalHeader array
                      message: string }

module private CommitParsers =
    let authorCommiterDataReader: Parser<string * string * int64, unit> =
        parse { let! nameRaw = (manyCharsTill anyChar (pstring "<"))
                // LibGit2Sharp do some weird name post processing by trimming some characters.
                // In order to comply with LibGit2Sharp, we are trying to mimiс such behaviour here.
                let name = nameRaw.Trim(List.toArray [' '; '"'; '.'])
                let! email = manyCharsTill anyChar (pchar '>')
                do! spaces
                let! date = pint64
                let! _ = manyCharsTill anyChar newline
                return (name, email, date) }

    let hashParser: Parser<string, unit> =
        let rec innerHashParser: int -> char list -> Parser<char list, unit> = fun n acc ->
            match n with
            | num when num > 0 -> hex >>= (fun x -> innerHashParser (num - 1) (acc @ [x]))
            | _ -> preturn acc
        attempt (innerHashParser 40 []) >>= fun x -> preturn ((new System.String (List.toArray x)).ToLower())

    let additionalHeaderValueParser: Parser<string, unit> =
        let rec innerValueParser: string -> Parser<string, unit> = fun acc ->
            manyCharsTill anyChar newline >>=
                fun line -> ((pchar ' ' >>. innerValueParser (acc + line + "\n")) <|> preturn (acc + line))
        innerValueParser ""

    let additionalHeaderParser: Parser<CommitAdditionalHeader, unit> =
        (many1Chars (digit <|> letter <|> anyOf "_-.,:;!?@#$%^&*()[]")) .>> spaces >>=
            fun header_name -> additionalHeaderValueParser >>= fun header_value ->
                preturn { name = header_name; value = header_value }

    let allAdditionalHeadersParser: Parser<CommitAdditionalHeader list, unit> =
        many additionalHeaderParser

    let authorParser: Parser<CommitAuthorHeader, unit> =
        pstring "author" >>. spaces >>. authorCommiterDataReader >>= fun (name, email, date) ->
            preturn  {name = name; email = email; date = date}

    let committerParser: Parser<CommitCommitterHeader, unit> =
        pstring "committer" >>. spaces >>. authorCommiterDataReader >>= fun (name, email, date) ->
            preturn {name = name; email = email; date = date}

    let treeParser: Parser<CommitTreeHeader, unit> =
        pstring "tree" >>. spaces >>. hashParser .>> newline >>=
            fun hash -> preturn {hash = hash}

    let parentParser: Parser<CommitParentHeader, unit> =
        pstring "parent" >>. spaces >>. hashParser .>> newline >>=
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
                let! additionalHeaders = allAdditionalHeadersParser
                let! message = messageParser
                return {tree = tree
                        parents = List.toArray parents
                        author = author
                        committer = commiter
                        additionalHeaders = List.toArray additionalHeaders
                        message = message} }
