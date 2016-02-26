using Lib_K_Relay;
using Lib_K_Relay.Interface;
using Lib_K_Relay.Networking;
using Lib_K_Relay.Networking.Packets;
using Lib_K_Relay.Networking.Packets.Client;
using Lib_K_Relay.Networking.Packets.DataObjects;
using Lib_K_Relay.Networking.Packets.Server;
using Lib_K_Relay.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;



namespace TriviaBot
{
    public class Trivia : IPlugin
    {
        int repeatCount = 0;
        string question = "";
        bool enabled = false;
        bool AcceptAnswers = false;
        bool first = true;
        bool isRepeating = false;
        Dictionary<string, int> scores = new Dictionary<string, int>();
        Dictionary<string, int> scoresWeek = new Dictionary<string, int>();
        ArrayList Answer = new ArrayList();
        public string GetAuthor()
        {
            return "XLights";
        }

        public string GetName()
        {
            return "ROTMG Trivia Bot";
        }

        public string GetDescription()
        {
            return "An interactive trivia bot";
        }

        public string[] GetCommands()
        {
            return new string[] { "/start" };
        }

        public void Initialize(Proxy proxy)
        {
            proxy.HookCommand("start", OnStartCommand);
            proxy.HookPacket(PacketType.TEXT, onText);


        }

        public void OnStartCommand(Client client, string command, string[] args)
        {
            enabled = true;
            loadScore();
            startQuestion(client);

        }
        public void loadScore()
        {
            System.IO.StreamReader file = null;
            string line;
            file = new System.IO.StreamReader("scores.txt");
            while ((line = file.ReadLine()) != null)
            {
                int index = line.IndexOf(':');
                string playerName = line.Substring(0, index);
                int score = Int32.Parse(line.Substring(index + 1));
                if (scores.ContainsKey(playerName))
                {
                    scores[playerName] = score;

                }
                else
                {
                    scores.Add(playerName, score);
                }
            }
            file.Close();
            file = new System.IO.StreamReader("scores-week.txt");
            while ((line = file.ReadLine()) != null)
            {
                int index = line.IndexOf(':');
                string playerName = line.Substring(0, index);
                int score = Int32.Parse(line.Substring(index + 1));
                if (scores.ContainsKey(playerName))
                {
                    scoresWeek[playerName] = score;

                }
                else
                {
                    scoresWeek.Add(playerName, score);
                }
            }
            file.Close();

        }
        public void startQuestion(Client c)
        {
            PlayerTextPacket pt1 = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
            pt1.Text = "Welcome to " + c.PlayerData.Name + "'s trivia show! The next question will begin in 5 seconds!";
            c.SendToServer(pt1);
            PluginUtils.Delay(5000, () =>
            {
                //Console.WriteLine("Step 1");
                poseQuestion(c);
            });
            if (first)
            {
                PluginUtils.Delay(12345, () => { announce(c); });
                first = false;
            }

        }

        public void poseQuestion(Client c)
        {
            //Console.WriteLine("Step 2");
            PlayerTextPacket questionPacket = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
            question = FindQuestion();
            questionPacket.Text = question;
            AcceptAnswers = true;
            c.SendToServer(questionPacket);
            PluginUtils.Delay(30000, () => { repeatQuestion(c, question); });//repeats the question if people are taking too long
            isRepeating = true;

        }

        public void repeatQuestion(Client c, string questionTemp)
        {
            if(AcceptAnswers && isRepeating && question == questionTemp)
            {
                repeatCount++;
                if(repeatCount >= 10)
                {
                    repeatCount = 0;
                    question = null;
                    Answer.Clear();
                    AcceptAnswers = false;
                    poseQuestion(c);
                    return;

                }
                string questionText = "";
                Random r = new Random();
                int randInt = r.Next(4);
                if (randInt == 0)
                {
                    questionText = "Here's the question: ";

                }

                PlayerTextPacket questionPacket = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
                questionText += question;
                questionPacket.Text = questionText;
                c.SendToServer(questionPacket);
                PluginUtils.Delay(30000, () => { repeatQuestion(c, questionTemp); });
                isRepeating = true;
            }
        }


        public string FindQuestion()
        {//picks a random question + answer
            //Console.WriteLine("Step 3");
            System.IO.StreamReader file = null;
            string s = "";
            try
            {
                file =
                   new System.IO.StreamReader("Questions.txt");


                string line;
                ArrayList al = new ArrayList();
                while ((line = file.ReadLine()) != null)
                {
                    //Console.WriteLine(line);
                    al.Add(line);
                }
               // Console.WriteLine(al.Count);
                int length = al.Count / 2;
                Random r = new Random();
                int q = r.Next(length);
                string answerUnparsed = (string)al[q * 2 + 1];
                //Console.Write(answerUnparsed);
                Answer.Clear();
                while (answerUnparsed.IndexOf(',') != -1)
                {

                    int index = answerUnparsed.IndexOf(',');
                    string str = answerUnparsed.Substring(0, index);
                    Answer.Add(str);
                    answerUnparsed = answerUnparsed.Substring(index + 2);

                }
                Answer.Add(answerUnparsed);
                s = (string)al[q * 2];
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                s = "";

            }
            return s;
        }

        public void onText(Client c, Packet p)
        {
            TextPacket tp = (TextPacket)p;
            string send;
            if (tp.Text.Equals("!score"))//for when you want to know your score
            {
                if (scores.ContainsKey(tp.Name))
                {
                    send = "/tell " + tp.Name + " You have " + scores[tp.Name] + " points!";
                }
                else
                {
                    send = "/tell " + tp.Name + " You have 0 points!";
                }
                PlayerTextPacket pt = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);//sends
                pt.Text = send;
                c.SendToServer(pt);
            }
            else if (tp.Text.Equals("!topscores"))//for finding the top 3 scores
            {
                string[] top = new string[3];//organizes the top 3 scores from the scores dictionary
                Dictionary<string, int> copy = null;
                copy = new Dictionary<string, int>(scores);
                string biggest = null;

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        biggest = copy.Keys.ToArray()[0];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("No more people at i=" + i);
                        copy.Add("null0", 0);
                        biggest = "null0";
                    }

                    for (int j = 0; j < copy.Keys.Count; j++)
                    {
                        if (copy[copy.Keys.ToArray()[j]] > copy[biggest])
                        {
                            biggest = copy.Keys.ToArray()[j];
                        }
                        
                    }
                    copy.Remove(biggest);
                    top[i] = biggest;

                }
                send = "/tell " + tp.Name + " The top 3 players are: ";
                for (int i = 0; i < 3; i++)
                {
                    if (top[i] != "null0")
                    {
                        send = send + top[i] + " with " + scores[top[i]];
                        if (i == 0)
                        {
                            send += " , ";
                        }
                        if (i == 1)
                        {
                            send += " ,and ";
                        }
                        else if (i == 2)
                        {
                            send += "!";
                        }
                    }
                    else
                    {
                        send += "and nobody else!";
                        break;
                    }
                }
                PlayerTextPacket pt = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);//sends
                pt.Text = send;
                c.SendToServer(pt);
            }
            else if(tp.Text.Length>=8 && tp.Text.Substring(0,8).Equals("!suggest"))
            {
                if (tp.Text.Length == 8)
                {
                    PlayerTextPacket error = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
                    error.Text = "/tell " + tp.Name + " You should use this command as !suggest [suggestion here]";
                    c.SendToServer(error);
                }
                else
                {
                    StreamWriter w = new StreamWriter("suggestions.txt", true);
                    w.WriteLine(tp.Text.Substring(10));
                    PlayerTextPacket pt = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
                    pt.Text = "/tell " + tp.Name + " Your suggestion was received and stored!";
                    c.SendToServer(pt);
                    w.Flush();
                    w.Close();
                }
            }
            else if(tp.Text == "!weekscores")
            {
                string[] top = new string[3];//organizes the top 3 scores from the scores dictionary
                Dictionary<string, int> copy = null;
                copy = new Dictionary<string, int>(scoresWeek);
                string biggest = null;

                for (int i = 0; i < 3; i++)
                {
                    try
                    {
                        biggest = copy.Keys.ToArray()[0];
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("No more people at i=" + i);
                        copy.Add("null0", 0);
                        biggest = "null0";
                    }

                    for (int j = 0; j < copy.Keys.Count; j++)
                    {
                        if (copy[copy.Keys.ToArray()[j]] > copy[biggest])
                        {
                            biggest = copy.Keys.ToArray()[j];
                        }

                    }
                    copy.Remove(biggest);
                    top[i] = biggest;

                }
                send = "/tell " + tp.Name + " The top 3 players this week are: ";
                for (int i = 0; i < 3; i++)
                {
                    if (top[i] != "null0")
                    {
                        send = send + top[i] + " with " + scoresWeek[top[i]];
                        if (i == 0)
                        {
                            send += " , ";
                        }
                        if (i == 1)
                        {
                            send += " ,and ";
                        }
                        else if (i == 2)
                        {
                            send += "!";
                        }
                    }
                    else
                    {
                        send += "and nobody else!";
                        break;
                    }
                }
                PlayerTextPacket pt = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);//sends
                pt.Text = send;
                c.SendToServer(pt);

            }
            if (AcceptAnswers)
            {//for when people want to answer
                //TextPacket tp = (TextPacket)p;
                for (int i = 0; i < Answer.Count; i++)
                {
                    if (tp.Text.ToLowerInvariant().Equals(((string)Answer[i]).ToLowerInvariant()))
                    {//if the answer is right
                        AcceptAnswers = false;
                        string Player = tp.Name;
                        PlayerTextPacket pt = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);
                        pt.Text = Player + " has the correct answer! 1 point is awarded to " + Player + "!";
                        c.SendToServer(pt);
                        isRepeating = false;
                        question = null;
                        if (scores.ContainsKey(Player))//handles scoring
                        {
                            scores[Player] = scores[Player] + 1;

                        }
                        else
                        {
                            scores.Add(Player, 1);
                        }
                        if (scoresWeek.ContainsKey(Player))//handles scoring
                        {
                            scoresWeek[Player] = scoresWeek[Player] + 1;

                        }
                        else
                        {
                            scoresWeek.Add(Player, 1);
                        }
                        save();
                        PluginUtils.Delay(15000, () =>
                        {
                            startQuestion(c);//starts another question after 25s
                        });
                    }
                }
            }

        }

        public void save()
        {
            System.IO.StreamWriter file = null;
            file = new StreamWriter("scores.txt");
            for (int i = 0; i < scores.Keys.Count; i++)
            {
                file.WriteLine(scores.Keys.ToArray()[i] + ":" + scores[scores.Keys.ToArray()[i]]);
            }
            file.Flush();
            file.Close();
            StreamWriter file2 = new StreamWriter("scores-week.txt");
            for (int i = 0; i < scoresWeek.Keys.Count; i++)
            {
                file2.WriteLine(scoresWeek.Keys.ToArray()[i] + ":" + scoresWeek[scoresWeek.Keys.ToArray()[i]]);
            }
            file2.Flush();
            file2.Close();
            //PluginUtils.Delay(60000, save);
        }
        public void announce(Client c)
        {
            string[] announcements = {"Type !score to find your score!", "Type !topscores to find the 3 highest scoring players!",
                "Type !suggest [ suggestion here ] to suggest a question or feature!",
                                         "Have a question/suggestion, complaint, or a good trivia question? Message me on realmeye!", "Type !weekscores to find out the top 3 scores this week!"};
            Random r = new Random();
            int q = r.Next(announcements.Length);
            PlayerTextPacket pt = (PlayerTextPacket)Packet.Create(PacketType.PLAYERTEXT);//sends
            pt.Text = announcements[q];
            c.SendToServer(pt);
            PluginUtils.Delay(60000, () => { announce(c); });


        }
    }

    /*private bool _enabled = true;

    public string GetAuthor() 
    { return "ME!"; }

    public string GetName() 
    { return "My Very Own Plugin"; }

    public string GetDescription() 
    { return "This plugin let's you know when you've said something in chat."; }

    public string[] GetCommands()
    { return new string[] { "/myplugin enable:disable" }; }

    public void Initialize(Proxy proxy)
    {

        proxy.HookCommand("myplugin", OnMyPluginCommand);
        proxy.HookPacket(PacketType.PLAYERTEXT, OnPlayerText);
        proxy.HookPacket(PacketType.TEXT, onText);
    }


    private void OnMyPluginCommand(Client client, string command, string[] args)
    {
        if (args.Length == 0) return;

        if (args[0] == "enable")  _enabled = true;
        if (args[0] == "disable") _enabled = false;
    }

    private void OnPlayerText(Client client, Packet packet)
    {
        if (!_enabled) return;
        PlayerTextPacket playerText = (PlayerTextPacket)packet;

        client.SendToClient(PluginUtils.CreateOryxNotification(
            "My Plugin", "You said: " + playerText.Text));
    }
    public void onText(Client c, Packet p)
    {
        TextPacket tp = (TextPacket)p;
        Console.WriteLine(tp.Text);
    }
}*/
}


