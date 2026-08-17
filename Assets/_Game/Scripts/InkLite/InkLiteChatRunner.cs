using System.Collections;
using UnityEngine;

namespace PracticeAnything.InkLite
{
    public sealed class InkLiteChatRunner : MonoBehaviour
    {
        [SerializeField] private TextAsset scenarioAsset;
        [SerializeField] private InkLiteChatView view;
        [SerializeField] private string startNode = "start";
        [SerializeField] private float npcTypingSeconds = 1f;
        [SerializeField] private float npcTypingFrameSeconds = 0.25f;
        [SerializeField] private float messageGapSeconds = 0.25f;

        private InkLiteScenario scenario;
        private Coroutine playRoutine;

        private void Start()
        {
            Restart();
        }

        public void Restart()
        {
            if (playRoutine != null)
            {
                StopCoroutine(playRoutine);
            }

            view.Clear();

            try
            {
                scenario = InkLiteParser.Parse(scenarioAsset.text);
                playRoutine = StartCoroutine(PlayNode(startNode));
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception, this);
                view.SetStatus(exception.Message);
            }
        }

        private IEnumerator PlayNode(string nodeId)
        {
            if (!scenario.TryGetNode(nodeId, out InkLiteNode node))
            {
                view.SetStatus($"Missing node: {nodeId}");
                yield break;
            }

            for (int i = 0; i < node.Commands.Count; i++)
            {
                InkLiteCommand command = node.Commands[i];
                if (command is InkLiteMessageCommand message)
                {
                    if (message.Speaker == InkLiteSpeaker.Npc)
                    {
                        yield return PlayNpcTyping();
                    }

                    view.AddMessage(message.Speaker, message.MessageType, message.Content);
                    yield return new WaitForSeconds(messageGapSeconds);
                    continue;
                }

                if (command is InkLiteChoiceCommand choice)
                {
                    bool selected = false;
                    InkLiteChoiceOption selectedOption = null;
                    view.ShowChoices(choice.Options, index =>
                    {
                        selectedOption = choice.Options[index];
                        selected = true;
                    });

                    yield return new WaitUntil(() => selected);
                    view.HideChoices();
                    view.AddMessage(InkLiteSpeaker.Player, InkLiteMessageType.Text, selectedOption.Reply);
                    yield return new WaitForSeconds(messageGapSeconds);
                    yield return PlayNode(selectedOption.NextNode);
                    yield break;
                }

                if (command is InkLiteGotoCommand gotoCommand)
                {
                    yield return PlayNode(gotoCommand.Target);
                    yield break;
                }

                if (command is InkLiteEndCommand)
                {
                    view.SetStatus("模擬結束");
                    yield break;
                }
            }

            view.SetStatus("模擬結束");
        }

        private IEnumerator PlayNpcTyping()
        {
            string[] frames = { ".", "..", "..." };
            float elapsed = 0f;
            int frameIndex = 0;

            view.ShowTyping(true);
            while (elapsed < npcTypingSeconds)
            {
                view.SetTypingText(frames[frameIndex]);
                frameIndex = (frameIndex + 1) % frames.Length;

                float waitSeconds = Mathf.Min(npcTypingFrameSeconds, npcTypingSeconds - elapsed);
                elapsed += waitSeconds;
                yield return new WaitForSeconds(waitSeconds);
            }

            view.ShowTyping(false);
        }
    }
}
