

using JangadHisabApp.Dtos;

namespace JangadHisabApp.Service
{
    public interface ITokenService
    {
        string GenerateToken(tokendto tokenDto);
    }
}
