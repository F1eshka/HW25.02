using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Rock_Scissors_Paper
{
    class Server
    {
        private static UdpClient udpServer;
        private static IPEndPoint player1Endpoint, player2Endpoint;
        private static Dictionary<string, string> rules = new Dictionary<string, string>
        {
            {"Rock", "Scissors"},
            {"Scissors", "Paper"},
            {"Paper", "Rock"}
        };

        private static string player1Choice = null, player2Choice = null;
        private static int player1Score = 0, player2Score = 0;
        private static int roundsPlayed = 0;
        private const int MaxRounds = 5;
        private const int ReceiveTimeout = 5000; 
        private static bool gameEnded = false;

        static void Main()
        {
            try
            {
                udpServer = new UdpClient(8080);
                Console.WriteLine("UDP Server is running on port 8080... Waiting for players...");

                while (player1Endpoint == null || player2Endpoint == null)
                {
                    ReceiveConnection();
                }

                Console.WriteLine("Both players are connected! Let the game begin...");
                SendToBothPlayers("Game started! Round 1");

                while (roundsPlayed < MaxRounds && !gameEnded)
                {
                    player1Choice = player2Choice = null;
                    bool movesReceived = ReceiveMovesWithTimeout();
                    if (!movesReceived)
                    {
                        SendToBothPlayers("One of the players disconnected. Game over!");
                        break;
                    }

                    string result = DetermineWinner();
                    roundsPlayed++;

                    SendToBothPlayers($"Round {roundsPlayed}: {result}");

                    Thread.Sleep(1000);
                }

                if (!gameEnded)
                {
                    string finalResult = player1Score > player2Score ? "Player 1 wins!" :
                                         player2Score > player1Score ? "Player 2 wins!" : "Tie!";
                    SendToBothPlayers($"Game over! {finalResult}");
                }

                Console.WriteLine("Game over. The server is shutting down...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Server error: {ex.Message}");
            }
            finally
            {
                udpServer?.Close();
            }
        }

        private static void ReceiveConnection()
        {
            try
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = udpServer.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (message == "connect")
                {
                    if (player1Endpoint == null)
                    {
                        player1Endpoint = remoteEP;
                        udpServer.Send(Encoding.UTF8.GetBytes("You're Player 1."), 10, player1Endpoint);
                        Console.WriteLine($"Player 1 connected: {player1Endpoint}");
                    }
                    else if (player2Endpoint == null && !remoteEP.Equals(player1Endpoint))
                    {
                        player2Endpoint = remoteEP;
                        udpServer.Send(Encoding.UTF8.GetBytes("You're Player 2."), 10, player2Endpoint);
                        Console.WriteLine($"Player 2 connected: {player2Endpoint}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when connecting a player: {ex.Message}");
            }
        }

        private static bool ReceiveMovesWithTimeout()
        {
            DateTime startTime = DateTime.Now;
            while (player1Choice == null || player2Choice == null)
            {
                if ((DateTime.Now - startTime).TotalMilliseconds > ReceiveTimeout)
                {
                    Console.WriteLine("Timeout waiting for players' moves!");
                    return false;
                }

                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udpServer.Receive(ref remoteEP);
                    string move = Encoding.UTF8.GetString(data).Trim();

                    move = char.ToUpper(move[0]) + move.Substring(1).ToLower();

                    if (!rules.ContainsKey(move) && move != "Draw" && move != "Surrender")
                    {
                        Console.WriteLine($"Player sent an incorrect move: {move}");
                        continue;
                    }

                    if (remoteEP.Equals(player1Endpoint))
                        player1Choice = move;
                    else if (remoteEP.Equals(player2Endpoint))
                        player2Choice = move;

                    HandleSpecialMoves();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error when receiving a move: {ex.Message}");
                }
            }
            return true;
        }

        private static void HandleSpecialMoves()
        {
            if (player1Choice == "Surrender")
            {
                gameEnded = true;
                SendToBothPlayers("Game over! Player 2 wins by surrender.");
            }
            else if (player2Choice == "Surrender")
            {
                gameEnded = true;
                SendToBothPlayers("Game over! Player 1 wins by surrender.");
            }
            else if (player1Choice == "Draw" && player2Choice == "Draw")
            {
                gameEnded = true;
                SendToBothPlayers("Game over! Players agreed to a draw.");
            }
        }

        private static string DetermineWinner()
        {
            if (player1Choice == "Draw" || player2Choice == "Draw")
            {
                if (player1Choice == "Draw" && player2Choice == "Draw")
                    return "Players agreed to a draw.";
                return "Waiting for opponent's response...";
            }

            if (player1Choice == player2Choice)
                return "Tie!";

            if (rules[player1Choice] == player2Choice)
            {
                player1Score++;
                return $"Player 1 wins! ({player1Choice} beats {player2Choice})";
            }
            else
            {
                player2Score++;
                return $"Player 2 wins! ({player2Choice} beats {player1Choice})";
            }
        }

        private static void SendToBothPlayers(string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                if (player1Endpoint != null)
                    udpServer.Send(data, data.Length, player1Endpoint);
                if (player2Endpoint != null)
                    udpServer.Send(data, data.Length, player2Endpoint);
                Console.WriteLine($"Sent to players: {message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error when sending data to players: {ex.Message}");
            }
        }
    }
}