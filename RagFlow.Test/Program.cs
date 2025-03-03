
using static RagFlow.NET.Fs.RagFlowClient;

namespace RagFlow.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var client = createClient("ragflow-M1ZjdjMjQ4ZjNlODExZWY4ZmJkMDI0Mm", "http://localhost");
            var aa = listChatAssistantsByName.Invoke("dlmaster").Invoke(client);
            if (aa.IsOk)
            {
                var assistants = aa.ResultValue;
                foreach (var ass in assistants)
                {
                    System.Console.WriteLine(ass.Name);
                }
                var assistant = assistants[0];

                var chatSession = createChatSession("testLib", assistant, client);
                var res = converseWithChatAssistant("强化学习是什么", false, chatSession.ResultValue);
                if (res.IsOk)
                {
                    foreach (var chat in res.ResultValue)
                    {
                        Console.WriteLine(chat.Answer);
                        
                    }
                }

            }

        }
    }
}
