using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using OpenAI; 

public class GameManager : MonoBehaviour
{
    [SerializeField] TMP_InputField inputMsg, inputLeader;

    [SerializeField] TextMeshProUGUI responseText; 

    OpenAIApi openAI = new OpenAIApi(); 

    List<ChatMessage> messages = new List<ChatMessage>();

    [SerializeField] Leader[] leaders; 

    public async void AskGPT(string leader, string msg, string action, string role) {
        ChatMessage newMsg = new ChatMessage(); 
        newMsg.Content = "Leader: " + leader + "\nMessage: " + msg + "\nAction: " + action;
        newMsg.Role = role; 

        messages.Add(newMsg);

        CreateChatCompletionRequest request = new CreateChatCompletionRequest(); 
        request.Messages = messages;
        request.Model = "gpt-4";
        request.Temperature = 0.3f;
        request.MaxTokens = 264;
        request.FrequencyPenalty = 0;
        request.PresencePenalty = 0; 

        Debug.Log("request made"); 

        var response = await openAI.CreateChatCompletion(request); 

        if (response.Choices != null && response.Choices.Count > 0) {
            var chatResponse = response.Choices[0].Message;
            messages.Add(chatResponse);

            responseText.text = chatResponse.Content; 
            Debug.Log(chatResponse.Content); 
        }
    }

    public async void AskGPT(string msg, string role) {
        ChatMessage newMsg = new ChatMessage(); 
        newMsg.Content = msg; 
        newMsg.Role = role; 

        messages.Add(newMsg); 

        /*CreateChatCompletionRequest request = new CreateChatCompletionRequest();
        request.Messages = messages;
        request.Model = "gpt-4";

        var response = await openAI.CreateChatCompletion(request);

        if (response.Choices != null && response.Choices.Count > 0) {
            var chatResponse = response.Choices[0].Message;
            messages.Add(chatResponse);

            Debug.Log(chatResponse.Content);
        }*/
    }

    // Start is called before the first frame update
    void Start()
    {
        var initialPrompt = Resources.Load<TextAsset>("initial");

        AskGPT(initialPrompt.text, "system"); 
    }

    public void Action(string action) {
        AskGPT(inputLeader.text, inputMsg.text, action, "user"); 
    }
}
