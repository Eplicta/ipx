using Eplicta.Ipx.Tool;

return IpxCommandLine.Run(args, Console.Out, Console.Error, !Console.IsOutputRedirected && Environment.GetEnvironmentVariable("NO_COLOR") is null);
