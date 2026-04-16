namespace SatisfactoryTools.Solver.Api.Services;

public sealed class ShareValidationException : Exception
{
	public ShareValidationException(string message)
		: base(message)
	{
	}
}
