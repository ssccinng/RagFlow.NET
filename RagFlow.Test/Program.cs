
using static RagFlow.NET.Fs.RagFlowClient;

var client = createClient("ragflow-M1ZjdjMjQ4ZjNlODExZWY4ZmJkMDI0Mm", "http://localhost");
var assistantss = listChatAssistants("dlmaster", client);
if (assistantss.IsOk)
{
    var assistants = assistantss.ResultValue;
    foreach (var ass in assistants)
    {
        System.Console.WriteLine(ass.Name);
    }
    var assistant = assistants[0];

    var chats = listChatAssistantSessions(assistant);

    foreach (var ch in chats.ResultValue)
    {
        Console.WriteLine(ch?.Name);
        updateChatSession(ch?.Name + 1, ch);
    }

    var chatSession = createChatSession("testLib", assistant);
    var res = converseWithChatAssistant("强化学习是什么", false, chatSession.ResultValue);
    if (res.IsOk)
    {
        foreach (var chat in res.ResultValue)
        {
            Console.WriteLine(chat.Answer);
        }
    }

}