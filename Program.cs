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
		await Console.Out.WriteLineAsync("Recebemos uma conex�o Websocket!");

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

	int count = 0;

	int sumResultado = 0;

	int previsto = 0;

	bool trintaFlag = true;

	bool sessentaflag = true;

	bool canprever = false;

	while (webSocket.State == WebSocketState.Open)
	{
		Array.Clear(buffer, 0, buffer.Length);

		var resultadoRecebimento = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
		count++;
		sumResultado += resultadoRecebimento.Count;
		Console.WriteLine("Recebi " + count + " - " + resultadoRecebimento.Count + "    :::    " + sumResultado);

		if (sumResultado > previsto / 3 && canprever && trintaFlag)
		{
			Console.WriteLine("trinta flag -----\n\n");

			trintaFlag = false;
			await EnviarMensagem("30", "info", webSocket);
		}
		else if (sumResultado > (previsto / 1.5) && canprever && sessentaflag)
		{
			Console.WriteLine("sessenta flag -----\n\n");
			sessentaflag = false;
			await EnviarMensagem("60", "info", webSocket);
		}


		if (resultadoRecebimento.MessageType == WebSocketMessageType.Text)
		{
			var mensagemRecebida = Encoding.UTF8.GetString(buffer, 0, resultadoRecebimento.Count);

			mensagem += mensagemRecebida;

			Console.WriteLine(mensagem.Length);

			if (resultadoRecebimento.EndOfMessage)
			{

				Mensagem _mensagem = JsonConvert.DeserializeObject<Mensagem>(mensagem)!;

				if (_mensagem.Type == "text")
				{
					await Console.Out.WriteLineAsync(_mensagem.Name + ": " + _mensagem.Data);
					await EnviarMensagem(_mensagem.Data, "text", webSocket);
					canprever = false;

				}
				else if (_mensagem.Type == "file")
				{
					//string diretorioDestino = "C:\\Projetos\\WebSocket\\Cliente\\documents";
					string diretorioDestino = "C:\\xampp\\htdocs\\documents";

					string caminhoCompleto = Path.Combine(diretorioDestino, _mensagem.Name);

					try
					{
						byte[] arquivoBytes = Convert.FromBase64String(_mensagem.Data);

						File.WriteAllBytes(caminhoCompleto, arquivoBytes);

						await Console.Out.WriteLineAsync("Arquivo salvo com sucesso em: " + caminhoCompleto);

						await EnviarMensagem("documents/" + _mensagem.Name, "file", webSocket);

					}
					catch (Exception ex)
					{
						await Console.Out.WriteLineAsync("Erro ao salvar o arquivo:" + ex.Message);
					}
					canprever = false;

				}
				else if (_mensagem.Type == "info")
				{

					try
					{
						previsto = (int)((int.Parse(_mensagem.Data)) * 1.34);

						canprever = true;
					}
					catch
					{
                        await Console.Out.WriteLineAsync("\n\nMensagem Data: " + mensagem);
                    }
				}
				count = 0;
				sumResultado = 0;
				mensagem = string.Empty;
				trintaFlag = true;
				sessentaflag = true;
			}

		}
		else if (resultadoRecebimento.MessageType == WebSocketMessageType.Close)
		{
			await Console.Out.WriteLineAsync("Conex�o encerrada.");
			break;
		}
	}
}

async Task EnviarMensagem(string mensagem, string tipo, WebSocket webSocket)
{
	Mensagem mensagemResponse = new Mensagem()
	{
		Type = tipo,
		Name = "Voc�",
		Data = mensagem,
		Size = mensagem.Length
	};

	var jsonResponse = JsonConvert.SerializeObject(mensagemResponse);

	var bufferMensagem = Encoding.UTF8.GetBytes(jsonResponse);

	await webSocket.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
}

app.Run();

public class Mensagem
{
	public string Type { get; set; }
	public string Name { get; set; }
	public string Data { get; set; }
	public int Size { get; set; }
}
