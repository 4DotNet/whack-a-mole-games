using System.Text.RegularExpressions;
using Wam.Games.ErrorCodes;
using Wam.Games.Exceptions;

namespace Wam.Games.ExtensionMethods;

public static class StringExtensions
{

    private const string GameCodeRegex = @"^[A-Z0-9]{6,8}$";


    public static string ValidateGameCode(this string gameCode)
    {
        gameCode = gameCode.ToUpperInvariant();
        if (string.IsNullOrWhiteSpace(gameCode))
        {
            throw new WamGameException(WamGameErrorCode.InvalidGameCode, "Game code cannot be empty");
        }

        if (!Regex.IsMatch(gameCode, GameCodeRegex))
        {
            throw new WamGameException(WamGameErrorCode.InvalidGameCode, "Game code must be 6-8 characters long and only contain letters and numbers");
        }
        return gameCode;
    }

}