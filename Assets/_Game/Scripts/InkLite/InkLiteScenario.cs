using System;
using System.Collections.Generic;

namespace PracticeAnything.InkLite
{
    public enum InkLiteSpeaker
    {
        Npc,
        Player
    }

    public enum InkLiteMessageType
    {
        Text,
        Image
    }

    public abstract class InkLiteCommand
    {
    }

    public sealed class InkLiteMessageCommand : InkLiteCommand
    {
        public InkLiteMessageCommand(InkLiteSpeaker speaker, InkLiteMessageType messageType, string content)
        {
            Speaker = speaker;
            MessageType = messageType;
            Content = content;
        }

        public InkLiteSpeaker Speaker { get; }
        public InkLiteMessageType MessageType { get; }
        public string Content { get; }
    }

    public sealed class InkLiteChoiceCommand : InkLiteCommand
    {
        public InkLiteChoiceCommand(IReadOnlyList<InkLiteChoiceOption> options)
        {
            Options = options;
        }

        public IReadOnlyList<InkLiteChoiceOption> Options { get; }
    }

    public sealed class InkLiteGotoCommand : InkLiteCommand
    {
        public InkLiteGotoCommand(string target)
        {
            Target = target;
        }

        public string Target { get; }
    }

    public sealed class InkLiteEndCommand : InkLiteCommand
    {
    }

    public sealed class InkLiteChoiceOption
    {
        public InkLiteChoiceOption(string text, string reply, string nextNode)
        {
            Text = text;
            Reply = reply;
            NextNode = nextNode;
        }

        public string Text { get; }
        public string Reply { get; }
        public string NextNode { get; }
    }

    public sealed class InkLiteNode
    {
        public InkLiteNode(string id)
        {
            Id = id;
        }

        public string Id { get; }
        public List<InkLiteCommand> Commands { get; } = new();
    }

    public sealed class InkLiteScenario
    {
        private readonly Dictionary<string, InkLiteNode> nodes = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, InkLiteNode> Nodes => nodes;

        public void AddNode(InkLiteNode node)
        {
            nodes.Add(node.Id, node);
        }

        public bool TryGetNode(string id, out InkLiteNode node)
        {
            return nodes.TryGetValue(id, out node);
        }
    }
}
