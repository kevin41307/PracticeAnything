using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PracticeAnything.InkLite
{
    public static class InkLiteParser
    {
        private static readonly Regex MessageRegex = new(@"^(npc|player)\s*:\s*(text|image)\s+""(.*)""\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex ChoiceRegex = new(@"^-\s*""(.*?)""(?:\s*=>\s*""(.*?)"")?\s*->\s*([A-Za-z0-9_\-]+)\s*$", RegexOptions.Compiled);

        public static InkLiteScenario Parse(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                throw new FormatException("InkLite scenario is empty.");
            }

            InkLiteScenario scenario = new();
            InkLiteNode currentNode = null;
            bool readingChoice = false;
            List<InkLiteChoiceOption> pendingOptions = null;
            string[] lines = source.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = StripComment(lines[i]).Trim();
                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("@", StringComparison.Ordinal))
                {
                    FlushChoice(currentNode, ref readingChoice, ref pendingOptions);
                    string id = line[1..].Trim();
                    if (id.Length == 0)
                    {
                        throw Error(i, "Node id is empty.");
                    }

                    currentNode = new InkLiteNode(id);
                    scenario.AddNode(currentNode);
                    continue;
                }

                if (currentNode == null)
                {
                    throw Error(i, "Command appears before the first @node.");
                }

                if (readingChoice && line.StartsWith("-", StringComparison.Ordinal))
                {
                    Match optionMatch = ChoiceRegex.Match(line);
                    if (!optionMatch.Success)
                    {
                        throw Error(i, "Invalid choice option. Use: - \"Option\" => \"Player reply\" -> target");
                    }

                    string text = optionMatch.Groups[1].Value;
                    string reply = optionMatch.Groups[2].Success ? optionMatch.Groups[2].Value : text;
                    string nextNode = optionMatch.Groups[3].Value;
                    pendingOptions.Add(new InkLiteChoiceOption(text, reply, nextNode));
                    continue;
                }

                FlushChoice(currentNode, ref readingChoice, ref pendingOptions);

                if (line.Equals("choice:", StringComparison.OrdinalIgnoreCase))
                {
                    readingChoice = true;
                    pendingOptions = new List<InkLiteChoiceOption>();
                    continue;
                }

                if (line.Equals("end", StringComparison.OrdinalIgnoreCase))
                {
                    currentNode.Commands.Add(new InkLiteEndCommand());
                    continue;
                }

                if (line.StartsWith("goto ", StringComparison.OrdinalIgnoreCase))
                {
                    currentNode.Commands.Add(new InkLiteGotoCommand(line[5..].Trim()));
                    continue;
                }

                Match messageMatch = MessageRegex.Match(line);
                if (messageMatch.Success)
                {
                    InkLiteSpeaker speaker = messageMatch.Groups[1].Value.Equals("npc", StringComparison.OrdinalIgnoreCase) ? InkLiteSpeaker.Npc : InkLiteSpeaker.Player;
                    InkLiteMessageType type = messageMatch.Groups[2].Value.Equals("image", StringComparison.OrdinalIgnoreCase) ? InkLiteMessageType.Image : InkLiteMessageType.Text;
                    currentNode.Commands.Add(new InkLiteMessageCommand(speaker, type, messageMatch.Groups[3].Value));
                    continue;
                }

                throw Error(i, $"Unknown command: {line}");
            }

            FlushChoice(currentNode, ref readingChoice, ref pendingOptions);
            Validate(scenario);
            return scenario;
        }

        private static void FlushChoice(InkLiteNode currentNode, ref bool readingChoice, ref List<InkLiteChoiceOption> pendingOptions)
        {
            if (!readingChoice)
            {
                return;
            }

            if (pendingOptions == null || pendingOptions.Count < 1 || pendingOptions.Count > 3)
            {
                throw new FormatException("choice must contain 1-3 options.");
            }

            currentNode.Commands.Add(new InkLiteChoiceCommand(pendingOptions));
            readingChoice = false;
            pendingOptions = null;
        }

        private static void Validate(InkLiteScenario scenario)
        {
            if (!scenario.Nodes.ContainsKey("start"))
            {
                throw new FormatException("InkLite scenario must contain @start.");
            }

            foreach (InkLiteNode node in scenario.Nodes.Values)
            {
                foreach (InkLiteCommand command in node.Commands)
                {
                    if (command is InkLiteGotoCommand gotoCommand && !scenario.Nodes.ContainsKey(gotoCommand.Target))
                    {
                        throw new FormatException($"Node @{node.Id} has goto to missing node @{gotoCommand.Target}.");
                    }

                    if (command is InkLiteChoiceCommand choiceCommand)
                    {
                        foreach (InkLiteChoiceOption option in choiceCommand.Options)
                        {
                            if (!scenario.Nodes.ContainsKey(option.NextNode))
                            {
                                throw new FormatException($"Node @{node.Id} has choice to missing node @{option.NextNode}.");
                            }
                        }
                    }
                }
            }
        }

        private static string StripComment(string line)
        {
            int index = line.IndexOf("//", StringComparison.Ordinal);
            return index < 0 ? line : line[..index];
        }

        private static FormatException Error(int lineIndex, string message)
        {
            return new FormatException($"Line {lineIndex + 1}: {message}");
        }
    }
}
