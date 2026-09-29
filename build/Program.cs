using System;
using Cake.Frosting;

return new CakeHost()
    .UseWorkingDirectory("../")
    .UseContext<BuildScripts.BuildContext>()
    .Run(args);
