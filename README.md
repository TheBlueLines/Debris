# Debris

Network protocol for TTMC applications

## Installation

Download the repo and build the solution with:

```bash
dotnet build
```
Add the builded dll file to your project
```bash
Solution Explorer > Dependencies > Add Project Reference > Browse > OK
```

## Example

### Client

```csharp
using Debris;
using System.Net.Sockets;

namespace ClientExample
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Client client = new("127.0.0.1", 54321);
			client.handle = new ClientHandle();
			Console.WriteLine("Client started!");

			Packet packet = new Packet()
			{
				id = 1,
				data = Engine.PrefixedString("Szia Világ!")
			};
			client.SendPacket(packet);
			Console.WriteLine("Packet sent!");
			Thread.Sleep(1000);
		}
	}
	public class ClientHandle : Handle
	{
		public override Packet? Message(Packet packet, NetworkStream stream)
		{
			Console.WriteLine("Client got: " + packet.id);
			return null;
		}
	}
}
```

### Server

```csharp
using Debris;
using System.Net.Sockets;

namespace ServerExample
{
	internal class Program
	{
		static void Main(string[] args)
		{
			Server server = new(54321);
			server.handle = new ServerHandle();
			Console.WriteLine("Server started!");
		}
	}
	public class ServerHandle : Handle
	{
		public override Packet? Message(Packet packet, NetworkStream stream)
		{
			Console.WriteLine("Server got: " + packet.id);
			return packet;
		}
	}
}
```

## License

[MIT](https://choosealicense.com/licenses/mit/)