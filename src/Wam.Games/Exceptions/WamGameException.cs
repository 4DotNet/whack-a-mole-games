using Wam.Core.Exceptions;
using Wam.Games.ErrorCodes;

namespace Wam.Games.Exceptions;

public class WamGameException(WamGameErrorCode error, string message) : WamException(error, message)
{
    
}