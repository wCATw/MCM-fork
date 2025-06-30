using System.Reflection;
using System.Collections;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Barotrauma;
using Barotrauma.Networking;
using Barotrauma.Items.Components;

namespace MultiplayerCrewManager
{
    class ClientManager
    {
        private static Dictionary<Client, Character> mapping = new Dictionary<Client, Character>();

        public static void Set(Client client, Character character)
        {
            if (mapping.ContainsKey(client))
            {
                mapping[client] = character;
            }
            else
            {
                mapping.Add(client, character);
            }
        }

        public static bool IsCurrentlyControlled(Character character)
        {
            return mapping.Values.Contains(character);
        }
    }

    class McmClientManager
    {
        private Dictionary<Client, Character> mapping = new Dictionary<Client, Character>();

        public void Set(Client client, Character newCharacter)
        {
            string[] args = new string[]
            {
                client.Name,         
                newCharacter.Name   
            };
            var cmd = DebugConsole.Commands.FirstOrDefault(c => c.Names.Any(n => n == "setclientcharacter"));
            if (client == null || newCharacter == null || string.IsNullOrWhiteSpace(client.Name) || string.IsNullOrWhiteSpace(newCharacter.Name))
            {
                return;
            }
             
            if (cmd != null)
            {
                cmd.Execute(args);
            }
            else
            {
                DebugConsole.NewMessage("Command not found: setclientcharacter", Color.Red);
            }
        }

        public Character? Get(Client client)
        {
            foreach ((var _client, var _char) in mapping)
            {
                if (client == _client) return _char;
            }
            return null;
        }
    }

    class McmControl
    {
        private McmClientManager clientManager;

        public McmControl()
        {
            clientManager = new McmClientManager();
        }

        public void TryGiveControl(Client client, int charId)
        {
            var character = Character.CharacterList.FirstOrDefault(c => charId == c.ID && c.TeamID == CharacterTeamType.Team1);
            ChatMessage cm = null;

            if (character == null)
            {
                cm = ChatMessage.Create("[Server]",
                    $"[MCM] Failed to gain control: Character ID [{charId}] not found",
                    ChatMessageType.Error, null, client);
                cm.IconStyle = "StoreShoppingCrateIcon";
                GameMain.Server.SendDirectChatMessage(cm, client);
                return;
            }

            if (client.Character == character) return;
            if (ClientManager.IsCurrentlyControlled(character))
            {
                cm = ChatMessage.Create("[Server]",
                    $"[MCM] Failed to gain control: '{character.DisplayName}' is already in use",
                    ChatMessageType.Error, null, client);
                cm.IconStyle = "StoreShoppingCrateIcon";
                GameMain.Server.SendDirectChatMessage(cm, client);
                return;
            }

            // Update the mapping
            ClientManager.Set(client, character);
            
            // Actually give control of the character to the client
            clientManager.Set(client, character);

            cm = ChatMessage.Create("[Server]",
                $"[MCM] Gained control of '{character.DisplayName}'",
                ChatMessageType.Server, null, client);
            cm.IconStyle = "StoreShoppingCrateIcon";
            GameMain.Server.SendDirectChatMessage(cm, client);
        }

        public void AssignAiCharacters()
        {
            // assign old
            var clients = Client.ClientList.Where(c => c.Connection.Endpoint.ToString() != "PIPE" && c.Character == null);
            var unassigned = new Queue<Character>();
            foreach (var character in Character.CharacterList.Where(c => c.TeamID == CharacterTeamType.Team1))
            {
                var client = clients.FirstOrDefault(c => c.Name == character.Name);
                if (client != null) 
                {
                    ClientManager.Set(client, character);
                    clientManager.Set(client, character);
                }
                else unassigned.Enqueue(character);
            }
            // assign unassigned
            clients = Client.ClientList.Where(c => c.Connection.Endpoint.ToString() != "PIPE" && c.Character == null);
            foreach (var client in clients)
            {
                if (unassigned.Count <= 0) break;
                var character = unassigned.Dequeue();
                ClientManager.Set(client, character);
                clientManager.Set(client, character);
            }
        }
    }
}
