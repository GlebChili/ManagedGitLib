namespace ManagedGitLib.Parsers

open FParsec

type AdditionalHeader = { name: string; value: string }

type SignatureRecord = { name: string; email: string; date: int64 }

module private CommonParsers =
    let whitespaces: Parser<unit, unit> =
        many (pchar ' ' <|> pchar '\t') >>. preturn ()

    let additionalHeaderValueParser: Parser<string, unit> =
        let rec innerValueParser: string -> Parser<string, unit> = fun acc ->
            manyCharsTill anyChar newline >>=
                fun line -> ((pchar ' ' >>. innerValueParser (acc + line + "\n")) <|> preturn (acc + line))
        innerValueParser ""

    let additionalHeaderParser: Parser<AdditionalHeader, unit> =
        (many1Chars (digit <|> letter <|> anyOf "_-.,:;!?@#$%^&*()[]")) .>> spaces >>=
            fun header_name -> additionalHeaderValueParser >>= fun header_value ->
                preturn { name = header_name; value = header_value }

    let allAdditionalHeadersParser: Parser<AdditionalHeader list, unit> =
        many additionalHeaderParser

    let hashParser: Parser<string, unit> =
        let rec innerHashParser: int -> char list -> Parser<char list, unit> = fun n acc ->
            match n with
            | num when num > 0 -> hex >>= (fun x -> innerHashParser (num - 1) (acc @ [x]))
            | _ -> preturn acc
        attempt (innerHashParser 40 []) >>= fun x -> preturn ((new System.String (List.toArray x)).ToLower())

    let signatureParser: Parser<SignatureRecord, unit> =
        parse { let! nameRaw = (manyCharsTill anyChar (pstring "<"))
                // LibGit2Sharp do some weird name post processing by trimming some characters.
                // In order to comply with LibGit2Sharp, we are trying to mimiс such behaviour here.
                let name = nameRaw.Trim(List.toArray [' '; '"'; '.'])
                let! email = manyCharsTill anyChar (pchar '>')
                do! spaces
                let! date = pint64
                do! spaces
                let! _ = pchar('+') <|> pchar('-')
                do! digit >>. digit >>. digit >>. digit >>. whitespaces
                return { name = name; email = email; date = date } }
