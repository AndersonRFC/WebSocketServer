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

	string mensagem = string.Empty;

	while (webSocket.State == WebSocketState.Open)
	{
		Array.Clear(buffer, 0, buffer.Length);

		var resultadoRecebimento = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

		if (resultadoRecebimento.MessageType == WebSocketMessageType.Text)
		{
			var mensagemRecebida = Encoding.UTF8.GetString(buffer, 0, resultadoRecebimento.Count);

			mensagem += mensagemRecebida;


			if (resultadoRecebimento.EndOfMessage)
			{

				Mensagem _mensagem = JsonConvert.DeserializeObject<Mensagem>(mensagem)!;

				if (_mensagem.Type == "text")
				{
					await Console.Out.WriteLineAsync(_mensagem.Name+": " + _mensagem.Data);
					await EnviarMensagem(_mensagem.Data, webSocket);
				}
				else if (_mensagem.Type == "file")
				{
					string diretorioDestino = "C:\\Projetos\\WebSocket\\Cliente\\documents";

					string caminhoCompleto = Path.Combine(diretorioDestino, _mensagem.Name);

					try
					{
						byte[] arquivoBytes = Convert.FromBase64String(_mensagem.Data);

						File.WriteAllBytes(caminhoCompleto, arquivoBytes);

						await Console.Out.WriteLineAsync("Arquivo salvo com sucesso em: " + caminhoCompleto);
                    }
					catch (Exception ex)
					{
                        await Console.Out.WriteLineAsync("Erro ao salvar o arquivo:" + ex.Message);
                    }
				}
				mensagem = string.Empty;
			}

		}
		else if (resultadoRecebimento.MessageType == WebSocketMessageType.Close)
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

public class Mensagem
{
	public string Type { get; set; }
	public string Name { get; set; }
	public string Data { get; set; }
}
