using System;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        { 
            StartClient();
        }

        static void StartClient()
        {
            TcpClient client = new TcpClient("127.0.0.1", 9999);
            NetworkStream stream = client.GetStream();

            byte[] buffer = new byte[1024];
            int bytesRead = stream.Read(buffer, 0, buffer.Length);
            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            Console.WriteLine(message);

            if (message.Contains("Waiting for Player 2"))
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine(message);
            }

            while (true)
            {
                bytesRead = stream.Read(buffer, 0, buffer.Length);
                string boardState = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine("Current board state:");
                Console.WriteLine(boardState);

                bytesRead = stream.Read(buffer, 0, buffer.Length);
                string serverResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                Console.WriteLine(serverResponse);

                if (serverResponse.Contains("wins") || serverResponse.Contains("draw"))
                {
                    break;
                }

                if (serverResponse.Contains("Your move"))
                {
                    while (true)
                    {
                        Console.WriteLine("Enter your move (1-9): ");
                        string move = Console.ReadLine();
                        byte[] moveData = Encoding.ASCII.GetBytes(move);
                        stream.Write(moveData, 0, moveData.Length);

                        bytesRead = stream.Read(buffer, 0, buffer.Length);
                        serverResponse = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        Console.WriteLine(serverResponse);

                        if (!serverResponse.Contains("Invalid move"))
                        {
                            break;
                        }
                    }
                }
            }

            client.Close();
        }
    }
}
