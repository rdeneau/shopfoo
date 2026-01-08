[<RequireQualifiedAccess>]
module internal Shopfoo.Data.Xml

open System.IO
open System.Xml.Serialization
open Shopfoo.Data.Serialization

[<Literal>]
let MediaType = "application/xml"

type Serializer() =
    interface IXmlSerializer with
        member val ContentType = MediaType

        member _.Serialize(source: obj) =
            let serializer = XmlSerializer(source.GetType())
            use stringWriter = new StringWriter()
            serializer.Serialize(stringWriter, source)
            stringWriter.ToString()

        member _.Deserialize<'a>(content: string) =
            let serializer = XmlSerializer(typeof<'a>)
            use stringReader = new StringReader(content)
            serializer.Deserialize(stringReader) :?> 'a