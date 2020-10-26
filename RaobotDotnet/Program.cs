using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.DependencyInjection;
using System.Net;

namespace RaobotDotnet
{
    class Program
    {
        private readonly IConfiguration _config;
        private DiscordSocketClient _client;
        public static CommandService _commands;
        public static IServiceProvider _services;

        static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public Program()
        {
            // create the configuration
            var _builder = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile(path:"config.json");

            _config = _builder.Build();
            
        }

        public async Task MainAsync()
        {
            _client = new DiscordSocketClient(new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Info
            });
            _client.Log += Log;
            _commands = new CommandService();
            _services = new ServiceCollection().BuildServiceProvider();
            // 봇이 메시지 수신 때 처리하도록
            _client.MessageReceived += CommandRecieved;

            await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
            await _client.LoginAsync(TokenType.Bot, _config["Discord_Token"]);
            await _client.StartAsync();

            // 봇 종료 방지를 위한 블로킹
            await Task.Delay(-1);
        }

        /// <summary>
        /// </summary>
        /// <param name="msgParam"></param>
        /// <returns></returns>
        private async Task CommandRecieved(SocketMessage messageParam)
        {
            var message = messageParam as SocketUserMessage;

            //debug 용
            Console.WriteLine("{0} {1}:{2}", message.Channel.Name, message.Author.Username, message);
            if (message == null)
                return;

            if (message.Author.IsBot)
                return;

            int pos = 0;

            var context = new SocketCommandContext(_client, message);

            var result = await _commands.ExecuteAsync(context: context,argPos: pos, services: null); 

            //--------------------------------------------------------------------------
            var CommandContext = message.Content;
            var author = message.Author.Mention;
            if (message.Channel.Name.ToString()[0] == '_')
            {
                string source = detectLangs(message.Content);
                Console.WriteLine(source);
                if(source == "unk" || source == "py")
                {
                    return;
                }
                string target = (source == "ko") ? "ja" : "ko";
                Console.WriteLine("source : {0} , target : {1}, message : {2}", source, target, CommandContext);
                await context.Channel.SendMessageAsync(author + " "+tranResult(source, target, message.Content)); ;
            }
            if (CommandContext == "testest")
            {
                await message.Channel.SendMessageAsync("asdfasafd!");
            }

        }

        private string detectLangs(string source)
        {
            try
            {
                string sUrl = "https://openapi.naver.com/v1/papago/detectLangs";
                string sParam = "query=" + source;
                byte[] byteArry = Encoding.UTF8.GetBytes(sParam);

                WebRequest webRequest = WebRequest.Create(sUrl);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";

                //header 
                webRequest.Headers.Add("X-Naver-Client-Id", _config["Naver_Id"]);
                webRequest.Headers.Add("X-Naver-Client-Secret", _config["Naver_Secret"]);

                webRequest.ContentLength = byteArry.Length;

                Stream stream = webRequest.GetRequestStream();
                stream.Write(byteArry, 0, byteArry.Length);
                stream.Close();

                WebResponse webResponse = webRequest.GetResponse();
                stream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(stream);
                string sRetrun = streamReader.ReadToEnd();

                streamReader.Close();
                stream.Close();
                webResponse.Close();

                JObject jObject = JObject.Parse(sRetrun);
                return jObject["langCode"].ToString();
            }catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                return "unk"; 
            }
        }

        private string tranResult(string source, string target, string text)
        {
            try
            {
                string sUrl = "https://openapi.naver.com/v1/papago/n2mt";

                string sParam = string.Format("source={0}&target={1}&text={2}", source, target, text);
                Console.WriteLine(sParam);

                byte[] byteArry = Encoding.UTF8.GetBytes(sParam);

                WebRequest webRequest = WebRequest.Create(sUrl);
                webRequest.Method = "POST";
                webRequest.ContentType = "application/x-www-form-urlencoded";

                //header 
                webRequest.Headers.Add("X-Naver-Client-Id", _config["Naver_Id"]);
                webRequest.Headers.Add("X-Naver-Client-Secret", _config["Naver_Secret"]);

                webRequest.ContentLength = byteArry.Length;

                Stream stream = webRequest.GetRequestStream();
                stream.Write(byteArry, 0, byteArry.Length);
                stream.Close();

                WebResponse webResponse = webRequest.GetResponse();
                stream = webResponse.GetResponseStream();
                StreamReader streamReader = new StreamReader(stream);
                string sRetrun = streamReader.ReadToEnd();

                streamReader.Close();
                stream.Close();
                webResponse.Close();

                JObject jObject = JObject.Parse(sRetrun);
                return jObject["message"]["result"]["translatedText"].ToString();
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return "Error";
            }
        }

        private Task Log(LogMessage message)
        {
            Console.WriteLine(message.ToString());
            return Task.CompletedTask;
        }
    }

    public class ttsModule : ModuleBase<SocketCommandContext>
    {
        //tts 모드용 모듈이다
        

        //tts 명령으로 해당 음성 채널에 tts on 설정
        [Command("!tts", RunMode = RunMode.Async)]
        public async Task TestCommand()
        {
            // 사용자가 음성 채널에 들어가 있지 않는 경우
            if(Context.Guild.CurrentUser.VoiceChannel == null)
            {
                await Context.Channel.SendMessageAsync("VCに入ってくれ");
                return;
            }
            Console.WriteLine("VC" + Context.Guild.Channels.ToString());
            
        }
    }

}
