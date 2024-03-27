using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

app.UseWebSockets();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async context =>
{
	if (context.WebSockets.IsWebSocketRequest)
	{
		var webSocket = await context.WebSockets.AcceptWebSocketAsync();
		await Console.Out.WriteLineAsync("Recebemos uma conexão Websocket!");

		await ReceberMensagens(webSocket);
	}
	else
	{
		context.Response.StatusCode = 499;
	}
});

async Task ReceberMensagens(WebSocket webSocket)
{
	var buffer = new byte[10 * 1024 * 1024];

	while (webSocket.State == WebSocketState.Open)
	{
		Array.Clear(buffer, 0, buffer.Length);

		var mensagemRecebida = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

		if (mensagemRecebida.MessageType == WebSocketMessageType.Text)
		{
			var mensagem = Encoding.UTF8.GetString(buffer, 0, mensagemRecebida.Count);

			await Console.Out.WriteLineAsync("Mensagem recebida: " + mensagem);

			await EnviarMensagem(mensagem, webSocket);

		}
		else if (mensagemRecebida.MessageType == WebSocketMessageType.Close)
		{
			await Console.Out.WriteLineAsync("Conexão encerrada.");
			break;
		}
	}
}

async Task EnviarMensagem(string mensagem, WebSocket webSocket)
{
	var bufferMensagem = Encoding.UTF8.GetBytes(mensagem);

	await webSocket.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
}

app.Run();

//var jsonMensagem = JsonConvert.SerializeObject(mensagem);