// For more information see https://aka.ms/fsharp-console-apps
open RagFlow.NET.Fs
//let a = 1
let session = RagFlowClient.createClient "ragflow-xx" "http://172.31.192.1"

let res = RagFlowClient.listChatAssistantsByName "scixing" session

let chatIds = res |> Result.map (fun x -> x.Data |> Array.map _.Id ) |> Result.defaultValue [||]

let Id = if chatIds.Length > 0 then chatIds.[0] else failwith "No chat found"

RagFlowClient.createChatSession "uchiha" (Id.ToString("N")) session
|> Result.bind (RagFlowClient.converseWithChatAssistant "hello")
|> printfn "%A"

