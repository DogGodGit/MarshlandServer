using System;
using System.Configuration;
using System.Text.RegularExpressions;
using NamedPipeWrapper;
using ServerControlMessage;

namespace MainServer
{
    class ServerControlledClient
    {
        private readonly NamedPipeClient<ServerControlMessageType> myClient;
        internal bool KeepRunning { get; set; }
        private ShutdownMessageManager shutdownMessageManager;

        public ServerControlledClient()
        {
            KeepRunning = true;
            string pipeName = ConfigurationManager.AppSettings["PipeName"];
            myClient = new NamedPipeClient<ServerControlMessageType>(pipeName);
            myClient.ServerMessage += OnServerMessage;
            myClient.Error += OnError;
            myClient.Start();
        }

        internal void Update()
        {
            if (KeepRunning)
            {
                if (shutdownMessageManager != null)
                {
                    KeepRunning = shutdownMessageManager.Update();
                }
            }
            else
            {
                myClient.Stop();
            }
        }

        private void OnServerMessage(NamedPipeConnection<ServerControlMessageType, ServerControlMessageType> connection,
            ServerControlMessageType message)
        {
            Console.WriteLine("Server says: {0}", message);

            string[] messageComponents = Regex.Split(message.Text, ";");

            if (messageComponents[0] == "ShutdownSystemMessage")
            {
                if (messageComponents.Length == 4)
                // We should have "ShutdownSystemMessage", a shutdown time component, a time interval to send messages out and the actual message
                {
                    string shutdownTimeComponent = messageComponents[1];
                    int shutdownTime = int.Parse(shutdownTimeComponent);
                    string timeIntervalComponent = messageComponents[2];
                    int timeInterval = int.Parse(timeIntervalComponent);
                    string actualMessage = messageComponents[3];

                    shutdownMessageManager = new ShutdownMessageManager(shutdownTime, timeInterval, actualMessage);
                    // Get this over to program to start firing out messages every x minutes and then shutdown
                }
            }
            else if (messageComponents[0] == "Welcome")
            {
                myClient.PushMessage(new ServerControlMessageType
                {
                    Id = Program.m_worldID,
                    Text = Program.m_ServerName
                });
            }
            else if (messageComponents[0] == "SystemMessage")
            {
                if (messageComponents.Length > 1)
                {
                    string systemMessage = messageComponents[1];
                    Program.sendSystemMessage(systemMessage, true, false);
                }
            }
            else
            {
                //We have an issue with the system message
            }
        
        }

        private void OnError(Exception exception)
        {
            Console.WriteLine("ERROR: {0}", exception);
        }
    }
}
