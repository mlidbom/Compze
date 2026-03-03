namespace AccountManagement.Domain.Passwords;

public class PasswordDoesNotMatchPolicyException : ArgumentException
{
   internal PasswordDoesNotMatchPolicyException(IReadOnlyList<Password.Policy.Failures> passwordPolicyFailures) : base(BuildTessage(passwordPolicyFailures)) => Failures = passwordPolicyFailures;

   public IReadOnlyList<Password.Policy.Failures> Failures { get; private set; }

   static string BuildTessage(IEnumerable<Password.Policy.Failures> passwordPolicyFailures) => string.Join(",", passwordPolicyFailures.Select(failure => failure.ToString()));
}