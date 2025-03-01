// For more information see https://aka.ms/fsharp-console-apps
open RagFlow.NET.Fs
//let a = 1
let session = RagFlowClient.createClient "ragflow-M1ZjdjMjQ4ZjNlODExZWY4ZmJkMDI0Mm" "http://172.31.192.1"

let res = RagFlowClient.listChatAssistantsByName "ds" session

let chatIds = res |> Result.map (fun x -> x.Data |> Array.map _.Id ) |> Result.defaultValue [||]

let Id = if chatIds.Length > 0 then chatIds.[0] else failwith "No chat found"
//RagFlowClient.createDefaultChatSession (Id.ToString("N")) session
//|> RagFlowClient.converseWithChatAssistant "索尔迦雷欧是什么宝可梦" true
//|> Result.map Seq.toList
//|> printfn "%A"
//Requests.ListChatAssistants.Defalut

let chat =
    RagFlowClient.createChatSession "uchiha" (Id.ToString("N")) session
    |> Result.bind (RagFlowClient.converseWithChatAssistant "太乐巴戈斯" true)

match chat with
| Ok x -> x |> Seq.iter (fun x -> printfn "%A" (x.Data.Record |> Option.map _.Answer ))
| Error e -> printfn "%A" e

