using SemanticKernelServer.Models;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.AI.ChatCompletion;
using Microsoft.SemanticKernel.AI.TextCompletion;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI.ChatCompletion;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using System.Text;
using System.Text.Json;

namespace SemanticKernelServer.Controllers
{
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly ILogger<ChatController> _logger;
        private readonly IKernel _kernel;
        private ISKFunction _simpleBeginChatFunction;
        private ISKFunction _simpleChatFunction;

        public ChatController(ILogger<ChatController> logger)
        {
            _logger = logger;
            _kernel = Kernel.Builder.Build();
            /*_kernel.Config.AddAzureOpenAITextCompletionService(
                "",                     // Alias used by the kernel
                "",                  // Azure OpenAI Deployment Name
                "", // Azure OpenAI Endpoint
                ""        // Azure OpenAI Key
            );//*/
            _kernel.Config.AddAzureChatCompletionService(
              "chatgpt-azure",                     // Alias used by the kernel
              "gpt-35-turbo",                  // Azure OpenAI Deployment Name
              "", // Azure OpenAI Endpoint
              ""        // Azure OpenAI Key
          );

            RegisterSimpleChat();
            RegisterSimpleBeginChat();
        }

        [HttpPost]
        [Route("api/chat")]
        public async Task<IActionResult> Chat([FromBody] ChatBody chatBody)
        {
            return Ok((await SimpleChat(chatBody)).Result.Trim());
            /*string json = JsonSerializer.Serialize(chatBody, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            await HttpContext.Response.WriteAsync("```");
            foreach (string line in json.Split("\r\n"))
            {
                await HttpContext.Response.WriteAsync(line+"\r\n");
            }
            await HttpContext.Response.WriteAsync("```");
            return new EmptyResult();//*/
        }

        private void RegisterSimpleBeginChat()
        {
            string simpleChatFlex = @"{{$INPUT}}";

            var myPromptConfig = new PromptTemplateConfig
            {
                Description = "Take an input and summarize it super-succinctly.",
                Completion =
                {
                    MaxTokens = 4000,
                    Temperature = 0.7,
                    TopP = 0.95,
                }
            };
            var myPromptTemplate = new PromptTemplate(simpleChatFlex, myPromptConfig, _kernel);
            var myFunctionConfig = new SemanticFunctionConfig(myPromptConfig, myPromptTemplate);
            _simpleBeginChatFunction = _kernel.RegisterSemanticFunction(
                "SimpleBeginChatSkill",
                "summarizeBlurbFlex",
                myFunctionConfig);
        }

        private void RegisterSimpleChat() 
        {
            string simpleChatFlex = @"
                You are in the conversation with User, continue talking with User by replying User's latest input based on the conversation log below
                ---Begin Conversation Log---
                {{$HISTORY}}
            ";

            var myPromptConfig = new PromptTemplateConfig
            {
                Description = "Take an input and summarize it super-succinctly.",
                Completion =
                {
                    MaxTokens = 4000,
                    Temperature = 0.7,
                    TopP = 0.95,
                }
            };
            var myPromptTemplate = new PromptTemplate(simpleChatFlex, myPromptConfig, _kernel);
            var myFunctionConfig = new SemanticFunctionConfig(myPromptConfig, myPromptTemplate);
            _simpleChatFunction = _kernel.RegisterSemanticFunction(
                "SimpleChatSkill",
                "summarizeBlurbFlex",
                myFunctionConfig);
        }

        private async Task<SKContext> SimpleChat(ChatBody chatBody)
        {
            var myContext = new ContextVariables();
            var historyMessages = chatBody.Messages;
            string history = "";
            SKContext output = null;
            if (historyMessages.Length > 1)
            {
                history = string.Join("\r\n", historyMessages.Select(m => $"{(m.Role == "assistant" ? "" : "User said: ")} {m.Content}"));
                myContext.Set("HISTORY", history);
                output = await _kernel.RunAsync(myContext, _simpleChatFunction);
            }
            else
            {
                myContext.Set("INPUT", historyMessages[0].Content);
                output = await _kernel.RunAsync(myContext, _simpleBeginChatFunction);
            }
            
            return output;
        }
    }
}