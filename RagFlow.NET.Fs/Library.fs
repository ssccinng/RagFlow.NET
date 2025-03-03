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
type ListChatAssistantSessionResponse = JsonProvider<Sample="./ApiJson/ListChatAssistantSession.json", SampleIsList=true>
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
    let updateSession = "%s/api/v1/chats/%s/sessions/%s"
    [<Literal>]
    let deleteSession = "/api/v1/chats/%s/sessions"
    //let listA = "/api/v1/chats/{chat_id}/sessions"
    let ConverseWithChatAssistant = "/api/v1/chats/{0}/completions"
    [<Literal>]
    let listChatAssistantSessions = "%s/api/v1/chats/%s/sessions"




type Client = {
    Key: string
    //Id: string
    BaseUrl: string
}



type ChatAssistant = {
    Client: Client
    Id: string
    Name: string
    CreateTime: DateTime
    UpdateTime: DateTime
}


type ChatSession = {
    ChatAssistant: ChatAssistant
    SessionId: string
    Name: string
    // 加个名字?
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
        DatasetId: string
    }
    
    with static member Defalut = { Page = 1; PageSize = 30; OrderBy = "create_time"; Desc = true; Name = ""; DatasetId = "" }

    type ListChatAssistantSession = {
        Page: int
        PageSize: int
        OrderBy: string
        Desc: bool
        Name: string
        SessionId: string
    }
    with static member Defalut = { Page = 1; PageSize = 30; OrderBy = "create_time"; Desc = true; Name = ""; SessionId = "" }



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
    let listChatAssistantsAdv (data: Requests.ListChatAssistants) (client: Client) =
        let url = $"{client.BaseUrl}{Urls.listChatAssistants}"
        // let url = String.Format (route, data.Page, data.PageSize, data.OrderBy, data.Desc, data.Name, data.Id)

        let query = [ "page", data.Page.ToString(); "page_size", data.PageSize.ToString(); "orderby", data.OrderBy; "desc", data.Desc.ToString(); "name", data.Name; "id", data.DatasetId ] 
                    |> List.filter (fun (k, v) -> not( String.IsNullOrWhiteSpace v) )
                    //|> List.map (fun (k, v) -> $"{k}={v}")
                    //|> String.concat "&"
        try 
            let res = Http.RequestString(url, headers = [ "Authorization", "Bearer " + client.Key ], query = query , httpMethod = "Get") |> ListChatAssistantsResponse.Parse
            if res.Code = 0 then Ok (res.Data |> Array.map (fun x -> 
                    { Client = client ; 
                    Id = x.Id.ToString "N"; 
                    Name = x.Name; 
                    CreateTime = DateTimeOffset.FromUnixTimeMilliseconds(x.CreateTime).DateTime; 
                    UpdateTime = DateTimeOffset.FromUnixTimeMilliseconds(x.UpdateTime).DateTime }))
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message


    /// <summary>
    /// 列出所有的chat assistant
    /// </summary>
    let listChatAssistants name client = listChatAssistantsAdv { Requests.ListChatAssistants.Defalut with Name = name } client

    /// <summary>
    /// 创建一个chat session
    /// </summary>
    let createChatSession (name: string) (assistant: ChatAssistant) =
        let route = $"{assistant.Client.BaseUrl}{Urls.createSession}"
        let url = String.Format (route, assistant.Id)
        let body = $"{{\"name\": \"{name}\" }}"
        
        try 
        
            let res = 
                    Http.RequestString
                        (url, headers = [ "Authorization", "Bearer " + assistant.Client.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest body, httpMethod = "Post") 
                    |> CreateSessionResponse.Parse
            if res.Code = 0 then Ok {  ChatAssistant = assistant;
                                        SessionId = res.Data.Value.Id.ToString "N"; 
                                         Name = name }
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message

    /// <summary>
    /// 创建一个默认的chat session
    /// </summary>
    //let createDefaultChatSession (assistant: ChatAssistant)
    //    = { ChatAssistant = assistant; SessionId = None; Name = "" }


    /// <summary>
    /// 列出所有的chat session
    /// </summary>
    let listChatAssistantSessionsAdv (data: Requests.ListChatAssistantSession) (assistant: ChatAssistant) =
        let url = sprintf Urls.listChatAssistantSessions assistant.Client.BaseUrl assistant.Id
        let query = [ "page", data.Page.ToString(); 
                "page_size", data.PageSize.ToString(); 
                "orderby", data.OrderBy; 
                "desc", data.Desc.ToString(); 
                "name", data.Name; 
                "id", data.SessionId ] 
                    |> List.filter (fun (k, v) -> not( String.IsNullOrWhiteSpace v) )
        try 
            let res = Http.RequestString(url, 
                        headers = [ "Authorization", "Bearer " + assistant.Client.Key ], 
                        query = query,
                        httpMethod = "Get") 
                        |> ListChatAssistantSessionResponse.Parse
            if res.Code = 0 
            then 
                Ok (res.Data |> Array.map (fun x -> { ChatAssistant = assistant; SessionId = x.Id.ToString "N"; Name = x.Name }))
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message

    let listChatAssistantSessions assistant = listChatAssistantSessionsAdv Requests.ListChatAssistantSession.Defalut assistant
        

    let updateChatSession name (session: ChatSession) =
        let url = sprintf Urls.updateSession session.ChatAssistant.Client.BaseUrl session.ChatAssistant.Id session.SessionId
        let body = $"{{\"name\": \"{name}\" }}"
        try 
            let res = 
                    Http.RequestString
                        (url, headers = [ "Authorization", "Bearer " + session.ChatAssistant.Client.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest body, httpMethod = "Put") 
                    |> UpdateSessionResponse.Parse
            if res.Code = 0 then Ok { session with Name = name }
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message

    let deleteChatSession (sessions: ChatSession array) = 
        if Array.isEmpty sessions then Ok ()
        else
            let session = sessions.[0]
            let url = sprintf Urls.deleteSession session.SessionId
            try 
                let body = JsonSerializer.Serialize {|ids = sessions |> Array.map (fun x -> x.SessionId) |}
                let res = 
                        Http.RequestString
                            (url, headers = [ "Authorization", "Bearer " + session.ChatAssistant.Client.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest body, httpMethod = "Delete")
                        |> DeleteSessionResponse.Parse
                if res.Code = 0 then Ok ()
                else Error (res.Message |> Option.defaultValue "Error")
            with ex -> Error ex.Message



    let converseWithChatAssistant (message: string) (stream: bool) (session: ChatSession) =
        let route = $"{session.ChatAssistant.Client.BaseUrl}{Urls.ConverseWithChatAssistant}"
        let url = String.Format (route, session.ChatAssistant.Id)
        let options = JsonSerializerOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull) 
        let body = JsonSerializer.Serialize ({| question = message; stream = stream; session_id = session.SessionId |}, options)
        try 
            if stream then
                let res = Http.RequestStream
                            (url, 
                            headers = [ "Authorization", "Bearer " + session.ChatAssistant.Client.Key; HttpRequestHeaders.ContentType "application/json" ],
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
                                headers = [ "Authorization", "Bearer " + session.ChatAssistant.Client.Key; HttpRequestHeaders.ContentType "application/json" ], 
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


        
