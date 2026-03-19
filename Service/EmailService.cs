using JangadHisabApp.Service;
using System.Net;
using System.Net.Mail;

public class EmailService : IEmailService
{
    public async Task SendOtpEmail(string toEmail, int otp)
    {
        var mail = new MailMessage
        {
            From = new MailAddress("bhavinsoft40271@gmail.com", "Jangad Hisab"),
            Subject = "Password Forgot OTP",
            IsBodyHtml = true
        };

        mail.To.Add(toEmail);

        mail.Body = $@"
<!DOCTYPE html>
<html>
<body style='font-family: Arial, sans-serif; background-color:#f4f6f8; padding:20px;'>
    <div style='max-width:500px; margin:auto; background:#ffffff; padding:20px; border-radius:6px;'>
        <h2>Password Forgot Request</h2>
        <p>Hello,</p>
        <p>Your OTP for password Forgot is:</p>
        <h1 style='color:#2e86de;'>{otp}</h1>
       
        <p style='font-size:12px;color:#777;'>Do not share this OTP with anyone.</p>
        <hr />
        <p style='font-size:12px;color:#777;'>© 2025 Jangad Hisab</p>
    </div>
</body>
</html>";

        using var smtp = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(
                "bhavinsoft40271@gmail.com",
                "uzjbmyplaydfyumy"
            ),
            EnableSsl = true
        };

        await smtp.SendMailAsync(mail);
        //< p > This OTP is valid for < b > 10 minutes </ b >.</ p >
    }

}
