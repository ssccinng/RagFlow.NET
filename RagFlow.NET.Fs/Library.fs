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


module Urls = 
    let createDb = "/api/v1/datasets"
    let listChatAssistants = "/api/v1/chats"
    let createSession = "/api/v1/chats/{0}/sessions"
    //let listA = "/api/v1/chats/{chat_id}/sessions"
    let ConverseWithChatAssistant = "/api/v1/chats/{0}/completions"


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
            if res.Code = 0 then Ok res
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message


    // 
    let listChatAssistantsByName = listChatAssistants 1 30 "create_time" true ""


    let createChatSession (name: string) (chatId: string) (session: Client)=
        let route = $"{session.BaseUrl}{Urls.createSession}"
        let url = String.Format (route, chatId)
        let body = $"{{\"name\": \"{name}\" }}"
        
        try 
        
            let res = 
                    Http.RequestString
                        (url, headers = [ "Authorization", "Bearer " + session.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest(body), httpMethod = "Post") 
                    |> CreateSessionResponse.Parse
            if res.Code = 0 then Ok { Key = session.Key; ChatId = chatId; SessionId = Some (res.Data.Value.Id.ToString("N")); BaseUrl = session.BaseUrl }
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message

    let createDefaultChatSession  (chatId: string) (session: Client)
        = { Key = session.Key; ChatId = chatId; SessionId = None; BaseUrl = session.BaseUrl}


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
                        resStr.Split("data:")
                            |> Array.filter (fun x -> not (String.IsNullOrWhiteSpace x))
                            |> Array.map ConverseWithChatAssistantResponse.Parse
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
            |> Array.toSeq
            ) 
            // 分隔一下
        with ex -> Error ex.Message


        
