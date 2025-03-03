// For more information see https://aka.ms/fsharp-console-apps
open RagFlow.NET.Fs
open RagFlow.NET.Fs
//let a = 1
let session = RagFlowClient.createClient "ragflow-M1ZjdjMjQ4ZjNlODExZWY4ZmJkMDI0Mm" "http://172.31.192.1"

let res = RagFlowClient.listChatAssistantsByName "dlmaster" session



let Id = 
    match res with
    | Ok x -> x |> Seq.head 
    | Error e -> failwith e

//RagFlowClient.createDefaultChatSession (Id.ToString("N")) session
//|> RagFlowClient.converseWithChatAssistant "索尔迦雷欧是什么宝可梦" true
//|> Result.map Seq.toList
//|> printfn "%A"
//Requests.ListChatAssistants.Defalut
let chat =
    RagFlowClient.createChatSession "uchiha" Id session
    |> Result.bind (RagFlowClient.converseWithChatAssistant "强化学习是什么" false)

match chat with
| Ok x -> x |> Seq.iter (fun x -> printfn "%A" (x.Answer     ))
| Error e -> printfn "%A" e

