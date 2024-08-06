using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TicTacToeServer
{
    class Program
    {
        private static List<TcpClient> clients = new List<TcpClient>();
        private static char[,] board = new char[3, 3] { { '1', '2', '3' }, { '4', '5', '6' }, { '7', '8', '9' } };
        private static int currentPlayer = 1;
        private static bool gameEnded = false;

        static async Task Main(string[] args)
        {
            await StartServerAsync();
        }

        static async Task StartServerAsync()
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 9999);
            listener.Start();
            Console.WriteLine("Server started. Waiting for connections...");

            try
            {
                while (true)
                {
                    TcpClient client = await listener.AcceptTcpClientAsync();
                    clients.Add(client);
                    Console.WriteLine("Client connected.");

                    if (clients.Count == 1)
                    {
                        await SendMessageAsync(client, "Waiting for Player 2...");
                    }

                    if (clients.Count == 2)
                    {
                        BroadcastMessage("Both players connected. Game starting...");
                        Task.Run(() => HandleClientAsync(clients[0], 1));
                        Task.Run(() => HandleClientAsync(clients[1], 2));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
            }
        }

        static async Task HandleClientAsync(TcpClient client, int playerNumber)
        {
            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            SendBoardState(stream);

            try
            {
                while (!gameEnded)
                {
                    if (currentPlayer == playerNumber)
                    {
                        await SendMessageAsync(client, "Your move.");

                        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        string moveString = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

                        if (IsValidMove(moveString, out int row, out int col))
                        {
                            char mark = (playerNumber == 1) ? 'X' : 'O';
                            board[row, col] = mark;
                            BroadcastBoardState();

                            if (CheckForWinner())
                            {
                                BroadcastMessage($"Player {playerNumber} wins!");
                                await NotifyGameEndAsync($"Player {playerNumber} wins!");
                                gameEnded = true;
                                break;
                            }
                            else if (IsBoardFull())
                            {
                                BroadcastMessage("It's a draw!");
                                await NotifyGameEndAsync("It's a draw!");
                                gameEnded = true;
                                break;
                            }

                            currentPlayer = (currentPlayer == 1) ? 2 : 1;
                            BroadcastMessage($"Wait for Player {currentPlayer}'s move.");
                        }
                        else
                        {
                            await SendMessageAsync(client, "Invalid move. Try again.");
                        }
                    }

                    await Task.Delay(100);  // Add a small delay to prevent busy-waiting
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred with a client: " + ex.Message);
            }
            finally
            {
                clients.Remove(client);
                client.Close();
                Console.WriteLine("Client disconnected.");
            }
        }

        public static void SendBoardState(NetworkStream stream)
        {
            byte[] boardData = Encoding.ASCII.GetBytes(GetBoardString());
            stream.Write(boardData, 0, boardData.Length);
            Console.WriteLine("Current board state:");
            Console.WriteLine(GetBoardString());
        }

        public static async Task SendMessageAsync(TcpClient client, string message)
        {
            byte[] messageData = Encoding.ASCII.GetBytes(message);
            await client.GetStream().WriteAsync(messageData, 0, messageData.Length);
        }

        public static void BroadcastMessage(string message)
        {
            byte[] messageData = Encoding.ASCII.GetBytes(message);
            foreach (var client in clients)
            {
                client.GetStream().Write(messageData, 0, messageData.Length);
            }
        }

        public static void BroadcastBoardState()
        {
            byte[] boardData = Encoding.ASCII.GetBytes(GetBoardString());
            foreach (var client in clients)
            {
                client.GetStream().Write(boardData, 0, boardData.Length);
            }
            Console.WriteLine("Current board state:");
            Console.WriteLine(GetBoardString());
        }

        public static string GetBoardString()
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    sb.Append(board[i, j]);
                    if (j < 2)
                        sb.Append(',');
                }
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public static bool CheckForWinner()
        {
            // Check rows, columns, and diagonals for a win
            for (int i = 0; i < 3; i++)
            {
                if (board[i, 0] == board[i, 1] && board[i, 1] == board[i, 2])
                    return true; // row win
                if (board[0, i] == board[1, i] && board[1, i] == board[2, i])
                    return true; // column win
            }

            if (board[0, 0] == board[1, 1] && board[1, 1] == board[2, 2])
                return true; 
            if (board[0, 2] == board[1, 1] && board[1, 1] == board[2, 0])
                return true; 

            return false;
        }

        public static bool IsBoardFull()
        {
           
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] != 'X' && board[i, j] != 'O')
                        return false;
                }
            }
            return true;
        }

        public static bool IsValidMove(string moveString, out int row, out int col)
        {
            row = col = -1;
            if (int.TryParse(moveString, out int move) && move >= 1 && move <= 9)
            {
                int index = move - 1;
                row = index / 3;
                col = index % 3;
                return board[row, col] != 'X' && board[row, col] != 'O';
            }
            return false;
        }

        public static async Task NotifyGameEndAsync(string message)
        {
            foreach (var client in clients)
            {
                await SendMessageAsync(client, message);
            }
            Console.WriteLine(message);
        }
    }
}
