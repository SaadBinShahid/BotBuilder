using System;
using System.Threading.Tasks;
using System.Web.Http;

using Microsoft.Bot.Connector;
using Microsoft.Bot.Builder.Dialogs;
using System.Web.Http.Description;
using System.Net.Http;
using System.Diagnostics;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Autofac;
using System.Linq;
using System.Collections.Generic;

namespace Microsoft.Bot.Sample.SimpleEchoBot
{
    [Serializable]
    public class EchoDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
        }

        public async Task MessageReceivedAsync(IDialogContext context, IAwaitable<IMessageActivity> argument)
        {
            var message = await argument;
            await context.PostAsync("You said: " + message.Text);
            context.Wait(MessageReceivedAsync);
        }
    }

    [BotAuthentication]
    public class MessagesController : ApiController
    {
        /// <summary>
        /// POST: api/Messages
        /// receive a message from a user and send replies
        /// </summary>
        /// <param name="activity"></param>
        [ResponseType(typeof(void))]
        public virtual async Task<HttpResponseMessage> Post([FromBody] Activity activity)
        {
            // check if activity is of type message
            if (activity != null && activity.GetActivityType() == ActivityTypes.Message)
            {
                HandleUserMessage(activity);
            }
            else
            {
                HandleSystemMessage(activity);
            }
            return new HttpResponseMessage(System.Net.HttpStatusCode.Accepted);
        }

        private async Task<Activity> HandleSystemMessage(Activity message)
        {
            if (message.Type == ActivityTypes.DeleteUserData)
            {
                // Implement user deletion here
                // If we handle user deletion, return a real message
            }
            else if (message.Type == ActivityTypes.ConversationUpdate)
            {
                // Handle conversation state changes, like members being added and removed
                // Use .MembersAdded and Activity.MembersRemoved and Activity.Action for info
                // Not available in all channels
                Activity activity = message;
                IConversationUpdateActivity update = activity;
                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                {
                    var client = scope.Resolve<IConnectorClient>();
                    if (update.MembersAdded.Any())
                    {
                        var reply = activity.CreateReply();
                        var newMembers = update.MembersAdded?.Where(t => t.Id != activity.Recipient.Id);
                        foreach (var newMember in newMembers)
                        {
                            reply.Text = "Welcome";
                            if (!string.IsNullOrEmpty(newMember.Name))
                            {
                                reply.Text +=  $" {newMember.Name}";
                            }
                            reply.Text += ", I'm a automated bot planning to takeover the world !";
                            await client.Conversations.ReplyToActivityAsync(reply);
                        }
                    }
                }
            }
            else if (message.Type == ActivityTypes.ContactRelationUpdate)
            {
                // Handle add/remove from contact lists
                // Activity.From + Activity.Action represent what happened
            }
            else if (message.Type == ActivityTypes.Typing)
            {
                // Handle knowing tha the user is typing
            }
            else if (message.Type == ActivityTypes.Ping)
            {
            }

            return null;
        }

        private async Task<Activity> HandleUserMessage(Activity activity)
        {
            if (activity.Type == ActivityTypes.Message)
            {
                using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, activity))
                {
                    var client = scope.Resolve<IConnectorClient>();
                    var reply = activity.CreateReply();


                    if (activity.Text == "Yes, I'm.")
                    {
                        reply.Text = "Well, I work !";
                        reply.Attachments.Add(new Attachment()
                        {
                            ContentUrl = "https://upload.wikimedia.org/wikipedia/en/a/a6/Bender_Rodriguez.png",
                            ContentType = "image/png",
                            Name = "Bender_Rodriguez.png"
                        });
                    }
                    else if (activity.Text == "No, Order a pizza")
                    {
                        //await Conversation.SendAsync(activity, MakeRoot);
                        reply.Text = "TODO: Start Order a Pizza Process.!";
                    }
                    else
                    {
                        reply.Text = "Are you here to test the bot ?";

                        CardAction yesButton = new CardAction()
                        {
                            Type = "imBack",
                            Title = "Yes, I'm.",
                            Value = "Yes, I'm."
                        };

                        CardAction noButton = new CardAction()
                        {
                            Type = "imBack",
                            Title = "No, Order a pizza",
                            Value = "No, Order a pizza"
                        };


                        List<CardAction> cardButtons = new List<CardAction>();
                        cardButtons.Add(yesButton);
                        cardButtons.Add(noButton);

                        HeroCard plCard = new HeroCard()
                        {
                            //Title = "I'm a hero card",
                            //Subtitle = "Pig Latin Wikipedia Page",
                            Buttons = cardButtons
                        };
                        Attachment plAttachment = plCard.ToAttachment();
                        reply.Attachments.Add(plAttachment);
                    }
                    await client.Conversations.ReplyToActivityAsync(reply);
                }
            }

            return null;
        }

    }
}