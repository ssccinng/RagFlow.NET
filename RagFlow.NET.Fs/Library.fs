namespace RagFlow.NET.Fs
open FSharp.Data
open System
open System.Text.Json
type CreateDbResponse = JsonProvider<Sample="ApiJson/CreateDb.json", SampleIsList=true>
type ListChatAssistantsResponse = JsonProvider<Sample="ApiJson/ListChatAssistants.json", SampleIsList=true>
type CreateSessionResponse = JsonProvider<Sample="ApiJson/CreateSession.json", SampleIsList=true>


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


module RagFlowClient =

    let createClient (key: string) (baseUrl: string) = { Key = key; BaseUrl = baseUrl }

    let listChatAssistants (page: int) (pageSize: int) (orderBy: string) (desc: bool) (name: string) (id: string) (session: Client) =
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

    let listChatAssistantsByName (name: string) (session: Client) =
        listChatAssistants 1 30 "create_time" true name "" session


    let createChatSession  (name: string) (chatId: string) (session: Client)=
        let route = $"{session.BaseUrl}{Urls.createSession}"
        let url = String.Format (route, chatId)
        let body = $"{{\"name\": \"{name}\" }}"
        
        try 
        
            let res = 
                    Http.RequestString
                        (url, headers = [ "Authorization", "Bearer " + session.Key ; HttpRequestHeaders.ContentType "application/json"], body = TextRequest(body), httpMethod = "Post") 
                    |> CreateSessionResponse.Parse
            if res.Code = 0 then Ok { Key = session.Key; ChatId = chatId; SessionId = Some (res.Data.Value.ChatId.ToString("N")); BaseUrl = session.BaseUrl }
            else Error (res.Message |> Option.defaultValue "Error")
        with ex -> Error ex.Message

    let createDefaultChatSession  (chatId: string) (session: Client)  = { Key = session.Key; ChatId = chatId; SessionId = None; BaseUrl = session.BaseUrl}


    let converseWithChatAssistant (message: string) (session: ChatSession) =
        let route = $"{session.BaseUrl}{Urls.ConverseWithChatAssistant}"
        let url = String.Format (route, session.ChatId)
        let body = {| question = message; stream = false; session = session.SessionId |} |> JsonSerializer.Serialize
        try 
            let res = Http.RequestString
                                (url, 
                                headers = [ "Authorization", "Bearer " + session.Key; HttpRequestHeaders.ContentType "application/json" ], 
                                body = TextRequest body, httpMethod = "Post")
                      
            Ok (res.Split("data:")) // 分隔一下
        with ex -> Error ex.Message


        
