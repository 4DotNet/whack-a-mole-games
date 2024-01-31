using Wam.Core.ErrorCodes;

namespace Wam.Games.ErrorCodes;

public abstract class WamGameErrorCode : WamErrorCode
{
    public static WamGameErrorCode GameNotFound => new GameNotFound();
    public static WamGameErrorCode GameIsFull => new GameIsFull();
    public static WamGameErrorCode InvalidState => new InvalidState();
    public static WamGameErrorCode InvalidPlayer => new InvalidPlayer();
    public static WamGameErrorCode NewGameAlreadyExists => new NewGameAlreadyExists();
    public static WamGameErrorCode ActiveGameAlreadyExists => new ActiveGameAlreadyExists();
    public static WamGameErrorCode InvalidGameVoucher => new InvalidGameVoucher();
    public static WamGameErrorCode PlayerNotFound => new PlayerNotFound();
    public static WamGameErrorCode InvalidGameCode => new InvalidGameCode();

    public override string Namespace => $"{base.Namespace}.Games";
}


public class GameNotFound : WamGameErrorCode
{
    public override string Code => nameof(GameNotFound);
}

public class GameIsFull : WamGameErrorCode
{
    public override string Code => nameof(GameIsFull);
}

public class InvalidState : WamGameErrorCode
{
    public override string Code => nameof(InvalidState);
}

public class InvalidPlayer : WamGameErrorCode
{
    public override string Code => nameof(InvalidPlayer);
}

public class NewGameAlreadyExists : WamGameErrorCode
{
    public override string Code => nameof(NewGameAlreadyExists);
}

public class ActiveGameAlreadyExists : WamGameErrorCode
{
    public override string Code => nameof(ActiveGameAlreadyExists);
}

public class InvalidGameVoucher : WamGameErrorCode
{
    public override string Code => nameof(InvalidGameVoucher);
}

public class PlayerNotFound : WamGameErrorCode
{
    public override string Code => nameof(PlayerNotFound);
}

public class InvalidGameCode : WamGameErrorCode
{
    public override string Code => nameof(InvalidGameCode);
}