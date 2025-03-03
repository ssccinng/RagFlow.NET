namespace RagFlow.NET.Fs
open FSharp.Data
open System
open System.Text.Json
open System.Text.Json.Serialization
open System.IO
open System.Text
type CreateDbResponse = JsonProvider<Sample="./ApiJson/CreateDb.json", SampleIsList=true>
type ListChatAssistantsResponse = JsonProvider<Sample="./ApiJson/ListChatAssistants.json", SampleIsList=true>
type CreateSessionResponse = JsonProvider<Sample="./ApiJson/CreateSession.json", SampleIsList=true>
type ConverseWithChatAssistantResponse = JsonProvider<Sample="./ApiJson/ConverseWithChatAssistant.json", SampleIsList=true>
type DeleteSessionResponse = JsonProvider<Sample="./ApiJson/DeleteSession.json", SampleIsList=true>
type UpdateSessionResponse = JsonProvider<Sample="./ApiJson/UpdateSession.json", SampleIsList=true>


module Urls = 
    let createDb = "/api/v1/datasets"
    let listChatAssistants = "/api/v1/chats"


    /// <summary>
    /// 创建一个ragflow客户端
    /// </summary>
    let createSession = "/api/v1/chats/{0}/sessions"
    [<Literal>]
    let updateSession = "/api/v1/chats/%s/sessions/%s"
    [<Literal>]
    let deleteSession = "/api/v1/chats/%s/sessions"
    //let listA = "/api/v1/chats/{chat_id}/sessions"
    let ConverseWithChatAssistant = "/api/v1/chats/{0}/completions"
    [<Literal>]
    let listChatAssistantSessions = "/api/v1/chats/%s/sessions"




type Client = {
    Key: string
    //Id: string
    BaseUrl: string
}

type ChatSession = {
    Key: string
    ChatId: string
    BaseUrl: string
    SessionId: string option
    Name: string
    // 加个名字?
}

type ChatAssistant = {
    Id: string
    Name: string
    CreateTime: DateTime
    UpdateTime: DateTime
}

// type AnswerReference = {
//     Answer: string
//     AudioBinary: byte array option
// }

type ChatResponse = {
    Answer: string
    AudioBinary: byte array option
    SessionId: string
    Id: string
    //AnswerReference: string option
}


module Requests = 
    type ListChatAssistants = {
        Page: int
        PageSize: int
        OrderBy: string
        Desc: bool
        Name: string
        Id: string
    }
    with static member Defalut = { Page = 1; PageSize = 30; OrderBy = "create_time"; Desc = true; Name = ""; Id = "" }




module RagFlowClient =
    /// <summary>
    /// 创建一个ragflow客户端
    /// </summary>
    /// <param name="key"></param>
    /// <param name="baseUrl"></param>
    let createClient (key: string) (baseUrl: string) = { Key = key; BaseUrl = baseUrl }
    /// <summary>
    /// 列出所有的chat assistant
    /// </summary>
    /// <param name="page"></param>
    /// <param name="pageSize"></param>
    /// <param name="orderBy"></param>
    /// <param name="desc"></param>
    /// <param name="name"></param>
    /// <param name="id"></param>
    /// <param name="session"></param>
    let listChatAssistants (page: int) (pageSize: int) (orderBy: string) (desc: bool)  (id: string) (name: string) (session: Client) =
        let route = $"{session.BaseUrl}{Urls.listChatAssistants}"
        let url = String.Format (route, page, pageSize, orderBy, desc, name, id)

        let query = [ "page", page.ToString(); "page_size", pageSize.ToString(); "orderby", orderBy; "desc", desc.ToString(); "name", name; "id", id ] 
                    |> List.filter (fun (k, v) -> not( String.IsNullOrWhiteSpace v) )
                    //|> List.map (fun (k, v) -> $"{k}={v}")
                    //|> String.concat "&"
        try 
            let res = Http.RequestString(url, headers = [ "Authorization", "Bearer " + session.Key ], query = query , httpMethod = "Get") |> ListChatAssistantsResponse.Parse
            if res.Code = 0 then Ok (res.Data |> Array.map (fun x -> { Id = x.Id.ToString("N"); Name = x.Name; CreateTime = DateTimeOffset.FromUnixTimeMilliseconds(x.CreateTime).DateTime; UpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(x.UpdateTime).DateTime }))
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message


    /// <summary>
    /// 列出所有的chat assistant
    /// </summary>
    let listChatAssistantsByName = listChatAssistants 1 30 "create_time" true ""


    /// <summary>
    /// 创建一个chat session
    /// </summary>
    let createChatSession (name: string) (chatai: ChatAssistant) (session: Client)=
        let route = $"{session.BaseUrl}{Urls.createSession}"
        let url = String.Format (route, chatai.Id)
        let body = $"{{\"name\": \"{name}\" }}"
        
        try 
        
            let res = 
                    Http.RequestString
                        (url, headers = [ "Authorization", "Bearer " + session.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest(body), httpMethod = "Post") 
                    |> CreateSessionResponse.Parse
            if res.Code = 0 then Ok { Key = session.Key; 
                                        ChatId = chatai.Id; 
                                        SessionId = Some (res.Data.Value.Id.ToString("N")); 
                                        BaseUrl = session.BaseUrl; Name = name }
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message

    /// <summary>
    /// 创建一个默认的chat session
    /// </summary>
    let createDefaultChatSession  (chatId: string) (session: Client)
        = { Key = session.Key; ChatId = chatId; SessionId = None; BaseUrl = session.BaseUrl; Name = "" }

    let listChatAssistantSessions (chatId: string) (session: ChatSession) =
        let route = $"{session.BaseUrl}{Urls.listChatAssistantSessions}"
        let url = String.Format (route, chatId)
        try 
            let res = Http.RequestString(url, headers = [ "Authorization", "Bearer " + session.Key ], httpMethod = "Get") |> CreateSessionResponse.Parse
            if res.Code = 0 then Ok res
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message


    let updateChatSession name (session: ChatSession) =
        let url = sprintf Urls.updateSession session.ChatId session.SessionId.Value
        let body = $"{{\"name\": \"{name}\" }}"
        try 
            let res = 
                    Http.RequestString
                        (url, headers = [ "Authorization", "Bearer " + session.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest body, httpMethod = "Put") 
                    |> UpdateSessionResponse.Parse
            if res.Code = 0 then Ok { session with Name = name }
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message

    let deleteChatSession (sessions: ChatSession array) = 
        if Array.isEmpty sessions then Ok ()
        else
            let session = sessions.[0]
            let url = sprintf Urls.deleteSession session.ChatId
            try 
                let body = JsonSerializer.Serialize {|ids = sessions |> Array.map (fun x -> x.SessionId.Value) |}
                let res = 
                        Http.RequestString
                            (url, headers = [ "Authorization", "Bearer " + session.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest body, httpMethod = "Delete")
                        |> DeleteSessionResponse.Parse
                if res.Code = 0 then Ok ()
                else Error (res.Message |> Option.defaultValue "Error")
            with ex -> Error ex.Message



    let converseWithChatAssistant (message: string) (stream: bool) (session: ChatSession) =
        let route = $"{session.BaseUrl}{Urls.ConverseWithChatAssistant}"
        let url = String.Format (route, session.ChatId)
        let options = JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull) 
        let body = JsonSerializer.Serialize ({| question = message; stream = stream; session_id = session.SessionId |}, options)
        try 
            if stream then
                let res = Http.RequestStream
                            (url, 
                            headers = [ "Authorization", "Bearer " + session.Key; HttpRequestHeaders.ContentType "application/json" ],
                            body = TextRequest body,
                            httpMethod = "Post")
                let mutable buffer = Array.zeroCreate 4096

                Ok (seq {
                    let getData() =
                        let revCnt = res.ResponseStream.Read (buffer, 0, 4096)
                        let resStr = Encoding.UTF8.GetString(buffer, 0, revCnt)
                        resStr.Split "data:"
                            |> Array.filter (fun x -> not (String.IsNullOrWhiteSpace x))
                            |> Array.map ConverseWithChatAssistantResponse.Parse
                            |> Array.map (fun x -> { 
                                                Answer = x.Data.Record 
                                                        |> Option.map (fun x -> x.Answer) 
                                                        |> Option.defaultValue "";
                                                AudioBinary = x.Data.Record 
                                                            |> Option.map (fun x -> x.AudioBinary.JsonValue.AsArray() 
                                                                                    |> Array.map (fun x -> byte (x.AsInteger()) ))
                                                            //|> Option.defaultValue None; 
                                                SessionId = match x.Data.Record with 
                                                            | Some x -> x.SessionId.ToString("N")
                                                            | None -> "";   
                                                Id = x.Data.Record |> Option.map (fun x -> x.Id.ToString()) |> Option.defaultValue "";
                                                //AnswerReference = x.Data.Record |> Option.map (fun x -> x.Reference) |> Option.defaultValue None 
                                                })
                    let rec loop() = seq {
                        let res = getData()
                        if res.Length > 0 then
                            yield! res
                            yield! loop()
                    }

                    yield! loop()
                        
                })
                
            else
            let res = Http.RequestString
                                (url, 
                                headers = [ "Authorization", "Bearer " + session.Key; HttpRequestHeaders.ContentType "application/json" ], 
                                body = TextRequest body, httpMethod = "Post")
                      
            Ok (
            res.Trim().Split("data:")
            |> Array.filter (fun x -> not (String.IsNullOrWhiteSpace x))
            |> Array.map ConverseWithChatAssistantResponse.Parse
            |> Array.map (fun x -> { 
                Answer = x.Data.Record 
                        |> Option.map (fun x -> x.Answer) 
                        |> Option.defaultValue "";
                AudioBinary = x.Data.Record 
                            |> Option.map (fun x -> x.AudioBinary.JsonValue.AsArray() 
                                                    |> Array.map (fun x -> byte (x.AsInteger()) ))
                            //|> Option.defaultValue None; 
                SessionId = match x.Data.Record with 
                            | Some x -> x.SessionId.ToString("N")
                            | None -> "";   
                Id = x.Data.Record |> Option.map (fun x -> x.Id.ToString()) |> Option.defaultValue "";
                //AnswerReference = x.Data.Record |> Option.map (fun x -> x.Reference) |> Option.defaultValue None 
                })
            |> Array.toSeq
            ) 
            // 分隔一下
        with ex -> Error ex.Message


        
