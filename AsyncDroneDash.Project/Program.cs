namespace AsyncDroneDash.Project;

public static class Program
{
    public static Task Main()
    {
        var menu = new Presentation.DroneDashMenu();
        return menu.RunAsync();
    }
}
