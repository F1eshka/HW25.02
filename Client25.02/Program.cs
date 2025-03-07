using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Client
{
    class Client
    {
        private static UdpClient udpClient;
        private static IPEndPoint serverEndpoint;
        private static string[] moves = { "Rock", "Paper", "Scissors" };
        private const int ReceiveTimeout = 5000; 

        static void Main()
        {
            try
            {
                udpClient = new UdpClient();
                serverEndpoint = new IPEndPoint(IPAddress.Loopback, 8080);

                Console.WriteLine("Connecting to the server...");
                byte[] connectData = Encoding.UTF8.GetBytes("connect");
                udpClient.Send(connectData, connectData.Length, serverEndpoint);

                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                udpClient.Client.ReceiveTimeout = ReceiveTimeout;
                byte[] response = udpClient.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(response);
                Console.WriteLine(message);

                response = udpClient.Receive(ref remoteEP);
                Console.WriteLine(Encoding.UTF8.GetString(response));

                while (true)
                {
                    Console.WriteLine("\nSelect: 1-Rock, 2-Scissors, 3-Paper, 4-Offer Draw, 5-Surrender");
                    int choice;
                    do
                    {
                        Console.Write("Your choice 1-5: ");
                    } while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > 5);

                    string move;
                    if (choice >= 1 && choice <= 3)
                    {
                        move = moves[choice - 1];
                    }
                    else if (choice == 4)
                    {
                        move = "Draw";
                    }
                    else
                    {
                        move = "Surrender";
                    }

                    byte[] moveData = Encoding.UTF8.GetBytes(move);
                    udpClient.Send(moveData, moveData.Length, serverEndpoint);

                    response = udpClient.Receive(ref remoteEP);
                    string result = Encoding.UTF8.GetString(response);
                    Console.WriteLine(result);

                    if (result.Contains("Game over"))
                        break;
                }
            }
            catch (SocketException ex)
            {
                Console.WriteLine($"Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                udpClient?.Close();
                Console.WriteLine("Client disconnected.");
            }
        }
    }
}