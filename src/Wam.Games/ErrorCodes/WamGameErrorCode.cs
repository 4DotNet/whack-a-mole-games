using Wam.Core.ErrorCodes;

namespace Wam.Games.ErrorCodes;

public abstract class WamGameErrorCode : WamErrorCode
{
    public static WamGameErrorCode GameNotFound => new GameNotFound();
    public static WamGameErrorCode GameIsFull => new GameNotFound();
    public static WamGameErrorCode InvalidState => new GameNotFound();
    public static WamGameErrorCode InvalidPlayer => new GameNotFound();
    public static WamGameErrorCode NewGameAlreadyExists => new GameNotFound();
    public static WamGameErrorCode ActiveGameAlreadyExists => new GameNotFound();
    public static WamGameErrorCode InvalidGameVoucher => new GameNotFound();
    public static WamGameErrorCode PlayerNotFound => new PlayerNotFound();

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