namespace io.radston12.fakerank.Commands.FakeRankChildren
{
    using System;

    using CommandSystem;

    using Extensions;

    using io.radston12.fakerank.Helpers;

    using Exiled.API.Features;

    /// <summary>
    /// Clears a fakerank for the player executing or a custom player
    /// </summary>
    public class Clear : ICommand
    {
        public string Command { get; } = "clear";
        public string Description { get; } = "Clears a fakerank";
        public string[] Aliases { get; } = new string[] { };

        /// <inheritdoc/>
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            Player player = Player.Get(sender);
            Player target = player;

            if (player.IsHost)
            {
                response = "[FAKERANK] This command is only for players!";
                return false;
            }

            if (!player.RemoteAdminAccess)
            {
                response = "[FAKERANK] Well well well How can u enter remote admin commands without remote admin permissions?";
                return false;
            }

            if (arguments.Count != 0)
                target = RAUserIdParser.getByCommandArgument(arguments.At(0));


            if (target == null)
            {
                response = "[FAKERANK] Player not found!";
                return false;
            }

            if (FakeRankStorage.Storage.ContainsKey(player.UserId))
            {
                FakeRankStorage.Storage.Remove(player.UserId);
                FakeRankStorage.Save();
            }

            player.RankName = "";
            player.RankColor = "default";

            if (arguments.Count != 0)
                response = $"[FAKERANK] Nuked rank of {target.Nickname}!";
            else
                response = $"[FAKERANK] Nuked your rank!";

            return false;
        }
    }
}