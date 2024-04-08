using Newtonsoft.Json;
using System.Net.WebSockets;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();

var clientesConectados = new List<WebSocket>();

var usersConectados = new List<User>();

var mensagensPreCriadas = new List<string>
{
	"Bem-vindo ao chat!",
	"O servidor esta pronto para receber suas mensagens."
};

app.UseWebSockets();

app.MapGet("/", () => "Hello World!");

app.Map("/ws", async context =>
{
	if (context.WebSockets.IsWebSocketRequest)
	{
		var webSocket = await context.WebSockets.AcceptWebSocketAsync();
		await Console.Out.WriteLineAsync("Recebemos uma conexão Websocket!");

		clientesConectados.Add(webSocket);

		var newUser = new User
		{
			Connection = webSocket,
			Name = webSocket.GetHashCode().ToString(),
		};

		usersConectados.Add(newUser);

		await NotificarEntrada(newUser);

		await AtualizarListaUsuarios(usersConectados);

		foreach (var mensagem in mensagensPreCriadas)
		{
			Mensagem mensagemResponse = new Mensagem()
			{
				Type = "text",
				Name = "System",
				Data = mensagem
			};

			var jsonResponse = JsonConvert.SerializeObject(mensagemResponse);

			var bufferMensagem = Encoding.UTF8.GetBytes(jsonResponse);

			await webSocket.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
		}

		await ReceberMensagens(webSocket, newUser);
	}
	else
	{
		context.Response.StatusCode = 499;
	}
});

async Task ReceberMensagens(WebSocket webSocket, User user)
{
	var buffer = new byte[10 * 1024 * 1024];

	string mensagem = string.Empty;

	int count = 0;

	int sumResultado = 0;

	int previsto = 0;

	bool trintaFlag = true;

	bool sessentaFlag = true;

	bool noventaNoveFlag = true;

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
			await EnviarMensagem("30", "info", webSocket, user, false);
		}
		else if (sumResultado > (previsto / 1.5) && canprever && sessentaFlag)
		{
			Console.WriteLine("sessenta flag -----\n\n");
			sessentaFlag = false;
			await EnviarMensagem("60", "info", webSocket, user, false);
		}
		else if(sumResultado >= (previsto / 1.1) && canprever && noventaNoveFlag)
		{
			Console.WriteLine("noventaNove flag -----\n\n");

			noventaNoveFlag = false;

			await EnviarMensagem("99", "info", webSocket, user, false);
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
					await EnviarMensagem(_mensagem.Data, "text", webSocket, user, true);
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

						await EnviarMensagem("documents/" + _mensagem.Name, "file", webSocket, user, true);

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
				else if (_mensagem.Type == "rename")
				{
					user.Name = _mensagem.Data;
					await AtualizarListaUsuarios(usersConectados);
				}

				count = 0;
				sumResultado = 0;
				mensagem = string.Empty;
				trintaFlag = true;
				sessentaFlag = true;
				noventaNoveFlag = true;
			}

		}
		else if (resultadoRecebimento.MessageType == WebSocketMessageType.Close)
		{
			await Console.Out.WriteLineAsync("Conexão encerrada.");
			break;
		}
	}

	//var thisUser = usersConectados.Where(u => u.Connection == webSocket).ToList().First();

	usersConectados.Remove(user);
	clientesConectados.Remove(webSocket);
	await NotificarSaida(user);
	await AtualizarListaUsuarios(usersConectados);

}

async Task EnviarMensagem(string mensagem, string tipo, WebSocket webSocket, User user, bool toAll)
{
	Mensagem mensagemResponse = new Mensagem()
	{
		Type = tipo,
		Name = user.Name,
		Data = mensagem,
		Size = mensagem.Length
	};

	var jsonResponse = JsonConvert.SerializeObject(mensagemResponse);

	var bufferMensagem = Encoding.UTF8.GetBytes(jsonResponse);
	if (toAll)
	{
		foreach (var cliente in clientesConectados)
		{
			if (cliente.State == WebSocketState.Open)
			{
				await cliente.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
			}
		}
	}
	else
	{
		await webSocket.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
	}
}

async Task NotificarEntrada(User user)
{
	Mensagem mensagemResponse = new Mensagem()
	{
		Type = "newUser",
		Name = user.Name,
		Data = user.Name,
		Size = user.Name.Length
	};

	var jsonResponse = JsonConvert.SerializeObject(mensagemResponse);

	var bufferMensagem = Encoding.UTF8.GetBytes(jsonResponse);

	foreach (var cliente in clientesConectados)
	{
		if (cliente.State == WebSocketState.Open)
		{
			await cliente.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}
}

async Task NotificarSaida(User user)
{
	Mensagem mensagemResponse = new Mensagem()
	{
		Type = "outUser",
		Name = user.Name,
		Data = user.Name,
		Size = user.Name.Length
	};

	var jsonResponse = JsonConvert.SerializeObject(mensagemResponse);

	var bufferMensagem = Encoding.UTF8.GetBytes(jsonResponse);

	foreach (var cliente in clientesConectados)
	{
		if (cliente.State == WebSocketState.Open)
		{
			await cliente.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}
}

async Task AtualizarListaUsuarios(List<User> usuariosConectados)
{
	List<UserResponse> usersResponse = new();

	foreach (var user in usuariosConectados)
	{
		usersResponse.Add(new UserResponse
		{
			Name = user.Name,
		});
	}

	var ListaAtualizada = new
	{
		Type = "listUsers",
		Data = usersResponse.ToArray()
	};

	var jsonResponse = JsonConvert.SerializeObject(ListaAtualizada);

	var bufferMensagem = Encoding.UTF8.GetBytes(jsonResponse);

	foreach (var cliente in clientesConectados)
	{
		if (cliente.State == WebSocketState.Open)
		{
			await cliente.SendAsync(new ArraySegment<byte>(bufferMensagem), WebSocketMessageType.Text, true, CancellationToken.None);
		}
	}

}

app.Run();

public class Mensagem
{
	public string Type { get; set; }
	public string Name { get; set; }
	public string Data { get; set; }
	public int Size { get; set; }
}

public class User
{
	public WebSocket Connection { get; set; }
	public string Name { get; set; }
}

public class UserResponse
{
	public string Name { get; set; }
}
