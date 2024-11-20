// ReSharper disable MemberCanBeMadeStatic.Global

namespace AccountManagement.API;

public partial class AccountResource
{
    public static partial class Command
    {
        public partial class LogIn
        {
            public class LoginAttemptResult
            {
                public string AuthenticationToken { get; private set; }  = string.Empty;
                public bool Succeeded { get; private set; }

                public static LoginAttemptResult Success(string authenticationToken) => new()
                                                                                        {
                                                                                            AuthenticationToken = authenticationToken,
                                                                                            Succeeded = true
                                                                                        };

                public static LoginAttemptResult Failure() => new()
                                                              {
                                                                  Succeeded = false
                                                              };
            }
        }
    }
}