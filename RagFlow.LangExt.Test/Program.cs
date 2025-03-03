using LanguageExt;
using static LanguageExt.Prelude;
using static LanguageExt.FSharp;
using static RagFlow.NET.Fs.RagFlowClient;


listChatAssistants("dlmaster",
    createClient("ragflow-M1ZjdjMjQ4ZjNlODExZWY4ZmJkMDI0Mm", "http://localhost"))
    .Apply(fs)
    .Map(s => s[0])
    .Bind(s => createChatSession("test", s).Apply(fs))
    .Bind(s => converseWithChatAssistant("强化学习是什么", false, s).Apply(fs))
    .Match
    (
        Right: s => s.Iter(s => Console.WriteLine(s.Answer)),
        Left: Console.WriteLine
    );

