namespace ManagedGitLib.Parsers

open FParsec

type TaggerHeader = { name: string; email: string; date: int64 }

type ParsedTag = { object: string
                   typeOf: string
                   name: string
                   tagger: TaggerHeader
                   additionalHeaders: AdditionalHeader array
                   message: string }

module private TagParsers =
    let objectParser: Parser<string, unit> =
        pstring "object" >>. spaces >>. CommonParsers.hashParser .>> newline

    let typeParser: Parser<string, unit> =
        pstring "type" >>. spaces >>. many1CharsTill anyChar newline

    let tagNameParser: Parser<string, unit> =
        pstring "tag" >>. spaces >>. many1CharsTill anyChar newline

    let taggerParser: Parser<TaggerHeader, unit> =
        pstring "tagger" >>. spaces >>. CommonParsers.signatureParser .>> newline >>= fun {name=name; email=email; date=date} ->
            preturn { name=name; email=email; date=date }

    let tagMessageParser: Parser<string, unit> =
        (many1 newline) >>. manyChars anyChar

    let tagParser: Parser<ParsedTag, unit> =
        parse { let! object = objectParser
                let! type' = typeParser
                let! name = tagNameParser
                let! tagger = taggerParser
                let! rawAdditionalHeaders = CommonParsers.allAdditionalHeadersParser
                let additionalHeaders = List.toArray rawAdditionalHeaders
                let! message = tagMessageParser
                return { object = object
                         typeOf = type'
                         name = name
                         tagger = tagger
                         additionalHeaders = additionalHeaders
                         message = message } }