using BusinessLogicLayer.Services;

namespace UnitTests.Infrastructure;

/// <summary>
/// Thay thế FirebaseService trong unit test.
/// Bỏ qua việc kết nối Firebase thật, ghi nhận tham số để dùng trong Assert.
/// Sử dụng: var firebase = new TestFirebaseService();
///          firebase.ThrowOnSend = new Exception("lỗi");  // nếu muốn giả lập lỗi
///          Assert.Equal("user_605", firebase.Topic);
/// </summary>
public class TestFirebaseService : FirebaseService
{
    /// <summary>Số lần SendNotificationToTopicAsync được gọi.</summary>
    public int CallCount { get; private set; }

    /// <summary>Topic của lần gọi cuối cùng.</summary>
    public string? Topic { get; private set; }

    /// <summary>Title của lần gọi cuối cùng.</summary>
    public string? Title { get; private set; }

    /// <summary>Body của lần gọi cuối cùng.</summary>
    public string? Body { get; private set; }

    /// <summary>
    /// Nếu được gán, SendNotificationToTopicAsync sẽ ném exception này
    /// thay vì trả về thành công – dùng cho TC-RSS-045.
    /// </summary>
    public Exception? ThrowOnSend { get; set; }

    /// <summary>
    /// Gọi constructor protected FirebaseService(skipInit: true)
    /// để bỏ qua việc đọc file credential và khởi tạo Firebase.
    /// </summary>
    public TestFirebaseService() : base(skipInit: true) { }

    public override Task<string> SendNotificationToTopicAsync(
        string topic, string title, string body)
    {
        CallCount++;
        Topic = topic;
        Title = title;
        Body = body;

        if (ThrowOnSend != null)
            throw ThrowOnSend;

        return Task.FromResult("fake-message-id");
    }
}
