namespace JangadHisabApp.Service
{
    public interface IEmailService
    {
        Task SendOtpEmail(string toEmail, int otp);
    }
}
