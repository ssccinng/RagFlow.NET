net start winnat
net stop winnat

host.docker.internal:11434

你好！ 我是你的成也大木，想知道什么宝可梦相关的知识吗

大木博士
ollama pull bge-m3
ollama pull deepseek-r1:7b

git clone https://github.com/infiniflow/ragflow

{
  "builder": {
    "gc": {
      "defaultKeepStorage": "20GB",
      "enabled": true
    }
  },
  "experimental": false,
  "registry-mirrors": [
    "https://docker-0.unsee.tech",
    "https://hub-mirror.c.163.com",
    "https://mirror.baidubce.com",
    "https://do.nark.eu.org",
    "https://dc.j8.work",
    "https://docker.m.daocloud.io",
    "https://dockerproxy.com",
    "https://docker.mirrors.ustc.edu.cn",
    "https://docker.nju.edu.cn",
    "https://docker.1panel.live",
    "https://docker.1panelproxy.com"
  ]
}


from ragflow_sdk import RAGFlow
import requests


rag_object = RAGFlow(api_key="ragflow-M1ZjdjMjQ4ZjNlODExZWY4ZmJkMDI0Mm", base_url="http://172.31.192.1")
assistant = rag_object.list_chats(name="scixing")
assistant = assistant[0]
session = assistant.create_session()



print("\n==================== scixing =====================\n")
print("Hello. What can I do for you?")

while True:
    question = input("\n==================== User =====================\n> ")
    print("\n==================== scixing =====================\n")
    
    cont = ""
    for ans in session.ask(question, stream=True):
        print(ans.content[len(cont):], end='', flush=True)
        cont = ans.content
