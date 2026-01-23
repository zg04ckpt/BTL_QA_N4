using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;

namespace BusinessLogicLayer.Services;

public class FirebaseService
{
    private readonly FirebaseMessaging _messaging;

    public FirebaseService()
    {
        // Initialize Firebase app
        if (FirebaseApp.DefaultInstance == null)
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromFile("wwwroot/Configs/firebase-adminsdk.json")
            });
        }

        _messaging = FirebaseMessaging.DefaultInstance;
    }

    // Send notification to a topic
    public async Task<string> SendNotificationToTopicAsync(string topic, string title, string body)
    {
        var message = new Message
        {
            Topic = topic,
            Notification = new Notification
            {
                Title = title,
                Body = body
            }
        };

        // Send the notification
        return await _messaging.SendAsync(message);
    }
}